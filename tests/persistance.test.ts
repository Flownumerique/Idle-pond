/**
 * Versionnage de save et aller-retour Decimal (§10, §12 jalon v0.1).
 *
 * Rétrofiter un versionnage sur des saves existantes coûte un wipe : le champ
 * et la chaîne de migrations existent dès la v0.1, même vides.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import { VERSION_SAVE } from '../src/noyau/constantes'
import { etatInitial } from '../src/noyau/noyau'
import {
  deserialiser,
  deserialiserDecimal,
  migrer,
  serialiser,
  serialiserDecimal,
} from '../src/adaptateurs/persistance'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

describe('persistance', () => {
  it('un Decimal fait l’aller-retour à l’exact', () => {
    const valeurs = [
      new Decimal(0),
      new Decimal(1),
      new Decimal('1.2345678901234567e30'),
      new Decimal(2.4).pow(61),
      new Decimal(1).div(3),
    ]
    for (const valeur of valeurs) {
      const retour = deserialiserDecimal(serialiserDecimal(valeur), new Decimal(-1))
      expect(retour.eq(valeur), `${valeur} n’est pas revenu identique`).toBe(true)
    }
  })

  it('un Decimal illisible retombe sur le repli plutôt que sur NaN', () => {
    expect(deserialiserDecimal(undefined, new Decimal(7)).eq(7)).toBe(true)
    expect(deserialiserDecimal({}, new Decimal(7)).eq(7)).toBe(true)
    expect(deserialiserDecimal('pas un nombre', new Decimal(7)).eq(7)).toBe(true)
  })

  it('un état complet survit à un aller-retour par JSON', () => {
    const depart = etatDeTravail()
    const texte = JSON.stringify(serialiser(depart))
    const retour = deserialiser(JSON.parse(texte), etatInitial(0))
    comparerAToleranceFlottante(retour, depart, 0)
  })

  it('la save porte une version et la chaîne de migrations existe', () => {
    const save = serialiser(etatInitial(1))
    expect(save.versionSave).toBe(VERSION_SAVE)
    expect(() => migrer(save)).not.toThrow()
  })

  it('une save d’une version inconnue refuse de se charger en silence', () => {
    const save = { versionSave: VERSION_SAVE - 1, contenu: {} }
    expect(() => migrer(save)).toThrow(/Migration de save manquante/)
  })
})
