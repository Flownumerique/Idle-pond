/**
 * Parties de référence pour le portage Unity — spec 2026-09-27 §3.
 *
 * Lancé UNE fois, pendant que le TypeScript est encore la vérité :
 *   npx tsx tests/parite/generer-references.ts
 *
 * Tout ce qui sort d'ici porte déjà les noms du Codex (Souffle, insufflation,
 * renaissance) : c'est le C# qui doit s'y conformer, pas l'inverse. Un Decimal
 * s'écrit « mantisse e exposant », exactement, sans passer par toString()
 * qui arrondit les quasi-entiers.
 */
import { mkdirSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import Decimal from 'break_infinity.js'
import * as C from '../../src/noyau/constantes'
import { ameliorer, benir, creuser, debloquer, eclore, grandir, tick, tirer } from '../../src/noyau/noyau'
import { ASSISES, PALIERS_LIVRES } from '../../src/donnees/assises'
import { ESPECES } from '../../src/donnees/especes'
import { PALIERS } from '../../src/donnees/paliers'
import { BENEDICTIONS, BENEDICTION_GLOBALE_ID } from '../../src/donnees/benedictions'
import { SUCCES } from '../../src/donnees/succes/index'
import { simuler } from '../../src/simulateur/simulateur'
import { etatDeTravail } from '../etat-de-travail'
import { rejoue } from '../joueur'
import { d, insufflation, instantane } from './instantane'

const DOSSIER = fileURLToPath(new URL('../../../../Assets/IdlePond/Tests/Reference/', import.meta.url))

/* ─── Le lexique du Codex ───────────────────────────────────────────────── */

const terme = (t: string) =>
  ({ multiplicateur_benediction: 'multiplicateur_insufflation', benediction_globale: 'insufflation_globale', cout_benediction: 'cout_insufflation' })[t] ?? t
const constante = (nom: string) =>
  ({
    BENEDICTION_CIBLEE_PAR_RANG: 'INSUFFLATION_CIBLEE_PAR_RANG',
    BENEDICTION_GLOBALE_PAR_RANG: 'INSUFFLATION_GLOBALE_PAR_RANG',
    SOUFFLE_COUT_DE_BENEDICTION_CIBLEE: 'SOUFFLE_COUT_D_INSUFFLATION_CIBLEE',
    SOUFFLE_COUT_DE_BENEDICTION_GLOBALE: 'SOUFFLE_COUT_D_INSUFFLATION_GLOBALE',
    RATIO_COUT_DE_BENEDICTION: 'RATIO_COUT_D_INSUFFLATION',
    NOMBRE_D_ECLOSIONS_VISE: 'NOMBRE_DE_RENAISSANCES_VISE',
    CONTENANCE_PAR_ECLOSION: 'CONTENANCE_PAR_RENAISSANCE',
  })[nom] ?? nom

/* ─── Les scénarios — le C# les rejoue à l'identique (PariteTests) ──────── */

function scenarioSequence() {
  const SEQUENCE = [0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900]
  let etat = etatDeTravail(4242)
  const instants = [instantane(etat)]
  for (const dt of SEQUENCE) {
    etat = tick(etat, dt)
    instants.push(instantane(etat))
  }
  return { nom: 'sequence', instants }
}

function scenarioJoueur() {
  return { nom: 'joueur', instants: [60, 600, 1800, 3600].map((s) => instantane(rejoue(s))) }
}

function scenarioRenaissances() {
  let etat = etatDeTravail(777)
  const instants = [instantane(etat)]
  for (let cycle = 0; cycle < 3; cycle += 1) {
    etat = tick(etat, 3600)
    for (let i = 0; i < 3; i += 1) etat = grandir(etat)
    etat = eclore(etat)
    instants.push(instantane(etat))
    etat = benir(etat, BENEDICTION_GLOBALE_ID)
    etat = benir(etat, 'benediction-vairon')
    etat = creuser(etat)
    etat = debloquer(etat, 'vairon')
    for (let n = 0; n < 20; n += 1) etat = ameliorer(etat, 'vairon')
    etat = tick(etat, 600)
    instants.push(instantane(etat))
  }
  return { nom: 'renaissances', instants }
}

/* ─── Decimal ──────────────────────────────────────────────────────────── */

function referencesDecimal() {
  const valeurs = [
    '0', '1', '-1', '0.5', '2.4', '116', '299', '18', '0.1', '0.2', '1e-7', '123456789.123',
    '1e15', '1e21', '5e-324', '1.7976931348623157e308', '1e500', '-3.5e-400', '9.99999999999999e99',
    '47.6', '60', '1e12', '1e14', '3.3333333333333335', '-2.5', '1234.5', '1e-14', '2.5e-14',
  ].map((s) => new Decimal(s))
  const nombres = [0, 1, -1, 0.5, 2, 3.7, -1.5, 62, 1.15, 2.4, 1e300, 1e308, -1e308, 0.03]
  const cas: unknown[] = []
  // JSON ne sait pas écrire l'infini ni NaN : ils passent en chaîne, que le C# relit.
  const r = (x: Decimal | number | boolean) =>
    x instanceof Decimal ? d(x) : typeof x === 'number' && !Number.isFinite(x) ? String(x) : x
  valeurs.forEach((a, i) => {
    for (const op of ['neg', 'abs', 'recip', 'floor', 'round', 'ceil', 'toNumber', 'log10', 'exp'] as const) {
      if (op === 'exp' && Math.abs(a.toNumber()) > 1000) continue
      cas.push({ op, a: i, r: r((a[op] as () => Decimal | number)()) })
    }
    valeurs.forEach((b, j) => {
      for (const op of ['add', 'sub', 'mul', 'div', 'eq', 'lt', 'gt', 'lte', 'gte', 'max', 'min', 'cmp'] as const) {
        cas.push({ op, a: i, b: j, r: r((a[op] as (v: Decimal) => Decimal | number | boolean)(b)) })
      }
    })
    for (const x of nombres) {
      cas.push({ op: 'mulNombre', a: i, x, r: r(a.mul(x)) })
      cas.push({ op: 'addNombre', a: i, x, r: r(a.add(x)) })
      cas.push({ op: 'pow', a: i, x, r: r(a.pow(x)) })
    }
  })
  for (const base of [10, 2.4, 1.15, 2, 0.5]) {
    for (const x of [0, 1, 2, 3.5, 62, 2047, -3]) cas.push({ op: 'powStatique', base, x, r: r(Decimal.pow(base, x)) })
  }
  return { valeurs: valeurs.map(d), cas }
}

/* ─── PRNG ─────────────────────────────────────────────────────────────── */

function referencesPrng() {
  return {
    suites: [0, 1, 12345, 4242, 4294967295, 2654435769].map((graine) => {
      let prng = { graine }
      const tirages: [number, number][] = []
      for (let i = 0; i < 20; i += 1) {
        const [valeur, suivant] = tirer(prng)
        tirages.push([valeur, suivant.graine])
        prng = suivant
      }
      return { graine, tirages }
    }),
  }
}

/* ─── Constantes et données ────────────────────────────────────────────── */

function referencesConstantes() {
  const nombres = Object.fromEntries(
    Object.entries(C)
      .filter(([nom, v]) => typeof v === 'number' && nom !== 'VERSION_SAVE')
      .map(([nom, v]) => [constante(nom), v]),
  )
  return {
    nombres,
    SEUILS_DE_JALON: C.SEUILS_DE_JALON,
    COUTS_DE_NOEUD: C.COUTS_DE_NOEUD,
    multiplicateurDePalier: C.multiplicateurDePalier(),
    densiteExposant: C.densiteExposant(),
  }
}

function referencesDonnees() {
  return {
    paliersLivres: PALIERS_LIVRES,
    assises: ASSISES,
    especes: ESPECES,
    paliers: PALIERS,
    insufflations: BENEDICTIONS.map((b) => ({ id: insufflation(b.id), portee: b.portee, espece: b.espece })),
    succes: SUCCES.map((s) => ({
      id: s.id,
      famille: s.famille,
      visibilite: s.visibilite,
      assise: s.assise,
      declencheur: { ...s.declencheur, quoi: s.declencheur.quoi === 'eclosions' ? 'renaissances' : s.declencheur.quoi },
      effet:
        s.effet === null
          ? null
          : s.effet.genre === 'verbe'
            ? { genre: 'verbe', capacite: s.effet.capacite }
            : { genre: s.effet.genre, terme: terme(s.effet.terme), part: s.effet.part },
    })),
  }
}

/* ─── Écriture ─────────────────────────────────────────────────────────── */

function ecrire(nom: string, contenu: unknown) {
  writeFileSync(`${DOSSIER}${nom}.json`, JSON.stringify(contenu, null, 1) + '\n', 'utf8')
  console.log(`écrit ${nom}.json`)
}

mkdirSync(DOSSIER, { recursive: true })
ecrire('decimal', referencesDecimal())
ecrire('prng', referencesPrng())
ecrire('constantes', referencesConstantes())
ecrire('donnees', referencesDonnees())
const simulation = simuler(15, undefined, 7)
ecrire('parties', {
  scenarios: [scenarioSequence(), scenarioJoueur(), scenarioRenaissances()],
  simulation: {
    instant: instantane(simulation.etat),
    cyclesAcheves: simulation.cyclesAcheves,
    secondesActives: simulation.secondesActives,
    secondesEcoulees: simulation.secondesEcoulees,
  },
})
