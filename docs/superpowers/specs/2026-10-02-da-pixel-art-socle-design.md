# La DA pixel art, le socle et la Noue — design

Écrit le 2026-10-02. Validé section par section en conversation le même jour, avec des
maquettes (finesse du pixel, croissance du héros, style de l'interface).

## Intention

La scène d'IdlePond passe de la coupe au trait (portée du web) au **pixel art 2D**. Ce
document conçoit le **premier sous-projet** de la direction artistique : le socle de rendu,
et la Noue entièrement dessinée avec des sprites **provisoires générés par script**. Le vrai
dessin viendra plus tard et remplacera les provisoires **sans toucher au code**.

Le cœur, c'est la consigne du 2026-09-17 : le joueur doit **voir son poisson évoluer**.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| Qui dessine | Personne pour l'instant : des provisoires en pixel art générés par script, le vrai dessin plus tard |
| La finesse | **Grain moyen : 240 px de large** pour la scène |
| La croissance du héros | Le sprite **change** quand il grossit (des tailles dessinées), pas un agrandissement |
| Le corps | Chaque milieu traversé laisse son adaptation sur le héros (peau caillouteuse pour la pression, rouges de lave, électricité…), qui **s'accumule** et rend le sprite de plus en plus beau et coloré |
| Le registre | Les milieux élémentaires (lave, électricité…) sont **admis** — amendement du GDD §15.2 — **et** représentés par des phénomènes réels : volcanisme sous-marin, bioélectricité. Ni flamme ni magie |
| L'interface | **Hybride** : cadres, boutons et jauges en pixel art, textes et chiffres en police nette. C'est un sous-projet **à part** |
| Le rendu | **URP 2D**, rendu en basse résolution dans une texture, avec de vraies lumières 2D |

### Ce qui est supposé, pas dit

- Le corps par accumulation du GDD §15.1 est le système qui porte la demande : une marque
  par assise fixée. La « peau caillouteuse » est sa *minéralisation*.
- L'attribution d'un milieu à chaque assise (laquelle est la lave, laquelle l'électricité)
  est du **contenu**, tranché avec les assises II à VI. Ce socle ne fixe que le mécanisme.
- Les deux variantes de marque du GDD §10.3 (acclimatation complète ou interrompue) ne sont
  pas suivies par le noyau. Le gabarit leur réserve une place ; la Noue n'en dessine qu'une.

### Critère de réussite

1. Tous les tests NUnit sont verts en batch (EditMode et PlayMode), et la CI aussi.
2. La Noue se joue entièrement en pixel art : six paliers où la lumière baisse, les bancs du
   vairon, de la loche et de l'épinoche, le héros qui mue trois fois puis redevient petit à
   la renaissance **en gardant sa marque**, et le voile d'eau trouble.
3. Les captures de contrôle en portrait et en paysage, aux stades 0 à 3, ont été relues par
   l'utilisateur.
4. Un PNG déposé à la place d'un provisoire, conforme à son gabarit, s'affiche sans aucune
   modification de code.

## §1. Périmètre

**Dedans.** La migration vers URP 2D ; le rendu en basse résolution affiché dans `#scene` ;
les lumières 2D et la courbe de lumière par palier ; le héros composé (quatre stades, des
marques ancrées, la mue) ; les décors des six paliers de la Noue ; les trois bancs ; le
voile ; le générateur de provisoires ; les gabarits et leur cahier des charges.

**Dehors, chacun sa spec.** L'habillage de l'UI (option hybride) ; les assises II à VI et
leurs milieux ; le vrai dessin ; les animations riches, les particules au-delà de la mue,
l'audio ; l'import Aseprite.

**Prévu pour six assises dès maintenant.** Une assise apporte, en **données** : sa palette,
sa courbe de lumière, son décor, sa marque sur le héros (calque, teinte, lumière). Ajouter
la lave plus tard, c'est ajouter une entrée de registre et des PNG — pas du code.

## §2. Le rendu

- **URP 2D Renderer.** Une caméra de scène orthographique dédiée, à **1 unité Unity = 1
  pixel** du dessin.
- **Une texture de rendu**, filtrage `Point`, affichée en fond de l'élément `#scene` de
  l'UI Toolkit. Le facteur d'agrandissement **k est un entier** : `k = max(1, round(largeur
  de #scene en pixels écran / 240))`. La texture fait `⌈largeur / k⌉ × ⌈hauteur / k⌉`. Tous
  les pixels ont la même taille ; la largeur visible du monde varie un peu d'un écran à
  l'autre autour de 240, la hauteur suit l'élément (≈ 170 px en portrait, moins en paysage).
  La texture est recréée quand `#scene` change de taille (`GeometryChangedEvent`).
- Le cadrage actuel (la caméra bornée au rectangle de `#scene` par `camera.rect`)
  **disparaît**. La caméra principale ne sert plus qu'à l'UI.
- **Défilement.** La caméra garde le palier le plus bas visible, comme aujourd'hui, mais sa
  position est arrondie au **pixel entier** — aucun tremblement de sous-pixel.
- **La lumière.**
  - une lumière ambiante globale, faible ;
  - une **Light2D par palier**, sur sa bande, dont l'intensité et la teinte suivent la
    *courbe de lumière de l'assise* (données) ;
  - des lumières ponctuelles portées par ce qui émet : les marques du héros qui en déclarent
    une, plus tard le mana.
  
  Tout étant rendu à la résolution de la texture, la lumière est pixellisée elle aussi.
- **Budget.** Seules les lumières des paliers visibles sont actives : environ quatre à
  l'écran.

## §3. Le héros composé

Le sprite du héros est une **fonction de son stade et de ses marques**, calculée par la
vue pure.

**Le stade** (0 à 3) remplace l'échelle continue de la spec 2026-09-17 [D12].

| Stade | Niveau du héros | Longueur du corps |
|---|---|---|
| 0 | 1 à 3 | 21 px |
| 1 | 4 à 15 | 28 px |
| 2 | 16 à 255 | 42 px |
| 3 | 256 et plus | 63 px |

`niveauDuHeros` appartient au cycle : à la **renaissance**, le héros redevient stade 0 —
mais garde **toutes** ses marques. Il repart petit, chaque fois plus paré : c'est le retour
dans l'œuf, rendu visible.

**Les marques** viennent de `permanent.couches` (les assises fixées, dans l'ordre). Pour
chaque assise, le registre d'art déclare :

- un **calque** par stade, posé sur un **point d'ancrage** du corps (le GDD prévoit six
  paires d'ancrages) ;
- en option, une **teinte** appliquée au corps (le rouge de la lave, le gris de la pierre) ;
- en option, une **lumière** émise (luminescence, décharge).

Les calques s'empilent dans l'ordre des couches : la dernière assise traversée est dessus.

**La nage.** Quatre images où **seuls la queue et les nageoires bougent** ; le tronc, qui
porte les ancrages, est identique d'une image à l'autre. S'y ajoute une ondulation
verticale d'un pixel. C'est cette contrainte qui permet de poser les marques sans les
redessiner à chaque image ; elle est écrite dans le gabarit.

**La mue.** Quand le stade monte, la scène joue un événement court : un flash, des écailles
qui tombent (particules), puis la nouvelle taille. La vue expose le stade précédent pour
que la scène le détecte ; elle ne la joue pas au chargement d'une partie.

## §4. Le décor, les bancs, le voile

- **Un palier est une bande de 56 px.** Le héros au stade 3 (63 px) déborde de sa bande :
  c'est voulu, il devient plus grand que son monde. La valeur est mesurée sur les captures
  avant d'être figée.
- **Chaque assise fournit son décor** (données + PNG) : un fond qui se répète en largeur, un
  sol de vase, des éléments de bord, sa palette, sa courbe de lumière.
- **La berge de la Noue** porte l'inversion d'échelle (GDD §15.2) : des racines géantes qui
  pendent du haut de la première bande, et la lumière qui tombe en rayons tramés.
- **Les bancs.** Chaque espèce a un sprite animé de 2 à 4 images, de 7 à 12 px selon son
  rang. L'effectif dessiné reste `1 + ⌊log₂ niveau⌋`, plafonné à 14. Les poissons nagent en
  aller-retour dans leur bande ; leurs décalages sont tirés d'une **graine fixe** par palier
  — même état, même image.
- **L'eau trouble.** Un voile **tramé** en pixels, dans la couleur de l'assise, qui monte en
  0,9 s quand `eauTroublee` passe à vrai. Sans texte (GDD §2.4).
- **Ordre de rendu** (Sorting Layers nommées, de l'arrière vers l'avant) : `Fond`, `Rayons`,
  `Bord`, `Bancs`, `Corps`, `Marques`, `Particules`, `Voile`.

## §5. Les assets, les gabarits, les provisoires

**Rangement.** `Assets/IdlePond/Art/` : `Heros/`, `Especes/`, `Assises/<id>/`. Des PNG au
nom imposé, par exemple `Heros/corps-stade2.png` (une planche de quatre images),
`Heros/marques/noue-stade2.png`, `Especes/vairon.png`, `Assises/noue/fond.png`.

**Import imposé par script.** Un `AssetPostprocessor` règle tout ce qui entre dans `Art/` :
1 pixel par unité, filtrage `Point`, sans compression, sans mipmaps, découpe des planches
selon le gabarit, pivot fixe. Un réglage d'import n'est jamais fait à la main.

**Les gabarits sont des données** (`Jeu/Scene/Gabarits.cs`) : dimensions et nombre d'images
de chaque planche ; ancrages du héros par stade ; palettes autorisées par famille. Le
générateur et les tests les lisent. Un document `docs/da/gabarits.md` en est tiré : c'est le
**cahier des charges** du vrai dessin.

**Le générateur de provisoires.** Menu « IdlePond ▸ Générer les sprites provisoires », et
point d'entrée batch. Il dessine par code : formes, contour d'un pixel, tramage ordonné,
palettes de la Noue. Il produit les corps aux quatre stades, la marque de la Noue (les
branchies), les trois espèces, le décor des six paliers et le voile. Il est **déterministe**
et n'écrase **jamais** un PNG qui ne vient pas de lui : chaque provisoire porte une
étiquette dans son `.meta` (`userData`), et un fichier sans elle est un vrai dessin, protégé.

**Remplacer un provisoire** : déposer le PNG du même nom, aux mêmes dimensions. Les tests
vérifient sa conformité au gabarit.

## §6. Le code

**`Jeu/Scene/VueDeScene.cs`** (pure, inchangée dans son principe) :
- `VueDuHeros` : `Echelle` devient `Stade` ; s'ajoutent `StadePrecedent` et, pour chaque
  couche, l'assise qui l'a donnée ;
- `Clef` suit ces champs.

**`SceneDeLaMare` est réécrite** et découpée en classes à rôle unique, sous
`Jeu/Scene/` :

| Classe | Rôle |
|---|---|
| `SceneDeLaMare` | MonoBehaviour racine : écoute `Partie.EtatChange`, calcule la vue, distribue |
| `RenduPixel` | La texture de rendu, le facteur entier k, l'affichage dans `#scene`, la caméra de scène |
| `Decor` | Les bandes, fonds, sols et bords, par assise |
| `Eclairage` | L'ambiante, la Light2D par palier, la courbe de lumière |
| `Bancs` | Les poissons de chaque palier, leur nage |
| `HerosEnPixels` | Le corps, les marques, leurs teintes et lumières, la mue |
| `Voile` | L'eau trouble |
| `Gabarits`, `RegistreDArt` | Données : formats, ancrages, palettes ; une assise → décor, marque, lumière |

**L'éditeur** (`Editeur/`) :
- `GenerateurDeSprites` : les provisoires ;
- `ImportDArt` : l'`AssetPostprocessor` ;
- `ConfigurationURP` : crée et versionne l'asset de pipeline URP, le moteur de rendu 2D et
  l'assignation dans les réglages graphiques et de qualité — par script, comme les scènes ;
- `GenerateurDeScenes` : les Sorting Layers, la caméra de scène, la racine de la scène.

**Paquets ajoutés** : `com.unity.render-pipelines.universal` (le moteur de rendu 2D et
`Light2D` y sont inclus).

## §7. Les tests

**EditMode — la vue pure** (`VueDeSceneTests`) :
- les seuils de stade (3 → 0, 4 → 1, 15 → 1, 16 → 2, 255 → 2, 256 → 3) ;
- après une renaissance : stade 0, marques conservées et dans l'ordre ;
- la mue : `StadePrecedent` ≠ `Stade` exactement quand le niveau franchit un seuil ;
- l'effectif des bancs, inchangé.

**EditMode — les gabarits** (`GabaritsTests`) : chaque PNG de `Art/` a les dimensions et le
nombre d'images de son gabarit ; chaque ancrage tombe dans le tronc du corps de son stade ;
chaque pixel opaque appartient à une palette autorisée ; chaque assise livrée a son décor,
sa marque et sa courbe de lumière ; chaque espèce livrée a son sprite.

**EditMode — le générateur** (`GenerateurDeSpritesTests`) : deux passages produisent les
mêmes octets ; un PNG sans étiquette n'est pas écrasé.

**PlayMode** : la scène rend dans une texture dont la taille est l'élément `#scene` divisé
par un k entier ; `MareJouableTests` reste vert.

**Capture de contrôle** : `outils/unity.sh methode IdlePond.Editeur.Captures.Prendre` (batch
**avec** affichage graphique) écrit des PNG en 1080×1920 et 1920×1080, héros aux stades 0 à
3, avec et sans voile, dans `Logs/captures/`. Une capture se relit ; elle ne s'assertit pas.

## §8. Le GDD

- **§15.2, amendement du registre** : les assises profondes peuvent être des milieux
  élémentaires, toujours rendus par des phénomènes réels (volcanisme sous-marin sans flamme,
  bioélectricité). « Dominante vitale » et « pas magique » restent la règle ; « pas de
  combustion sous l'eau » aussi.
- **[D12]** de la spec 2026-09-17 est remplacé par les stades du §3.

## §9. Découpage

| # | Étape | Fini quand |
|---|---|---|
| 0 | `ConfigurationURP`, paquet URP, rendu actuel inchangé sous URP | `MareJouableTests` vert sous URP |
| 1 | `RenduPixel` : texture à k entier dans `#scene`, caméra de scène dédiée | test PlayMode de la texture vert ; capture relue |
| 2 | `Gabarits`, `RegistreDArt`, `ImportDArt`, `GenerateurDeSprites` | `GabaritsTests` et `GenerateurDeSpritesTests` verts |
| 3 | `VueDeScene` : stades, mue, marques | `VueDeSceneTests` vert |
| 4 | `Decor` et `Eclairage` | captures des six paliers relues |
| 5 | `Bancs`, `Voile` | captures relues |
| 6 | `HerosEnPixels` : stades, marque de la Noue, mue | captures aux stades 0 à 3 relues |
| 7 | `docs/da/gabarits.md`, GDD §15.2, ROADMAP | documents relus |

## Risques

- **La migration URP casse le rendu ou l'UI Toolkit.** Parade : c'est l'étape 0, seule, et
  le test PlayMode la vérifie avant tout dessin.
- **Light2D trop lourdes sur mobile.** Parade : une lumière par palier visible seulement ;
  la compilation mobile reste à vérifier dès que les modules sont installés.
- **La bande de 56 px trop serrée au stade 3.** Parade : mesurée sur les captures de
  l'étape 6, ajustée avant d'écrire le cahier des gabarits.
- **Rendu non vérifiable en `-nographics`.** Les captures demandent un batch avec affichage ;
  la relecture reste humaine.
