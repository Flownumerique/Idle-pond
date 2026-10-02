/**
 * IdlePond — le registre des bénédictions.
 *
 * Contenu pur, engendré depuis les espèces : une ciblée par espèce, une
 * globale. Aucune valeur ici — les graines vivent dans `constantes.ts`, les
 * formules dans `economie.ts`. Les identifiants entrent dans les saves : figés.
 */
import type { Benediction, BenedictionId, EspeceId } from '../noyau/types'
import { ESPECES } from './especes'

export const BENEDICTION_GLOBALE_ID = 'benediction-globale'

function construire(): readonly Benediction[] {
  const globale: Benediction = { id: BENEDICTION_GLOBALE_ID, portee: 'globale', espece: null }
  const ciblees = ESPECES.map<Benediction>((espece) => ({
    id: `benediction-${espece.id}`,
    portee: 'ciblee',
    espece: espece.id,
  }))
  return [globale, ...ciblees]
}

export const BENEDICTIONS: readonly Benediction[] = construire()

export function benedictionParId(id: BenedictionId): Benediction | undefined {
  return BENEDICTIONS.find((b) => b.id === id)
}

/** La ciblée d'une espèce. Lance si l'espèce n'existe pas : le registre est engendré depuis elles. */
export function benedictionCibleeDe(espece: EspeceId): Benediction {
  const ciblee = BENEDICTIONS.find((b) => b.portee === 'ciblee' && b.espece === espece)
  if (ciblee === undefined) throw new Error(`Aucune bénédiction ciblée pour l'espèce ${espece}`)
  return ciblee
}
