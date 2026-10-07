# L'écran mobile — la mare au centre, les menus en tiroir — design

Écrit le 2026-10-07. Validé en conversation le même jour, à partir de la maquette de
l'utilisateur (canevas « IdlePond — Écran principal mobile » : la vue mare et trois menus
ouverts). C'est le sous-projet « habillage de l'UI » que la spec DA du 2026-10-02 renvoyait
à plus tard.

## Intention

Un idle classique pensé pour le téléphone : on REGARDE sa mare. L'écran actuel est une page
(la scène en bandeau de 40 %, puis des panneaux qui défilent) ; il devient une vue, avec
une barre fine en haut, la mare au centre, un dock de boutons en bas, et des tiroirs.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| La vue centrale | La **coupe** actuelle (paliers, lumières), pas la vue de dessus de la maquette |
| Les textes et les actes | Ceux du **jeu** (Souffle, Convaincre, Monter…). La maquette donne la disposition et le style. Ses « Foi », « Bénédictions », « Place » sont d'ailleurs des mots morts (Codex §5) |
| Creuser | Un **bouton posé sur la mare**, au-dessus du dock |

## §1. La disposition (portrait d'abord)

- **Barre du haut** : le mana (gros, Pixelify) et le débit ; la pastille du Souffle à
  droite ; la jauge de contenance dessous, avec en fantôme la part que l'acquis de séjour
  ajoute (`Contenance = ContenanceMana × (1 + AcquisDeSejour)`). L'eau trouble éteint la
  jauge, sans texte (GDD §2.4).
- **La mare** : `#scene` remplit tout l'espace entre la barre et le dock. Par-dessus :
  l'étiquette du lieu (nom + profondeur en brasses, jamais « palier », §3), la ligne de
  retour d'absence, les annonces de succès, et le bouton **Creuser plus bas**, en bas au
  centre. Creuser suit la règle des achats : éteint quand on ne peut pas payer, caché
  seulement quand il n'y a plus de roche.
- **Le dock** : Toi, Espèces, Insuffler, Journal, L'œuf (doré). La pastille du Journal
  compte les succès arrivés depuis sa dernière ouverture.
- **Les tiroirs** : montent du bas, 78 % de la hauteur, poignée et bouton fermer ; la mare
  reste visible derrière un voile. Toucher le voile, le bouton fermer, ou le bouton actif
  du dock referme.

| Tiroir | Contenu (contrôleurs existants, déplacés) |
|---|---|
| Toi | Héros (Grandir) ; le détail de la contenance (débit du héros, saturation) |
| Espèces | Les cartes d'espèces ; la table de captation s'y ouvre au-dessus |
| Insuffler | Insufflations |
| Journal | Succès |
| L'œuf | Le nombre de retours dans l'œuf ; Renaissance |

**Paysage** (largeur ≥ 1,2 × hauteur) : même disposition ; le tiroir s'ouvre à droite, sur
40 % de la largeur et toute la hauteur de la mare.

## §2. Le style

- Police (OFL, versionnée avec sa licence) : **Nunito** pour tout ce qui s'affiche — texte,
  titres et compteurs de la barre en gras, chiffres, prix. Révisé le même jour à la demande
  de l'utilisateur (« les textes doivent être en une police lisible ») : Pixelify Sans, puis
  Atkinson Hyperlegible, ont été essayées et écartées. JetBrains Mono quitte l'écran.
- Palette de la maquette : fond `#0B1716`, bordure `#1E3532`, surface `#14302C`, tiroir
  `#10211F`, texte `#E6EFEA`, texte doux `#9FB8B1`, mana `#6FE0CF`, or `#F0B54A`.
- Les icônes du dock sont en **pixel art**, des grilles de 12 × 12 écrites en données et
  dessinées par `Painter2D` dans la couleur du texte : pas de fichier image, pas de paquet.

## §3. Ce qui ne change pas

- Aucune règle du jeu dans l'interface (§5.3) ; les contrôleurs gardent leur logique.
- Les noms que les tests lisent : `mana-valeur`, `souffle-valeur`, `espece-*`,
  `annonces`, `scene`.
- `Adaptation` (portrait / paysage) reste la seule décision de mise en page du code.

## Critère de réussite

1. Tests verts (EditMode, PlayMode), dont : le dock ouvre et referme les tiroirs ; un seul
   tiroir à la fois ; les icônes sont des grilles valides.
2. Dans l'atelier, en portrait comme en paysage : la mare occupe le centre, les cinq
   tiroirs s'ouvrent, Creuser est au-dessus du dock. Captures relues par l'utilisateur.

## Révision 2 — la maquette « plus lisible » (2026-10-07)

L'utilisateur a mis à jour sa maquette. Ce qui change :

- **Le dock passe à quatre boutons** : Toi, Espèces, L'œuf, Journal. Les insufflations
  quittent le dock pour le tiroir de l'œuf : elles se paient en Souffle, qui se gagne en y
  rentrant.
- **Chaque tiroir a un sous-titre** sous son titre.
- **Toi** : une fiche (vignette, « Rester t'a gagné +x % », grille captation / contenance /
  toi seul / retours dans l'œuf), puis la carte Grandir. Elle remplace le panneau de
  contenance.
- **Espèces** : groupées par lieu (en-tête, profondeur atteinte, « convaincues / total ») ;
  chaque rangée porte sa vignette à la couleur de l'espèce, ses crans, son débit, la barre
  vers le prochain seuil de jalon (« ×4 au cran 25 ») et son bouton. Une espèce plus bas est
  grisée, sans nom (« ??? à 3 brasses ») ; le lieu suivant est montré verrouillé, ses espèces
  en « ??? » — il n'a pas encore de nom, l'écran dit « Plus bas ».
- **L'œuf** : l'œuf, le Souffle laissé et ce que la contenance gardera, « Tu emportes / Tu
  laisses », le bouton (toujours confirmé par Rentrer / Rester), puis les insufflations. Le
  gain ne se calcule que tiroir ouvert.
- **Journal** : un résumé « N sur M arrivés dans ce lieu », des filtres par famille (Tous,
  Profondeur, Seuils, Gestes) et une grille de cartes.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| « Réclamer » et les rangs Or / Argent / Bronze | **Écran seulement** : les effets restent automatiques, ni geste ni rang — le noyau ne change pas |
| Les lieux pas encore livrés | **Le suivant verrouillé**, puis « ??? » |

Le vocabulaire reste celui du jeu : la maquette écrit « Foi », « Place », « Population »,
« Acclimatation », « Assise II », « palier 3 / 6 », mots morts ou interdits à l'écran
(Codex §5, `LexiqueTests`).
