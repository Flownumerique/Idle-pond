/**
 * IdlePond — arbre de technique.
 *
 * §4.3 : la technique baisse les COÛTS et automatise. Elle ne monte aucune
 * production. Le typage des effets (`TermeDeCout | TermeDeConfort` pour les
 * nœuds chiffre, `CapaciteId` pour les nœuds verbe) rend la faute
 * inexprimable ; le test de canon la vérifie tout de même sur le registre, car
 * c'est le test le plus fréquemment échoué du projet.
 *
 * Les compteurs survivent à l'éclosion et ne se redistribuent jamais : on ne
 * désapprend pas. Les points sont par branche, donc non fongibles — aucune
 * monnaie commune n'est introduite, un pool fongible rouvrirait la porte du
 * Corail de Prestige par le côté (§7.4).
 */
import type {
  BrancheTechniqueId,
  CapaciteId,
  EtatJeu,
  RegimeCompteur,
  TermeDeCout,
  TermeDeConfort,
} from './types'
import { NOEUDS_TECHNIQUE } from '../donnees/noeuds-technique'

/** §7.1 — régime de compteur par branche. Trois bornées, trois non bornées. */
export const REGIME_PAR_BRANCHE: Readonly<Record<BrancheTechniqueId, RegimeCompteur>> = {
  creusement: 'non_borne',
  amelioration: 'non_borne',
  entretien: 'non_borne',
  recrutement: 'borne',
  construction: 'borne',
  eclosion: 'borne',
}

/**
 * [P] — les couples (A, B) des trois branches non bornées sont « à mesurer »
 * (§13.3) et le §13.3 ne leur donne AUCUNE graine. Ils ne sont donc pas
 * inventés ici : la table est vide, et elle sera résolue à l'envers en v0.4
 * depuis la table `verbe → cycle d'ouverture visé` (§7.2). Tant qu'un couple
 * manque, la branche rend zéro point — un arbre muet, jamais un arbre deviné.
 */
export const COUPLES_A_B: Readonly<Partial<Record<BrancheTechniqueId, { readonly a: number; readonly b: number }>>> = {}

/**
 * [P] — tables de seuils des trois branches bornées : figées en v0.4, quand le
 * calendrier des verbes est arrêté. Sur un compteur qui plafonne, « encore 2
 * éclosions » est lisible et exact là où le logarithme n'ajoute qu'une barre
 * opaque.
 */
export const SEUILS_PAR_BRANCHE_BORNEE: Readonly<Partial<Record<BrancheTechniqueId, readonly number[]>>> = {}

/** Points rendus par une branche, d'après son compteur d'usage. */
export function pointsDeBranche(branche: BrancheTechniqueId, compteur: number): number {
  if (REGIME_PAR_BRANCHE[branche] === 'borne') {
    const seuils = SEUILS_PAR_BRANCHE_BORNEE[branche]
    if (seuils === undefined) return 0
    return seuils.filter((s) => compteur >= s).length
  }
  const couple = COUPLES_A_B[branche]
  if (couple === undefined) return 0
  return Math.floor(couple.a * Math.log(1 + compteur / couple.b))
}

export function pointsDisponibles(etat: EtatJeu, branche: BrancheTechniqueId): number {
  const rendus = pointsDeBranche(branche, etat.permanent.compteursTechnique[branche] ?? 0)
  const depenses = NOEUDS_TECHNIQUE.filter(
    (n) => n.branche === branche && etat.permanent.noeudsTechnique.includes(n.id),
  ).reduce((somme, n) => somme + n.cout, 0)
  return rendus - depenses
}

/**
 * Facteur appliqué à un terme de coût ou de confort par les nœuds acquis.
 * Neutre = 1. Un nœud « −20 % » porte un facteur 0.8, jamais un « +12 % »
 * flottant sans terme nommé (§7.5 règle 3).
 */
export function facteurDeTechnique(etat: EtatJeu, terme: TermeDeCout | TermeDeConfort): number {
  let facteur = 1
  for (const noeud of NOEUDS_TECHNIQUE) {
    if (noeud.effet.nature !== 'chiffre') continue
    if (noeud.effet.terme !== terme) continue
    if (!etat.permanent.noeudsTechnique.includes(noeud.id)) continue
    facteur *= noeud.effet.facteur
  }
  return facteur
}

/** Capacités ouvertes par l'arbre. Les succès en ouvrent d'autres, jamais les mêmes. */
export function capacitesDeLArbre(etat: EtatJeu): ReadonlySet<CapaciteId> {
  const ouvertes = new Set<CapaciteId>()
  for (const noeud of NOEUDS_TECHNIQUE) {
    if (noeud.effet.nature !== 'verbe') continue
    if (!etat.permanent.noeudsTechnique.includes(noeud.id)) continue
    ouvertes.add(noeud.effet.capacite)
  }
  return ouvertes
}

/** Incrémente un compteur d'usage. Le compteur ne se dépense pas : il produit des points. */
export function creditCompteur(
  compteurs: Readonly<Record<BrancheTechniqueId, number>>,
  branche: BrancheTechniqueId,
  montant: number,
): Record<BrancheTechniqueId, number> {
  return { ...compteurs, [branche]: (compteurs[branche] ?? 0) + montant }
}
