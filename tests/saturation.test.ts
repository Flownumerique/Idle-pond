/**
 * La saturation de la jauge — GDD §2.4.
 *
 * « Alerte à 85 % : l'eau se trouble. Saturation à 100 % : la captation
 * s'arrête. Divergence non choisie après un délai : la ponte se déclenche
 * seule, et fixe moins d'acquis qu'une ponte choisie. »
 *
 * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
 * partie. C'est la seule pénalité du jeu, et elle est douce. » Ces tests
 * portent autant sur la douceur que sur le déclenchement : une divergence
 * qu'une absence ordinaire suffirait à provoquer violerait le pilier n° 2, et
 * passerait pourtant un test qui ne regarderait que le délai.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import {
  CAP_HORS_LIGNE_HEURES_MAXIMUM,
  DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES,
  PART_D_ACQUIS_FIXEE_PAR_DIVERGENCE_NON_CHOISIE,
  SEUIL_D_ALERTE_DE_CONTENANCE,
} from '../src/noyau/constantes'
import { creuser, eclore, tick } from '../src/noyau/noyau'
import {
  divergenceNonChoisieEstDue,
  eauTroublee,
  estSature,
  partDeContenance,
} from '../src/noyau/economie'
import type { EtatJeu } from '../src/noyau/types'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'

const H = 3600
const DELAI = DELAI_DE_DIVERGENCE_NON_CHOISIE_HEURES * H

/** Un état déjà plein, pour n'avoir pas à attendre le remplissage. */
function jaugePleine(): EtatJeu {
  const etat = etatDeTravail()
  return {
    ...etat,
    cycle: { ...etat.cycle, manaCourant: etat.permanent.contenanceMana },
  }
}

describe('§2.4 — l’alerte et la saturation', () => {
  it('l’eau se trouble au seuil, et pas avant', () => {
    const etat = etatDeTravail()
    const plafond = etat.permanent.contenanceMana
    const a = (part: number): EtatJeu => ({
      ...etat,
      cycle: { ...etat.cycle, manaCourant: plafond.mul(part) },
    })
    expect(eauTroublee(a(SEUIL_D_ALERTE_DE_CONTENANCE - 0.01))).toBe(false)
    expect(eauTroublee(a(SEUIL_D_ALERTE_DE_CONTENANCE))).toBe(true)
    expect(eauTroublee(a(1))).toBe(true)
  })

  it('la part de contenance ne dépasse jamais 1', () => {
    const etat = jaugePleine()
    expect(partDeContenance(etat)).toBe(1)
    expect(estSature(etat)).toBe(true)
    expect(partDeContenance(tick(etat, H))).toBe(1)
  })

  it('saturé, le stock ne monte plus — il ne gagne plus', () => {
    const etat = jaugePleine()
    const apres = tick(etat, 4 * H)
    expect(apres.cycle.manaCourant.eq(etat.permanent.contenanceMana)).toBe(true)
  })
})

