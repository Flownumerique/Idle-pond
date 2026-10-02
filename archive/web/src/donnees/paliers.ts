/**
 * IdlePond — les 62 paliers, et l'espèce que chacun ouvre.
 *
 * Un palier porte au plus UNE espèce, et deux paliers sur trois n'en portent
 * aucune : le noyau v1.0 §1.3 ancre une espèce tous les trois paliers à partir
 * du premier de l'assise. Ce qu'un palier sans espèce apporte n'est pas rien —
 * c'est le multiplicateur de profondeur, qui porte `D` là où le bestiaire ne le
 * porte pas.
 *
 * Le banc a disparu avec le modèle à population : il n'y a plus d'unité
 * intermédiaire entre l'espèce et le palier, donc plus de liste par palier.
 */
import type { Espece, EspeceId, IndexPalier, Palier } from '../noyau/types'
import { NOMBRE_DE_PALIERS } from '../noyau/constantes'
import { ASSISES } from './assises'
import { ESPECES, especeParId } from './especes'

function construirePaliers(): readonly Palier[] {
  const paliers: Palier[] = []
  for (const assise of ASSISES) {
    for (let local = 0; local < assise.nombreDePaliers; local += 1) {
      const index = assise.indexPremierPalier + local
      const espece = ESPECES.find((e) => e.palier === index)
      paliers.push({ index, assise: assise.id, espece: espece?.id ?? null })
    }
  }
  if (paliers.length !== NOMBRE_DE_PALIERS) {
    throw new Error(`Compte de paliers incohérent : ${paliers.length} au lieu de ${NOMBRE_DE_PALIERS}`)
  }
  return paliers
}

export const PALIERS: readonly Palier[] = construirePaliers()

/** L'espèce qu'ouvre ce palier, s'il en ouvre une. */
export function especeDuPalier(index: IndexPalier): Espece | undefined {
  const id: EspeceId | null = PALIERS[index]?.espece ?? null
  return id === null ? undefined : especeParId(id)
}
