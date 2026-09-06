/**
 * Les annonces de succès.
 *
 * §8.2 : « Notification discrète et non bloquante — une ligne qui apparaît et
 * s'efface. JAMAIS une fenêtre. » Rien ici ne prend le focus, rien n'attend un
 * clic, rien n'interrompt. Un joueur absent ne rate rien : le détail reste dans
 * l'écran des succès.
 */
import { useEffect } from 'react'
import type { SuccesId } from '../noyau/types'
import { texteDuSucces } from '../donnees/textes-provisoires'

interface Props {
  readonly annonces: readonly SuccesId[]
  readonly surExpiration: (id: SuccesId) => void
}

const DUREE_MS = 6000

export function Annonces({ annonces, surExpiration }: Props) {
  useEffect(() => {
    if (annonces.length === 0) return
    const minuteries = annonces.map((id) => setTimeout(() => surExpiration(id), DUREE_MS))
    return () => minuteries.forEach(clearTimeout)
  }, [annonces, surExpiration])

  return (
    <div
      aria-live="polite"
      className="pointer-events-none fixed inset-x-0 bottom-4 z-10 flex flex-col items-center gap-1 px-4"
    >
      {annonces.slice(-3).map((id) => (
        <p
          key={id}
          className="annonce rounded-full border border-eau-clair bg-eau-fond/95 px-4 py-1.5 font-texte text-sm text-jour shadow-lg"
        >
          {texteDuSucces(id).rapport}
        </p>
      ))}
    </div>
  )
}
