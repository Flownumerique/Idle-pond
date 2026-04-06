import Decimal from 'break_infinity.js';
import type { PoissonInstance } from '../store/useGameStore';

export interface ChallengeDef {
  id: string;
  name: string;
  description: string;
  pearlReward: number;
  check: (state: {
    poissons: PoissonInstance[];
    pondDepth: number;
    researchUnlocked: string[];
    sessionManaEarned: Decimal;
  }) => boolean;
}

export const CHALLENGE_POOL: ChallengeDef[] = [
  {
    id: 'ch_depth_1',
    name: 'Premières Profondeurs',
    description: 'Atteindre la profondeur 1',
    pearlReward: 1,
    check: (s) => s.pondDepth >= 1,
  },
  {
    id: 'ch_depth_2',
    name: 'Plongée Confirmée',
    description: 'Atteindre la profondeur 2',
    pearlReward: 2,
    check: (s) => s.pondDepth >= 2,
  },
  {
    id: 'ch_depth_3',
    name: 'Au Cœur des Eaux',
    description: 'Atteindre la profondeur 3',
    pearlReward: 3,
    check: (s) => s.pondDepth >= 3,
  },
  {
    id: 'ch_fish_10',
    name: 'Petit Banc',
    description: 'Posséder 10 poissons',
    pearlReward: 1,
    check: (s) => s.poissons.length >= 10,
  },
  {
    id: 'ch_fish_30',
    name: 'Grand Banc',
    description: 'Posséder 30 poissons',
    pearlReward: 2,
    check: (s) => s.poissons.length >= 30,
  },
  {
    id: 'ch_fish_50',
    name: 'Armée des Eaux',
    description: 'Posséder 50 poissons',
    pearlReward: 3,
    check: (s) => s.poissons.length >= 50,
  },
  {
    id: 'ch_ruby',
    name: 'Sang de Rubis',
    description: 'Acheter un Poisson Rubis',
    pearlReward: 1,
    check: (s) => s.poissons.some(f => f.type === 'ruby'),
  },
  {
    id: 'ch_diamond',
    name: 'Éclat de Diamant',
    description: 'Acheter un Poisson Diamant',
    pearlReward: 2,
    check: (s) => s.poissons.some(f => f.type === 'diamond'),
  },
  {
    id: 'ch_level_10',
    name: 'Vétéran',
    description: 'Améliorer un poisson au niveau 10',
    pearlReward: 1,
    check: (s) => s.poissons.some(f => f.level >= 10),
  },
  {
    id: 'ch_level_25',
    name: 'Élite',
    description: 'Améliorer un poisson au niveau 25',
    pearlReward: 2,
    check: (s) => s.poissons.some(f => f.level >= 25),
  },
  {
    id: 'ch_level_50',
    name: 'Champion',
    description: 'Améliorer un poisson au niveau 50',
    pearlReward: 3,
    check: (s) => s.poissons.some(f => f.level >= 50),
  },
  {
    id: 'ch_mana_100k',
    name: 'Torrent de Mana',
    description: 'Gagner 100 000 Mana dans cette session',
    pearlReward: 2,
    check: (s) => s.sessionManaEarned.gte(100_000),
  },
  {
    id: 'ch_mana_10m',
    name: 'Déluge de Mana',
    description: 'Gagner 10 000 000 Mana dans cette session',
    pearlReward: 3,
    check: (s) => s.sessionManaEarned.gte(10_000_000),
  },
  {
    id: 'ch_research_3',
    name: 'Chercheur',
    description: 'Débloquer 3 recherches',
    pearlReward: 2,
    check: (s) => s.researchUnlocked.length >= 3,
  },
  {
    id: 'ch_research_all_bio',
    name: 'Biologiste',
    description: 'Compléter la branche Biologie',
    pearlReward: 4,
    check: (s) => ['bio_1', 'bio_2', 'bio_3', 'bio_4'].every(id => s.researchUnlocked.includes(id)),
  },
];

/** Sélection déterministe de 3 défis selon la date du jour */
export const getDailyChallengeIds = (): string[] => {
  const d = new Date();
  let seed = d.getFullYear() * 10000 + (d.getMonth() + 1) * 100 + d.getDate();
  const available = [...CHALLENGE_POOL.map(c => c.id)];
  const result: string[] = [];
  for (let i = 0; i < 3 && available.length > 0; i++) {
    seed = (seed * 1664525 + 1013904223) & 0x7fffffff;
    const idx = Math.abs(seed) % available.length;
    result.push(available[idx]);
    available.splice(idx, 1);
  }
  return result;
};
