/**
 * Test de `sansChaines` — le balayage de canon.test.ts (§4, tâche 8) ne vaut
 * que ce que vaut cet outil. Un gabarit n'est pas de la simple prose : `${…}`
 * porte du code, et un motif mort qui reviendrait comme USAGE dans une
 * interpolation — un accès (`etat.bancs`), un appel (`acheterPlace(dt)`) —
 * doit rester visible. Sans ce test, une régression de `sansChaines` rendrait
 * le balayage aveugle sans qu'aucun test ne le dise (relecture du 2026-09-09).
 */
import { describe, expect, it } from 'vitest'
import { sansChaines } from './outils'

describe('sansChaines', () => {
  it('blanchit une chaîne simple ou double', () => {
    expect(sansChaines("'bancs'")).not.toContain('bancs')
    expect(sansChaines('"bancs"')).not.toContain('bancs')
  })

  it('blanchit le texte statique d’un gabarit sans interpolation', () => {
    expect(sansChaines('`Un banc te suit`')).not.toContain('banc')
  })

  it('préserve intact le contenu d’une interpolation — c’est du code', () => {
    // Les deux scénarios de réintroduction cités en relecture : un accès et
    // un appel, tous deux invisibles pour l'ancienne version de l'outil.
    expect(sansChaines('`${etat.bancs}`')).toContain('etat.bancs')
    expect(sansChaines('`${acheterPlace(dt)}`')).toContain('acheterPlace')
  })

  it('blanchit le texte statique AUTOUR d’une interpolation, sans toucher à celle-ci', () => {
    const resultat = sansChaines('`${part * 100}%`')
    expect(resultat).toContain('part * 100')
    expect(resultat).not.toContain('%')
  })

  it('ne rouvre pas le faux positif sur un gabarit où le motif mort est dans le texte, pas l’expression', () => {
    // Le cas réel de src/donnees/textes-provisoires.ts : « bancs » est dans
    // le texte statique (fiction légitime), `n` est dans l'expression (code).
    // Le motif doit disparaître, `n` doit rester.
    const resultat = sansChaines('`${n} crans tenus, tous bancs confondus`')
    expect(resultat).not.toContain('bancs')
    expect(resultat).toContain('${n}')
  })

  it('gère un gabarit imbriqué dans une interpolation sans se perdre', () => {
    const resultat = sansChaines('`${a ? `${b}` : c}`')
    expect(resultat).toContain('a')
    expect(resultat).toContain('b')
    expect(resultat).toContain('c')
  })
})
