/**
 * Ce qui s'est passé pendant l'absence.
 *
 * On ne punit jamais l'absence, et on ne la fête pas non plus : la ligne dit ce
 * qui a été crédité, puis s'en va. Le rapport détaillé est un nœud verbe de la
 * branche Entretien (§7.3), donc v0.4 — pas ici.
 */
import type { RetourAffiche } from '../etat/magasin'
import { duree } from './format'

interface Props {
  readonly retour: RetourAffiche | null
  readonly surFermeture: () => void
}

export function Retour({ retour, surFermeture }: Props) {
  if (retour === null) return null
  return (
    <button
      type="button"
      onClick={surFermeture}
      className="w-full rounded-lg border border-eau-bord bg-eau-fond/50 px-3 py-2 text-left text-sm text-jour-doux transition-colors hover:border-eau-clair"
    >
      La mare a tourné sans toi pendant {duree(retour.secondesCreditees)}.
    </button>
  )
}
