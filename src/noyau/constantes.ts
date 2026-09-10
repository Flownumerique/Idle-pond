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

/** Coût de niveau, achat répétable. */
export const RATIO_COUT_NIVEAU = 1.15

/**
 * Seuils, et leur MULTIPLICATEUR CUMULÉ — amendement v1.1 §2.C.
 *
 * La colonne est le multiplicateur cumulé LU AU SEUIL : chaque seuil double, et
 * le total à 100 individus vaut ×16, jamais ×1024. Toute lecture « ×4
 * supplémentaire » est fausse, et `D = 2.31` a été calibré contre celle-ci.
 *
 * Les seuils portent sur le NIVEAU depuis le noyau v1.0 §1.3. Il n'y a plus de
 * population qui converge : le seuil tombe à l'instant où le niveau est payé,
 * et le multiplicateur se reperd à l'éclosion avec le niveau lui-même.
 */
export const SEUILS_DE_JALON: readonly { readonly seuil: number; readonly multiplicateurCumule: number }[] = [
  { seuil: 10, multiplicateurCumule: 2 },
  { seuil: 25, multiplicateurCumule: 4 },
  { seuil: 50, multiplicateurCumule: 8 },
  { seuil: 100, multiplicateurCumule: 16 },
]

/**
 * Niveau au-delà duquel une espèce pose un drapeau PERMANENT (§2.C).
 *
 * L'unique exception à « le multiplicateur de seuil se reperd à l'éclosion » :
 * avoir déjà porté une espèce au niveau cent accorde un bonus définitif,
 * conservé à l'éclosion. C'est un drapeau par espèce, distinct du
 * multiplicateur de seuil, et il ne se repose jamais.
 */
export const SEUIL_DU_DRAPEAU_PERMANENT = 100

/** Une espèce apparaît tous les N paliers, à partir du premier de son assise. */
export const ESPECE_TOUS_LES_N_PALIERS = 3

/**
 * Débit de base d'une espèce, rapporté à celui de la précédente.
 *
 * « Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
 * précédentes : elle double donc l'assiette additive à niveaux égaux. » C'est
 * la valeur du harnais qui a produit `RESULTATS.md` ; à elle seule elle ne
 * porte PAS `D`, et c'est voulu — le reste vient de `multiplicateurDePalier`.
 */
export const DEBIT_RATIO_ESPECE = 2

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
 * `m_p` — multiplicateur global accordé par chaque palier ouvert.
 *
 * Sur les trois paliers qui séparent deux espèces, la production totale doit
 * être multipliée par `D³`, faute de quoi le rapport `g/D` — donc toute la
 * forme de la courbe — cesse de tenir. L'espèce nouvelle apporte
 * `DEBIT_RATIO_ESPECE` ; les paliers apportent le reste. Donc
 * `m_p³ × ratio = D³`.
 *
 * Sans ce terme, la production croîtrait en `D_RATIO^(1/3)` par palier — 1.26
 * — pendant que le coût croît en `g` — 2.4. L'écart se compose : le cycle 8
 * demanderait déjà des milliers d'heures. C'est la seule façon de garder les
 * deux tiers de paliers qui ne portent aucune espèce du bon côté du calibrage.
 */
export function multiplicateurDePalier(): number {
  return Math.pow(
    Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS) / DEBIT_RATIO_ESPECE,
    1 / ESPECE_TOUS_LES_N_PALIERS,
  )
}

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

/**
 * `d₀` — densité de référence du multiplicateur de densité, forme
 * `(1 + densité / d₀) ^ (θ/α)` (contraintes globales du plan).
 *
 * [P] graine — seed neutre à 1, à résoudre quand les nombres seront calibrés.
 */
export const DENSITE_DE_REFERENCE = 1

/**
 * [P] graine — bonus global accordé par espèce ayant déjà atteint le niveau
 * cent. Définitif, conservé à l'éclosion. Mesuré en v0.3.
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
 * [P] graine — débit de base de la PREMIÈRE espèce, mana/s par niveau.
 *
 * Recalé au jalon v0.2, et pas par goût : à 1 mana/s, tout coûtait moins que
 * quelques secondes de production et le premier cycle se figeait au bout de dix
 * minutes, faute d'avoir quoi que ce soit à acheter. Le plancher de cadence du
 * §8.4 est ce qui l'a fait apparaître. À remesurer en v0.3.
 */
export const TAUX_BASE_AU_PALIER_0 = 0.2

