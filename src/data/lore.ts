export interface LoreEntry {
  id: string;
  title: string;
  text: string;
  unlockedAtDepth: number;
}

export const LORE_ENTRIES: LoreEntry[] = [
  {
    id: 'lore_0',
    title: 'L\'Étang des Merveilles',
    text: 'Les eaux scintillent d\'une lumière dorée. À peine creusé, cet étang semble déjà répondre à quelque chose d\'invisible. Les Poissons Or y paressent, absorbant la mana de la lumière du soleil. On dit que chaque écaille est un fragment de rêve pétrifié.',
    unlockedAtDepth: 0,
  },
  {
    id: 'lore_1',
    title: 'Eaux intermédiaires',
    text: 'Plus profond, l\'eau vire au rouge rosé. Les parois révèlent des filons de minéraux inconnus. Les Poissons Rubis émergent de ces strates comme s\'ils y avaient toujours été endormis, attendant que quelqu\'un creuse assez loin pour les libérer.',
    unlockedAtDepth: 1,
  },
  {
    id: 'lore_2',
    title: 'Cristaux et Silence',
    text: 'La lumière ne pénètre plus qu\'en filets timides. Des cristaux naturels tapissent les parois, vibrant d\'une fréquence imperceptible. Les Poissons Diamant sont nés ici — forgés par la pression et la magie ancienne, ils transforment l\'obscurité en éclat pur.',
    unlockedAtDepth: 2,
  },
  {
    id: 'lore_3',
    title: 'L\'Appel des Abysses',
    text: 'Les ténèbres vous enveloppent. Des bioluminescences étranges éclairent ces profondeurs insondables. Les Poissons Abyssaux semblent venir d\'un autre plan d\'existence — leurs yeux reflètent des étoiles que personne n\'a encore cartographiées.',
    unlockedAtDepth: 3,
  },
  {
    id: 'lore_4',
    title: 'Le Fond de Toutes Choses',
    text: 'Vous avez atteint le fond. Ici, le temps s\'écoule différemment. La magie ancienne est à son apogée. Des gravures sur la roche racontent une légende : une créature céleste dormirait dans les profondeurs, attendant d\'être trouvée par quelqu\'un qui aurait osé tout recommencer.',
    unlockedAtDepth: 4,
  },
];
