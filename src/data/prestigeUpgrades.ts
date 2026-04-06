export interface PrestigeUpgradeDef {
  id: string;
  name: string;
  description: string;
  flavour: string;
  cost: number; // perles
  requires: string | null;
  tier: 1 | 2 | 3;
}

export const PRESTIGE_UPGRADES: PrestigeUpgradeDef[] = [
  // ─── Tier 1 — Qualité de vie ─────────────────────────────────────
  {
    id: 'prest_mana',
    name: 'Mémoire des Eaux',
    description: 'Démarrer avec 5 000 Mana après un Prestige',
    flavour: 'L\'eau se souvient de ce qui l\'a traversée.',
    cost: 1,
    requires: null,
    tier: 1,
  },
  {
    id: 'prest_creusage',
    name: 'Creusage Hérité',
    description: 'Le premier creusage d\'étang coûte -50%',
    flavour: 'Les sillons précédents guident le burin.',
    cost: 1,
    requires: null,
    tier: 1,
  },
  {
    id: 'prest_level',
    name: 'Vitesse Initiale',
    description: 'Les poissons achetés démarrent au niveau 3',
    flavour: 'Chaque génération profite des leçons de la précédente.',
    cost: 2,
    requires: null,
    tier: 1,
  },
  {
    id: 'prest_pearl_bonus',
    name: 'Résonance Abyssale',
    description: '+30% de Perles gagnées par Prestige',
    flavour: 'L\'abîme révèle plus à ceux qui l\'ont déjà touché.',
    cost: 2,
    requires: 'prest_mana',
    tier: 1,
  },

  // ─── Tier 2 — Impactant ──────────────────────────────────────────
  {
    id: 'prest_keep_fish',
    name: 'Héritage Cristallin',
    description: 'Conserver 10% de vos poissons après un Prestige',
    flavour: 'Certains êtres transcendent les cycles.',
    cost: 4,
    requires: 'prest_level',
    tier: 2,
  },
  {
    id: 'prest_depth_1',
    name: 'Profondeur Mémorisée',
    description: 'Démarrer à la profondeur 1 après un Prestige',
    flavour: 'La roche garde l\'empreinte du premier coup de pelle.',
    cost: 5,
    requires: 'prest_creusage',
    tier: 2,
  },
  {
    id: 'prest_pearl_bonus_2',
    name: 'Accumulation Éternelle',
    description: '+50% de Perles gagnées par Prestige',
    flavour: 'L\'expérience se cristallise en sagesse.',
    cost: 5,
    requires: 'prest_pearl_bonus',
    tier: 2,
  },
  {
    id: 'prest_level_10',
    name: 'Évolution Accélérée',
    description: 'Les poissons achetés démarrent au niveau 10',
    flavour: 'Dix générations de savoir encodées dans chaque écaille.',
    cost: 6,
    requires: 'prest_keep_fish',
    tier: 2,
  },

  // ─── Tier 3 — Puissant / Fin de partie ──────────────────────────
  {
    id: 'prest_depth_2',
    name: 'Éveil Abyssal',
    description: 'Démarrer à la profondeur 2 après un Prestige',
    flavour: 'La mémoire de l\'étang va plus loin que les yeux ne voient.',
    cost: 10,
    requires: 'prest_depth_1',
    tier: 3,
  },
  {
    id: 'prest_celestial_cost',
    name: 'Essence Légendaire',
    description: 'Le Poisson Céleste coûte -60%',
    flavour: 'Le divin reconnaît ceux qui l\'ont déjà approché.',
    cost: 10,
    requires: null,
    tier: 3,
  },
  {
    id: 'prest_global_mult',
    name: 'Transcendance',
    description: '+100% de revenu global permanent (survive aux Prestiges)',
    flavour: 'Au-delà du cycle, il n\'y a que la lumière.',
    cost: 12,
    requires: 'prest_level_10',
    tier: 3,
  },
  {
    id: 'prest_mana_max',
    name: 'Océan Intérieur',
    description: 'Démarrer avec 1 000 000 Mana après un Prestige',
    flavour: 'Ce qui était accumulation devient nature.',
    cost: 15,
    requires: 'prest_depth_2',
    tier: 3,
  },
];
