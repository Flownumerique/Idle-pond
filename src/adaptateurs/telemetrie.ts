/**
 * IdlePond — télémétrie.
 *
 * §11 : instrumentée dès le jalon v0.1. Elle est un OBJET DE MESURE, pas de la
 * finition — c'est elle qui réfute `α`, l'exposant de densité, les couples
 * (A, B) et le rapport g/D. Ce qui n'est pas mesuré ne peut pas être calibré,
 * et un paramètre non calibré est un paramètre inventé.
 *
 * `tauDeRepeuplementSecondes` est parti avec le modèle à population : il n'y a
 * plus de population à repeupler, donc plus de `k` à mesurer.
 *
 * Les métriques dérivables de l'état sont relevées ici, purement. Celles qui
 * demandent l'heure (temps calendaire, intervalle réel entre deux sessions)
 * passent par l'horloge, et c'est pour cela que ce module est un adaptateur.
 */
import type { BrancheTechniqueId, EtatJeu } from '../noyau/types'
import { pointsDeBranche } from '../noyau/technique'
import { REGIME_PAR_BRANCHE } from '../noyau/technique'

export interface ReleveDeCycle {
  readonly index: number
  readonly dureeEcouleeSecondes: number
  readonly paliersOuverts: number
  readonly productionPicParSeconde: number
  readonly souffleGagne: number
}

export interface Releve {
  readonly nombreEclosions: number
  readonly cycles: readonly ReleveDeCycle[]
  /** Détecte l'assèchement de mi-partie. */
  readonly intervalleMoyenEntreSuccesSecondes: number | null
  readonly pointsDeTechniqueRendus: Readonly<Record<BrancheTechniqueId, number>>
  /**
   * Temps de jeu ÉCOULÉ — le temps calendaire du §11, pas le temps actif.
   * Le temps actif se mesure du côté des politiques, pas ici : le noyau ne sait
   * pas quand quelqu'un regarde.
   */
  readonly tempsEcouleSecondes: number
}

export function relever(etat: EtatJeu): Releve {
  const branches = Object.keys(REGIME_PAR_BRANCHE) as BrancheTechniqueId[]
  const pointsDeTechniqueRendus = Object.fromEntries(
    branches.map((b) => [b, pointsDeBranche(b, etat.permanent.compteursTechnique[b] ?? 0)]),
  ) as Record<BrancheTechniqueId, number>

  const intervalles = etat.telemetrie.intervallesEntreSucces
  return {
    nombreEclosions: etat.permanent.nombreEclosions,
    cycles: etat.telemetrie.cycles.map((c) => ({
      index: c.index,
      dureeEcouleeSecondes: c.dureeEcouleeSecondes,
      paliersOuverts: c.paliersOuverts,
      productionPicParSeconde: c.productionPicParSeconde.toNumber(),
      souffleGagne: c.souffleGagne.toNumber(),
    })),
    intervalleMoyenEntreSuccesSecondes:
      intervalles.length > 0 ? intervalles.reduce((a, b) => a + b, 0) / intervalles.length : null,
    pointsDeTechniqueRendus,
    tempsEcouleSecondes: etat.tempsJeuSecondes,
  }
}

/** Un puits de télémétrie. L'implémentation réseau viendra ; le contrat non. */
export interface Collecteur {
  publier(releve: Releve): void
}

export const collecteurSilencieux: Collecteur = {
  publier: () => undefined,
}
