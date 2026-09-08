/**
 * IdlePond — télémétrie.
 *
 * §11 : instrumentée dès le jalon v0.1. Elle est un OBJET DE MESURE, pas de la
 * finition — c'est elle qui réfute `α`, l'exposant de densité, `k`, les couples
 * (A, B) et le rapport g/D. Ce qui n'est pas mesuré ne peut pas être calibré,
 * et un paramètre non calibré est un paramètre inventé.
 *
 * Les métriques dérivables de l'état sont relevées ici, purement. Celles qui
 * demandent l'heure (temps calendaire, intervalle réel entre deux sessions)
 * passent par l'horloge, et c'est pour cela que ce module est un adaptateur.
 */
import type { BrancheTechniqueId, EtatJeu } from '../noyau/types'
import { pointsDeBranche } from '../noyau/technique'
import { REGIME_PAR_BRANCHE } from '../noyau/technique'
import { vitesseDeRepeuplement } from '../noyau/densite'

export interface ReleveDeCycle {
  readonly index: number
  readonly dureeEcouleeSecondes: number
  /** Risque n° 1 : « la redescente devient le jeu ». Métrique n° 1. */
  readonly fractionEnRedescente: number
  readonly paliersOuverts: number
  readonly productionPicParSeconde: number
  readonly foiGagnee: number
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
  /**
   * Temps caractéristique du repeuplement, en secondes, là où le héros se
   * tient. `k` est l'une des trois valeurs que le jalon v0.3 doit mesurer.
   *
   * Il est relevé parce qu'il DÉGÉNÈRE : la densité vaut `pointe^α` depuis
   * l'amendement v1.1 §2.A, donc elle croît avec la production, sans borne. La
   * coupler à `k` — canal du §6.5 de la v1.0 — rend le repeuplement quasi
   * instantané dès les premiers cycles, et la mécanique cesse d'exister. Le
   * §2.B, lui, fait passer la densité par un rapport que la saturation borne.
   * C'est la décision ouverte V11, et cette métrique est ce qui la tranchera.
   */
  readonly tauDeRepeuplementSecondes: number
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
      fractionEnRedescente:
        c.dureeEcouleeSecondes > 0 ? c.secondesEnRedescente / c.dureeEcouleeSecondes : 0,
      paliersOuverts: c.paliersOuverts,
      productionPicParSeconde: c.productionPicParSeconde.toNumber(),
      foiGagnee: c.foiGagnee.toNumber(),
    })),
    intervalleMoyenEntreSuccesSecondes:
      intervalles.length > 0 ? intervalles.reduce((a, b) => a + b, 0) / intervalles.length : null,
    pointsDeTechniqueRendus,
    tempsEcouleSecondes: etat.tempsJeuSecondes,
    tauDeRepeuplementSecondes: 1 / vitesseDeRepeuplement(),
  }
}

/** Un puits de télémétrie. L'implémentation réseau viendra ; le contrat non. */
export interface Collecteur {
  publier(releve: Releve): void
}

export const collecteurSilencieux: Collecteur = {
  publier: () => undefined,
}
