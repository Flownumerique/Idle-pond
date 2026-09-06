/**
 * IdlePond — TOUS les textes affichés, et ils sont PROVISOIRES.
 *
 * ╔════════════════════════════════════════════════════════════════════════╗
 * ║  [P] P3 — CE FICHIER N'EST PAS DU CANON.                               ║
 * ║                                                                        ║
 * ║  Le §14 déclare les conventions phonétiques bloquantes pour « tout nom ║
 * ║  d'assise, d'espèce et de bâtiment », et le jalon v0.2 demande en même ║
 * ║  temps « la mare nommée » et « aucun texte définitif ». Les deux se    ║
 * ║  tiennent si, et seulement si, tout le texte affiché vit à un seul     ║
 * ║  endroit : celui-ci.                                                   ║
 * ║                                                                        ║
 * ║  Rien de ce fichier n'est repris dans un identifiant, une clef de save ║
 * ║  ou une donnée. Remplacer ces chaînes par les noms canoniques ne       ║
 * ║  touche aucune autre ligne du dépôt et n'invalide aucune sauvegarde.   ║
 * ║                                                                        ║
 * ║  Seule « la mare » vient du contrat lui-même (§3 et §12). Le reste     ║
 * ║  attend l'auteur.                                                      ║
 * ╚════════════════════════════════════════════════════════════════════════╝
 *
 * Règle d'écriture, elle non provisoire (§8.3) : le narrateur rapporte ce qui
 * est arrivé et ce que ça a changé. Il n'explique JAMAIS pourquoi le monde
 * fonctionne ainsi. Une seule phrase qui livre la vraie physique annule le
 * principe « nommé sans être compris » et grille le retournement d'échelle de
 * fin de partie.
 */
import type { AssiseId, EspeceId, SuccesId } from '../noyau/types'

/** Le nom propre du lieu. L'UI n'affiche jamais « Assise I » (§3). */
export const NOM_DES_ASSISES: Readonly<Record<AssiseId, string>> = {
  'assise-1': 'la mare',
}

export const NOM_DES_ESPECES: Readonly<Record<EspeceId, string>> = {
  'espece-1-1': 'les tourneurs',
  'espece-1-2': 'les brouteurs',
  'espece-1-3': 'les fileurs',
}

export interface TexteDeSucces {
  /** Titre court. Visible dès que le succès est listé, même fermé. */
  readonly nom: string
  /** La condition, masquée tant que le succès est fermé. */
  readonly condition: string
  /** Ce qui est arrivé, et ce que ça a changé. Rien d'autre. */
  readonly rapport: string
}

export const TEXTES_DE_SUCCES: Readonly<Record<SuccesId, TexteDeSucces>> = {
  /* — Actes ——————————————————————————————————————————————————————————————— */
  'acte-premiere-conviction': {
    nom: 'Un banc te suit',
    condition: 'Convaincre un premier banc',
    rapport: 'Ils sont venus sans qu’on les appelle deux fois.',
  },
  'acte-deuxieme-niveau': {
    nom: 'Ils reviennent',
    condition: 'Mener un banc au deuxième niveau',
    rapport: 'Un de plus s’est joint sans qu’on insiste.',
  },
  'acte-cinquieme-niveau': {
    nom: 'Le geste prend',
    condition: 'Mener un banc au cinquième niveau',
    rapport: 'Ça va plus vite qu’au début.',
  },
  'acte-premier-banc-de-cinq': {
    nom: 'Cinq à demeure',
    condition: 'Cinq individus installés dans un même banc',
    rapport: 'Ils ne repartent plus entre deux passages.',
  },
  'acte-premier-creusement': {
    nom: 'La roche cède',
    condition: 'Creuser une fois plus bas',
    rapport: 'Le fond s’est ouvert. Il y avait de la place dessous.',
  },
  'acte-deux-bancs': {
    nom: 'Ils sont deux',
    condition: 'Convaincre deux bancs à la fois',
    rapport: 'Le second n’a pas fui en voyant le premier.',
  },
  'acte-trois-bancs': {
    nom: 'La mare répond',
    condition: 'Convaincre trois bancs à la fois',
    rapport: 'On ne peut plus les compter d’un seul regard.',
  },
  'acte-premier-palier-sature': {
    nom: 'Plein à ras',
    condition: 'Remplir un creux jusqu’à sa cible',
    rapport: 'Ce creux ne prend plus personne. Il faudra descendre.',
  },
  'acte-dixieme-niveau': {
    nom: 'La main est faite',
    condition: 'Mener un banc au dixième niveau',
    rapport: 'Le geste se répète tout seul, maintenant.',
  },

  /* — Seuils, engendrés par gabarit ——————————————————————————————————————— */
  'seuil-espece-1-1-10': { nom: 'Dix tourneurs', condition: 'Dix individus', rapport: 'Ils tournent ensemble.' },
  'seuil-espece-1-1-25': { nom: 'Vingt-cinq tourneurs', condition: 'Vingt-cinq individus', rapport: 'Le cercle s’élargit.' },
  'seuil-espece-1-1-50': { nom: 'Cinquante tourneurs', condition: 'Cinquante individus', rapport: 'On entend le courant qu’ils font.' },
  'seuil-espece-1-1-100': { nom: 'Cent tourneurs', condition: 'Cent individus', rapport: 'Le fond bouge quand ils passent.' },
  'seuil-espece-1-2-10': { nom: 'Dix brouteurs', condition: 'Dix individus', rapport: 'La vase est plus claire.' },
  'seuil-espece-1-2-25': { nom: 'Vingt-cinq brouteurs', condition: 'Vingt-cinq individus', rapport: 'Ils ont nettoyé jusqu’aux bords.' },
  'seuil-espece-1-2-50': { nom: 'Cinquante brouteurs', condition: 'Cinquante individus', rapport: 'Rien ne se dépose plus.' },
  'seuil-espece-1-2-100': { nom: 'Cent brouteurs', condition: 'Cent individus', rapport: 'L’eau est nette jusqu’au fond.' },
  'seuil-espece-1-3-10': { nom: 'Dix fileurs', condition: 'Dix individus', rapport: 'Les fils tiennent d’une paroi à l’autre.' },
  'seuil-espece-1-3-25': { nom: 'Vingt-cinq fileurs', condition: 'Vingt-cinq individus', rapport: 'On ne voit plus la roche derrière.' },
  'seuil-espece-1-3-50': { nom: 'Cinquante fileurs', condition: 'Cinquante individus', rapport: 'Ça retient ce qui tombe.' },
  'seuil-espece-1-3-100': { nom: 'Cent fileurs', condition: 'Cent individus', rapport: 'Tout le creux est tendu.' },

  /* — Franchissements ————————————————————————————————————————————————————— */
  'franchissement-premiere-eclosion': {
    nom: 'Rentrer',
    condition: 'Éclore une première fois',
    rapport: 'Tout est resté en bas. Presque tout.',
  },
  'franchissement-deuxieme-eclosion': {
    nom: 'Rentrer encore',
    condition: 'Éclore deux fois',
    rapport: 'La descente a été plus courte que la première.',
  },
  'franchissement-troisieme-eclosion': {
    nom: 'Le chemin se sait',
    condition: 'Éclore trois fois',
    rapport: 'Les mêmes fonds, et plus vite.',
  },
  'franchissement-densite': {
    nom: 'La mare est chargée',
    condition: 'Charger le premier creux',
    rapport: 'Ce qui a été porté là y est resté.',
  },
  'franchissement-fond-de-la-mare': {
    nom: 'Le fond de la mare',
    condition: 'Ouvrir la mare jusqu’au fond',
    rapport: 'Il n’y a plus de roche à ouvrir ici.',
  },
}

