/**
 * IdlePond — vocabulaire de l'état de jeu.
 *
 * Tier 2. Le lexique du §3 du prompt de lancement s'applique ici sans
 * exception : assise, palier, banc, éclosion, densité, Foi, bénédiction,
 * technique, acclimatation, conviction. Aucun anglicisme, aucun « prestige »,
 * aucune « ponte ».
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
/** Une espèce installée sur un palier. « Le banc suit », « Compter les bancs ». */
export type BancId = string
export type TypeManaId = string
export type NoeudTechniqueId = string
export type BenedictionId = string
export type SuccesId = string

/* ─── Termes de formule (§7.5 règle 3) ──────────────────────────────────────
 * « Aucun effet chiffré flottant. Un nœud cible toujours un TermeDeFormule
 * nommé, donc auditable dans le détail de captation. »
 *
 * La partition production / coût / confort n'est pas cosmétique : c'est elle
 * qui rend la frontière du §4.3 vérifiable par un test plutôt que par une
 * relecture. Une bénédiction ne touche que des termes de production ; un nœud
 * de technique ne touche que des termes de coût ou de confort.
 */

export type TermeDeProduction =
  | 'taux_base'
  | 'effectif'
  | 'rendement_acclimatation'
  | 'multiplicateur_jalon'
  | 'benediction_ciblee'
  | 'benediction_globale'

export type TermeDeCout =
  | 'cout_creuser'
  | 'cout_niveau'
  | 'cout_deblocage'
  | 'cout_reconviction'
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
  'effectif',
  'rendement_acclimatation',
  'multiplicateur_jalon',
  'benediction_ciblee',
  'benediction_globale',
]

export const TERMES_DE_COUT: readonly TermeDeCout[] = [
  'cout_creuser',
  'cout_niveau',
  'cout_deblocage',
  'cout_reconviction',
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

/** Une bénédiction monte la production, jamais autre chose (§4.3). */
export type EffetDeBenediction =
  | { readonly forme: 'multiplicative'; readonly terme: TermeDeProduction; readonly espece: EspeceId }
  | { readonly forme: 'additive'; readonly terme: TermeDeProduction }

export interface Benediction {
  readonly id: BenedictionId
  readonly effet: EffetDeBenediction
  readonly coutParRang: readonly number[]
}

/* ─── Succès (§8) ───────────────────────────────────────────────────────────*/

export type FamilleDeSucces = 'franchissement' | 'seuil' | 'acte'
export type VisibiliteDeSucces = 'ouvert' | 'ferme' | 'secret'

export type EffetDeSucces =
  | { readonly nature: 'chiffre'; readonly terme: TermeDeFormule; readonly facteur: number }
  | { readonly nature: 'verbe'; readonly capacite: CapaciteId }

export interface Succes {
  readonly id: SuccesId
  readonly famille: FamilleDeSucces
  readonly visibilite: VisibiliteDeSucces
  /** Verrouillage par assise : jamais listé avant que son assise soit atteinte. */
  readonly assise: AssiseId
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

export interface Espece {
  readonly id: EspeceId
  readonly assise: AssiseId
}

/** Une espèce installée sur un palier : l'unité d'achat du joueur. */
export interface Banc {
  readonly id: BancId
  readonly espece: EspeceId
  readonly palier: IndexPalier
}

export interface Palier {
  readonly index: IndexPalier
  readonly assise: AssiseId
  readonly bancs: readonly Banc[]
}

/* ─── État ──────────────────────────────────────────────────────────────────*/

/** PRNG à graine, dans l'état. Aucun Math.random dans le noyau. */
export interface EtatPrng {
  readonly graine: number
}

export interface EtatBanc {
  /** Niveau du banc. 0 = pas encore convaincu. Fixe l'effectif cible. */
  readonly niveau: number
  /** Effectif réel, qui converge vers la cible à la vitesse de repeuplement. */
  readonly effectif: number
}

/** Ce que l'éclosion emporte. f = 1 : reset complet, aucune fraction conservée. */
export interface EtatCycle {
  readonly manaCourant: Decimal
  readonly paliersOuverts: number
  readonly bancs: Readonly<Record<BancId, EtatBanc>>
  /** Indexe le gain de densité et le gain de Foi (§6.5, §6.6). */
  readonly productionPicParSeconde: Decimal
  readonly dureeSecondes: number
}

/** Ce que l'éclosion ne touche pas. Un être surévolué conserve ses acquis. */
export interface EtatPermanent {
  /** Charge de mana par palier. Persistante, monotone croissante. */
  readonly densites: readonly number[]
  /** Rendement du héros par type de mana. Jamais repayé, jamais remis à zéro. */
  readonly acclimatations: Readonly<Record<TypeManaId, number>>
  readonly foi: Decimal
  /** Limite le stock de mana, pas la production. Conservée à l'éclosion. */
  readonly contenanceMana: Decimal
  /** Une marque par assise fixée. */
  readonly couches: readonly AssiseId[]
  readonly profondeurMaxAtteinte: number
  readonly compteursTechnique: Readonly<Record<BrancheTechniqueId, number>>
  readonly noeudsTechnique: readonly NoeudTechniqueId[]
  readonly benedictions: Readonly<Record<BenedictionId, number>>
  readonly succesDebloques: readonly SuccesId[]
  readonly nombreEclosions: number
  /** Le mana expire vers l'ambiant. Il n'est pas détruit (Tier 0 §5). */
  readonly manaAmbiant: Decimal
  /** Compteur Entretien : heures effectivement créditées, jamais écoulées. */
  readonly heuresHorsLigneCreditees: number
}

export interface MesureDeCycle {
  readonly index: number
  readonly dureeActiveSecondes: number
  readonly secondesEnRedescente: number
  readonly paliersOuverts: number
  readonly productionPicParSeconde: Decimal
  readonly foiGagnee: Decimal
}

export interface EtatTelemetrie {
  readonly cycles: readonly MesureDeCycle[]
  readonly secondesEnRedescente: number
  readonly secondesDepuisDernierSucces: number
  readonly intervallesEntreSucces: readonly number[]
}

export interface EtatJeu {
  readonly versionSave: number
  readonly prng: EtatPrng
  readonly tempsJeuSecondes: number
  readonly cycle: EtatCycle
  readonly permanent: EtatPermanent
  readonly telemetrie: EtatTelemetrie
}

/** Une ligne du détail de captation (§8.2) : chaque terme attribuable. */
export interface LigneDeCaptation {
  readonly terme: TermeDeFormule
  readonly valeur: number
  readonly source: string
}
