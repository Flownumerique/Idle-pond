/**
 * IdlePond — mise en forme pour l'écran.
 *
 * Règle d'UI absolue (§3) : l'interface n'affiche JAMAIS un nom générique de
 * couche. Pas de « Zone 3 », pas de « Assise II » — et pas davantage
 * « palier 4 », puisque `assise` et `palier` sont des termes de code et de GDD,
 * pas d'écran.
 *
 * Ce qui s'affiche à la place : le nom propre du lieu, et une PROFONDEUR. Une
 * mesure n'est pas un nom de couche.
 */
import type Decimal from 'break_infinity.js'
import type { AssiseId, EspeceId, SourceDeTerme } from '../noyau/types'
import { NOM_DES_ASSISES, NOM_DES_ESPECES } from '../donnees/textes-provisoires'

const SUFFIXES = ['', ' k', ' M', ' G', ' T', ' P', ' E'] as const

/** Un montant de mana ou de Foi, lisible d'un coup d'œil. */
export function montant(valeur: Decimal): string {
  const nombre = valeur.toNumber()
  if (!Number.isFinite(nombre)) return valeur.toExponential(2)
  if (nombre < 1000) {
    if (Number.isInteger(nombre)) return nombre.toString()
    return nombre < 10 ? nombre.toFixed(1) : Math.floor(nombre).toString()
  }

  let reste = nombre
  let rang = 0
  while (reste >= 1000 && rang < SUFFIXES.length - 1) {
    reste /= 1000
    rang += 1
  }
  if (rang === SUFFIXES.length - 1 && reste >= 1000) return valeur.toExponential(2)
  return `${reste.toFixed(reste < 10 ? 2 : 1)}${SUFFIXES[rang]}`
}

/**
 * Un coût, arrondi VERS LE HAUT.
 *
 * Un coût de 58,8 affiché « 58 » à côté d'une bourse de 58 donne un bouton
 * grisé sans raison visible. Sur un montant produit l'arrondi par défaut est
 * honnête ; sur un montant à payer, il ment.
 */
export function cout(valeur: Decimal): string {
  const nombre = valeur.toNumber()
  if (Number.isFinite(nombre) && nombre < 1000) {
    return nombre < 10 ? nombre.toFixed(1) : Math.ceil(nombre).toString()
  }
  return montant(valeur)
}

export function entier(valeur: number): string {
  return Math.floor(valeur).toLocaleString('fr-FR')
}

export function duree(secondes: number): string {
  if (!Number.isFinite(secondes) || secondes < 0) return '—'
  if (secondes < 90) return `${Math.round(secondes)} s`
  if (secondes < 5400) return `${Math.round(secondes / 60)} min`
  const heures = secondes / 3600
  return heures < 24 ? `${heures.toFixed(1)} h` : `${Math.round(heures / 24)} j`
}

/**
 * La profondeur d'un banc, en brasses. Une mesure, pas un nom de couche : le
 * premier creux est à zéro brasse, on descend d'une brasse par creusement.
 */
export function profondeur(palier: number): string {
  return palier === 0 ? 'à fleur d’eau' : `${palier} brasse${palier > 1 ? 's' : ''}`
}

export function nomDeLAssise(assise: AssiseId): string {
  return NOM_DES_ASSISES[assise] ?? 'plus bas'
}

/** Le même nom, en tête de phrase. */
export function nomDeLAssiseCapitale(assise: AssiseId): string {
  const nom = nomDeLAssise(assise)
  return nom.charAt(0).toUpperCase() + nom.slice(1)
}

export function nomDeLEspece(espece: EspeceId): string {
  return NOM_DES_ESPECES[espece] ?? 'un banc sans nom'
}

/** La source d'un terme, mise en mots ici et pas dans le noyau. */
export function sourceDuTerme(source: SourceDeTerme): string {
  switch (source.quoi) {
    case 'population':
      return 'ce qui vit là'
    case 'palier':
      return profondeur(source.palier)
    case 'acclimatation':
      return 'ce que tu supportes'
    case 'place':
      return `${source.place} places faites`
    case 'drapeaux_permanents':
      return source.especes === 0
        ? 'aucune espèce au complet'
        : `${source.especes} espèce${source.especes > 1 ? 's' : ''} déjà au complet`
    case 'benedictions_globales':
      return 'bénédictions, toutes espèces'
    case 'benedictions_ciblees':
      return 'bénédictions, cette espèce'
  }
}
