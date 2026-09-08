/**
 * La contenance et l'acquis de séjour — amendement v1.1 §2.B.
 *
 * « Un cycle nominal multiplie la contenance par ≈47,1 (tolérance 2 %). »
 *
 * Ce facteur n'est écrit nulle part dans le code de l'éclosion, et c'est tout
 * l'intérêt du test : il doit ÉMERGER de `A∞` et `τ₀`. Tier 0 §8 — le plafond
 * ne monte que par séjour prolongé en mana dense ; une contenance indexée sur
 * le compteur d'éclosions violerait l'invariant, et passerait ce test tout en
 * étant fausse. C'est pourquoi le test regarde aussi la forme de la montée,
 * pas seulement son résultat.
 */
import { describe, expect, it } from 'vitest'
import {
  ACQUIS_MAX,
  CONTENANCE_PAR_ECLOSION,
  DUREE_DU_CYCLE_1_HEURES,
  TAU_SEJOUR_HEURES,
} from '../src/noyau/constantes'
import { eclore, etatInitial, tick } from '../src/noyau/noyau'
import { multiplicateurDensite } from '../src/noyau/densite'

const H = 3600

describe('contenance', () => {
  it('un cycle nominal la multiplie par ≈47,1', () => {
    const depart = etatInitial(1)
    const apresSejour = tick(depart, DUREE_DU_CYCLE_1_HEURES * H)
    const apres = eclore(apresSejour)
    const rapport = apres.permanent.contenanceMana.div(depart.permanent.contenanceMana).toNumber()
    expect(rapport).toBeCloseTo(CONTENANCE_PAR_ECLOSION, 0)
    expect(Math.abs(rapport / CONTENANCE_PAR_ECLOSION - 1)).toBeLessThan(0.02)
  })

  it('la cible dérivée vaut bien g^4.4', () => {
    expect(CONTENANCE_PAR_ECLOSION).toBeCloseTo(47.1, 1)
  })

  it('l’acquis sature vers A∞, et son t₉₀ tombe à ≈2 h', () => {
    const depart = etatInitial(1)
    const a90 = tick(depart, 2 * H).cycle.acquisDeSejour
    expect(a90 / ACQUIS_MAX).toBeCloseTo(0.9, 2)
    const aLInfini = tick(depart, 200 * H).cycle.acquisDeSejour
    expect(aLInfini).toBeCloseTo(ACQUIS_MAX, 3)
  })

  it('rester au-delà de la saturation ne rapporte plus de profondeur', () => {
    // L'effet secondaire recherché du §2.B, et il ne doit pas se casser : passé
    // la saturation, rester ne rapporte plus que de la Foi. C'est ce qui rend
    // réelle la seule vraie décision du joueur.
    const depart = etatInitial(1)
    const troisHeures = eclore(tick(depart, 3 * H)).permanent.contenanceMana
    const centHeures = eclore(tick(depart, 100 * H)).permanent.contenanceMana
    const gainDuSurplus = centHeures.div(troisHeures).toNumber()
    expect(gainDuSurplus).toBeLessThan(1.04)
  })

  it('l’acquis se dépense entièrement à l’éclosion', () => {
    const apres = eclore(tick(etatInitial(1), 3 * H))
    expect(apres.cycle.acquisDeSejour).toBe(0)
  })

  it('la densité raccourcit le séjour, elle ne le rallonge jamais', () => {
    expect(multiplicateurDensite(0)).toBe(1)
    expect(multiplicateurDensite(1)).toBe(1)
    expect(multiplicateurDensite(10)).toBeGreaterThan(1)
    // Une eau dense sature plus vite : c'est la compensation du §2.A.
    const depart = etatInitial(1)
    const dense = {
      ...depart,
      permanent: { ...depart.permanent, densites: depart.permanent.densites.map(() => 10) },
    }
    expect(tick(dense, H).cycle.acquisDeSejour).toBeGreaterThan(tick(depart, H).cycle.acquisDeSejour)
  })

  it('τ₀ est bien le temps caractéristique à densité neutre', () => {
    const apres = tick(etatInitial(1), TAU_SEJOUR_HEURES * H)
    expect(apres.cycle.acquisDeSejour / ACQUIS_MAX).toBeCloseTo(1 - Math.exp(-1), 6)
  })
})
