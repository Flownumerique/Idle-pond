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
    // Une version SANS migration déclarée. Un trou dans la chaîne doit crier,
    // pas charger à moitié : rétrofiter un versionnage coûte un wipe, mais
    // charger une save qu'on ne sait pas lire en coûte un aussi.
    const save = { versionSave: -1, contenu: {} }
    expect(() => migrer(save)).toThrow(/Migration de save manquante/)
  })

  it('une save du jalon précédent se réveille au sortir de l’œuf', () => {
    // 1 → 2 : la géométrie de la Noue a changé sous ses pieds. Le cycle est
    // rendu, le permanent conservé — exactement ce que l'éclosion fait quinze
    // fois par partie.
    const ancienne = {
      versionSave: 1,
      contenu: {
        permanent: {
          succesDebloques: ['seuil-espece-1-1-10', 'acte-premiere-conviction'],
          couches: ['assise-1'],
        },
      },
    }
    const repli = etatInitial(0)
    const reprise = deserialiser(ancienne, repli)
    expect(reprise.versionSave).toBe(VERSION_SAVE)
    expect(reprise.permanent.succes['seuil-vairon-10']).toBeDefined()
    expect(reprise.permanent.couches).toEqual(['noue'])
    expect(reprise.cycle.paliersOuverts).toBe(repli.cycle.paliersOuverts)
  })

  it('2 → 3 : les succès acquis reçoivent un registre, les bénédictions disparaissent', () => {
    // Le registre d'une entrée fige sa langue (GDD §14.5). Une save v2 ne l'a
    // jamais porté : la migration inscrit le palier que ses franchissements
    // impliquent, faute de pouvoir reconstituer celui d'alors.
    const ancienne = {
      versionSave: 2,
      contenu: {
        permanent: {
          nombreEclosions: 3,
          succesDebloques: ['seuil-vairon-10', 'acte-premiere-conviction'],
          benedictions: { 'quelque-chose': 2 },
        },
      },
    }
    const reprise = deserialiser(ancienne, etatInitial(0))

    expect(reprise.permanent.succes['seuil-vairon-10']).toEqual({
      obtenuAuCycle: 3,
      registre: 'directives',
    })
    expect(Object.keys(reprise.permanent.succes)).toHaveLength(2)
    expect('benedictions' in reprise.permanent).toBe(false)
    expect('succesDebloques' in reprise.permanent).toBe(false)
  })

  it('une save migrée se sérialise comme une save native de même contenu', () => {
    // L'ordre des clefs d'un Record est celui de leur insertion, et le test de
    // déterminisme compare des chaînes. Une migration qui insérerait dans
    // l'ordre de la save ferait diverger deux parties identiques.
    const idsDansLeDesordre = ['acte-premiere-conviction', 'seuil-vairon-10']
    const migree = deserialiser(
      {
        versionSave: 2,
        contenu: { permanent: { nombreEclosions: 0, succesDebloques: idsDansLeDesordre } },
      },
      etatInitial(0),
    )
    const inverse = deserialiser(
      {
        versionSave: 2,
        contenu: { permanent: { nombreEclosions: 0, succesDebloques: [...idsDansLeDesordre].reverse() } },
      },
      etatInitial(0),
    )
    expect(Object.keys(migree.permanent.succes)).toEqual(Object.keys(inverse.permanent.succes))
  })
})
