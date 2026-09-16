/**
 * Le critère du §12 : « une partie sans UI atteint l'éclosion 2 en headless ».
 *
 * Politique naïve — acheter le moins cher qui soit payable — pour les ACHATS
 * seulement : si elle n'y arrive pas, c'est l'économie qui est fausse, pas la
 * politique.
 *
 * L'ÉCLOSION, elle, ne suit pas cette politique naïve : elle suit `doitEclore`
 * du simulateur, LA décision canonique du jeu (§6.4 — rester pour la
 * contenance jusqu'à saturation de l'acquis de séjour, puis éclore). Écrire
 * une seconde règle à la main ici — par exemple « éclore dès `estBloque` » —
 * ignorerait cette décision : tant que l'acquis n'a pas fait son travail, une
 * éclosion précoce ne gagne presque aucune contenance, et le test cesserait de
 * prouver l'économie pour se mettre à prouver une politique d'éclosion
 * différente de celle du jeu. Une seule définition, partagée par import
 * (tâche 10 → tâche 11) : c'est la leçon de la tâche 9 sur les listes
 * recopiées à la main, qui avaient dérivé en silence.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import { etatInitial, tick, creuser, debloquer, ameliorer, eclore } from '../src/noyau/noyau'
import { contenance, coutDeDescente, coutDeDeblocage, coutDeNiveau, toutEstCreuse } from '../src/noyau/economie'
import { doitEclore, POLITIQUE_PAR_DEFAUT } from '../src/simulateur/simulateur'
import { ESPECES } from '../src/donnees/especes'
import type { EtatJeu } from '../src/noyau/types'

interface OptionAchat {
  readonly cout: Decimal
  readonly appliquer: (etat: EtatJeu) => EtatJeu
}

/** Toutes les dépenses payables MAINTENANT, moins-cher-d'abord — le tri se fait au retour. */
function optionsPayables(etat: EtatJeu): readonly OptionAchat[] {
  const options: OptionAchat[] = []
  if (!toutEstCreuse(etat)) {
    const cible = etat.cycle.paliersOuverts
    const cout = coutDeDescente(etat, cible)
    // Hors de portée pour toujours si ça dépasse la contenance (§6.4), pas
    // seulement pour l'instant si ça dépasse le mana courant.
    // `contenance(etat)`, jamais `permanent.contenanceMana` : depuis que le
    // plafond monte pendant le cycle, les deux ont cessé d'être la même chose,
    // et lire la seconde faisait croire le creusement fermé alors que le noyau
    // le rouvrait — la partie ne pouvait plus éclore du tout. La règle vit dans
    // le noyau ; on l'appelle, on ne la recopie pas.
    if (cout.lte(contenance(etat)) && etat.cycle.manaCourant.gte(cout)) {
      options.push({ cout, appliquer: creuser })
    }
  }
  for (const espece of ESPECES) {
    if (espece.palier >= etat.cycle.paliersOuverts) continue
    const vivante = etat.cycle.especes[espece.id]
    const debloquee = vivante !== undefined && vivante.debloquee
    const cout = debloquee ? coutDeNiveau(etat, espece, vivante.niveau) : coutDeDeblocage(etat, espece)
    if (etat.cycle.manaCourant.lt(cout)) continue
    options.push({
      cout,
      appliquer: debloquee ? (e) => ameliorer(e, espece.id) : (e) => debloquer(e, espece.id),
    })
  }
  return options
}

function acheterLeMoinsCher(etat: EtatJeu): EtatJeu {
  const meilleure = optionsPayables(etat).reduce<OptionAchat | null>(
    (m, option) => (m === null || option.cout.lt(m.cout) ? option : m),
    null,
  )
  return meilleure === null ? etat : meilleure.appliquer(etat)
}

describe('§12 — une partie headless', () => {
  it('atteint l’éclosion 2 sans interface, en moins de 200 heures de jeu', () => {
    let etat = etatInitial(2026)
    let secondes = 0
    while (etat.permanent.nombreEclosions < 2 && secondes < 200 * 3600) {
      const avant = etat
      etat = acheterLeMoinsCher(etat)
      if (etat === avant) {
        // Plus aucun achat naïf possible : soit la décision canonique
        // d'éclore est mûre, soit il faut simplement laisser l'acquis de
        // séjour avancer vers sa saturation.
        if (doitEclore(etat, POLITIQUE_PAR_DEFAUT)) etat = eclore(etat)
        else {
          etat = tick(etat, 60)
          secondes += 60
        }
      }
    }
    expect(etat.permanent.nombreEclosions).toBeGreaterThanOrEqual(2)
    expect(etat.permanent.profondeurMaxAtteinte).toBeGreaterThan(6)
  })
})
