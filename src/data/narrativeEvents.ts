export interface NarrativeEvent {
  id: string;
  text: string;
  /** Profondeur minimale requise pour que l'événement apparaisse */
  minDepth?: number;
  /** Type de poisson requis dans l'étang */
  requiresFishType?: string;
}

export const NARRATIVE_EVENTS: NarrativeEvent[] = [
  // ─── Surface (depth 0-1) ─────────────────────────────────────────
  {
    id: 'surface_ripple',
    text: 'Un frisson parcourt la surface de l\'étang. Les Poissons Or se regroupent en cercles parfaits.',
  },
  {
    id: 'morning_glow',
    text: 'La lumière du matin traverse l\'eau en colonnes dorées. La production de mana semble plus vive que d\'ordinaire.',
  },
  {
    id: 'bubble_trail',
    text: 'Une piste de bulles remonte des profondeurs. Quelque chose se réveille en dessous.',
  },
  {
    id: 'golden_reflection',
    text: 'Les écailles des Poissons Or captent la lumière ambiante et forment un motif étrange sur le fond de l\'étang.',
  },
  {
    id: 'carpe_dance',
    text: 'Deux Carpes Dorées nagent en spirale synchronisée. Les anciens disent que cela annonce une abondance de mana.',
    requiresFishType: 'carpe',
  },

  // ─── Couches intermédiaires (depth 1-2) ──────────────────────────
  {
    id: 'ruby_flash',
    text: 'Un éclair rouge traverse l\'eau. Un Poisson Rubis vient d\'activer tous ses jalons simultanément.',
    minDepth: 1,
    requiresFishType: 'ruby',
  },
  {
    id: 'mineral_vein',
    text: 'Une nouvelle veine de minerai magique est apparue dans la paroi. L\'étang vibre d\'une fréquence nouvelle.',
    minDepth: 1,
  },
  {
    id: 'crystal_song',
    text: 'Les cristaux du récif corallien résonnent d\'un son inaudible pour l\'oreille humaine. Les poissons l\'entendent, eux.',
    minDepth: 2,
    requiresFishType: 'diamond',
  },
  {
    id: 'deep_pressure',
    text: 'La pression augmente imperceptiblement. Les poissons des profondeurs semblent plus alertes.',
    minDepth: 2,
  },
  {
    id: 'dragonfly_swarm',
    text: 'Un essaim de Libellules de Saphir trace des motifs dans l\'eau. Une carte ? Une prophétie ? Personne ne sait.',
    minDepth: 1,
    requiresFishType: 'dragonfly',
  },

  // ─── Abysses (depth 3) ───────────────────────────────────────────
  {
    id: 'abyss_call',
    text: 'Un son grave et lointain monte des abysses. Les Poissons Abyssaux s\'immobilisent puis reprennent leur nage, comme rien n\'était.',
    minDepth: 3,
    requiresFishType: 'abyssal',
  },
  {
    id: 'unknown_light',
    text: 'Une lumière inconnue pulse brièvement au fond de l\'étang. Ni or, ni rubis, ni diamant — quelque chose d\'autre.',
    minDepth: 3,
  },
  {
    id: 'octopus_message',
    text: 'La Pieuvre Ombre change de couleur en motifs rapides. Les chercheurs qui ont étudié ces séquences affirment qu\'elles répètent toujours la même phrase : "Creuse encore."',
    minDepth: 3,
    requiresFishType: 'octopus',
  },
  {
    id: 'star_eyes',
    text: 'Les yeux des Poissons Abyssaux reflètent des constellations inconnues. Ces étoiles n\'existent dans aucun atlas astronomique.',
    minDepth: 3,
    requiresFishType: 'abyssal',
  },

  // ─── Zone hydrothermale (depth 4) ────────────────────────────────
  {
    id: 'lava_rumble',
    text: 'Les cheminées hydrothermales crachent simultanément. La chaleur augmente de plusieurs degrés. La mana aussi.',
    minDepth: 4,
  },
  {
    id: 'salamander_rite',
    text: 'La Salamandre Ignée plonge directement dans un panache volcanique. Elle en ressort plus brillante qu\'avant.',
    minDepth: 4,
    requiresFishType: 'salamander',
  },
  {
    id: 'eel_arc',
    text: 'L\'Anguille de Feu génère un arc électrique visible à travers toute la couche hydrothermale. Brièvement, l\'eau prend une teinte violette.',
    minDepth: 4,
    requiresFishType: 'eel',
  },
  {
    id: 'thermal_pulse',
    text: 'Une vague de chaleur magique monte du bas. Pendant quelques secondes, la mana de tous les poissons tremble avant de reprendre son cours normal — mais légèrement amplifiée.',
    minDepth: 4,
  },

  // ─── Plaine abyssale (depth 5) ────────────────────────────────────
  {
    id: 'jellyfish_drift',
    text: 'Des dizaines de Méduses dérivent en formation parfaite. Elles s\'orientent toutes dans la même direction — vers le bas.',
    minDepth: 5,
    requiresFishType: 'jellyfish',
  },
  {
    id: 'shark_circle',
    text: 'Le Requin des Abysses tourne en cercles concentriques autour d\'un point précis. Il sait quelque chose que vous ne savez pas encore.',
    minDepth: 5,
    requiresFishType: 'shark',
  },
  {
    id: 'abyssal_wind',
    text: 'Un courant traverse la plaine abyssale — impossible à l\'échelle de la profondeur. La mana se densifie dans son sillage.',
    minDepth: 5,
  },

  // ─── Fosse des origines (depth 6) ────────────────────────────────
  {
    id: 'dragon_breath',
    text: 'Le Dragon des Mers exhale lentement. Sa respiration crée des vortex de mana concentrée qui persistent plusieurs minutes.',
    minDepth: 6,
    requiresFishType: 'dragon',
  },
  {
    id: 'leviathan_stir',
    text: 'Le Léviathan Cristallin se retourne dans les profondeurs. Un fragment de cristal se détache et tombe — il vaut plus que tout votre étang réuni.',
    minDepth: 6,
    requiresFishType: 'leviathan',
  },
  {
    id: 'primal_inscription',
    text: 'Une nouvelle inscription apparaît sur la paroi de la fosse. Elle n\'y était pas hier. Elle décrit exactement votre étang, en détail.',
    minDepth: 6,
  },
  {
    id: 'origin_pulse',
    text: 'Un pouls lent et régulier monte de la fosse des origines. Il bat exactement au rythme de la production de mana de votre étang. Coïncidence ?',
    minDepth: 6,
  },

  // ─── Nexus de Mana (depth 7) ──────────────────────────────────────
  {
    id: 'nexus_awakening',
    text: 'Le Nexus vibre. Les filaments de mana pure changent de couleur, du doré au blanc brillant. Quelque chose approche de son plein potentiel.',
    minDepth: 7,
  },
  {
    id: 'egregore_vision',
    text: 'L\'Égrégore projette une image dans votre esprit : un étang infini, des poissons innombrables, une lumière sans source. Puis cela disparaît.',
    minDepth: 7,
    requiresFishType: 'egregore',
  },
  {
    id: 'celestial_gaze',
    text: 'Le Poisson Céleste vous regarde directement pendant une longue seconde. Vous avez la certitude absolue qu\'il vous connaît depuis bien avant votre naissance.',
    minDepth: 7,
    requiresFishType: 'celestial',
  },
  {
    id: 'mana_overflow',
    text: 'La mana déborde brièvement de l\'étang, remontant sous forme de vapeur lumineuse. Quiconque la voit dit ressentir une paix inexplicable.',
    minDepth: 7,
  },
];

/** Retourne les événements disponibles selon l'état actuel */
export const getAvailableEvents = (
  pondDepth: number,
  fishTypes: string[]
): NarrativeEvent[] => {
  return NARRATIVE_EVENTS.filter(e => {
    if (e.minDepth !== undefined && pondDepth < e.minDepth) return false;
    if (e.requiresFishType && !fishTypes.includes(e.requiresFishType)) return false;
    return true;
  });
};

/** Sélectionne un événement aléatoire parmi les disponibles */
export const pickRandomEvent = (pondDepth: number, fishTypes: string[]): NarrativeEvent | null => {
  const available = getAvailableEvents(pondDepth, fishTypes);
  if (available.length === 0) return null;
  return available[Math.floor(Math.random() * available.length)];
};
