/**
 * IdlePond — tous les paramètres, un seul endroit (§5.3).
 *
 * Trois sections, et elles ne se mélangent pas :
 *   §13.1 FIXÉS    — décision de canon. Ne pas toucher sans en ouvrir une.
 *   §13.2 DÉRIVÉS  — recalculés ici, jamais saisis à la main.
 *   §13.3 GRAINES  — marqués « à mesurer ». Une graine n'est pas une valeur :
 *                    elle est là pour être réfutée par le simulateur ou la
 *                    télémétrie, et tout code qui la traite comme fixée est
 *                    en faute.
 *
 * Règle du §2.3 : aucune décision de canon n'est prise dans le code. Une valeur
 * manquante devient une constante nommée et commentée, jamais un nombre magique.
 */

/* ═══ §13.1 — FIXÉS ═════════════════════════════════════════════════════════ */

/** `g` — coût de palier. Chaque palier coûte ×2.4 le précédent. */
export const G_COUT_PALIER = 2.4

/** `f` — tarif de redescente. 1 = reset complet, aucune fraction conservée. */
export const F_TARIF_REDESCENTE = 1

/** Coût de niveau, achat répétable. */
export const RATIO_COUT_NIVEAU = 1.15

/**
 * Seuils de jalon : 10 / 25 / 50 / 100 → ×2 / ×4 / ×8 / ×16.
 *
 * [P] — le §6.2 apparie des seuils à des ratios sans dire s'ils se cumulent.
 * Deux lectures : le multiplicateur TOTAL atteint le ×16 au niveau 100, ou
 * chaque seuil applique son ratio et le produit atteint ×1024. La table
 * ci-dessous porte la première lecture, la plus littérale ; le mode est nommé
 * pour qu'un basculement soit un changement d'une ligne et non d'un modèle.
 * À trancher avec l'auteur avant le calibrage v0.3.
 */
export const SEUILS_DE_JALON: readonly { readonly seuil: number; readonly multiplicateur: number }[] = [
  { seuil: 10, multiplicateur: 2 },
  { seuil: 25, multiplicateur: 4 },
  { seuil: 50, multiplicateur: 8 },
  { seuil: 100, multiplicateur: 16 },
]

/** 'total' : la colonne donne le multiplicateur atteint. 'cumule' : il se multiplie. */
export const LECTURE_DES_SEUILS_DE_JALON: 'total' | 'cumule' = 'total'

/** 62 paliers, distribution plate. */
export const NOMBRE_DE_PALIERS = 62

/** Six assises. Chacune son type de mana, ses assets complets. */
export const NOMBRE_D_ASSISES = 6

/** ~4,4 paliers par cycle. */
export const PALIERS_PAR_CYCLE_VISE = 4.4

/** Nombre d'éclosions visé sur la partie. */
export const NOMBRE_D_ECLOSIONS_VISE = 15

/** ~21 espèces de base + ~6 divergences. */
export const NOMBRE_D_ESPECES_DE_BASE = 21
export const NOMBRE_DE_DIVERGENCES = 6

/** Plafond hors ligne : 6 h au départ, 24 h par la branche Entretien. */
export const CAP_HORS_LIGNE_HEURES_INITIAL = 6
export const CAP_HORS_LIGNE_HEURES_MAXIMUM = 24

/** Coûts par nœud, identiques dans toutes les branches. */
export const COUTS_DE_NOEUD: readonly number[] = [5, 12, 25, 45, 80]

/** §13.4 — cibles de courbe fixées. */
export const DUREE_DU_CYCLE_1_HEURES = 3
export const CROISSANCE_PAR_CYCLE_VISEE = 1.18

/* ═══ §13.2 — DÉRIVÉS ═══════════════════════════════════════════════════════ */

/**
 * `g/D` = croissance_par_cycle ^ (1 / paliers_par_cycle).
 * Un écart de 4 %, et il produit toute la forme de la courbe (§6.3).
 */
export const RAPPORT_G_SUR_D = Math.pow(CROISSANCE_PAR_CYCLE_VISEE, 1 / PALIERS_PAR_CYCLE_VISE)

/**
 * `D` — production totale d'un palier, rapportée au palier précédent.
 * Se mesure sur le palier entier, jamais par espèce : sinon le nombre
 * d'espèces par palier fait dériver le ratio sans que personne ne le voie.
 */
export const D_PRODUCTION_PAR_PALIER = G_COUT_PALIER / RAPPORT_G_SUR_D

/* ═══ §13.3 — GRAINES, À MESURER. NE JAMAIS TRAITER COMME FIXÉES ════════════ */

/** [P] graine — `α`, gain de densité. Mesuré en v0.3. */
export const ALPHA_GAIN_DE_DENSITE = 0.6

/** [P] graine — exposant du multiplicateur de densité. Mesuré en v0.3. */
export const EXPOSANT_DU_MULTIPLICATEUR_DE_DENSITE = 0.5

