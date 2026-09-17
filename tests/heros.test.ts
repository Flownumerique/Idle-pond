/**
 * L'axe héros — spec 2026-09-17 §3.1.
 *
 * Le héros GRANDIT pendant la vie, avec du mana : c'est le quatrième achat du
 * noyau v1.0 §1.2 amendé. Son niveau se reperd à l'éclosion (f = 1), et ce
 * qu'il apporte est un multiplicateur global NOMMÉ, jamais un facteur flottant.
 */
import { describe, expect, it } from "vitest"
import Decimal from "break_infinity.js"
import {
  BONUS_PAR_NIVEAU_DU_HEROS,
  DEBIT_RATIO_ESPECE,
  D_PRODUCTION_PAR_PALIER,
  ESPECE_TOUS_LES_N_PALIERS,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  NIVEAU_DU_HEROS_AU_DEPART,
  RATIO_COUT_DE_CROISSANCE,
  COUT_CREUSER_AU_PALIER_1,
  G_COUT_PALIER,
  DEBIT_HEROS,
  multiplicateurDePalier,
} from "../src/noyau/constantes"
import { TERMES_DE_COUT, TERMES_DE_PRODUCTION } from "../src/noyau/types"
import { etatInitial, grandir, eclore, tick } from "../src/noyau/noyau"
import {
  coutDeCroissance,
  detailDuHeros,
  multiplicateurDuHeros,
  multiplicateursGlobaux,
  productionDuHeros,
  productionTotaleParSeconde,
} from "../src/noyau/economie"
import { etatDeTravail } from "./etat-de-travail"
import { comparerAToleranceFlottante } from "./outils"
import type { EtatJeu } from "../src/noyau/types"

function auNiveau(etat: EtatJeu, niveauDuHeros: number): EtatJeu {
  return { ...etat, cycle: { ...etat.cycle, niveauDuHeros } }
}

describe("A1 — les graines de l'axe héros", () => {
  it("le héros démarre au niveau 1, dans le cycle", () => {
    expect(NIVEAU_DU_HEROS_AU_DEPART).toBe(1)
    expect(etatInitial(1).cycle.niveauDuHeros).toBe(NIVEAU_DU_HEROS_AU_DEPART)
  })

  it("le bonus par niveau est une graine strictement positive et modeste", () => {
    expect(BONUS_PAR_NIVEAU_DU_HEROS).toBeGreaterThan(0)
    expect(BONUS_PAR_NIVEAU_DU_HEROS).toBeLessThan(0.5)
  })

  it("D par palier ne bouge pas : le héros en prend une part, la profondeur le reste", () => {
    // Contrainte globale du plan. Avant ce plan : m_p³ × ratio = D³. Après :
    // (m_p × (1 + b))³ × ratio = D³. Un niveau de héros par palier, et la
    // croissance par palier est inchangée.
    const parTroisPaliers =
      Math.pow(multiplicateurDePalier() * (1 + BONUS_PAR_NIVEAU_DU_HEROS), ESPECE_TOUS_LES_N_PALIERS) *
      DEBIT_RATIO_ESPECE
    expect(parTroisPaliers).toBeCloseTo(Math.pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS), 6)
  })

  it("le premier niveau coûte plus que la charge de l'œuf", () => {
    // Sinon le joueur naïf (moins cher d'abord) grandit avant de convaincre le
    // vairon, et le plancher de cadence du §8.4 tombe dès la première minute.
    expect(COUT_CREUSER_AU_PALIER_1 * RATIO_COUT_DE_CROISSANCE).toBeGreaterThan(MANA_A_LA_SORTIE_DE_L_OEUF)
  })

  it("les termes du héros sont nommés au registre", () => {
    expect(TERMES_DE_PRODUCTION as string[]).toContain("multiplicateur_heros")
    expect(TERMES_DE_COUT as string[]).toContain("cout_croissance")
  })
})

