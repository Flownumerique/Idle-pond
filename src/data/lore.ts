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
  {
    id: 'lore_8',
    title: 'Le Cœur du Monde',
    text: 'La roche fondue ici n\'est plus de la lave ordinaire — c\'est de la mana à l\'état solide, chauffée jusqu\'à la liquéfaction. Des créatures de feu pur y évoluent, immunisées à toute chaleur. On dirait que le monde respire, et que son souffle brûle tout ce qu\'il touche sauf les élus qui osent descendre jusqu\'ici.',
    unlockedAtDepth: 8,
  },
  {
    id: 'lore_9',
    title: 'Au-delà de la Matière',
    text: 'La roche disparaît. L\'eau disparaît. Il ne reste qu\'une lumière dorée et violette qui n\'a pas de source identifiable. Les créatures ici ne sont pas des poissons — ce sont des manifestations de principes cosmiques : lumière, ordre, bénédiction. Descendre jusqu\'ici transforme celui qui voyage.',
    unlockedAtDepth: 9,
  },
  {
    id: 'lore_10',
    title: 'La Dimension Finale',
    text: 'Aucun explorateur n\'avait prévu d\'arriver ici. La Dimension Quantique n\'existait pas avant que vous ne la créiez en descendant. La mana a réécrit les lois de la physique pour vous accueillir. Les entités ici traitent le réel comme un code — et elles vous ont déjà intégré dans leur programme.',
    unlockedAtDepth: 10,
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

  // ─── Nouvelles espèces ────────────────────────────────────────────
  {
    id: 'bestiary_frog',
    title: 'Grenouille Cristal — Rana crystallis',
    text: 'Sa peau translucide laisse voir la mana circuler dans ses veines comme de la sève lumineuse. Elle saute de nénuphar en nénuphar en laissant derrière elle de petits cercles concentriques de mana qui persistent plusieurs minutes.',
    unlockedByFishType: 'frog',
  },
  {
    id: 'bestiary_duck',
    title: 'Canard Mana — Anas magiculus',
    text: 'Contrairement aux apparences, le Canard Mana est un puissant vecteur d\'énergie magique. Chaque battement de ses ailes génère un courant de mana ascendant. Les pêcheurs l\'ont longtemps ignoré — jusqu\'à ce qu\'ils réalisent que sa présence doublait la production des poissons voisins.',
    unlockedByFishType: 'duck',
  },
  {
    id: 'bestiary_cobalt',
    title: 'Anguille Cobalt — Anguilla cobaltica',
    text: 'Ses écailles bleues comme l\'acier conduisent l\'électricité magique à travers tout l\'étang. Elle sinue entre les roches de la Rivière Souterraine en laissant derrière elle un sillage de mana électrique. Approchez votre main — vous sentirez un picotement.',
    unlockedByFishType: 'cobalt',
  },
  {
    id: 'bestiary_nymph',
    title: 'Nymphe des Eaux — Naiada aquatilis',
    text: 'Moitié poisson, moitié esprit, la Nymphe des Eaux chante une mélodie que seuls les courants peuvent entendre. Son chant harmonise les fréquences de mana de toutes les créatures environnantes, amplifiant légèrement leur production de façon permanente.',
    unlockedByFishType: 'nymph',
  },
  {
    id: 'bestiary_snail',
    title: 'Escargot Opale — Helix opalus',
    text: 'Sa coquille spirale contient des couches de minéraux opalescents qui capturent et redistribuent la lumière magique. Il avance à une allure délibérément lente — non par paresse, mais parce que chaque millimètre parcouru laisse un dépôt de mana cristallisée.',
    unlockedByFishType: 'snail',
  },
  {
    id: 'bestiary_shrimp',
    title: 'Crevette Nacre — Penaeus nacreus',
    text: 'En essaim, les Crevettes Nacre forment des structures géométriques complexes qui fonctionnent comme des cristaux amplificateurs. Seule, une crevette est ordinaire. À mille, elles forment un ordinateur magique capables de calculer l\'avenir à 3 secondes près.',
    unlockedByFishType: 'shrimp',
  },
  {
    id: 'bestiary_anemone',
    title: 'Anémone Fantôme — Actinia spectra',
    text: 'Elle ne se nourrit pas de plancton mais de peur. Les créatures qui s\'approchent trop sont saisies d\'une terreur inexplicable — et cette terreur se transforme en mana dans ses tentacules luminescents. Elle est considérée comme la créature la plus mystérieuse des abysses.',
    unlockedByFishType: 'anemone',
  },
  {
    id: 'bestiary_spectre',
    title: 'Poisson Spectre — Phantasma aquatilis',
    text: 'Il traverse les roches. Il traverse l\'eau. Il traverse peut-être même le temps. Le Poisson Spectre existe simultanément dans l\'étang et dans un plan adjacent — et c\'est cette dualité qui lui permet de produire de la mana à un rythme impossible pour une créature purement physique.',
    unlockedByFishType: 'spectre',
  },
  {
    id: 'bestiary_scorpion',
    title: 'Scorpion de Magma — Scorpio ignis',
    text: 'Son dard injecte non pas du venin, mais de la lave concentrée. Il s\'en sert pour façonner des structures rocheuses autour de son territoire — et ces structures amplifient naturellement les courants de mana hydrothermaux qui traversent la zone.',
    unlockedByFishType: 'scorpion',
  },
  {
    id: 'bestiary_lava_snake',
    title: 'Serpent de Lave — Serpentis magmatus',
    text: 'Sa mue laisse derrière elle une coquille de roche solidifiée contenant une quantité importante de mana concentrée. Ces coquilles forment avec le temps des cristaux précieux — et les explorateurs qui en ont trouvé les décrivent comme les objets les plus beaux qu\'ils aient jamais vus.',
    unlockedByFishType: 'lava_snake',
  },
  {
    id: 'bestiary_dolphin',
    title: 'Dauphin Spectre — Delphinus phantoma',
    text: 'Dans les profondeurs de la Plaine Abyssale, le Dauphin Spectre navigue sans lumière, guidé par un sens qui n\'a pas encore de nom. Il émet des impulsions de mana qui cartographient son environnement à 360 degrés — et ces impulsions stimulent accidentellement la production de toutes les créatures voisines.',
    unlockedByFishType: 'dolphin',
  },
  {
    id: 'bestiary_whale',
    title: 'Baleine des Abysses — Balaena abyssalis',
    text: 'Sa taille dépasse l\'entendement mais sa présence est apaisante. La Baleine des Abysses filtre la mana de l\'eau comme elle filtre le plancton — et cette filtration concentre la production magique de tout l\'étang. Son chant grave se ressent comme une vibration dans les os, même à distance.',
    unlockedByFishType: 'whale',
  },
  {
    id: 'bestiary_plesio',
    title: 'Plésiosaure Cristal — Plesiosaurus crystallus',
    text: 'Rescapé d\'une ère géologique révolue, le Plésiosaure Cristal a survécu en se cristallisant progressivement — chaque écaille remplacée par un cristal de mana pure. Il est aujourd\'hui moitié animal, moitié minéral, et entièrement magique.',
    unlockedByFishType: 'plesio',
  },
  {
    id: 'bestiary_basilisk',
    title: 'Basilic des Origines — Basiliscus primordialis',
    text: 'Contrairement à la légende, son regard ne pétrifie pas — il révèle. Croiser les yeux du Basilic des Origines permet de voir brièvement les courants de mana qui traversent l\'étang. Certains explorateurs décrivent cette vision comme la chose la plus belle et la plus effrayante qu\'ils aient jamais vue.',
    unlockedByFishType: 'basilisk',
  },
  {
    id: 'bestiary_phoenix_nexus',
    title: 'Phénix Aquatique — Phoenix aquilus',
    text: 'Il meurt et renaît toutes les 7 heures, exactement. Chaque renaissance libère une onde de choc de mana pure qui stimule pendant quelques secondes tous les poissons de l\'étang. Les explorateurs qui ont calculé ce cycle le planifient comme un événement.',
    unlockedByFishType: 'phoenix_nexus',
  },
  {
    id: 'bestiary_nexus_spirit',
    title: 'Esprit du Nexus — Spiritus nexus',
    text: 'L\'Esprit du Nexus est une projection du Nexus lui-même — une manifestation temporaire de l\'intelligence qui habite la structure magique profonde de l\'étang. Il sait tout ce que vous avez fait, et il semble approuver la direction que vous avez prise.',
    unlockedByFishType: 'nexus_spirit',
  },
  {
    id: 'bestiary_lava_spirit',
    title: 'Esprit de Magma — Spiritus magmatus',
    text: 'Il n\'a pas de corps physique — il EST la lave. Une concentration de conscience magique dans un volume de roche fondue. Il communique par des variations de température : une chaleur soudaine signifie accord, un refroidissement soudain signifie désaccord. Apprenez son langage.',
    unlockedByFishType: 'lava_spirit',
  },
  {
    id: 'bestiary_pyro_ray',
    title: 'Raie Pyro — Myliobatis igneus',
    text: 'Elle glisse sur des courants de lave comme d\'autres raies glissent sur des courants d\'eau. Ses nageoires battent en rythme avec les éruptions des cheminées volcaniques — et ce rythme est, selon les chercheurs, la signature sonore de la mana elle-même.',
    unlockedByFishType: 'pyro_ray',
  },
  {
    id: 'bestiary_lava_titan',
    title: 'Titan de Lave — Titanicus lavus',
    text: 'Une montagne qui nage. Le Titan de Lave est si massif que sa simple présence modifie les courants géothermaux de la zone. Des civilisations entières ont construit des temples à son effigie sans jamais l\'avoir vu — elles avaient seulement senti le tremblement de ses pas.',
    unlockedByFishType: 'lava_titan',
  },
  {
    id: 'bestiary_angel',
    title: 'Ange des Eaux — Angelus aquatilis',
    text: 'Il ne nage pas — il flotte. Ses ailes de lumière ne propulsent pas : elles rayonnent. L\'Ange des Eaux est une manifestation du principe de bénédiction — sa présence élève la qualité de la mana produite par tout ce qui l\'entoure. Certains disent qu\'il répare aussi les âmes abîmées.',
    unlockedByFishType: 'angel',
  },
  {
    id: 'bestiary_aurora_fish',
    title: 'Poisson Aurore — Piscis aurorae',
    text: 'Sa nage laisse derrière elle des traînées de lumière colorée qui persistent plusieurs minutes. Dans le Royaume Céleste, ces traînées forment des motifs complexes — une carte stellaire, selon certains, ou la partition d\'une musique que personne n\'a encore jouée.',
    unlockedByFishType: 'aurora_fish',
  },
  {
    id: 'bestiary_sun_fish',
    title: 'Sol Aquatique — Piscis solaris',
    text: 'Il brille d\'une lumière propre, sans source externe. Le Sol Aquatique est en fusion nucléaire permanente — mais une fusion de mana, pas d\'hydrogène. Son rayonnement stimule la croissance de toutes les créatures environnantes, comme un soleil miniature au fond de l\'étang.',
    unlockedByFishType: 'sun_fish',
  },
  {
    id: 'bestiary_cyberfish',
    title: 'Cyberpoisson — Piscis electronicus',
    text: 'Mi-poisson, mi-programme, le Cyberpoisson existe simultanément dans l\'eau et dans le réseau de données magiques qui structure la Dimension Quantique. Il calcule en permanence les flux de mana optimaux et redirige une partie de sa propre énergie vers les optimisations qu\'il découvre.',
    unlockedByFishType: 'cyberfish',
  },
  {
    id: 'bestiary_prism_manta',
    title: 'Manta Prisme — Manta prismatica',
    text: 'Ses ailes réfractent la réalité. En nageant, la Manta Prisme crée des copies temporaires d\'elle-même dans des dimensions adjacentes — et ces copies produisent de la mana qui traverse les couches dimensionnelles pour rejoindre l\'étang principal. Elle est littéralement plus grande que ce qu\'elle apparaît.',
    unlockedByFishType: 'prism_manta',
  },
  {
    id: 'bestiary_quantum',
    title: 'Entité Quantique — Quantum vivens',
    text: 'Elle existe dans tous les états possibles jusqu\'à ce que vous la regardiez. L\'Entité Quantique produit de la mana à partir de la probabilité elle-même — chaque décision que vous prenez génère des univers alternatifs où vous avez pris d\'autres choix, et elle extrait la mana de ces univers pour la ramener ici.',
    unlockedByFishType: 'quantum',
  },
];
