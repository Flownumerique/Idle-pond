/**
 * Les deux puits de la descente — GDD §4.1 et §6.4.
 *
 * « Un puits, un levier. Aucun coût n'a deux leviers — c'est ce qui rend
 * l'ensemble équilibrable. »
 *
 * Ce que ces tests protègent n'est pas une valeur — `f` est une graine, et le
 * simulateur montre qu'elle ne suffit pas à atteindre les 20–25 % — mais la
 * FORME : deux puits distincts, un levier chacun.
 *
 * Le second bloc, « la conviction est payée par la densité » (§7.1), est parti
 * le 2026-09-09 avec le modèle à population : il n'y a plus de banc à
 * reconvaincre, et le déblocage d'une espèce est une fraction du coût de son
 * palier (noyau v1.0 §1.3). La tâche 6 réécrit ce qui reste ici.
 */
import { describe, expect, it } from 'vitest'
import { F_FRACTION_D_AMENAGEMENT } from '../src/noyau/constantes'
import { coutBaseDuPalier, coutDeDescente, estUnAmenagement } from '../src/noyau/economie'
import { etatInitial } from '../src/noyau/noyau'
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
