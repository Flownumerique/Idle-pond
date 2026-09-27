/**
 * IdlePond — vocabulaire de l'état de jeu.
 *
 * Tier 2. Lexique : assise, palier, espèce, niveau, densité, Souffle, technique.
 * Aucun anglicisme, aucun « prestige ».
 *
 * Le banc a disparu le 2026-09-09 avec le modèle à population : une espèce
 * n'est plus une population installée sur un palier, c'est un générateur qu'on
 * débloque une fois et dont on monte le niveau (noyau v1.0 §1.3).
 *
 * Le GDD est le document directif depuis le 2026-09-08. Deux conséquences ici :
 * `bénédiction` a disparu — le Souffle n'achète que des miracles (§4.2) — et le mot
 * « éclosion » désigne encore l'acte que le GDD nomme PONTE, en attendant le
 * renommage transverse qui touche les identifiants et les sauvegardes.
 *
 * Ce module ne contient que des types et les registres de vocabulaire qui
 * doivent exister à l'exécution pour que les tests de canon (§4.3) puissent
 * les parcourir. Aucun paramètre : ils vivent tous dans constantes.ts.
 */
import type Decimal from 'break_infinity.js'

/* ─── Identifiants ──────────────────────────────────────────────────────── */

/** Subdivision majeure. Six. Type de mana propre. */
export type AssiseId = string
/** Subdivision d'une assise. 62 au total. Indexé globalement, 0-based. */
export type IndexPalier = number
export type EspeceId = string
export type TypeManaId = string
export type NoeudTechniqueId = string
export type SuccesId = string
export type BenedictionId = string

export type PorteeDeBenediction = 'ciblee' | 'globale'

/**
 * Une bénédiction — noyau v1.0 §4.2. Deux formes, deux natures : la ciblée
 * MULTIPLIE une espèce nommée, la globale ADDITIONNE au débit de base de
 * toutes. L'additif écrase tôt et s'efface tard ; le croisement se fait seul.
 */
export interface Benediction {
  readonly id: BenedictionId
  readonly portee: PorteeDeBenediction
  /** L'espèce visée. `null` pour la globale. */
  readonly espece: EspeceId | null
}

/* ─── Termes de formule (§7.5 règle 3) ──────────────────────────────────────
 * « Aucun effet chiffré flottant. Un nœud cible toujours un TermeDeFormule
 * nommé, donc auditable dans le détail de captation. »
 *
 * La partition production / coût / confort n'est pas cosmétique : c'est elle
 * qui rend vérifiable par un test, plutôt que par une relecture, le fait
 * qu'aucun système gratuit ne monte la production. Un nœud de technique ne
 * touche que des termes de coût ou de confort.
 *
 * Plus aucune source ne cible un terme de production : le GDD §4.2 interdit que
 * le Souffle achète du rendement, et l'amendement v1.1 §2.D interdit qu'un succès
 * en donne. Le registre est donc entièrement descriptif aujourd'hui — il nomme
 * ce qui compose la captation, pour le détail auditable du §14.3.
 */

export type TermeDeProduction =
  | 'taux_base'
  | 'niveau'
  | 'multiplicateur_jalon'
  | 'multiplicateur_drapeau'
  /**
   * Multiplicateur global accordé par la profondeur ouverte. Multipliait déjà
   * la production avant la tâche 9, sans être nommé ici — une violation du
   * §7.5 règle 3 qu'aucun test ne surveillait. Fermée avec `multiplicateur_densite`.
   */
  | 'multiplicateur_profondeur'
  /** Multiplicateur de densité (`(1 + densité/d₀)^(θ/α)`), appliqué à la production depuis la tâche 9. */
  | 'multiplicateur_densite'
  /**
   * Débit propre du héros, mana/s — une constante, pas un multiplicateur.
   * N'est attribuable à AUCUNE espèce, ce qui l'avait laissé hors du registre
   * jusqu'à la revue de qualité de la tâche 9 : un effet chiffré flottant,
   * exactement ce que §7.5 règle 3 interdit, juste parce qu'il n'avait pas
   * d'espèce à qui s'attribuer.
   */
  | 'debit_heros'
  /**
   * Multiplicateur global du niveau du héros — spec 2026-09-17 [D2] :
   * `(1 + BONUS_PAR_NIVEAU_DU_HEROS) ^ (niveau − 1)`. C'est le quatrième achat
   * en mana, et sa part de `D` est retirée au multiplicateur de profondeur.
   */
  | 'multiplicateur_heros'
  /** Bénédiction ciblée sur l'espèce : `(1 + c) ^ rang`. Noyau v1.0 §4.2. */
  | 'multiplicateur_benediction'
  /** Bénédiction globale : `+ k × rang` sur le débit de base de chaque espèce. */
  | 'benediction_globale'

