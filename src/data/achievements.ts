import Decimal from 'break_infinity.js';
import type { PoissonInstance } from '../store/useGameStore';

export interface AchievementDef {
  id: string;
  name: string;
  description: string;
  gemReward: number;
  check: (state: {
    poissons: PoissonInstance[];
    pondDepth: number;
    mana: Decimal;
    prestiges?: number;
    researchUnlocked?: string[];
    unlockedAchievementIds?: string[];
  }) => boolean;
}

export const ACHIEVEMENTS: AchievementDef[] = [
  // ─── Nombre de poissons ──────────────────────────────────────────
  {
    id: 'first_fish',
    name: 'Premier Plongeon',
    description: 'Acheter votre premier poisson',
    gemReward: 5,
    check: (s) => s.poissons.length >= 1,
  },
  {
    id: 'fish_10',
    name: 'Banc de Poissons',
    description: 'Posséder 10 poissons',
    gemReward: 10,
    check: (s) => s.poissons.length >= 10,
  },
  {
    id: 'fish_50',
    name: 'Armée Aquatique',
    description: 'Posséder 50 poissons',
    gemReward: 25,
    check: (s) => s.poissons.length >= 50,
  },
  {
    id: 'fish_100',
    name: 'Mer Intérieure',
    description: 'Posséder 100 poissons',
    gemReward: 50,
    check: (s) => s.poissons.length >= 100,
  },
  {
    id: 'fish_200',
    name: 'Récif Vivant',
    description: 'Posséder 200 poissons',
    gemReward: 100,
    check: (s) => s.poissons.length >= 200,
  },
  {
    id: 'fish_500',
    name: 'Océan Personnel',
    description: 'Posséder 500 poissons',
    gemReward: 250,
    check: (s) => s.poissons.length >= 500,
  },

  // ─── Premières espèces ───────────────────────────────────────────
  {
    id: 'first_carpe',
    name: 'Carpe Bienvenue',
    description: 'Acheter une Carpe Dorée',
    gemReward: 8,
    check: (s) => s.poissons.some(f => f.type === 'carpe'),
  },
  {
    id: 'first_ruby',
    name: 'Eaux Intermédiaires',
    description: 'Acheter un Poisson Rubis',
    gemReward: 10,
    check: (s) => s.poissons.some(f => f.type === 'ruby'),
  },
  {
    id: 'first_dragonfly',
    name: 'Ailes de Saphir',
    description: 'Acheter une Libellule de Saphir',
    gemReward: 15,
    check: (s) => s.poissons.some(f => f.type === 'dragonfly'),
  },
  {
    id: 'first_diamond',
    name: 'Plongée Profonde',
    description: 'Acheter un Poisson Diamant',
    gemReward: 20,
    check: (s) => s.poissons.some(f => f.type === 'diamond'),
  },
  {
    id: 'first_crab',
    name: 'Gardien du Récif',
    description: 'Acheter un Crabe Cristal',
    gemReward: 25,
    check: (s) => s.poissons.some(f => f.type === 'crab'),
  },
  {
    id: 'first_abyssal',
    name: 'L\'Appel des Abysses',
    description: 'Acheter un Poisson Abyssal',
    gemReward: 30,
    check: (s) => s.poissons.some(f => f.type === 'abyssal'),
  },
  {
    id: 'first_octopus',
    name: 'Dans l\'Ombre',
    description: 'Acheter une Pieuvre Ombre',
    gemReward: 35,
    check: (s) => s.poissons.some(f => f.type === 'octopus'),
  },
  {
    id: 'first_salamander',
    name: 'Baptême du Feu',
    description: 'Acheter une Salamandre Ignée',
    gemReward: 40,
    check: (s) => s.poissons.some(f => f.type === 'salamander'),
  },
  {
    id: 'first_eel',
    name: 'Courant Thermique',
    description: 'Acheter une Anguille de Feu',
    gemReward: 50,
    check: (s) => s.poissons.some(f => f.type === 'eel'),
  },
  {
    id: 'first_jellyfish',
    name: 'Lumière dans les Ténèbres',
    description: 'Acheter une Méduse Bioluminescente',
    gemReward: 60,
    check: (s) => s.poissons.some(f => f.type === 'jellyfish'),
  },
  {
    id: 'first_shark',
    name: 'Apex Abyssal',
    description: 'Acheter un Requin des Abysses',
    gemReward: 75,
    check: (s) => s.poissons.some(f => f.type === 'shark'),
  },
  {
    id: 'first_dragon',
    name: 'L\'Ancien Éveillé',
    description: 'Acheter un Dragon des Mers',
    gemReward: 100,
    check: (s) => s.poissons.some(f => f.type === 'dragon'),
  },
  {
    id: 'first_leviathan',
    name: 'Colosse des Profondeurs',
    description: 'Acheter un Léviathan Cristallin',
    gemReward: 150,
    check: (s) => s.poissons.some(f => f.type === 'leviathan'),
  },
  {
    id: 'first_egregore',
    name: 'Conscience Collective',
    description: 'Acheter un Égrégore Aquatique',
    gemReward: 200,
    check: (s) => s.poissons.some(f => f.type === 'egregore'),
  },
  {
    id: 'first_celestial',
    name: 'Divinité Aquatique',
    description: 'Acheter le Poisson Céleste',
    gemReward: 500,
    check: (s) => s.poissons.some(f => f.type === 'celestial'),
  },

  // ─── Profondeur ──────────────────────────────────────────────────
  {
    id: 'depth_1',
    name: 'Creuseur',
    description: 'Atteindre la profondeur 1',
    gemReward: 10,
    check: (s) => s.pondDepth >= 1,
  },
  {
    id: 'depth_2',
    name: 'Explorateur',
    description: 'Atteindre la profondeur 2',
    gemReward: 20,
    check: (s) => s.pondDepth >= 2,
  },
  {
    id: 'depth_3',
    name: 'Abyssal',
    description: 'Atteindre la profondeur 3',
    gemReward: 30,
    check: (s) => s.pondDepth >= 3,
  },
  {
    id: 'depth_4',
    name: 'Zone de Feu',
    description: 'Atteindre la profondeur 4',
    gemReward: 50,
    check: (s) => s.pondDepth >= 4,
  },
  {
    id: 'depth_5',
    name: 'La Grande Plaine',
    description: 'Atteindre la profondeur 5',
    gemReward: 75,
    check: (s) => s.pondDepth >= 5,
  },
  {
    id: 'depth_6',
    name: 'La Fosse',
    description: 'Atteindre la profondeur 6',
    gemReward: 100,
    check: (s) => s.pondDepth >= 6,
  },
  {
    id: 'depth_7',
    name: 'Nexus de Mana',
    description: 'Atteindre la profondeur maximale — le Nexus',
    gemReward: 200,
    check: (s) => s.pondDepth >= 7,
  },

  // ─── Mana ────────────────────────────────────────────────────────
  {
    id: 'mana_1k',
    name: 'Réserve de Mana',
    description: 'Accumuler 1 000 Mana',
    gemReward: 5,
    check: (s) => s.mana.gte(1_000),
  },
  {
    id: 'mana_1m',
    name: 'Torrent de Mana',
    description: 'Accumuler 1 000 000 Mana',
    gemReward: 15,
    check: (s) => s.mana.gte(1_000_000),
  },
  {
    id: 'mana_1b',
    name: 'Océan de Mana',
    description: 'Accumuler 1 000 000 000 Mana',
    gemReward: 30,
    check: (s) => s.mana.gte(1_000_000_000),
  },
  {
    id: 'mana_1t',
    name: 'Déluge de Mana',
    description: 'Accumuler 1 000 000 000 000 Mana',
    gemReward: 60,
    check: (s) => s.mana.gte(new Decimal('1e12')),
  },
  {
    id: 'mana_1q',
    name: 'Avalanche de Mana',
    description: 'Accumuler 1 000 000 000 000 000 Mana',
    gemReward: 120,
    check: (s) => s.mana.gte(new Decimal('1e15')),
  },
  {
    id: 'mana_1qi',
    name: 'Singularité de Mana',
    description: 'Accumuler 1e18 Mana',
    gemReward: 250,
    check: (s) => s.mana.gte(new Decimal('1e18')),
  },

  // ─── Niveaux de poissons ─────────────────────────────────────────
  {
    id: 'fish_level_10',
    name: 'Vétéran',
    description: 'Améliorer un poisson au niveau 10',
    gemReward: 10,
    check: (s) => s.poissons.some(f => f.level >= 10),
  },
  {
    id: 'fish_level_25',
    name: 'Champion',
    description: 'Améliorer un poisson au niveau 25',
    gemReward: 20,
    check: (s) => s.poissons.some(f => f.level >= 25),
  },
  {
    id: 'fish_level_50',
    name: 'Élite',
    description: 'Améliorer un poisson au niveau 50',
    gemReward: 40,
    check: (s) => s.poissons.some(f => f.level >= 50),
  },
  {
    id: 'fish_level_100',
    name: 'Légende',
    description: 'Améliorer un poisson au niveau maximum (100)',
    gemReward: 100,
    check: (s) => s.poissons.some(f => f.level >= 100),
  },
  {
    id: 'five_maxed',
    name: 'Armée de Légendes',
    description: 'Avoir 5 poissons au niveau 100',
    gemReward: 300,
    check: (s) => s.poissons.filter(f => f.level >= 100).length >= 5,
  },

  // ─── Prestige ────────────────────────────────────────────────────
  {
    id: 'first_prestige',
    name: 'Renaissance',
    description: 'Effectuer votre premier Prestige',
    gemReward: 50,
    check: (s) => (s.prestiges ?? 0) >= 1,
  },
  {
    id: 'prestige_3',
    name: 'Cycle Éternel',
    description: 'Effectuer 3 Prestiges',
    gemReward: 100,
    check: (s) => (s.prestiges ?? 0) >= 3,
  },
  {
    id: 'prestige_5',
    name: 'Maître du Recommencement',
    description: 'Effectuer 5 Prestiges',
    gemReward: 200,
    check: (s) => (s.prestiges ?? 0) >= 5,
  },
  {
    id: 'prestige_10',
    name: 'Transcendance',
    description: 'Effectuer 10 Prestiges',
    gemReward: 500,
    check: (s) => (s.prestiges ?? 0) >= 10,
  },

  // ─── Recherche ───────────────────────────────────────────────────
  {
    id: 'first_research',
    name: 'Premier Corail',
    description: 'Débloquer votre premier nœud de Corail de Prestige',
    gemReward: 15,
    check: (s) => (s.researchUnlocked?.length ?? 0) >= 1,
  },
  {
    id: 'research_5',
    name: 'Chercheur',
    description: 'Débloquer 5 nœuds de Corail de Prestige',
    gemReward: 40,
    check: (s) => (s.researchUnlocked?.length ?? 0) >= 5,
  },
  {
    id: 'research_10',
    name: 'Scientifique des Profondeurs',
    description: 'Débloquer 10 nœuds de Corail de Prestige',
    gemReward: 100,
    check: (s) => (s.researchUnlocked?.length ?? 0) >= 10,
  },

  // ─── Diversité ───────────────────────────────────────────────────
  {
    id: 'ruby_10',
    name: 'Banc de Rubis',
    description: 'Posséder 10 Poissons Rubis',
    gemReward: 20,
    check: (s) => s.poissons.filter(f => f.type === 'ruby').length >= 10,
  },
  {
    id: 'abyssal_5',
    name: 'Essaim Abyssal',
    description: 'Posséder 5 Poissons Abyssaux',
    gemReward: 50,
    check: (s) => s.poissons.filter(f => f.type === 'abyssal').length >= 5,
  },
  {
    id: 'all_biomes',
    name: 'Cartographe des Profondeurs',
    description: 'Posséder au moins un poisson de chaque biome (profondeurs 0-4)',
    gemReward: 150,
    check: (s) => {
      const types = new Set(s.poissons.map(f => f.type));
      return ['gold', 'ruby', 'diamond', 'abyssal', 'salamander'].every(t => types.has(t));
    },
  },
];
