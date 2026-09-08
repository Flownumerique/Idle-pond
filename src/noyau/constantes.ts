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

/**
 * Ce que l'éclosion emporte du peuplement et de la géométrie. 1 = tout.
 *
 * Conforme au GDD §10.1 : « les parois se referment, les galeries s'effondrent,
 * les paliers profonds redeviennent inaccessibles », et « ils ne se souviennent
 * pas — il faut reconvaincre ». Rien ne se conserve à moitié.
 *
 * À NE PAS CONFONDRE avec le `f` du GDD §6.4, qui est une tout autre quantité :
 * la fraction du coût d'origine que coûte un palier RETRAVERSÉ. C'est
 * `F_FRACTION_D_AMENAGEMENT`, plus bas.
 */
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

/* ─── La redescente — GDD §6.4 ──────────────────────────────────────────────
 *
 * « Le puits de la redescente est aménager : rendre un palier de nouveau
 * habitable pour du vivant ordinaire. »
 *
 *   coût_aménagement(palier) = coût_base(palier) × f × réduction_technique
 *
 * Le héros ne repaie JAMAIS son acclimatation (Tier 0 §3) : ce qui se repaie
 * est l'habitabilité du palier pour son peuple. « Ce n'est pas lui qui se
 * réacclimate, c'est son peuple qui n'y est jamais allé. »
 */

/**
 * [P5] graine — `f`, fraction du coût d'origine que coûte un palier déjà
 * atteint dans une vie précédente. Paramètre global unique.
 *
 * Le GDD le veut « réglé pour viser les 20–25 % » de cycle en redescente, et
 * prévient au même endroit que le régler seul est tourner le mauvais bouton :
 * « c'est k, le taux de repeuplement, qui produit réellement les 20–25 % ».
 * Les deux sont à calibrer ensemble ([P6]), et `k` est aujourd'hui dégénéré —
 * décision ouverte V11. Cette graine est donc une entrée de mesure, pas un
 * réglage abouti.
 */
export const F_FRACTION_D_AMENAGEMENT = 0.25

/**
 * [P] graine — exposant de la densité dans le coût de conviction (GDD §7.1).
 *
 * Le §7.1 écrit une division par la densité locale, sans exposant ni forme
 * normalisée. Prise au pied de la lettre, elle rendrait la conviction gratuite
 * dès la mi-partie — la densité vaut `pointe^α` et croît sans borne — et elle
 * diviserait par zéro au premier cycle, où rien n'est encore chargé.
 *
 * La forme retenue est `(1 + densité)^e` : neutre à densité nulle, donc le
 * premier cycle n'est pas touché, et croissante ensuite. `e` est la graine.
 */
export const EXPOSANT_RECONVICTION_DENSITE = 0.5

/* ─── Les deux canaux de captation — GDD §3 et §3.0 ─────────────────────────
 *
 *   captation/s =   débit_natif(population_vivante_présente)
 *                 + débit_acclimaté(part_mûre(palier) × rendement_acclimatation)
 *
 * Additifs, jamais multiplicatifs. « Fixé (canon) » au §16.1, comme la borne du
 * canal acclimaté par la part mûre.
 */

/**
 * [P29] graine — temps caractéristique de maturation d'un palier, en heures.
 *
 * Le GDD la laisse explicitement ouverte et dit ce qu'elle décide : « le premier
 * réglage décide si l'arbitrage se joue à l'échelle d'une session ou d'un
 * cycle ». Calée sur la durée d'un cycle du §16.2, donc sur le cycle : peupler
 * ou laisser mûrir est une décision qui engage une vie, pas une session.
 */
export const TAU_MATURATION_HEURES = 6

/**
 * [P29] graine — place à laquelle l'eau d'un palier est moitié vive, moitié
 * mûre à l'équilibre.
 *
 * C'est le bouton de sensibilité de l'arbitrage : plus il est bas, plus vite un
 * peu de peuplement écrase le rendement acclimaté.
 */
export const PLACE_QUI_DILUE_A_MOITIE = 10