/**
 * Les familles engendrées par gabarit ont leur texte engendré aussi.
 *
 * Écrire à la main quarante entrées qui ne diffèrent que par un nombre serait
 * une invitation à la faute de copie, et le §8.1 range explicitement ces
 * familles dans le « généré par gabarit ». Les actes et les franchissements,
 * eux, restent écrits un par un : ils sont « à la main » dans le même §8.1.
 */
/** Le rapport change avec le rang : deux entrées voisines ne se lisent jamais pareil. */
function tour<T>(liste: readonly T[], n: number): T {
  return liste[n % liste.length]
}

const RAPPORTS_DE_MARE: readonly string[] = [
  'On ne les compte plus d’un seul regard.',
  'Il y a du monde jusque dans les recoins.',
  'L’eau bouge toute seule, maintenant.',
  'Le fond ne se voit plus à travers eux.',
  'Ça tient sans qu’on s’en occupe.',
]

const RAPPORTS_DE_PROFONDEUR: readonly string[] = [
  'Le fond est plus loin qu’à la dernière descente.',
  'La lumière ne descend plus jusqu’ici.',
  'L’eau est froide, et plus lourde.',
]

const GABARITS: readonly { readonly motif: RegExp; readonly texte: (n: number) => TexteDeSucces }[] = [
  {
    motif: /^seuil-mare-(\d+)$/,
    texte: (n) => ({
      nom: `${n} dans la mare`,
      condition: `${n} individus, tous bancs confondus`,
      rapport: tour(RAPPORTS_DE_MARE, Math.floor(n / 20)),
    }),
  },
  {
    motif: /^seuil-profondeur-(\d+)$/,
    texte: (n) => ({
      nom: `${n} creusements`,
      condition: `Ouvrir ${n} fois plus bas dans la même vie`,
      rapport: tour(RAPPORTS_DE_PROFONDEUR, n),
    }),
  },
  {
    motif: /^seuil-palier-sature-(\d+)$/,
    texte: () => ({
      nom: 'Un creux de plus est plein',
      condition: 'Remplir un creux jusqu’à sa cible',
      rapport: 'Celui-là ne prendra plus personne.',
    }),
  },
]

/** Repli sobre : un succès sans texte reste listable, il ne casse pas l'écran. */
export const TEXTE_DE_SUCCES_INCONNU: TexteDeSucces = {
  nom: '—',
  condition: '—',
  rapport: '—',
}

export function texteDuSucces(id: SuccesId): TexteDeSucces {
  const ecrit = TEXTES_DE_SUCCES[id]
  if (ecrit !== undefined) return ecrit
  for (const gabarit of GABARITS) {
    const trouve = gabarit.motif.exec(id)
    if (trouve !== null) return gabarit.texte(Number(trouve[1]))
  }
  return TEXTE_DE_SUCCES_INCONNU
}
