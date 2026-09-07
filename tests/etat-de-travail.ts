/**
 * Un état de jeu non trivial pour les tests du noyau : plusieurs paliers
 * ouverts, des bancs à des niveaux différents, des effectifs en cours de
 * convergence et des densités inégales — donc une vitesse de repeuplement
 * différente par palier. Un état plat ne prouverait pas grand-chose.
 */
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../src/noyau/types'
import { acheterPlace, convaincre, creuser, etatInitial, tick } from '../src/noyau/noyau'
import { BANCS } from '../src/donnees/paliers'

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
  for (const banc of BANCS.filter((b) => b.palier < etat.cycle.paliersOuverts)) {
    etat = convaincre(etat, banc.id)
    for (let n = 0; n < 12 + banc.palier; n += 1) etat = acheterPlace(etat, banc.id)
  }
  // Un peu d'avance pour que les effectifs soient en cours de route, ni à zéro
  // ni à la cible : c'est le régime où l'exponentielle peut mentir.
  return tick(etat, 137)
}