/** [P] graine — coût de creusement du palier 1. Croît ensuite en g^index. */
export const COUT_CREUSER_AU_PALIER_1 = 60

/**
 * [P] graine — ancre d'échelle du coût de niveau, au rang d'espèce 0.
 *
 * Remplace `COUT_DEBLOCAGE_AU_PALIER_0` et `COUT_DE_PLACE_AU_PALIER_0`, qui
 * chiffraient la conviction d'un banc et sa première place. Le déblocage n'a
 * plus d'ancre à lui — il est une fraction du coût de son palier, voir
 * `COUT_DEBLOCAGE_RATIO` — et c'est le NIVEAU qui porte l'échelle.
 *
 * Recalé au 2026-09-09 contre le plancher de cadence du §8.4, comme
 * `TAUX_BASE_AU_PALIER_0` l'avait été au jalon v0.2, et il est encadré des deux
 * côtés :
 *
 *   trop bas  — la Noue s'épuise en sept minutes et la première demi-heure
 *               finit sur vingt minutes de silence ;
 *   trop haut — le deuxième achat de la partie tombe au-delà de la cinquième
 *               minute, et la cadence du §8.4 est trouée dès le début.
 *
 * La fenêtre mesurée est étroite : au-delà de ≈95, l'intervalle entre le
 * premier creusement et le deuxième cran dépasse les 300 s. Le harnais de
 * `RESULTATS.md` porte l'équivalent de 500 s de débit là où on en met 450 ;
 * l'écart est celui d'une échelle absolue, pas d'une forme. À remesurer en v0.3.
 */
export const COUT_DU_PREMIER_NIVEAU = 90

/**
 * Ce que coûte de débloquer une espèce, en fraction du coût de son palier.
 *
 * Le déblocage suit le palier plutôt qu'une échelle à lui : c'est le palier qui
 * dit à quelle profondeur l'espèce vit, et donc ce qu'il en coûte de l'y
 * atteindre.
 */
export const COUT_DEBLOCAGE_RATIO = 0.6

/**
 * DÉRIVÉ — un niveau coûte autant que 450 s du débit de base de SON espèce.
 *
 * Écrit comme un rapport, et pas comme un montant, pour que le coût d'un niveau
 * suive le débit de l'espèce qu'il monte : le temps de remboursement d'un
 * niveau est alors le même pour la première espèce et pour la vingt et unième,
 * et aucune ne devient un piège en profondeur. C'est ce qui remplace l'ancien
 * `g^palier` du coût de place, dont le rôle est passé au multiplicateur de
 * profondeur.
 */
export const COUT_NIVEAU_PAR_DEBIT = COUT_DU_PREMIER_NIVEAU / TAUX_BASE_AU_PALIER_0

/** Nombre de paliers ouverts au début d'un cycle. Le héros démarre dans la mare. */
export const PALIERS_OUVERTS_AU_DEPART = 1

/**
 * [P] graine — mana porté à la sortie de l'œuf.
 *
 * DEUX mécanismes tiennent l'amorçage, et ils ne sont PAS interchangeables —
 * c'est une mesure de la tâche 9, pas une hypothèse :
 *
 *   `MANA_A_LA_SORTIE_DE_L_OEUF` (cette constante) tient §8.4 (premier succès
 *   quasi immédiat) et §4.2 (aucun clic obligatoire). C'est une charge
 *   UNIQUE, calée sur le coût du premier déblocage — « ni plus ni moins » —
 *   qui bootstrappe puis cesse de compter : une fois dépensée, elle
 *   n'influence plus jamais le rythme du jeu.
 *
 *   `DEBIT_HEROS`, plus bas, tient `RESULTATS.md` finding 3 — mais le
 *   contenu réel de finding 3 est l'état DÉGÉNÉRÉ (production exactement
 *   nulle, donc plus rien n'est jamais affordable après que la charge
 *   ci-dessus a été dépensée), pas la vitesse du premier achat. Un débit
 *   minuscule suffit à fermer ce trou-là ; il n'a jamais eu besoin d'être
 *   grand.
 *
 * La tâche 9 a d'abord tenté de remplacer la charge par un débit
 * suffisamment grand pour, À LUI SEUL, tenir §8.4. Mesuré et réfuté : un
 * débit PERMANENT (ajouté à l'assiette à chaque tick, pour toujours, pas
 * seulement au démarrage) assez grand pour financer le premier déblocage en
 * moins de 120 s accélère aussi, en permanence, toute la suite de la partie
 * — l'avance initiale se compose sur des dizaines d'achats dans une économie
 * à paliers géométriques — et épuise le registre fini de succès de la Noue
 * bien avant la trentième minute (voir le rapport de la tâche 9, mesures à
 * l'appui : aucune marge testée entre 0.17× et 2× le plancher arithmétique ne
 * satisfait simultanément « premier succès < 120 s » et « aucun trou > 5 min
 * sur 30 min »). Une charge UNIQUE n'a pas ce défaut : elle ne pousse
 * personne en avant après avoir été dépensée. D'où les deux mécanismes,
 * délibérément, plutôt qu'un seul répondant à deux besoins différents.
 *
 * Calé sur le coût du premier déblocage, ni plus ni moins — c'est aussi ce
 * qui permet à `tests/voix.test.ts:73` d'appeler `debloquer` avant tout
 * tick : à `manaCourant` égal au coût pile, l'achat réussit sans qu'aucune
 * seconde de jeu se soit écoulée.
 */
