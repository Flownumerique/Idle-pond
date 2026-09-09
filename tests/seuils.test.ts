/**
 * Les seuils — amendement v1.1 §2.C, relu par le noyau v1.0 §1.3.
 *
 * « Une espèce à cent produit exactement ×16 sa base, et pose le drapeau +3 %
 * global. » Ce qui a changé le 2026-09-09 : cent se lit sur le NIVEAU, pas sur
 * un effectif. Le seuil tombe donc à l'achat, plus jamais avec le temps.
 *
 * Le ×16 n'est pas décoratif : `D = 2.31` a été calibré contre cette lecture.
 * Si la table redevenait multiplicative — ×1024 à cent —, tout le calibrage
 * serait à refaire, et ce test est ce qui le dirait.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { BONUS_GLOBAL_A_CENT_INDIVIDUS, SEUIL_DU_DRAPEAU_PERMANENT } from '../src/noyau/constantes'
import { ameliorer, debloquer, eclore, etatInitial, tick } from '../src/noyau/noyau'
import {
  debitBaseDeLEspece,
  multiplicateurDeSeuil,
  multiplicateurDesDrapeaux,
  productionDeLEspece,
} from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'

const ESPECE = ESPECES[0]

function auNiveau(niveau: number): EtatJeu {
  const depart = etatInitial(1)
  return {
    ...depart,
    cycle: {
      ...depart.cycle,
      especes: { [ESPECE.id]: { debloquee: true, niveau } },
    },
  }
}

describe('seuils', () => {
  it('la table est cumulée : on lit le multiplicateur du seuil franchi', () => {
    expect(multiplicateurDeSeuil(0)).toBe(1)
    expect(multiplicateurDeSeuil(9)).toBe(1)
    expect(multiplicateurDeSeuil(10)).toBe(2)
    expect(multiplicateurDeSeuil(24)).toBe(2)
    expect(multiplicateurDeSeuil(25)).toBe(4)
    expect(multiplicateurDeSeuil(50)).toBe(8)
    expect(multiplicateurDeSeuil(99)).toBe(8)
    expect(multiplicateurDeSeuil(100)).toBe(16)
    expect(multiplicateurDeSeuil(10_000)).toBe(16)
  })

  it('cent ne vaut JAMAIS ×1024', () => {
    // 2 × 4 × 8 × 16 : la lecture multiplicative, celle contre laquelle il ne
    // faut pas calibrer. Si elle repassait, `D = 2.31` serait faux.
    expect(multiplicateurDeSeuil(100)).not.toBe(1024)
  })

  it('une espèce au niveau cent produit exactement ×16 sa base', () => {
    const etat = auNiveau(SEUIL_DU_DRAPEAU_PERMANENT)
    const base = debitBaseDeLEspece(ESPECE).mul(SEUIL_DU_DRAPEAU_PERMANENT)
    const obtenue = productionDeLEspece(etat, ESPECE)
    // Aucun drapeau posé : il tombe à l'achat, et personne n'a rien acheté ici.
    expect(multiplicateurDesDrapeaux(etat)).toBe(1)
    expect(obtenue.div(base).toNumber()).toBeCloseTo(16, 9)
  })

  it('le seuil se lit sur le niveau, et le temps n’y change rien', () => {
    const neuf = auNiveau(9)
    const attendu = debitBaseDeLEspece(ESPECE).mul(9)
    expect(productionDeLEspece(neuf, ESPECE).div(attendu).toNumber()).toBeCloseTo(1, 9)
    // Huit heures plus tard, toujours ×1 : rien ne franchit un seuil tout seul.
    expect(productionDeLEspece(tick(neuf, 8 * 3600), ESPECE).eq(productionDeLEspece(neuf, ESPECE))).toBe(true)
  })

  it('le centième niveau pose le drapeau permanent À L’ACHAT, et il vaut +3 %', () => {
    const depart = etatInitial(1)
    let etat: EtatJeu = {
      ...depart,
      cycle: { ...depart.cycle, manaCourant: new Decimal('1e30') },
      permanent: { ...depart.permanent, contenanceMana: new Decimal('1e40') },
    }
    etat = debloquer(etat, ESPECE.id)
    while (etat.cycle.especes[ESPECE.id].niveau < SEUIL_DU_DRAPEAU_PERMANENT) {
      etat = ameliorer(etat, ESPECE.id)
    }
    expect(etat.permanent.especesAyantAtteintCent).toContain(ESPECE.id)
    expect(multiplicateurDesDrapeaux(etat)).toBeCloseTo(1 + BONUS_GLOBAL_A_CENT_INDIVIDUS, 9)
  })

  it('le drapeau survit à l’éclosion, le multiplicateur de seuil non', () => {
    const depart = etatInitial(1)
    const avecDrapeau: EtatJeu = {
      ...auNiveau(SEUIL_DU_DRAPEAU_PERMANENT),
      permanent: { ...depart.permanent, especesAyantAtteintCent: [ESPECE.id] },
    }
    const apresEclosion = eclore(avecDrapeau)
    expect(apresEclosion.permanent.especesAyantAtteintCent).toContain(ESPECE.id)
    expect(apresEclosion.cycle.especes).toEqual({})
    expect(productionDeLEspece(apresEclosion, ESPECE).eq(new Decimal(0))).toBe(true)
  })
})
