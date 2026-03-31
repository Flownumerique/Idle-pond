import Decimal from 'break_infinity.js';

const SUFFIXES = ['', 'k', 'M', 'B']; // On s'arrête à B (Billion/Milliard)

export function formatNumber(value: Decimal | number | string): string {
  const decimalValue = new Decimal(value);

  // Si inférieur à 1000, on affiche le nombre entier
  if (decimalValue.lt(1000)) {
    return Math.floor(decimalValue.toNumber()).toString();
  }

  // Entre 1000 et 1e12 (exclus), on utilise les suffixes
  if (decimalValue.lt(1e12)) {
    // Calcul de l'index du suffixe (1 = k, 2 = M, 3 = B)
    const e = Math.floor(decimalValue.log10() / 3);

    if (e < SUFFIXES.length) {
      // Division par 10^(3*e) pour obtenir le nombre avec virgule
      const mantissa = decimalValue.div(Decimal.pow(10, e * 3)).toNumber();
      // On garde 2 décimales et on ajoute le suffixe
      return `${mantissa.toFixed(2)}${SUFFIXES[e]}`;
    }
  }

  // À partir de 1e12, on passe en notation scientifique
  const m = decimalValue.mantissa.toFixed(2);
  const e = decimalValue.exponent;
  return `${m}e${e}`;
}
