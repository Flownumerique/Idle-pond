/* HARNAIS DE MESURE — NON INTÉGRÉ AU JEU.
 * Ces fichiers ont produit docs/RESULTATS.md. Ils servent de référence de
 * lecture pour le modèle du noyau v1.0, jamais de code de production : le jeu
 * vit dans src/, avec Decimal et l'état immuable. Ne pas importer depuis src/.
 */

/**
 * IdlePond — constantes de calibrage
 *
 * Fichier unique importé par le jeu ET par le simulateur.
 * Toute valeur ici est une GRAINE destinée à être réfutée par la mesure.
 */

// ---------------------------------------------------------------- géographie

/** Nombre de paliers par assise. Total = 62. */
export const PALIERS_PAR_ASSISE = [6, 12, 12, 12, 12, 8] as const;

/** Une espèce apparaît tous les N paliers, à partir du premier de l'assise. */
export const ESPECE_TOUS_LES = 3;

export const NB_PALIERS = PALIERS_PAR_ASSISE.reduce((a, b) => a + b, 0);

// ------------------------------------------------------------------- couts

/** Coût du premier palier. */
export const COUT_CREUSER_BASE = 25;

/** `g` — croissance du coût de creusement par palier. */
export const G = 2.4;

/** Coût de déblocage d'une espèce, en fraction du coût de son palier. */
export const COUT_DEBLOCAGE_RATIO = 0.6;

/** Coût du niveau 1 de la première espèce. */
export const COUT_NIVEAU_BASE = 50;

/** Croissance du coût par niveau. */
export const COUT_NIVEAU_CROISSANCE = 1.15;

// -------------------------------------------------------------- production

/**
 * Débit propre du héros, mana/s. C'est sa mutation : il capte l'ambiant seul.
 * Sans lui, la partie ne démarre pas — production nulle, donc mana nul,
 * donc aucune espèce jamais débloquée. Le noyau v1.0 n'avait pas d'amorçage.
 */
export const DEBIT_HEROS = 0.05;

/** Débit de base de la première espèce, mana/s. */
export const DEBIT_BASE = 0.1;

/**
 * Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
 * précédentes : elle double donc l'assiette additive à niveaux égaux.
 */
export const DEBIT_RATIO_ESPECE = 2;

/**
 * `m_p` — multiplicateur global accordé par chaque palier ouvert.
 *
 * Dérivation : sur les 3 paliers séparant deux espèces, la production totale
 * doit être multipliée par D^3. Une espèce nouvelle apporte ×2 (elle double
 * l'assiette). Donc m_p^3 × 2 = D^3.
 */
export const P = {
  /** `D` — production totale par palier. Ajusté par le calibreur. */
  D: 2.31,
  /** `alpha` — exposant du gain de densité à l'éclosion. Ajusté. */
  ALPHA: 0.6,
  /**
   * `theta` — part du besoin que la densité compense.
   * theta = 1 : compensation exacte, les cycles ne s'allongent plus du tout.
   * theta = 0 : aucune compensation, les cycles s'allongent sans fin.
   * C'est LUI qui règle la forme de la courbe, pas `D`.
   */
  THETA: 0.8,
  /** Échelle globale de production. Règle la DURÉE absolue, rien d'autre. */
  ECHELLE: 1,
};

export function multPalier(): number {
  return Math.pow(Math.pow(P.D, ESPECE_TOUS_LES) / DEBIT_RATIO_ESPECE, 1 / ESPECE_TOUS_LES);
}

/** Seuils de niveau et multiplicateur sur l'espèce. */
export const SEUILS: ReadonlyArray<readonly [number, number]> = [
  [10, 2],
  [25, 4],
  [50, 8],
  [100, 16],
];

/** Bonus de production GLOBALE accordé par une espèce au niveau 100. */
export const BONUS_GLOBAL_NIVEAU_100 = 0.03;

// ---------------------------------------------------------------- éclosion

/**
 * Marge de contenance gagnée à chaque éclosion, exprimée en paliers.
 * C'est ce qui autorise à descendre plus bas au cycle suivant.
 */
export const PALIERS_GAGNES_PAR_ECLOSION = 4;

/**
 * Paliers de marge gagnés à la n-ième éclosion.
 *
 * `RAMPE = 0` : marge constante (+4 à chaque fois). C'était l'hypothèse du
 * noyau v1.0 — et le simulateur montre qu'elle produit des cycles de durée
 * PLATE, pas croissante.
 * `RAMPE > 0` : la marge s'élargit avec le corps. Chaque éclosion couvre plus
 * de terrain que la précédente, donc dure plus longtemps.
 */
export const RAMPE_PALIERS = 0;

export function paliersGagnes(eclosion: number): number {
  const n = NB_ECLOSIONS - 1;
  const bas = PALIERS_GAGNES_PAR_ECLOSION - RAMPE_PALIERS;
  const haut = PALIERS_GAGNES_PAR_ECLOSION + RAMPE_PALIERS;
  return bas + ((haut - bas) * eclosion) / Math.max(1, n);
}

export const CONTENANCE_INITIALE = COUT_CREUSER_BASE * Math.pow(G, PALIERS_GAGNES_PAR_ECLOSION);

/**
 * Exposant du multiplicateur de densité.
 *
 * LOI DÉCOUVERTE AU SIMULATEUR : il doit valoir 1 / ALPHA.
 *
 * À chaque éclosion, la production de pointe est multipliée par g^(paliers
 * gagnés). Le gain de densité vaut pointe^ALPHA, donc la densité est
 * multipliée par g^(paliers × ALPHA). Pour que le multiplicateur de densité
 * suive exactement le besoin — g^paliers — il faut l'élever à 1/ALPHA.
 *
 * Si le produit ALPHA × exposant est < 1, la densité prend du retard sur le
 * coût et les cycles s'allongent sans fin : c'est le bras droit du « U ».
 */
export function densiteExposant(): number {
  return P.THETA / P.ALPHA;
}

/** `d0` — densité de référence. */
export const DENSITE_REF = 10;

// ------------------------------------------------------------------- foi

/** Foi produite par seconde, par unité de production totale (log-amortie). */
export const FOI_TAUX = 0.02;

// -------------------------------------------------------------- hors ligne

export const PLAFOND_HORS_LIGNE_H = 6;

// -------------------------------------------------------------- simulation

/** Pas d'intégration du simulateur, en secondes. */
export const PAS_SIM = 60;

/** Nombre d'éclosions simulées. */
export const NB_ECLOSIONS = 15;

/** Garde-fou : arrêt si un cycle dépasse cette durée. */
export const CYCLE_MAX_H = 400;