export const MANA_A_LA_SORTIE_DE_L_OEUF = COUT_CREUSER_AU_PALIER_1 * COUT_DEBLOCAGE_RATIO

/**
 * [P] graine — débit propre du héros, mana/s. C'est sa mutation : il capte
 * l'ambiant tout seul, sans attendre aucune espèce.
 *
 * Ferme le TROU DÉGÉNÉRÉ de `RESULTATS.md` finding 3 : une fois la charge de
 * `MANA_A_LA_SORTIE_DE_L_OEUF` dépensée, si plus aucune production
 * n'existait, le mana resterait à zéro pour toujours et aucune espèce ne
 * serait plus jamais débloquée. Un débit minuscule suffit à l'empêcher — il
 * n'a jamais eu besoin d'être assez grand pour, À LUI SEUL, tenir le
 * plancher de cadence du §8.4 : c'est le rôle de la charge ci-dessus. Voir
 * son commentaire pour la mesure qui sépare les deux rôles.
 *
 * Le terme devient négligeable dès que la première espèce est montée de
 * quelques niveaux (`tests/amorcage.test.ts`). À remesurer en v0.3, comme
 * `TAUX_BASE_AU_PALIER_0`.
 */
export const DEBIT_HEROS = 0.05

/**
 * [P] — échelle globale de production, neutre à 1. Elle ne règle que la DURÉE
 * absolue d'une partie, rien dans sa forme.
 *
 * À lire comme du code mort tant qu'elle vaut 1 : c'est voulu. C'est le cadran
 * qu'un calibrage ultérieur actionnera pour accélérer ou ralentir le jeu dans
 * son ensemble sans toucher un seul ratio. L'introduire maintenant, même
 * inerte, évite de retraverser tous les appelants de `productionTotaleParSeconde`
 * le jour où elle bougera.
 */
export const ECHELLE_DE_PRODUCTION = 1

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

/**
 * DÉRIVÉE, depuis le 2026-09-09 — elle valait 1200, saisis à la main.
 *
 * Le blocage doux tombe quand `coût_base × g^P > contenance`. La contenance de
 * départ est donc exactement ce qui décide du nombre de paliers du premier
 * cycle, et le canon en fixe la valeur : `PALIERS_PAR_CYCLE_VISE`. L'écrire
 * plutôt que la choisir est ce que demande le §13.2, et c'est la dérivation du
 * harnais de `RESULTATS.md` (`COUT_CREUSER_BASE × g^paliers_gagnés`).
 *
 * À 1200, le premier cycle s'arrêtait à cinq paliers sur les six de la Noue et
 * la première demi-heure se terminait sur six minutes de silence, faute d'avoir
 * encore quelque chose à acheter.
 */
export const CONTENANCE_INITIALE = COUT_CREUSER_AU_PALIER_1 * Math.pow(G_COUT_PALIER, PALIERS_PAR_CYCLE_VISE)

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

/* ─── Saturation de la jauge — noyau v1.0 §2.2 ──────────────────────────────
 *
 * « Le blocage est doux : il peut continuer à jouer indéfiniment. » Une
 * alerte, une captation qui cesse — et rien d'autre. La jauge pleine ne
 * déclenche plus aucune éclosion : rester au plafond n'est plus une décision
 * qui se prend toute seule à sa place.
 */

/** Alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. » */
export const SEUIL_D_ALERTE_DE_CONTENANCE = 0.85

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
export const VERSION_SAVE = 5
