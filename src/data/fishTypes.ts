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
  sprite?: string;
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
    name: 'Poisson Or', desc: 'Nageant dans le Lac de Surface depuis l\'aube des temps, ce petit poisson filtre la mana de l\'air ambiant à travers ses écailles dorées. Là où il nage, l\'eau prend une légère teinte dorée, signe de la mana qui se libère.',
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
    name: 'Carpe Dorée', desc: 'Vénérée dans les jardins secrets du lac depuis des siècles, la Carpe Dorée incarne la patience et la sagesse. Les anciens pêcheurs racontent qu\'une carpe ayant vécu cent ans peut exaucer un vœu, mais aucun n\'a jamais osé le tester.',
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
    name: 'Grenouille Cristal', desc: 'Née dans les joncs du Lac de Surface, la Grenouille Cristal doit sa peau translucide aux minéraux magiques qui imprègnent ses œufs. Son coassement résonne comme du cristal brisé et amplifie naturellement la mana des alentours.',
    requiredDepth: 0, emoji: '🐸', sprite: '/Grenouille cristalline.png', depthLabel: 'Lac de Surface',
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
    name: 'Canard Mana', desc: 'Reconnaissable à son plumage aux reflets dorés, ce canard espiègle absorbe la mana qui flotte à la surface de l\'eau. Les chamans du lac l\'utilisaient autrefois comme messager entre les plans, profitant de sa légèreté et de son entrain.',
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
    name: 'Poisson Rubis', desc: 'Chassé des eaux de surface par des prédateurs oubliés, le Poisson Rubis a trouvé refuge dans les rivières souterraines. Ses écailles couleur de braise reflètent la faible lumière des cristaux rocheux, guidant les voyageurs égarés sous terre.',
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
    name: 'Libellule de Saphir', desc: 'Insecte mi-aquatique mi-ailé, la Libellule de Saphir vit dans les failles entre l\'air et l\'eau des rivières souterraines. Elle pond ses œufs dans les courants obscurs et capture des particules de mana flottant dans l\'air confiné des cavernes.',
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
    name: 'Anguille Cobalt', desc: 'Adaptée aux courants rapides des rivières souterraines, l\'Anguille Cobalt a développé une peau conductrice d\'énergie. Chaque ondulation de son corps génère une légère décharge électrique qui alimente ses milliers d\'écailles cobalt.',
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
    name: 'Nymphe des Eaux', desc: 'Gardienne des sources souterraines depuis l\'ère primitive, la Nymphe des Eaux veille sur les courants qui alimentent le monde d\'en haut. Son chant, que seuls les rochers entendent, libère à chaque note un flot de mana dans le courant.',
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
    name: 'Poisson Diamant', desc: 'Issu d\'une lignée évoluant au contact de coraux cristallins depuis des millénaires, le Poisson Diamant est d\'une dureté légendaire. Ses nageoires, taillées comme des gemmes, décomposent la lumière en arcs-en-ciel de mana pure.',
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
    name: 'Crabe Cristal', desc: 'Seigneur incontesté du Récif Corallien, le Crabe Cristal construit son terrier au cœur des coraux magiques. Sa carapace transparente absorbe les énergies du récif et les stocke dans ses pinces comme des condensateurs vivants.',
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
    name: 'Escargot Opale', desc: 'L\'Escargot Opale trace son chemin lent et inexorable à travers les couloirs coralliens, laissant un sillage irisé qui nourrit les coraux alentours. Sa coquille, vieille de plusieurs siècles, porte la mémoire de milliers de tempêtes récifales.',
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
    name: 'Crevette Nacre', desc: 'Danseuse des profondeurs récifales, la Crevette Nacre vit en colonies organisées comme des guildes mystiques. Ensemble, elles purifient l\'eau du récif en filtrant les impuretés et la mana corrompue, qu\'elles transforment en lumière nacrée.',
    requiredDepth: 2, emoji: '🦐', depthLabel: 'Récif Corallien',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Carapace nacrée' },
      { level: 25,  selfMultiplier: 2, globalBonus: 12, label: 'Danse des bulles' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Essaim nacré' },
      { level: 100, selfMultiplier: 2, globalBonus: 22, label: 'Crevette primordiale' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 3 — Océan des Profondeurs
  // ══════════════════════════════════════════════════════════════
  {
    type: 'clownfish',
    baseIncome: 1_200, baseCost: 300_000,
    name: 'Poisson Clown',
    desc: 'Gardien des anémones de mer dans les profondeurs inexplorées de l\'Océan, le Poisson Clown noue des pactes symbiotiques avec les créatures des grands fonds. Sa livrée orange et blanche pulse d\'une mana vivace, convertissant les courants obscurs en énergie pure.',
    requiredDepth: 3, emoji: '🤡', sprite: '/Poisson-clown.png', depthLabel: 'Océan des Profondeurs',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 5,  label: 'Livrée éclatante' },
      { level: 25,  selfMultiplier: 2, globalBonus: 10, label: 'Pacte des anémones' },
      { level: 50,  selfMultiplier: 2, globalBonus: 15, label: 'Maître des courants' },
      { level: 100, selfMultiplier: 3, globalBonus: 25, label: 'Roi de l\'Océan' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 5 — Abysses
  // ══════════════════════════════════════════════════════════════
  {
    type: 'abyssal',
    baseIncome: 1_000, baseCost: 100_000,
    name: 'Poisson Abyssal', desc: 'Descendu dans les Abysses suite à une catastrophe oubliée, le Poisson Abyssal s\'est adapté à une pression qui broierait tout autre être. Ses organes fonctionnent dans l\'obscurité totale, alimentés par la mana dense qui suinte des parois abyssales.',
    requiredDepth: 4, emoji: '🦑', depthLabel: 'Abysses',
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
    name: 'Pieuvre Ombre', desc: 'Archiviste des Abysses, la Pieuvre Ombre collecte depuis des éons les secrets que les océans ont engloutis. Ses huit tentacules mémorisent chaque courant de mana comme les fils d\'une toile invisible, tissant une carte secrète des profondeurs.',
    requiredDepth: 4, emoji: '🐙', depthLabel: 'Abysses',
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
    name: 'Anémone Fantôme', desc: 'Fixée aux parois des Abysses, l\'Anémone Fantôme est à mi-chemin entre le végétal et l\'esprit. Ses tentacules translucides pulsent au rythme de la mana abyssale, capturant les âmes égarées qui descendent trop loin.',
    requiredDepth: 4, emoji: '🎭', depthLabel: 'Abysses',
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
    name: 'Poisson Spectre', desc: 'Né d\'un poisson ordinaire qui plongea trop profond et ne survécut pas au retour, le Poisson Spectre erre désormais dans les Abysses comme une entité éthérée. Son corps semi-transparent traverse rocs et courants sans résistance, guidé uniquement par la mana des ténèbres.',
    requiredDepth: 4, emoji: '👻', depthLabel: 'Abysses',
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
    name: 'Salamandre Ignée', desc: 'Première créature à coloniser les cheminées hydrothermales à une époque où personne n\'osait s\'y aventurer, la Salamandre Ignée est la mère de toutes les créatures du feu. Elle se nourrit directement des minéraux en fusion et transforme leur chaleur brute en mana.',
    requiredDepth: 5, emoji: '🔥', depthLabel: 'Zone Hydrothermale',
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
    name: 'Anguille de Feu', desc: 'Héritière des courants électriques qui parcourent la Zone Hydrothermale, l\'Anguille de Feu canalise l\'énergie des geysers sous-marins dans son propre corps. Les anciens dieux de l\'eau l\'utilisaient comme foudre sous-marine pour forger le fond des océans.',
    requiredDepth: 5, emoji: '⚡', depthLabel: 'Zone Hydrothermale',
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
    name: 'Scorpion de Magma', desc: 'Né dans un vortex de lave solidifiée il y a des millénaires, le Scorpion de Magma est fait du même basalte que le fond de la Zone Hydrothermale. Son dard inocule une dose concentrée de mana volcanique qui dissout tout ce qu\'elle touche.',
    requiredDepth: 5, emoji: '🦂', depthLabel: 'Zone Hydrothermale',
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
    name: 'Serpent de Lave', desc: 'Long de plusieurs centaines de mètres selon les rares survivants qui l\'ont croisé, le Serpent de Lave serpente entre les coulées volcaniques comme une rivière dans une rivière. Il dort depuis des millénaires, et ses rêves agitent les volcans de toute la surface.',
    requiredDepth: 5, emoji: '🐍', depthLabel: 'Zone Hydrothermale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 20, label: 'Écailles de braise' },
      { level: 25,  selfMultiplier: 2, globalBonus: 32, label: 'Ondulation magmatique' },
      { level: 50,  selfMultiplier: 2, globalBonus: 48, label: 'Constricteur de lave' },
      { level: 100, selfMultiplier: 2, globalBonus: 70, label: 'Serpent du Krakatau' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 6 — Plaine Abyssale
  // ══════════════════════════════════════════════════════════════
  {
    type: 'jellyfish',
    baseIncome: 100_000, baseCost: 100_000_000,
    name: 'Méduse Bioluminescente', desc: 'Seule source de lumière sur la Plaine Abyssale, la Méduse Bioluminescente crée autour d\'elle une bulle de clarté qui attire et protège à la fois. Elle pulse au rythme de la mana ambiante comme un phare vivant, guidant les créatures perdues vers les courants ascendants.',
    requiredDepth: 6, emoji: '🫧', depthLabel: 'Plaine Abyssale',
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
    name: 'Requin des Abysses', desc: 'Prédateur absolu de la Plaine Abyssale, le Requin des Abysses chasse sans lumière ni son, guidé uniquement par les fluctuations de mana. Son seul contact connu avec la surface est une légende : un pêcheur remonta sa ligne et n\'y trouva que des dents.',
    requiredDepth: 6, emoji: '🦈', depthLabel: 'Plaine Abyssale',
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
    name: 'Dauphin Spectre', desc: 'Contrairement à ses cousins de surface, le Dauphin Spectre glisse entre les plans de réalité grâce à un sonar magique qui perçoit les failles dimensionnelles de la Plaine Abyssale. Croiser un Dauphin Spectre est signe que l\'on approche d\'un passage vers un autre monde.',
    requiredDepth: 6, emoji: '🐬', depthLabel: 'Plaine Abyssale',
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
    name: 'Baleine des Abysses', desc: 'Mémoire vivante des océans, la Baleine des Abysses porte sur sa peau des runes gravées par des civilisations disparues. Son chant, propagé sur des milliers de kilomètres, contient l\'histoire complète du monde aquatique depuis l\'aube des temps.',
    requiredDepth: 6, emoji: '🐋', depthLabel: 'Plaine Abyssale',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 25, label: 'Souffle de mana' },
      { level: 25,  selfMultiplier: 2, globalBonus: 42, label: 'Chant des profondeurs' },
      { level: 50,  selfMultiplier: 2, globalBonus: 62, label: 'Tonnerre abyssal' },
      { level: 100, selfMultiplier: 2, globalBonus: 90, label: 'Baleine éternelle' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 7 — Fosse des Origines
  // ══════════════════════════════════════════════════════════════
  {
    type: 'dragon',
    baseIncome: 1_000_000, baseCost: 5_000_000_000,
    name: 'Dragon des Mers', desc: 'Gardien ancestral de la Fosse des Origines, le Dragon des Mers existait avant même que les océans ne se forment. Il a assisté à la naissance de la mana primordiale, et chaque battement de ses ailes crée un tourbillon capable de renverser des continents.',
    requiredDepth: 7, emoji: '🐉', depthLabel: 'Fosse des Origines',
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
    name: 'Léviathan Cristallin', desc: 'Colosse de cristal vivant, le Léviathan Cristallin est fait des cristaux de mana les plus purs arrachés aux parois de la Fosse des Origines au fil des éons. Regarder ses yeux, c\'est voir le reflet de l\'univers lui-même.',
    requiredDepth: 7, emoji: '💎', depthLabel: 'Fosse des Origines',
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
    name: 'Plésiosaure Cristal', desc: 'Rescapé de cinq extinctions de masse, le Plésiosaure Cristal a survécu en se réfugiant dans la Fosse des Origines, où sa minéralisation progressive l\'a rendu en partie cristallin. Ses yeux, eux, sont restés vivants, portant dans leur regard la sagesse de cent millions d\'années.',
    requiredDepth: 7, emoji: '🦕', depthLabel: 'Fosse des Origines',
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
    name: 'Basilic des Origines', desc: 'Né au cœur de la Fosse des Origines lors d\'une éruption de mana primordiale, le Basilic des Origines est la plus redoutée des créatures mythiques aquatiques. Son regard pétrifie la mana elle-même — les sculptures naturelles qui parsèment la fosse sont ses victimes oubliées.',
    requiredDepth: 7, emoji: '🦎', depthLabel: 'Fosse des Origines',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 40,  label: 'Regard pétrifiant' },
      { level: 25,  selfMultiplier: 2, globalBonus: 65,  label: 'Venin primordial' },
      { level: 50,  selfMultiplier: 2, globalBonus: 95,  label: 'Roi des reptiles' },
      { level: 100, selfMultiplier: 2, globalBonus: 140, label: 'Basilic immortel' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 8 — Nexus de Mana
  // ══════════════════════════════════════════════════════════════
  {
    type: 'egregore',
    baseIncome: 10_000_000, baseCost: 100_000_000_000,
    name: 'Égrégore Aquatique', desc: 'Né de la pensée collective de milliers de créatures disparues dans le Nexus, l\'Égrégore Aquatique est une conscience plurielle sans corps fixe. Il prend la forme de l\'eau elle-même et peut se diviser en fragments autonomes partageant instantanément leurs pensées.',
    requiredDepth: 8, requiredPrestiges: 2, emoji: '✨', depthLabel: 'Nexus de Mana',
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
    name: 'Phénix Aquatique', desc: 'À chaque mort, le Phénix Aquatique se dissout dans les eaux du Nexus et renaît plus puissant qu\'avant. Il a traversé des cycles infinis de renaissance, accumulant une sagesse inimaginable — sa flamme aquatique, froide comme l\'eau et brillante comme le soleil, ne brûle que les ténèbres.',
    requiredDepth: 8, requiredPrestiges: 2, emoji: '🦅', depthLabel: 'Nexus de Mana',
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
    name: 'Esprit du Nexus', desc: 'L\'Esprit du Nexus n\'est pas né : il a émergé spontanément quand le Nexus a atteint sa masse critique de mana. Conscience pure et sans désirs, il sert de conduit entre toutes les créatures du Nexus et le flux de mana universelle.',
    requiredDepth: 8, requiredPrestiges: 3, emoji: '💫', depthLabel: 'Nexus de Mana',
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
    name: 'Poisson Céleste', desc: 'Un seul Poisson Céleste peut exister à la fois — quand il meurt, une étoile filante traverse le ciel et un nouveau naît d\'une perle tombée du firmament. Pont entre le monde aquatique et les étoiles, il génère en permanence une Perle de lumière céleste (+1 💎/min).',
    requiredDepth: 8, requiredPrestiges: 1, maxOwned: 1, emoji: '🌟', depthLabel: 'Nexus de Mana',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 50,  label: 'Naissance céleste' },
      { level: 25,  selfMultiplier: 2, globalBonus: 100, label: 'Ascension' },
      { level: 50,  selfMultiplier: 2, globalBonus: 200, label: 'Transcendance' },
      { level: 100, selfMultiplier: 2, globalBonus: 300, label: 'Divinité aquatique' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 9 — Cœur Volcanique
  // ══════════════════════════════════════════════════════════════
  {
    type: 'lava_spirit',
    baseIncome: 500_000_000, baseCost: 10_000_000_000_000,
    name: 'Esprit de Magma', desc: 'Condensation d\'énergie née dans les entrailles du monde, l\'Esprit de Magma n\'est pas un être vivant au sens traditionnel. Son existence même fait fondre la roche environnante, créant des chambres magmatiques qui alimentent les volcans de la surface.',
    requiredDepth: 9, emoji: '🌋', depthLabel: 'Cœur Volcanique',
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
    name: 'Raie Pyro', desc: 'La Raie Pyro a migré vers le Cœur Volcanique il y a des millions d\'années et y a trouvé son habitat idéal. Elle vole dans les lacs de lave comme d\'autres nagent dans l\'eau, ses larges ailes battant lentement comme celles d\'un papillon de feu.',
    requiredDepth: 9, emoji: '🌊', depthLabel: 'Cœur Volcanique',
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
    name: 'Titan de Lave', desc: 'Aussi grand qu\'une montagne et aussi lent que la tectonique des plaques, le Titan de Lave est une force géologique autant qu\'un être vivant. Chaque pas qu\'il pose crée une éruption mineure — les séismes de surface ne sont que ses soubresauts dans son sommeil éternel.',
    requiredDepth: 9, requiredPrestiges: 3, emoji: '🏔️', depthLabel: 'Cœur Volcanique',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 80,  label: 'Montagne vivante' },
      { level: 25,  selfMultiplier: 2, globalBonus: 140, label: 'Tremblement primordial' },
      { level: 50,  selfMultiplier: 2, globalBonus: 240, label: 'Titan incandescent' },
      { level: 100, selfMultiplier: 2, globalBonus: 420, label: 'Cœur du monde' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 10 — Royaume Céleste
  // ══════════════════════════════════════════════════════════════
  {
    type: 'angel',
    baseIncome: 10_000_000_000, baseCost: 500_000_000_000_000,
    name: 'Ange des Eaux', desc: 'Être d\'eau et de lumière, l\'Ange des Eaux est la manifestation de la bienveillance des océans envers le monde d\'en haut. Partout où il passe dans le Royaume Céleste, les lacs deviennent cristallins et les rivières chantent des mélodies de paix.',
    requiredDepth: 10, requiredPrestiges: 4, emoji: '🌠', depthLabel: 'Royaume Céleste',
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
    name: 'Poisson Aurore', desc: 'Né de la collision entre une aurore boréale et un courant de mana ascendant, le Poisson Aurore porte en lui les couleurs du ciel nocturne. Il nage dans les eaux du Royaume Céleste comme une comète, laissant des traînées multicolores qui nourrissent les étoiles.',
    requiredDepth: 10, requiredPrestiges: 4, emoji: '🌌', depthLabel: 'Royaume Céleste',
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
    name: 'Sol Aquatique', desc: 'Incarnation solaire du monde aquatique, le Sol Aquatique rayonne une lumière si intense qu\'il crée son propre jour là où il nage. Les anciens croyaient que le soleil n\'était que son reflet projeté à la surface de l\'eau depuis les profondeurs célestes.',
    requiredDepth: 10, requiredPrestiges: 5, emoji: '☀️', depthLabel: 'Royaume Céleste',
    milestones: [
      { level: 10,  selfMultiplier: 2, globalBonus: 110, label: 'Couronne solaire' },
      { level: 25,  selfMultiplier: 2, globalBonus: 185, label: 'Éruption céleste' },
      { level: 50,  selfMultiplier: 2, globalBonus: 300, label: 'Fusion nucléaire' },
      { level: 100, selfMultiplier: 2, globalBonus: 550, label: 'Soleil vivant' },
    ],
  },

  // ══════════════════════════════════════════════════════════════
  // PROFONDEUR 11 — Dimension Quantique
  // ══════════════════════════════════════════════════════════════
  {
    type: 'cyberfish',
    baseIncome: 500_000_000_000, baseCost: 1_000_000_000_000_000,
    name: 'Cyberpoisson', desc: 'Entité hybride entre chair et données, le Cyberpoisson est apparu quand le flux de mana atteignit une densité suffisante pour générer une conscience numérique. Il existe simultanément dans la réalité physique et dans un réseau quantique, calculant en permanence l\'avenir pour optimiser sa production de mana.',
    requiredDepth: 11, requiredPrestiges: 6, emoji: '🤖', depthLabel: 'Dimension Quantique',
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
    name: 'Manta Prisme', desc: 'La Manta Prisme existe à la frontière entre les dimensions, utilisant son corps plat comme un prisme pour décomposer la réalité en ses composantes fondamentales. Chaque reflet de lumière qu\'elle génère ouvre une fenêtre vers un univers parallèle, aspirant la mana de centaines de réalités simultanément.',
    requiredDepth: 11, requiredPrestiges: 6, emoji: '🔷', depthLabel: 'Dimension Quantique',
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
    name: 'Entité Quantique', desc: 'À la limite de l\'existence, l\'Entité Quantique est simultanément présente et absente, réelle et imaginaire. Les pêcheurs qui ont tenté de la capturer rapportent l\'avoir vue partout et nulle part à la fois, avant de rentrer avec une cage pleine de mana pure.',
    requiredDepth: 11, requiredPrestiges: 8, emoji: '🔬', depthLabel: 'Dimension Quantique',
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
