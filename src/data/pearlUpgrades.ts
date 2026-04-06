export interface PearlUpgradeDef {
  id: string;
  name: string;
  description: string;
  cost: number; // perles
  requires: string | null;
  effect: {
    globalIncomePercent?: number;
    fishCostReductionPercent?: number;
    offlineMultPercent?: number;
  };
}

export const PEARL_UPGRADES: PearlUpgradeDef[] = [
  // Production
  {
    id: 'pearl_prod_1',
    name: 'Courant mystique',
    description: '+25% de revenu global (permanent)',
    cost: 2,
    requires: null,
    effect: { globalIncomePercent: 25 },
  },
  {
    id: 'pearl_prod_2',
    name: 'Flux abyssal',
    description: '+50% de revenu global (permanent)',
    cost: 5,
    requires: 'pearl_prod_1',
    effect: { globalIncomePercent: 50 },
  },
  {
    id: 'pearl_prod_3',
    name: 'Torrent éternel',
    description: '+100% de revenu global (permanent)',
    cost: 12,
    requires: 'pearl_prod_2',
    effect: { globalIncomePercent: 100 },
  },

  // Coûts
  {
    id: 'pearl_cost_1',
    name: 'Pêche efficace',
    description: '-20% coût d\'achat des poissons',
    cost: 3,
    requires: null,
    effect: { fishCostReductionPercent: 20 },
  },
  {
    id: 'pearl_cost_2',
    name: 'Maîtrise de l\'élevage',
    description: '-30% coût d\'achat des poissons',
    cost: 8,
    requires: 'pearl_cost_1',
    effect: { fishCostReductionPercent: 30 },
  },

  // Hors-ligne
  {
    id: 'pearl_offline',
    name: 'Gardien des eaux',
    description: '×2 production hors-ligne',
    cost: 4,
    requires: null,
    effect: { offlineMultPercent: 100 },
  },
];
