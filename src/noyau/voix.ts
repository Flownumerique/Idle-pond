/**
 * IdlePond — les paliers de voix (GDD §13.1).
 *
 * « Axe de progression le plus important du jeu : il porte le tutoriel,
 * l'interface et le récit en même temps. »
 *
 * | Palier      | Ce que l'UI montre                          | Déclencheur     |
 * |-------------|---------------------------------------------|-----------------|
 * | La pente    | Aucun chiffre. Une jauge sans graduation.   | Début           |
 * | Les signes  | Le lexique. Barres graduées, ordres de gr.  | 1ᵉʳ franchis.   |
 * | Les direct. | Chiffres complets, débits, taux.            | 3ᵉ franchis.    |
 * | Le dialogue | Tout.                                       | Le relais       |
 *
 * Ce module ne fait qu'une chose : dire à quel palier le héros en est. La
 * numérisation de l'UI qui en découle appartient à l'UI, et le registre figé
 * des succès (§14.5) le lit au moment où une entrée tombe.
 *
 * Pourquoi une dérivation et non un champ d'état : le palier est entièrement
 * fonction du nombre de franchissements survécus, et le contrat du §5.1
 * n'admet aucun état hors du reducer. Le jour où le relais existera (§12.2),
 * c'est l'ÉVÉNEMENT qui entrera dans l'état permanent — pas le palier, qui
 * continuera de s'en déduire ici.
 */
import type { EtatJeu, PalierDeVoix } from './types'
import {
  FRANCHISSEMENTS_POUR_LES_DIRECTIVES,
  FRANCHISSEMENTS_POUR_LES_SIGNES,
} from './constantes'

/** Ordre du plus pauvre au plus riche. Une voix ne redescend jamais. */
export const PALIERS_DE_VOIX: readonly PalierDeVoix[] = ['pente', 'signes', 'directives', 'dialogue']

/**
 * Le palier courant.
 *
 * `dialogue` n'est pas atteignable : il est déclenché par le relais (§12.2),
 * qui est du contenu de la v1.0. Il figure dans le type parce que le registre
 * d'un succès est figé POUR TOUJOURS au moment où il tombe — un type qui
 * gagnerait une valeur plus tard rendrait les saves d'aujourd'hui ambiguës.
 */
export function palierDeVoix(etat: EtatJeu): PalierDeVoix {
  return palierDeVoixApres(etat.permanent.nombreEclosions)
}

/**
 * Le même, depuis le seul nombre de franchissements.
 *
 * Existe pour la migration de save, qui n'a pas d'`EtatJeu` sous la main — elle
 * travaille sur du contenu sérialisé, et fabriquer un état complet pour lire
 * une seule dérivation reviendrait à dupliquer la règle.
 */
export function palierDeVoixApres(franchissements: number): PalierDeVoix {
  if (franchissements >= FRANCHISSEMENTS_POUR_LES_DIRECTIVES) return 'directives'
  if (franchissements >= FRANCHISSEMENTS_POUR_LES_SIGNES) return 'signes'
  return 'pente'
}

/** Vrai si le palier `atteint` est au moins `requis`. Pour la numérisation de l'UI. */
export function voixAuMoins(atteint: PalierDeVoix, requis: PalierDeVoix): boolean {
  return PALIERS_DE_VOIX.indexOf(atteint) >= PALIERS_DE_VOIX.indexOf(requis)
}
