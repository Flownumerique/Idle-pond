import Decimal from 'break_infinity.js';

export const formatNumber = (num: Decimal | number | string): string => {
  const d = new Decimal(num);

  if (d.lt(1000)) {
    return d.toNumber().toFixed(0);
  }

  // Custom abbreviations if needed, or scientific notation for big ones
  if (d.lt(1e6)) return (d.toNumber() / 1e3).toFixed(2) + 'k';
  if (d.lt(1e9)) return (d.toNumber() / 1e6).toFixed(2) + 'M';
  if (d.lt(1e12)) return (d.toNumber() / 1e9).toFixed(2) + 'B';
  if (d.lt(1e15)) return (d.toNumber() / 1e12).toFixed(2) + 'T';

  return d.toExponential(2);
};
