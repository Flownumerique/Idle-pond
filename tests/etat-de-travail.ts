/**
 * Un état de jeu non trivial pour les tests du noyau : plusieurs paliers
 * ouverts, des espèces débloquées à des niveaux différents, des densités
 * inégales. Un état plat ne prouverait pas grand-chose.
 *
 * Plus aucun `tick` de mise en route à la fin : un niveau agit à l'instant où
 * il est payé, donc l'état est complet dès le dernier achat. C'est exactement
 * ce que le passage au modèle à niveau a supprimé — le régime transitoire où
 * l'exponentielle pouvait mentir.
 */
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../src/noyau/types'
import { ameliorer, creuser, debloquer, etatInitial } from '../src/noyau/noyau'
import { ESPECES } from '../src/donnees/especes'

export function etatDeTravail(graine = 12345, contenance = '1e14'): EtatJeu {
  let etat = etatInitial(graine)
  etat = {
    ...etat,
    cycle: { ...etat.cycle, manaCourant: new Decimal('1e12') },
    permanent: {
      ...etat.permanent,
      contenanceMana: new Decimal(contenance),
      densites: etat.permanent.densites.map((_, index) => index * 0.13),
      profondeurMaxAtteinte: 9,
    },
  }
  for (let i = 0; i < 6; i += 1) etat = creuser(etat)
  for (const espece of ESPECES.filter((e) => e.palier < etat.cycle.paliersOuverts)) {
    etat = debloquer(etat, espece.id)
    for (let n = 0; n < 12 + espece.palier; n += 1) etat = ameliorer(etat, espece.id)
  }
  return etat
}
