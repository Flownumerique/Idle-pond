/**
 * IdlePond — succès de famille ACTE.
 *
 * §8.1 : « Premier temple, premier portail, première reconviction. Une fois
 * chacun. À la main, ~30 sur la partie. » Ceux de l'assise I sont les gestes
 * d'ouverture : convaincre, creuser, remplir, répéter.
 *
 * Ils portent l'essentiel du plancher du §8.4 sur les toutes premières minutes,
 * parce qu'un acte se déclenche à l'instant où le joueur fait quelque chose —
 * là où un seuil d'effectif attend que la population monte.
 */
import type { Succes } from '../../noyau/types'
import { PART_REMISE_D_UN_SUCCES } from '../../noyau/constantes'
import { idDeBanc } from '../paliers'

const ASSISE = 'noue'

export const ACTES: readonly Succes[] = [
  {
    id: 'acte-premiere-conviction',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'bancs_convaincus', seuil: 1 },
    effet: null,
  },
  {
    // Les toutes premières minutes tiennent sur ces deux-là. §8.4 : « premier
    // succès dans les deux premières minutes », puis un toutes les 3 à 5.
    // Sans eux, le joueur convainc un banc et attend neuf minutes.
    id: 'acte-deuxieme-niveau',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'place_de_banc', banc: idDeBanc('vairon', 0), seuil: 2 },
    effet: null,
  },
  {
    id: 'acte-cinquieme-niveau',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'place_de_banc', banc: idDeBanc('vairon', 0), seuil: 5 },
    effet: { genre: 'reduction_cout', terme: 'cout_place', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-premier-banc-de-cinq',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'effectif_de_banc', banc: idDeBanc('vairon', 0), seuil: 5 },
    effet: null,
  },
  {
    id: 'acte-premier-creusement',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'paliers_ouverts', seuil: 2 },
    effet: { genre: 'reduction_cout', terme: 'cout_creuser', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-deux-bancs',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'bancs_convaincus', seuil: 2 },
    effet: { genre: 'reduction_cout', terme: 'reduction_technique', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-dixieme-niveau',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'place_de_banc', banc: idDeBanc('vairon', 0), seuil: 10 },
    effet: { genre: 'reduction_cout', terme: 'cout_place', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-trois-bancs',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'bancs_convaincus', seuil: 3 },
    effet: { genre: 'reduction_cout', terme: 'reduction_technique', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-premier-palier-sature',
    famille: 'acte',
    // Secret : un emplacement vide, rien d'autre. Le joueur découvrira qu'un
    // palier peut être plein en le remplissant, pas en lisant une consigne.
    visibilite: 'secret',
    assise: ASSISE,
    declencheur: { quoi: 'palier_sature', palier: 0 },
    effet: { genre: 'reduction_cout', terme: 'cout_place', part: PART_REMISE_D_UN_SUCCES },
  },
]
