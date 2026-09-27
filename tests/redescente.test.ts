/**
 * f = 1 — reset complet. Le noyau v1.0 §3.1 ferme [P5] : « Tout se repaie au
 * prix d'origine. Il n'y a pas de tarif réduit à la redescente, comme dans
 * n'importe quel idle. »
 */
import { describe, expect, it } from 'vitest'
import Decimal from 'break_infinity.js'
import { etatInitial, creuser } from '../src/noyau/noyau'
import { coutDeDescente } from '../src/noyau/economie'
import { eclore } from '../src/noyau/eclosion'

describe('§3.1 — la redescente se paie plein tarif', () => {
  it('un palier déjà atteint dans une vie passée coûte exactement ce qu’il coûtait', () => {
    let etat = {
      ...etatInitial(7),
      cycle: { ...etatInitial(7).cycle, manaCourant: new Decimal('1e30') },
      permanent: { ...etatInitial(7).permanent, contenanceMana: new Decimal('1e40') },
    }
    const coutNeuf = coutDeDescente(etat, 1)
    for (let i = 0; i < 5; i += 1) etat = creuser(etat)
    expect(etat.permanent.profondeurMaxAtteinte).toBeGreaterThanOrEqual(6)

    const apres = { ...eclore(etat), cycle: { ...eclore(etat).cycle, manaCourant: new Decimal('1e30') } }
    expect(coutDeDescente(apres, 1).eq(coutNeuf)).toBe(true)
  })

  it('creuser un palier neuf et le recreuser après éclosion coûtent le même prix', () => {
    const neuf = etatInitial(7)
    const riche = {
      ...neuf,
      cycle: { ...neuf.cycle, manaCourant: new Decimal('1e30') },
      permanent: {
        ...neuf.permanent,
        contenanceMana: new Decimal('1e40'),
        profondeurMaxAtteinte: 40, // une vie passée est allée très bas
      },
    }
    // la profondeur déjà atteinte ne doit rien changer au prix
    expect(coutDeDescente(riche, 3).eq(coutDeDescente(neuf, 3))).toBe(true)
  })
})
