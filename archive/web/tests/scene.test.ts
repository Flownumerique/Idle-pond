/**
 * La scène — spec 2026-09-17 §3.3. Ce qui se teste en node : la projection
 * pure de l'état vers ce que Phaser dessine, et les données de palette.
 * Phaser lui-même n'est pas monté ici.
 */
import { readFileSync } from 'node:fs'
import { join, resolve } from 'node:path'
import { describe, expect, it } from 'vitest'
import { etatInitial, creuser, debloquer, ameliorer } from '../src/noyau/noyau'
import { ASSISES } from '../src/donnees/assises'
import { ESPECES } from '../src/donnees/especes'
import { echelleDuHeros, vueDeLaScene } from '../src/scene/vue'
import { MARQUE_PAR_ASSISE, PALETTES, paletteDe } from '../src/scene/palette'
import { etatDeTravail } from './etat-de-travail'

describe('C2 — la vue de la scène', () => {
  it('liste exactement les paliers ouverts, avec leur assise et leur espèce', () => {
    const etat = etatDeTravail()
    const vue = vueDeLaScene(etat)
    expect(vue.paliers).toHaveLength(etat.cycle.paliersOuverts)
    vue.paliers.forEach((p, i) => {
      expect(p.index).toBe(i)
      expect(ASSISES.some((a) => a.id === p.assise)).toBe(true)
    })
    const avecEspece = vue.paliers.filter((p) => p.espece !== null)
    expect(avecEspece.map((p) => p.espece?.id)).toEqual(
      ESPECES.filter((e) => e.palier < etat.cycle.paliersOuverts && etat.cycle.especes[e.id]?.debloquee).map((e) => e.id),
    )
  })

  it('une espèce non débloquée n’apparaît pas, même si son palier est ouvert', () => {
    let etat = etatInitial(1)
    etat = { ...etat, cycle: { ...etat.cycle, manaCourant: etat.cycle.manaCourant.mul(1e6) } }
    etat = creuser(creuser(creuser(etat)))
    expect(vueDeLaScene(etat).paliers.every((p) => p.espece === null)).toBe(true)
    etat = debloquer(etat, ESPECES[0].id)
    etat = ameliorer(etat, ESPECES[0].id)
    const premier = vueDeLaScene(etat).paliers[0]
    expect(premier.espece).toEqual({ id: ESPECES[0].id, rang: 0, niveau: 2 })
  })

  it('le héros porte son niveau, son échelle et ses couches', () => {
    const etat = {
      ...etatDeTravail(),
      cycle: { ...etatDeTravail().cycle, niveauDuHeros: 16 },
      permanent: { ...etatDeTravail().permanent, couches: ['noue'] },
    }
    const heros = vueDeLaScene(etat).heros
    expect(heros.niveau).toBe(16)
    expect(heros.echelle).toBeCloseTo(2, 9)
    expect(heros.couches).toEqual(['noue'])
  })

  it('l’échelle vaut 1 + 0,25·log₂(niveau)', () => {
    expect(echelleDuHeros(1)).toBe(1)
    expect(echelleDuHeros(2)).toBeCloseTo(1.25, 9)
    expect(echelleDuHeros(256)).toBeCloseTo(3, 9)
  })

  it('l’eau trouble et la saturation passent dans la vue', () => {
    const vue = vueDeLaScene(etatInitial(1))
    expect(vue.eauTroublee).toBe(false)
    expect(vue.sature).toBe(false)
  })

  it('la vue est sérialisable telle quelle — c’est ce qui traverse vers Phaser', () => {
    expect(() => JSON.stringify(vueDeLaScene(etatDeTravail()))).not.toThrow()
  })
})

describe('C2 — la palette', () => {
  it('six palettes, une par assise, et la lumière baisse en descendant', () => {
    expect(Object.keys(PALETTES)).toEqual(ASSISES.map((a) => a.id))
    const lumieres = ASSISES.map((a) => paletteDe(a.id).lumiere)
    for (let i = 1; i < lumieres.length; i += 1) expect(lumieres[i]).toBeLessThan(lumieres[i - 1])
  })

  it('une marque par assise, toutes différentes', () => {
    const marques = ASSISES.map((a) => MARQUE_PAR_ASSISE[a.id])
    expect(new Set(marques).size).toBe(ASSISES.length)
  })

  it('vue.ts et palette.ts n’importent pas Phaser', () => {
    const racine = resolve(__dirname, '..')
    for (const f of ['src/scene/vue.ts', 'src/scene/palette.ts']) {
      expect(readFileSync(join(racine, f), 'utf8')).not.toMatch(/from\s+['"]phaser['"]/)
    }
  })
})
