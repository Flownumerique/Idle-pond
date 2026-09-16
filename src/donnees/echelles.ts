/**
 * IdlePond — puissances tabulées des ratios géométriques.
 *
 * Contenu pur, sans logique : deux suites entièrement déterminées par les
 * constantes du §13. Elles sont tabulées une fois parce que `Decimal.pow` est
 * appelé des centaines de milliers de fois par simulation, et qu'un simulateur
 * lent est un simulateur qu'on ne lance pas.
 *
 * Ce n'est pas un cache au sens du §5.1 — rien ici ne dépend d'un état de jeu,
 * rien ne se met à jour, rien ne se souvient d'un tick : ce sont des constantes
 * dérivées de constantes, au même titre que la liste des paliers. Le noyau
 * reste sans état hors du reducer.
 */
import Decimal from 'break_infinity.js'
import {
  DEBIT_RATIO_ESPECE,
  G_COUT_PALIER,
  NOMBRE_DE_PALIERS,
  RATIO_COUT_NIVEAU,
  TAUX_BASE_AU_PALIER_0,
  multiplicateurDePalier,
} from '../noyau/constantes'

function tabuler(ratio: number, longueur: number): readonly Decimal[] {
  const table: Decimal[] = [new Decimal(1)]
  for (let i = 1; i < longueur; i += 1) table.push(table[i - 1].mul(ratio))
  return table
}

/** g^p, pour tous les paliers. */
const PUISSANCES_DE_G = tabuler(G_COUT_PALIER, NOMBRE_DE_PALIERS + 1)

/**
 * 1.15^n. Le niveau n'est pas borné par le canon, seulement par ce que le
 * joueur peut porter : au-delà de la table, on retombe sur le calcul direct.
 */
const NIVEAUX_TABULES = 2048
const PUISSANCES_DU_COUT_DE_NIVEAU = tabuler(RATIO_COUT_NIVEAU, NIVEAUX_TABULES)

export function puissanceDeG(exposant: number): Decimal {
  return PUISSANCES_DE_G[exposant] ?? Decimal.pow(G_COUT_PALIER, exposant)
}

export function puissanceDuCoutDeNiveau(exposant: number): Decimal {
  return PUISSANCES_DU_COUT_DE_NIVEAU[exposant] ?? Decimal.pow(RATIO_COUT_NIVEAU, exposant)
}

/**
 * `m_p ^ paliers`, le multiplicateur global de profondeur.
 *
 * Tabulé, et non calculé par `tabuler` : les deux suites plus haut se
 * construisent par multiplications successives, celle-ci par `Decimal.pow`
 * comme l'écrivait `multiplicateurDeProfondeur`. Ce n'est pas un détail de
 * forme — les deux chemins ne rendent pas le même flottant, et toute mesure
 * déjà prise bougerait sous nos pieds. La table mémorise l'expression exacte,
 * elle ne la réécrit pas.
 */
function puissanceDuPalier(exposant: number): Decimal {
  return Decimal.pow(multiplicateurDePalier(), exposant)
}

const PUISSANCES_DU_MULTIPLICATEUR_DE_PALIER = Array.from({ length: NOMBRE_DE_PALIERS + 2 }, (_, p) =>
  puissanceDuPalier(p),
)

export function puissanceDuMultiplicateurDePalier(exposant: number): Decimal {
  return PUISSANCES_DU_MULTIPLICATEUR_DE_PALIER[exposant] ?? puissanceDuPalier(exposant)
}

/**
 * Débit de base par RANG d'espèce — `TAUX_BASE_AU_PALIER_0 × ratio^rang`.
 *
 * Même raison que les puissances de `g` : `debitBaseDeLEspece` est appelée deux
 * fois par espèce et par décision d'achat, soit des dizaines de millions de
 * fois par `simuler(45)`, pour rendre à chaque fois l'un d'une vingtaine de
 * nombres. La table porte l'expression telle quelle, `Math.pow` compris.
 */
function debitBaseDuRangCalcule(rang: number): Decimal {
  return new Decimal(TAUX_BASE_AU_PALIER_0).mul(Math.pow(DEBIT_RATIO_ESPECE, rang))
}

const DEBITS_DE_BASE = Array.from({ length: NOMBRE_DE_PALIERS + 1 }, (_, rang) => debitBaseDuRangCalcule(rang))

export function debitBaseDuRang(rang: number): Decimal {
  return DEBITS_DE_BASE[rang] ?? debitBaseDuRangCalcule(rang)
}
