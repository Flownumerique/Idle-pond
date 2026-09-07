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
  EXPOSANT_REPEUPLEMENT_DENSITE,
  K_TAUX_DE_REPEUPLEMENT,
  PRODUCTION_DE_REFERENCE,
  densiteExposant,
} from './constantes'
import { facteurDeTechnique } from './technique'

export function densiteDuPalier(etat: EtatJeu, palier: IndexPalier): number {
  return etat.permanent.densites[palier] ?? 0
}

/**
 * Multiplicateur de densité : `densité ^ (θ/α)` (amendement v1.1 §2.A).
 *
 * Il ne multiplie pas une production. Il raccourcit le temps caractéristique du
 * séjour (§2.B) : c'est la traduction mécanique de « séjour en mana DENSE ».
 *
 * Planché à 1 : une densité nulle ne doit pas ralentir le séjour au-delà de
 * `τ₀`, qui est déjà le cas neutre.
 */
export function multiplicateurDensite(densite: number): number {
  return Math.pow(Math.max(1, densite), densiteExposant())
}

/**
 * Vitesse de repeuplement d'un banc, par seconde.
 *
 * [P] — le §6.5 de la v1.0 dit que le gain de densité « retourne dans la
 * vitesse de repeuplement » ; l'amendement v1.1 §2.B la fait retourner dans
 * l'acquis de séjour. Les deux canaux coexistent dans les documents et aucun
 * n'annule l'autre. Celui-ci porte donc un exposant NOMMÉ et volontairement
 * doux, distinct de `θ/α` : appliquer le multiplicateur plein des deux côtés
 * compterait deux fois la même compensation. À trancher en v0.3.
 */
export function vitesseDeRepeuplement(etat: EtatJeu, palier: IndexPalier): number {
  const densite = densiteDuPalier(etat, palier)
  return K_TAUX_DE_REPEUPLEMENT * Math.pow(1 + densite, EXPOSANT_REPEUPLEMENT_DENSITE)
}

/**
 * Densité qu'un cycle laisse derrière lui : `pointe ^ α` (§2.A, étape 2).
 *
 * C'est de là que part toute la chaîne : la pointe étant multipliée par
 * `g^paliers` à chaque éclosion, la densité l'est par `g^(paliers × α)`, et le
 * multiplicateur par `g^(paliers × θ)`.
 */
export function densiteLaisseeParLeCycle(productionDePic: Decimal): number {
  const rapport = productionDePic.div(PRODUCTION_DE_REFERENCE)
  if (rapport.lte(0)) return 0
  return Math.pow(rapport.toNumber(), ALPHA_GAIN_DE_DENSITE)
}

/**
 * Porte la densité des paliers occupés au niveau que le cycle a laissé.
 *
 * `max`, jamais une affectation : la densité ne redescend JAMAIS (Tier 0). Un
 * cycle plus court que le précédent laisse moins de charge derrière lui, et ne
 * doit pas pouvoir défaire ce qui a été acquis.
 */
export function appliquerGainDeDensite(
  etat: EtatJeu,
  paliersOuverts: number,
  productionDePic: Decimal,
): readonly number[] {
  const conservation = facteurDeTechnique(etat, 'densite_conservee')
  const laissee = densiteLaisseeParLeCycle(productionDePic) * conservation
  if (!(laissee > 0)) return etat.permanent.densites
  return etat.permanent.densites.map((d, index) => (index < paliersOuverts ? Math.max(d, laissee) : d))
}
