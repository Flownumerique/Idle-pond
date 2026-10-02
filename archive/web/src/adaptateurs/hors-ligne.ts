/**
 * IdlePond — crédit du temps hors ligne.
 *
 * « Le calcul est un appel unique à `tick` avec un grand `dt` — c'est
 * précisément ce que garantit le filtre du §5.2 » (§10). Il n'y a donc rien à
 * rattraper, aucune boucle de rattrapage, aucune approximation : le même
 * reducer, un seul pas.
 *
 * On ne punit jamais l'absence. Le plafond borne ce que l'absence RAPPORTE ; il
 * ne retire rien, et rien ne se dégrade pendant qu'on n'est pas là.
 */
import type { EtatJeu } from '../noyau/types'
import { CAP_HORS_LIGNE_HEURES_INITIAL } from '../noyau/constantes'
import { tickDetaille } from '../noyau/noyau'
import { facteurDeTechnique } from '../noyau/technique'
import { secondesHorsLigneCreditees } from './horloge'

export interface RetourDeHorsLigne {
  readonly etat: EtatJeu
  readonly secondesCreditees: number
}

/** Plafond courant, en heures. La branche Entretien le pousse de 6 h à 24 h. */
export function capHorsLigneCourantHeures(etat: EtatJeu): number {
  return CAP_HORS_LIGNE_HEURES_INITIAL * facteurDeTechnique(etat, 'cap_hors_ligne')
}

export function crediterHorsLigne(
  etat: EtatJeu,
  dernierInstantMs: number,
  maintenantMs: number,
): RetourDeHorsLigne {
  const secondes = secondesHorsLigneCreditees(
    dernierInstantMs,
    maintenantMs,
    capHorsLigneCourantHeures(etat),
  )
  if (secondes <= 0) return { etat, secondesCreditees: 0 }

  const avance = tickDetaille(etat, secondes).etat
  return {
    etat: {
      ...avance,
      permanent: {
        ...avance.permanent,
        // Le compteur Entretien lit les heures CRÉDITÉES, jamais le temps
        // écoulé. Sans ça, avancer son horloge farme l'arbre permanent — et
        // c'est la seule protection anti-triche nécessaire dans tout le jeu.
        heuresHorsLigneCreditees: avance.permanent.heuresHorsLigneCreditees + secondes / 3600,
      },
    },
    secondesCreditees: secondes,
  }
}
