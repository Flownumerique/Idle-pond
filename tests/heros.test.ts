/**
 * L'axe héros — spec 2026-09-17 §3.1.
 *
 * Le héros GRANDIT pendant la vie, avec du mana : c'est le quatrième achat du
 * noyau v1.0 §1.2 amendé. Son niveau se reperd à l'éclosion (f = 1), et ce
 * qu'il apporte est un multiplicateur global NOMMÉ, jamais un facteur flottant.
 */
import { describe, expect, it } from "vitest"
import {
  BONUS_PAR_NIVEAU_DU_HEROS,
  DEBIT_RATIO_ESPECE,
  D_PRODUCTION_PAR_PALIER,
  ESPECE_TOUS_LES_N_PALIERS,
  MANA_A_LA_SORTIE_DE_L_OEUF,
  NIVEAU_DU_HEROS_AU_DEPART,
  RATIO_COUT_DE_CROISSANCE,
  COUT_CREUSER_AU_PALIER_1,
  multiplicateurDePalier,
} from "../src/noyau/constantes"
import { TERMES_DE_COUT, TERMES_DE_PRODUCTION } from "../src/noyau/types"
import { etatInitial } from "../src/noyau/noyau"

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
