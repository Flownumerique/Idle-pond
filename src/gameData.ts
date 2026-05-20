export interface Biome {
  depth: number;
  name: string;
  blurb: string;
  palette: { top: string; mid: string; deep: string };
  upgradeCost: number;
}

export interface Fish {
  id: string;
  name: string;
  desc: string;
  depth: number;
  base: number;
  cost: number;
  requiredPrestiges?: number;
  maxOwned?: number;
}

export interface ResearchNode {
  id: string;
  label: string;
  effect: string;
  cost: number;
  requires: string | null;
}

export interface ResearchBranch {
  id: string;
  name: string;
  hue: number;
  desc: string;
  nodes: ResearchNode[];
}

export interface Achievement {
  id: string;
  name: string;
  desc: string;
  reward: number;
  check: (s: { mana: number; depth: number; prestiges: number; researchCount: number; totalFish: number; maxLevel: number }) => boolean;
}

export interface JournalEntry {
  depth: number;
  name: string;
  text: string;
}

export interface RunUpgrade {
  id: string;
  name: string;
  desc: string;
  cost: number;
  icon: string;
  requires?: string;
}

export interface DailyChallenge {
  id: string;
  name: string;
  desc: string;
  progress: number;
  reward: number;
}

export interface PearlUpgrade {
  id: string;
  name: string;
  desc: string;
  cost: number;
  icon: string;
  requires?: string;
}

export interface PrestigeUpgrade {
  id: string;
  name: string;
  desc: string;
  cost: number;
  icon: string;
  requires?: string;
}

export interface NarrativeEvent {
  id: string;
  text: string;
  minDepth: number;
}

export const BIOMES: Biome[] = [
  {
    depth: 0,
    name: "Surface Lake",
    blurb: "Shallow waters bathed in sunlight. Reeds, lily pads, golden reflections.",
    palette: { top: "oklch(0.86 0.05 88)", mid: "oklch(0.72 0.07 195)", deep: "oklch(0.55 0.07 210)" },
    upgradeCost: 500,
  },
  {
    depth: 1,
    name: "Underground River",
    blurb: "A swift current carves through stone galleries. Cold, crystalline, lit by ruby seams.",
    palette: { top: "oklch(0.68 0.05 200)", mid: "oklch(0.5 0.06 215)", deep: "oklch(0.36 0.07 230)" },
    upgradeCost: 5_000,
  },
  {
    depth: 2,
    name: "Coral Reef",
    blurb: "Bioluminescent corals draw rainbows through the water.",
    palette: { top: "oklch(0.62 0.08 200)", mid: "oklch(0.5 0.08 220)", deep: "oklch(0.38 0.08 245)" },
    upgradeCost: 50_000,
  },
  {
    depth: 3,
    name: "The Abyss",
    blurb: "Pure darkness pierced only by strange bioluminescence. Pressure immense.",
    palette: { top: "oklch(0.42 0.07 235)", mid: "oklch(0.3 0.07 250)", deep: "oklch(0.18 0.06 260)" },
    upgradeCost: 500_000,
  },
  {
    depth: 4,
    name: "Hydrothermal Vents",
    blurb: "Sulfur plumes and incandescent minerals. Igneous creatures thrive in the warmth.",
    palette: { top: "oklch(0.38 0.07 30)", mid: "oklch(0.28 0.09 25)", deep: "oklch(0.2 0.08 22)" },
    upgradeCost: 5_000_000,
  },
  {
    depth: 5,
    name: "Abyssal Plain",
    blurb: "An endless plateau in deepest dark. Giant jellyfish drift in silence.",
    palette: { top: "oklch(0.3 0.06 260)", mid: "oklch(0.22 0.06 270)", deep: "oklch(0.16 0.05 280)" },
    upgradeCost: 50_000_000,
  },
  {
    depth: 6,
    name: "Trench of Origins",
    blurb: "The deepest fissure ever mapped. Primal glyphs cover the walls.",
    palette: { top: "oklch(0.26 0.07 295)", mid: "oklch(0.18 0.07 290)", deep: "oklch(0.12 0.06 285)" },
    upgradeCost: 500_000_000,
  },
  {
    depth: 7,
    name: "Mana Nexus",
    blurb: "The beating heart of aquatic magic. Mana made visible to the eye.",
    palette: { top: "oklch(0.6 0.13 290)", mid: "oklch(0.42 0.14 295)", deep: "oklch(0.28 0.13 300)" },
    upgradeCost: 5_000_000_000,
  },
];

