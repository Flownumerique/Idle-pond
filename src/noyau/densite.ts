/**
 * IdlePond — densité.
 *
 * Tier 0, invariant : la densité ne redescend pas. Elle est monotone croissante
 * par palier et survit à l'éclosion. Rien dans ce module ne doit pouvoir la
 * faire baisser, et le test de canon parcourt les 15 cycles du simulateur pour
 * s'en assurer.
 *
 * §6.5 : le gain de densité est indexé sur la production de pic du cycle, pas
 * sur la profondeur, et il retourne dans la vitesse de repeuplement.
 */
import type Decimal from 'break_infinity.js'
import type { EtatJeu, IndexPalier } from './types'
import {
  ALPHA_GAIN_DE_DENSITE,
  EXPOSANT_DE_DENSITE,
  K_TAUX_DE_REPEUPLEMENT,
  PRODUCTION_DE_REFERENCE,
} from './constantes'
import { facteurDeTechnique } from './technique'

export function densiteDuPalier(etat: EtatJeu, palier: IndexPalier): number {
  return etat.permanent.densites[palier] ?? 0
}

/**
 * Vitesse de repeuplement d'un banc, par seconde. C'est là que la densité
 * revient : plus la charge de mana d'un palier est haute, plus vite le banc
 * retrouve son effectif après une éclosion.
 */
export function vitesseDeRepeuplement(etat: EtatJeu, palier: IndexPalier): number {
  const densite = densiteDuPalier(etat, palier)
  return K_TAUX_DE_REPEUPLEMENT * Math.pow(1 + densite, EXPOSANT_DE_DENSITE)
}

/**
 * Gain de densité d'un cycle, indexé sur la production de pic.
 *
 * [P] graine — la forme logarithmique n'est fixée par aucun document. Elle est
 * retenue parce que la production croît géométriquement d'un cycle à l'autre :
 * un gain linéaire en pic exploserait, un gain logarithmique reste à peu près
 * proportionnel à la profondeur atteinte. À réfuter en v0.3.
 */
export function gainDeDensite(productionDePic: Decimal): number {
  const rapport = productionDePic.div(PRODUCTION_DE_REFERENCE)
  if (rapport.lte(0)) return 0
  return ALPHA_GAIN_DE_DENSITE * Math.log1p(rapport.toNumber())
}

/** Applique le gain aux paliers occupés pendant le cycle. Jamais décroissant. */
export function appliquerGainDeDensite(
  etat: EtatJeu,
  paliersOuverts: number,
  productionDePic: Decimal,
): readonly number[] {
  const conservation = facteurDeTechnique(etat, 'densite_conservee')
  const gain = gainDeDensite(productionDePic) * conservation
  if (!(gain > 0)) return etat.permanent.densites
  return etat.permanent.densites.map((d, index) => (index < paliersOuverts ? d + gain : d))
}
