/**
 * Les deux canaux de captation et le plafond de maturation — GDD §3 et §3.0.
 *
 *   captation/s =   débit_natif(population vivante présente)
 *                 + débit_acclimaté(part_mûre(palier) × rendement_acclimatation)
 *
 * Ce que ces tests protègent avant tout, c'est que la somme reste ADDITIVE et
 * que le second terme reste BORNÉ. Le §3.0 dit pourquoi : « les deux canaux sont
 * additifs, et une somme est dominée par son terme non borné ». Si l'acclimaté
 * suivait la densité absolue — qui ne redescend jamais —, la captation cesserait
 * de dépendre de la population vers la mi-partie et les trois conséquences du
 * §3 cesseraient d'être vraies.
 */
import { describe, expect, it } from 'vitest'
import {
  INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE,
  PLACE_QUI_DILUE_A_MOITIE,
  TAU_MATURATION_HEURES,
} from '../src/noyau/constantes'
import {
  partMureDuPalier,
  placeDuPalier,
  productionAcclimateeDuPalier,
  productionDuBanc,
  productionTotaleParSeconde,
  tauxBaseDuPalier,
} from '../src/noyau/economie'
import { PART_MURE_D_UNE_EAU_INTOUCHEE, cibleDeMaturation } from '../src/noyau/maturation'
import { acheterPlace, convaincre, eclore, etatInitial, tick } from '../src/noyau/noyau'
import { BANCS } from '../src/donnees/paliers'
import type { EtatJeu } from '../src/noyau/types'
import Decimal from 'break_infinity.js'

const H = 3600

/**
 * Une partie neuve, mais avec de quoi acheter.
 *
 * `etatInitial` sort de l'œuf avec exactement le prix d'une conviction : sans
 * cette bourse, les achats de place échouent en silence et les tests
 * mesureraient une eau que personne n'a peuplée.
 */
function avecDeQuoiAcheter(): EtatJeu {
  const etat = etatInitial(1)
  return {
    ...etat,
    cycle: { ...etat.cycle, manaCourant: new Decimal('1e9') },
    permanent: { ...etat.permanent, contenanceMana: new Decimal('1e12') },
  }
}

/** Le même, avec un banc convaincu et de la place faite. */
function palierPeuple(places: number): EtatJeu {
  let etat = convaincre(avecDeQuoiAcheter(), BANCS[0].id)
  for (let i = 0; i < places; i += 1) etat = acheterPlace(etat, BANCS[0].id)
  return etat
}

describe('§3 — deux canaux, et ils s’additionnent', () => {
  it('la captation totale est exactement la somme des deux, jamais un produit', () => {
    const etat = tick(convaincre(etatInitial(1), BANCS[0].id), 600)

    const natif = productionDuBanc(etat, BANCS[0])
    const acclimate = productionAcclimateeDuPalier(etat, 0)
    expect(natif.gt(0)).toBe(true)
    expect(acclimate.gt(0)).toBe(true)

    const total = productionTotaleParSeconde(etat)
    expect(total.sub(natif.add(acclimate)).abs().div(total).toNumber()).toBeLessThan(1e-12)
  })

  it('l’acclimaté ne dépend d’aucun vivant : il tombe sur une eau vide', () => {
    // C'est ce qui donne un revenu au héros à l'instant où il rouvre une
    // galerie, avant d'y avoir ramené qui que ce soit — et c'est pourquoi le
    // §3 tient à ce que les canaux soient additifs plutôt que multiplicatifs.
    const neuf = etatInitial(1)
    expect(Object.keys(neuf.cycle.bancs)).toEqual([])
    expect(productionAcclimateeDuPalier(neuf, 0).gt(0)).toBe(true)
    expect(productionTotaleParSeconde(neuf).gt(0)).toBe(true)
  })

  it('il reste borné — « très bas » face à un natif à 100 %', () => {
    // La borne est structurelle : part_mûre ∈ [0,1], donc l'acclimaté d'un
    // palier ne peut jamais dépasser `n` individus équivalents. Un banc peuplé
    // en porte cent, et son multiplicateur de seuil par-dessus.
    const palier = 0
    const plafond = tauxBaseDuPalier(palier).mul(INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE)
    const neuf = etatInitial(1)

    // Une densité absurde, et une eau entièrement mûre : le pire cas. C'est
    // exactement la configuration que le §3.0 refuse de laisser exploser, et
    // elle doit rester sous le plafond des individus équivalents.
    const sature: EtatJeu = {
      ...neuf,
      permanent: {
        ...neuf.permanent,
        densites: neuf.permanent.densites.map(() => 1e30),
        partsMures: neuf.permanent.partsMures.map(() => 1),
      },
    }
    expect(productionAcclimateeDuPalier(sature, palier).lte(plafond)).toBe(true)
    // Et il ne bouge pas d'un iota quand la densité est multipliée par mille.
    const millefois: EtatJeu = {
      ...sature,
      permanent: { ...sature.permanent, densites: sature.permanent.densites.map(() => 1e33) },
    }
    expect(productionAcclimateeDuPalier(millefois, palier).eq(productionAcclimateeDuPalier(sature, palier))).toBe(true)
  })
})

