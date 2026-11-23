# IdlePond — le cahier des gabarits

Ce que doit respecter un dessin pour remplacer un provisoire **sans toucher au code**.
Source de vérité : `Assets/IdlePond/Jeu/Scene/Gabarits.cs` et `RegistreDArt.cs` ; les tests
`GabaritsTests` vérifient chaque fichier livré.

> **Cible validée le 2026-11-23, pas encore dans le code** (Codex v1.4, §2 ; maquettes
> `apercu-poissons-v3.png` et `apercu-finesse-scene.png`). Tant que le code n'a pas changé, les
> sections suivantes décrivent l'état actuel et font foi pour déposer un PNG ; ce cadre dit ce
> qui va changer, pour que personne ne dessine à l'ancienne échelle.
>
> - **La scène passe à 360 px de large** (`Gabarits.LARGEUR_VISEE`, aujourd'hui 240 ; repli sur
>   320 si un vrai téléphone ne suit pas). Tout l'art provisoire se refait à cette échelle :
>   décor, bandes, héros, marques.
> - **Le poisson de troupe** : environ **24 px** de long (aujourd'hui 7 à 12). La formule de
>   longueur par rang est à revoir avec la finesse.
> - **Le représentant** de chaque tribu : un gabarit de plus, `Especes/representants/{id}-i{0..3}.png`,
>   environ **60 px** de long, quatre images de nage comme le héros (seules la queue et les
>   nageoires bougent), toujours plus petit que le héros adulte. Un seul par tribu à l'écran.
> - **La taille à l'écran reste réglable** à part, par les gabarits.
> - **Lumière** : un accent est une couleur, pas une lumière ; une espèce n'émet qu'en éclair,
>   sur un événement.
>
> **Déjà dans le code** (2026-11-23, à 240 px) : les palettes des six espèces et leur accent,
> celles de la Noue, du Gour et du héros (bible v0.3, Codex v1.5) — voir « Palettes ».

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
| 0 | 1 à 3 | 21 | 23 × 15 |
| 1 | 4 à 15 | 28 | 30 × 17 |
| 2 | 16 à 255 | 42 | 44 × 23 |
| 3 | 256 et plus | 63 | 65 × 31 |

Points d'ancrage (origine **en bas à gauche**), un par marque possible :

| Stade | Branchies | Tête | Dos | Ventre | Flanc | Queue |
|---|---|---|---|---|---|---|
| 0 | 18, 7 | 20, 7 | 15, 10 | 15, 4 | 13, 7 | 9, 7 |
| 1 | 24, 8 | 26, 8 | 19, 11 | 19, 5 | 16, 8 | 12, 8 |
| 2 | 36, 11 | 39, 11 | 28, 16 | 28, 6 | 24, 11 | 17, 11 |
| 3 | 53, 15 | 58, 15 | 42, 23 | 42, 7 | 36, 15 | 26, 15 |

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

Deux images de nage, regard à droite. Longueur `7 + min(5, rang / 4)` (division entière) : 7 px (rang 0) à
12 px (rang 20) ; cadre `(longueur + 2) × (2·⌈0,2·longueur⌉ + 5)`. Livrées : vairon, loche
(7 px, cadre 9 × 9).

## Le décor — `Fonds/{assise}/`

| Fichier | Taille | Rôle |
|---|---|---|
| `fond.png` | 32 × 56 | une bande d'eau, répétée en largeur, du clair en haut au sombre en bas ; en bas, la vase et ses herbes, ou les galets et leur mousse |
| `rayons.png` | 256 × 112 | les rayons du jour sur les deux premières bandes, tramés |
| `berge.png` | 256 × 56 | la surface, les nénuphars, les racines qui pendent de la première bande (si l'assise a une berge) |
| `ambiance.png` | 256 × 56 | ce qui flotte dans chaque bande, devant l'eau : le pollen ; les méduses et les grosses pierres (si l'assise en a) |

## L'eau — `Eau/`

`voile.png` (8 × 8, damier blanc, teinté au rendu) ; `eclat.png` (2 × 2, blanc).

## Palettes

- **Héros** (bible v0.3) : `#F2B84A` corps (or), `#C07A2A` dos et rayons des nageoires
  (ambre), `#FFE6A8` ventre, `#FFF0C0` reflet, `#E89A3A` nageoires, `#FF9A80` joue, `#121C1A`
  contour ; marque de la Noue `#8FE070` (vert de Sève), du Gour `#8FD8FF` (bleu de Courant).
- **Espèces** (corps · dos · ventre · accent), contour `#121C1A` :

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
