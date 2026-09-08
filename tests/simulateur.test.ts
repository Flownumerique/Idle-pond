/**
 * Critère d'acceptation du jalon v0.1 :
 * « le simulateur tourne 15 cycles sans jouer, et le test d'équivalence de pas
 * passe. »
 *
 * Le simulateur n'a pas de moteur à lui : il appelle le même `tick` que le jeu
 * avec un `dt` plus grand. Ce test vérifie donc deux choses à la fois — que
 * quinze cycles s'enchaînent, et que les invariants du Tier 0 tiennent sur
 * toute la durée, pas seulement à l'arrivée.
 */
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { ACQUIS_MAX, CONTENANCE_INITIALE, NOMBRE_D_ECLOSIONS_VISE } from '../src/noyau/constantes'
import { POLITIQUE_PAR_DEFAUT, simuler } from '../src/simulateur/simulateur'
import { relever } from '../src/adaptateurs/telemetrie'
import { PALIERS_LIVRES, TYPE_MANA_NATAL } from '../src/donnees/assises'

describe('simulateur', () => {
  it('une partie sans UI atteint l’éclosion 2 en headless', () => {
    // Critère d'acceptation du §6 de l'amendement v1.1. Il porte sur le monde
    // LIVRÉ — la Noue et ses six paliers —, pas sur les 62 que le simulateur
    // mesure : c'est le jeu qu'un joueur touche qui doit boucler.
    const partie = simuler(2, undefined, 1, undefined, PALIERS_LIVRES)
    expect(partie.cycleNonConvergent).toBeNull()
    expect(partie.etat.permanent.nombreEclosions).toBe(2)
    expect(partie.etat.permanent.contenanceMana.gt(CONTENANCE_INITIALE)).toBe(true)
    expect(partie.etat.permanent.densites[0]).toBeGreaterThan(0)
  })

  it(`enchaîne ${NOMBRE_D_ECLOSIONS_VISE} cycles sans jouer`, () => {
    const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.cycleNonConvergent, 'un cycle n’a pas convergé').toBeNull()
    expect(resultat.cyclesAcheves).toBe(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.releve.cycles.length).toBe(NOMBRE_D_ECLOSIONS_VISE)
    for (const cycle of resultat.releve.cycles) {
      expect(cycle.dureeEcouleeSecondes).toBeGreaterThan(0)
      expect(cycle.paliersOuverts).toBeGreaterThan(0)
    }
  })

  it('sépare le temps ACTIF du temps ÉCOULÉ', () => {
    // §11 : « durée de cycle, active et calendaire », et « intervalle réel
    // entre deux sessions | distingue temps actif et temps calendaire ».
    // Les confondre fait lire les ~600 h calendaires du §5.4 comme si c'étaient
    // les ~38 h actives — l'erreur exacte que les jalons v0.1 et v0.2 ont
    // rapportée deux fois.
    const resultat = simuler(5)
    expect(resultat.tempsActifSecondes).toBeGreaterThan(0)
    expect(resultat.tempsEcouleSecondes).toBeGreaterThan(resultat.tempsActifSecondes)
    expect(resultat.sessions.length).toBeGreaterThan(0)
    const somme = resultat.sessions.reduce((t, s) => t + s.secondesActives, 0)
    expect(somme).toBeCloseTo(resultat.tempsActifSecondes, 6)
  })

  it('l’intervalle de check-in est le seul réglage de temps calendaire', () => {
    // §5.4 : « aucun réglage de paramètre ne produira de croissance de cycle en
    // temps actif — seules les politiques ». Le vérifier plutôt que d'y croire :
    // doubler l'absence doit à peu près doubler le calendaire, et laisser
    // l'actif tranquille.
    const court = simuler(6, { ...POLITIQUE_PAR_DEFAUT, intervalleDeCheckInSecondes: 2 * 3600 })
    const long = simuler(6, { ...POLITIQUE_PAR_DEFAUT, intervalleDeCheckInSecondes: 8 * 3600 })
    expect(long.tempsEcouleSecondes).toBeGreaterThan(court.tempsEcouleSecondes * 2)
    const ecartActif = Math.abs(long.tempsActifSecondes / court.tempsActifSecondes - 1)
    expect(ecartActif).toBeLessThan(0.5)
  })

  it('le joueur rentre dans l’œuf sur la saturation, pas sur un minuteur', () => {
    // La seule vraie décision du §6.4, rendue réelle par l'acquis saturant du
    // §2.B : à chaque éclosion, l'acquis doit avoir fait son travail.
    let cycles = 0
    simuler(4, undefined, 1, (etat) => {
      if (etat.permanent.nombreEclosions === cycles) return
      cycles = etat.permanent.nombreEclosions
      // Juste après l'éclosion l'acquis est remis à zéro ; ce qui compte est
      // que la contenance ait bien été multipliée par un acquis saturé.
      expect(etat.cycle.acquisDeSejour).toBe(0)
    })
    const partie = simuler(4)
    const attendu = Math.pow(1 + ACQUIS_MAX * POLITIQUE_PAR_DEFAUT.fractionDeSaturationPourEclore, 4)
    expect(partie.etat.permanent.contenanceMana.div(CONTENANCE_INITIALE).toNumber()).toBeGreaterThan(attendu)
  })

  it('la densité ne redescend jamais (Tier 0)', () => {
    let precedentes: readonly number[] | null = null
    const verifier = (etat: EtatJeu) => {
      if (precedentes !== null) {
        etat.permanent.densites.forEach((densite, palier) => {
          expect(densite, `densité du palier ${palier}`).toBeGreaterThanOrEqual(precedentes![palier])
        })
      }
      precedentes = etat.permanent.densites
    }
    simuler(NOMBRE_D_ECLOSIONS_VISE, undefined, 1, verifier)
    expect(precedentes).not.toBeNull()
    expect(precedentes!.some((d) => d > 0)).toBe(true)
  })

  it('les acquis permanents ne se reperdent jamais', () => {
    let eclosions = 0
    let contenance = 0
    let foi = 0
    let compteurs = 0
    simuler(NOMBRE_D_ECLOSIONS_VISE, undefined, 1, (etat) => {
      // Un être surévolué conserve ses acquis à vie : le héros ne repaie
      // jamais son acclimatation, et l'éclosion ne la remet pas à zéro.
      expect(etat.permanent.acclimatations[TYPE_MANA_NATAL]).toBe(1)
      expect(etat.permanent.nombreEclosions).toBeGreaterThanOrEqual(eclosions)
      expect(etat.permanent.contenanceMana.toNumber()).toBeGreaterThanOrEqual(contenance)
      expect(etat.permanent.foi.toNumber()).toBeGreaterThanOrEqual(foi)
      const somme = Object.values(etat.permanent.compteursTechnique).reduce((a, b) => a + b, 0)
      expect(somme, 'un compteur de technique a reculé : on ne désapprend pas').toBeGreaterThanOrEqual(compteurs)
      eclosions = etat.permanent.nombreEclosions
      contenance = etat.permanent.contenanceMana.toNumber()
      foi = etat.permanent.foi.toNumber()
      compteurs = somme
    })
    expect(eclosions).toBe(NOMBRE_D_ECLOSIONS_VISE)
  })

  it('l’éclosion emporte le peuplement et la géométrie, et rien d’autre', () => {
    const resultat = simuler(2)
    expect(resultat.etat.cycle.paliersOuverts).toBe(1)
    expect(Object.keys(resultat.etat.cycle.bancs)).toEqual([])
    expect(resultat.etat.permanent.profondeurMaxAtteinte).toBeGreaterThan(1)
    // Le mana courant expire vers l'ambiant. Il n'est pas détruit : aucun
    // système d'IdlePond ne se comporte comme un puits.
    expect(resultat.etat.permanent.manaAmbiant.gt(0)).toBe(true)
  })

  it('la progression descend réellement d’un cycle à l’autre', () => {
    const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)
    const premier = resultat.releve.cycles[0]
    const dernier = resultat.releve.cycles[resultat.releve.cycles.length - 1]
    expect(dernier.paliersOuverts).toBeGreaterThan(premier.paliersOuverts)
  })

  it('la fraction passée à redescendre est relevée à chaque cycle', () => {
    // Risque n° 1 du §15 : « la redescente devient le jeu ». La métrique
    // existe dès le premier jalon, faute de quoi elle ne dira jamais rien.
    const resultat = simuler(5)
    for (const cycle of resultat.releve.cycles) {
      expect(cycle.fractionEnRedescente).toBeGreaterThanOrEqual(0)
      expect(cycle.fractionEnRedescente).toBeLessThanOrEqual(1)
    }
    expect(resultat.releve.cycles.slice(1).some((c) => c.fractionEnRedescente > 0)).toBe(true)
  })
})

describe('ce que la mesure signale au jalon v0.3', () => {
  it('le repeuplement dégénère : τ s’effondre au fil des cycles', () => {
    // Ce test ne défend pas le comportement, il le RELÈVE. La densité vaut
    // `pointe^α` (§2.A), donc elle croît avec la production sans borne ; la
    // coupler à `k` — canal du §6.5 de la v1.0 — rend le repeuplement quasi
    // instantané et fait disparaître la mécanique. Le §2.B, lui, fait passer la
    // densité par un rapport que la saturation borne.
    //
    // C'est la décision ouverte V11. Si l'auteur tranche pour découpler, ce
    // test change de sens et devient une garantie ; s'il tranche pour borner le
    // couplage, il devient une borne. En l'état il empêche seulement que la
    // dégénérescence passe inaperçue.
    const depart = relever(simuler(1).etat).tauDeRepeuplementSecondes
    const arrivee = relever(simuler(6).etat).tauDeRepeuplementSecondes
    expect(depart).toBeGreaterThan(arrivee)
    expect(arrivee).toBeLessThan(1)
  })
})