export type TermeDeCout =
  /**
   * Ouvrir un palier — creusement neuf ou retraversée, noyau v1.0 §3.1 : `f` =
   * 1, un seul puits, un seul levier. Débouché des effets chiffrés de succès
   * (amendement v1.1 §2.D).
   */
  | 'cout_creuser'
  /** Monter une espèce d'un niveau. L'achat répétable de la boucle, ×1.15. */
  | 'cout_niveau'
  /**
   * Débloquer une espèce. Une fois par espèce et par vie.
   *
   * Il n'est plus payé par la densité. Le noyau v1.0 §1.3 en fait une fraction
   * du coût du palier qui porte l'espèce, et rien d'autre : la densité n'a plus
   * qu'un seul débouché, la production, par le multiplicateur de densité (elle
   * est sortie du repeuplement puis du temps du séjour). Le terme redevient donc
   * un levier ordinaire, que technique et succès peuvent viser.
   */
  | 'cout_deblocage'
  /** Faire grandir le héros d'un niveau — spec 2026-09-17 [D3]. Fraction du coût du palier de même rang. */
  | 'cout_croissance'
  /** Une bénédiction, en Souffle. Un terme de coût comme un autre : la technique pourra le viser. */
  | 'cout_benediction'
  | 'cout_temple'
  | 'cout_portail'
  | 'cout_reouverture'

/** Ni production ni coût : plafonds, confort, lisibilité. */
export type TermeDeConfort =
  | 'cap_hors_ligne'
  | 'densite_conservee'
  | 'contenance_de_depart'
  | 'niveau_de_depart'
  | 'charge_alliee_par_reponse'

export type TermeDeFormule = TermeDeProduction | TermeDeCout | TermeDeConfort

export const TERMES_DE_PRODUCTION: readonly TermeDeProduction[] = [
  'taux_base',
  'niveau',
  'multiplicateur_jalon',
  'multiplicateur_drapeau',
  'multiplicateur_profondeur',
  'multiplicateur_densite',
  'debit_heros',
  'multiplicateur_heros',
  'multiplicateur_benediction',
  'benediction_globale',
]

export const TERMES_DE_COUT: readonly TermeDeCout[] = [
  'cout_creuser',
  'cout_niveau',
  'cout_deblocage',
  'cout_croissance',
  'cout_benediction',
  'cout_temple',
  'cout_portail',
  'cout_reouverture',
]

export const TERMES_DE_CONFORT: readonly TermeDeConfort[] = [
  'cap_hors_ligne',
  'densite_conservee',
  'contenance_de_depart',
  'niveau_de_depart',
  'charge_alliee_par_reponse',
]

/* ─── Capacités (§7.5 règle 1) ──────────────────────────────────────────────
 * « Une capacité a exactement une source. » Les verbes viennent de l'arbre ET
 * des succès ; un même CapaciteId ne doit jamais être atteignable par les deux.
 * La source est déclarée dans la donnée, et le test de canon la vérifie.
 * Liste transcrite du §7.3 — les nœuds qui les portent sont du contenu v0.4.
 */
export type CapaciteId =
  | 'file_de_descente'
  | 'creusement_auto'
  | 'achat_auto'
  | 'achat_auto_max'
  | 'deblocage_auto'
  | 'lecture_debits'
  | 'automatismes_hors_ligne'
  | 'rapport_de_retour'
  | 'navigation_directe'
  | 'lecture_eau'
  | 'retour_rapide'

export type SourceDeCapacite = 'technique' | 'succes'

/* ─── Technique (§7) ────────────────────────────────────────────────────────*/

export type BrancheTechniqueId =
  | 'creusement'
  | 'amelioration'
  | 'recrutement'
  | 'entretien'
  | 'construction'
  | 'eclosion'

