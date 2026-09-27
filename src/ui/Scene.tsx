/**
 * Monte Phaser dans React et lui pousse la vue.
 *
 * Un seul `Phaser.Game` par montage, détruit au démontage. La vue est
 * recalculée à chaque changement d'état — dix fois par seconde — et c'est la
 * scène qui décide si elle redessine (elle compare la vue sérialisée). Ce
 * composant ne contient aucune règle de jeu : `vueDeLaScene` est pure.
 */
import { useEffect, useRef } from 'react'
import Phaser from 'phaser'
import type { EtatJeu } from '../noyau/types'
import { vueDeLaScene } from '../scene/vue'
import { EVENEMENT_VUE, SceneDeLaMare } from '../scene/SceneDeLaMare'

export function Scene({ etat }: { readonly etat: EtatJeu }) {
  const conteneur = useRef<HTMLDivElement>(null)
  const jeu = useRef<Phaser.Game | null>(null)

  useEffect(() => {
    if (conteneur.current === null || jeu.current !== null) return
    jeu.current = new Phaser.Game({
      type: Phaser.AUTO,
      parent: conteneur.current,
      transparent: true,
      scale: { mode: Phaser.Scale.RESIZE, width: '100%', height: '100%' },
      scene: [SceneDeLaMare],
    })
    return () => {
      jeu.current?.destroy(true)
      jeu.current = null
    }
  }, [])

  useEffect(() => {
    jeu.current?.events.emit(EVENEMENT_VUE, vueDeLaScene(etat))
  }, [etat])

  return <div ref={conteneur} className="h-72 w-full overflow-hidden rounded-lg border border-eau-bord bg-eau-abysse" />
}
