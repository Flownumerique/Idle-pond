/**
 * IdlePond — les six assises.
 *
 * Contenu pur, sans logique.
 *
 * L'assise I est nommée par l'amendement v1.1 §2.E : `la Noue`, /nu/,
 * hydronyme réel désignant une dépression humide, monosyllabe. Identifiant
 * `noue`. Les cinq autres attendent encore la charte phonétique — leur
 * identifiant reste neutre, et rien de générique ne s'affiche à l'écran (§3).
 *
 * [P] — répartition des paliers. Le §5.1 fixe 62 paliers en « distribution
 * plate (~4,4 par cycle) » et le §6 donne six paliers à la Noue. Les deux ne
 * se tiennent que si « plate » qualifie la distribution PAR CYCLE et non par
 * assise — sinon il en faudrait ~10,3 chacune. C'est la lecture retenue : la
 * Noue est courte parce qu'elle enseigne, les 56 paliers restants se
 * répartissent sur cinq assises. À confirmer.
 */
import type { Assise } from '../noyau/types'
import { NOMBRE_DE_PALIERS } from '../noyau/constantes'

/** Nommées au fur et à mesure que la charte phonétique descend (§2.E). */
const IDENTIFIANTS_D_ASSISE: readonly (string | undefined)[] = ['noue']

const PALIERS_PAR_ASSISE: readonly number[] = [6, 11, 11, 11, 11, 12]

function construireAssises(): readonly Assise[] {
  const assises: Assise[] = []
  let index = 0
  for (let rang = 0; rang < PALIERS_PAR_ASSISE.length; rang += 1) {
    const nombreDePaliers = PALIERS_PAR_ASSISE[rang]
    assises.push({
      id: IDENTIFIANTS_D_ASSISE[rang] ?? `assise-${rang + 1}`,
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