/**
 * §7.1 — deux régimes de compteur, et c'est une correction par rapport aux
 * versions antérieures qui appliquaient le logarithme partout.
 * - non_borne : points = floor(A · ln(1 + compteur / B))
 * - borne     : table de seuils directe, le nœud n s'ouvre au k-ième événement
 */
export type RegimeCompteur = 'non_borne' | 'borne'

/** Un nœud chiffre cible un terme ; un nœud verbe ouvre une capacité. */
export type EffetDeNoeud =
  | { readonly nature: 'chiffre'; readonly terme: TermeDeCout | TermeDeConfort; readonly facteur: number }
  | { readonly nature: 'verbe'; readonly capacite: CapaciteId }

export interface NoeudTechnique {
  readonly id: NoeudTechniqueId
  readonly branche: BrancheTechniqueId
  readonly rang: number
  readonly cout: number
  readonly effet: EffetDeNoeud
}

/* ─── La voix — GDD §13.1 ───────────────────────────────────────────────────
 *
 * Quatre paliers, et c'est l'axe de progression que le GDD dit le plus
 * important : il porte le tutoriel, l'interface et le récit en même temps.
 *
 * Il n'est pas porté dans l'état, contrairement à ce que le §18.5 esquisse : il
 * se DÉRIVE du nombre de franchissements survécus (voir `voix.ts`). Un champ
 * stocké serait un cache d'une valeur recalculable, et le contrat du §5.1
 * n'admet aucun état hors du reducer. Ce qui devra être stocké le jour du
 * relais (§12.2) est l'événement, pas le palier qui s'en déduit.
 */
export type PalierDeVoix = 'pente' | 'signes' | 'directives' | 'dialogue'

/* ─── Succès (§8) ───────────────────────────────────────────────────────────*/

export type FamilleDeSucces = 'franchissement' | 'seuil' | 'acte'
export type VisibiliteDeSucces = 'ouvert' | 'ferme' | 'secret'

/**
 * Le déclencheur d'un succès est toujours un SEUIL relu sur l'état de fin de
 * tick, jamais un événement consommé au vol.
 *
 * Ce n'est pas un choix de commodité : un déclencheur qui aurait besoin
 * d'observer l'intérieur d'un intervalle ferait diverger 480 pas de 60 s d'un
 * pas de 8 h, et emporterait le hors ligne avec lui (§5.2). Toute condition qui
 * ne s'exprime pas comme une lecture de seuil sur l'état est du décor narratif,
 * pas un déclencheur.
 */
export type DeclencheurDeSucces =
  | { readonly quoi: 'eclosions'; readonly seuil: number }
  | { readonly quoi: 'paliers_ouverts'; readonly seuil: number }
  | { readonly quoi: 'profondeur_max'; readonly seuil: number }
  | { readonly quoi: 'especes_debloquees'; readonly seuil: number }
  | { readonly quoi: 'niveau_d_espece'; readonly espece: EspeceId; readonly seuil: number }
  | { readonly quoi: 'niveaux_cumules'; readonly seuil: number }
  | { readonly quoi: 'production_par_seconde'; readonly seuil: number }
  | { readonly quoi: 'souffle'; readonly seuil: number }
  | { readonly quoi: 'densite_de_palier'; readonly palier: IndexPalier; readonly seuil: number }
  /**
   * Le palier ne peut plus rien recevoir.
   *
   * Remplace `palier_sature`, qui lisait un effectif contre sa cible. Sans
   * population, « plein » se lit sur le NIVEAU : le palier est au complet quand
   * l'espèce qu'il porte a atteint le seuil du drapeau permanent. Un palier qui
   * ne porte aucune espèce ne prendra jamais personne — il l'est dès qu'il
   * s'ouvre.
   */
  | { readonly quoi: 'palier_au_complet'; readonly palier: IndexPalier }

