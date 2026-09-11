/**
 * IdlePond — densité.
 *
 * Tier 0, invariant : la densité ne redescend pas. Elle est monotone croissante
 * par palier et survit à l'éclosion. Rien dans ce module ne doit pouvoir la
 * faire baisser, et le test de canon parcourt les 15 cycles du simulateur pour
 * s'en assurer.
 *
 * §6.5 : le gain de densité est indexé sur la production de pic du cycle, pas
 * sur la profondeur. Elle a deux débouchés, tous deux via `multiplicateurDensite` :
 * l'acquis de séjour — « séjour en mana dense » (Tier 0 §8) — et, depuis la
 * tâche 9, la production elle-même (§10). Les deux usages appellent la MÊME
 * fonction avec la MÊME grandeur, `densiteDuSejour` ci-dessous : voir son
 * commentaire pour ce qui interdit qu'une autre grandeur porte ce nom.
 *
 * `vitesseDeRepeuplement` est partie avec le modèle à population le 2026-09-09.
 * V11 avait déjà découplé la densité du repeuplement ; il ne restait plus qu'un
 * `k` constant, et il n'y a plus de population à repeupler.
 */
import type Decimal from 'break_infinity.js'
import type { EtatJeu, IndexPalier } from './types'
import {
  ALPHA_GAIN_DE_DENSITE,
  DENSITE_DE_REFERENCE,
  PRODUCTION_DE_REFERENCE,
  densiteExposant,
} from './constantes'
import { facteurDeTechnique } from './technique'

export function densiteDuPalier(etat: EtatJeu, palier: IndexPalier): number {
  return etat.permanent.densites[palier] ?? 0
}

/**
 * Densité du séjour : la plus dense des eaux où le héros se tient — le
 * maximum sur les paliers OUVERTS, pas une somme.
 *
 * [P] — le §2.A écrit `multiplicateurDensite(s)` pour l'état entier, alors que
 * la densité est portée par palier. Le maximum sur les paliers ouverts est
 * retenu : c'est celle qu'il peut effectivement habiter. En pratique la
 * question est peu sensible — l'éclosion porte tous les paliers occupés à la
 * même valeur —, mais elle le deviendrait si une assise cessait d'être
 * revisitée à chaque vie.
 *
 * C'EST LA SEULE GRANDEUR NOMMÉE « densité » qui doit nourrir
 * `multiplicateurDensite`, pour le séjour COMME pour la production : la
 * dérivation de `densiteExposant` (`constantes.ts`) suppose que son argument
 * EST la densité, la grandeur qui vaut `pointe^α` — un scalaire, jamais une
 * somme. Une SOMME sur les paliers ouverts croît aussi avec leur NOMBRE, et
 * glisse un `(p_new/p_old)^(θ/α)` non budgété sur le `g^(paliers × θ)` voulu à
 * chaque éclosion — mesuré : environ ×18 sur une partie complète.
 */
export function densiteDuSejour(etat: EtatJeu): number {
  let densite = 0
  for (let palier = 0; palier < etat.cycle.paliersOuverts; palier += 1) {
    densite = Math.max(densite, densiteDuPalier(etat, palier))
  }
  return densite
}

/**
 * Multiplicateur de densité : `(1 + densité / d₀) ^ (θ/α)` (amendement v1.1
 * §2.A, forme des contraintes globales du plan).
 *
 * Il raccourcit le temps caractéristique du séjour (§2.B) — c'est la
 * traduction mécanique de « séjour en mana DENSE » — et, depuis la tâche 9, il
 * multiplie aussi la production (§10) au même titre que le multiplicateur de
 * profondeur : les deux sont des TermeDeFormule nommés dans le détail de
 * captation, jamais des facteurs flottants (§7.5 règle 3).
 *
 * À densité nulle il vaut exactement 1 : une eau neutre ne raccourcit ni ne
 * rallonge le séjour au-delà de `τ₀`, qui est déjà le cas neutre.
 */
export function multiplicateurDensite(densite: number): number {
  return Math.pow(1 + densite / DENSITE_DE_REFERENCE, densiteExposant())
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
