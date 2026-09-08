/**
 * IdlePond — les espèces de base.
 *
 * ~21 espèces réparties sur six assises (§13.1). Les divergences (~6) sont du
 * contenu v0.5 et ne sont pas déclarées ici.
 *
 * Les trois espèces de la Noue sont nommées par l'amendement v1.1 §2.E —
 * noms réels du français d'eau douce, aucun qualificatif, aucune invention :
 *
 *   `vairon`    amorçage. Débit minuscule, population énorme. Le banc, la
 *               première chose qui accepte.
 *   `loche`     fouisseuse — le lien avec le creusement est gratuit.
 *   `epinoche`  dure, tient dans une eau qui se charge. Prépare la contrainte
 *               de l'assise II.
 *
 * `tanche` est RÉSERVÉE et ne doit être assignée à aucun générateur :
 * longévité, faible débit, très forte contenance, c'est le portrait du héros
 * (`especes-cadre.md` §2).
 *
 * [P] P3 — les dix-huit espèces plus profondes attendent la charte. Leur
 * identifiant reste neutre ; le placement reste autorial (§4.2).
 */
import type { Espece } from '../noyau/types'
import { NOMBRE_D_ESPECES_DE_BASE } from '../noyau/constantes'
import { ASSISES } from './assises'

/** 3 espèces sur l'assise I : c'est le contenu du jalon v0.2 (§12). */
const ESPECES_PAR_ASSISE: readonly number[] = [3, 3, 4, 4, 4, 3]

/** Réservé au héros, jamais à un générateur (§2.E). */
export const ESPECE_RESERVEE = 'tanche'

const ESPECES_DE_LA_NOUE: readonly string[] = ['vairon', 'loche', 'epinoche']

function construireEspeces(): readonly Espece[] {
  const especes: Espece[] = []
  ASSISES.forEach((assise, rang) => {
    for (let i = 0; i < ESPECES_PAR_ASSISE[rang]; i += 1) {
      const id = rang === 0 ? ESPECES_DE_LA_NOUE[i] : `espece-${assise.rang}-${i + 1}`
      if (id === ESPECE_RESERVEE) throw new Error('`tanche` est réservée au héros (§2.E)')
      especes.push({ id, assise: assise.id })
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
