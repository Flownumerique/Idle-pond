/**
 * Les seuils — amendement v1.1 §2.C.
 *
 * « Une espèce à 100 individus produit exactement ×16 sa base, et pose le
 * drapeau +3 % global. »
 *
 * Le ×16 n'est pas décoratif : `D = 2.31` a été calibré contre cette lecture.
 * Si la table redevenait multiplicative — ×1024 à cent individus — tout le
 * calibrage serait à refaire, et ce test est ce qui le dirait.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { BONUS_GLOBAL_A_CENT_INDIVIDUS, SEUIL_DU_DRAPEAU_PERMANENT } from '../src/noyau/constantes'
import { eclore, etatInitial, tick } from '../src/noyau/noyau'
import {
  multiplicateurDeSeuil,
  multiplicateurDesDrapeaux,
  productionDuBanc,
  tauxBaseDuBanc,
} from '../src/noyau/economie'
import { PALIERS } from '../src/donnees/paliers'

const BANC = PALIERS[0].bancs[0]

function avecEffectif(effectif: number): EtatJeu {
  const depart = etatInitial(1)
  return {
    ...depart,
    cycle: {
      ...depart.cycle,
      bancs: { [BANC.id]: { place: SEUIL_DU_DRAPEAU_PERMANENT, effectif } },
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

  it('cent individus ne valent JAMAIS ×1024', () => {
    // 2 × 4 × 8 × 16 : la lecture multiplicative, celle contre laquelle il ne
    // faut pas calibrer. Si elle repassait, `D = 2.31` serait faux.
    expect(multiplicateurDeSeuil(100)).not.toBe(1024)
  })

  it('une espèce à cent individus produit exactement ×16 sa base', () => {
    const etat = avecEffectif(SEUIL_DU_DRAPEAU_PERMANENT)
    const base = tauxBaseDuBanc(BANC).mul(SEUIL_DU_DRAPEAU_PERMANENT)
    const obtenue = productionDuBanc(etat, BANC)
    // Aucun drapeau posé tant que le tick n'a pas relu l'état : la base est nue.
    expect(multiplicateurDesDrapeaux(etat)).toBe(1)
    expect(obtenue.div(base).toNumber()).toBeCloseTo(16, 9)
  })

  it('le seuil se lit sur l’effectif, pas sur la place achetée', () => {
    const depart = etatInitial(1)
    const beaucoupDePlaceVide: EtatJeu = {
      ...depart,
      cycle: { ...depart.cycle, bancs: { [BANC.id]: { place: 1000, effectif: 9 } } },
    }
    expect(productionDuBanc(beaucoupDePlaceVide, BANC).div(tauxBaseDuBanc(BANC).mul(9)).toNumber()).toBeCloseTo(1, 9)
  })

  it('cent individus posent le drapeau permanent, et il vaut +3 %', () => {
    const apres = tick(avecEffectif(SEUIL_DU_DRAPEAU_PERMANENT), 0.1)
    expect(apres.permanent.especesAyantAtteintCent).toContain(BANC.espece)
    expect(multiplicateurDesDrapeaux(apres)).toBeCloseTo(1 + BONUS_GLOBAL_A_CENT_INDIVIDUS, 9)
  })

  it('le drapeau survit à l’éclosion, le multiplicateur de seuil non', () => {
    const apres = tick(avecEffectif(SEUIL_DU_DRAPEAU_PERMANENT), 0.1)
    const apresEclosion = eclore(apres)
    expect(apresEclosion.permanent.especesAyantAtteintCent).toContain(BANC.espece)
    expect(apresEclosion.cycle.bancs).toEqual({})
    expect(productionDuBanc(apresEclosion, BANC).eq(new Decimal(0))).toBe(true)
  })
})
