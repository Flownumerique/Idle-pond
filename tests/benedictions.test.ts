/**
 * Les bénédictions — noyau v1.0 §4, spec 2026-09-17 §3.2.
 *
 * Permanentes, payées en Foi, et l'unique chose que la Foi achète tant que les
 * miracles sont gelés. Ciblée : multiplicateur sur une espèce. Globale :
 * additif sur le débit de base de toutes les espèces, présentes et futures.
 */
import Decimal from 'break_infinity.js'
import { describe, expect, it } from 'vitest'
import type { EtatJeu } from '../src/noyau/types'
import { etatInitial } from '../src/noyau/noyau'
import { BENEDICTION_GLOBALE_ID, BENEDICTIONS, benedictionCibleeDe, benedictionParId } from '../src/donnees/benedictions'
import {
  BENEDICTION_CIBLEE_PAR_RANG,
  BENEDICTION_GLOBALE_PAR_RANG,
  FOI_COUT_DE_BENEDICTION_CIBLEE,
  FOI_COUT_DE_BENEDICTION_GLOBALE,
  RATIO_COUT_DE_BENEDICTION,
} from '../src/noyau/constantes'
import { ESPECES } from '../src/donnees/especes'
import { etatDeTravail } from './etat-de-travail'
import { comparerAToleranceFlottante } from './outils'
import {
  coutDeBenediction,
  coutDeNiveau,
  debitBaseDeLEspece,
  debitBeni,
  detailDeCaptation,
  multiplicateurDeBenediction,
  productionDeLEspece,
  rangDeBenediction,
} from '../src/noyau/economie'

function benie(etat: EtatJeu, rangs: Record<string, number>): EtatJeu {
  return { ...etat, permanent: { ...etat.permanent, benedictions: { ...etat.permanent.benedictions, ...rangs } } }
}

describe('B1 — le registre', () => {
  it('la globale est trouvable par son identifiant, et chaque espèce a sa ciblée', () => {
    expect(benedictionParId(BENEDICTION_GLOBALE_ID)?.portee).toBe('globale')
    for (const espece of ESPECES) {
      const ciblee = benedictionCibleeDe(espece.id)
      expect(ciblee.portee).toBe('ciblee')
      expect(ciblee.espece).toBe(espece.id)
      expect(benedictionParId(ciblee.id)).toBe(ciblee)
    }
    expect(BENEDICTIONS).toHaveLength(ESPECES.length + 1)
  })

  it('les graines sont positives, et le coût croît', () => {
    expect(BENEDICTION_CIBLEE_PAR_RANG).toBeGreaterThan(0)
    expect(BENEDICTION_GLOBALE_PAR_RANG).toBeGreaterThan(0)
    expect(FOI_COUT_DE_BENEDICTION_CIBLEE).toBeGreaterThan(0)
    expect(FOI_COUT_DE_BENEDICTION_GLOBALE).toBeGreaterThan(0)
    expect(RATIO_COUT_DE_BENEDICTION).toBeGreaterThan(1)
  })
})

