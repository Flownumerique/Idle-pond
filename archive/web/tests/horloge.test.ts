/**
 * L'horloge (§10) — le seul module autorisé à lire l'heure système.
 *
 * Le compteur Entretien ne lit que les heures CRÉDITÉES, jamais le temps
 * écoulé. Sans ça, avancer son horloge farme l'arbre permanent — et c'est la
 * seule protection anti-triche nécessaire dans tout le jeu.
 */
import { describe, expect, it } from 'vitest'
import { CAP_HORS_LIGNE_HEURES_INITIAL, CAP_HORS_LIGNE_HEURES_MAXIMUM } from '../src/noyau/constantes'
import { capHorsLigneSecondes, horlogeFigee, secondesHorsLigneCreditees } from '../src/adaptateurs/horloge'

const H = 3600_000

describe('horloge', () => {
  it('crédite le temps écoulé tant qu’il reste sous le plafond', () => {
    expect(secondesHorsLigneCreditees(0, 2 * H, 6)).toBe(2 * 3600)
  })

  it('plafonne l’avance : une horloge poussée ne rapporte pas plus', () => {
    expect(secondesHorsLigneCreditees(0, 40 * H, 6)).toBe(6 * 3600)
    expect(secondesHorsLigneCreditees(0, 400 * H, 24)).toBe(24 * 3600)
  })

  it('ignore le recul d’horloge plutôt que de retirer quoi que ce soit', () => {
    // On ne punit jamais l'absence, et on ne punit pas davantage une horloge
    // qui recule : elle ne crédite rien, elle ne retire rien.
    expect(secondesHorsLigneCreditees(10 * H, 2 * H, 6)).toBe(0)
    expect(secondesHorsLigneCreditees(10 * H, 10 * H, 6)).toBe(0)
  })

  it('le plafond reste entre 6 h et 24 h, quoi qu’on lui demande', () => {
    expect(capHorsLigneSecondes(1)).toBe(CAP_HORS_LIGNE_HEURES_INITIAL * 3600)
    expect(capHorsLigneSecondes(999)).toBe(CAP_HORS_LIGNE_HEURES_MAXIMUM * 3600)
  })

  it('l’horloge figée rend les tests déterministes', () => {
    const horloge = horlogeFigee(1000)
    expect(horloge.maintenantMs()).toBe(1000)
    horloge.avancerMs(500)
    expect(horloge.maintenantMs()).toBe(1500)
  })
})
