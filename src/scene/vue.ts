/**
 * IdlePond — la vue de la scène : ce que Phaser dessine, sans Phaser.
 *
 * Projection PURE de l'état (spec 2026-09-17 [D9]). La scène ne lit jamais
 * `EtatJeu` : elle reçoit cette structure, sérialisable, et la dessine. C'est
 * ce qui rend la scène testable en node par ses données, et remplaçable par
 * de vrais sprites sans toucher au noyau.
 *
 * Même contrainte de pureté que le noyau : aucun import hors `noyau/`,
 * `donnees/` et `scene/`. `tests/scene.test.ts` vérifie l'absence de Phaser.
 */
import type { AssiseId, EspeceId, EtatJeu } from '../noyau/types'
import { eauTroublee, estSature } from '../noyau/economie'
import { PALIERS } from '../donnees/paliers'
import { ESPECES } from '../donnees/especes'
import { assiseDuPalier } from '../donnees/assises'

export interface VueDEspece {
  readonly id: EspeceId
  readonly rang: number
  readonly niveau: number
}

export interface VueDePalier {
  readonly index: number
  readonly assise: AssiseId
  /** 1 à 6. */
  readonly rangDAssise: number
  /** L'espèce débloquée que ce palier porte, ou rien. */
  readonly espece: VueDEspece | null
}

export interface VueDuHeros {
  readonly niveau: number
  /** Facteur de taille du corps — spec [D12]. */
  readonly echelle: number
  readonly couches: readonly AssiseId[]
}

export interface VueDeScene {
  readonly paliers: readonly VueDePalier[]
  readonly heros: VueDuHeros
  readonly eauTroublee: boolean
  readonly sature: boolean
}

/** `1 + 0,25 · log₂(niveau)` : ×1 au niveau 1, ×2 à 16, ×3 à 256. */
export function echelleDuHeros(niveau: number): number {
  return 1 + 0.25 * Math.log2(Math.max(1, niveau))
}

export function vueDeLaScene(etat: EtatJeu): VueDeScene {
  const paliers: VueDePalier[] = []
  for (let index = 0; index < etat.cycle.paliersOuverts; index += 1) {
    const palier = PALIERS[index]
    const assise = assiseDuPalier(index)
    let espece: VueDEspece | null = null
    if (palier.espece !== null) {
      const vivante = etat.cycle.especes[palier.espece]
      const definition = ESPECES.find((e) => e.id === palier.espece)
      if (vivante?.debloquee === true && definition !== undefined) {
        espece = { id: definition.id, rang: definition.rang, niveau: vivante.niveau }
      }
    }
    paliers.push({ index, assise: assise.id, rangDAssise: assise.rang, espece })
  }
  return {
    paliers,
    heros: {
      niveau: etat.cycle.niveauDuHeros,
      echelle: echelleDuHeros(etat.cycle.niveauDuHeros),
      couches: etat.permanent.couches,
    },
    eauTroublee: eauTroublee(etat),
    sature: estSature(etat),
  }
}
