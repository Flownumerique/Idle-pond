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
> - **Chaque espèce gagne un accent** : une quatrième couleur, sur quelques pixels (épines,
>   runes, voile…). Champ `Accent` à ajouter à `CouleursDePoisson`.
> - **Lumière** : un accent est une couleur, pas une lumière ; une espèce n'émet qu'en éclair,
>   sur un événement.
> - **Nouvelles palettes d'espèces** (corps · dos · ventre · accent) :
>
> | Espèce | Corps | Dos | Ventre | Accent |
> |---|---|---|---|---|
> | vairon | `#7FC8A0` | `#2E7D5B` | `#E8483A` | `#B8E86A` (queue-feuille) |
> | loche | `#C08A3E` | `#6E4A22` | `#F0D9A0` | `#4FE0D0` (runes) |
> | épinoche | `#6FA05A` | `#2F5A35` | `#D8283A` (gorge) | `#FF8FA0` (épines) ; œil `#4AA8FF` |
> | chabot | `#7C8790` | `#4B545C` | `#C9CFC9` | `#C8A040` (lichen) |
> | lamproie | `#9B7FC0` | `#5A3F7A` | `#D9C8F0` | `#9FF0FF` (anneau de bouche) |
> | ombre | `#A8B8D8` | `#4A5A8A` | `#E8EEF8` | voile `#C060F0` / `#40D0C0` (une couleur par image) |
>
> Les palettes des lieux (Noue, Gour) et du héros sont la prochaine étape.

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

## Les espèces — `Especes/{id}-i{0,1}.png`

Deux images de nage, regard à droite. Longueur `7 + min(5, rang / 4)` (division entière) : 7 px (rang 0) à
12 px (rang 20) ; cadre `(longueur + 2) × (2·⌈0,2·longueur⌉ + 5)`. Livrées : vairon, loche
(7 px, cadre 9 × 9).

## Le décor — `Fonds/{assise}/`

| Fichier | Taille | Rôle |
|---|---|---|
| `fond.png` | 32 × 56 | une bande d'eau, répétée en largeur ; la vase en bas |
| `rayons.png` | 256 × 112 | les rayons du jour sur les deux premières bandes, tramés |
| `berge.png` | 256 × 56 | les racines qui pendent de la première bande (si l'assise a une berge) |

## L'eau — `Eau/`

`voile.png` (8 × 8, damier blanc, teinté au rendu) ; `eclat.png` (2 × 2, blanc).

## Palettes

- **Héros** : `#C49C5C` corps, `#8C683A` dos, `#E2CE96` ventre, `#121C1A` contour ;
  marque de la Noue `#5E4426`.
- **Espèces** : vairon `#96AAA0` `#566C64` `#C8D4CC` ; loche `#A08A5E` `#6A5A3A` `#CCB88A` ;
  contour `#121C1A`.
- **Noue** : eau `#4A8070` `#34645C` `#244A48`, vase `#28221A`, racines `#3A2C20` `#5C4630`,
  rayon `#96BE96`.
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
