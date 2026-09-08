/**
 * Test de déterminisme (§12, jalon v0.1).
 *
 * « Même graine + même séquence de dt ⇒ même état final. »
 *
 * Le PRNG est à graine et vit dans l'état ; deux exécutions du même état avec
 * le même dt donnent le même résultat, bit pour bit. Aucune tolérance ici,
 * contrairement à l'équivalence de pas : une divergence, même minuscule, veut
 * dire qu'un état vit hors du reducer.
 */
import { describe, expect, it } from 'vitest'
import { tick, tirer } from '../src/noyau/noyau'
import { serialiser } from '../src/adaptateurs/persistance'
import { simuler } from '../src/simulateur/simulateur'
import { etatDeTravail } from './etat-de-travail'

const SEQUENCE = [0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900]

function jouer(): unknown {
  let etat = etatDeTravail(4242)
  for (const dt of SEQUENCE) etat = tick(etat, dt)
  return serialiser(etat)
}

describe('déterminisme', () => {
  it('même graine et même séquence de dt donnent le même état, bit pour bit', () => {
    expect(JSON.stringify(jouer())).toBe(JSON.stringify(jouer()))
  })

  it('deux simulations de même graine sont identiques', () => {
    const a = simuler(3, undefined, 7)
    const b = simuler(3, undefined, 7)
    expect(JSON.stringify(serialiser(a.etat))).toBe(JSON.stringify(serialiser(b.etat)))
  })

  it('le PRNG est pur : il rend une valeur et un état suivant, sans muter', () => {
    const depart = { graine: 12345 }
    const [valeur, suivant] = tirer(depart)
    const [memeValeur, memeSuivant] = tirer(depart)
    expect(depart.graine).toBe(12345)
    expect(valeur).toBe(memeValeur)
    expect(suivant.graine).toBe(memeSuivant.graine)
    expect(valeur).toBeGreaterThanOrEqual(0)
    expect(valeur).toBeLessThan(1)
  })

  it('le chemin continu ne consomme jamais de hasard', () => {
    // Sinon 480 pas de 60 s tireraient 480 fois là où un pas de 8 h tire une
    // fois, et l'équivalence de pas tomberait avec le hors ligne et le
    // simulateur. Le hasard n'a droit de cité que sur des événements discrets.
    const depart = etatDeTravail()
    expect(tick(depart, 28800).prng.graine).toBe(depart.prng.graine)
    let parPas = depart
    for (let i = 0; i < 100; i += 1) parPas = tick(parPas, 60)
    expect(parPas.prng.graine).toBe(depart.prng.graine)
  })
})
