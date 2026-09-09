/**
 * IdlePond — succès de famille ACTE.
 *
 * §8.1 : « Premier temple, premier portail, première reconviction. Une fois
 * chacun. À la main, ~30 sur la partie. » Ceux de l'assise I sont les gestes
 * d'ouverture : débloquer, monter, creuser, répéter.
 *
 * Ils portent l'essentiel du plancher du §8.4 sur les toutes premières minutes,
 * parce qu'un acte se déclenche à l'instant où le joueur fait quelque chose.
 * Depuis que l'espèce est un générateur à niveau, c'est vrai de TOUS les
 * déclencheurs : plus rien n'attend qu'une population monte. La cadence tient
 * donc à l'échelonnement des seuils, et à rien d'autre.
 *
 * Les identifiants ne bougent pas — le registre est figé (§8) — même là où le
 * mot « banc » y survit à la mécanique qui l'a porté.
 */
import type { Succes } from '../../noyau/types'
import { PART_REMISE_D_UN_SUCCES } from '../../noyau/constantes'

const ASSISE = 'noue'

export const ACTES: readonly Succes[] = [
  {
    id: 'acte-premiere-conviction',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'especes_debloquees', seuil: 1 },
    effet: null,
  },
  {
    // Les toutes premières minutes tiennent sur ces deux-là. §8.4 : « premier
    // succès dans les deux premières minutes », puis un toutes les 3 à 5.
    id: 'acte-deuxieme-niveau',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'niveau_d_espece', espece: 'vairon', seuil: 2 },
    effet: null,
  },
  {
    id: 'acte-cinquieme-niveau',
    famille: 'acte',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'niveau_d_espece', espece: 'vairon', seuil: 5 },
    effet: { genre: 'reduction_cout', terme: 'cout_niveau', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-premier-banc-de-cinq',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'niveaux_cumules', seuil: 3 },
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
    declencheur: { quoi: 'especes_debloquees', seuil: 2 },
    effet: { genre: 'reduction_cout', terme: 'reduction_technique', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-dixieme-niveau',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'niveau_d_espece', espece: 'vairon', seuil: 10 },
    effet: { genre: 'reduction_cout', terme: 'cout_niveau', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    // La Noue ne porte plus que deux espèces : « trois bancs » n'y existe plus.
    // Le geste qu'il marquait — avoir deux bancs et les tenir tous les deux —
    // se lit désormais sur la seconde espèce.
    id: 'acte-trois-bancs',
    famille: 'acte',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'niveau_d_espece', espece: 'loche', seuil: 10 },
    effet: { genre: 'reduction_cout', terme: 'reduction_technique', part: PART_REMISE_D_UN_SUCCES },
  },
  {
    id: 'acte-premier-palier-sature',
    famille: 'acte',
    // Secret : un emplacement vide, rien d'autre. Le joueur découvrira qu'un
    // creux peut être plein en le remplissant, pas en lisant une consigne.
    visibilite: 'secret',
    assise: ASSISE,
    declencheur: { quoi: 'palier_au_complet', palier: 0 },
    effet: { genre: 'reduction_cout', terme: 'cout_niveau', part: PART_REMISE_D_UN_SUCCES },
  },
]
