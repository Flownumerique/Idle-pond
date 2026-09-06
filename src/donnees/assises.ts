/**
 * IdlePond — les six assises.
 *
 * Contenu pur, sans logique.
 *
 * [P] P3 — aucun nom propre ici. Les conventions phonétiques ne sont pas
 * tranchées, et le §14 les déclare bloquantes pour tout nom d'assise, d'espèce
 * et de bâtiment. Les identifiants ci-dessous sont des identifiants de code ;
 * l'UI n'affichera jamais ni eux ni un nom générique de couche (§3), elle
 * affichera le nom propre du lieu quand il existera.
 *
 * [P] — la répartition des paliers entre assises n'est pas fixée par le
 * contrat au-delà de « 62, distribution plate ». Celle-ci est plate à un
 * palier près et sera relue au premier jalon de contenu.
 */
import type { Assise } from '../noyau/types'
import { NOMBRE_DE_PALIERS } from '../noyau/constantes'

const PALIERS_PAR_ASSISE: readonly number[] = [10, 10, 10, 11, 11, 10]

function construireAssises(): readonly Assise[] {
  const assises: Assise[] = []
  let index = 0
  for (let rang = 0; rang < PALIERS_PAR_ASSISE.length; rang += 1) {
    const nombreDePaliers = PALIERS_PAR_ASSISE[rang]
    assises.push({
      id: `assise-${rang + 1}`,
      rang: rang + 1,
      // Chaque assise a son type de mana propre. [P] mana-typologie.md (Tier 1)
      // n'est pas disponible dans le dépôt : les identifiants sont provisoires.
      typeMana: `type-mana-${rang + 1}`,
      indexPremierPalier: index,
      nombreDePaliers,
    })
    index += nombreDePaliers
  }
  if (index !== NOMBRE_DE_PALIERS) {
    throw new Error(`Distribution des paliers incohérente : ${index} au lieu de ${NOMBRE_DE_PALIERS}`)
  }
  return assises
}

export const ASSISES: readonly Assise[] = construireAssises()

export function assiseDuPalier(index: number): Assise {
  const assise = ASSISES.find(
    (a) => index >= a.indexPremierPalier && index < a.indexPremierPalier + a.nombreDePaliers,
  )
  if (assise === undefined) throw new Error(`Palier hors des assises : ${index}`)
  return assise
}

export const TYPE_MANA_NATAL = ASSISES[0].typeMana

/**
 * Ce que le jalon v0.2 livre réellement : l'assise I, et elle seule.
 *
 * « Règle d'engagement : aucune assise n'est produite avant que la précédente
 * ait été mesurée. On coupe au milieu, jamais à la fin » (§12). Les 62 paliers
 * existent dans la donnée parce que c'est l'économie que le simulateur mesure ;
 * le jeu, lui, s'arrête où le contenu s'arrête.
 */
export const PALIERS_LIVRES = ASSISES[0].nombreDePaliers
