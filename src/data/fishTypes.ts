import type { PoissonInstance } from '../store/useGameStore';

export interface MilestoneBoost {
  level: number;
  selfMultiplier: number;  // Multiplicateur sur le revenu propre du poisson
  globalBonus: number;     // Bonus % ajouté à la production globale (0 si aucun)
  label: string;
}

export interface FishType {
  type: string;
  baseIncome: number;
  baseCost: number;
  name: string;
  desc: string;
  requiredDepth: number;
  emoji: string;
  depthLabel: string;
  milestones: MilestoneBoost[];
}

export const FISH_TYPES: FishType[] = [
  {
    type: 'gold',
    baseIncome: 1,
    baseCost: 10,
    name: 'Poisson Or',
    desc: 'Produit 1 Mana/s',
    requiredDepth: 0,
    emoji: '🐟',
    depthLabel: 'Eaux peu profondes',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Écailles renforcées' },
      { level: 25,  selfMultiplier: 2, globalBonus: 0, label: 'Nage rapide' },
      { level: 50,  selfMultiplier: 2, globalBonus: 0, label: 'Or pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 1, label: 'Ruée vers l\'or' },
    ],
  },
  {
    type: 'ruby',
    baseIncome: 10,
    baseCost: 150,
    name: 'Poisson Rubis',
    desc: 'Produit 10 Mana/s',
    requiredDepth: 1,
    emoji: '🐠',
    depthLabel: 'Eaux intermédiaires',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0,  label: 'Pierre précieuse' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5,  label: 'Éclat rubis' },
      { level: 50,  selfMultiplier: 2, globalBonus: 5,  label: 'Cœur de rubis' },
      { level: 100, selfMultiplier: 2, globalBonus: 10, label: 'Maître rubis' },
    ],
  },
  {
    type: 'diamond',
    baseIncome: 100,
    baseCost: 5000,
    name: 'Poisson Diamant',
    desc: 'Produit 100 Mana/s',
    requiredDepth: 2,
    emoji: '🐡',
    depthLabel: 'Eaux profondes',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Diamant brut' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Taille parfaite' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Diamant pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 20, label: 'Diamant légendaire' },
    ],
  },
  {
    type: 'abyssal',
    baseIncome: 1000,
    baseCost: 100000,
    name: 'Poisson Abyssal',
    desc: 'Produit 1 000 Mana/s',
    requiredDepth: 3,
    emoji: '🦑',
    depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Éveil abyssal' },
      { level: 25,  selfMultiplier: 2, globalBonus: 20, label: 'Profondeur infinie' },
      { level: 50,  selfMultiplier: 2, globalBonus: 30, label: 'Maître des abysses' },
      { level: 100, selfMultiplier: 2, globalBonus: 50, label: 'Seigneur des abysses' },
    ],
  },
];

/** Multiplicateur de jalons pour un poisson donné (sur son propre revenu) */
export const getSelfMilestoneMultiplier = (fish: PoissonInstance): number => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return 1;
  return type.milestones
    .filter(m => fish.level >= m.level)
    .reduce((acc, m) => acc * m.selfMultiplier, 1);
};

/** Multiplicateur global calculé depuis tous les poissons possédés */
export const getGlobalMultiplier = (poissons: PoissonInstance[]): number => {
  let totalPercent = 0;
  for (const fish of poissons) {
    const type = FISH_TYPES.find(t => t.type === fish.type);
    if (!type) continue;
    for (const m of type.milestones) {
      if (fish.level >= m.level) totalPercent += m.globalBonus;
    }
  }
  return 1 + totalPercent / 100;
};

/** Prochain jalon non atteint pour un poisson */
export const getNextMilestone = (fish: PoissonInstance): MilestoneBoost | null => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return null;
  return type.milestones.find(m => fish.level < m.level) ?? null;
};
