# IdlePond — le cahier des gabarits

Ce que doit respecter un dessin pour remplacer un provisoire **sans toucher au code**.
Source de vérité : `Assets/IdlePond/Jeu/Scene/Gabarits.cs` et `RegistreDArt.cs` ; les tests
`GabaritsTests` vérifient chaque fichier livré.

> **La scène est à 360 px de large depuis le 2026-11-24** (`Gabarits.LARGEUR_VISEE`, 240 avant ;
> Codex v1.4, §2 ; maquettes `apercu-poissons-v3.png` et `apercu-finesse-scene.png`). Toutes les
> mesures du dessin ont suivi d'un même facteur 1,5 : bande de 84 px, décor, héros. À l'écran, la
> mise en page ne bouge pas ; il y a seulement une fois et demie plus de pixels pour dessiner. Repli
> sur 320 px si un vrai téléphone ne suit pas.
>
> **Lumière** : un accent est une couleur, pas une lumière ; une espèce n'émet qu'en éclair, sur un
> événement.

## Règles communes

- PNG, **un fichier par image**, déposé sous `Assets/IdlePond/Art/` au **nom exact** du
  provisoire. L'import est automatique (1 px par unité, filtrage net, sans compression).
- Pixels **opaques ou transparents**, jamais de demi-transparence.
- Couleurs prises dans la **palette de la famille** (ci-dessous) ; une couleur nouvelle
  s'ajoute d'abord dans `RegistreDArt.cs`.
- Le générateur de provisoires ne remplace jamais un fichier déposé à la main.

## Le héros — `Heros/corps-s{stade}-i{image}.png`

Le poisson regarde **à droite**. Quatre images de nage par stade : **seules la queue et les
nageoires bougent** ; le tronc est identique d'une image à l'autre (les marques s'y posent).

| Stade | Niveau | Longueur | Cadre (l × h) |
|---|---|---|---|
| 0 | 1 à 3 | 32 | 34 × 22 |
| 1 | 4 à 15 | 42 | 44 × 31 |
| 2 | 16 à 255 | 63 | 65 × 45 |
| 3 | 256 et plus | 95 | 97 × 62 |

Le héros est dessiné (bible v0.4) par `outils/da/heros.py`, d'après `docs/da/inspirations/` : la
hauteur du cadre est celle de ses nageoires. Les calques d'éveil (voile, anneau, aura) ont le cadre
du corps agrandi de 12, 15, 21 et 27 px de chaque côté selon le stade ; l'étoile fait 7 × 7.

Points d'ancrage (origine **en bas à gauche**), un par marque possible :

| Stade | Branchies | Tête | Dos | Ventre | Flanc | Queue |
|---|---|---|---|---|---|---|
| 0 | 24, 11 | 27, 9 | 19, 15 | 19, 7 | 19, 11 | 13, 11 |
| 1 | 33, 16 | 37, 14 | 26, 20 | 26, 12 | 26, 16 | 17, 16 |
| 2 | 50, 24 | 56, 23 | 38, 30 | 38, 18 | 38, 24 | 23, 24 |
| 3 | 77, 33 | 87, 32 | 63, 41 | 63, 25 | 63, 33 | 42, 33 |

Chaque ancrage est un pixel **opaque du tronc**, de la même couleur dans les quatre images.
Ce tableau est le calcul de `Gabarits.Ancrage` ; si le code change, c'est lui qui fait foi.

## Les marques — `Heros/marques/{assise}-s{stade}.png`

Même cadre que le corps de son stade ; ne peint **que sur le corps**. Une marque par assise
traversée, empilées dans l'ordre des assises. GDD §10.3 prévoit deux variantes (acclimatation
complète ou interrompue) : le noyau ne les suit pas encore, une seule est livrée.

| Assise | Ancrage | Teinte du corps | Lumière |
|---|---|---|---|
| noue | Branchies | — | — |
| gour | Dos — les nageoires du dos et du ventre | — | — |

Les nageoires du héros ne battent pas (seule la queue bat) : la marque du Gour s'y pose, une
colonne de membrane bleue entre deux rayons, la colonne suivante reste orange.

## Les espèces — `Especes/{id}-i{0,1}.png`