export const FISH: Fish[] = [
  { id: "gold",    name: "Goldfin",           desc: "Loyal companion of bright shallows.",      depth: 0, base: 1,          cost: 10 },
  { id: "carp",    name: "Sun Carp",           desc: "Slow, ceremonial, blessed by light.",      depth: 0, base: 4,          cost: 60 },
  { id: "frog",    name: "Crystal Frog",       desc: "Hops between lily pads in spring rain.",   depth: 0, base: 7,          cost: 45 },
  { id: "duck",    name: "Mana Duckling",      desc: "Skims the surface, gathering pollen.",     depth: 0, base: 12,         cost: 110 },
  { id: "ruby",    name: "Ruby Minnow",        desc: "Flashes scarlet in galleries of stone.",   depth: 1, base: 10,         cost: 150 },
  { id: "sapph",   name: "Sapphire Dragonfly", desc: "Wings hum at twilight intervals.",         depth: 1, base: 35,         cost: 800 },
  { id: "eel",     name: "Cobalt Eel",         desc: "Coils through underground currents.",      depth: 1, base: 60,         cost: 2_500 },
  { id: "nymph",   name: "Water Nymph",        desc: "Folkloric. Sings in submerged caverns.",   depth: 1, base: 100,        cost: 6_000 },
  { id: "diamond", name: "Diamond Puffer",     desc: "Refracts light into a hundred colours.",   depth: 2, base: 100,        cost: 5_000 },
  { id: "crab",    name: "Crystal Crab",       desc: "Architect of glassy reef cathedrals.",     depth: 2, base: 350,        cost: 25_000 },
  { id: "snail",   name: "Opal Snail",         desc: "Slow merchant of pearlescent spirals.",    depth: 2, base: 600,        cost: 80_000 },
  { id: "shrimp",  name: "Nacre Shrimp",       desc: "Shimmers in the warm reef shallows.",      depth: 2, base: 1_000,      cost: 200_000 },
  { id: "abyss",   name: "Abyssal Lantern",    desc: "Burns blue in the lightless deep.",        depth: 3, base: 1_000,      cost: 100_000 },
  { id: "octo",    name: "Shadow Octopus",     desc: "Folds itself into walls of dark water.",   depth: 3, base: 3_500,      cost: 500_000 },
  { id: "anemone", name: "Phantom Anemone",    desc: "Drifts on currents of pure mana.",         depth: 3, base: 6_000,      cost: 1_500_000 },
  { id: "spectre", name: "Spectre Fish",       desc: "Half-seen between two heartbeats.",        depth: 3, base: 10_000,     cost: 5_000_000 },
  { id: "salam",   name: "Igneous Salamander", desc: "Slips between vents and shadow.",          depth: 4, base: 10_000,     cost: 2_000_000 },
  { id: "fanguil", name: "Fire Eel",           desc: "Threads heat through cold trenches.",      depth: 4, base: 35_000,     cost: 10_000_000 },
  { id: "scorp",   name: "Magma Scorpion",     desc: "Carapace of cooled basalt.",               depth: 4, base: 60_000,     cost: 30_000_000 },
  { id: "lavas",   name: "Lava Serpent",       desc: "Coils around chimneys of sulfur.",         depth: 4, base: 100_000,    cost: 100_000_000 },
  { id: "jelly",   name: "Bioluminescent Jelly", desc: "Lanterns drifting on slow tides.",       depth: 5, base: 100_000,    cost: 100_000_000 },
  { id: "shark",   name: "Abyssal Shark",      desc: "Older than memory, patient as silt.",      depth: 5, base: 350_000,    cost: 500_000_000 },
  { id: "dolph",   name: "Spectre Dolphin",    desc: "Speaks in clicks no surface ear hears.",   depth: 5, base: 600_000,    cost: 2_000_000_000 },
  { id: "whale",   name: "Abyssal Whale",      desc: "Song carries through every trench.",       depth: 5, base: 1_000_000,  cost: 5_000_000_000 },
  { id: "dragon",  name: "Sea Dragon",         desc: "The first to swim. The last to forget.",   depth: 6, base: 1_000_000,  cost: 5_000_000_000 },
  { id: "leviat",  name: "Crystal Leviathan",  desc: "Body of pressure-grown glass.",            depth: 6, base: 3_500_000,  cost: 25_000_000_000 },
  { id: "egreg",   name: "Aquatic Egregore",   desc: "A thought given gills.",                   depth: 7, base: 10_000_000, cost: 100_000_000_000, requiredPrestiges: 2 },
  { id: "celest",  name: "Celestial Fish",     desc: "One alone. Generates gemmes through dream.", depth: 7, base: 50_000_000, cost: 1_000_000_000_000, requiredPrestiges: 1, maxOwned: 1 },
];

