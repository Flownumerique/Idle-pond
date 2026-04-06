export interface ResearchDef {
  id: string;
  name: string;
  description: string;
  branch: 'biologie' | 'geologie' | 'alchimie' | 'mystique' | 'oceanologie';
  cost: number; // gemmes
  requires: string | null;
  effect: {
    globalIncomePercent?: number;
    pondCostReductionPercent?: number;
    boostDurationBonusMs?: number;
    boostCostReduction?: number;
    boostMultiplierBonus?: number;
    milestoneLevelReduction?: number;
    gemmeRewardMultPercent?: number;
    passiveGemmesPerMin?: number;
    fishCostReductionPercent?: number;
    deepFishIncomePercent?: number;  // bonus pour poissons profondeur 4+
  };
}

export const RESEARCH: ResearchDef[] = [
  // ─── Biologie ────────────────────────────────────────────────────
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
    description: 'Les jalons s\'activent 1 niveau plus tôt',
    branch: 'biologie',
    cost: 300,
    requires: 'bio_3',
    effect: { milestoneLevelReduction: 1 },
  },
  {
    id: 'bio_5',
    name: 'Transcendance biologique',
    description: '+75% de revenu global',
    branch: 'biologie',
    cost: 700,
    requires: 'bio_4',
    effect: { globalIncomePercent: 75 },
  },

  // ─── Géologie ────────────────────────────────────────────────────
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
  {
    id: 'geo_4',
    name: 'Art de l\'abîme',
    description: '-50% coût de creusage',
    branch: 'geologie',
    cost: 400,
    requires: 'geo_3',
    effect: { pondCostReductionPercent: 50 },
  },

  // ─── Alchimie ────────────────────────────────────────────────────
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
  {
    id: 'alch_4',
    name: 'Grand Œuvre',
    description: 'Le boost produit ×3 au lieu de ×2',
    branch: 'alchimie',
    cost: 350,
    requires: 'alch_3',
    effect: { boostMultiplierBonus: 1 },
  },

  // ─── Mystique ────────────────────────────────────────────────────
  {
    id: 'myst_1',
    name: 'Intuition des gemmes',
    description: '+25% de 💎 gagnées par les succès',
    branch: 'mystique',
    cost: 40,
    requires: null,
    effect: { gemmeRewardMultPercent: 25 },
  },
  {
    id: 'myst_2',
    name: 'Cristallisation passive',
    description: 'Génère +1 💎 par minute automatiquement',
    branch: 'mystique',
    cost: 100,
    requires: 'myst_1',
    effect: { passiveGemmesPerMin: 1 },
  },
  {
    id: 'myst_3',
    name: 'Flux abyssal',
    description: 'Génère +2 💎 par minute supplémentaires',
    branch: 'mystique',
    cost: 250,
    requires: 'myst_2',
    effect: { passiveGemmesPerMin: 2 },
  },
  {
    id: 'myst_4',
    name: 'Œil de l\'étang',
    description: '+50% de 💎 gagnées par les succès',
    branch: 'mystique',
    cost: 500,
    requires: 'myst_3',
    effect: { gemmeRewardMultPercent: 50 },
  },

  // ─── Océanologie ─────────────────────────────────────────────────
  {
    id: 'ocean_1',
    name: 'Cartographie abyssale',
    description: '+20% de revenu global · Les biomes profonds révèlent leurs secrets',
    branch: 'oceanologie',
    cost: 80,
    requires: null,
    effect: { globalIncomePercent: 20 },
  },
  {
    id: 'ocean_2',
    name: 'Courants des profondeurs',
    description: '-20% coût de tous les poissons',
    branch: 'oceanologie',
    cost: 200,
    requires: 'ocean_1',
    effect: { fishCostReductionPercent: 20 },
  },
  {
    id: 'ocean_3',
    name: 'Symbiose abyssale',
    description: '+50% de revenu des créatures de profondeur 4+',
    branch: 'oceanologie',
    cost: 500,
    requires: 'ocean_2',
    effect: { deepFishIncomePercent: 50 },
  },
  {
    id: 'ocean_4',
    name: 'Nexus Connecté',
    description: '+3 💎/min passifs · +30% de revenu global',
    branch: 'oceanologie',
    cost: 1000,
    requires: 'ocean_3',
    effect: { passiveGemmesPerMin: 3, globalIncomePercent: 30 },
  },
];
