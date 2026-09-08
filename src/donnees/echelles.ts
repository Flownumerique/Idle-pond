/**
 * IdlePond — puissances tabulées des ratios géométriques.
 *
 * Contenu pur, sans logique : trois suites entièrement déterminées par les
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
  D_PRODUCTION_PAR_PALIER,
  G_COUT_PALIER,
  NOMBRE_DE_PALIERS,
  RATIO_COUT_NIVEAU,
} from '../noyau/constantes'

function tabuler(ratio: number, longueur: number): readonly Decimal[] {
  const table: Decimal[] = [new Decimal(1)]
  for (let i = 1; i < longueur; i += 1) table.push(table[i - 1].mul(ratio))
  return table
}

/** g^p, pour tous les paliers. */
const PUISSANCES_DE_G = tabuler(G_COUT_PALIER, NOMBRE_DE_PALIERS + 1)

/** D^p, pour tous les paliers. */
const PUISSANCES_DE_D = tabuler(D_PRODUCTION_PAR_PALIER, NOMBRE_DE_PALIERS + 1)

/**
 * 1.15^n. Le niveau n'est pas borné par le canon, seulement par ce que le
 * joueur peut porter : au-delà de la table, on retombe sur le calcul direct.
 */
const NIVEAUX_TABULES = 2048
const PUISSANCES_DU_COUT_DE_NIVEAU = tabuler(RATIO_COUT_NIVEAU, NIVEAUX_TABULES)

export function puissanceDeG(exposant: number): Decimal {
  return PUISSANCES_DE_G[exposant] ?? Decimal.pow(G_COUT_PALIER, exposant)
}

export function puissanceDeD(exposant: number): Decimal {
  return PUISSANCES_DE_D[exposant] ?? Decimal.pow(D_PRODUCTION_PAR_PALIER, exposant)
}

export function puissanceDuCoutDeNiveau(exposant: number): Decimal {
  return PUISSANCES_DU_COUT_DE_NIVEAU[exposant] ?? Decimal.pow(RATIO_COUT_NIVEAU, exposant)
}
