/**
 * IdlePond — succès de famille FRANCHISSEMENT.
 *
 * §8.1 : « Éclosion, couche fixée, acclimatation complétée, fin d'assise.
 * ~1 par cycle. À la main. »
 *
 * §8.4 impose que la PREMIÈRE ÉCLOSION en déclenche un, obligatoirement. C'est
 * le seul succès de tout le registre qui soit exigé nommément par le contrat,
 * et le test de plancher le vérifie.
 *
 * ── Sur l'effet : ils sont chiffrés, et c'est une dérogation assumée ────────
 * Le §8.2 pose « verbe pour les franchissements » comme défaut ORIENTATIF. Il
 * n'est pas suivi ici, pour une raison structurelle et non par facilité : les
 * onze CapaciteId du §7.3 appartiennent tous à l'arbre, et le §7.5 règle 1 veut
 * qu'une capacité ait exactement une source. Donner un verbe à un
 * franchissement demanderait donc d'inventer une capacité — c'est-à-dire du
 * canon — et de puiser dans les ~5 verbes que le §7.5 règle 2 réserve aux
 * succès pour toute la partie.
 *
 * Les cinq verbes de succès restent donc à allouer, avec l'arbitrage du budget
 * du §7.6, au jalon v0.4. L'assise I livre des chiffres.
 */
import type { Succes } from '../../noyau/types'
import { REMISE_D_UN_SUCCES_DE_SEUIL } from '../../noyau/constantes'
import { ASSISES } from '../assises'

const ASSISE = 'assise-1'

export const FRANCHISSEMENTS: readonly Succes[] = [
  {
    id: 'franchissement-premiere-eclosion',
    famille: 'franchissement',
    visibilite: 'ouvert',
    assise: ASSISE,
    declencheur: { quoi: 'eclosions', seuil: 1 },
    effet: { nature: 'chiffre', terme: 'cout_creuser', facteur: REMISE_D_UN_SUCCES_DE_SEUIL },
  },
  {
    id: 'franchissement-deuxieme-eclosion',
    famille: 'franchissement',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'eclosions', seuil: 2 },
    effet: { nature: 'chiffre', terme: 'cout_creuser', facteur: REMISE_D_UN_SUCCES_DE_SEUIL },
  },
  {
    id: 'franchissement-troisieme-eclosion',
    famille: 'franchissement',
    visibilite: 'ferme',
    assise: ASSISE,
    declencheur: { quoi: 'eclosions', seuil: 3 },
    effet: { nature: 'chiffre', terme: 'cout_deblocage', facteur: REMISE_D_UN_SUCCES_DE_SEUIL },
  },
  {
    id: 'franchissement-densite',
    famille: 'franchissement',
    visibilite: 'secret',
    assise: ASSISE,
    declencheur: { quoi: 'densite_de_palier', palier: 0, seuil: 0.5 },
    effet: null,
  },
  {
    id: 'franchissement-fond-de-la-mare',
    famille: 'franchissement',
    visibilite: 'ferme',
    assise: ASSISE,
    // Fin d'assise : tous les paliers de la mare ouverts dans la même vie.
    declencheur: { quoi: 'paliers_ouverts', seuil: ASSISES[0].nombreDePaliers },
    effet: { nature: 'chiffre', terme: 'cout_creuser', facteur: REMISE_D_UN_SUCCES_DE_SEUIL },
  },
]
