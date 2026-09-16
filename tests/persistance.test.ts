/**
 * Versionnage de save et aller-retour Decimal (§10, §12 jalon v0.1).
 *
 * Rétrofiter un versionnage sur des saves existantes coûte un wipe : le champ
 * et la chaîne de migrations existent dès la v0.1, même vides.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import { REGLAGE_CANONIQUE, VERSION_SAVE } from '../src/noyau/constantes'
import { etatInitial } from '../src/noyau/noyau'
import {
  deserialiser,
  deserialiserDecimal,
  migrer,
  MIGRATIONS,
  serialiser,
  serialiserDecimal,
  type SaveSerialisee,
} from '../src/adaptateurs/persistance'
import { relever } from '../src/adaptateurs/telemetrie'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

describe('persistance', () => {
  it('le réglage n’est PAS persisté : il appartient à la version du jeu, pas à la partie', () => {
    // R41. Un réglage persisté figerait l'ancienne courbe dans les saves
    // existantes au moment même où la tâche 13 recalibre — et le jour où la
    // croissance du séjour change de valeur, une partie en cours doit suivre.
    expect(REGLAGE_CANONIQUE.croissanceDuSejourParPalier).toBeGreaterThan(0)
    const regle = { ...etatInitial(1), reglage: { croissanceDuSejourParPalier: 1.5 } }
    const save = serialiser(regle)
    expect(JSON.stringify(save.contenu)).not.toContain('croissanceDuSejour')
    expect(deserialiser(save, etatInitial(1)).reglage).toEqual(REGLAGE_CANONIQUE)
  })

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

describe('migration 4 → 5 : le modèle à population meurt sans emporter la save', () => {
  it('une save v4 se relit, ses champs morts sont ignorés, aucun n’est supprimé', () => {
    // Le brief écrivait `version: 4`, mais `SaveSerialisee` porte `versionSave` :
    // avec le mauvais nom, `migrer` lit `save.versionSave === undefined`, la
    // boucle `for (version = undefined; version < VERSION_SAVE; …)` ne tourne
    // jamais, et la migration 4 → 5 ne s'exécute pas — le test passerait quand
    // même par accident (le repli comble `especes`), sans avoir rien vérifié.
    // Mesuré en lisant `migrer()` dans src/adaptateurs/persistance.ts.
    const v4 = {
      versionSave: 4,
      contenu: {
        cycle: { bancs: { 'vairon@0': { place: 9, effectif: 7 } }, manaCourant: '500' },
        permanent: { partsMures: [1, 1, 1], acclimatations: { douce: 1 }, nombreEclosions: 2 },
      },
    } as unknown as SaveSerialisee

    const relu = deserialiser(v4, etatInitial(1))
    expect(relu.permanent.nombreEclosions).toBe(2)
    // `etatInitial(1).cycle.especes` vaut `{}` (cycleInitial()) : l'assertion
    // du brief tient toujours, mesurée directement plutôt que supposée — une
    // save v4 n'a jamais porté de niveau d'espèce, il n'y a rien à reconstruire
    // depuis `bancs`.
    expect(relu.cycle.especes).toEqual({})
    expect(relu.cycle.manaCourant.eq(500)).toBe(true)
  })

  it('la migration ne supprime aucun champ : elle les laisse passer', () => {
    const migre = MIGRATIONS[4]({
      cycle: { bancs: { x: { place: 1, effectif: 1 } } },
      permanent: { partsMures: [1] },
    }) as Record<string, Record<string, unknown>>
    expect(migre.cycle).toHaveProperty('bancs')
    expect(migre.permanent).toHaveProperty('partsMures')
    expect(migre.cycle).toHaveProperty('especes')
  })

  it('la save porte la version courante après migration — 4 → 5, puis 5 → 6', () => {
    // Écrit `5` jusqu'à la tâche 12 : la chaîne s'est allongée d'un maillon
    // (retrait de la mesure de redescente), et une save v4 le traverse aussi.
    const migre = deserialiser({ versionSave: 4, contenu: {} } as unknown as SaveSerialisee, etatInitial(0))
    expect(migre.versionSave).toBe(6)
  })

  it('ce que le joueur perd et ce qu’il garde : le cycle n’est qu’une éclosion de plus, la progression permanente survit intacte', () => {
    // Une save v4 avec de la progression permanente réelle ET un cycle en
    // cours (dans l'ancien modèle à population, donc avec `bancs`, jamais
    // `especes`). La migration 4 → 5 ne touche que `cycle.especes` — elle ne
    // vide pas le reste de `cycle` ni ne touche `permanent`.
    const v4 = {
      versionSave: 4,
      contenu: {
        cycle: {
          bancs: { 'vairon@0': { place: 9, effectif: 7 } },
          manaCourant: '1234',
          paliersOuverts: 6,
        },
        permanent: {
          partsMures: [1, 1, 1],
          acclimatations: { douce: 1 },
          secondesEnSaturation: 42,
          nombreEclosions: 5,
          contenanceMana: '99999',
          densites: [3, 2, 1, 0, 0, 0],
          foi: '77',
          compteursTechnique: { creusement: 4, amelioration: 1, recrutement: 0, entretien: 2, construction: 0, eclosion: 5 },
          noeudsTechnique: ['creusement-1'],
        },
      },
    } as unknown as SaveSerialisee

    const relu = deserialiser(v4, etatInitial(0))

    // Perdu : les niveaux d'espèces du cycle en cours ne sont pas reconstruits
    // depuis `bancs` — ils n'existaient pas dans ce modèle. C'est une éclosion
    // de plus, pas une perte de progression, puisque rien de permanent n'y
    // était rangé.
    expect(relu.cycle.especes).toEqual({})

    // Gardé : toute la progression permanente traverse la migration intacte.
    expect(relu.permanent.nombreEclosions).toBe(5)
    expect(relu.permanent.contenanceMana.eq(99999)).toBe(true)
    expect(relu.permanent.densites).toEqual([3, 2, 1, 0, 0, 0])
    expect(relu.permanent.foi.eq(77)).toBe(true)
    expect(relu.permanent.compteursTechnique).toEqual({
      creusement: 4,
      amelioration: 1,
      recrutement: 0,
      entretien: 2,
      construction: 0,
      eclosion: 5,
    })
    expect(relu.permanent.noeudsTechnique).toEqual(['creusement-1'])
  })
})

describe('migration 5 → 6 : la mesure de redescente meurt sans emporter la save', () => {
  // Une télémétrie v5 telle que `serialiser` l'écrivait : le compteur courant
  // et sa copie dans chaque cycle clos.
  const telemetrieV5 = () => ({
    cycles: [
      {
        index: 0,
        dureeEcouleeSecondes: 10_800,
        secondesEnRedescente: 700,
        paliersOuverts: 5,
        productionPicParSeconde: '321',
        foiGagnee: '17',
      },
    ],
    secondesEnRedescente: 1234,
    secondesDepuisDernierSucces: 5,
    intervallesEntreSucces: [60, 90],
  })

  it('MIGRATIONS[5] existe et ne supprime aucun champ : le compteur mort passe tel quel', () => {
    // Appelée DIRECTEMENT : un test qui ne passerait que par `deserialiser`
    // resterait vert sur une chaîne qui ne l'exécute pas (le repli comble).
    const migre = MIGRATIONS[5]({ telemetrie: telemetrieV5() }) as {
      telemetrie: { secondesEnRedescente: number; cycles: Record<string, unknown>[] }
    }
    expect(migre.telemetrie.secondesEnRedescente).toBe(1234)
    expect(migre.telemetrie.cycles[0].secondesEnRedescente).toBe(700)
  })

  it('une save v5 se relit en version 6, sa télémétrie vivante intacte, la mesure morte plus lue', () => {
    expect(VERSION_SAVE).toBe(6)
    const v5 = {
      versionSave: 5,
      contenu: { telemetrie: telemetrieV5(), permanent: { nombreEclosions: 3 } },
    } as unknown as SaveSerialisee
    const relu = deserialiser(v5, etatInitial(0))

    expect(relu.versionSave).toBe(6)
    expect(relu.permanent.nombreEclosions).toBe(3)
    expect(relu.telemetrie.intervallesEntreSucces).toEqual([60, 90])
    expect(relu.telemetrie.secondesDepuisDernierSucces).toBe(5)
    expect(relu.telemetrie.cycles).toHaveLength(1)
    expect(relu.telemetrie.cycles[0].dureeEcouleeSecondes).toBe(10_800)
    expect(relu.telemetrie.cycles[0].paliersOuverts).toBe(5)
    expect(relu.telemetrie.cycles[0].productionPicParSeconde.eq(321)).toBe(true)
    expect(relu.telemetrie.cycles[0].foiGagnee.eq(17)).toBe(true)

    // Le seul lecteur qu'avait le champ ne le lit plus : le relevé de cycle
    // n'en dérive plus aucune fraction.
    expect(relever(relu).cycles[0]).not.toHaveProperty('fractionEnRedescente')
  })
})
