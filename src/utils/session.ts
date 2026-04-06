import Decimal from 'break_infinity.js';

let _sessionManaEarned = new Decimal(0);

export const getSessionManaEarned = (): Decimal => _sessionManaEarned;
export const addSessionMana = (amount: Decimal): void => {
  _sessionManaEarned = _sessionManaEarned.plus(amount);
};
