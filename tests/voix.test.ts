/**
 * Les paliers de voix et le registre figé — GDD §13.1 et §14.5.
 *
 * « Une entrée est rédigée dans la langue que le héros avait au moment où il
 * l'a obtenue, et n'est jamais réécrite. »
 *
 * Le §14.9 range cette propriété parmi les trois qui « ne se rétrofitent pas ».
 * C'est vrai au sens le plus dur : le palier de voix d'un succès déjà tombé
 * n'est reconstituable depuis aucune autre donnée. S'il n'est pas inscrit au
 * moment du déclenchement, il est perdu — et aucun test ajouté plus tard ne
 * fera autre chose que constater la perte.
 */
import { describe, expect, it } from 'vitest'
import {
  FRANCHISSEMENTS_POUR_LES_DIRECTIVES,
  FRANCHISSEMENTS_POUR_LES_SIGNES,
} from '../src/noyau/constantes'
import { eclore, etatInitial, tick } from '../src/noyau/noyau'
import { palierDeVoix, palierDeVoixApres, voixAuMoins } from '../src/noyau/voix'
import { convaincre } from '../src/noyau/noyau'
import { BANCS } from '../src/donnees/paliers'
import { etatDeTravail } from './etat-de-travail'
import { SUCCES } from '../src/donnees/succes/index'
import type { EtatJeu } from '../src/noyau/types'

const H = 3600

/** Enchaîne `n` cycles complets, chacun assez long pour valoir un franchissement. */
function apresFranchissements(n: number): EtatJeu {
  let etat = etatInitial(1)
  for (let i = 0; i < n; i += 1) etat = eclore(tick(etat, 3 * H))
  return etat
}

describe('§13.1 — les paliers de voix', () => {
  it('la pente au départ, les signes au premier franchissement, les directives au troisième', () => {
    expect(palierDeVoixApres(0)).toBe('pente')
    expect(palierDeVoixApres(FRANCHISSEMENTS_POUR_LES_SIGNES - 1)).toBe('pente')
    expect(palierDeVoixApres(FRANCHISSEMENTS_POUR_LES_SIGNES)).toBe('signes')
    expect(palierDeVoixApres(FRANCHISSEMENTS_POUR_LES_DIRECTIVES - 1)).toBe('signes')
    expect(palierDeVoixApres(FRANCHISSEMENTS_POUR_LES_DIRECTIVES)).toBe('directives')
  })

  it('le dialogue reste inatteignable : il vient du relais, pas d’un compte', () => {
    // §12.2, contenu de la v1.0. Il figure dans le type parce que le registre
    // d'un succès est figé pour toujours : un type qui gagnerait une valeur
    // plus tard rendrait les saves d'aujourd'hui ambiguës.
    expect(palierDeVoixApres(1000)).not.toBe('dialogue')
  })

  it('la voix ne redescend jamais', () => {
    let precedent = -1
    const ordre = ['pente', 'signes', 'directives', 'dialogue']
    for (let n = 0; n < 20; n += 1) {
      const rang = ordre.indexOf(palierDeVoixApres(n))
      expect(rang).toBeGreaterThanOrEqual(precedent)
      precedent = rang
    }
  })

  it('voixAuMoins ordonne les paliers', () => {
    expect(voixAuMoins('directives', 'signes')).toBe(true)
    expect(voixAuMoins('signes', 'signes')).toBe(true)
    expect(voixAuMoins('pente', 'signes')).toBe(false)
  })
})

describe('§14.5 — le registre figé', () => {
  it('une entrée obtenue sous la pente reste sous la pente, des cycles plus tard', () => {
    // C'est tout l'objet du système : « la bibliothèque devient la preuve du
    // chemin parcouru — le joueur qui relit ses vieilles entrées voit à quel
    // point il se trompait, et personne n'a eu besoin de le lui dire ».
    let etat = convaincre(etatInitial(1), BANCS[0].id)
    etat = tick(etat, 1)

    const premiers = Object.entries(etat.permanent.succes)
    expect(premiers.length).toBeGreaterThan(0)
    for (const [, entree] of premiers) {
      expect(entree.registre).toBe('pente')
      expect(entree.obtenuAuCycle).toBe(0)
    }

    for (let i = 0; i < FRANCHISSEMENTS_POUR_LES_DIRECTIVES; i += 1) etat = eclore(tick(etat, 3 * H))
    expect(palierDeVoix(etat)).toBe('directives')

    // Rien n'a été réécrit.
    for (const [id, entree] of premiers) {
      expect(etat.permanent.succes[id]).toEqual(entree)
    }
  })

  it('une entrée tombée plus tard porte la langue de ce moment-là', () => {
    let etat = apresFranchissements(FRANCHISSEMENTS_POUR_LES_SIGNES)
    expect(palierDeVoix(etat)).toBe('signes')

    const avant = new Set(Object.keys(etat.permanent.succes))
    etat = tick(convaincre(etat, BANCS[0].id), 1)
    const nouveaux = Object.entries(etat.permanent.succes).filter(([id]) => !avant.has(id))

    expect(nouveaux.length).toBeGreaterThan(0)
    for (const [, entree] of nouveaux) {
      expect(entree.registre).toBe('signes')
      expect(entree.obtenuAuCycle).toBe(FRANCHISSEMENTS_POUR_LES_SIGNES)
    }
  })

  it('un succès acquis ne se ré-obtient jamais, et son entrée ne bouge pas', () => {
    // §14.4 : « les succès ne sont pas re-déclenchables. Un succès marque la
    // première fois. » Sinon le gain devient une rente indexée sur le nombre
    // de pontes, et la courbe casse.
    let etat = tick(convaincre(etatInitial(1), BANCS[0].id), 1)
    const premiere = { ...etat.permanent.succes }

    etat = eclore(tick(etat, 3 * H))
    etat = tick(convaincre(etat, BANCS[0].id), 1)

    for (const [id, entree] of Object.entries(premiere)) {
      expect(etat.permanent.succes[id]).toEqual(entree)
    }
  })

  it('la table est ordonnée par le registre, jamais par l’arrivée', () => {
    // `etatDeTravail` a six paliers ouverts et des bancs peuplés : de quoi
    // faire tomber plusieurs succès, ce qu'une partie neuve ne permet pas —
    // elle démarre avec de quoi convaincre un seul banc.
    const etat = tick(etatDeTravail(), 600)

    const obtenus = Object.keys(etat.permanent.succes)
    expect(obtenus.length).toBeGreaterThan(1)
    expect(obtenus).toEqual(
      SUCCES.filter((s) => etat.permanent.succes[s.id] !== undefined).map((s) => s.id),
    )
  })

  it('l’ordre des entrées ne dépend pas de la taille du pas', () => {
    // L'ordre des clefs d'un objet est celui de leur insertion, et le test de
    // déterminisme compare des chaînes de sérialisation. Deux succès franchis
    // dans le même intervalle arrivent ensemble sous un grand pas et l'un après
    // l'autre sous de petits pas : insérer à l'arrivée ferait diverger deux
    // parties identiques sur leur seule sauvegarde.
    const depart = etatDeTravail()

    const enUnPas = tick(depart, 600)
    let parPetitsPas = depart
    for (let i = 0; i < 600; i += 1) parPetitsPas = tick(parPetitsPas, 1)

    expect(Object.keys(parPetitsPas.permanent.succes)).toEqual(
      Object.keys(enUnPas.permanent.succes),
    )
  })
})
