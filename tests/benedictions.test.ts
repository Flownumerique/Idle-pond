/**
 * Les bénédictions — noyau v1.0 §4, spec 2026-09-17 §3.2.
 *
 * Permanentes, payées en Foi, et l'unique chose que la Foi achète tant que les
 * miracles sont gelés. Ciblée : multiplicateur sur une espèce. Globale :
 * additif sur le débit de base de toutes les espèces, présentes et futures.
 */
import { describe, expect, it } from 'vitest'
import { BENEDICTION_GLOBALE_ID, BENEDICTIONS, benedictionCibleeDe, benedictionParId } from '../src/donnees/benedictions'
import {
  BENEDICTION_CIBLEE_PAR_RANG,
  BENEDICTION_GLOBALE_PAR_RANG,
  FOI_COUT_DE_BENEDICTION_CIBLEE,
  FOI_COUT_DE_BENEDICTION_GLOBALE,
  RATIO_COUT_DE_BENEDICTION,
} from '../src/noyau/constantes'
import { ESPECES } from '../src/donnees/especes'

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
