/**
 * IdlePond — population d'un banc : effectif, repeuplement, legs.
 *
 * Le filtre du §5.2 est ce qui décide de tout ce module : « toute mécanique du
 * cœur doit se calculer en un seul pas pour dt = 8 heures ». On ne suit donc
 * aucun individu, on n'itère sur aucune file, on ne vérifie rien à chaque tick.
 *
 * L'effectif converge vers sa cible par une exponentielle, dont la primitive
 * est fermée. C'est ce qui rend le hors ligne et le simulateur exacts d'un seul
 * coup, et non approchés : avancer de 8 h en un pas ou en 480 pas de 60 s donne
 * le même effectif ET la même intégrale de production.
 *
 * Aucune action du joueur ne peut faire baisser un effectif : sa seule prise
 * sur la mortalité est de la faire baisser (§4.2).
 */

export interface AvanceeDeBanc {
  /** Effectif à la fin de l'intervalle. */
  readonly effectif: number
  /** ∫ effectif dt sur l'intervalle : le facteur temps de la production. */
  readonly integraleEffectif: number
}

/** L'effectif cible d'un banc est fixé par son niveau. Niveau 0 = pas encore convaincu. */
export function effectifCible(niveau: number): number {
  return niveau
}

/**
 * Avance exacte de l'effectif sur `dt` secondes.
 *
 *   e(t)  = C + (e0 − C)·e^(−k·t)
 *   ∫e dt = C·Δ + (e0 − C)·(1 − e^(−k·Δ))/k
 *
 * Les deux formes composent exactement : avancer de Δ puis de Δ' revient à
 * avancer de Δ+Δ'. C'est la propriété que le test d'équivalence de pas vérifie.
 */
export function avancerBanc(effectif: number, cible: number, k: number, dt: number): AvanceeDeBanc {
  if (dt <= 0) return { effectif, integraleEffectif: 0 }
  if (k <= 0) return { effectif, integraleEffectif: effectif * dt }
  const ecart = effectif - cible
  // expm1 plutôt que 1 - exp : sur les pas de 100 ms, k·dt vaut ~3e-4 et la
  // soustraction naïve perd les chiffres qui font tenir l'équivalence de pas.
  const perte = -Math.expm1(-k * dt)
  return {
    effectif: cible + ecart * (1 - perte),
    integraleEffectif: cible * dt + (ecart * perte) / k,
  }
}
