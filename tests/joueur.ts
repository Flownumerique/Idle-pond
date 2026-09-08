/**
 * Un joueur simulé à la cadence du jeu, pour mesurer ce qui se voit à l'écran :
 * il achète le moins cher dès qu'il peut, à chaque seconde.
 *
 * Ce n'est pas le simulateur du §12 — celui-là mesure l'économie complète sur
 * 62 paliers et quinze cycles. Celui-ci joue l'assise I telle qu'elle est
 * LIVRÉE, à la vitesse où un humain la verrait, et sert au plancher du §8.4.
 */
import type { EtatJeu, SuccesId } from '../src/noyau/types'
import { FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES } from '../src/noyau/constantes'
import { convaincre, creuser, etatInitial, acheterPlace, tickDetaille } from '../src/noyau/noyau'
import { enregistrerIntervalleDeSucces } from '../src/noyau/succes'
import { contenance, coutDeDescente, coutDeConviction, coutDePlace, toutEstCreuse } from '../src/noyau/economie'
import { PALIERS_LIVRES } from '../src/donnees/assises'
import { PALIERS } from '../src/donnees/paliers'

export interface Declenchement {
  readonly id: SuccesId
  readonly instantSecondes: number
}

function depenser(etat: EtatJeu): EtatJeu {
  let courant = etat
  for (let garde = 0; garde < 200; garde += 1) {
    const plafond = contenance(courant)
    let meilleure: { cout: import('break_infinity.js').default; appliquer: (e: EtatJeu) => EtatJeu } | null = null
    const retenir = (cout: import('break_infinity.js').default, appliquer: (e: EtatJeu) => EtatJeu) => {
      if (cout.gt(plafond)) return
      if (meilleure === null || cout.lt(meilleure.cout)) meilleure = { cout, appliquer }
    }
    if (!toutEstCreuse(courant)) retenir(coutDeDescente(courant, courant.cycle.paliersOuverts), creuser)
    for (let palier = 0; palier < courant.cycle.paliersOuverts; palier += 1) {
      for (const banc of PALIERS[palier].bancs) {
        const place = courant.cycle.bancs[banc.id]?.place ?? 0
        const id = banc.id
        retenir(
          place === 0 ? coutDeConviction(courant, banc) : coutDePlace(courant, banc, place),
          place === 0 ? (e) => convaincre(e, id) : (e) => acheterPlace(e, id),
        )
      }
    }
    if (meilleure === null) return courant
    const choix: { cout: import('break_infinity.js').default; appliquer: (e: EtatJeu) => EtatJeu } = meilleure
    if (choix.cout.gt(courant.cycle.manaCourant)) return courant
    const suivant = choix.appliquer(courant)
    if (suivant === courant) return courant
    courant = suivant
  }
  return courant
}

/** Les trente premières minutes, seconde par seconde, sur l'assise I livrée. */
export function joueUneDemiHeure(graine = 1): readonly Declenchement[] {
  let etat = etatInitial(graine, PALIERS_LIVRES)
  const releve: Declenchement[] = []
  etat = depenser(etat)
  for (let instant = 1; instant <= FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES; instant += 1) {
    const resultat = tickDetaille(etat, 1)
    etat = depenser(resultat.etat)
    for (const id of resultat.declenches) releve.push({ id, instantSecondes: instant })
  }
  return releve
}

/** Rejoue `secondes` de partie, à la cadence du jeu, sur le monde livré. */
export function rejoue(secondes: number, limite = PALIERS_LIVRES): EtatJeu {
  let etat = etatInitial(3, limite)
  etat = depenser(etat)
  for (let t = 0; t < secondes; t += 1) {
    const r = tickDetaille(etat, 1)
    etat = depenser(enregistrerIntervalleDeSucces(r.etat, r.declenches))
  }
  return etat
}