/**
 * Effet d'un succès — amendement v1.1 §2.D.
 *
 * « Un succès ne peut porter qu'un effet qui existe déjà comme terme de
 * technique : réduction de coût, relèvement de plafond, ou verbe. JAMAIS de
 * production. »
 *
 * Il n'y a donc aucun variant `production`, et il ne faut pas en ajouter.
 * Deux raisons :
 *
 *   1. un succès est SUBI. Un multiplicateur de production que personne n'a
 *      choisi est exactement le « multiplicateur flottant » qu'interdit le
 *      §7.5.3 ;
 *   2. les verbes sont déjà budgétés — ~15 au total, dont 10 à l'arbre et ~5
 *      aux succès, une source unique par CapaciteId.
 *
 * Une troisième raison est tombée avec les bénédictions : « la production est
 * le seul débouché du Souffle » n'a plus d'objet, le Souffle n'achetant que des
 * miracles (GDD §4.2).
 *
 * [P] ARBITRAGE OUVERT. Le GDD §14.3 range `rendement` parmi les cibles
 * légitimes d'un effet chiffré, et son schéma §14.9 type la cible sur
 * `TermeDeFormule` entier — donc il AUTORISE ce que l'amendement v1.1 §2.D
 * interdit. Le GDD étant directif, cette restriction ne survit que parce
 * qu'elle est strictement plus sûre et que le schéma du §14.9 est antérieur à
 * l'amendement. À trancher explicitement plutôt qu'à subir.
 *
 * Écarté explicitement : faire payer les succès en Souffle. Le Souffle est ADRESSÉ,
 * il ne se gagne pas par exploit.
 */
export type EffetDeSucces =
  | { readonly genre: 'reduction_cout'; readonly terme: TermeDeCout; readonly part: number }
  | { readonly genre: 'plafond'; readonly terme: TermeDeConfort; readonly part: number }
  | { readonly genre: 'verbe'; readonly capacite: CapaciteId }

/**
 * Ce qu'on retient d'un succès obtenu — GDD §14.5 et §18.5.
 *
 * `registre` FIGE la langue de l'entrée : « une entrée est rédigée dans la
 * langue que le héros avait au moment où il l'a obtenue, et n'est jamais
 * réécrite ». C'est la propriété que le §14.9 range parmi celles qui « ne se
 * rétrofitent pas », et c'est la raison d'être de cette structure : sans elle,
 * relire une vieille entrée la montrerait dans la langue d'aujourd'hui, et la
 * bibliothèque cesserait d'être la preuve du chemin parcouru.
 *
 * Ne pas confondre avec le registre des succès de `donnees/succes/index.ts`,
 * qui est la LISTE des identifiants livrés. Le mot sert aux deux ; le GDD ne
 * connaît que celui-ci.
 */
export interface EntreeDeSucces {
  /** Rang du cycle pendant lequel il est tombé. 0 = la première vie. */
  readonly obtenuAuCycle: number
  readonly registre: PalierDeVoix
}

export interface Succes {
  readonly id: SuccesId
  readonly famille: FamilleDeSucces
  readonly visibilite: VisibiliteDeSucces
  /** Verrouillage par assise : jamais listé avant que son assise soit atteinte. */
  readonly assise: AssiseId
  readonly declencheur: DeclencheurDeSucces
  readonly effet: EffetDeSucces | null
}

/* ─── Contenu structurel ────────────────────────────────────────────────────*/

export interface Assise {
  readonly id: AssiseId
  readonly rang: number
  readonly typeMana: TypeManaId
  readonly indexPremierPalier: IndexPalier
  readonly nombreDePaliers: number
}

/**
 * Un générateur, et l'unité d'achat du joueur (noyau v1.0 §1.3).
 *
 * `rang` est son rang global, de 0 à 20 : c'est lui qui porte le débit de base,
 * qui croît d'une espèce à la suivante. `palier` est le palier qui l'ancre :
 * elle n'est débloquable qu'une fois ce palier ouvert, et une espèce apparaît
 * tous les trois paliers à partir du premier de son assise.
 */
export interface Espece {
  readonly id: EspeceId
  readonly assise: AssiseId
  readonly rang: number
  readonly palier: IndexPalier
}

/** Un palier porte au plus une espèce — une tous les trois (RESULTATS, finding 4). */
export interface Palier {
  readonly index: IndexPalier
  readonly assise: AssiseId
  readonly espece: EspeceId | null
}

/* ─── État ──────────────────────────────────────────────────────────────────*/

/** PRNG à graine, dans l'état. Aucun Math.random dans le noyau. */
export interface EtatPrng {
  readonly graine: number
}