export const RESEARCH_BRANCHES: ResearchBranch[] = [
  {
    id: "bio", name: "Biology", hue: 145,
    desc: "Yield and milestone bonuses",
    nodes: [
      { id: "bio_1", label: "Symbiosis",    effect: "+15% global income",              cost: 5,   requires: null },
      { id: "bio_2", label: "Vitality",     effect: "+25% global income",              cost: 20,  requires: "bio_1" },
      { id: "bio_3", label: "Abundance",    effect: "+40% global income",              cost: 60,  requires: "bio_2" },
      { id: "bio_4", label: "Anticipation", effect: "Milestones arrive 1 level early", cost: 120, requires: "bio_3" },
      { id: "bio_5", label: "Symphony",     effect: "+75% global income",              cost: 250, requires: "bio_4" },
    ],
  },
  {
    id: "geo", name: "Geology", hue: 70,
    desc: "Cheaper to dig deeper",
    nodes: [
      { id: "geo_1", label: "Toolmaking",  effect: "−15% dig cost", cost: 8,   requires: null },
      { id: "geo_2", label: "Engineering", effect: "−30% dig cost", cost: 30,  requires: "geo_1" },
      { id: "geo_3", label: "Geomancy",    effect: "−40% dig cost", cost: 90,  requires: "geo_2" },
      { id: "geo_4", label: "Continental", effect: "−50% dig cost", cost: 220, requires: "geo_3" },
    ],
  },
  {
    id: "alch", name: "Alchemy", hue: 295,
    desc: "Boosts that last longer, cost less",
    nodes: [
      { id: "alch_1", label: "Brewing",       effect: "Boost +1 min",          cost: 10,  requires: null },
      { id: "alch_2", label: "Refinement",    effect: "Boost cost −2 gemmes",  cost: 40,  requires: "alch_1" },
      { id: "alch_3", label: "Distillery",    effect: "Boost +5 min",          cost: 100, requires: "alch_2" },
      { id: "alch_4", label: "Transmutation", effect: "Boost multiplier ×3",   cost: 240, requires: "alch_3" },
    ],
  },
  {
    id: "myst", name: "Mystic", hue: 195,
    desc: "Passive gemmes & richer rewards",
    nodes: [
      { id: "myst_1", label: "Insight",    effect: "+25% gemme rewards",   cost: 12,  requires: null },
      { id: "myst_2", label: "Channeling", effect: "+1 gemme / minute",    cost: 45,  requires: "myst_1" },
      { id: "myst_3", label: "Resonance",  effect: "+2 gemmes / minute",   cost: 110, requires: "myst_2" },
      { id: "myst_4", label: "Apotheosis", effect: "+50% gemme rewards",   cost: 260, requires: "myst_3" },
    ],
  },
  {
    id: "ocean", name: "Oceanology", hue: 175,
    desc: "Special bonuses for the deep",
    nodes: [
      { id: "ocean_1", label: "Cartography",   effect: "−20% fish cost",          cost: 15,  requires: null },
      { id: "ocean_2", label: "Pressure",      effect: "+20% global income",      cost: 50,  requires: "ocean_1" },
      { id: "ocean_3", label: "Depth Mastery", effect: "+50% to depth-4+ fish",   cost: 130, requires: "ocean_2" },
      { id: "ocean_4", label: "Tide-Reader",   effect: "+3 gemmes / minute",      cost: 280, requires: "ocean_3" },
    ],
  },
];