describe('§3.0 — le plafond de maturation', () => {
  it('une eau que rien n’habite est mûre, et le reste', () => {
    // §6.5, la mer relique : « peu de vivant, aucun réensemencement, des ères de
    // vieillissement ininterrompu ». Le héros descend dans du mûr.
    const neuf = etatInitial(1)
    expect(partMureDuPalier(neuf, 0)).toBe(PART_MURE_D_UNE_EAU_INTOUCHEE)
    expect(partMureDuPalier(tick(neuf, 100 * H), 0)).toBeCloseTo(1, 6)
  })

  it('peupler un palier dilue son type, donc écrase son rendement acclimaté', () => {
    // « Peupler un palier le noie de signature vive, donc dilue son type mûr,
    // donc fait baisser son rendement acclimaté. » C'est l'arbitrage entier.
    const etat = palierPeuple(30)
    expect(placeDuPalier(etat, 0)).toBeGreaterThan(PLACE_QUI_DILUE_A_MOITIE)

    const avant = partMureDuPalier(etat, 0)
    // Six temps caractéristiques : il reste e^-6, soit deux millièmes de
    // l'écart initial. À quatre il en resterait dix fois plus, et le test
    // mesurerait la tolérance plutôt que la convergence.
    const apres = partMureDuPalier(tick(etat, 6 * TAU_MATURATION_HEURES * H), 0)
    expect(apres).toBeLessThan(avant)
    expect(apres).toBeCloseTo(cibleDeMaturation(placeDuPalier(etat, 0)), 2)
  })

  it('laissé maigre, le type mûrit et le rendement remonte', () => {
    // L'autre branche de la table du §3.0. Ce qui remonte est une PROPORTION :
    // « la densité absolue continue de monter et ne redescend jamais, seule la
    // proportion bouge ».
    const etat = tick(palierPeuple(30), 4 * TAU_MATURATION_HEURES * H)
    const dilue = partMureDuPalier(etat, 0)

    // L'éclosion emporte la population : l'eau redevient maigre, donc mûrit.
    const apresEclosion = tick(eclore(etat), 4 * TAU_MATURATION_HEURES * H)
    expect(partMureDuPalier(apresEclosion, 0)).toBeGreaterThan(dilue)
  })

  it('la cible ne dépend que de la place, et vaut la moitié au seuil nommé', () => {
    expect(cibleDeMaturation(0)).toBe(1)
    expect(cibleDeMaturation(PLACE_QUI_DILUE_A_MOITIE)).toBeCloseTo(0.5, 12)
    expect(cibleDeMaturation(1e9)).toBeLessThan(1e-6)
    expect(cibleDeMaturation(1e9)).toBeGreaterThan(0)
  })

  it('la part mûre survit à l’éclosion : c’est une propriété de l’eau', () => {
    const etat = tick(palierPeuple(30), 2 * TAU_MATURATION_HEURES * H)

    const avant = partMureDuPalier(etat, 0)
    expect(avant).toBeLessThan(1)
    // Juste après l'éclosion, rien n'a encore eu le temps de mûrir : la valeur
    // est transmise telle quelle, elle ne se réinitialise pas.
    expect(partMureDuPalier(eclore(etat), 0)).toBeCloseTo(avant, 12)
  })

  it('l’arbitrage est réel : peupler gagne en débit et perd en eau mûre', () => {
    // Les deux colonnes de la table du §3.0, mesurées côte à côte sur le même
    // palier. Sans ça, « l'arbitrage » ne serait qu'une intention.
    const duree = 4 * TAU_MATURATION_HEURES * H
    const base = convaincre(avecDeQuoiAcheter(), BANCS[0].id)
    const peuple = tick(palierPeuple(30), duree)
    const maigre = tick(base, duree)

    expect(productionDuBanc(peuple, BANCS[0]).gt(productionDuBanc(maigre, BANCS[0]))).toBe(true)
    expect(productionAcclimateeDuPalier(peuple, 0).lt(productionAcclimateeDuPalier(maigre, 0))).toBe(true)
  })
})