describe('§2.4 — la divergence non choisie', () => {
  it('elle se déclenche seule au bout du délai, et pas avant', () => {
    const etat = jaugePleine()

    const avant = tick(etat, DELAI - H)
    expect(avant.permanent.nombreEclosions).toBe(0)
    expect(divergenceNonChoisieEstDue(avant)).toBe(false)

    const apres = tick(etat, DELAI + H)
    expect(apres.permanent.nombreEclosions).toBe(1)
    // Elle rend bien un début de vie : la jauge est repartie de zéro.
    expect(apres.cycle.secondesEnSaturation).toBe(0)
    expect(apres.cycle.paliersOuverts).toBe(1)
  })

  it('elle fixe moins d’acquis qu’une éclosion choisie', () => {
    // « fixe moins d'acquis qu'une ponte choisie ». Moins, jamais rien : le
    // joueur ne perd pas sa partie.
    const etat = jaugePleine()
    const auBord = tick(etat, DELAI - 1)

    const choisie = eclore(auBord).permanent.contenanceMana
    const subie = tick(auBord, 2).permanent.contenanceMana

    expect(subie.lt(choisie)).toBe(true)
    expect(subie.gt(auBord.permanent.contenanceMana)).toBe(true)

    const gainChoisi = choisie.div(auBord.permanent.contenanceMana).sub(1).toNumber()
    const gainSubi = subie.div(auBord.permanent.contenanceMana).sub(1).toNumber()
    expect(gainSubi / gainChoisi).toBeCloseTo(PART_D_ACQUIS_FIXEE_PAR_DIVERGENCE_NON_CHOISIE, 6)
  })

  it('le compteur repart à zéro dès qu’on dépense', () => {
    // C'est ce qui rend la pénalité évitable par n'importe quel geste, et donc
    // douce : il n'y a rien à surveiller, il suffit de jouer.
    const presque = tick(jaugePleine(), DELAI - H)
    expect(presque.cycle.secondesEnSaturation).toBeCloseTo(DELAI - H, 6)

    const apresDepense = creuser(presque)
    expect(apresDepense.cycle.manaCourant.lt(apresDepense.permanent.contenanceMana)).toBe(true)

    // Le compteur a bien été rendu. Il peut repartir tout de suite — l'eau de
    // cet état se remplit en quelques secondes — mais il repart de zéro, et
    // c'est ce qui compte : ce qui était accumulé est perdu.
    const repris = tick(apresDepense, 60).cycle.secondesEnSaturation
    expect(repris).toBeLessThan(presque.cycle.secondesEnSaturation)
    expect(repris).toBeLessThanOrEqual(60)
  })

  it('aucune absence, même au plafond maximal, ne suffit à la déclencher', () => {
    // Pilier n° 2 : « ne jamais punir l'absence ». Le plafond hors ligne borne
    // ce qu'une absence CRÉDITE ; si un seul crédit maximal suffisait à faire
    // pondre, la branche Entretien rendrait le jeu punitif en l'améliorant.
    const apresUneAbsenceMaximale = tick(jaugePleine(), CAP_HORS_LIGNE_HEURES_MAXIMUM * H)
    expect(apresUneAbsenceMaximale.permanent.nombreEclosions).toBe(0)
  })

  it('elle tombe au même instant quelle que soit la taille du pas', () => {
    // La règle vit dans le tick précisément pour ça. Si elle vivait au-dessus
    // du noyau, un pas de 8 h et 480 pas de 60 s ne la déclencheraient pas au
    // même moment, et le hors ligne tomberait avec elle.
    const depart = jaugePleine()
    const total = DELAI + 2 * H

    const enUnPas = tick(depart, total)

    let parPetitsPas = depart
    const NOMBRE_DE_PAS = 300
    for (let i = 0; i < NOMBRE_DE_PAS; i += 1) parPetitsPas = tick(parPetitsPas, total / NOMBRE_DE_PAS)

    expect(enUnPas.permanent.nombreEclosions).toBe(1)
    expect(parPetitsPas.permanent.nombreEclosions).toBe(1)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })

  it('l’instant de saturation est coupé exactement, même en cours de remplissage', () => {
    // Le cas qui compte : la jauge se remplit PENDANT le pas. L'instant est
    // résolu par dichotomie, et le décompte ne doit commencer qu'à ce
    // moment-là — ni au début du pas, ni à sa fin.
    const etat = etatDeTravail()
    const total = 6 * H

    // La contenance est calibrée sur la production réelle plutôt que devinée :
    // la moitié de ce que cet état produit sur la fenêtre, donc une saturation
    // qui tombe quelque part au milieu du pas.
    const aVide: EtatJeu = { ...etat, cycle: { ...etat.cycle, manaCourant: new Decimal(0) } }
    const produitSurLaFenetre = tick(
      { ...aVide, permanent: { ...aVide.permanent, contenanceMana: new Decimal('1e300') } },
      total,
    ).cycle.manaCourant

    const aMoitie: EtatJeu = {
      ...aVide,
      permanent: { ...aVide.permanent, contenanceMana: produitSurLaFenetre.div(2) },
    }

    const enUnPas = tick(aMoitie, total)
    let parPetitsPas = aMoitie
    for (let i = 0; i < 360; i += 1) parPetitsPas = tick(parPetitsPas, total / 360)

    expect(enUnPas.cycle.secondesEnSaturation).toBeGreaterThan(0)
    expect(enUnPas.cycle.secondesEnSaturation).toBeLessThan(total)
    comparerAToleranceFlottante(parPetitsPas, enUnPas)
  })
})
