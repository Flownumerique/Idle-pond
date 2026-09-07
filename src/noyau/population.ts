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
  /**
   * ∫ multiplicateur(effectif) × effectif dt.
   *
   * C'est ce terme, et pas le précédent, qui donne le mana produit : depuis
   * l'amendement v1.1 §2.C le multiplicateur de seuil se lit sur l'EFFECTIF,
   * donc il change à l'intérieur de l'intervalle.
   */
  readonly integralePonderee: number
  /** Multiplicateur de seuil à la fin de l'intervalle. */
  readonly multiplicateurFinal: number
}

export interface SeuilPondere {
  readonly seuil: number
  readonly multiplicateurCumule: number
}

function multiplicateurA(effectif: number, seuils: readonly SeuilPondere[]): number {
  let multiplicateur = 1
  for (const seuil of seuils) {
    if (effectif >= seuil.seuil) multiplicateur = seuil.multiplicateurCumule
  }
  return multiplicateur
}

/**
 * L'effectif cible d'un banc est la PLACE qu'on lui a faite. Place 0 = pas
 * encore convaincu.
 *
 * Le joueur n'achète jamais un individu : il ouvre de la place, et la
 * population y vient d'elle-même. C'est ce qui met du temps entre l'achat et
 * son effet, et c'est ce temps qui fait le jeu.
 */
export function effectifCible(place: number): number {
  return place
}

/**
 * Avance exacte de l'effectif sur `dt` secondes, seuils compris.
 *
 *   e(t)  = C + (e0 − C)·e^(−k·t)
 *   ∫e dt = C·Δ + (e0 − C)·(1 − e^(−k·Δ))/k
 *
 * Depuis l'amendement v1.1 §2.C, le multiplicateur de seuil se lit sur
 * l'effectif : il change donc EN COURS D'INTERVALLE, et ∫m(e)·e dt n'a plus la
 * forme fermée ci-dessus. Un multiplicateur figé au début du pas ferait
 * diverger 480 pas de 60 s d'un pas de 8 h — les petits pas franchiraient le
 * seuil tôt et produiraient davantage.
 *
 * La sortie : `e` est monotone, donc chaque seuil est franchi AU PLUS UNE FOIS,
 * à un instant qu'on sait résoudre à la main —
 *
 *   t_s = −ln((s − C) / (e0 − C)) / k
 *
 * — et l'intervalle se découpe en au plus cinq morceaux à multiplicateur
 * constant, chacun d'intégrale fermée. Ce n'est pas une itération sur une file
 * d'événements que le §1 interdirait : c'est une partition analytique bornée,
 * dont le coût ne dépend ni de `dt` ni de l'histoire de la partie.
 */
export function avancerBanc(
  effectif: number,
  cible: number,
  k: number,
  dt: number,
  seuils: readonly SeuilPondere[] = [],
): AvanceeDeBanc {
  if (dt <= 0) {
    return {
      effectif,
      integraleEffectif: 0,
      integralePonderee: 0,
      multiplicateurFinal: multiplicateurA(effectif, seuils),
    }
  }

  const ecart = effectif - cible
  if (k <= 0 || ecart === 0) {
    const multiplicateur = multiplicateurA(effectif, seuils)
    return {
      effectif,
      integraleEffectif: effectif * dt,
      integralePonderee: multiplicateur * effectif * dt,
      multiplicateurFinal: multiplicateur,
    }
  }

  // expm1 plutôt que 1 - exp : sur les pas de 100 ms, k·dt vaut ~3e-4 et la
  // soustraction naïve perd les chiffres qui font tenir l'équivalence de pas.
  const perte = -Math.expm1(-k * dt)
  const effectifFinal = cible + ecart * (1 - perte)
  const integraleEffectif = cible * dt + (ecart * perte) / k

  const instants = instantsDeFranchissement(effectif, cible, k, dt, seuils)
  if (instants.length === 0) {
    const multiplicateur = multiplicateurA(effectif, seuils)
    return {
      effectif: effectifFinal,
      integraleEffectif,
      integralePonderee: multiplicateur * integraleEffectif,
      multiplicateurFinal: multiplicateur,
    }
  }

  let integralePonderee = 0
  let debut = 0
  for (const fin of [...instants, dt]) {
    if (fin <= debut) continue
    // Le multiplicateur est lu au MILIEU du morceau : au bord exact, l'erreur
    // d'arrondi peut retomber du mauvais côté du seuil.
    const multiplicateur = multiplicateurA(effectifA((debut + fin) / 2, effectif, cible, k), seuils)
    integralePonderee += multiplicateur * integraleSur(debut, fin, effectif, cible, k)
    debut = fin
  }

  return {
    effectif: effectifFinal,
    integraleEffectif,
    integralePonderee,
    multiplicateurFinal: multiplicateurA(effectifFinal, seuils),
  }
}

function effectifA(t: number, effectif: number, cible: number, k: number): number {
  return cible + (effectif - cible) * Math.exp(-k * t)
}

/** ∫ e dt sur [a, b], fermée. */
function integraleSur(a: number, b: number, effectif: number, cible: number, k: number): number {
  const ecart = effectif - cible
  return cible * (b - a) + (ecart * (Math.exp(-k * a) - Math.exp(-k * b))) / k
}

/** Instants, croissants, où l'effectif franchit un seuil. Au plus un par seuil. */
function instantsDeFranchissement(
  effectif: number,
  cible: number,
  k: number,
  dt: number,
  seuils: readonly SeuilPondere[],
): readonly number[] {
  const instants: number[] = []
  for (const { seuil } of seuils) {
    const rapport = (seuil - cible) / (effectif - cible)
    if (!(rapport > 0) || rapport >= 1) continue
    const instant = -Math.log(rapport) / k
    if (instant > 0 && instant < dt) instants.push(instant)
  }
  return instants.sort((a, b) => a - b)
}
