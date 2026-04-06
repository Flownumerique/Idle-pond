export interface LoreEntry {
  id: string;
  title: string;
  text: string;
  unlockedAtDepth?: number;
  unlockedByFishType?: string;  // Bestiaire : débloqué en possédant ce type de poisson
}

// ─── Journal de l'Étang (par profondeur) ──────────────────────────────────────
export const LORE_ENTRIES: LoreEntry[] = [
  {
    id: 'lore_0',
    title: 'L\'Étang des Merveilles',
    text: 'Les eaux scintillent d\'une lumière dorée. À peine creusé, cet étang semble déjà répondre à quelque chose d\'invisible. Les Poissons Or y paressent, absorbant la mana de la lumière du soleil. On dit que chaque écaille est un fragment de rêve pétrifié.',
    unlockedAtDepth: 0,
  },
  {
    id: 'lore_1',
    title: 'Eaux Souterraines',
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
    title: 'Chaleur de la Terre',
    text: 'Un grondement sourd monte du fond. La roche elle-même transpire. Des cheminées hydrothermales crachent des panaches de minéraux incandescents. C\'est ici que la vie et la lave se mêlent — les créatures ignées puisent leur énergie au cœur même de la planète.',
    unlockedAtDepth: 4,
  },
  {
    id: 'lore_5',
    title: 'La Plaine Sans Horizon',
    text: 'Au-delà des volcans, une plaine s\'étend à l\'infini dans l\'obscurité. Pas un rocher, pas un relief — seulement l\'eau et des méduses géantes qui dérivent comme des lanternes oubliées. Le silence ici n\'est pas vide : il est plein d\'une présence ancienne.',
    unlockedAtDepth: 5,
  },
  {
    id: 'lore_6',
    title: 'La Fosse des Premiers',
    text: 'Une fissure verticale descend plus loin que l\'œil ne peut voir. Les parois sont gravées de symboles antérieurs à toute écriture connue. Des chercheurs affirment que cette fosse est la cicatrice d\'un événement cosmique — le moment où la mana a pénétré le monde pour la première fois.',
    unlockedAtDepth: 6,
  },
  {
    id: 'lore_7',
    title: 'Le Nexus',
    text: 'Vous êtes au cœur de tout. La mana est ici visible à l\'œil nu, flottant en filaments dorés entre les roches. Des entités de lumière pure se meuvent avec intention — ni poissons, ni créatures, mais quelque chose entre les deux. Elles vous observent. Elles ont toujours su que vous viendriez.',
    unlockedAtDepth: 7,
  },
];

