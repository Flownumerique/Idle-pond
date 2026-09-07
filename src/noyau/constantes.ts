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
 * Seuils, et leur MULTIPLICATEUR CUMULÉ — amendement v1.1 §2.C.
 *
 * La colonne est le multiplicateur cumulé LU AU SEUIL : chaque seuil double, et
 * le total à 100 individus vaut ×16, jamais ×1024. Toute lecture « ×4
 * supplémentaire » est fausse, et `D = 2.31` a été calibré contre celle-ci.
 *
 * Les seuils portent sur l'EFFECTIF, pas sur la place achetée : le joueur
 * achète de la place, la population croît seule vers son plafond, et les
 * seuils tombent avec le temps. Le multiplicateur se recalcule depuis
 * l'effectif courant à chaque tick, et se reperd à l'éclosion comme la
 * population.
 */
export const SEUILS_DE_JALON: readonly { readonly seuil: number; readonly multiplicateurCumule: number }[] = [
  { seuil: 10, multiplicateurCumule: 2 },
  { seuil: 25, multiplicateurCumule: 4 },
  { seuil: 50, multiplicateurCumule: 8 },
  { seuil: 100, multiplicateurCumule: 16 },
]

/**
 * Seuil au-delà duquel une espèce pose un drapeau PERMANENT (§2.C).
 *
 * L'unique exception à « le multiplicateur de seuil se reperd à l'éclosion » :
 * avoir déjà atteint cent individus d'une espèce accorde un bonus définitif,
 * conservé à l'éclosion. C'est un drapeau par espèce, distinct du
 * multiplicateur de seuil, et il ne se repose jamais.
 */
export const SEUIL_DU_DRAPEAU_PERMANENT = 100

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

/**
 * Contenance par éclosion : `g ^ paliers_par_cycle` = ×47,1.
 *
 * Dérivé, et atteint VIA l'acquis de séjour — jamais écrit dans le code de
 * l'éclosion. C'est la cible que le test de contenance vérifie à 2 % près.
 */
export const CONTENANCE_PAR_ECLOSION = Math.pow(G_COUT_PALIER, PALIERS_PAR_CYCLE_VISE)

/* ═══ §13.3 — GRAINES, À MESURER. NE JAMAIS TRAITER COMME FIXÉES ════════════ */

/** [P] graine — `α`, gain de densité. Mesuré en v0.3, conjointement avec `θ`. */
export const ALPHA_GAIN_DE_DENSITE = 0.6

/**
 * `θ` — part du besoin que la densité compense. Borne dure [0, 1].
 * Amendement v1.1 §2.A. C'est le seul bouton qui agisse sur la FORME de la
 * courbe ; `D` agit sur son niveau.
 *
 * [P] graine — mesuré en v0.3, conjointement avec `α`.
 */
export const THETA_PART_COMPENSEE = 0.8

/**
 * Exposant du multiplicateur de densité — DÉRIVÉ, jamais saisi.
 *
 * La chaîne, à ne pas perdre de vue en le lisant :
 *   1. à chaque éclosion, la production de pointe est multipliée par
 *      `g^paliers_gagnés` ;
 *   2. le gain de densité vaut `pointe^α`, donc la densité est multipliée par
 *      `g^(paliers × α)` ;
 *   3. pour que le multiplicateur de densité suive EXACTEMENT le besoin
 *      (`g^paliers`), l'exposant doit valoir `1/α` ;
 *   4. `θ` est la fraction de ce besoin qu'on choisit de compenser.
 *
 * θ = 1 → compensation exacte, cycles plats.
 * θ = 0 → aucune compensation, les cycles s'allongent sans fin.
 */
export function densiteExposant(): number {
  return THETA_PART_COMPENSEE / ALPHA_GAIN_DE_DENSITE
}

/** [P] graine — `k`, taux de repeuplement, par seconde. Mesuré en v0.3. */
export const K_TAUX_DE_REPEUPLEMENT = 1 / 300

/**
 * [P] graine — couplage de la densité à la vitesse de repeuplement.
 *
 * Distinct de `θ/α`, et il doit le rester : la densité agit déjà pleinement sur
 * l'acquis de séjour (§2.B), et lui donner le même exposant ici compterait deux
 * fois la même compensation. Voir la note de `vitesseDeRepeuplement`.
 */
export const EXPOSANT_REPEUPLEMENT_DENSITE = 0.5

/**
 * [P] graine — bonus global accordé par espèce ayant déjà atteint cent
 * individus. Définitif, conservé à l'éclosion. Mesuré en v0.3.
 *
 * [P] — le §2.C dit « +3 % de production globale » par espèce sans dire si
 * plusieurs espèces s'additionnent ou se composent. L'addition est retenue :
 * elle ne compose pas, donc elle ne peut pas surprendre à vingt et une espèces.
 */
export const BONUS_GLOBAL_A_CENT_INDIVIDUS = 0.03

/* ─── Graines d'échelle économique ──────────────────────────────────────────
 * Le prompt de lancement fixe les RATIOS (g, D, ×1.15) mais aucune échelle
 * absolue. Ces graines fixent l'origine des trois courbes géométriques ; elles
 * ne changent aucune forme et seront réfutées par le calibreur.
 */

