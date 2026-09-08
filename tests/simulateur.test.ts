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
import { ACQUIS_MAX, CONTENANCE_INITIALE, K_TAUX_DE_REPEUPLEMENT, NOMBRE_D_ECLOSIONS_VISE } from '../src/noyau/constantes'
import { POLITIQUE_PAR_DEFAUT, simuler } from '../src/simulateur/simulateur'
import { relever } from '../src/adaptateurs/telemetrie'
import { PALIERS_LIVRES, TYPE_MANA_NATAL } from '../src/donnees/assises'
import { etatInitial, tick } from '../src/noyau/noyau'
import { vitesseDeRepeuplement } from '../src/noyau/densite'

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

describe('V11 — la densité est découplée du repeuplement', () => {
  /**
   * Ce test RELEVAIT une dégénérescence ; depuis la décision du 2026-09-08 il
   * la GARANTIT éteinte, et c'est le renversement qu'annonçait son ancien
   * commentaire.
   *
   * Ce qu'il protège : le délai entre l'achat d'une place et son effet. C'est
   * lui qui fait le jeu — « le joueur achète de la place et de la qualité de
   * place, la population suit » (GDD §7.2) — et il avait disparu, la densité
   * valant `pointe^α` et croissant donc sans borne. τ tombait de 300 s à
   * 10⁻⁴ s en quinze cycles.
   */
  it('τ ne s’effondre plus : il vaut sa graine, du premier cycle au dernier', () => {
    const tauNominal = 1 / K_TAUX_DE_REPEUPLEMENT
    for (const cycles of [1, 6, NOMBRE_D_ECLOSIONS_VISE]) {
      const tau = relever(simuler(cycles).etat).tauDeRepeuplementSecondes
      expect(tau, `τ après ${cycles} cycles`).toBeCloseTo(tauNominal, 6)
    }
  })

  it('la densité n’a plus qu’un débouché : l’acquis de séjour', () => {
    // Une eau dense doit encore accélérer le SÉJOUR — c'est le canal du §2.B,
    // celui qu'on garde — et ne plus rien changer au repeuplement. Vérifier les
    // deux ensemble est ce qui distingue un découplage d'une suppression.
    const depart = etatInitial(1)
    const dense: EtatJeu = {
      ...depart,
      permanent: { ...depart.permanent, densites: depart.permanent.densites.map(() => 1e6) },
    }
    expect(tick(dense, 3600).cycle.acquisDeSejour).toBeGreaterThan(
      tick(depart, 3600).cycle.acquisDeSejour,
    )
    expect(vitesseDeRepeuplement()).toBe(K_TAUX_DE_REPEUPLEMENT)
  })
})
