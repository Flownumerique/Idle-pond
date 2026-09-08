/**
 * IdlePond — TOUS les textes affichés, et ils sont PROVISOIRES.
 *
 * ╔════════════════════════════════════════════════════════════════════════╗
 * ║  Les NOMS de la Noue sont désormais du canon (amendement v1.1 §2.E).   ║
 * ║  Le lieu, les trois espèces et l'espèce réservée sont arrêtés.         ║
 * ║                                                                        ║
 * ║  Les PHRASES, elles, restent provisoires : le jalon v0.2 demande       ║
 * ║  « aucun texte définitif ». Elles vivent ici, à un seul endroit, et    ║
 * ║  aucune n'est reprise dans un identifiant, une clef de save ou une     ║
 * ║  donnée — les réécrire ne touche aucune autre ligne du dépôt et        ║
 * ║  n'invalide aucune sauvegarde.                                         ║
 * ║                                                                        ║
 * ║  [P] P3 reste ouvert pour les assises II à VI.                         ║
 * ╚════════════════════════════════════════════════════════════════════════╝
 *
 * La charte phonétique du §2.E est une jauge de profondeur : le joueur entend
 * qu'il descend avant de le lire. L'assise I n'invente rien — voyelles claires,
 * lexique réel du français d'eau douce. Le monde n'est pas exotique, il est
 * VIEUX.
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
  noue: 'la Noue',
}

export const NOM_DES_ESPECES: Readonly<Record<EspeceId, string>> = {
  vairon: 'le vairon',
  loche: 'la loche',
  epinoche: 'l’épinoche',
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
    condition: 'Faire une deuxième place dans un banc',
    rapport: 'Un de plus s’est joint sans qu’on insiste.',
  },
  'acte-cinquieme-niveau': {
    nom: 'Le geste prend',
    condition: 'Faire cinq places dans un banc',
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
    nom: 'La Noue répond',
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
    condition: 'Faire dix places dans un banc',
    rapport: 'Le geste se répète tout seul, maintenant.',
  },

  /* — Seuils, engendrés par gabarit ——————————————————————————————————————— */
  'seuil-vairon-10': { nom: 'Dix vairons', condition: 'Dix individus', rapport: 'Ils tiennent le banc ensemble.' },
  'seuil-vairon-25': { nom: 'Vingt-cinq vairons', condition: 'Vingt-cinq individus', rapport: 'Le banc vire d’un seul tenant.' },
  'seuil-vairon-50': { nom: 'Cinquante vairons', condition: 'Cinquante individus', rapport: 'On entend le courant qu’ils font.' },
  'seuil-vairon-100': { nom: 'Cent vairons', condition: 'Cent individus', rapport: 'Ils ne repartiront plus. Jamais.' },
  'seuil-loche-10': { nom: 'Dix loches', condition: 'Dix individus', rapport: 'Le fond est remué par en dessous.' },
  'seuil-loche-25': { nom: 'Vingt-cinq loches', condition: 'Vingt-cinq individus', rapport: 'Elles ouvrent des passages qu’on n’a pas creusés.' },
  'seuil-loche-50': { nom: 'Cinquante loches', condition: 'Cinquante individus', rapport: 'La vase ne tient plus en place.' },
  'seuil-loche-100': { nom: 'Cent loches', condition: 'Cent individus', rapport: 'Elles ne repartiront plus. Jamais.' },
  'seuil-epinoche-10': { nom: 'Dix épinoches', condition: 'Dix individus', rapport: 'Elles tiennent là où l’eau se charge.' },
  'seuil-epinoche-25': { nom: 'Vingt-cinq épinoches', condition: 'Vingt-cinq individus', rapport: 'Rien ne les déloge du bord.' },
  'seuil-epinoche-50': { nom: 'Cinquante épinoches', condition: 'Cinquante individus', rapport: 'L’eau lourde ne leur fait plus rien.' },
  'seuil-epinoche-100': { nom: 'Cent épinoches', condition: 'Cent individus', rapport: 'Elles ne repartiront plus. Jamais.' },

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
    nom: 'La Noue est chargée',
    condition: 'Charger le premier creux',
    rapport: 'Ce qui a été porté là y est resté.',
  },
  'franchissement-fond-de-la-mare': {
    nom: 'Le fond de la Noue',
    condition: 'Ouvrir la Noue jusqu’au fond',
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
      nom: `${n} dans la Noue`,
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
