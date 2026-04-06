import type { PoissonInstance } from '../store/useGameStore';

export interface MilestoneBoost {
  level: number;
  selfMultiplier: number;
  globalBonus: number;
  label: string;
}

export interface FishType {
  type: string;
  baseIncome: number;
  baseCost: number;
  name: string;
  desc: string;
  requiredDepth: number;
  requiredPrestiges?: number;
  maxOwned?: number;
  emoji: string;
  depthLabel: string;
  milestones: MilestoneBoost[];
}

export const FISH_TYPES: FishType[] = [

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 0 — Lac de Surface
  // ══════════════════════════════════════════════════════════════
  {
    type: 'gold',
    baseIncome: 1, baseCost: 10,
    name: 'Poisson Or', desc: 'Produit 1 Mana/s',
    requiredDepth: 0, emoji: '🐟', depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Écailles renforcées' },
      { level: 25,  selfMultiplier: 2, globalBonus: 0, label: 'Nage rapide' },
      { level: 50,  selfMultiplier: 2, globalBonus: 0, label: 'Or pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 1, label: 'Ruée vers l\'or' },
    ],
  },
  {
    type: 'carpe',
    baseIncome: 4, baseCost: 60,
    name: 'Carpe Dorée', desc: 'Produit 4 Mana/s',
    requiredDepth: 0, emoji: '🎏', depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Écailles d\'orange' },
      { level: 25,  selfMultiplier: 2, globalBonus: 1, label: 'Carpe élégante' },
      { level: 50,  selfMultiplier: 2, globalBonus: 2, label: 'Carpe impériale' },
      { level: 100, selfMultiplier: 2, globalBonus: 3, label: 'Carpe du destin' },
    ],
  },
  {
    type: 'frog',
    baseIncome: 7, baseCost: 45,
    name: 'Grenouille Cristal', desc: 'Produit 7 Mana/s',
    requiredDepth: 0, emoji: '🐸', depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Saut de mana' },
      { level: 25,  selfMultiplier: 2, globalBonus: 1, label: 'Peau translucide' },
      { level: 50,  selfMultiplier: 2, globalBonus: 2, label: 'Coassement magique' },
      { level: 100, selfMultiplier: 2, globalBonus: 3, label: 'Grenouille légendaire' },
    ],
  },
  {
    type: 'duck',
    baseIncome: 12, baseCost: 110,
    name: 'Canard Mana', desc: 'Produit 12 Mana/s',
    requiredDepth: 0, emoji: '🦆', depthLabel: 'Lac de Surface',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Plumage doré' },
      { level: 25,  selfMultiplier: 2, globalBonus: 1, label: 'Vol rasant' },
      { level: 50,  selfMultiplier: 2, globalBonus: 2, label: 'Canard chamanique' },
      { level: 100, selfMultiplier: 2, globalBonus: 4, label: 'Canard cosmique' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 1 — Rivière Souterraine
  // ══════════════════════════════════════════════════════════════
  {
    type: 'ruby',
    baseIncome: 10, baseCost: 150,
    name: 'Poisson Rubis', desc: 'Produit 10 Mana/s',
    requiredDepth: 1, emoji: '🐠', depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0,  label: 'Pierre précieuse' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5,  label: 'Éclat rubis' },
      { level: 50,  selfMultiplier: 2, globalBonus: 5,  label: 'Cœur de rubis' },
      { level: 100, selfMultiplier: 2, globalBonus: 10, label: 'Maître rubis' },
    ],
  },
  {
    type: 'dragonfly',
    baseIncome: 35, baseCost: 800,
    name: 'Libellule de Saphir', desc: 'Produit 35 Mana/s',
    requiredDepth: 1, emoji: '🪲', depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0, label: 'Ailes scintillantes' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5, label: 'Vol de saphir' },
      { level: 50,  selfMultiplier: 2, globalBonus: 5, label: 'Essaim bleu' },
      { level: 100, selfMultiplier: 2, globalBonus: 8, label: 'Reine des eaux claires' },
    ],
  },
  {
    type: 'cobalt',
    baseIncome: 60, baseCost: 2_500,
    name: 'Anguille Cobalt', desc: 'Produit 60 Mana/s',
    requiredDepth: 1, emoji: '💧', depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0,  label: 'Peau cobalt' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5,  label: 'Courant électrique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 8,  label: 'Sinuosité parfaite' },
      { level: 100, selfMultiplier: 2, globalBonus: 12, label: 'Anguille prismatique' },
    ],
  },
  {
    type: 'nymph',
    baseIncome: 100, baseCost: 6_000,
    name: 'Nymphe des Eaux', desc: 'Produit 100 Mana/s',
    requiredDepth: 1, emoji: '🧜', depthLabel: 'Rivière Souterraine',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 0,  label: 'Chant aquatique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 5,  label: 'Magie de source' },
      { level: 50,  selfMultiplier: 2, globalBonus: 8,  label: 'Gardienne du courant' },
      { level: 100, selfMultiplier: 2, globalBonus: 15, label: 'Souveraine de la rivière' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 2 — Récif Corallien
  // ══════════════════════════════════════════════════════════════
  {
    type: 'diamond',
    baseIncome: 100, baseCost: 5_000,
    name: 'Poisson Diamant', desc: 'Produit 100 Mana/s',
    requiredDepth: 2, emoji: '🐡', depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Diamant brut' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Taille parfaite' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Diamant pur' },
      { level: 100, selfMultiplier: 2, globalBonus: 20, label: 'Diamant légendaire' },
    ],
  },
  {
    type: 'crab',
    baseIncome: 350, baseCost: 25_000,
    name: 'Crabe Cristal', desc: 'Produit 350 Mana/s',
    requiredDepth: 2, emoji: '🦀', depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Carapace nacrée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Pinces cristallines' },
      { level: 50,  selfMultiplier: 2, globalBonus: 10, label: 'Seigneur du récif' },
      { level: 100, selfMultiplier: 2, globalBonus: 15, label: 'Crabe légendaire' },
    ],
  },
  {
    type: 'snail',
    baseIncome: 600, baseCost: 80_000,
    name: 'Escargot Opale', desc: 'Produit 600 Mana/s',
    requiredDepth: 2, emoji: '🐚', depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Coquille irisée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Traînée de lumière' },
      { level: 50,  selfMultiplier: 2, globalBonus: 12, label: 'Opale vivante' },
      { level: 100, selfMultiplier: 2, globalBonus: 18, label: 'Escargot des merveilles' },
    ],
  },
  {
    type: 'shrimp',
    baseIncome: 1_000, baseCost: 200_000,
    name: 'Crevette Nacre', desc: 'Produit 1 000 Mana/s',
    requiredDepth: 2, emoji: '🦐', depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Carapace nacrée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 12, label: 'Danse des bulles' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Essaim nacré' },
      { level: 100, selfMultiplier: 2, globalBonus: 22, label: 'Crevette primordiale' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 3 — Abysses
  // ══════════════════════════════════════════════════════════════
  {
    type: 'abyssal',
    baseIncome: 1_000, baseCost: 100_000,
    name: 'Poisson Abyssal', desc: 'Produit 1 000 Mana/s',
    requiredDepth: 3, emoji: '🦑', depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Éveil abyssal' },
      { level: 25,  selfMultiplier: 2, globalBonus: 20, label: 'Profondeur infinie' },
      { level: 50,  selfMultiplier: 2, globalBonus: 30, label: 'Maître des abysses' },
      { level: 100, selfMultiplier: 2, globalBonus: 50, label: 'Seigneur des abysses' },
    ],
  },
  {
    type: 'octopus',
    baseIncome: 3_500, baseCost: 500_000,
    name: 'Pieuvre Ombre', desc: 'Produit 3 500 Mana/s',
    requiredDepth: 3, emoji: '🐙', depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Tentacules spectraux' },
      { level: 25,  selfMultiplier: 2, globalBonus: 15, label: 'Encre de ténèbres' },
      { level: 50,  selfMultiplier: 2, globalBonus: 20, label: 'Maîtresse des ombres' },
      { level: 100, selfMultiplier: 2, globalBonus: 30, label: 'Archiviste abyssale' },
    ],
  },
  {
    type: 'anemone',
    baseIncome: 6_000, baseCost: 1_500_000,
    name: 'Anémone Fantôme', desc: 'Produit 6 000 Mana/s',
    requiredDepth: 3, emoji: '🎭', depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Tentacules spectraux' },
      { level: 25,  selfMultiplier: 2, globalBonus: 15, label: 'Floraison abyssale' },
      { level: 50,  selfMultiplier: 2, globalBonus: 22, label: 'Fleur de ténèbres' },
      { level: 100, selfMultiplier: 2, globalBonus: 38, label: 'Anémone immortelle' },
    ],
  },
  {
    type: 'spectre',
    baseIncome: 10_000, baseCost: 5_000_000,
    name: 'Poisson Spectre', desc: 'Produit 10 000 Mana/s',
    requiredDepth: 3, emoji: '👻', depthLabel: 'Abysses',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 10, label: 'Forme éthérée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 18, label: 'Traversée des ombres' },
      { level: 50,  selfMultiplier: 2, globalBonus: 28, label: 'Maître de l\'invisible' },
      { level: 100, selfMultiplier: 2, globalBonus: 45, label: 'Entité spectrales' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 4 — Zone Hydrothermale
  // ══════════════════════════════════════════════════════════════
  {
    type: 'salamander',
    baseIncome: 10_000, baseCost: 2_000_000,
    name: 'Salamandre Ignée', desc: 'Produit 10 000 Mana/s',
    requiredDepth: 4, emoji: '🔥', depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 15, label: 'Flamme primordiale' },
      { level: 25,  selfMultiplier: 2, globalBonus: 20, label: 'Peau ignifugée' },
      { level: 50,  selfMultiplier: 2, globalBonus: 30, label: 'Maîtresse du feu' },
      { level: 100, selfMultiplier: 2, globalBonus: 40, label: 'Cœur volcanique' },
    ],
  },
  {
    type: 'eel',
    baseIncome: 35_000, baseCost: 10_000_000,
    name: 'Anguille de Feu', desc: 'Produit 35 000 Mana/s',
    requiredDepth: 4, emoji: '⚡', depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Décharge primaire' },
      { level: 25,  selfMultiplier: 2, globalBonus: 30, label: 'Courant thermique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 40, label: 'Arc électrique' },
      { level: 100, selfMultiplier: 2, globalBonus: 60, label: 'Foudre abyssale' },
    ],
  },
  {
    type: 'scorpion',
    baseIncome: 60_000, baseCost: 30_000_000,
    name: 'Scorpion de Magma', desc: 'Produit 60 000 Mana/s',
    requiredDepth: 4, emoji: '🦂', depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 15, label: 'Dard incandescent' },
      { level: 25,  selfMultiplier: 2, globalBonus: 25, label: 'Armure de lave' },
      { level: 50,  selfMultiplier: 2, globalBonus: 38, label: 'Venin de feu' },
      { level: 100, selfMultiplier: 2, globalBonus: 55, label: 'Scorpion primordial' },
    ],
  },
  {
    type: 'lava_snake',
    baseIncome: 100_000, baseCost: 100_000_000,
    name: 'Serpent de Lave', desc: 'Produit 100 000 Mana/s',
    requiredDepth: 4, emoji: '🐍', depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Écailles de braise' },
      { level: 25,  selfMultiplier: 2, globalBonus: 32, label: 'Ondulation magmatique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 48, label: 'Constricteur de lave' },
      { level: 100, selfMultiplier: 2, globalBonus: 70, label: 'Serpent du Krakatau' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 5 — Plaine Abyssale
  // ══════════════════════════════════════════════════════════════
  {
    type: 'jellyfish',
    baseIncome: 100_000, baseCost: 100_000_000,
    name: 'Méduse Bioluminescente', desc: 'Produit 100 000 Mana/s',
    requiredDepth: 5, emoji: '🫧', depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Voile lumineux' },
      { level: 25,  selfMultiplier: 2, globalBonus: 30, label: 'Pulsation de mana' },
      { level: 50,  selfMultiplier: 2, globalBonus: 40, label: 'Ballet de lumière' },
      { level: 100, selfMultiplier: 2, globalBonus: 50, label: 'Gardienne de la plaine' },
    ],
  },
  {
    type: 'shark',
    baseIncome: 350_000, baseCost: 500_000_000,
    name: 'Requin des Abysses', desc: 'Produit 350 000 Mana/s',
    requiredDepth: 5, emoji: '🦈', depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 25, label: 'Dents de cristal' },
      { level: 25,  selfMultiplier: 2, globalBonus: 40, label: 'Prédateur silencieux' },
      { level: 50,  selfMultiplier: 2, globalBonus: 60, label: 'Apex abyssal' },
      { level: 100, selfMultiplier: 2, globalBonus: 80, label: 'Terreur des profondeurs' },
    ],
  },
  {
    type: 'dolphin',
    baseIncome: 600_000, baseCost: 2_000_000_000,
    name: 'Dauphin Spectre', desc: 'Produit 600 000 Mana/s',
    requiredDepth: 5, emoji: '🐬', depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Sonar magique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 35, label: 'Bond spectral' },
      { level: 50,  selfMultiplier: 2, globalBonus: 55, label: 'Chant abyssal' },
      { level: 100, selfMultiplier: 2, globalBonus: 78, label: 'Guide des profondeurs' },
    ],
  },
  {
    type: 'whale',
    baseIncome: 1_000_000, baseCost: 5_000_000_000,
    name: 'Baleine des Abysses', desc: 'Produit 1 000 000 Mana/s',
    requiredDepth: 5, emoji: '🐋', depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 25, label: 'Souffle de mana' },
      { level: 25,  selfMultiplier: 2, globalBonus: 42, label: 'Chant des profondeurs' },
      { level: 50,  selfMultiplier: 2, globalBonus: 62, label: 'Tonnerre abyssal' },
      { level: 100, selfMultiplier: 2, globalBonus: 90, label: 'Baleine éternelle' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 6 — Fosse des Origines
  // ══════════════════════════════════════════════════════════════
  {
    type: 'dragon',
    baseIncome: 1_000_000, baseCost: 5_000_000_000,
    name: 'Dragon des Mers', desc: 'Produit 1 000 000 Mana/s',
    requiredDepth: 6, emoji: '🐉', depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 30,  label: 'Éveil draconique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 50,  label: 'Souffle d\'abîme' },
      { level: 50,  selfMultiplier: 2, globalBonus: 75,  label: 'Ancien gardien' },
      { level: 100, selfMultiplier: 2, globalBonus: 100, label: 'Dieu des profondeurs' },
    ],
  },
  {
    type: 'leviathan',
    baseIncome: 3_500_000, baseCost: 25_000_000_000,
    name: 'Léviathan Cristallin', desc: 'Produit 3 500 000 Mana/s',
    requiredDepth: 6, emoji: '💎', depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 40,  label: 'Éveil du colosse' },
      { level: 25,  selfMultiplier: 2, globalBonus: 70,  label: 'Armure de cristal' },
      { level: 50,  selfMultiplier: 2, globalBonus: 100, label: 'Titan des abysses' },
      { level: 100, selfMultiplier: 2, globalBonus: 150, label: 'Léviathan immortel' },
    ],
  },
  {
    type: 'plesio',
    baseIncome: 6_000_000, baseCost: 75_000_000_000,
    name: 'Plésiosaure Cristal', desc: 'Produit 6 000 000 Mana/s',
    requiredDepth: 6, emoji: '🦕', depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 35,  label: 'Éveil du Mésozoïque' },
      { level: 25,  selfMultiplier: 2, globalBonus: 60,  label: 'Nageur éternel' },
      { level: 50,  selfMultiplier: 2, globalBonus: 88,  label: 'Fossile vivant' },
      { level: 100, selfMultiplier: 2, globalBonus: 130, label: 'Gardien des origines' },
    ],
  },
  {
    type: 'basilisk',
    baseIncome: 10_000_000, baseCost: 250_000_000_000,
    name: 'Basilic des Origines', desc: 'Produit 10 000 000 Mana/s',
    requiredDepth: 6, emoji: '🦎', depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 40,  label: 'Regard pétrifiant' },
      { level: 25,  selfMultiplier: 2, globalBonus: 65,  label: 'Venin primordial' },
      { level: 50,  selfMultiplier: 2, globalBonus: 95,  label: 'Roi des reptiles' },
      { level: 100, selfMultiplier: 2, globalBonus: 140, label: 'Basilic immortel' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 7 — Nexus de Mana
  // ══════════════════════════════════════════════════════════════
  {
    type: 'egregore',
    baseIncome: 10_000_000, baseCost: 100_000_000_000,
    name: 'Égrégore Aquatique', desc: 'Produit 10 000 000 Mana/s',
    requiredDepth: 7, requiredPrestiges: 2, emoji: '✨', depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Conscience émergente' },
      { level: 25,  selfMultiplier: 2, globalBonus: 80,  label: 'Réseau psychique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 120, label: 'Esprit collectif' },
      { level: 100, selfMultiplier: 2, globalBonus: 200, label: 'Transcendance aquatique' },
    ],
  },
  {
    type: 'phoenix_nexus',
    baseIncome: 75_000_000, baseCost: 2_500_000_000_000,
    name: 'Phénix Aquatique', desc: 'Produit 75 000 000 Mana/s',
    requiredDepth: 7, requiredPrestiges: 2, emoji: '🦅', depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Renaissance ignée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 90,  label: 'Flamme de nexus' },
      { level: 50,  selfMultiplier: 2, globalBonus: 160, label: 'Cycle éternel' },
      { level: 100, selfMultiplier: 2, globalBonus: 260, label: 'Phénix omniscient' },
    ],
  },
  {
    type: 'nexus_spirit',
    baseIncome: 150_000_000, baseCost: 8_000_000_000_000,
    name: 'Esprit du Nexus', desc: 'Produit 150 000 000 Mana/s',
    requiredDepth: 7, requiredPrestiges: 3, emoji: '💫', depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 60,  label: 'Conscience du nexus' },
      { level: 25,  selfMultiplier: 2, globalBonus: 100, label: 'Flux de mana pur' },
      { level: 50,  selfMultiplier: 2, globalBonus: 200, label: 'Esprit omnipotent' },
      { level: 100, selfMultiplier: 2, globalBonus: 300, label: 'Nexus incarné' },
    ],
  },
  {
    type: 'celestial',
    baseIncome: 50_000_000, baseCost: 1_000_000_000_000,
    name: 'Poisson Céleste', desc: 'Produit 50 000 000 Mana/s — +1 💎/min',
    requiredDepth: 7, requiredPrestiges: 1, maxOwned: 1, emoji: '🌟', depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Naissance céleste' },
      { level: 25,  selfMultiplier: 2, globalBonus: 100, label: 'Ascension' },
      { level: 50,  selfMultiplier: 2, globalBonus: 200, label: 'Transcendance' },
      { level: 100, selfMultiplier: 2, globalBonus: 300, label: 'Divinité aquatique' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 8 — Cœur Volcanique
  // ══════════════════════════════════════════════════════════════
  {
    type: 'lava_spirit',
    baseIncome: 500_000_000, baseCost: 10_000_000_000_000,
    name: 'Esprit de Magma', desc: 'Produit 500 000 000 Mana/s',
    requiredDepth: 8, emoji: '🌋', depthLabel: 'Cœur Volcanique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 70,  label: 'Incandescence primaire' },
      { level: 25,  selfMultiplier: 2, globalBonus: 120, label: 'Fusion magmatique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 200, label: 'Naissance d\'un volcan' },
      { level: 100, selfMultiplier: 2, globalBonus: 350, label: 'Esprit du Feu éternel' },
    ],
  },
  {
    type: 'pyro_ray',
    baseIncome: 1_500_000_000, baseCost: 50_000_000_000_000,
    name: 'Raie Pyro', desc: 'Produit 1 500 000 000 Mana/s',
    requiredDepth: 8, emoji: '🌊', depthLabel: 'Cœur Volcanique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 75,  label: 'Ailes de braise' },
      { level: 25,  selfMultiplier: 2, globalBonus: 130, label: 'Battement magmatique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 220, label: 'Tsunami de lave' },
      { level: 100, selfMultiplier: 2, globalBonus: 380, label: 'Raie de l\'Apocalypse' },
    ],
  },
  {
    type: 'lava_titan',
    baseIncome: 5_000_000_000, baseCost: 200_000_000_000_000,
    name: 'Titan de Lave', desc: 'Produit 5 000 000 000 Mana/s',
    requiredDepth: 8, requiredPrestiges: 3, emoji: '🏔️', depthLabel: 'Cœur Volcanique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 80,  label: 'Montagne vivante' },
      { level: 25,  selfMultiplier: 2, globalBonus: 140, label: 'Tremblement primordial' },
      { level: 50,  selfMultiplier: 2, globalBonus: 240, label: 'Titan incandescent' },
      { level: 100, selfMultiplier: 2, globalBonus: 420, label: 'Cœur du monde' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 9 — Royaume Céleste
  // ══════════════════════════════════════════════════════════════
  {
    type: 'angel',
    baseIncome: 10_000_000_000, baseCost: 500_000_000_000_000,
    name: 'Ange des Eaux', desc: 'Produit 10 000 000 000 Mana/s',
    requiredDepth: 9, requiredPrestiges: 4, emoji: '🌠', depthLabel: 'Royaume Céleste',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 90,  label: 'Ailes de lumière' },
      { level: 25,  selfMultiplier: 2, globalBonus: 150, label: 'Bénédiction divine' },
      { level: 50,  selfMultiplier: 2, globalBonus: 250, label: 'Grâce éternelle' },
      { level: 100, selfMultiplier: 2, globalBonus: 450, label: 'Archange aquatique' },
    ],
  },
  {
    type: 'aurora_fish',
    baseIncome: 30_000_000_000, baseCost: 2_000_000_000_000_000,
    name: 'Poisson Aurore', desc: 'Produit 30 000 000 000 Mana/s',
    requiredDepth: 9, requiredPrestiges: 4, emoji: '🌌', depthLabel: 'Royaume Céleste',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 100, label: 'Voile boréal' },
      { level: 25,  selfMultiplier: 2, globalBonus: 170, label: 'Arc-en-ciel céleste' },
      { level: 50,  selfMultiplier: 2, globalBonus: 280, label: 'Danse aurorale' },
      { level: 100, selfMultiplier: 2, globalBonus: 500, label: 'Aurore infinie' },
    ],
  },
  {
    type: 'sun_fish',
    baseIncome: 100_000_000_000, baseCost: 5_000_000_000_000_000,
    name: 'Sol Aquatique', desc: 'Produit 100 000 000 000 Mana/s',
    requiredDepth: 9, requiredPrestiges: 5, emoji: '☀️', depthLabel: 'Royaume Céleste',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 110, label: 'Couronne solaire' },
      { level: 25,  selfMultiplier: 2, globalBonus: 185, label: 'Éruption céleste' },
      { level: 50,  selfMultiplier: 2, globalBonus: 300, label: 'Fusion nucléaire' },
      { level: 100, selfMultiplier: 2, globalBonus: 550, label: 'Soleil vivant' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 10 — Dimension Quantique
  // ══════════════════════════════════════════════════════════════
  {
    type: 'cyberfish',
    baseIncome: 500_000_000_000, baseCost: 1_000_000_000_000_000,
    name: 'Cyberpoisson', desc: 'Produit 500 000 000 000 Mana/s',
    requiredDepth: 10, requiredPrestiges: 6, emoji: '🤖', depthLabel: 'Dimension Quantique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 120, label: 'Firmware quantique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 200, label: 'Surcharge de mana' },
      { level: 50,  selfMultiplier: 2, globalBonus: 350, label: 'Intelligence artificielle' },
      { level: 100, selfMultiplier: 2, globalBonus: 600, label: 'Singularité numérique' },
    ],
  },
  {
    type: 'prism_manta',
    baseIncome: 2_000_000_000_000, baseCost: 4_000_000_000_000_000,
    name: 'Manta Prisme', desc: 'Produit 2 000 000 000 000 Mana/s',
    requiredDepth: 10, requiredPrestiges: 6, emoji: '🔷', depthLabel: 'Dimension Quantique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 130, label: 'Réfraction quantique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 220, label: 'Ailes de données' },
      { level: 50,  selfMultiplier: 2, globalBonus: 370, label: 'Superposition d\'états' },
      { level: 100, selfMultiplier: 2, globalBonus: 650, label: 'Manta omnidimensionnelle' },
    ],
  },
  {
    type: 'quantum',
    baseIncome: 10_000_000_000_000, baseCost: 9_000_000_000_000_000,
    name: 'Entité Quantique', desc: 'Produit 10 000 000 000 000 Mana/s',
    requiredDepth: 10, requiredPrestiges: 8, emoji: '🔬', depthLabel: 'Dimension Quantique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 150, label: 'Superposition quantique' },
      { level: 25,  selfMultiplier: 2, globalBonus: 250, label: 'Intrication magique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 400, label: 'Effondrement de fonction' },
      { level: 100, selfMultiplier: 2, globalBonus: 700, label: 'Conscience quantique' },
    ],
  },
];

export const getSelfMilestoneMultiplier = (fish: PoissonInstance, levelReduction = 0): number => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return 1;
  return type.milestones
    .filter(m => fish.level >= Math.max(1, m.level - levelReduction))
    .reduce((acc, m) => acc * m.selfMultiplier, 1);
};

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

export const getNextMilestone = (fish: PoissonInstance, levelReduction = 0): MilestoneBoost | null => {
  const type = FISH_TYPES.find(t => t.type === fish.type);
  if (!type) return null;
  return type.milestones.find(m => fish.level < Math.max(1, m.level - levelReduction)) ?? null;
};
