/**
 * Référence du crédit hors ligne pour le portage Unity — obligation n° 2 de la revue
 * finale du plan 1 : aucune partie de `parties.json` n'exerce `heuresHorsLigneCreditees`
 * (toujours 0). Ce fichier comble le trou, dans un fichier À PART, parce que les
 * références existantes sont figées.
 *
 * Lancé UNE fois, avant l'archivage du TypeScript :
 *   npx tsx tests/parite/generer-hors-ligne.ts
 *
 * Le C# les rejoue dans `PariteHorsLigneTests`.
 */
import { writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import type { EtatJeu } from '../../src/noyau/types'
import { ameliorer, creuser, debloquer, eclore, grandir, tick } from '../../src/noyau/noyau'
import { capHorsLigneCourantHeures, crediterHorsLigne } from '../../src/adaptateurs/hors-ligne'
import { etatDeTravail } from '../etat-de-travail'
import { instantane } from './instantane'

const FICHIER = fileURLToPath(new URL('../../Assets/IdlePond/Tests/Reference/hors-ligne.json', import.meta.url))

const DEPART_MS = 1_700_000_000_000
const HEURE_MS = 3_600_000

/**
 * Les absences, dans l'ordre où elles sont créditées. `ecouleMs` peut être négatif
 * (horloge reculée) ou nul. La renaissance intercalée vérifie que les heures
 * créditées sont permanentes.
 */
const ETAPES: ({ genre: 'absence'; ecouleMs: number } | { genre: 'renaissance' })[] = [
  { genre: 'absence', ecouleMs: 2 * HEURE_MS }, // sous le plafond
  { genre: 'absence', ecouleMs: 1_234_567 }, // des millisecondes qui ne tombent pas rond
  { genre: 'absence', ecouleMs: 30 * HEURE_MS }, // au-delà : plafonné à 6 h
  { genre: 'absence', ecouleMs: -5 * 60_000 }, // horloge reculée : rien, jamais à l'envers
  { genre: 'absence', ecouleMs: 0 },
  { genre: 'renaissance' },
  { genre: 'absence', ecouleMs: 3 * HEURE_MS }, // après la renaissance
  { genre: 'absence', ecouleMs: 100 * HEURE_MS },
]

/** Une partie qui a de quoi produire pendant l'absence. */
function partieEnCours(): EtatJeu {
  let etat = etatDeTravail(9001)
  etat = tick(etat, 600)
  etat = creuser(etat)
  etat = debloquer(etat, 'vairon')
  for (let n = 0; n < 10; n += 1) etat = ameliorer(etat, 'vairon')
  return tick(etat, 600)
}

function scenario() {
  let etat = partieEnCours()
  let instant = DEPART_MS
  const pas: unknown[] = [{ etape: 'depart', instant: instantane(etat) }]
  for (const e of ETAPES) {
    if (e.genre === 'renaissance') {
      etat = tick(etat, 3600)
      for (let i = 0; i < 3; i += 1) etat = grandir(etat)
      etat = eclore(etat)
      pas.push({ etape: 'renaissance', instant: instantane(etat) })
      continue
    }
    const maintenant = instant + e.ecouleMs
    const capHeures = capHorsLigneCourantHeures(etat)
    const retour = crediterHorsLigne(etat, instant, maintenant)
    etat = retour.etat
    // Comme Partie : l'instant de référence avance, même quand l'horloge a reculé.
    instant = maintenant
    pas.push({
      etape: 'absence',
      dernierInstantMs: maintenant - e.ecouleMs,
      maintenantMs: maintenant,
      capHeures,
      secondesCreditees: retour.secondesCreditees,
      instant: instantane(etat),
    })
  }
  return pas
}

writeFileSync(FICHIER, JSON.stringify({ pas: scenario() }, null, 1) + '\n', 'utf8')
console.log('écrit hors-ligne.json')
