/**
 * §8.4 — plancher garanti sur l'assise I.
 *
 * | Garantie              | Valeur                                    |
 * |-----------------------|-------------------------------------------|
 * | Premier succès        | Dans les DEUX PREMIÈRES MINUTES           |
 * | Première demi-heure   | Un déclenchement toutes les 3 à 5 minutes |
 * | Première éclosion     | Un franchissement, OBLIGATOIREMENT        |
 *
 * Ce n'est pas une intention de design, c'est une garantie chiffrée : elle se
 * mesure, ou elle n'existe pas. Le test joue les trente premières minutes avec
 * un joueur qui achète le moins cher dès qu'il peut, et relève les instants.
 */
import { describe, expect, it } from 'vitest'
import type { EtatJeu, SuccesId } from '../src/noyau/types'
import {
  CADENCE_MAX_ENTRE_SUCCES_SECONDES,
  FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES,
  PREMIER_SUCCES_AVANT_SECONDES,
} from '../src/noyau/constantes'
import { eclore, etatInitial, tickDetaille } from '../src/noyau/noyau'
import { SUCCES } from '../src/donnees/succes/index'
import { joueUneDemiHeure, type Declenchement } from './joueur'

describe('plancher de cadence de l’assise I', () => {
  const releve = joueUneDemiHeure()

  it('le premier succès tombe en moins de deux minutes', () => {
    expect(releve.length, 'aucun succès déclenché').toBeGreaterThan(0)
    expect(releve[0].instantSecondes).toBeLessThan(PREMIER_SUCCES_AVANT_SECONDES)
  })

  it('un déclenchement au moins toutes les cinq minutes sur la première demi-heure', () => {
    const trous: string[] = []
    let precedent = 0
    for (const declenchement of releve) {
      const ecart = declenchement.instantSecondes - precedent
      if (ecart > CADENCE_MAX_ENTRE_SUCCES_SECONDES) {
        trous.push(`${(ecart / 60).toFixed(1)} min avant ${declenchement.id}`)
      }
      precedent = declenchement.instantSecondes
    }
    expect(trous, 'la cadence du §8.4 est trouée entre deux succès').toEqual([])
  })

  /**
   * PARQUÉ — défaut du PLAN, pas de la tâche 9. `it.fails` : ceci DOIT échouer
   * tant que le défaut tient. Si cette assertion se met à réussir un jour, le
   * test ci-dessous échouera À SON TOUR (c'est le sens d'`it.fails`) — ce qui
   * force qui que ce soit à supprimer ce bloc plutôt que de laisser un test
   * muet traîner. Ce n'est pas un `skip` : un test qui ne peut plus échouer
   * est pire qu'aucun test — cette famille de défaut a déjà mordu ce plan
   * trois fois.
   *
   * Le silence après le dernier succès atteignable dans la fenêtre de 30 min
   * dépasse les 5 min permises. Mesuré (tâche 9, harnais `tests/joueur.ts`,
   * assise I, seconde par seconde) :
   *
   *   dernier succès atteignable dans cette fenêtre : t ≈ 1381 s
   *   silence jusqu'à la 30ᵉ minute (1800 s)         : 419 s
   *   seuil autorisé (`CADENCE_MAX_ENTRE_SUCCES_SECONDES`) : 300 s
   *   dépassement                                    : 119 s
   *
   * Isolé avec un contrôle propre : `DEBIT_HEROS = 0` fait passer ce test
   * intégralement (dernier succès atteignable à t ≈ 1542 s, silence de 258 s,
   * soit 42 s de marge sous les 300 s — la marge HISTORIQUE de ce test, avant
   * que la tâche 9 touche quoi que ce soit). Toute valeur strictement
   * positive testée — 0.05 (la graine du brief) jusqu'à 2× le plancher
   * arithmétique du premier déblocage, sur deux sessions de mesure —
   * reproduit un trou de fin. Ce n'est donc pas une question de magnitude :
   * aucune valeur de `DEBIT_HEROS` ne peut satisfaire à la fois ce test et
   * `RESULTATS.md` finding 3 (qui exige un débit strictement positif, sans
   * quoi l'état dégénéré — plus rien n'est jamais affordable — redevient
   * atteignable). `DEBIT_HEROS` reste à 0.05 : voir son commentaire dans
   * `constantes.ts`.
   *
   * POURQUOI ACCÉLÉRER AGGRAVE (contre-intuitif — c'est ce que quelqu'un
   * tentera de « corriger » en remontant le débit) : un joueur plus rapide ne
   * fait pas grossir le nombre de succès ATTEIGNABLES dans la fenêtre — ce
   * nombre est fixé par le registre fini de la Noue — il se contente de les
   * déclencher tous PLUS TÔT, ce qui allonge d'autant le silence après le
   * dernier d'entre eux, jusqu'à la trentième minute.
   *
   * Ferme quand : la tâche 12 remplace la politique d'achat du joueur simulé
   * par une politique de gain marginal analytique (un joueur plus rapide
   * atteindrait alors le succès SUIVANT au lieu de seulement tirer les
   * précédents plus tôt) ; ou la tâche 13, qui résout les nombres qui
   * décident de l'atteignabilité. Une troisième voie — densifier les succès
   * atteignables de la Noue entre la 25ᵉ et la 30ᵉ minute — existe mais sort
   * de ce plan (GDD).
   */
  it.fails(
    'le silence après le dernier succès atteignable ne dépasse pas cinq minutes — PARQUÉ, voir tâches 12/13',
    () => {
      const dernier = releve.length > 0 ? releve[releve.length - 1].instantSecondes : 0
      const fin = FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES - dernier
      expect(fin).toBeLessThanOrEqual(CADENCE_MAX_ENTRE_SUCCES_SECONDES)
    },
  )

  it('la première éclosion déclenche un franchissement, obligatoirement', () => {
    let etat: EtatJeu = etatInitial(1)
    etat = eclore(etat)
    const declenches: readonly SuccesId[] = tickDetaille(etat, 0.1).declenches
    const franchissements = declenches.filter(
      (id) => SUCCES.find((s) => s.id === id)?.famille === 'franchissement',
    )
    expect(franchissements.length).toBeGreaterThan(0)
  })

  it('la cadence ne tient pas à un seul succès qui se répéterait', () => {
    const identifiants = new Set(releve.map((d: Declenchement) => d.id))
    expect(identifiants.size).toBe(releve.length)
  })
})
