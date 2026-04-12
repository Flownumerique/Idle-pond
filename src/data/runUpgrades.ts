import Decimal from 'break_infinity.js';

export interface RunUpgrade {
  id: string;
  name: string;
  desc: string;
  emoji: string;
  manaCost: Decimal;
  requires?: string;
  effect: {
    globalIncomePercent?: number;
    pondCostReductionPercent?: number;
    fishCostReductionPercent?: number;
  };
}

// Améliorations globales — s'achètent avec du Mana, se réinitialisent au Prestige
export const GLOBAL_UPGRADES: RunUpgrade[] = [
  {
    id: 'eau_enrichie',
    name: 'Eau Enrichie',
    desc: 'L\'étang déborde de mana. Tous les revenus +25 %.',
    emoji: '💧',
    manaCost: new Decimal(500),
    effect: { globalIncomePercent: 25 },
  },
  {
    id: 'algues_mystiques',
    name: 'Algues Mystiques',
    desc: 'Des algues rares colonisent le fond. Revenus +50 %.',
    emoji: '🌿',
    manaCost: new Decimal(5_000),
    requires: 'eau_enrichie',
    effect: { globalIncomePercent: 50 },
  },
  {
    id: 'cristal_de_fond',
    name: 'Cristal de Fond',
    desc: 'Un cristal ancré au fond amplifie chaque vibration. Revenus ×2.',
    emoji: '💎',
    manaCost: new Decimal(50_000),
    requires: 'algues_mystiques',
    effect: { globalIncomePercent: 100 },
  },
  {
    id: 'vortex_arcanique',
    name: 'Vortex Arcanique',
    desc: 'Un tourbillon de mana brute accélère tout. Revenus ×3.',
    emoji: '🌀',
    manaCost: new Decimal(500_000),
    requires: 'cristal_de_fond',
    effect: { globalIncomePercent: 200 },
  },
  {
    id: 'tempete_de_mana',
    name: 'Tempête de Mana',
    desc: 'La mana se déchaîne, fertilisant chaque être vivant. Revenus ×5.',
    emoji: '⚡',
    manaCost: new Decimal(5_000_000),
    requires: 'vortex_arcanique',
    effect: { globalIncomePercent: 400 },
  },
  {
    id: 'resonance_abyssale',
    name: 'Résonance Abyssale',
    desc: 'Les profondeurs vibrent à l\'unisson. Revenus ×10.',
    emoji: '🌊',
    manaCost: new Decimal(50_000_000),
    requires: 'tempete_de_mana',
    effect: { globalIncomePercent: 900 },
  },
  {
    id: 'singularite',
    name: 'Singularité',
    desc: 'L\'étang transcende les lois naturelles. Revenus ×20.',
    emoji: '✨',
    manaCost: new Decimal(500_000_000),
    requires: 'resonance_abyssale',
    effect: { globalIncomePercent: 1900 },
  },
];

// Améliorations de zone — réduisent le coût de creusage
export const ZONE_UPGRADES: RunUpgrade[] = [
  {
    id: 'outils_affutes',
    name: 'Outils Affûtés',
    desc: 'Réduit le coût de creusage de 10 %.',
    emoji: '⛏️',
    manaCost: new Decimal(300),
    effect: { pondCostReductionPercent: 10 },
  },
  {
    id: 'dynamite_runique',
    name: 'Dynamite Runique',
    desc: 'Explosions ciblées. Creusage -20 % supplémentaire.',
    emoji: '💣',
    manaCost: new Decimal(8_000),
    requires: 'outils_affutes',
    effect: { pondCostReductionPercent: 20 },
  },
  {
    id: 'forage_magique',
    name: 'Forage Magique',
    desc: 'Le mana dissout la roche. Creusage -30 % supplémentaire.',
    emoji: '🔮',
    manaCost: new Decimal(200_000),
    requires: 'dynamite_runique',
    effect: { pondCostReductionPercent: 30 },
  },
  {
    id: 'dissolution_abyssale',
    name: 'Dissolution Abyssale',
    desc: 'La matière fond sous la pression de la mana. Creusage -40 % supplémentaire.',
    emoji: '🌑',
    manaCost: new Decimal(10_000_000),
    requires: 'forage_magique',
    effect: { pondCostReductionPercent: 40 },
  },
];