export const ACHIEVEMENTS: Achievement[] = [
  { id: "first",    name: "First Splash",    desc: "Buy your first fish.",          reward: 5,   check: (s) => s.totalFish >= 1 },
  { id: "ten",      name: "Bustling",        desc: "Own 10 fish.",                  reward: 10,  check: (s) => s.totalFish >= 10 },
  { id: "fifty",    name: "Teeming",         desc: "Own 50 fish.",                  reward: 25,  check: (s) => s.totalFish >= 50 },
  { id: "hundred",  name: "A Hundred Souls", desc: "Own 100 fish.",                 reward: 50,  check: (s) => s.totalFish >= 100 },
  { id: "deep1",    name: "First Descent",   desc: "Reach depth 1.",                reward: 10,  check: (s) => s.depth >= 1 },
  { id: "deep3",    name: "Into Shadow",     desc: "Reach the Abyss.",              reward: 35,  check: (s) => s.depth >= 3 },
  { id: "deep5",    name: "Plainsdrifter",   desc: "Reach the Abyssal Plain.",      reward: 80,  check: (s) => s.depth >= 5 },
  { id: "deep7",    name: "Nexus-Touched",   desc: "Reach the Mana Nexus.",         reward: 200, check: (s) => s.depth >= 7 },
  { id: "mana_k",   name: "Brimming",        desc: "Hold 10,000 mana at once.",     reward: 15,  check: (s) => s.mana >= 10_000 },
  { id: "mana_m",   name: "Reservoir",       desc: "Hold 1 million mana at once.",  reward: 40,  check: (s) => s.mana >= 1_000_000 },
  { id: "mana_b",   name: "Ocean of Mana",   desc: "Hold 1 billion mana at once.",  reward: 120, check: (s) => s.mana >= 1_000_000_000 },
  { id: "level10",  name: "Apprentice",      desc: "Bring a fish to level 10.",     reward: 15,  check: (s) => s.maxLevel >= 10 },
  { id: "level25",  name: "Adept",           desc: "Bring a fish to level 25.",     reward: 40,  check: (s) => s.maxLevel >= 25 },
  { id: "prestige1",name: "First Renewal",   desc: "Prestige once.",                reward: 50,  check: (s) => s.prestiges >= 1 },
  { id: "research1",name: "Spark",           desc: "Unlock one research node.",     reward: 15,  check: (s) => s.researchCount >= 1 },
  { id: "research5",name: "Sprout",          desc: "Unlock five research nodes.",   reward: 60,  check: (s) => s.researchCount >= 5 },
];

export const JOURNAL: JournalEntry[] = [
  { depth: 0, name: "Day 1 — The Hollow",
    text: "I bought a parcel of marshland on a whim. The water is brown and the reeds are taller than I am. The old map calls this place L'Étang des Merveilles. I will believe it when I see it." },
  { depth: 1, name: "The Stone Beneath",
    text: "There is a sound below the water that is not water. I dug a meter further and found a vein of cold red stone, smooth as if it had been polished by something patient." },
  { depth: 2, name: "Light That Lives",
    text: "The coral here is alive in the wrong way — it makes its own light. The puffers refract it into rainbows that hang in the water like washing on a line." },
  { depth: 3, name: "Where the Stars Are Wrong",
    text: "Past a certain depth, the dark has texture. The fish here carry their own lanterns. When I close my eyes I see constellations I do not recognize." },
  { depth: 4, name: "Salt and Sulfur",
    text: "Chimneys of mineral spit black smoke into water that should be cold but isn't. The salamanders coil around the heat the way cats sleep on hearths." },
  { depth: 5, name: "The Listening Plain",
    text: "Flat. Endless. The jellies pass overhead like clouds. I could hear my own pulse, but instead I heard a song. Distant. Patient. Approaching." },
  { depth: 6, name: "Glyphs Older Than Speech",
    text: "The walls are carved. The carvings are not in any human hand. Something here learned to write before it learned to sleep." },
  { depth: 7, name: "Mana Made Visible",
    text: "At the heart of the world, magic is no longer invisible. It pools and ripples in the water like ink in milk. The Egregore turns its many eyes toward me and I find I am not afraid." },
];

