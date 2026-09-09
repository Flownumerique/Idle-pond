/**
 * IdlePond — les espèces de base.
 *
 * 21 espèces réparties 2 / 4 / 4 / 4 / 4 / 3 sur six assises. Ce n'est pas un
 * choix : c'est le décompte de « une espèce tous les trois paliers, à partir du
 * premier de chaque assise » appliqué à 6/12/12/12/12/8 (RESULTATS.md,
 * finding 4). Le chiffre de 24 du noyau v1.0 était une estimation.
 *
 * Les divergences (~6) sont du contenu v0.5 et ne sont pas déclarées ici.
 *
 * Les trois espèces nommées par l'amendement v1.1 §2.E le restent — noms réels
 * du français d'eau douce, aucun qualificatif, aucune invention :
 *
 *   `vairon`    amorçage. Débit minuscule, foule énorme. La première chose qui
 *               accepte. Assise I, palier 0.
 *   `loche`     fouisseuse — le lien avec le creusement est gratuit. Assise I,
 *               palier 3.
 *   `epinoche`  dure, tient dans une eau qui se charge. Prépare la contrainte
 *               de l'assise II — et elle y est passée : la Noue n'en porte plus
 *               que deux, l'épinoche ouvre l'assise suivante. Elle est nommée
 *               et justifiée au canon, donc elle n'est pas engendrée.
 *
 * `tanche` est RÉSERVÉE et ne doit être assignée à aucun générateur :
 * longévité, faible débit, très forte contenance, c'est le portrait du héros
 * (`especes-cadre.md` §2).
 *
 * [P] P3 — les dix-sept espèces plus profondes attendent la charte. Leur
 * identifiant reste neutre ; le placement reste autorial (§4.2).
 */
import type { Espece, EspeceId } from '../noyau/types'
import { ESPECE_TOUS_LES_N_PALIERS, NOMBRE_D_ESPECES_DE_BASE } from '../noyau/constantes'
import { ASSISES } from './assises'

/** 21 espèces : 2 / 4 / 4 / 4 / 4 / 3 (RESULTATS.md, finding 4). */
const ESPECES_PAR_ASSISE: readonly number[] = [2, 4, 4, 4, 4, 3]

/** Réservé au héros, jamais à un générateur (§2.E). */
export const ESPECE_RESERVEE = 'tanche'

/**
 * Les espèces déjà nommées au canon, par assise puis par rang dans l'assise.
 *
 * Une case vide laisse l'identifiant s'engendrer. C'est la charte phonétique du
 * §2.E qui descend, assise après assise ; rien de générique ne s'affiche à
 * l'écran, les noms d'écran vivant dans `textes-provisoires.ts`.
 */
const ESPECES_NOMMEES: readonly (readonly string[])[] = [
  ['vairon', 'loche'],
  ['epinoche'],
]

function construireEspeces(): readonly Espece[] {
  const especes: Espece[] = []
  ASSISES.forEach((assise, rangAssise) => {
    for (let i = 0; i < ESPECES_PAR_ASSISE[rangAssise]; i += 1) {
      const id = ESPECES_NOMMEES[rangAssise]?.[i] ?? `espece-${assise.rang}-${i + 1}`
      if (id === ESPECE_RESERVEE) throw new Error('`tanche` est réservée au héros (§2.E)')
      especes.push({
        id,
        assise: assise.id,
        rang: especes.length,
        // une espèce tous les 3 paliers, à partir du premier de l'assise
        palier: assise.indexPremierPalier + i * ESPECE_TOUS_LES_N_PALIERS,
      })
    }
  })
  if (especes.length !== NOMBRE_D_ESPECES_DE_BASE) {
    throw new Error(`Compte d'espèces incohérent : ${especes.length} au lieu de ${NOMBRE_D_ESPECES_DE_BASE}`)
  }
  return especes
}

export const ESPECES: readonly Espece[] = construireEspeces()

const PAR_ID = new Map(ESPECES.map((e) => [e.id, e]))

export function especeParId(id: EspeceId): Espece | undefined {
  return PAR_ID.get(id)
}

export function especesDeLAssise(assiseId: string): readonly Espece[] {
  return ESPECES.filter((e) => e.assise === assiseId)
}
