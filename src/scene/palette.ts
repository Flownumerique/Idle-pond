/**
 * IdlePond — palettes et marques, données pures de la scène.
 *
 * GDD §15.2 : « la lumière est la variable de progression la plus lisible.
 * Elle décroît continûment jusqu'à ce que la seule lumière restante soit celle
 * que le mana produit. » Six palettes, une par assise, la lumière qui baisse.
 *
 * GDD §15.1 : « un corps de base sur lequel s'ajoutent des marques, une par
 * assise fixée : branchies, membranes, luminescence, minéralisation,
 * épaississement. » Cinq marques nommées pour six assises : la sixième, le
 * halo, est un [P] — à confirmer par la DA.
 *
 * Aucun PNG : §15.3 bloque le premier sprite définitif sur l'anatomie du corps
 * de base, et §15.4 met l'imagerie de l'Étang des Merveilles hors registre.
 */
import type { AssiseId } from '../noyau/types'
import { ASSISES } from '../donnees/assises'

export interface PaletteDAssise {
  /** La roche derrière l'eau, 0xRRGGBB. */
  readonly fond: number
  /** L'eau elle-même. */
  readonly eau: number
  /** 0 à 1 — ce qu'il reste de jour. Décroît strictement en descendant. */
  readonly lumiere: number
}

export type MarqueId = 'branchies' | 'membranes' | 'luminescence' | 'mineralisation' | 'epaississement' | 'halo'

const PALETTES_PAR_RANG: readonly PaletteDAssise[] = [
  { fond: 0x24312b, eau: 0x2f5f5a, lumiere: 1.0 },
  { fond: 0x1d2a2c, eau: 0x244a52, lumiere: 0.75 },
  { fond: 0x17222a, eau: 0x1b3a4b, lumiere: 0.5 },
  { fond: 0x121a24, eau: 0x152b3d, lumiere: 0.32 },
  { fond: 0x1a1414, eau: 0x2a1c1c, lumiere: 0.18 },
  { fond: 0x0b0d14, eau: 0x0f1424, lumiere: 0.08 },
]

const MARQUES_PAR_RANG: readonly MarqueId[] = [
  'branchies',
  'membranes',
  'luminescence',
  'mineralisation',
  'epaississement',
  'halo',
]

export const PALETTES: Readonly<Record<AssiseId, PaletteDAssise>> = Object.fromEntries(
  ASSISES.map((a, i) => [a.id, PALETTES_PAR_RANG[i]]),
)

export const MARQUE_PAR_ASSISE: Readonly<Record<AssiseId, MarqueId>> = Object.fromEntries(
  ASSISES.map((a, i) => [a.id, MARQUES_PAR_RANG[i]]),
)

export function paletteDe(assise: AssiseId): PaletteDAssise {
  return PALETTES[assise] ?? PALETTES_PAR_RANG[PALETTES_PAR_RANG.length - 1]
}