export const RUN_UPGRADES: RunUpgrade[] = [
  { id: "ru_yield_1",  name: "Honeyed Reeds",      desc: "+25% income from Surface fish.",       cost: 5_000,       icon: "❀" },
  { id: "ru_yield_2",  name: "Mineral Polish",     desc: "+25% income from River fish.",         cost: 250_000,     icon: "✦", requires: "ru_yield_1" },
  { id: "ru_yield_3",  name: "Lantern Oil",        desc: "+40% income from Abyss fish.",         cost: 50_000_000,  icon: "☉", requires: "ru_yield_2" },
  { id: "ru_dig_1",    name: "Sharpened Spade",    desc: "−15% dig cost for the next 3 digs.",   cost: 25_000,      icon: "⛏" },
  { id: "ru_dig_2",    name: "Engineer's Crew",    desc: "−25% dig cost (this run).",            cost: 2_500_000,   icon: "⛏", requires: "ru_dig_1" },
  { id: "ru_boost_1",  name: "Brewer's Apprentice",desc: "Boost lasts +2 min (this run).",       cost: 100_000,     icon: "⚗" },
  { id: "ru_speed_1",  name: "Quickened Current",  desc: "Fish breeding +20% — buy more for less.", cost: 1_500_000, icon: "≈" },
];

export const DAILY_CHALLENGES: DailyChallenge[] = [
  { id: "ch_earn_10k",  name: "Honest Coin",       desc: "Earn 10,000 mana today.",      progress: 0.6, reward: 1 },
  { id: "ch_buy_5",     name: "Five for the Pond", desc: "Buy 5 fish in a single day.",  progress: 0.4, reward: 1 },
  { id: "ch_dig_once",  name: "One Step Lower",    desc: "Dig deeper at least once.",    progress: 0.0, reward: 2 },
];

export const PEARL_UPGRADES: PearlUpgrade[] = [
  { id: "pm_offline_1", name: "Soft Tides",     desc: "+25% offline gains.",            cost: 2, icon: "☾" },
  { id: "pm_offline_2", name: "Patient Waters", desc: "+50% offline gains, cap +6 h.", cost: 6, icon: "☾", requires: "pm_offline_1" },
  { id: "pm_gemme_1",   name: "Insight",        desc: "+15% gemme rewards.",            cost: 3, icon: "◇" },
  { id: "pm_boost_1",   name: "Reservoir",      desc: "Boost cost −20% (min 1 gemme).", cost: 4, icon: "⚡" },
  { id: "pm_unique_1",  name: "Lily Garland",   desc: "Cosmetic — flower wreath on surface.", cost: 3, icon: "❀" },
  { id: "pm_unique_2",  name: "Lantern Stones", desc: "Cosmetic — glow stones in Abyss.", cost: 5, icon: "✦" },
];

export const PRESTIGE_UPGRADES: PrestigeUpgrade[] = [
  { id: "pu_mana_1",      name: "Saved Reservoir", desc: "Start each run with 5,000 mana.",   cost: 5,  icon: "◐" },
  { id: "pu_depth_1",     name: "Familiar Hollow", desc: "Start each run at depth 1.",        cost: 10, icon: "⌒", requires: "pu_mana_1" },
  { id: "pu_level_1",     name: "Practised Hands", desc: "Fish start at level 3.",            cost: 12, icon: "✦", requires: "pu_depth_1" },
  { id: "pu_keep_fish",   name: "Loyal Stock",     desc: "Keep 10% of fish through renewal.", cost: 18, icon: "♥" },
  { id: "pu_pearl_bonus", name: "Lustred Pearls",  desc: "+25% pearls from each renewal.",    cost: 8,  icon: "○" },
  { id: "pu_global_1",    name: "Old Memory",      desc: "Permanent +10% global income.",     cost: 25, icon: "𓆟", requires: "pu_pearl_bonus" },
];

