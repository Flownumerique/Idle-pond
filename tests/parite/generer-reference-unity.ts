/**
 * Génère la référence de parité du portage Unity.
 *
 *   npm run reference:unity
 *
 * Tant que la version web reste la référence, le port C# doit rendre, sur les
 * mêmes entrées, ce que rend ce code-ci : mêmes états (à 1e-9 près), mêmes
 * succès aux mêmes instants, mêmes chaînes à l'écran, et les mêmes saves après
 * migration — à l'octet près. Ce script exécute le TypeScript et écrit ce qu'il
 * rend dans `unity/IdlePond/Assets/IdlePond/Tests/EditMode/Parite/`, où le test
 * `PariteAvecLeWebTests` le relit.
 *
 * À relancer après TOUTE modification du noyau, des données ou des textes, et à
 * committer avec elle : un écart entre les deux ports est alors visible dans le
 * diff de la référence avant de l'être dans un test rouge.
 *
 * Les écrans sont recalculés ici avec les MÊMES expressions que les composants
 * de `src/ui/` : ce fichier est le seul endroit où elles sont dupliquées, et il
 * n'existe que pour la parité.
 */
import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../../src/noyau/types'
import {
  acheterPlace,
  convaincre,
  creuser,
  eclore,
  etatInitial,
  tick,
  tickDetaille,
  tirer,
} from '../../src/noyau/noyau'
import {
  contenance,
  coutDeConviction,
  coutDeDescente,
  coutDePlace,
  detailDeCaptation,
  detailDuCanalAcclimate,
  eauTroublee,
  estBloque,
  estSature,
  estUnAmenagement,
  partDeContenance,
  productionAcclimateeDuPalier,
  productionDuBanc,
  productionTotaleParSeconde,
  toutEstCreuse,
} from '../../src/noyau/economie'
import { gainDeFoiPrevu } from '../../src/noyau/eclosion'
import { effectifCible } from '../../src/noyau/population'
import { progressionVersLeSucces, succesListables } from '../../src/noyau/succes'
import { DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES } from '../../src/noyau/constantes'
import { deserialiser, serialiser, type SaveSerialisee } from '../../src/adaptateurs/persistance'
import { ASSISES, PALIERS_LIVRES } from '../../src/donnees/assises'
import { BANCS, PALIERS } from '../../src/donnees/paliers'
import { SUCCES } from '../../src/donnees/succes/index'
import { texteDuSucces } from '../../src/donnees/textes-provisoires'
import { cout, duree, montant, nomDeLAssise, nomDeLEspece, profondeur, sourceDuTerme } from '../../src/ui/format'
import { simuler } from '../../src/simulateur/simulateur'
import { etatDeTravail } from '../etat-de-travail'
import { joueUneDemiHeure, rejoue } from '../joueur'

const H = 3600
const SORTIE = resolve(
  __dirname,
  '../../unity/IdlePond/Assets/IdlePond/Tests/EditMode/Parite/reference-web.json',
)

/* ─── Les écrans, tels que les composants React les calculent ─────────────── */

