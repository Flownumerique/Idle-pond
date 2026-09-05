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
import { NOMBRE_D_ECLOSIONS_VISE } from '../src/noyau/constantes'
import { simuler } from '../src/simulateur/simulateur'
import { TYPE_MANA_NATAL } from '../src/donnees/assises'

describe('simulateur', () => {
  it(`enchaîne ${NOMBRE_D_ECLOSIONS_VISE} cycles sans jouer`, () => {
    const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.cycleNonConvergent, 'un cycle n’a pas convergé').toBeNull()
    expect(resultat.cyclesAcheves).toBe(NOMBRE_D_ECLOSIONS_VISE)
    expect(resultat.releve.cycles.length).toBe(NOMBRE_D_ECLOSIONS_VISE)
    for (const cycle of resultat.releve.cycles) {
      expect(cycle.dureeActiveSecondes).toBeGreaterThan(0)
      expect(cycle.paliersOuverts).toBeGreaterThan(0)
    }
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
