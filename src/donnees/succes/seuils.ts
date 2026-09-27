/**
 * IdlePond — succès de famille SEUIL.
 *
 * §8.1 : « 10/25/50/100 individus, palier saturé, divergence observée.
 * Continue. GÉNÉRÉS PAR GABARIT. » Ce fichier est donc un gabarit, pas une
 * liste.
 *
 * Les seuils comptaient des INDIVIDUS ; ils comptent maintenant des NIVEAUX —
 * il n'y a plus de population. Les quatre paliers du §8.1 sont inchangés :
 * 10 / 25 / 50 / 100, et le centième est celui qui pose le drapeau permanent.
 *
 * L'épinoche est passée en tête de l'assise II avec la répartition
 * 2/4/4/4/4/3 ; ses quatre entrées la suivent plutôt que de disparaître, le
 * registre étant figé (§8). Elles ne seront listées que lorsque son assise sera
 * atteinte, donc jamais avant qu'elle soit produite.
 *
 * Effet : chiffre, conformément au défaut orientatif du §8.2, et une remise de
 * coût plutôt qu'une montée de production — voir la lecture du §4.3 retenue
 * dans `EffetDeSucces`.
 */
import type { Espece, Succes } from '../../noyau/types'
import { PART_REMISE_D_UN_SUCCES } from '../../noyau/constantes'
import { ASSISES } from '../assises'
import { ESPECES, especesDeLAssise } from '../especes'

const ASSISE = 'noue'

/** Les seuils du §8.1, à la lettre — lus sur le niveau. */
const SEUILS_DE_NIVEAU: readonly number[] = [10, 25, 50, 100]

/** Fermé passé le premier : le joueur a compris le motif, on ne le lui répète pas. */
function visibiliteDuRang(rang: number): Succes['visibilite'] {
  return rang === 0 ? 'ouvert' : 'ferme'
}

function gabarit(espece: Espece): readonly Succes[] {
  return SEUILS_DE_NIVEAU.map((seuil, rang) => ({
    id: `seuil-${espece.id}-${seuil}`,
    famille: 'seuil' as const,
    visibilite: visibiliteDuRang(rang),
    assise: espece.assise,
    declencheur: { quoi: 'niveau_d_espece' as const, espece: espece.id, seuil },
    effet: {
      genre: 'reduction_cout' as const,
      terme: 'cout_niveau' as const,
      part: PART_REMISE_D_UN_SUCCES,
    },
  }))
}

/**
 * L'épinoche, nommée au canon (§2.E), a suivi la répartition en tête de
 * l'assise II. Ses seuils la suivent : un identifiant entré au registre n'en
 * sort plus.
 */
const EPINOCHE = ESPECES.filter((e) => e.id === 'epinoche')

/**
 * Ce que la mare porte en tout. Le gabarit par espèce se tait dès que le joueur
 * change de banc ; celui-ci compte tous les niveaux tenus et ne se tait jamais.
 */
const SEUILS_DE_LA_MARE: readonly number[] = [25, 45, 70, 95, 130, 200, 320, 500]

/** Profondeur atteinte dans la vie courante. L'axe de la descente. */
const SEUILS_DE_PROFONDEUR: readonly number[] = [3, 5]

const PALIERS_DE_L_ASSISE = ASSISES[0].nombreDePaliers

export const SEUILS: readonly Succes[] = [
  ...[...especesDeLAssise(ASSISE), ...EPINOCHE].flatMap((espece) => gabarit(espece)),

  ...SEUILS_DE_LA_MARE.map((seuil, rang) => ({
    id: `seuil-mare-${seuil}`,
    famille: 'seuil' as const,
    visibilite: visibiliteDuRang(rang),
    assise: ASSISE,
    declencheur: { quoi: 'niveaux_cumules' as const, seuil },
    effet: {
      genre: 'reduction_cout' as const,
      terme: 'cout_niveau' as const,
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

  // Creux au complet : le §8.1 le range explicitement dans les seuils. Le
  // premier est un acte — la découverte qu'un creux peut être plein ; les
  // suivants sont la mesure de ce qu'on a rempli, et restent secrets pour ne
  // pas transformer l'écran en liste de courses.
  ...Array.from({ length: PALIERS_DE_L_ASSISE - 1 }, (_, index) => {
    const palier = index + 1
    return {
      id: `seuil-palier-sature-${palier}`,
      famille: 'seuil' as const,
      visibilite: 'secret' as const,
      assise: ASSISE,
      declencheur: { quoi: 'palier_au_complet' as const, palier },
      effet: {
        genre: 'reduction_cout' as const,
        terme: 'cout_niveau' as const,
        part: PART_REMISE_D_UN_SUCCES,
      },
    }
  }),
]