/**
 * [P] — le §13.2 dérive l'exposant de densité de `θ / α`, mais `θ` n'est défini
 * nulle part dans le prompt de lancement ni dans les documents disponibles. La
 * dérivation est donc impossible à ce jalon : le noyau applique la graine du
 * §13.3 telle quelle et la question est portée à l'auteur avant v0.3.
 */
export const EXPOSANT_DE_DENSITE = EXPOSANT_DU_MULTIPLICATEUR_DE_DENSITE

/** [P] graine — `k`, taux de repeuplement, par seconde. Mesuré en v0.3. */
export const K_TAUX_DE_REPEUPLEMENT = 1 / 300

/** [P] graine — bonus global au niveau 100. Mesuré en v0.3. */
export const BONUS_GLOBAL_AU_NIVEAU_100 = 0.03

/* ─── Graines d'échelle économique ──────────────────────────────────────────
 * Le prompt de lancement fixe les RATIOS (g, D, ×1.15) mais aucune échelle
 * absolue. Ces graines fixent l'origine des trois courbes géométriques ; elles
 * ne changent aucune forme et seront réfutées par le calibreur.
 */

/** [P] graine — taux de base d'un palier au palier 0, mana/s par individu. */
export const TAUX_BASE_AU_PALIER_0 = 1

/** [P] graine — coût de creusement du palier 1. Croît ensuite en g^index. */
export const COUT_CREUSER_AU_PALIER_1 = 60

/** [P] graine — coût de conviction d'un banc du palier 0. Croît en g^index. */
export const COUT_DEBLOCAGE_AU_PALIER_0 = 10

/** [P] graine — coût du premier niveau d'un banc du palier 0. Croît en g^index. */
export const COUT_NIVEAU_AU_PALIER_0 = 8

/** Nombre de paliers ouverts au début d'un cycle. Le héros démarre dans la mare. */
export const PALIERS_OUVERTS_AU_DEPART = 1

/**
 * [P] graine — mana porté à la sortie de l'œuf.
 *
 * Il en faut : le mana courant est perdu à l'éclosion (§6.5) et la production
 * naît du premier banc convaincu. Sans cette charge de départ, la seule façon
 * d'amorcer une vie serait un clic — or aucune présence active n'est requise
 * et il n'y a pas de clic obligatoire (§4.2). Calé sur le coût de la première
 * conviction, ni plus ni moins.
 */
export const MANA_A_LA_SORTIE_DE_L_OEUF = COUT_DEBLOCAGE_AU_PALIER_0

/* ─── Graines de contenance ─────────────────────────────────────────────────
 * §6.4 : « la contenance limite le stock, pas la production ». Elle est donc un
 * état permanent et non une quantité dérivée de la production courante — sinon
 * elle grandirait avec chaque niveau acheté et le blocage doux n'arriverait
 * jamais. Elle est conservée à l'éclosion (§6.5) et progresse d'un cycle à
 * l'autre, indexée sur la production de pic du cycle, comme la densité.
 *
 * [P] — la loi de croissance de la contenance n'est fixée par aucun document.
 * Ces graines la rendent monotone et non explosive ; à trancher en v0.3.
 */
export const CONTENANCE_INITIALE = 200
export const CONTENANCE_EN_SECONDES_DE_PRODUCTION_DE_PIC = 600

/* ─── Graines d'éclosion ────────────────────────────────────────────────────*/

/** [P] graine — référence de production servant à indexer densité et Foi. */
export const PRODUCTION_DE_REFERENCE = 1

/** [P] graine — barème de Foi. Foi = base × (pic / référence) ^ exposant. */
export const FOI_BASE = 1
export const FOI_EXPOSANT = 0.5

/* ─── Acclimatation ─────────────────────────────────────────────────────────*/

/**
 * [P] — Tier 0 : le héros ne repaie jamais son acclimatation, et l'éclosion ne
 * la remet pas à zéro. Le noyau porte donc l'acclimatation en état permanent.
 * Son MÉCANISME D'ACQUISITION est du contenu v0.5 et n'est pas inventé ici :
 * d'ici là le rendement est plein sur tous les types, ce qui permet au
 * simulateur de traverser les six assises sans qu'aucune règle soit devinée.
 */
export const RENDEMENT_ACCLIMATATION_PLEIN_JUSQU_EN_V05 = 1

/* ─── Budget de verbes (§7.5 règle 2) ───────────────────────────────────────*/

/** L'arbre en porte 10 ; il en reste ~5 pour les succès. Plafond partagé. */
export const BUDGET_DE_VERBES_TOTAL = 15
export const BUDGET_DE_VERBES_ARBRE = 10

/* ─── Boucle ────────────────────────────────────────────────────────────────*/

/** Le jeu appelle tick à 100 ms. Le simulateur l'appelle avec 60 s ou 8 h. */
export const PERIODE_DE_TICK_MS = 100

/** Version de save courante. Toute évolution passe par une migration. */
export const VERSION_SAVE = 1
