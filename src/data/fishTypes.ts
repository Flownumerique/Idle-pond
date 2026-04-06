import type { PoissonInstance } from '../store/useGameStore';

export interface MilestoneBoost {
  level: number;
  selfMultiplier: number;
  globalBonus: number;  // % ajouté à la production globale
  label: string;
}

export interface FishType {
  type: string;
  baseIncome: number;
  baseCost: number;
  name: string;
  desc: string;
  requiredDepth: number;
  requiredPrestiges?: number;  // prestige minimum requis
  maxOwned?: number;           // limite d'exemplaires (undefined = illimité)
  emoji: string;
  depthLabel: string;
  milestones: MilestoneBoost[];
}

export const FISH_TYPES: FishType[] = [
  // ─── Profondeur 0 — Lac de Surface ───────────────────────────────
  {
    type: 'gold',
    baseIncome: 1,
    baseCost: 10,
    name: 'Poisson Or',
    desc: 'Produit 1 Mana/s',
    requiredDepth: 0,
    emoji: '🐟',
    depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Écailles renforcées' },
      { level: 25,  selfMultiplier: 2, globalBonus: 0, label: 'Nage rapide' },
      { level: 50,  selfMultiplier: 2, globalBonus: 0, label: 'Or pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 1, label: 'Ruée vers l\'or' },
    ],
  },
  {
    type: 'carpe',
    baseIncome: 4,
    baseCost: 60,
    name: 'Carpe Dorée',
    desc: 'Produit 4 Mana/s',
    requiredDepth: 0,
    emoji: '🎏',
    depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Écailles d\'orange' },
      { level: 25,  selfMultiplier: 2, globalBonus: 1, label: 'Carpe élégante' },
      { level: 50,  selfMultiplier: 2, globalBonus: 2, label: 'Carpe impériale' },
      { level: 100, selfMultiplier: 2, globalBonus: 3, label: 'Carpe du destin' },
    ],
  },

  // ─── Profondeur 1 — Rivière Souterraine ──────────────────────────
  {
    type: 'ruby',
    baseIncome: 10,
    baseCost: 150,
    name: 'Poisson Rubis',
    desc: 'Produit 10 Mana/s',
    requiredDepth: 1,
    emoji: '🐠',
    depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0,  label: 'Pierre précieuse' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5,  label: 'Éclat rubis' },
      { level: 50,  selfMultiplier: 2, globalBonus: 5,  label: 'Cœur de rubis' },
      { level: 100, selfMultiplier: 2, globalBonus: 10, label: 'Maître rubis' },
    ],
  },
  {
    type: 'dragonfly',
    baseIncome: 35,
    baseCost: 800,
    name: 'Libellule de Saphir',
    desc: 'Produit 35 Mana/s',
    requiredDepth: 1,
    emoji: '🪲',
    depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Ailes scintillantes' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5, label: 'Vol de saphir' },
      { level: 50,  selfMultiplier: 2, globalBonus: 5, label: 'Essaim bleu' },
      { level: 100, selfMultiplier: 2, globalBonus: 8, label: 'Reine des eaux claires' },
    ],
  },

  // ─── Profondeur 2 — Récif Corallien ──────────────────────────────
  {
    type: 'diamond',
    baseIncome: 100,
    baseCost: 5_000,
    name: 'Poisson Diamant',
    desc: 'Produit 100 Mana/s',
    requiredDepth: 2,
    emoji: '🐡',
    depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Diamant brut' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Taille parfaite' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Diamant pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 20, label: 'Diamant légendaire' },
    ],
  },
  {
    type: 'crab',
    baseIncome: 350,
    baseCost: 25_000,
    name: 'Crabe Cristal',
    desc: 'Produit 350 Mana/s',
    requiredDepth: 2,
    emoji: '🦀',
    depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Carapace nacrée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Pinces cristallines' },
      { level: 50,  selfMultiplier: 2, globalBonus: 10, label: 'Seigneur du récif' },
      { level: 100, selfMultiplier: 2, globalBonus: 15, label: 'Crabe légendaire' },
    ],
  },

  // ─── Profondeur 3 — Abysses ───────────────────────────────────────
  {
    type: 'abyssal',
    baseIncome: 1_000,
    baseCost: 100_000,
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
  {
    type: 'octopus',
    baseIncome: 3_500,
    baseCost: 500_000,
    name: 'Pieuvre Ombre',
    desc: 'Produit 3 500 Mana/s',
    requiredDepth: 3,
    emoji: '🐙',
    depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Tentacules spectraux' },
      { level: 25,  selfMultiplier: 2, globalBonus: 15, label: 'Encre de ténèbres' },
      { level: 50,  selfMultiplier: 2, globalBonus: 20, label: 'Maîtresse des ombres' },
      { level: 100, selfMultiplier: 2, globalBonus: 30, label: 'Archiviste abyssale' },
    ],
  },

  // ─── Profondeur 4 — Zone Hydrothermale ───────────────────────────
  {
    type: 'salamander',
    baseIncome: 10_000,
    baseCost: 2_000_000,
    name: 'Salamandre Ignée',
    desc: 'Produit 10 000 Mana/s',
    requiredDepth: 4,
    emoji: '🔥',
    depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 15, label: 'Flamme primordiale' },
      { level: 25,  selfMultiplier: 2, globalBonus: 20, label: 'Peau ignifugée' },
      { level: 50,  selfMultiplier: 2, globalBonus: 30, label: 'Maîtresse du feu' },
      { level: 100, selfMultiplier: 2, globalBonus: 40, label: 'Cœur volcanique' },
    ],
  },
  {
    type: 'eel',
    baseIncome: 35_000,
    baseCost: 10_000_000,
    name: 'Anguille de Feu',
    desc: 'Produit 35 000 Mana/s',
    requiredDepth: 4,
    emoji: '⚡',
    depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Décharge primaire' },
      { level: 25,  selfMultiplier: 2, globalBonus: 30, label: 'Courant thermique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 40, label: 'Arc électrique' },
      { level: 100, selfMultiplier: 2, globalBonus: 60, label: 'Foudre abyssale' },
    ],
  },

  // ─── Profondeur 5 — Plaine Abyssale ──────────────────────────────
  {
    type: 'jellyfish',
    baseIncome: 100_000,
    baseCost: 100_000_000,
    name: 'Méduse Bioluminescente',
    desc: 'Produit 100 000 Mana/s',
    requiredDepth: 5,
    emoji: '🫧',
    depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Voile lumineux' },
      { level: 25,  selfMultiplier: 2, globalBonus: 30, label: 'Pulsation de mana' },
      { level: 50,  selfMultiplier: 2, globalBonus: 40, label: 'Ballet de lumière' },
      { level: 100, selfMultiplier: 2, globalBonus: 50, label: 'Gardienne de la plaine' },
    ],
  },
  {
    type: 'shark',
    baseIncome: 350_000,
    baseCost: 500_000_000,
    name: 'Requin des Abysses',
    desc: 'Produit 350 000 Mana/s',
    requiredDepth: 5,
    emoji: '🦈',
    depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 25, label: 'Dents de cristal' },
      { level: 25,  selfMultiplier: 2, globalBonus: 40, label: 'Prédateur silencieux' },
      { level: 50,  selfMultiplier: 2, globalBonus: 60, label: 'Apex abyssal' },
      { level: 100, selfMultiplier: 2, globalBonus: 80, label: 'Terreur des profondeurs' },
    ],
  },

  // ─── Profondeur 6 — Fosse des Origines ───────────────────────────
  {
    type: 'dragon',
    baseIncome: 1_000_000,
    baseCost: 5_000_000_000,
    name: 'Dragon des Mers',
    desc: 'Produit 1 000 000 Mana/s',
    requiredDepth: 6,
    emoji: '🐉',
    depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 30,  label: 'Éveil draconique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 50,  label: 'Souffle d\'abîme' },
      { level: 50,  selfMultiplier: 2, globalBonus: 75,  label: 'Ancien gardien' },
      { level: 100, selfMultiplier: 2, globalBonus: 100, label: 'Dieu des profondeurs' },
    ],
  },
  {
    type: 'leviathan',
    baseIncome: 3_500_000,
    baseCost: 25_000_000_000,
    name: 'Léviathan Cristallin',
    desc: 'Produit 3 500 000 Mana/s',
    requiredDepth: 6,
    emoji: '💎',
    depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 40,  label: 'Éveil du colosse' },
      { level: 25,  selfMultiplier: 2, globalBonus: 70,  label: 'Armure de cristal' },
      { level: 50,  selfMultiplier: 2, globalBonus: 100, label: 'Titan des abysses' },
      { level: 100, selfMultiplier: 2, globalBonus: 150, label: 'Léviathan immortel' },
    ],
  },

  // ─── Profondeur 7 — Nexus de Mana ────────────────────────────────
  {
    type: 'egregore',
    baseIncome: 10_000_000,
    baseCost: 100_000_000_000,
    name: 'Égrégore Aquatique',
    desc: 'Produit 10 000 000 Mana/s',
    requiredDepth: 7,
    requiredPrestiges: 2,
    emoji: '✨',
    depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Conscience émergente' },
      { level: 25,  selfMultiplier: 2, globalBonus: 80,  label: 'Réseau psychique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 120, label: 'Esprit collectif' },
      { level: 100, selfMultiplier: 2, globalBonus: 200, label: 'Transcendance aquatique' },
    ],
  },
  {
    type: 'celestial',
    baseIncome: 50_000_000,
    baseCost: 1_000_000_000_000,
    name: 'Poisson Céleste',
    desc: 'Produit 50 000 000 Mana/s — +1 💎/min',
    requiredDepth: 7,
    requiredPrestiges: 1,
    maxOwned: 1,
    emoji: '🌟',
    depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Naissance céleste' },
      { level: 25,  selfMultiplier: 2, globalBonus: 100, label: 'Ascension' },
      { level: 50,  selfMultiplier: 2, globalBonus: 200, label: 'Transcendance' },
      { level: 100, selfMultiplier: 2, globalBonus: 300, label: 'Divinité aquatique' },
    ],
  },
];

/** Multiplicateur de jalons propre à un poisson (levelReduction = bonus de recherche bio_4) */
export const getSelfMilestoneMultiplier = (fish: PoissonInstance, levelReduction = 0): number => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return 1;
  return type.milestones
    .filter(m => fish.level >= Math.max(1, m.level - levelReduction))
    .reduce((acc, m) => acc * m.selfMultiplier, 1);
};

/** Multiplicateur global calculé depuis tous les poissons possédés */
export const getGlobalMultiplier = (poissons: PoissonInstance[], levelReduction = 0): number => {
  let totalPercent = 0;
  for (const fish of poissons) {
    const type = FISH_TYPES.find(t => t.type === fish.type);
    if (!type) continue;
    for (const m of type.milestones) {
      if (fish.level >= Math.max(1, m.level - levelReduction)) {
        totalPercent += m.globalBonus;
      }
    }
  }
  return 1 + totalPercent / 100;
};

/** Prochain jalon non atteint pour un poisson */
export const getNextMilestone = (fish: PoissonInstance, levelReduction = 0): MilestoneBoost | null => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return null;
  return type.milestones.find(m => fish.level < Math.max(1, m.level - levelReduction)) ?? null;
};
