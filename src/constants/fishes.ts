import Decimal from 'break_infinity.js';
import { Fish } from '../store/useGameStore';

export const FISHES: Fish[] = [
  {
    id: 'guppy',
    name: 'Guppy',
    baseCost: new Decimal(10),
    growthRate: 1.07,
    baseIncome: new Decimal(1)
  },
  {
    id: 'neon',
    name: 'Néon',
    baseCost: new Decimal(150),
    growthRate: 1.10,
    baseIncome: new Decimal(8)
  },
  {
    id: 'poisson_rouge',
    name: 'Poisson Rouge',
    baseCost: new Decimal(1500),
    growthRate: 1.12,
    baseIncome: new Decimal(45)
  },
  {
    id: 'carpe_koi',
    name: 'Carpe Koï',
    baseCost: new Decimal(12000),
    growthRate: 1.15,
    baseIncome: new Decimal(200)
  }
];
