/**
 * Une espèce est un générateur avec un niveau — noyau v1.0 §1.3.
 *
 * Le joueur n'achète plus de la place en attendant que la population monte :
 * il débloque une fois, il monte un niveau, et l'effet est immédiat. Plus
 * aucune population n'est simulée, donc plus rien ne converge pendant un pas.
 */
import { describe, expect, it } from 'vitest'
import Decimal from 'break_infinity.js'
import { etatInitial, creuser, debloquer, ameliorer, tick } from '../src/noyau/noyau'
import { multiplicateurDeSeuil, productionTotaleParSeconde } from '../src/noyau/economie'
import { ESPECES } from '../src/donnees/especes'
import { PALIERS } from '../src/donnees/paliers'

const RICHE = (graine = 1) => {
  const base = etatInitial(graine)
  return {
    ...base,
    cycle: { ...base.cycle, manaCourant: new Decimal('1e30') },
    permanent: { ...base.permanent, contenanceMana: new Decimal('1e40') },
  }
}

describe('une espèce est un générateur avec un niveau (noyau v1.0 §1.3)', () => {
  it('21 espèces, réparties 2/4/4/4/4/3', () => {
    expect(ESPECES).toHaveLength(21)
    const parAssise = new Map<string, number>()
    for (const e of ESPECES) parAssise.set(e.assise, (parAssise.get(e.assise) ?? 0) + 1)
    expect([...parAssise.values()]).toEqual([2, 4, 4, 4, 4, 3])
  })

  it('une espèce tous les 3 paliers, à partir du premier de son assise', () => {
    for (const espece of ESPECES) {
      const palier = PALIERS[espece.palier]
      expect(palier.espece).toBe(espece.id)
    }
    const porteurs = PALIERS.filter((p) => p.espece !== null)
    expect(porteurs).toHaveLength(21)
  })

  it('débloquer met le niveau à 1 et produit immédiatement', () => {
    let etat = RICHE()
    const premiere = ESPECES[0]
    // Depuis la tâche 9, le héros seul produit déjà (RESULTATS.md, finding 3) :
    // ce n'est plus le départ de 0 qui prouve l'achat, c'est la hausse.
    const avant = productionTotaleParSeconde(etat)
    etat = debloquer(etat, premiere.id)
    expect(etat.cycle.especes[premiere.id].niveau).toBe(1)
    expect(productionTotaleParSeconde(etat).gt(avant)).toBe(true)
  })

  it('le niveau agit sans délai : aucune population ne converge', () => {
    let etat = RICHE()
    etat = debloquer(etat, ESPECES[0].id)
    const avant = productionTotaleParSeconde(etat)
    etat = ameliorer(etat, ESPECES[0].id)
    const apres = productionTotaleParSeconde(etat)
    expect(apres.gt(avant)).toBe(true)
    // et le simple écoulement du temps ne change rien à la production
    expect(productionTotaleParSeconde(tick(etat, 3600)).eq(apres)).toBe(true)
  })

  it('les seuils lisent le niveau, cumulés, et cent vaut ×16', () => {
    expect(multiplicateurDeSeuil(1)).toBe(1)
    expect(multiplicateurDeSeuil(9)).toBe(1)
    expect(multiplicateurDeSeuil(10)).toBe(2)
    expect(multiplicateurDeSeuil(25)).toBe(4)
    expect(multiplicateurDeSeuil(50)).toBe(8)
    expect(multiplicateurDeSeuil(100)).toBe(16)
    expect(multiplicateurDeSeuil(5000)).toBe(16)
  })

  it('une espèce dont le palier n’est pas ouvert ne se débloque pas', () => {
    let etat = RICHE()
    const profonde = ESPECES[ESPECES.length - 1]
    etat = debloquer(etat, profonde.id)
    expect(etat.cycle.especes[profonde.id]?.debloquee ?? false).toBe(false)
  })

  it('creuser jusqu’au palier de la deuxième espèce la rend débloquable', () => {
    let etat = RICHE()
    const seconde = ESPECES[1]
    while (etat.cycle.paliersOuverts <= seconde.palier) etat = creuser(etat)
    etat = debloquer(etat, seconde.id)
    expect(etat.cycle.especes[seconde.id].debloquee).toBe(true)
  })
})
