/**
 * Tests de canon — les phrases qui, si elles sont violées, invalident le build.
 *
 * Ils portent sur des registres aujourd'hui vides. C'est délibéré : le §8 note
 * que le typage, la visibilité et le registre figé « ne se rétrofitent pas », et
 * la même chose vaut pour les gardes. Un test de frontière ajouté après le
 * contenu ne fait que constater les dégâts.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join, relative, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import {
  BUDGET_DE_VERBES_ARBRE,
  BUDGET_DE_VERBES_TOTAL,
  CROISSANCE_PAR_CYCLE_VISEE,
  D_PRODUCTION_PAR_PALIER,
  F_TARIF_REDESCENTE,
  G_COUT_PALIER,
  NOMBRE_DE_PALIERS,
  NOMBRE_D_ESPECES_DE_BASE,
  PALIERS_PAR_CYCLE_VISE,
  RAPPORT_G_SUR_D,
} from '../src/noyau/constantes'
import { TERMES_DE_CONFORT, TERMES_DE_COUT, TERMES_DE_PRODUCTION } from '../src/noyau/types'
import type { CapaciteId } from '../src/noyau/types'
import { NOEUDS_TECHNIQUE } from '../src/donnees/noeuds-technique'
import { BENEDICTIONS } from '../src/donnees/benedictions'
import { SUCCES } from '../src/donnees/succes/index'
import { PALIERS } from '../src/donnees/paliers'
import { ESPECES } from '../src/donnees/especes'
import { ASSISES } from '../src/donnees/assises'
import { FRACTION_CONSERVEE } from '../src/noyau/eclosion'
import { sansCommentaires } from './outils'

const RACINE = resolve(__dirname, '..')

function fichiersTs(racine: string): string[] {
  return readdirSync(racine).flatMap((entree) => {
    const chemin = join(racine, entree)
    if (statSync(chemin).isDirectory()) return fichiersTs(chemin)
    return chemin.endsWith('.ts') || chemin.endsWith('.tsx') ? [chemin] : []
  })
}

describe('§4.3 — la frontière technique / bénédiction', () => {
  it('aucun nœud de technique ne monte une production', () => {
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature !== 'chiffre') continue
      const admis = [...TERMES_DE_COUT, ...TERMES_DE_CONFORT] as string[]
      expect(admis, `le nœud ${noeud.id} cible ${noeud.effet.terme}`).toContain(noeud.effet.terme)
    }
  })

  it('aucune bénédiction ne réduit un coût ni n’automatise', () => {
    for (const benediction of BENEDICTIONS) {
      expect(TERMES_DE_PRODUCTION as string[], `la bénédiction ${benediction.id}`).toContain(
        benediction.effet.terme,
      )
    }
  })

  it('aucun effet chiffré ne flotte sans terme nommé', () => {
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature !== 'chiffre') continue
      expect(typeof noeud.effet.terme, `le nœud ${noeud.id}`).toBe('string')
    }
  })
})

describe('§7.5 — les trois règles dures de l’arbre', () => {
  it('une capacité a exactement une source', () => {
    const parLArbre = new Set<CapaciteId>()
    for (const noeud of NOEUDS_TECHNIQUE) {
      if (noeud.effet.nature === 'verbe') parLArbre.add(noeud.effet.capacite)
    }
    for (const succes of SUCCES) {
      if (succes.effet?.nature !== 'verbe') continue
      expect(
        parLArbre.has(succes.effet.capacite),
        `${succes.effet.capacite} est atteignable par l’arbre ET par le succès ${succes.id}`,
      ).toBe(false)
    }
  })

  it('le budget de verbes est commun et tenu', () => {
    const verbesDeLArbre = NOEUDS_TECHNIQUE.filter((n) => n.effet.nature === 'verbe').length
    const verbesDesSucces = SUCCES.filter((s) => s.effet?.nature === 'verbe').length
    expect(verbesDeLArbre).toBeLessThanOrEqual(BUDGET_DE_VERBES_ARBRE)
    expect(verbesDeLArbre + verbesDesSucces).toBeLessThanOrEqual(BUDGET_DE_VERBES_TOTAL)
  })
})

describe('§13 — les valeurs fixées et leurs dérivations', () => {
  it('g/D se dérive de la croissance et des paliers par cycle, jamais saisi', () => {
    expect(RAPPORT_G_SUR_D).toBeCloseTo(Math.pow(CROISSANCE_PAR_CYCLE_VISEE, 1 / PALIERS_PAR_CYCLE_VISE), 12)
    // Le §6.3 cite « 1.039 » ; la dérivation exacte donne 1.03833. L'écart est
    // un arrondi du document, pas un désaccord : c'est bien la dérivation qui
    // fait foi, le §13.2 classant g/D en « recalculer, ne pas saisir ».
    expect(Math.abs(RAPPORT_G_SUR_D - 1.039)).toBeLessThan(1e-3)
  })

  it('D vaut g / 1.039, soit 2.31', () => {
    expect(D_PRODUCTION_PAR_PALIER).toBeCloseTo(G_COUT_PALIER / RAPPORT_G_SUR_D, 12)
    expect(D_PRODUCTION_PAR_PALIER).toBeCloseTo(2.31, 2)
  })

  it('D ≠ g, sinon la durée d’un cycle est plate', () => {
    expect(D_PRODUCTION_PAR_PALIER).not.toBeCloseTo(G_COUT_PALIER, 2)
  })

  it('f = 1 : reset complet, aucune fraction conservée', () => {
    expect(F_TARIF_REDESCENTE).toBe(1)
    expect(FRACTION_CONSERVEE).toBe(0)
  })

  it('62 paliers, 6 assises, 21 espèces de base', () => {
    expect(PALIERS.length).toBe(NOMBRE_DE_PALIERS)
    expect(ASSISES.length).toBe(6)
    expect(ESPECES.length).toBe(NOMBRE_D_ESPECES_DE_BASE)
  })

  it('chaque palier appartient à une assise et porte au moins un banc', () => {
    for (const palier of PALIERS) {
      expect(ASSISES.some((a) => a.id === palier.assise)).toBe(true)
      expect(palier.bancs.length).toBeGreaterThan(0)
    }
  })
})

describe('§3 — le lexique s’applique au code, pas seulement à la prose', () => {
  const PERIMES: readonly { readonly motif: RegExp; readonly quoi: string }[] = [
    { motif: /\bponte\b/i, quoi: 'ponte (périmé — décrit l’acte inverse de l’éclosion)' },
    { motif: /\bprestige\b/i, quoi: 'prestige' },
    { motif: /\brebirth\b/i, quoi: 'rebirth' },
    { motif: /\bgemmes?\b/i, quoi: 'gemme' },
    { motif: /\bperles?\b/i, quoi: 'perle' },
    { motif: /\bcorail\b/i, quoi: 'Corail' },
    { motif: /\bpart_?m[uû]re\b/i, quoi: 'part mûre (modèle de maturation supprimé)' },
    { motif: /\bbuyFish\b/, quoi: 'buyFish (achat par exemplaire)' },
    { motif: /\bpondDepth\b|\bzoneId?\b|\bbiomes?\b/i, quoi: 'ancien vocabulaire de zones/biomes' },
    { motif: /\blayers?\b/i, quoi: 'layer (dire assise)' },
    { motif: /\b[ée]tages?\b/i, quoi: 'étage (toléré à l’oral, jamais dans le code)' },
    { motif: /\bstrates?\b/i, quoi: 'strate (réservé au plan des dieux, interdit pour de la roche)' },
  ]

  it('aucun terme périmé ne subsiste dans le code de src/', () => {
    const fautes: string[] = []
    for (const fichier of fichiersTs(join(RACINE, 'src'))) {
      const code = sansCommentaires(readFileSync(fichier, 'utf8'))
      for (const { motif, quoi } of PERIMES) {
        if (motif.test(code)) fautes.push(`${relative(RACINE, fichier)} : ${quoi}`)
      }
    }
    expect(fautes).toEqual([])
  })
})
