export interface ResearchDef {
  id: string;
  name: string;
  description: string;
  branch: 'biologie' | 'geologie' | 'alchimie';
  cost: number; // gemmes
  requires: string | null;
  effect: {
    globalIncomePercent?: number;
    pondCostReductionPercent?: number;
    boostDurationBonusMs?: number;
    boostCostReduction?: number;
    milestoneLevelReduction?: number;
  };
}

export const RESEARCH: ResearchDef[] = [
  // Biologie — booste les revenus
  {
    id: 'bio_1',
    name: 'Alimentation enrichie',
    description: '+15% de revenu global',
    branch: 'biologie',
    cost: 20,
    requires: null,
    effect: { globalIncomePercent: 15 },
  },
  {
    id: 'bio_2',
    name: 'Symbiose aquatique',
    description: '+25% de revenu global',
    branch: 'biologie',
    cost: 50,
    requires: 'bio_1',
    effect: { globalIncomePercent: 25 },
  },
  {
    id: 'bio_3',
    name: 'Évolution accélérée',
    description: '+50% de revenu global',
    branch: 'biologie',
    cost: 120,
    requires: 'bio_2',
    effect: { globalIncomePercent: 50 },
  },
  {
    id: 'bio_4',
    name: 'Mutation magique',
    description: 'Les jalons de niveau s\'activent 1 niveau plus tôt',
    branch: 'biologie',
    cost: 300,
    requires: 'bio_3',
    effect: { milestoneLevelReduction: 1 },
  },

  // Géologie — réduit les coûts de creusage
  {
    id: 'geo_1',
    name: 'Forage optimisé',
    description: '-15% coût de creusage',
    branch: 'geologie',
    cost: 30,
    requires: null,
    effect: { pondCostReductionPercent: 15 },
  },
  {
    id: 'geo_2',
    name: 'Techniques avancées',
    description: '-25% coût de creusage',
    branch: 'geologie',
    cost: 80,
    requires: 'geo_1',
    effect: { pondCostReductionPercent: 25 },
  },
  {
    id: 'geo_3',
    name: 'Maîtrise des profondeurs',
    description: '-40% coût de creusage',
    branch: 'geologie',
    cost: 200,
    requires: 'geo_2',
    effect: { pondCostReductionPercent: 40 },
  },

  // Alchimie — améliore le boost temporaire
  {
    id: 'alch_1',
    name: 'Élixir de vitalité',
    description: '+2 min de durée du boost',
    branch: 'alchimie',
    cost: 25,
    requires: null,
    effect: { boostDurationBonusMs: 2 * 60 * 1000 },
  },
  {
    id: 'alch_2',
    name: 'Concentration pure',
    description: '+3 min de durée du boost',
    branch: 'alchimie',
    cost: 60,
    requires: 'alch_1',
    effect: { boostDurationBonusMs: 3 * 60 * 1000 },
  },
  {
    id: 'alch_3',
    name: 'Maîtrise alchimique',
    description: 'Le boost coûte 5 💎 au lieu de 10',
    branch: 'alchimie',
    cost: 150,
    requires: 'alch_2',
    effect: { boostCostReduction: 5 },
  },
];