Deux images de nage, regard à droite : la queue haute, la queue basse (l'ombre change aussi la
couleur de son voile). Un poisson de troupe mesure **24 px** ; les six tribus livrées partagent un
**cadre commun de 39 × 27**, le plus petit qui les contient toutes (la queue-feuille du vairon, les
barbillons de la loche, la voile de l'ombre). Elles sont dessinées, d'après la maquette validée,
par `outils/da/especes.py` — ce ne sont plus des provisoires.

## Les représentants — `Especes/representants/{id}-i{0..3}.png`

Le chef ou l'ancien de chaque tribu (Codex v1.4) : le même dessin à **60 px**, en **quatre images**
de nage comme le héros (la queue au milieu, haute, au milieu, basse ; le voile de l'ombre change de
couleur à mi-course), dans un **cadre commun de 94 × 58**. Toujours plus petit que le héros adulte
(97 × 62) ; un seul par tribu à l'écran. Il nage en tête de sa troupe, entre dans sa bande à la
conviction, et son portrait (l'image 0) remplace l'icône dans la fiche du tiroir Espèces. Produits
par `outils/da/especes.py` avec les poissons de troupe ; mêmes couleurs.

## La faune d'ambiance — `Faune/{nom}-i{n}.png`

Ce qui vit dans la mare sans produire (Codex v1.3, §2), dessiné au pixel par
`outils/da/faune.py` ; palette dans `RegistreDArt.FAUNE_DESSINEE`. Toutes les images d'une bête
ont le même cadre (le dessin est posé en bas à gauche) ; regard à droite.

| Bête | Cadre | Images | Où, quand |
|---|---|---|---|
| `libellule` | 14 × 7 | 3 (ailes hautes, basses, lanterne allumée) | deux, près de la surface de la Noue ; la lanterne s'allume en éclair, 0,4 s toutes les 7 s |
| `gerris` | 11 × 5 | 2 | trois, sur la surface ; ils filent puis se posent, et laissent une rune (un éclat pâle) 1,2 s |
| `grenouille` | 14 × 8 | 2 (repos, gorge gonflée) | au pied des nénuphars, les yeux à la surface ; une de plus par renaissance (trois au plus), et un chant plus fréquent |
| `hydre` | 7 × 12 | 2 | accrochée sous la surface, parmi les racines |
| `fleur` | 7 × 9 | 3 (bourgeon, fleur, grande fleur) | les jardins de la Sève, au fond des bandes de la Noue : une fleur de plus par renaissance (six au plus), qui grandit à la 3ᵉ et à la 6ᵉ |
| `ecrevisse`, `ecrevisse-banniere` | 14 × 6, 14 × 11 | 2 chacune | quand une tribu est ralliée : quatre écrevisses traversent le fond de sa bande en 7 s, la première porte la bannière de Sève |

Les saisons (décision du 2026-11-18, `Saisons.cs`) ajoutent `glace` (48 × 4, une tuile pavée sur
la surface l'hiver), `oeufs` (7 × 5, les grappes du frai au printemps) et `feuille` (6 × 4, deux
images, les feuilles mortes de la crue à l'automne). Les retours visuels (`Gerbes.cs`) n'ont pas de
dessin : ce sont des éclats teintés.

Les méduses d'eau douce du Gour sont dans son décor (`ambiance.png`) ; `meduse` (7 × 7, une
image) n'est que leur portrait dans la bibliothèque. Chaque bête y a sa fiche, dont le portrait
est une de ses images (la lanterne allumée, la gorge gonflée, la bannière, la grande fleur).

## Le décor — `Fonds/{assise}/`

| Fichier | Taille | Rôle |
|---|---|---|
| `fond.png` | 48 × 84 | une bande d'eau, répétée en largeur, du clair en haut au sombre en bas ; en bas, la vase et ses herbes, ou les galets et leur mousse |
| `rayons.png` | 384 × 168 | les rayons du jour sur les deux premières bandes, tramés |
| `berge.png` | 384 × 84 | la surface, les nénuphars, les racines qui pendent de la première bande (si l'assise a une berge) |
| `ambiance.png` | 384 × 84 | ce qui flotte dans chaque bande, devant l'eau : le pollen ; les méduses et les grosses pierres (si l'assise en a) |

## L'eau — `Eau/`

`voile.png` (8 × 8, damier blanc, teinté au rendu) ; `eclat.png` (2 × 2, blanc).

## Palettes

- **Héros** (bible v0.3) : `#F2B84A` corps (or), `#C07A2A` dos et rayons des nageoires
  (ambre), `#FFE6A8` ventre, `#FFF0C0` reflet, `#E89A3A` nageoires, `#FF9A80` joue, `#121C1A`
  contour ; marque de la Noue `#8FE070` (vert de Sève), du Gour `#8FD8FF` (bleu de Courant).
- **Espèces** (corps · dos · ventre · accent), contour `#121C1A` ; le dessin y ajoute nageoires,
  reflets, taches, joues et l'œil (toute la palette dans `RegistreDArt.ESPECES_DESSINEES`) :

| Espèce | Corps | Dos | Ventre | Accent |
|---|---|---|---|---|
| vairon | `#7FC8A0` | `#2E7D5B` | `#E8483A` | `#B8E86A` (queue-feuille) |
| loche | `#C08A3E` | `#6E4A22` | `#F0D9A0` | `#4FE0D0` (runes) |
| épinoche | `#6FA05A` | `#2F5A35` | `#D8283A` (gorge) | `#FF8FA0` (épines) ; œil `#4AA8FF` |
| chabot | `#7C8790` | `#4B545C` | `#C9CFC9` | `#C8A040` (lichen) |
| lamproie | `#9B7FC0` | `#5A3F7A` | `#D9C8F0` | `#9FF0FF` (anneau de bouche) |
| ombre | `#A8B8D8` | `#4A5A8A` | `#E8EEF8` | voile `#C060F0` / `#40D0C0` (une couleur par image) |

- **Noue** (la Sève) : eau, du haut vers le bas, `#A8F0D0` `#5CC8A0` `#2F9A80` `#1E6A62`
  `#163E44` ; surface `#D8FFF0` ; rayons `#FFF0A0` ; nénuphars `#4FB85A` `#6FD070`, fleur
  `#F4A8C8`, cœur `#FFE070` ; racines `#6A4C30`, bourgeon `#8FE070` ; herbes `#3FA060` `#5CC070`
  `#7CD070` ; vase `#4A3424` ; pollen `#FFE070` `#F6C840` `#DFFFA0`.
- **Gour** (le Courant) : eau `#9ADCF8` `#5AB0E6` `#3A80C8` `#24589A` `#16345E` ; écume `#E6F8FF`
  `#BFEAFF` ; galets `#8FA8C0` `#6E88A4` `#A8BCD0`, fond `#2A3E58` ; mousse `#4FA070` ; grosses
  pierres `#7A90A8` `#93A8BE` ; méduses `#E8F4FF` `#C8E8FF`.
- **Roche à creuser** : inchangée, voir `RegistreDArt.cs`.
- **Eau** : blanc `#FFFFFF`.

## Rendu et contrôle

- Les lumières de bande sont des `Light2D` de type **Sprite** (un sprite blanc de 1 × 1
  étiré aux dimensions de la bande), pas des lumières Freeform : l'intérieur d'une Freeform
  exige le paquet `com.unity.2d.common`, absent du projet.
- La texture du rendu en pixels a un tampon de profondeur de **24 bits** (exigé par URP 2D).
  Sous `-nographics` (tests en batch), `RenduPixel` calcule les dimensions mais ne crée
  aucune texture : le vrai chemin de la texture n'est exercé que par `outils/unity.sh captures`.
- `outils/unity.sh captures` écrit **18 PNG** dans `Logs/captures/` (le dossier est vidé avant
  la course) : portrait et paysage × niveaux 1, 4, 16, 256 × avec et sans voile (16), plus
  `portrait-surface.png` (2 bandes ouvertes : la berge, les racines et les rayons) et
  `portrait-mue.png` (0,15 s après le passage du niveau 3 au 4 : l'éclair et les écailles).
  Aucune capture d'écran : elle est vide en batch, le chemin réel de #scene se contrôle à la
  main, éditeur ouvert.