function ecran(etat: EtatJeu) {
  const mana = etat.cycle.manaCourant
  const plafond = contenance(etat)
  const plein = estSature(etat)

  const bancs = PALIERS.slice(0, etat.cycle.paliersOuverts).flatMap((palier) =>
    palier.bancs.map((banc) => {
      const vivant = etat.cycle.bancs[banc.id]
      const place = vivant?.place ?? 0
      const coutDuBanc = place === 0 ? coutDeConviction(etat, banc) : coutDePlace(etat, banc, place)
      return {
        bancId: banc.id,
        nom: place === 0 ? 'un banc s’attarde' : nomDeLEspece(banc.espece),
        profondeur: profondeur(banc.palier),
        effectif: place > 0 ? `${(vivant?.effectif ?? 0).toFixed(1)} / ${effectifCible(place)}` : null,
        production: place > 0 ? `+${montant(productionDuBanc(etat, banc))} / s` : null,
        action: place === 0 ? 'Convaincre' : 'Faire de la place',
        cout: cout(coutDuBanc),
        payable: mana.gte(coutDuBanc) && coutDuBanc.lte(plafond),
      }
    }),
  )

  const coutDuCreusement = coutDeDescente(etat, etat.cycle.paliersOuverts)
  const creusementPossible = !toutEstCreuse(etat) && coutDuCreusement.lte(plafond)

  const valeur = (v: number) => (v < 10 ? v.toFixed(3) : v.toFixed(1))
  const captations = bancs.map(({ bancId }) => {
    const banc = BANCS.find((b) => b.id === bancId)!
    const ligne = (l: ReturnType<typeof detailDeCaptation>[number]) => ({
      terme: l.terme,
      valeur: valeur(l.valeur),
      source: sourceDuTerme(l.source),
    })
    return {
      bancId,
      lignes: detailDeCaptation(etat, banc).map(ligne),
      natif: montant(productionDuBanc(etat, banc)),
      lignesAcclimatees: detailDuCanalAcclimate(etat, banc.palier).map(ligne),
      acclimate: montant(productionAcclimateeDuPalier(etat, banc.palier)),
    }
  })

  const liste = succesListables(etat, ASSISES[0].id)
  return {
    enTete: { foi: montant(etat.permanent.foi), eclosions: `${etat.permanent.nombreEclosions}` },
    contenance: {
      mana: montant(mana),
      plafond: `sur ${montant(plafond)}`,
      part: partDeContenance(etat),
      trouble: eauTroublee(etat),
      plein,
      debit: plein ? '+0 / s' : `+${montant(productionTotaleParSeconde(etat))} / s`,
      bloque: estBloque(etat),
    },
    mare: {
      titre: nomDeLAssise(ASSISES[0].id),
      bancs,
      creusement: toutEstCreuse(etat)
        ? { libelle: 'Il n’y a plus de roche à ouvrir ici', cout: null, possible: false }
        : {
            libelle: estUnAmenagement(etat, etat.cycle.paliersOuverts) ? 'Rendre le fond habitable' : 'Creuser plus bas',
            cout: cout(coutDuCreusement),
            possible: creusementPossible && !mana.lt(coutDuCreusement),
          },
    },
    captations,
    eclosion: { foi: montant(gainDeFoiPrevu(etat)), bloque: estBloque(etat) },
    succes: {
      acquis: [...liste.filter((e) => e.acquis)].reverse().map((e) => e.succes.id),
      enChemin: liste
        .filter((e) => !e.acquis && e.visibilite === 'ouvert')
        .map((e) => ({ id: e.succes.id, progression: progressionVersLeSucces(etat, e.succes) })),
      plusLoin: liste.filter((e) => !e.acquis && e.visibilite === 'ferme').map((e) => texteDuSucces(e.succes.id).nom),
      secrets: liste.filter((e) => !e.acquis && e.visibilite === 'secret').length,
    },
  }
}

/* ─── Les scénarios ──────────────────────────────────────────────────────── */

function jaugePleine(): EtatJeu {
  const etat = etatDeTravail()
  return { ...etat, cycle: { ...etat.cycle, manaCourant: etat.permanent.contenanceMana } }
}

function sequenceDeterministe(): EtatJeu {
  let etat = etatDeTravail(4242)
  for (const dt of [0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900]) etat = tick(etat, dt)
  return etat
}

/** Une vie jouée à la main : chaque acte, puis un peu de temps. */
function partieJouee(): EtatJeu {
  let etat = etatInitial(77, PALIERS_LIVRES)
  const vairon = PALIERS[0].bancs[0].id
  etat = convaincre(etat, vairon)
  etat = tick(etat, 90)
  for (let i = 0; i < 4; i += 1) etat = acheterPlace(tick(etat, 60), vairon)
  etat = tick(etat, 20 * 60)
  etat = creuser(etat)
  etat = convaincre(etat, PALIERS[1].bancs[0].id)
  etat = tick(etat, 3 * H)
  etat = eclore(etat)
  return tick(etat, 600)
}

const SAVES_WEB_ANCIENNES: readonly SaveSerialisee[] = [
  {
    versionSave: 1,
    contenu: {
      permanent: {
        succesDebloques: ['seuil-espece-1-1-10', 'acte-premiere-conviction', 'seuil-espece-1-3-25'],
        couches: ['assise-1'],
        nombreEclosions: 2,
      },
    },
  },
  {
    versionSave: 2,
    contenu: {
      permanent: {
        nombreEclosions: 3,
        succesDebloques: ['seuil-vairon-10', 'acte-premiere-conviction'],
        benedictions: { 'quelque-chose': 2 },
      },
    },
  },
  { versionSave: 3, contenu: serialiser(etatDeTravail()).contenu },
]

function valeurs<T>(liste: readonly number[], f: (v: number) => T): T[] {
  return liste.map(f)
}

const simulation15 = simuler(15)
const simulationLivree = simuler(2, undefined, 1, undefined, PALIERS_LIVRES)

