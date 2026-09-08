/**
 * IdlePond — la part mûre d'un palier, et le plafond de maturation.
 *
 * GDD §3.0, décision structurelle tranchée en v2.3 du GDD et portée au code le
 * 2026-09-08. C'est ce qui BORNE le canal acclimaté :
 *
 *   part_mûre(palier) = charge du type mûr ÷ charge totale du palier
 *
 * Le problème qu'elle résout, et il est arithmétique : les deux canaux sont
 * additifs, et une somme est dominée par son terme non borné. Le natif est
 * plafonné par la population, elle-même plafonnée par la capacité des paliers
 * ouverts. Si l'acclimaté suivait la densité — qui ne redescend jamais —, la
 * captation cesserait de dépendre de la population vers la mi-partie, et les
 * trois conséquences du §3 cesseraient d'être vraies.
 *
 * La borne est canonique et elle était déjà écrite. `mana-typologie.md` §1 : un
 * milieu dense en vivant est constamment réensemencé en signature vive, donc il
 * accumule sans mûrir. Une mare peut être immensément ancienne et rester
 * bloquée au degré 2 — riche et jeune en permanence.
 *
 * **Peupler un palier le noie de signature vive, donc dilue son type mûr, donc
 * fait baisser son rendement acclimaté.**
 *
 * ── Ce que le modèle retient, et ce qu'il écarte ────────────────────────────
 *
 * La part mûre est portée DIRECTEMENT comme état, et non calculée comme le
 * quotient de deux charges suivies séparément. Les deux disent la même chose —
 * le §3.0 définit bien un quotient — mais un quotient de deux exponentielles ne
 * s'intègre pas sous la forme fermée dont dépendent le hors ligne et le
 * simulateur, là où la part elle-même suit exactement la même loi que
 * l'effectif d'un banc, dont la primitive est déjà écrite (`population.ts`).
 *
 * Sa cible est fonction de la PLACE installée, pas de l'effectif vivant à
 * l'instant. Deux raisons, et la seconde est de conception :
 *
 *   1. la place ne change qu'entre deux ticks, donc la cible est constante sur
 *      l'intervalle et l'intégrale reste fermée ;
 *   2. le §3.0 veut « un arbitrage par palier, posé rarement — une décision
 *      d'allocation qui s'inscrit dans la durée, pas une action à répéter ».
 *      Indexer la dilution sur la place fait de ce qu'on a DÉCIDÉ de peupler la
 *      variable, au lieu d'un effectif qui dérive tout seul.
 *
 * [P29] — la vitesse de maturation reste à calibrer. Le GDD note qu'elle décide
 * si l'arbitrage se joue à l'échelle d'une session ou d'un cycle. La granularité
 * retenue est le PALIER, comme la formule du §3.0 l'écrit.
 */
import {
  PLACE_QUI_DILUE_A_MOITIE,
  TAU_MATURATION_HEURES,
} from './constantes'

export interface AvanceeDeMaturation {
  /** Part mûre à la fin de l'intervalle. */
  readonly part: number
  /** ∫ part dt sur l'intervalle : le facteur temps du canal acclimaté. */
  readonly integrale: number
}

/**
 * Part mûre d'équilibre pour une place donnée.
 *
 * Vaut 1 sur un palier que rien n'habite — une eau scellée, pauvre en vivant,
 * mûrit jusqu'à l'élémentaire (§6.5, la mer relique) — et tend vers 0 à mesure
 * qu'on la peuple. À `PLACE_QUI_DILUE_A_MOITIE`, l'eau est moitié vive.
 */
export function cibleDeMaturation(place: number): number {
  return 1 / (1 + Math.max(0, place) / PLACE_QUI_DILUE_A_MOITIE)
}

/**
 * Avance exacte de la part mûre sur `dt` secondes.
 *
 *   p(t)  = C + (p₀ − C)·e^(−t/τ)
 *   ∫p dt = C·Δ + (p₀ − C)·τ·(1 − e^(−Δ/τ))
 *
 * Même forme que l'effectif d'un banc, et pour la même raison : un pas de 8 h
 * et 480 pas de 60 s doivent rendre le même mana. `expm1` plutôt que
 * `1 - exp` — sur les pas de 100 ms l'exposant vaut ~5e-6, et la soustraction
 * naïve perd les chiffres qui font tenir l'équivalence de pas.
 */
export function avancerMaturation(part: number, cible: number, dt: number): AvanceeDeMaturation {
  const tau = TAU_MATURATION_HEURES * 3600
  if (!(dt > 0)) return { part, integrale: 0 }
  if (!(tau > 0)) return { part: cible, integrale: cible * dt }

  const ecart = part - cible
  const perte = -Math.expm1(-dt / tau)
  return {
    part: cible + ecart * (1 - perte),
    integrale: cible * dt + ecart * tau * perte,
  }
}

/**
 * L'état de maturation d'une partie neuve : tout est mûr.
 *
 * Ce n'est pas une commodité, c'est le §6.5. Une eau que rien n'habite et que
 * rien ne réensemence vieillit sans interruption — c'est ce qui permet à la mer
 * relique de porter un élémentaire là où la mare n'y arrivera jamais. Le héros
 * descend donc dans de l'eau mûre et la rend jeune en la peuplant, ce qui est
 * exactement la leçon que la géographie du jeu est censée enseigner.
 */
export const PART_MURE_D_UNE_EAU_INTOUCHEE = 1
