/**
 * IdlePond — horloge. LE SEUL module autorisé à lire l'heure système (§10).
 *
 * Deux règles, et la seconde est la seule protection anti-triche dont le jeu
 * ait besoin :
 *   - le recul d'horloge est ignoré ;
 *   - l'avance ne crédite que du temps hors ligne plafonné, et le compteur
 *     Entretien ne lit que les heures CRÉDITÉES, jamais le temps écoulé. Sans
 *     ça, avancer son horloge farme l'arbre permanent.
 *
 * On ne punit jamais l'absence : le plafond borne ce que l'absence rapporte,
 * il ne retire rien.
 */
import { CAP_HORS_LIGNE_HEURES_INITIAL, CAP_HORS_LIGNE_HEURES_MAXIMUM } from '../noyau/constantes'

export interface Horloge {
  maintenantMs(): number
}

export const horlogeSysteme: Horloge = {
  maintenantMs: () => Date.now(),
}

/** Horloge déterministe pour les tests et le simulateur. */
export function horlogeFigee(depart: number): Horloge & { avancerMs(delta: number): void } {
  let instant = depart
  return {
    maintenantMs: () => instant,
    avancerMs: (delta: number) => {
      instant += delta
    },
  }
}

export function capHorsLigneSecondes(heures: number): number {
  const borne = Math.min(Math.max(heures, CAP_HORS_LIGNE_HEURES_INITIAL), CAP_HORS_LIGNE_HEURES_MAXIMUM)
  return borne * 3600
}

/**
 * Secondes hors ligne effectivement créditées entre deux instants.
 * Rendues telles quelles au noyau, qui les absorbe en UN SEUL appel à tick —
 * c'est précisément ce que garantit le filtre du §5.2.
 */
export function secondesHorsLigneCreditees(
  dernierInstantMs: number,
  maintenantMs: number,
  capHeures: number,
): number {
  const ecoule = maintenantMs - dernierInstantMs
  if (!(ecoule > 0)) return 0
  return Math.min(ecoule / 1000, capHorsLigneSecondes(capHeures))
}
