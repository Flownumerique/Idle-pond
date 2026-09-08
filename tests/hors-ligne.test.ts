/**
 * Le crédit hors ligne (§10).
 *
 * « Le calcul est un appel unique à `tick` avec un grand `dt` — c'est
 * précisément ce que garantit le filtre du §5.2. » Le test le prend au mot :
 * ce que le crédit produit doit être, à l'identique, ce que produirait un seul
 * tick de la même durée.
 */
import { describe, expect, it } from 'vitest'
import { CAP_HORS_LIGNE_HEURES_INITIAL } from '../src/noyau/constantes'
import { tick } from '../src/noyau/noyau'
import { capHorsLigneCourantHeures, crediterHorsLigne } from '../src/adaptateurs/hors-ligne'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

const H = 3600_000

describe('hors ligne', () => {
  it('un seul appel à tick suffit pour toute l’absence', () => {
    const depart = etatDeTravail()
    const credit = crediterHorsLigne(depart, 0, 3 * H)
    expect(credit.secondesCreditees).toBe(3 * 3600)
    comparerAToleranceFlottante(
      { ...credit.etat, permanent: { ...credit.etat.permanent, heuresHorsLigneCreditees: 0 } },
      tick(depart, 3 * 3600),
      0,
    )
  })

  it('le plafond borne ce que l’absence rapporte, il ne retire rien', () => {
    const depart = etatDeTravail()
    const credit = crediterHorsLigne(depart, 0, 40 * H)
    expect(credit.secondesCreditees).toBe(CAP_HORS_LIGNE_HEURES_INITIAL * 3600)
    expect(credit.etat.cycle.manaCourant.gte(depart.cycle.manaCourant)).toBe(true)
  })

  it('le compteur Entretien ne lit que les heures créditées', () => {
    // Sans ça, avancer son horloge farme l'arbre permanent — et c'est la seule
    // protection anti-triche nécessaire dans tout le jeu.
    const depart = etatDeTravail()
    const honnete = crediterHorsLigne(depart, 0, 3 * H)
    const tricheur = crediterHorsLigne(depart, 0, 400 * H)
    expect(honnete.etat.permanent.heuresHorsLigneCreditees).toBeCloseTo(3, 6)
    expect(tricheur.etat.permanent.heuresHorsLigneCreditees).toBeCloseTo(CAP_HORS_LIGNE_HEURES_INITIAL, 6)
  })

  it('un recul d’horloge ne crédite rien et ne retire rien', () => {
    const depart = etatDeTravail()
    const credit = crediterHorsLigne(depart, 10 * H, 2 * H)
    expect(credit.secondesCreditees).toBe(0)
    expect(credit.etat).toBe(depart)
  })

  it('le plafond démarre à 6 h, avant toute technique', () => {
    expect(capHorsLigneCourantHeures(etatDeTravail())).toBe(CAP_HORS_LIGNE_HEURES_INITIAL)
  })
})
