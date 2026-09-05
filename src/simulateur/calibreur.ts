/**
 * IdlePond — calibreur.
 *
 * §7.2, et c'est le point le plus contre-intuitif du projet : le calibrage se
 * fait à l'envers.
 *
 *   « Le calendrier des verbes est le seul réglage de durée réelle du jeu. »
 *
 * Raison : le simulateur établit ~38 h de jeu ACTIF pour ~600 h CALENDAIRES.
 * L'écart entier est du temps d'attente entre check-ins. Or un nœud CHIFFRE
 * réduit un coût dans une économie géométrique : une réduction de facteur `c`
 * vaut `log_g(1/c)` paliers d'avance, soit — pour toute la branche Creusement —
 * moins d'un palier. C'est un décalage additif constant, qui ne compose pas. Un
 * nœud VERBE, lui, supprime des intervalles de check-in, c'est-à-dire les 560
 * heures qui ne sont pas du jeu actif.
 *
 * Conséquence, et elle est structurante pour ce fichier : la donnée d'ENTRÉE
 * est une table `verbe → cycle d'ouverture visé`. Le calibreur résout (A, B) à
 * l'envers. On ne règle JAMAIS « combien de points par cycle » à la main.
 */
import type { BrancheTechniqueId } from '../noyau/types'
import { COUTS_DE_NOEUD } from '../noyau/constantes'
import { REGIME_PAR_BRANCHE } from '../noyau/technique'

/** Ce que l'auteur fournit : à quel cycle chaque rang de la branche s'ouvre. */
export interface CibleDOuverture {
  readonly branche: BrancheTechniqueId
  /** Cycle visé pour chaque rang, index 0 = premier nœud. */
  readonly cyclesVises: readonly number[]
}

/** Ce que le simulateur observe : le compteur d'usage à la fin de chaque cycle. */
export type TrajectoireDeCompteur = readonly number[]

export interface CoupleAB {
  readonly a: number
  readonly b: number
  /** Somme des carrés des écarts, en cycles, entre ouverture visée et obtenue. */
  readonly erreur: number
}

function coutCumuleJusquAuRang(rang: number): number {
  return COUTS_DE_NOEUD.slice(0, rang + 1).reduce((somme, cout) => somme + cout, 0)
}

/** Cycle auquel (A, B) ouvre chaque rang, d'après la trajectoire mesurée. */
export function cyclesDOuverture(
  a: number,
  b: number,
  trajectoire: TrajectoireDeCompteur,
  nombreDeRangs: number,
): readonly (number | null)[] {
  const points = trajectoire.map((compteur) => Math.floor(a * Math.log(1 + compteur / b)))
  return Array.from({ length: nombreDeRangs }, (_, rang) => {
    const requis = coutCumuleJusquAuRang(rang)
    const cycle = points.findIndex((p) => p >= requis)
    return cycle === -1 ? null : cycle
  })
}

/**
 * Résout (A, B) à l'envers depuis les cycles d'ouverture visés.
 *
 * Balayage logarithmique sur B puis résolution directe de A : pour un B donné,
 * A est fixé par la contrainte du premier rang, et le reste de la branche
 * s'ensuit. C'est une recherche, pas une formule fermée — mais l'entrée reste
 * le calendrier des verbes, jamais un débit de points choisi à la main.
 *
 * Ne s'applique qu'aux branches à compteur NON BORNÉ : sur une branche bornée,
 * le §7.1 impose une table de seuils directe, et « encore 2 éclosions » est
 * lisible et exact là où le logarithme n'ajoute qu'une barre opaque.
 */
export function resoudreCoupleAB(
  cible: CibleDOuverture,
  trajectoire: TrajectoireDeCompteur,
): CoupleAB | null {
  if (REGIME_PAR_BRANCHE[cible.branche] === 'borne') return null
  if (trajectoire.length === 0 || cible.cyclesVises.length === 0) return null

  let meilleur: CoupleAB | null = null
  for (let exposant = -6; exposant <= 12; exposant += 0.05) {
    const b = Math.exp(exposant)
    for (let a = 0.5; a <= 400; a *= 1.02) {
      const obtenus = cyclesDOuverture(a, b, trajectoire, cible.cyclesVises.length)
      let erreur = 0
      for (let rang = 0; rang < cible.cyclesVises.length; rang += 1) {
        const obtenu = obtenus[rang]
        // Un rang qui ne s'ouvre jamais est pénalisé au-delà de l'horizon
        // simulé : un arbre jamais fini est aussi faux qu'un arbre fini au
        // cycle 8 (§7.6).
        const effectif = obtenu ?? trajectoire.length + cible.cyclesVises.length
        erreur += (effectif - cible.cyclesVises[rang]) ** 2
      }
      if (meilleur === null || erreur < meilleur.erreur) meilleur = { a, b, erreur }
      if (erreur === 0) return { a, b, erreur }
    }
  }
  return meilleur
}
