/**
 * IdlePond — les espèces de base.
 *
 * ~21 espèces réparties sur six assises (§13.1). Les divergences (~6) sont du
 * contenu v0.5 et ne sont pas déclarées ici.
 *
 * [P] P3 — aucun nom, aucune description, aucun trait. Le placement et la
 * sélection des espèces sont fixés par l'auteur (§4.2) : le tableau ci-dessous
 * ne fixe que le COMPTE par assise, qui est une valeur du §13.1, et laisse
 * l'attribution nominative entièrement ouverte.
 */
import type { Espece } from '../noyau/types'
import { NOMBRE_D_ESPECES_DE_BASE } from '../noyau/constantes'
import { ASSISES } from './assises'

/** 3 espèces sur l'assise I : c'est le contenu du jalon v0.2 (§12). */
const ESPECES_PAR_ASSISE: readonly number[] = [3, 3, 4, 4, 4, 3]

function construireEspeces(): readonly Espece[] {
  const especes: Espece[] = []
  ASSISES.forEach((assise, rang) => {
    for (let i = 0; i < ESPECES_PAR_ASSISE[rang]; i += 1) {
      especes.push({ id: `espece-${assise.rang}-${i + 1}`, assise: assise.id })
    }
  })
  if (especes.length !== NOMBRE_D_ESPECES_DE_BASE) {
    throw new Error(`Compte d'espèces incohérent : ${especes.length} au lieu de ${NOMBRE_D_ESPECES_DE_BASE}`)
  }
  return especes
}

export const ESPECES: readonly Espece[] = construireEspeces()

export function especesDeLAssise(assiseId: string): readonly Espece[] {
  return ESPECES.filter((e) => e.assise === assiseId)
}