/**
 * Ce qu'une espèce est, dans l'état : un interrupteur et un niveau.
 *
 * Aucune population, aucun effectif, aucune convergence — noyau v1.0 §1.3. Le
 * niveau agit à l'instant où il est payé, et les seuils 10 / 25 / 50 / 100 le
 * lisent directement : ils tombent à l'achat, plus jamais avec le temps. C'est
 * ce qui rend l'équivalence de pas triviale, là où une population qui converge
 * la rendait délicate.
 */
export interface EtatEspece {
  readonly debloquee: boolean
  readonly niveau: number
}

/** Ce que l'éclosion emporte. f = 1 : reset complet, aucune fraction conservée. */
export interface EtatCycle {
  readonly manaCourant: Decimal
  readonly paliersOuverts: number
  readonly especes: Readonly<Record<EspeceId, EtatEspece>>
  /** Indexe le gain de densité et le gain de Souffle (§6.5, §6.6). */
  readonly productionPicParSeconde: Decimal
  readonly dureeSecondes: number
  /**
   * Acquis de séjour, accumulation saturante vers `A∞` (§2.B).
   *
   * C'est par lui, et par lui seul, que la contenance monte : le plafond ne
   * monte QUE par séjour prolongé en mana dense (Tier 0 §8). « Dense » n'agit
   * plus sur l'acquis, dont le temps vaut `τ₀` constant : il agit sur la
   * production, par le multiplicateur de densité. Il se dépense entièrement à
   * l'éclosion.
   */
  readonly acquisDeSejour: number
  /**
   * Le niveau du héros dans CETTE vie — spec 2026-09-17 [D1]. Part à 1, monte
   * à l'achat, se reperd à l'éclosion : il ressort de l'œuf alevin. Ce qu'il
   * EST (contenance, couches) persiste ; ce qu'il a bâti de lui-même régresse.
   */
  readonly niveauDuHeros: number
}

/** Ce que l'éclosion ne touche pas. Un être surévolué conserve ses acquis. */
export interface EtatPermanent {
  /** Charge de mana par palier. Persistante, monotone croissante. */
  readonly densites: readonly number[]
  readonly souffle: Decimal
  /** Limite le stock de mana, pas la production. Conservée à l'éclosion. */
  readonly contenanceMana: Decimal
  /** Une marque par assise fixée. */
  readonly couches: readonly AssiseId[]
  readonly profondeurMaxAtteinte: number
  readonly compteursTechnique: Readonly<Record<BrancheTechniqueId, number>>
  readonly noeudsTechnique: readonly NoeudTechniqueId[]
  /**
   * Les succès obtenus, avec ce qu'on en retient (§14.5).
   *
   * Un Record plutôt qu'une liste, mais TOUJOURS reconstruit dans l'ordre du
   * registre : deux succès franchis pendant le même intervalle arrivent dans un
   * ordre qui dépend de la taille du pas, et l'ordre des clefs d'un objet est
   * celui de leur insertion. Le sérialiser dans l'ordre d'arrivée ferait
   * diverger un pas de 8 h de 480 pas de 60 s sur la seule chaîne de save.
   */
  readonly succes: Readonly<Record<SuccesId, EntreeDeSucces>>
  readonly nombreEclosions: number
  /**
   * Espèces ayant DÉJÀ atteint le niveau cent. Drapeau permanent, conservé à
   * l'éclosion — l'unique exception à la reperte du multiplicateur de seuil.
   * Il tombe désormais à l'ACHAT du centième niveau, jamais pendant un pas.
   */
  readonly especesAyantAtteintCent: readonly EspeceId[]
  /** Le mana expire vers l'ambiant. Il n'est pas détruit (Tier 0 §5). */
  readonly manaAmbiant: Decimal
  /** Compteur Entretien : heures effectivement créditées, jamais écoulées. */
  readonly heuresHorsLigneCreditees: number
  /**
   * Rang acheté de chaque bénédiction — noyau v1.0 §4, spec 2026-09-17 [D5].
   * Permanent : c'est l'écran d'améliorations du jeu. Vide tant que B1 n'a pas
   * rempli le registre.
   */
  readonly benedictions: Readonly<Record<BenedictionId, number>>
}

