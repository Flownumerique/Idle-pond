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
import { etatDeTravail } from './etat-de-travail'

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
    //
    // Le blocage est doux (noyau v1.0 §2.2) : rien ne borne plus la comparaison,
    // le joueur peut rester indéfiniment sans qu'aucune éclosion ne se
    // déclenche à sa place. `longSejour` n'a donc qu'à être largement plus long
    // qu'un cycle nominal — sa valeur exacte n'a plus de portée canonique.
    const longSejour = 200 * H
    expect(longSejour / H).toBeGreaterThan(10 * DUREE_DU_CYCLE_1_HEURES)

    const depart = etatInitial(1)
    const nominal = eclore(tick(depart, DUREE_DU_CYCLE_1_HEURES * H)).permanent.contenanceMana
    const bienPlusLong = eclore(tick(depart, longSejour)).permanent.contenanceMana

    expect(tick(depart, longSejour).permanent.nombreEclosions, 'aucune éclosion ne se déclenche seule').toBe(0)
    expect(bienPlusLong.div(nominal).toNumber()).toBeLessThan(1.04)
  })

  it('l’acquis se dépense entièrement à l’éclosion', () => {
    const apres = eclore(tick(etatInitial(1), 3 * H))
    expect(apres.cycle.acquisDeSejour).toBe(0)
  })

  it('la densité raccourcit le séjour, elle ne le rallonge jamais', () => {
    // Le point `multiplicateurDensite(1) === 1` verrouillait le PLANCHER de
    // l'ancienne forme (`Math.max(1, densité)^e`), remplacée depuis la tâche 9
    // par celle des contraintes globales du plan, `(1 + densité/d₀)^e` — les
    // deux coïncident à densité 0, pas à densité 1. Ce que ce test protège
    // n'est pas ce point-là : c'est l'invariant que son propre nom porte —
    // « elle ne le rallonge jamais » — donc `≥ 1` partout, et croissant.
    let precedent = multiplicateurDensite(0)
    expect(precedent).toBe(1)
    for (const densite of [0.5, 1, 2, 5, 10, 50]) {
      const valeur = multiplicateurDensite(densite)
      expect(valeur).toBeGreaterThanOrEqual(1)
      expect(valeur).toBeGreaterThanOrEqual(precedent)
      precedent = valeur
    }
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

describe('le blocage est doux (noyau v1.0 §2.2)', () => {
  it('une jauge pleine pendant une semaine ne déclenche aucune éclosion', () => {
    let etat = etatDeTravail()
    const eclosionsAvant = etat.permanent.nombreEclosions
    // 7 jours en un seul pas, jauge saturée du début à la fin
    etat = tick({ ...etat, cycle: { ...etat.cycle, manaCourant: etat.permanent.contenanceMana } }, 7 * 24 * 3600)
    expect(etat.permanent.nombreEclosions).toBe(eclosionsAvant)
    expect(etat.cycle.manaCourant.eq(etat.permanent.contenanceMana)).toBe(true)
  })
})
