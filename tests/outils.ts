/**
 * Outils de test — comparaison d'états à la tolérance flottante près.
 *
 * Le §12 formule le critère du test d'équivalence de pas ainsi : « 480 appels à
 * dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la tolérance
 * flottante près ». C'est cette tolérance-là, et rien de plus permissif : elle
 * couvre l'écart entre exp(-k·Δ) évalué une fois et (exp(-k·δ))^480, pas une
 * divergence de modèle.
 */
import Decimal from 'break_infinity.js'
import { expect } from 'vitest'

export const TOLERANCE_RELATIVE = 1e-9

function estDecimal(valeur: unknown): valeur is Decimal {
  return valeur instanceof Decimal
}

function proche(a: number, b: number, tolerance: number): boolean {
  if (a === b) return true
  if (!Number.isFinite(a) || !Number.isFinite(b)) return false
  const echelle = Math.max(Math.abs(a), Math.abs(b), 1e-300)
  return Math.abs(a - b) / echelle <= tolerance
}

export function comparerAToleranceFlottante(
  obtenu: unknown,
  attendu: unknown,
  tolerance = TOLERANCE_RELATIVE,
  chemin = '',
): void {
  if (estDecimal(attendu) || estDecimal(obtenu)) {
    const a = new Decimal(obtenu as Decimal)
    const b = new Decimal(attendu as Decimal)
    if (a.eq(b)) return
    const ecart = a.sub(b).abs().div(Decimal.max(a.abs(), b.abs()).add(1e-300))
    expect(ecart.toNumber(), `Decimal divergent en ${chemin} : ${a} vs ${b}`).toBeLessThanOrEqual(tolerance)
    return
  }
  if (typeof attendu === 'number' && typeof obtenu === 'number') {
    expect(proche(obtenu, attendu, tolerance), `nombre divergent en ${chemin} : ${obtenu} vs ${attendu}`).toBe(true)
    return
  }
  if (Array.isArray(attendu)) {
    expect(Array.isArray(obtenu), `tableau attendu en ${chemin}`).toBe(true)
    const tableau = obtenu as unknown[]
    expect(tableau.length, `longueur divergente en ${chemin}`).toBe(attendu.length)
    attendu.forEach((valeur, index) => comparerAToleranceFlottante(tableau[index], valeur, tolerance, `${chemin}[${index}]`))
    return
  }
  if (attendu !== null && typeof attendu === 'object') {
    expect(typeof obtenu, `objet attendu en ${chemin}`).toBe('object')
    const gauche = obtenu as Record<string, unknown>
    const droite = attendu as Record<string, unknown>
    const clefs = new Set([...Object.keys(gauche), ...Object.keys(droite)])
    for (const clef of clefs) {
      comparerAToleranceFlottante(gauche[clef], droite[clef], tolerance, chemin === '' ? clef : `${chemin}.${clef}`)
    }
    return
  }
  expect(obtenu, `valeur divergente en ${chemin}`).toStrictEqual(attendu)
}

/** Retire commentaires de bloc et de ligne : les tests portent sur le code, pas sur la prose. */
export function sansCommentaires(source: string): string {
  return source.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/(^|[^:])\/\/[^\n]*/g, '$1 ')
}
