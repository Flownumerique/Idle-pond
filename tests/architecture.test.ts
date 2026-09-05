/**
 * Test d'architecture — à écrire en premier (§5.3).
 *
 * « noyau/ ne doit importer AUCUN module hors de noyau/ et donnees/. Un test
 * qui parcourt les imports et échoue sinon. C'est la garantie la moins chère du
 * projet et celle qui sauve le simulateur. »
 */
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { sansCommentaires } from './outils'

const RACINE = resolve(__dirname, '..')
const NOYAU = join(RACINE, 'src/noyau')
const DONNEES = join(RACINE, 'src/donnees')

/**
 * `break_infinity.js` est la seule dépendance externe admise dans le noyau.
 * Elle est purement numérique — pas d'horloge, pas de DOM, pas d'état de
 * module — donc elle ne met en danger ni le déterminisme ni le simulateur, qui
 * sont les deux choses que cette règle protège. Toute autre importation nue est
 * un refus, y compris si elle paraît anodine.
 */
const PAQUETS_PURS_AUTORISES = new Set(['break_infinity.js'])

/** Ce que le §5.1 interdit nommément, plus ce qui casserait la pureté par la bande. */
const MOTIFS_IMPURS: readonly { readonly motif: RegExp; readonly quoi: string }[] = [
  { motif: /\bDate\s*\.\s*now\b/, quoi: 'Date.now' },
  { motif: /\bnew\s+Date\b/, quoi: 'new Date' },
  { motif: /\bMath\s*\.\s*random\b/, quoi: 'Math.random' },
  { motif: /\bperformance\s*\.\s*now\b/, quoi: 'performance.now' },
  { motif: /\bwindow\s*\./, quoi: 'window' },
  { motif: /\bdocument\s*\.\s*[a-zA-Z]/, quoi: 'document' },
  { motif: /\blocalStorage\b/, quoi: 'localStorage' },
  { motif: /\bsessionStorage\b/, quoi: 'sessionStorage' },
  { motif: /\bfetch\s*\(/, quoi: 'fetch' },
  { motif: /\bsetTimeout\b|\bsetInterval\b/, quoi: 'minuterie' },
  { motif: /\brequire\s*\(/, quoi: 'require' },
]

function fichiersTs(racine: string): string[] {
  return readdirSync(racine).flatMap((entree) => {
    const chemin = join(racine, entree)
    if (statSync(chemin).isDirectory()) return fichiersTs(chemin)
    return chemin.endsWith('.ts') || chemin.endsWith('.tsx') ? [chemin] : []
  })
}

function importsDe(source: string): string[] {
  const specificateurs: string[] = []
  const motif = /(?:import|export)[\s\S]*?from\s+['"]([^'"]+)['"]|import\s+['"]([^'"]+)['"]/g
  let trouve: RegExpExecArray | null
  while ((trouve = motif.exec(source)) !== null) {
    specificateurs.push(trouve[1] ?? trouve[2])
  }
  return specificateurs
}

describe('architecture', () => {
  const fichiersDuNoyau = fichiersTs(NOYAU)
  const fichiersDesDonnees = fichiersTs(DONNEES)

  it('le noyau contient des fichiers à vérifier', () => {
    expect(fichiersDuNoyau.length).toBeGreaterThan(5)
  })

  it("noyau/ n'importe rien hors de noyau/ et donnees/", () => {
    for (const fichier of fichiersDuNoyau) {
      const source = readFileSync(fichier, 'utf8')
      for (const specificateur of importsDe(source)) {
        const ou = relative(RACINE, fichier)
        if (!specificateur.startsWith('.')) {
          expect(
            PAQUETS_PURS_AUTORISES.has(specificateur),
            `${ou} importe le paquet « ${specificateur} », hors de la liste des paquets purs`,
          ).toBe(true)
          continue
        }
        const cible = resolve(fichier, '..', specificateur)
        const admis = cible.startsWith(NOYAU) || cible.startsWith(DONNEES)
        expect(admis, `${ou} importe « ${specificateur} », hors de noyau/ et donnees/`).toBe(true)
      }
    }
  })

  it('donnees/ reste du contenu pur, sans logique impure', () => {
    for (const fichier of fichiersDesDonnees) {
      const source = readFileSync(fichier, 'utf8')
      for (const specificateur of importsDe(source)) {
        const ou = relative(RACINE, fichier)
        if (!specificateur.startsWith('.')) {
          expect(PAQUETS_PURS_AUTORISES.has(specificateur), `${ou} importe « ${specificateur} »`).toBe(true)
          continue
        }
        const cible = resolve(fichier, '..', specificateur)
        expect(
          cible.startsWith(NOYAU) || cible.startsWith(DONNEES),
          `${ou} importe « ${specificateur} », hors de noyau/ et donnees/`,
        ).toBe(true)
      }
    }
  })

  it('noyau/ et donnees/ ne touchent ni horloge, ni hasard, ni DOM', () => {
    for (const fichier of [...fichiersDuNoyau, ...fichiersDesDonnees]) {
      const code = sansCommentaires(readFileSync(fichier, 'utf8'))
      for (const { motif, quoi } of MOTIFS_IMPURS) {
        expect(motif.test(code), `${relative(RACINE, fichier)} utilise ${quoi}`).toBe(false)
      }
    }
  })

  it("l'horloge système n'est lue que dans adaptateurs/horloge.ts", () => {
    const fautifs = fichiersTs(join(RACINE, 'src'))
      .filter((f) => !f.endsWith(join('adaptateurs', 'horloge.ts')))
      .filter((f) => /\bDate\s*\.\s*now\b/.test(sansCommentaires(readFileSync(f, 'utf8'))))
    expect(fautifs.map((f) => relative(RACINE, f))).toEqual([])
  })
})