export const NARRATIVE_EVENTS: NarrativeEvent[] = [
  { id: "ev_breeze", text: "A breeze passes through the reeds. The water remembers something brief.", minDepth: 0 },
  { id: "ev_lily",   text: "A lily turns by an inch, as if greeting someone.", minDepth: 0 },
  { id: "ev_stone",  text: "A single stone shifts in the gallery wall. The river accepts.", minDepth: 1 },
  { id: "ev_coral",  text: "The reef brightens twice, in two colours you do not have names for.", minDepth: 2 },
  { id: "ev_dark",   text: "The dark presses, gently, and then withdraws.", minDepth: 3 },
  { id: "ev_vent",   text: "A vent exhales. The water tastes of warm mineral for a moment.", minDepth: 4 },
  { id: "ev_song",   text: "Something sings, far below. The jellyfish all face the same way.", minDepth: 5 },
  { id: "ev_glyph",  text: "A glyph in the trench wall finishes a sentence it began an age ago.", minDepth: 6 },
  { id: "ev_nexus",  text: "Mana pools, ripples, and falls still. The pond is satisfied.", minDepth: 7 },
];

export const BESTIARY: Record<string, string> = {
  gold:    "Goldfin: smallest of the marvels. Hardly magical, but loyal. The first to follow a finger pointed at the surface.",
  carp:    "Sun Carp: slow ceremonial fish that holds the surface like a tea ceremony. Older keepers believe they bless what the sun touches.",
  frog:    "Crystal Frog: not strictly a fish. The reeds tolerate her on the rule that she carries spring rain across lilies.",
  duck:    "Mana Duckling: gathers pollen and stirs the surface enough to break monotony. Useful for visiting hawks.",
  ruby:    "Ruby Minnow: a thumb of scarlet that knows every gallery seam. They flash to escape and the river answers.",
  sapph:   "Sapphire Dragonfly: not a fish but loved by the river. Wings hum at exactly twilight intervals.",
  eel:     "Cobalt Eel: thread of cold in cold currents. Coils through stone like a question repeating itself.",
  nymph:   "Water Nymph: folkloric. Sings in submerged caverns. Whether she has gills is best left unprodded.",
  diamond: "Diamond Puffer: refracts light into a hundred colours when frightened, which is most days.",
  crab:    "Crystal Crab: builds cathedrals nobody attends. Reef geometry has improved demonstrably since.",
  snail:   "Opal Snail: very slow merchant of pearlescent spirals. Wears its inventory.",
  shrimp:  "Nacre Shrimp: shimmers in the warm reef shallows. Smaller than a thought and more numerous.",
  abyss:   "Abyssal Lantern: burns blue in the lightless deep. The first warm thing past a kilometer of dark.",
  octo:    "Shadow Octopus: folds itself into walls of dark water. You see eight eyes before you see one.",
  anemone: "Phantom Anemone: drifts on currents of pure mana. Touch it and remember someone you never met.",
  spectre: "Spectre Fish: half-seen between two heartbeats. Photographs never agree on its outline.",
  salam:   "Igneous Salamander: slips between vents and shadow. The vents seem to nod when she passes.",
  fanguil: "Fire Eel: threads heat through cold trenches. Leaves a scent of cooked salt that lingers.",
  scorp:   "Magma Scorpion: carapace of cooled basalt. Walks the seafloor as if it owns the lease.",
  lavas:   "Lava Serpent: coils around chimneys of sulfur, lazy and patient. Has not blinked in this age.",
  jelly:   "Bioluminescent Jelly: lantern drifting on slow tides. Their constellations rearrange while you watch.",
  shark:   "Abyssal Shark: older than memory, patient as silt. Will not eat you. Will not stop you.",
  dolph:   "Spectre Dolphin: speaks in clicks no surface ear hears. Surface ears miss most things.",
  whale:   "Abyssal Whale: song carries through every trench. The trenches remember when the song was new.",
  dragon:  "Sea Dragon: first to swim. Last to forget. Tail brushes the wall and a hundred glyphs realign.",
  leviat:  "Crystal Leviathan: body of pressure-grown glass. Refracts memory as well as light.",
  egreg:   "Aquatic Egregore: a thought given gills. Made of many fish thinking the same thought at once.",
  celest:  "Celestial Fish: one alone. Generates gemmes through dream. Do not wake her.",
};