// ─── Bestiaire (par espèce, débloqué en possédant le poisson) ─────────────────
export const BESTIAIRE_ENTRIES: LoreEntry[] = [
  {
    id: 'bestiary_gold',
    title: 'Poisson Or — Aureus vulgaris',
    text: 'Première espèce à avoir absorbé la mana ambiante, le Poisson Or est le symbole de l\'étang. Ses écailles stockent l\'énergie solaire et la convertissent en mana pur. Docile, il suit son propriétaire du regard — certains disent qu\'il reconnaît les visages.',
    unlockedByFishType: 'gold',
  },
  {
    id: 'bestiary_carpe',
    title: 'Carpe Dorée — Cyprinus mana',
    text: 'Plus grande que le Poisson Or, la Carpe Dorée porte chance selon la tradition aquatique. Elle nage toujours dans le sens des courants magiques, agissant comme un baromètre de la mana environnante. Son souffle libère de fines bulles irisées.',
    unlockedByFishType: 'carpe',
  },
  {
    id: 'bestiary_ruby',
    title: 'Poisson Rubis — Rubinus profundus',
    text: 'Né dans les filons de minéraux magiques de la première couche souterraine, le Poisson Rubis transforme la chaleur géothermique en mana. Ses écailles sont dures comme de la pierre précieuse et reflètent la lumière en mille éclats rouge sang.',
    unlockedByFishType: 'ruby',
  },
  {
    id: 'bestiary_dragonfly',
    title: 'Libellule de Saphir — Anisoptera crystallis',
    text: 'Malgré son nom, cette créature vit entièrement sous l\'eau. Ses ailes translucides vibrent à une fréquence qui amplifie les courants de mana. Chaque battement d\'aile génère une micro-onde de mana qui se propage dans tout l\'étang.',
    unlockedByFishType: 'dragonfly',
  },
  {
    id: 'bestiary_diamond',
    title: 'Poisson Diamant — Crystallus maximus',
    text: 'La pression extrême du récif corallien a cristallisé la chair de ce poisson sur des millions d\'années. Son corps semi-transparent laisse voir la mana circuler à l\'intérieur comme du sang. Il ne nage pas — il glisse, comme si l\'eau elle-même s\'écartait.',
    unlockedByFishType: 'diamond',
  },
  {
    id: 'bestiary_crab',
    title: 'Crabe Cristal — Cancinus vitreus',
    text: 'Gardien naturel du récif, le Crabe Cristal construit sa carapace avec des fragments de corail magique. Ses pinces sont capables de tailler la roche, mais aussi de canaliser la mana ambiante en flux concentrés. Il vit en solitaire et n\'attaque jamais le premier.',
    unlockedByFishType: 'crab',
  },
  {
    id: 'bestiary_abyssal',
    title: 'Poisson Abyssal — Abyssus phantoma',
    text: 'Personne ne sait d\'où il vient réellement. Les théories les plus sérieuses suggèrent qu\'il traverse les dimensions en utilisant la mana comme pont. Ses yeux ne voient pas la lumière ordinaire — ils perçoivent directement les flux énergétiques.',
    unlockedByFishType: 'abyssal',
  },
  {
    id: 'bestiary_octopus',
    title: 'Pieuvre Ombre — Octopus umbra',
    text: 'La Pieuvre Ombre possède huit bras dont chacun peut produire de la mana de manière indépendante. Elle change de couleur non pas par camouflage, mais pour communiquer dans un langage de lumière que les humains commencent à peine à déchiffrer. Elle est considérée comme la plus intelligente de toutes les espèces de l\'étang.',
    unlockedByFishType: 'octopus',
  },
  {
    id: 'bestiary_salamander',
    title: 'Salamandre Ignée — Salamandra magmatus',
    text: 'Née dans les cheminées hydrothermales, la Salamandre Ignée est immunisée à la chaleur extrême. Sa peau sécréte un liquide qui transforme l\'eau en vapeur de mana. Elle pond ses œufs directement dans la lave — les œufs qui survivent donnent naissance aux spécimens les plus puissants.',
    unlockedByFishType: 'salamander',
  },
  {
    id: 'bestiary_eel',
    title: 'Anguille de Feu — Electrophorus infernus',
    text: 'L\'Anguille de Feu n\'est pas réellement une anguille. C\'est une colonie de micro-organismes magiques qui se comportent comme un seul être. Chaque décharge électrique est en fait une communication entre les milliers d\'individus qui la composent.',
    unlockedByFishType: 'eel',
  },
  {
    id: 'bestiary_jellyfish',
    title: 'Méduse Bioluminescente — Medusa luminaris',
    text: 'La Méduse Bioluminescente est le lampion de la plaine abyssale. Sa lumière n\'est pas chimique — elle est purement magique, émise directement depuis son nœud de mana central. Les marins perdus disent qu\'une méduse les a guidés jusqu\'à la surface. On ne les a jamais crus.',
    unlockedByFishType: 'jellyfish',
  },
  {
    id: 'bestiary_shark',
    title: 'Requin des Abysses — Carcharodon manavorans',
    text: 'Le Requin des Abysses chasse la mana comme un prédateur chasse sa proie. Il détecte les concentrations d\'énergie à des kilomètres de distance. Ce n\'est pas un poisson de l\'étang : c\'est un visiteur qui a décidé de rester. Personne ne sait pourquoi.',
    unlockedByFishType: 'shark',
  },
  {
    id: 'bestiary_dragon',
    title: 'Dragon des Mers — Draco abyssalis',
    text: 'Les légendes parlaient de lui depuis toujours. Aucun explorateur n\'était descendu assez profond pour le confirmer. Le Dragon des Mers n\'est pas agressif — il est simplement si ancien que votre présence lui est parfaitement indifférente. Sa respiration génère autant de mana qu\'une journée entière de production normale.',
    unlockedByFishType: 'dragon',
  },
  {
    id: 'bestiary_leviathan',
    title: 'Léviathan Cristallin — Leviathan crystallus',
    text: 'Le Léviathan est la créature la plus grande jamais observée dans les profondeurs. Son corps est partiellement cristallisé — chaque cristal est un nœud de mana accumulé sur des millions d\'années. Certains morceaux tombés de son flanc ont formé des gisements entiers de minerai magique.',
    unlockedByFishType: 'leviathan',
  },
  {
    id: 'bestiary_egregore',
    title: 'Égrégore Aquatique — Egregorus mentalis',
    text: 'L\'Égrégore n\'est pas né — il a été invoqué. Collectivement, par les pensées et les désirs des explorateurs qui ont osé descendre jusqu\'au Nexus. C\'est la manifestation de la volonté humaine dans un monde de mana pure. Il produit ce que vous croyez qu\'il peut produire.',
    unlockedByFishType: 'egregore',
  },
  {
    id: 'bestiary_celestial',
    title: 'Poisson Céleste — Piscis divinus',
    text: 'Le Poisson Céleste n\'appartient pas à cet étang. Il appartient à tous les étangs, à tous les océans, à tous les plans d\'existence simultanément. Il n\'y en a qu\'un seul au monde — et pourtant, chaque explorateur qui atteint le Nexus après un prestige le trouve là, qui attend, comme s\'il avait toujours su.',
    unlockedByFishType: 'celestial',
  },
];