/**
 * [P] graine — taux de base d'un palier au palier 0, mana/s par individu.
 *
 * Recalé au jalon v0.2, et pas par goût : à 1 mana/s par individu, tout coûtait
 * moins que quelques secondes de production et le premier cycle se figeait au
 * bout de dix minutes, faute d'avoir quoi que ce soit à acheter. Le plancher de
 * cadence du §8.4 est ce qui l'a fait apparaître. À remesurer en v0.3.
 */
export const TAUX_BASE_AU_PALIER_0 = 0.2

/** [P] graine — coût de creusement du palier 1. Croît ensuite en g^index. */
export const COUT_CREUSER_AU_PALIER_1 = 60

/** [P] graine — coût de conviction d'un banc du palier 0. Croît en g^index. */
export const COUT_DEBLOCAGE_AU_PALIER_0 = 10

/**
 * [P] graine — coût de la première PLACE d'un banc du palier 0. Croît en g^index.
 *
 * Le joueur achète de la place — un plafond de population — jamais des
 * individus. « Niveau » est un reste d'Étang des Merveilles, banni des
 * identifiants (§2.C, §3).
 */
export const COUT_DE_PLACE_AU_PALIER_0 = 8

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

/* ─── Contenance et acquis de séjour — amendement v1.1 §2.B ─────────────────
 *
 * La loi de croissance n'était pas fixée parce que son RÉSULTAT l'était :
 * 62 paliers, ~4,4 par cycle. Elle est donc dérivée, jamais saisie.
 *
 * Le blocage doux tombe quand `coût_base × g^P > contenance`. Pour que `P`
 * avance de 4,4 par cycle, la contenance doit être multipliée par `g^4.4`,
 * soit ×47,1 par éclosion.
 *
 * ÉCRIRE CE FACTEUR DIRECTEMENT EST INTERDIT. Tier 0 §8 : le plafond ne monte
 * QUE par séjour prolongé en mana dense. Une contenance indexée sur le
 * compteur d'éclosions violerait l'invariant. Elle monte donc par une
 * accumulation saturante de l'acquis de séjour, dont le temps caractéristique
 * décroît quand la densité monte — « séjour en mana DENSE ».
 *
 * Effet secondaire recherché, à ne pas casser : passé la saturation, rester ne
 * rapporte plus de profondeur, seulement de la Foi. C'est ce qui rend réelle
 * la seule vraie décision du joueur.
 */
export const CONTENANCE_INITIALE = 1200

/**
 * `A∞` — plafond de l'acquis de séjour.
 *
 * [P] graine — RÉSOLU À L'ENVERS par le calibreur pour que le cycle nominal
 * rende `g^4.4` = ×47,1. Avec `τ₀` ci-dessous, trois heures de séjour portent
 * l'acquis à 46,1, donc la contenance à ×47,1 : c'est là que les deux graines
 * se tiennent l'une l'autre, et changer l'une sans l'autre casse la cible.
 */
export const ACQUIS_MAX = 47.6

/**
 * `τ₀` — temps caractéristique du séjour, en heures, à densité neutre.
 *
 * [P] graine — réglé pour un `t₉₀` ≈ 2 h sur un cycle de 3 h.
 */
export const TAU_SEJOUR_HEURES = 0.87

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

/* ─── Succès ────────────────────────────────────────────────────────────────*/

/**
 * Fraction de sa cible qu'un effectif doit atteindre pour qu'un palier compte
 * comme saturé. L'effectif converge par une exponentielle : il n'atteint jamais
 * exactement sa cible, et un seuil strict ne se déclencherait jamais.
 */
export const SATURATION_D_UN_PALIER = 0.99

/**
 * §8.4 — plancher garanti sur l'assise I. Ce ne sont pas des réglages : ce sont
 * les garanties que le contenu doit tenir, et que le test de cadence vérifie.
 */
export const PREMIER_SUCCES_AVANT_SECONDES = 120
export const CADENCE_MAX_ENTRE_SUCCES_SECONDES = 5 * 60
export const FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES = 30 * 60

/**
 * [P] graine — PART retirée d'un coût par un succès de la Noue.
 *
 * Petite, et volontairement : le §7.2 rappelle qu'une réduction de coût vaut
 * `log_g(1/c)` paliers d'avance, soit un décalage additif qui ne compose pas.
 * Le registre entier de la Noue pèse moins d'un palier. À mesurer en v0.3.
 */
export const PART_REMISE_D_UN_SUCCES = 0.02

/* ─── Budget de verbes (§7.5 règle 2) ───────────────────────────────────────*/

/** L'arbre en porte 10 ; il en reste ~5 pour les succès. Plafond partagé. */
export const BUDGET_DE_VERBES_TOTAL = 15
export const BUDGET_DE_VERBES_ARBRE = 10

/**
 * En deçà, l'absence est créditée mais pas annoncée : recharger la page n'est
 * pas revenir de quelque part, et « la mare a tourné sans toi pendant 0 s »
 * n'apprend rien à personne.
 */
export const SECONDES_MINIMALES_POUR_ANNONCER_LE_RETOUR = 60

/* ─── Boucle ────────────────────────────────────────────────────────────────*/

/** Le jeu appelle tick à 100 ms. Le simulateur l'appelle avec 60 s ou 8 h. */
export const PERIODE_DE_TICK_MS = 100

/** Version de save courante. Toute évolution passe par une migration. */
export const VERSION_SAVE = 2
