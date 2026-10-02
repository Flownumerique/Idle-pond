/**
 * IdlePond — la boucle de jeu.
 *
 * Porté du `GameLoopManager` de l'ancien projet, que le §9 range dans ce qui se
 * garde. Ce qui change : plus de singleton, plus d'état de module, plus de
 * logique métier. Elle lit l'heure, appelle `tick`, écrit l'état. C'est tout —
 * tout le reste est dans le noyau, où c'est testable.
 *
 * Le jeu appelle tick à 100 ms ; le simulateur avec 60 s ou 8 h. Un seul code.
 */
import { PERIODE_DE_TICK_MS } from '../noyau/constantes'
import type { EtatJeu, SuccesId } from '../noyau/types'
import { tickDetaille } from '../noyau/noyau'
import { enregistrerIntervalleDeSucces } from '../noyau/succes'
import { horlogeSysteme, type Horloge } from './horloge'

export interface OptionsDeBoucle {
  readonly lire: () => EtatJeu
  readonly ecrire: (etat: EtatJeu) => void
  readonly surSucces?: (declenches: readonly SuccesId[]) => void
  readonly horloge?: Horloge
}

export interface Boucle {
  demarrer(): void
  arreter(): void
}

export function creerBoucle(options: OptionsDeBoucle): Boucle {
  const horloge = options.horloge ?? horlogeSysteme
  let minuterie: ReturnType<typeof setInterval> | null = null
  let dernierInstantMs = horloge.maintenantMs()

  const pas = () => {
    const maintenant = horloge.maintenantMs()
    const dt = (maintenant - dernierInstantMs) / 1000
    dernierInstantMs = maintenant
    // Recul d'horloge : ignoré, jamais rattrapé à l'envers.
    if (!(dt > 0)) return

    const resultat = tickDetaille(options.lire(), dt)
    // L'intervalle entre deux succès est une observation, faite ici, à la
    // cadence où elle est faite — pas une mécanique du noyau (§11).
    options.ecrire(enregistrerIntervalleDeSucces(resultat.etat, resultat.declenches))
    if (resultat.declenches.length > 0) options.surSucces?.(resultat.declenches)
  }

  return {
    demarrer() {
      if (minuterie !== null) return
      dernierInstantMs = horloge.maintenantMs()
      minuterie = setInterval(pas, PERIODE_DE_TICK_MS)
    },
    arreter() {
      if (minuterie === null) return
      clearInterval(minuterie)
      minuterie = null
    },
  }
}