/**
 * [P] graine — force du canal acclimaté, exprimée en INDIVIDUS ÉQUIVALENTS.
 *
 * Un palier entièrement mûr rapporte autant que `n` individus y vivraient. Le
 * dire ainsi plutôt qu'en valeur absolue est ce qui garde le canal sur l'échelle
 * économique de l'autre : il suit `D^palier` comme le reste, donc il ne devient
 * ni négligeable ni dominant en descendant.
 *
 * Volontairement petit — le §3 le veut « très bas » face à un natif « à 100 %,
 * d'emblée ». Un banc peuplé passe la centaine d'individus et porte en plus son
 * multiplicateur de seuil ; peupler reste très largement supérieur en débit brut.
 *
 * ATTENTION : le second facteur du canal, `rendement_acclimatation`, vaut 1
 * partout jusqu'en v0.5. Le canal est donc aujourd'hui à sa force MAXIMALE, et
 * il faudra remesurer cette graine le jour où l'acclimatation sera réelle.
 */
export const INDIVIDUS_EQUIVALENTS_DU_CANAL_ACCLIMATE = 1

/**
 * [P] — l'affinité du §7.1 est du contenu v0.5, au même titre que
 * l'acclimatation : elle demande une table espèce × type de mana, et
 * `especes-cadre.md` n'est pas au dépôt. D'ici là elle vaut 1 partout, ce qui
 * laisse la formule juste sans qu'aucune valeur ne soit devinée.
 */
export const AFFINITE_PLEINE_JUSQU_EN_V05 = 1

/** [P] graine — `k`, taux de repeuplement, par seconde. Mesuré en v0.3. */
export const K_TAUX_DE_REPEUPLEMENT = 1 / 300

/*
 * [P6] — depuis V11, `k` est le SEUL réglage du repeuplement : la densité en
 * est découplée (voir `vitesseDeRepeuplement`). Le GDD §6.4 en fait le pilote
 * réel des 20–25 % de cycle en redescente — « c'est k qui les produit », là où
 * `f` ne fait qu'un décalage constant. Il se calibre donc avec `f`, jamais seul.
 */

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

/* ─── Saturation de la jauge — GDD §2.4 ─────────────────────────────────────
 *
 * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
 * partie. C'est la seule pénalité du jeu, et elle est douce. »
 *
 * Trois clauses, et elles se tiennent : une alerte, une captation qui cesse,
 * puis une divergence que le joueur n'a pas choisie.
 */

/** Alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. » */
export const SEUIL_D_ALERTE_DE_CONTENANCE = 0.85

/**
 * [P] graine — délai de saturation CONTINUE au bout duquel la divergence se
 * déclenche seule. Le GDD §2.4 pose le délai sans lui donner de valeur.
 *
 * Il est dérivé du plafond hors ligne, et il le faut : le pilier n° 2 est « ne
 * jamais punir l'absence », et une divergence qu'une seule absence suffirait à
 * déclencher serait exactement cela. Le délai doit donc rester hors d'atteinte
 * d'un retour au plafond MAXIMAL — pas seulement du plafond initial, sans quoi
 * la branche Entretien rendrait le jeu plus punitif à mesure qu'elle
 * l'améliore.
 *
 * Deux absences pleines sans le moindre geste entre elles : c'est de
 * l'inattention, pas une vie. Le compteur se remet à zéro à la première
 * dépense, donc revenir et faire quoi que ce soit suffit toujours à l'écarter.
 *
 * À mesurer en v0.3.
 */
export const DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES = 2 * CAP_HORS_LIGNE_HEURES_MAXIMUM

/**
 * [P] graine — part de l'acquis de séjour que fixe une divergence NON CHOISIE.
 *
 * §2.4 : « la ponte se déclenche seule, et fixe moins d'acquis qu'une ponte
 * choisie. » Moins, pas rien : la pénalité est douce, et le joueur ne perd
 * jamais sa partie. À mesurer en v0.3.
 */
export const PART_D_ACQUIS_FIXEE_PAR_DIVERGENCE_NON_CHOISIE = 0.5

/* ─── Paliers de voix — GDD §13.1 ───────────────────────────────────────────*/

/**
 * Franchissements survécus ouvrant chaque palier de voix.
 *
 * Le GDD §13.1 les donne en pontes : les signes à la première, les directives
 * à la troisième. `dialogue` n'a pas de seuil chiffré — il est déclenché par le
 * relais (§12.2), contenu de la v1.0, et reste donc inatteignable.
 */
export const FRANCHISSEMENTS_POUR_LES_SIGNES = 1
export const FRANCHISSEMENTS_POUR_LES_DIRECTIVES = 3

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
export const VERSION_SAVE = 4