const reference = {
  avertissement:
    'Fichier généré par tests/parite/generer-reference-unity.ts — ne pas modifier à la main. `npm run reference:unity`.',
  registreDesSucces: SUCCES.map((s) => s.id),
  textes: Object.fromEntries(SUCCES.map((s) => [s.id, texteDuSucces(s.id)])),
  prng: (() => {
    let prng = { graine: 12345 }
    const tirages: number[] = []
    for (let i = 0; i < 20; i += 1) {
      const [valeur, suivant] = tirer(prng)
      tirages.push(valeur)
      prng = suivant
    }
    return tirages
  })(),
  grandsNombres: (() => {
    const a = [
      new Decimal(0),
      new Decimal(1),
      new Decimal(2.4).pow(61),
      new Decimal('1.2345e30'),
      new Decimal(1).div(3),
      new Decimal(-7.5),
      new Decimal('9.99e-8'),
      new Decimal(1e21),
      new Decimal(123456.789),
    ]
    return {
      binaires: a.flatMap((x) =>
        a.map((y) => ({
          a: x.toString(),
          b: y.toString(),
          somme: x.add(y).toString(),
          difference: x.sub(y).toString(),
          produit: x.mul(y).toString(),
          quotient: y.eq(0) ? null : x.div(y).toString(),
          max: Decimal.max(x, y).toString(),
          lt: x.lt(y),
          gte: x.gte(y),
        })),
      ),
      unaires: a.map((x) => ({
        a: x.toString(),
        racine: x.pow(0.5).toString(),
        plancher: x.floor().toString(),
        fois115: x.mul(1.15).toString(),
        exponentielle: x.toExponential(2),
        nombre: x.toNumber(),
      })),
    }
  })(),
  format: {
    montant: valeurs([0, 1, 7.25, 9.99, 10.5, 999.9, 1000, 1234.5, 9999, 12345, 999999, 1.5e6, 2.4e9, 7.77e12, 3.3e15, 9.9e18, 1.2e21, 5e24], (v) =>
      montant(new Decimal(v)),
    ),
    cout: valeurs([0.4, 1, 5.55, 9.96, 10.2, 58.8, 999.1, 1000, 45678.9, 3.2e8], (v) => cout(new Decimal(v))),
    duree: valeurs([-1, 0, 12.5, 89.4, 90, 125, 3599, 5399, 5400, 7200, 86399, 86400, 400000], duree),
    profondeur: valeurs([0, 1, 2, 5, 11], profondeur),
    toFixed: valeurs([0, 0.05, 0.25, 1.005, 2.675, 1.45, 123.456, 9.9999, 0.0001234, 5e-7], (v) => [v.toFixed(1), v.toFixed(3)]),
  },
  couts: {
    descente: Array.from({ length: 63 }, (_, i) => coutDeDescente(etatInitial(1), i).toString()),
    descenteAmenagee: Array.from({ length: 63 }, (_, i) =>
      coutDeDescente({ ...etatInitial(1), permanent: { ...etatInitial(1).permanent, profondeurMaxAtteinte: 40 } }, i).toString(),
    ),
    place: Array.from({ length: 80 }, (_, i) => coutDePlace(etatInitial(1), BANCS[3], i + 1).toString()),
    conviction: BANCS.map((b) => coutDeConviction(etatDeTravail(), b).toString()),
  },
  etats: {
    etatDeTravail: serialiser(etatDeTravail()),
    apresHuitHeures: serialiser(tick(etatDeTravail(), 8 * H)),
    apresCentMillisecondes: serialiser(tick(etatDeTravail(), 0.1)),
    sequenceDeterministe: serialiser(sequenceDeterministe()),
    divergenceNonChoisie: serialiser(tick(jaugePleine(), DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES * H + 2 * H)),
    rejoueUneDemiHeure: serialiser(rejoue(1800)),
    partieJouee: serialiser(partieJouee()),
    declenchesAuPremierTick: tickDetaille(eclore(etatInitial(1)), 0.1).declenches,
  },
  demiHeure: joueUneDemiHeure(),
  simulations: {
    livree: {
      etat: serialiser(simulationLivree.etat),
      tempsActifSecondes: simulationLivree.tempsActifSecondes,
      sessions: simulationLivree.sessions.length,
    },
    quinze: {
      etat: serialiser(simulation15.etat),
      tempsActifSecondes: simulation15.tempsActifSecondes,
      tempsEcouleSecondes: simulation15.tempsEcouleSecondes,
      sessions: simulation15.sessions.length,
      cycles: simulation15.releve.cycles,
    },
  },
  ecrans: {
    depart: ecran(etatInitial(1, PALIERS_LIVRES)),
    etatDeTravail: ecran(etatDeTravail()),
    demiHeure: ecran(rejoue(1800)),
    partieJouee: ecran(partieJouee()),
    jaugePleine: ecran(jaugePleine()),
    simulationLivree: ecran(simulationLivree.etat),
  },
  migrations: SAVES_WEB_ANCIENNES.map((save) => ({
    save,
    // Ce que le web relit de cette save, puis réécrit : la chaîne que le port C#
    // doit produire à l'octet près. Le repli a une graine fixe pour que la
    // comparaison ne dépende pas de l'heure.
    reecrite: JSON.stringify(serialiser(deserialiser(save, etatInitial(0, PALIERS_LIVRES)))),
  })),
  saveDuMagasin: JSON.stringify({ ...serialiser(partieJouee()), dernierInstantMs: 1_700_000_000_000 }),
}

mkdirSync(dirname(SORTIE), { recursive: true })
writeFileSync(SORTIE, `${JSON.stringify(reference, null, 1)}\n`)
console.log(`Référence de parité écrite : ${SORTIE}`)
