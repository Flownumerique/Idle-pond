/**
 * IdlePond — succès de famille SEUIL.
 *
 * §8.1 : « 10/25/50/100 individus, palier saturé, divergence observée.
 * Continue. GÉNÉRÉS PAR GABARIT. » Ce fichier est donc un gabarit, pas une
 * liste : trois espèces × quatre seuils = douze succès, et le §8.4 note que le
 * plancher de cadence est presque gratuit pour cette raison.
 *
 * Les seuils comptent des INDIVIDUS, tous paliers confondus — une espèce de
 * l'assise I tient trois ou quatre paliers. Compter par banc en produirait
 * quarante au lieu de douze, et la cadence deviendrait un martèlement.
 *
 * Effet : chiffre, conformément au défaut orientatif du §8.2, et une remise de
 * coût plutôt qu'une montée de production — voir la lecture du §4.3 retenue
 * dans `EffetDeSucces`.
 */
import type { EspeceId, Succes } from '../../noyau/types'
import { PART_REMISE_D_UN_SUCCES } from '../../noyau/constantes'
import { ASSISES } from '../assises'
import { especesDeLAssise } from '../especes'

const ASSISE = 'noue'

/** Les seuils du §8.1, à la lettre. */
const SEUILS_D_INDIVIDUS: readonly number[] = [10, 25, 50, 100]

/** Fermé passé le premier : le joueur a compris le motif, on ne le lui répète pas. */
function visibiliteDuRang(rang: number): Succes['visibilite'] {
  return rang === 0 ? 'ouvert' : 'ferme'
}

function gabarit(espece: EspeceId): readonly Succes[] {
  return SEUILS_D_INDIVIDUS.map((seuil, rang) => ({
    id: `seuil-${espece}-${seuil}`,
    famille: 'seuil' as const,
    visibilite: visibiliteDuRang(rang),
    assise: ASSISE,
    declencheur: { quoi: 'effectif_d_espece' as const, espece, seuil },
    effet: {
      genre: 'reduction_cout' as const,
      terme: 'cout_place' as const,
      part: PART_REMISE_D_UN_SUCCES,
    },
  }))
}

/**
 * Effectif de la mare entière. Le gabarit par espèce se tait dès que le joueur
 * change de banc ; celui-ci compte tout ce qui vit et ne se tait jamais.
 */
const SEUILS_DE_LA_MARE: readonly number[] = [25, 45, 70, 95, 130, 200, 320, 500]

/** Profondeur atteinte dans la vie courante. L'axe de la descente. */
const SEUILS_DE_PROFONDEUR: readonly number[] = [3, 5]

const PALIERS_DE_L_ASSISE = ASSISES[0].nombreDePaliers

export const SEUILS: readonly Succes[] = [
  ...especesDeLAssise(ASSISE).flatMap((espece) => gabarit(espece.id)),

  ...SEUILS_DE_LA_MARE.map((seuil, rang) => ({
    id: `seuil-mare-${seuil}`,
    famille: 'seuil' as const,
    visibilite: visibiliteDuRang(rang),
    assise: ASSISE,
    declencheur: { quoi: 'effectif_total' as const, seuil },
    effet: {
      genre: 'reduction_cout' as const,
      terme: 'cout_place' as const,
      part: PART_REMISE_D_UN_SUCCES,
    },
  })),

  ...SEUILS_DE_PROFONDEUR.map((seuil, rang) => ({
    id: `seuil-profondeur-${seuil}`,
    famille: 'seuil' as const,
    visibilite: visibiliteDuRang(rang),
    assise: ASSISE,
    declencheur: { quoi: 'paliers_ouverts' as const, seuil },
    effet: {
      genre: 'reduction_cout' as const,
      terme: 'cout_creuser' as const,
      part: PART_REMISE_D_UN_SUCCES,
    },
  })),

  // Palier saturé : le §8.1 le range explicitement dans les seuils. Le premier
  // est un acte — la découverte qu'un palier peut être plein ; les suivants
  // sont la mesure de ce qu'on a rempli, et restent secrets pour ne pas
  // transformer l'écran en liste de courses.
  ...Array.from({ length: PALIERS_DE_L_ASSISE - 1 }, (_, index) => {
    const palier = index + 1
    return {
      id: `seuil-palier-sature-${palier}`,
      famille: 'seuil' as const,
      visibilite: 'secret' as const,
      assise: ASSISE,
      declencheur: { quoi: 'palier_sature' as const, palier },
      effet: {
        genre: 'reduction_cout' as const,
        terme: 'cout_place' as const,
        part: PART_REMISE_D_UN_SUCCES,
      },
    }
  }),
]
