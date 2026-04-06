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
  }) => boolean;
}

export const ACHIEVEMENTS: AchievementDef[] = [
  // Poissons
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

  // Espèces
  {
    id: 'first_ruby',
    name: 'Eaux Intermédiaires',
    description: 'Acheter un Poisson Rubis',
    gemReward: 10,
    check: (s) => s.poissons.some(f => f.type === 'ruby'),
  },
  {
    id: 'first_diamond',
    name: 'Plongée Profonde',
    description: 'Acheter un Poisson Diamant',
    gemReward: 20,
    check: (s) => s.poissons.some(f => f.type === 'diamond'),
  },
  {
    id: 'first_abyssal',
    name: 'L\'Appel des Abysses',
    description: 'Acheter un Poisson Abyssal',
    gemReward: 30,
    check: (s) => s.poissons.some(f => f.type === 'abyssal'),
  },

  // Profondeur
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
    name: 'Fond des Océans',
    description: 'Atteindre la profondeur maximale',
    gemReward: 50,
    check: (s) => s.pondDepth >= 4,
  },

  // Mana
  {
    id: 'mana_1k',
    name: 'Réserve de Mana',
    description: 'Accumuler 1 000 Mana',
    gemReward: 5,
    check: (s) => s.mana.gte(1000),
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

  // Niveaux de poissons
  {
    id: 'fish_level_10',
    name: 'Vétéran',
    description: 'Améliorer un poisson au niveau 10',
    gemReward: 10,
    check: (s) => s.poissons.some(f => f.level >= 10),
  },
  {
    id: 'fish_level_50',
    name: 'Élite',
    description: 'Améliorer un poisson au niveau 50',
    gemReward: 25,
    check: (s) => s.poissons.some(f => f.level >= 50),
  },
  {
    id: 'fish_level_100',
    name: 'Légende',
    description: 'Améliorer un poisson au niveau maximum (100)',
    gemReward: 100,
    check: (s) => s.poissons.some(f => f.level >= 100),
  },
];
