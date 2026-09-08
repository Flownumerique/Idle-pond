/**
 * Les deux puits de la descente, et le coût de conviction — GDD §4.1, §6.4, §7.1.
 *
 * « Un puits, un levier. L'aménagement est payé par la technique ; la
 * reconviction garde sa formule et reste payée par la densité. Aucun coût n'a
 * deux leviers — c'est ce qui rend l'ensemble équilibrable. »
 *
 * Ce que ces tests protègent n'est pas une valeur — `f` est une graine, et le
 * simulateur montre qu'elle ne suffit pas à atteindre les 20–25 % — mais la
 * FORME : deux puits distincts, un levier chacun, et une densité qui paie le
 * retour sans jamais le rendre gratuit au premier cycle.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import {
  EXPOSANT_RECONVICTION_DENSITE,
  F_FRACTION_D_AMENAGEMENT,
} from '../src/noyau/constantes'
import {
  coutBaseDuPalier,
  coutDeConviction,
  coutDeDescente,
  estUnAmenagement,
} from '../src/noyau/economie'
import { etatInitial } from '../src/noyau/noyau'
import { BANCS } from '../src/donnees/paliers'
import type { EtatJeu } from '../src/noyau/types'

/** Un état identique au départ, sauf la profondeur déjà atteinte dans une vie passée. */
function ayantDejaAtteint(profondeur: number): EtatJeu {
  const etat = etatInitial(1)
  return { ...etat, permanent: { ...etat.permanent, profondeurMaxAtteinte: profondeur } }
}

describe('§4.1 — creuser et aménager sont deux puits', () => {
  it('un palier jamais atteint se paie plein tarif', () => {
    const etat = ayantDejaAtteint(0)
    expect(estUnAmenagement(etat, 3)).toBe(false)
    expect(coutDeDescente(etat, 3).eq(coutBaseDuPalier(3))).toBe(true)
  })

  it('un palier déjà atteint dans une vie passée se paie f fois moins', () => {
    const etat = ayantDejaAtteint(10)
    expect(estUnAmenagement(etat, 3)).toBe(true)
    const attendu = coutBaseDuPalier(3).mul(F_FRACTION_D_AMENAGEMENT)
    expect(coutDeDescente(etat, 3).eq(attendu)).toBe(true)
  })

  it('la frontière est exactement la profondeur maximale atteinte', () => {
    // Le dernier palier connu s'aménage ; le premier inconnu se creuse. Une
    // erreur d'un cran ici ferait repayer plein tarif le palier qu'on vient de
    // quitter, ou brader le premier vrai creusement de la vie.
    const etat = ayantDejaAtteint(10)
    expect(estUnAmenagement(etat, 9)).toBe(true)
    expect(estUnAmenagement(etat, 10)).toBe(false)
    expect(estUnAmenagement(etat, 11)).toBe(false)
  })

  it('la première vie ne connaît que le creusement', () => {
    // Rien n'a encore été atteint : aucun palier n'est un retour, et le cycle 1
    // se joue donc exactement comme avant l'introduction de `f`.
    const neuf = etatInitial(1)
    for (let palier = 0; palier < 8; palier += 1) {
      expect(estUnAmenagement(neuf, palier)).toBe(false)
    }
  })
})

describe('§7.1 — la conviction est payée par la densité', () => {
  const banc = BANCS[0]

  it('à densité nulle, la formule est neutre : le premier cycle n’est pas touché', () => {
    const etat = etatInitial(1)
    expect(etat.permanent.densites[banc.palier]).toBe(0)
    const sansDensite = coutDeConviction(etat, banc)
    expect(sansDensite.gt(0)).toBe(true)
  })

  it('une eau déjà chargée rend le banc moins cher à reconvaincre', () => {
    // « La densité conservée est la mémoire du monde, et c'est elle qui paie le
    // retour. » Ce n'est pas un bonus : c'est la réponse du GDD au problème du
    // prestige, et elle doit se voir sur le coût.
    const etat = etatInitial(1)
    const charge: EtatJeu = {
      ...etat,
      permanent: {
        ...etat.permanent,
        densites: etat.permanent.densites.map((_, i) => (i === banc.palier ? 99 : 0)),
      },
    }
    const nu = coutDeConviction(etat, banc)
    const memoire = coutDeConviction(charge, banc)

    expect(memoire.lt(nu)).toBe(true)
    const attendu = nu.div(Math.pow(1 + 99, EXPOSANT_RECONVICTION_DENSITE))
    expect(memoire.sub(attendu).abs().div(attendu).toNumber()).toBeLessThan(1e-9)
  })

  it('elle décroît avec la densité, sans jamais atteindre zéro', () => {
    const etat = etatInitial(1)
    const a = (densite: number): Decimal =>
      coutDeConviction(
        {
          ...etat,
          permanent: {
            ...etat.permanent,
            densites: etat.permanent.densites.map((_, i) => (i === banc.palier ? densite : 0)),
          },
        },
        banc,
      )

    expect(a(10).lt(a(0))).toBe(true)
    expect(a(1e6).lt(a(10))).toBe(true)
    expect(a(1e30).gt(0)).toBe(true)
  })

  it('aucun acquis ne s’ajoute à la densité sur ce coût', () => {
    // « Un puits, un levier. » Une technique ou un succès qui allègerait aussi
    // la conviction lui en donnerait deux, et le canon test vérifie qu'aucun ne
    // vise le terme. Ici on vérifie l'autre bout : la formule ne LIT aucun
    // acquis, donc en acquérir n'y change rien.
    const etat = etatInitial(1)
    const avecTout: EtatJeu = {
      ...etat,
      permanent: {
        ...etat.permanent,
        noeudsTechnique: ['tout', 'ce', 'qui', 'existe'],
        succes: Object.fromEntries(
          ['a', 'b', 'c'].map((id) => [id, { obtenuAuCycle: 0, registre: 'pente' as const }]),
        ),
      },
    }
    expect(coutDeConviction(avecTout, banc).eq(coutDeConviction(etat, banc))).toBe(true)
  })
})