describe("A2 — ce que le niveau du héros vaut", () => {
  it("au niveau 1 le multiplicateur vaut exactement 1", () => {
    expect(multiplicateurDuHeros(auNiveau(etatInitial(1), 1))).toBe(1)
  })

  it("chaque niveau multiplie par (1 + b), et c'est un terme global", () => {
    const base = etatDeTravail()
    const un = auNiveau(base, 1)
    const cinq = auNiveau(base, 5)
    expect(multiplicateurDuHeros(cinq)).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 4), 12)
    const rapport = multiplicateursGlobaux(cinq).div(multiplicateursGlobaux(un)).toNumber()
    expect(rapport).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 4), 9)
    // La production TOTALE suit le même rapport, espèces comprises.
    const total = productionTotaleParSeconde(cinq).div(productionTotaleParSeconde(un)).toNumber()
    expect(total).toBeGreaterThan(rapport) // > : son débit propre monte aussi avec le niveau
  })

  it("son débit propre monte avec le niveau, multiplicateurs compris", () => {
    const un = auNiveau(etatInitial(1), 1)
    const trois = auNiveau(etatInitial(1), 3)
    expect(productionDuHeros(un).eq(new Decimal(DEBIT_HEROS).mul(multiplicateursGlobaux(un)))).toBe(true)
    expect(productionDuHeros(trois).eq(new Decimal(DEBIT_HEROS).mul(3).mul(multiplicateursGlobaux(trois)))).toBe(true)
  })

  it("le coût de croissance suit g, en fraction du coût du palier de même rang", () => {
    const etat = etatInitial(1)
    const premier = coutDeCroissance(etat, 1)
    expect(premier.toNumber()).toBeCloseTo(COUT_CREUSER_AU_PALIER_1 * RATIO_COUT_DE_CROISSANCE, 9)
    expect(coutDeCroissance(etat, 4).div(coutDeCroissance(etat, 3)).toNumber()).toBeCloseTo(G_COUT_PALIER, 9)
  })

  it("le détail du héros nomme ses deux termes, et sa source porte le niveau", () => {
    const lignes = detailDuHeros(auNiveau(etatInitial(1), 4))
    expect(lignes.map((l) => l.terme)).toEqual(['debit_heros', 'multiplicateur_heros'])
    expect(lignes[0].source).toEqual({ quoi: 'heros', niveau: 4 })
    expect(lignes[1].valeur).toBeCloseTo(Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, 3), 12)
  })
})

describe('A3 — grandir', () => {
  it("paie le coût, monte d'un niveau, crédite le compteur Amélioration", () => {
    const avant = { ...etatInitial(1), cycle: { ...etatInitial(1).cycle, manaCourant: new Decimal(1000) } }
    const cout = coutDeCroissance(avant, avant.cycle.niveauDuHeros)
    const apres = grandir(avant)
    expect(apres.cycle.niveauDuHeros).toBe(2)
    expect(apres.cycle.manaCourant.eq(avant.cycle.manaCourant.sub(cout))).toBe(true)
    expect(apres.permanent.compteursTechnique.amelioration).toBeCloseTo(
      avant.permanent.compteursTechnique.amelioration + cout.toNumber(),
      9,
    )
  })

  it('refuse sans rien changer si le mana manque', () => {
    const pauvre = { ...etatInitial(1), cycle: { ...etatInitial(1).cycle, manaCourant: new Decimal(1) } }
    expect(grandir(pauvre)).toBe(pauvre)
  })

  it("le niveau se reperd à l'éclosion : il ressort alevin", () => {
    const grandi = auNiveau(etatDeTravail(), 9)
    expect(eclore(grandi).cycle.niveauDuHeros).toBe(NIVEAU_DU_HEROS_AU_DEPART)
  })

  it('le niveau ne bouge jamais pendant un tick — le pas reste homogène', () => {
    const depart = auNiveau(etatDeTravail(), 6)
    let petits = depart
    for (let i = 0; i < 480; i += 1) petits = tick(petits, 60)
    const grand = tick(depart, 8 * 3600)
    expect(grand.cycle.niveauDuHeros).toBe(6)
    comparerAToleranceFlottante(petits, grand)
  })
})