describe('B2 — ce qu’une bénédiction vaut', () => {
  const vairon = ESPECES[0]

  it('sans bénédiction : rang 0, débit béni = débit de base, multiplicateur 1', () => {
    const etat = etatDeTravail()
    expect(rangDeBenediction(etat, BENEDICTION_GLOBALE_ID)).toBe(0)
    expect(debitBeni(etat, vairon).eq(debitBaseDeLEspece(vairon))).toBe(true)
    expect(multiplicateurDeBenediction(etat, vairon)).toBe(1)
  })

  it('la globale ajoute k × rang au débit de base de toute espèce', () => {
    const etat = benie(etatDeTravail(), { [BENEDICTION_GLOBALE_ID]: 3 })
    for (const espece of ESPECES.slice(0, 3)) {
      const attendu = debitBaseDeLEspece(espece).add(BENEDICTION_GLOBALE_PAR_RANG * 3)
      expect(debitBeni(etat, espece).eq(attendu)).toBe(true)
    }
  })

  it('la ciblée multiplie SON espèce par (1 + c)^rang, et aucune autre', () => {
    const etat = benie(etatDeTravail(), { [benedictionCibleeDe(vairon.id).id]: 2 })
    expect(multiplicateurDeBenediction(etat, vairon)).toBeCloseTo(Math.pow(1 + BENEDICTION_CIBLEE_PAR_RANG, 2), 12)
    expect(multiplicateurDeBenediction(etat, ESPECES[1])).toBe(1)
  })

  it('la production de l’espèce lit les deux', () => {
    const nue = etatDeTravail()
    const etat = benie(nue, { [BENEDICTION_GLOBALE_ID]: 1, [benedictionCibleeDe(vairon.id).id]: 1 })
    const rapport = productionDeLEspece(etat, vairon).div(productionDeLEspece(nue, vairon)).toNumber()
    const attendu =
      (debitBaseDeLEspece(vairon).toNumber() + BENEDICTION_GLOBALE_PAR_RANG) /
      debitBaseDeLEspece(vairon).toNumber() *
      (1 + BENEDICTION_CIBLEE_PAR_RANG)
    expect(rapport).toBeCloseTo(attendu, 9)
  })

  it('bénir ne renchérit pas le niveau : le coût suit le débit NON béni', () => {
    const nue = etatDeTravail()
    const etat = benie(nue, { [BENEDICTION_GLOBALE_ID]: 5 })
    expect(coutDeNiveau(etat, vairon, 7).eq(coutDeNiveau(nue, vairon, 7))).toBe(true)
  })

  it('le coût en Foi est géométrique, et la globale et la ciblée ont chacune leur base', () => {
    const etat = etatInitial(1)
    const globale = benedictionParId(BENEDICTION_GLOBALE_ID)!
    const ciblee = benedictionCibleeDe(vairon.id)
    expect(coutDeBenediction(etat, globale).toNumber()).toBeCloseTo(FOI_COUT_DE_BENEDICTION_GLOBALE, 9)
    expect(coutDeBenediction(etat, ciblee).toNumber()).toBeCloseTo(FOI_COUT_DE_BENEDICTION_CIBLEE, 9)
    const deuxRangs = benie(etat, { [ciblee.id]: 2 })
    expect(coutDeBenediction(deuxRangs, ciblee).toNumber()).toBeCloseTo(
      FOI_COUT_DE_BENEDICTION_CIBLEE * Math.pow(RATIO_COUT_DE_BENEDICTION, 2),
      9,
    )
  })

  it('le détail de captation nomme les deux termes, chacun à sa source', () => {
    const etat = benie(etatDeTravail(), { [BENEDICTION_GLOBALE_ID]: 2, [benedictionCibleeDe(vairon.id).id]: 1 })
    const lignes = detailDeCaptation(etat, vairon)
    const globale = lignes.find((l) => l.terme === 'benediction_globale')
    const ciblee = lignes.find((l) => l.terme === 'multiplicateur_benediction')
    const attenduRatioGlobale = debitBeni(etat, vairon).div(debitBaseDeLEspece(vairon)).toNumber()
    expect(globale?.valeur).toBeCloseTo(attenduRatioGlobale, 9)
    expect(globale?.source).toEqual({ quoi: 'benediction', rang: 2 })
    expect(ciblee?.valeur).toBeCloseTo(1 + BENEDICTION_CIBLEE_PAR_RANG, 12)
    expect(ciblee?.source).toEqual({ quoi: 'benediction', rang: 1 })
  })

  it('le produit des lignes de détail reste exact quand la globale est active', () => {
    // Régression : `benediction_globale` comptait deux fois son effet (une
    // fois figé dans `taux_base`, une fois comme ligne additive) — le produit
    // valait 0.15 au lieu de 1.00 quand une globale était active. Aucun test,
    // ni ici ni dans `captation.test.ts`, n'exerçait alors cette combinaison.
    const etat = benie(etatDeTravail(), { [BENEDICTION_GLOBALE_ID]: 3 })
    const lignes = detailDeCaptation(etat, vairon)
    const produit = lignes.reduce((acc, ligne) => acc.mul(ligne.valeur), new Decimal(1))
    comparerAToleranceFlottante(produit, productionDeLEspece(etat, vairon))
  })
})