export interface MesureDeCycle {
  readonly index: number
  /**
   * Durée ÉCOULÉE du cycle, en temps de jeu.
   *
   * Ce n'est pas la durée « active » du §11 : le noyau ne sait pas quand le
   * joueur est devant l'écran. Le temps actif est une quantité de POLITIQUE —
   * la somme des relevés — et il est mesuré par le simulateur, qui est le seul
   * à savoir quand son joueur revient. Confondre les deux fait lire ~600 h
   * calendaires comme si c'étaient les ~38 h actives visées.
   */
  readonly dureeEcouleeSecondes: number
  readonly paliersOuverts: number
  readonly productionPicParSeconde: Decimal
  readonly souffleGagne: Decimal
}

export interface EtatTelemetrie {
  readonly cycles: readonly MesureDeCycle[]
  readonly secondesDepuisDernierSucces: number
  readonly intervallesEntreSucces: readonly number[]
}

/**
 * Les réglages de la COURBE — ce que le calibreur résout, et que le canon fixe.
 *
 * Ils vivent dans l'état, et non en constantes de module lues directement par
 * les fonctions pures, pour une seule raison : le calibreur doit pouvoir
 * balayer des valeurs sans muter un module, et le §5.1 interdit au noyau tout
 * état hors du reducer. Même précédent que `limiteDeContenu` juste en dessous —
 * un seul code, plusieurs mondes.
 *
 * Ils ne sont PAS persistés (R41) : un réglage est une propriété de la VERSION
 * du jeu, pas de la partie. Le persister figerait l'ancienne courbe dans les
 * saves existantes au moment même où on la recalibre. `serialiser` l'omet,
 * `deserialiser` le reprend du repli, c'est-à-dire du canon.
 *
 * La tâche 13 y ajoutera `θ` et l'échelle de production, qui se lisent
 * aujourd'hui en constantes de module.
 */
export interface Reglage {
  /**
   * De combien le temps caractéristique du séjour est multiplié PAR PALIER de
   * profondeur atteinte (amendement v1.2). 1 = `τ` constant, la loi d'avant le
   * 2026-09-16.
   */
  readonly croissanceDuSejourParPalier: number
}

export interface EtatJeu {
  readonly versionSave: number
  readonly prng: EtatPrng
  readonly tempsJeuSecondes: number
  /**
   * Nombre de paliers effectivement livrés — la porte de jalon, pas une valeur
   * de canon. Le monde est dessiné sur 62 paliers dès la v0.1 parce que c'est
   * l'économie que le simulateur doit mesurer ; le JEU n'en offre que ce qui a
   * du contenu, l'assise I au jalon v0.2. « Aucune assise n'est produite avant
   * que la précédente ait été mesurée » (§12).
   *
   * Elle vit dans l'état plutôt qu'en constante pour que le jeu et le
   * simulateur puissent différer sans jamais forker le reducer : un seul code,
   * deux mondes.
   */
  readonly limiteDeContenu: number
  /** Voir `Reglage` : les boutons de la courbe, non persistés. */
  readonly reglage: Reglage
  readonly cycle: EtatCycle
  readonly permanent: EtatPermanent
  readonly telemetrie: EtatTelemetrie
}

/**
 * D'où vient un terme, dans le détail de captation (§8.2).
 *
 * Une structure, jamais une phrase : le noyau ne fabrique aucun texte d'écran.
 * S'il en fabriquait, il finirait par écrire « palier 0 » quelque part — or
 * `palier` est un terme de code et de GDD, pas d'écran (§3), et la faute
 * n'apparaîtrait qu'à la capture d'écran.
 */
export type SourceDeTerme =
  | { readonly quoi: 'niveau'; readonly niveau: number }
  | { readonly quoi: 'palier'; readonly palier: IndexPalier }
  | { readonly quoi: 'drapeaux_permanents'; readonly especes: number }
  /** Source du multiplicateur de profondeur : combien de paliers sont ouverts. */
  | { readonly quoi: 'profondeur'; readonly paliersOuverts: number }
  /** Source du multiplicateur de densité : la densité du séjour (le maximum sur les paliers ouverts). */
  | { readonly quoi: 'densite'; readonly densite: number }
  /** Source de `debit_heros` et de `multiplicateur_heros` : son niveau dans cette vie. */
  | { readonly quoi: 'heros'; readonly niveau: number }
  | { readonly quoi: 'benediction'; readonly rang: number }

/** Une ligne du détail de captation (§8.2) : chaque terme attribuable. */
export interface LigneDeCaptation {
  readonly terme: TermeDeFormule
  readonly valeur: number
  readonly source: SourceDeTerme
}
