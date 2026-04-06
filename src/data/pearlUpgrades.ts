// Le Marché des Perles utilise des GEMMES (💎), pas des Perles.
// Les Perles (🪸) sont réservées aux Améliorations de Prestige.

export interface PearlUpgradeDef {
  id: string;
  name: string;
  description: string;
  cost: number; // gemmes 💎
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
    description: '+25% de revenu global',
    cost: 50,
    requires: null,
    effect: { globalIncomePercent: 25 },
  },
  {
    id: 'pearl_prod_2',
    name: 'Flux abyssal',
    description: '+50% de revenu global',
    cost: 120,
    requires: 'pearl_prod_1',
    effect: { globalIncomePercent: 50 },
  },
  {
    id: 'pearl_prod_3',
    name: 'Torrent éternel',
    description: '+100% de revenu global',
    cost: 300,
    requires: 'pearl_prod_2',
    effect: { globalIncomePercent: 100 },
  },

  // Coûts
  {
    id: 'pearl_cost_1',
    name: 'Pêche efficace',
    description: '-20% coût d\'achat des poissons',
    cost: 75,
    requires: null,
    effect: { fishCostReductionPercent: 20 },
  },
  {
    id: 'pearl_cost_2',
    name: 'Maîtrise de l\'élevage',
    description: '-30% coût d\'achat des poissons',
    cost: 200,
    requires: 'pearl_cost_1',
    effect: { fishCostReductionPercent: 30 },
  },

  // Hors-ligne
  {
    id: 'pearl_offline',
    name: 'Gardien des eaux',
    description: '×2 production hors-ligne',
    cost: 100,
    requires: null,
    effect: { offlineMultPercent: 100 },
  },
];
