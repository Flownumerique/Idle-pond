/**
 * §8.4 — plancher garanti sur l'assise I.
 *
 * | Garantie              | Valeur                                    |
 * |-----------------------|-------------------------------------------|
 * | Premier succès        | Dans les DEUX PREMIÈRES MINUTES           |
 * | Première demi-heure   | Un déclenchement toutes les 3 à 5 minutes |
 * | Première éclosion     | Un franchissement, OBLIGATOIREMENT        |
 *
 * Ce n'est pas une intention de design, c'est une garantie chiffrée : elle se
 * mesure, ou elle n'existe pas. Le test joue les trente premières minutes avec
 * un joueur qui achète le moins cher dès qu'il peut, et relève les instants.
 */
import { describe, expect, it } from 'vitest'
import type { EtatJeu, SuccesId } from '../src/noyau/types'
import {
  CADENCE_MAX_ENTRE_SUCCES_SECONDES,
  FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES,
  PREMIER_SUCCES_AVANT_SECONDES,
} from '../src/noyau/constantes'
import { eclore, etatInitial, tickDetaille } from '../src/noyau/noyau'
import { SUCCES } from '../src/donnees/succes/index'
import { joueUneDemiHeure, type Declenchement } from './joueur'

describe('plancher de cadence de l’assise I', () => {
  const releve = joueUneDemiHeure()

  it('le premier succès tombe en moins de deux minutes', () => {
    expect(releve.length, 'aucun succès déclenché').toBeGreaterThan(0)
    expect(releve[0].instantSecondes).toBeLessThan(PREMIER_SUCCES_AVANT_SECONDES)
  })

  it('un déclenchement au moins toutes les cinq minutes sur la première demi-heure', () => {
    const trous: string[] = []
    let precedent = 0
    for (const declenchement of releve) {
      const ecart = declenchement.instantSecondes - precedent
      if (ecart > CADENCE_MAX_ENTRE_SUCCES_SECONDES) {
        trous.push(`${(ecart / 60).toFixed(1)} min avant ${declenchement.id}`)
      }
      precedent = declenchement.instantSecondes
    }
    const fin = FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES - precedent
    if (fin > CADENCE_MAX_ENTRE_SUCCES_SECONDES) {
      trous.push(`${(fin / 60).toFixed(1)} min de silence jusqu’à la trentième minute`)
    }
    expect(trous, 'la cadence du §8.4 est trouée').toEqual([])
  })

  it('la première éclosion déclenche un franchissement, obligatoirement', () => {
    let etat: EtatJeu = etatInitial(1)
    etat = eclore(etat)
    const declenches: readonly SuccesId[] = tickDetaille(etat, 0.1).declenches
    const franchissements = declenches.filter(
      (id) => SUCCES.find((s) => s.id === id)?.famille === 'franchissement',
    )
    expect(franchissements.length).toBeGreaterThan(0)
  })

  it('la cadence ne tient pas à un seul succès qui se répéterait', () => {
    const identifiants = new Set(releve.map((d: Declenchement) => d.id))
    expect(identifiants.size).toBe(releve.length)
  })
})
