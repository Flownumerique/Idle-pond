# Migration vers Unity — portage complet du jalon v0.2 amendé v1.1

> **Statut : portage écrit, parité établie hors Unity, recette dans l'éditeur
> à faire.** Le web reste la **référence** jusqu'à la bascule (§6). Toute
> modification du noyau, des données ou des textes se fait des deux côtés, puis
> `npm run reference:unity`.

Le projet Unity vit dans [`unity/IdlePond/`](../unity/IdlePond/README.md). Il
porte **tout** ce que le dépôt web contient de jouable et de mesurable : le
noyau, les données, les adaptateurs, le magasin, le simulateur et son
calibreur, l'écran, et les 118 tests — plus ce que la plateforme exige et que
le web n'avait pas à gérer (pause mobile, stockage natif, builds).

---

## 1. Ce qui est porté, et où

| Web (`src/`)                    | Unity (`unity/IdlePond/Assets/IdlePond/`)          | Assemblage             |
|---------------------------------|----------------------------------------------------|------------------------|
| `break_infinity.js`             | `Coeur/Nombres/GrandNombre.cs`, `NombreJs.cs`, `MathJs.cs` | `IdlePond.Coeur`  |
| `noyau/types.ts`                | `Coeur/Noyau/Types.cs`, `Identifiants.cs`, `TableOrdonnee.cs` | `IdlePond.Coeur` |
| `noyau/constantes.ts`           | `Coeur/Noyau/Constantes.cs`                        | `IdlePond.Coeur`       |
| `noyau/noyau.ts`                | `Coeur/Noyau/Reducteur.cs`                         | `IdlePond.Coeur`       |
| `noyau/economie.ts`, `densite.ts`, `maturation.ts`, `population.ts`, `eclosion.ts`, `technique.ts`, `voix.ts` | `Coeur/Noyau/` (un fichier chacun) | `IdlePond.Coeur` |
| `noyau/succes.ts`               | `Coeur/Noyau/SystemeDeSucces.cs`                   | `IdlePond.Coeur`       |
| `donnees/**`                    | `Coeur/Donnees/**`                                 | `IdlePond.Coeur`       |
| `adaptateurs/horloge.ts`, `hors-ligne.ts`, `persistance.ts`, `telemetrie.ts`, `boucle.ts` | `Adaptateurs/` (+ `Json.cs`) | `IdlePond.Adaptateurs` |
| `etat/magasin.ts` (Zustand)     | `Etat/Magasin.cs`                                  | `IdlePond.Etat`        |
| `ui/format.ts` + la logique des composants | `Presentation/Format.cs`, `Modeles.cs`  | `IdlePond.Presentation`|
| `ui/*.tsx`, `index.css`         | `Interface/Vues/*.cs`, `Interface/Resources/IdlePond/IdlePond.uss` | `IdlePond.Interface` |
| `main.tsx`, `App.tsx`           | `Interface/Jeu.cs`, `Ecran.cs`, `Amorce.cs`        | `IdlePond.Interface`   |
| `simulateur/**`                 | `Simulateur/Simulateur.cs`, `Calibreur.cs`         | `IdlePond.Simulateur`  |
| `tests/**` (118 tests)          | `Tests/EditMode/**` (178 tests)                    | `IdlePond.Tests.EditMode` |
| —                               | `Editeur/` : simulateur, sauvegardes, scène, builds | `IdlePond.Editeur`    |
| `scene/` (vide)                 | — (voir D13)                                       |                        |

### Le découpage, et ce qui le garde

```
IdlePond.Coeur          PUR. Aucune référence. Pas d'UnityEngine (noEngineReferences).
   ▲
IdlePond.Adaptateurs    Le monde impur : horloge, JSON, persistance. Pas d'UnityEngine.
   ▲
IdlePond.Etat           Le magasin. Pas d'UnityEngine.
IdlePond.Presentation   Ce que l'écran affiche, en chaînes. Pas d'UnityEngine.
IdlePond.Simulateur     Réutilise le Cœur tel quel. Pas d'UnityEngine.
   ▲
IdlePond.Interface      UnityEngine + UI Toolkit. Ne décide de rien.
IdlePond.Editeur        UnityEditor. Outils.
```

Le §5.3 (« `noyau/` n'importe rien hors de `noyau/` et `donnees/` ») est tenu
deux fois : par les `.asmdef` — Unity refuse de compiler une référence interdite —
et par `ArchitectureTests`, qui relit les `.asmdef`, le source (horloge, hasard,
fichiers, fils d'exécution, Unity) et, par réflexion, l'absence de tout champ
statique mutable dans le Cœur (« aucun état hors du reducer »).

---

## 2. Ce qui est garanti, et par quoi

| Garantie                                                        | Test                                   |
|-----------------------------------------------------------------|----------------------------------------|
| Les 118 tests du web, portés un à un                            | `Tests/EditMode/*Tests.cs`             |
| **Parité avec le web** : états complets à 1e-9 après 100 ms, 8 h, une divergence non choisie, une demi-heure jouée, une vie jouée à la main, 2 et **15 cycles simulés** | `Parite/PariteAvecLeWebTests.cs` |
| Parité : PRNG bit pour bit, arithmétique de break_infinity, coûts, instants de chaque succès de la première demi-heure | idem |
| Parité : **chaque chaîne des écrans** (6 états), textes des succès à l'octet | idem |
| Parité : saves web v1, v2, v3 migrées puis réécrites **à l'octet près** ; save du magasin web relue et réécrite à l'identique | idem |
| Le magasin (actes sauvegardés, retour annoncé, save illisible) et la boucle (dt d'horloge, recul, pause, trou d'horloge) | `MagasinEtBoucleTests.cs` |
| Nombres : écriture JS, `toFixed` exact, `expm1`, bizarreries de break_infinity | `NombresTests.cs`, `PersistanceTests.cs` |

La référence de parité est **produite par le TypeScript** :
`tests/parite/generer-reference-unity.ts` exécute le code web et écrit
`Tests/EditMode/Parite/reference-web.json`. Une mutation de 5·10⁻⁷ sur une seule
graine (`TauxBaseAuPalier0`) fait tomber 12 des 25 tests de parité : ils ne sont
pas décoratifs.

### Sans licence Unity

La CI (`.github/workflows/unity.yml`) vérifie tout ce qui précède **sans Unity** :

1. `npm test` — la référence elle-même ;
2. `npm run reference:unity` puis `git diff --exit-code` — la référence committée
   est à jour ;
3. `npm run verification:unity` — les 178 tests EditMode, compilés par .NET 8 en
   **C# 9** (la version d'Unity 6) et exécutés par un NUnit minimal
   (`Verification~/NUnitMinimal/`) ;
4. la compilation à blanc d'`Interface/` et d'`Editeur/` contre des signatures
   d'API Unity écrites à la main (`Verification~/CompilationUnity/`).

Le point 4 attrape une faute de frappe, pas une API mal comprise : **l'autorité
reste la compilation dans l'éditeur** (§5, étape 2).

---

## 3. Décisions de portage

**D1 — Les grands nombres : un port fidèle de `break_infinity.js`, bizarreries
comprises.** Ni `decimal` (28 chiffres, borné à 7,9·10²⁸ : les assises
profondes le dépassent), ni `double` (même problème à terme), ni le paquet
BreakInfinity.cs (une dépendance de plus, et un arrondi d'addition qui n'est pas
celui du web). La parité à 1e-9 exige la même arithmétique : même arrondi à
10¹⁴ dans l'addition, `mul(nombre)` qui ne convertit pas son argument, `toNumber`
qui recolle les quasi-entiers, et une division par zéro qui rend **zéro**.

**D2 — Le format de save est celui du web, à l'octet près.** Même clef
(`idlepond`), même enveloppe (`{ versionSave, contenu, dernierInstantMs }`),
mêmes noms de champs dans le même ordre, mêmes nombres (`String(nombre)` de
JavaScript), mêmes migrations 1→2→3→4. Conséquence : **un joueur du web migre
sa partie en copiant sa save** (§5), et une save Unity se recolle dans le web.

**D3 — Un JSON maison, pas `JsonUtility` ni Newtonsoft.** `JsonUtility` ne sait
ni les tables à clefs libres ni l'absence d'un champ, et vit dans UnityEngine ;
Newtonsoft écrit les nombres à sa façon. Trois cents lignes (`Adaptateurs/Json.cs`)
qui gardent l'ordre des clefs et écrivent les nombres comme le web.

**D4 — `TableOrdonnee<T>` pour chaque `Record<string, T>`.** L'ordre des clefs
d'un objet JavaScript est celui de leur insertion, et il entre dans la chaîne
de save que le test de déterminisme compare. Un `Dictionary` n'en garantit aucun.

**D5 — Des noms que C# impose.** Le réducteur s'appelle `Reducteur` et non
`Noyau` (une classe ne peut porter le nom de son espace de noms sans rendre ses
appels ambigus) ; `palierDeVoix(etat)` devient `Voix.PalierCourant(etat)` ; le
module `succes.ts` devient `SystemeDeSucces`, le type `Succes` gardant son nom.
Les identifiants de chaîne (`taux_base`, `pente`, `creusement`…) restent ceux du
web : ils entrent dans les saves et à l'écran.

**D6 — La boucle n'a pas de minuterie, et un trou d'horloge est une absence.**
L'hôte (`Jeu`) appelle `Boucle.Pas()` toutes les 100 ms ; le dt vient de
l'horloge. Le web a un défaut que le portage **corrige** : un onglet réveillé
après une nuit, ou une horloge avancée pendant la partie, crédite tout l'écart
d'un seul pas, **sans plafond** — ce que `horloge.ts` dit interdire. Ici, un
écart de plus de 5 s entre deux pas passe par le crédit hors ligne (plafonné,
compté dans les heures créditées, annoncé), et la mise en pause de
l'application arrête la boucle. Sans le rappel `surAbsence`, la boucle se
comporte exactement comme celle du web (c'est ce que teste la parité).

**D7 — La sauvegarde n'a pas lieu à chaque pas.** Zustand écrit dans le
localStorage toutes les 100 ms. Le magasin Unity écrit **après chaque acte** (on
ne perd jamais un achat), toutes les 5 s sinon, à la pause et à la fermeture.
Fichier atomique avec copie de secours sur les plateformes natives,
PlayerPrefs (IndexedDB) en WebGL.

**D8 — Une couche `Presentation` pure entre le noyau et l'écran.** Chaque
composant React est traduit en un *modèle* (mêmes conditions, mêmes chaînes) que
l'écran Unity ne fait que dessiner. C'est ce qui permet au test de canon
« aucun terme de couche ne sort dans un texte affiché » de porter sur les
chaînes réellement produites — et à la parité de comparer les écrans au web
sans éditeur.

**D9 — UI Toolkit, construit en code, stylé en USS.** Une seule page, des
listes qui se reconstruisent au gré de l'état : un UXML n'apporterait qu'un
second endroit où chercher un nom de classe. Les jetons de couleur sont les
oklch du web convertis en sRGB. Deux colonnes au-delà de 768 points, une en
deçà ; zone sûre respectée (encoches). Les polices du web (Newsreader,
monospace) ne sont pas livrées : voir `Interface/Resources/IdlePond/Polices/`.

**D10 — Aucune scène n'est nécessaire pour jouer.** `Amorce` crée le jeu dans
n'importe quelle scène ; `IdlePond ▸ Préparer la scène de jeu` crée la scène de
build. Les fichiers `.meta` ne sont pas committés par ce portage — Unity les
génère à la première ouverture, et **il faut alors les committer** (§5, étape 2),
sans quoi deux postes auraient des GUID différents.

**D11 — `tickDetaille` n'est plus récursif.** Il se rappelait après chaque
coupure ; une boucle fait exactement la même chose, sans risquer la pile sur une
longue absence découpée en nombreuses coupures.

**D12 — Les textes restent provisoires, et au même endroit.**
`Coeur/Donnees/TextesProvisoires.cs` est la copie conforme de
`textes-provisoires.ts`, vérifiée à l'octet par la parité. Les réécrire se fait
des deux côtés tant que le web reste la référence.

**D13 — Pas de scène visuelle, pas d'art.** Le web n'en a pas (« `scene/` —
Phaser, pas avant que l'assise I soit mesurée ») et l'écran est typographique
par décision. Les PNG de `public/` appartiennent à l'ancien projet (Étang des
Merveilles) et ne sont pas importés.

---

## 4. Ce que le portage ne change pas

- Aucune valeur de canon, aucune graine, aucune formule : la parité le prouve.
- Aucun ajout de contenu : la technique reste vide (v0.4), les assises II à VI
  restent sans nom, la voix n'altère pas encore l'écran.
- Les décisions ouvertes restent ouvertes (V11 tranché, `[P] P3`, `[P29]`, le
  renommage transverse éclosion → ponte du GDD).

---

## 5. Mode d'emploi

| Pour…                                  | Faire                                                               |
|----------------------------------------|---------------------------------------------------------------------|
| Ouvrir le projet                       | Unity Hub ▸ *Add* ▸ `unity/IdlePond` (Unity 6, 6000.0 LTS)          |
| Jouer                                  | Play, dans n'importe quelle scène                                   |
| Lancer les tests dans l'éditeur        | *Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All*               |
| Lancer les tests sans Unity            | `npm run verification:unity`                                        |
| Régénérer la référence de parité       | `npm run reference:unity`                                           |
| Simuler, calibrer                      | *IdlePond ▸ Simulateur*                                             |
| Migrer une partie du web               | copier la valeur `idlepond` du localStorage, puis *IdlePond ▸ Sauvegarde ▸ Importer depuis le presse-papiers* |
| Construire                             | *IdlePond ▸ Construire ▸ …*, ou `-executeMethod IdlePond.Editeur.Construction.ConstruireWebGL` |

---

## 6. Plan de bascule

1. **Portage et parité** — *fait.* 178 tests verts hors Unity, parité établie
   sur 15 cycles simulés.
2. **Recette dans l'éditeur.** Ouvrir le projet avec Unity 6, laisser
   l'import générer les `.meta` **et les committer** ; Test Runner EditMode :
   tout vert. C'est le premier moment où l'API Unity réelle est compilée : les
   écarts avec les signatures de `Verification~/CompilationUnity/` se corrigent
   ici, et les signatures avec.
3. **Recette visuelle.** Comparer l'écran à la version web (desktop, puis
   téléphone en portrait) ; installer les polices ; ajuster l'USS.
4. **Builds.** WebGL d'abord — il peut remplacer l'image Docker/nginx actuelle —,
   puis Android et iOS. Pour tester dans l'éditeur en CI, ajouter game-ci
   (`game-ci/unity-test-runner`) avec une licence Unity en secret.
5. **Double maintenance.** Tant que le web est la référence : toute modification
   du noyau, des données ou des textes est faite des deux côtés, `npm run
   reference:unity`, et la CI refuse une référence périmée.
6. **Bascule.** Unity devient la référence ; le générateur de parité est gelé
   avec la dernière référence, qui devient un test de non-régression ; les
   joueurs web migrent leur save (import, D2).
7. **Retrait du web.** `src/`, `tests/` et l'outillage Node sortent du dépôt ;
   la parité gelée reste.

### Risques et points à trancher

- **[P] Le coût de la double maintenance** croît avec chaque jalon : la bascule
  (étape 6) devrait précéder le jalon v0.3, qui touche à toutes les graines.
- **[P] Le renommage éclosion → ponte** (GDD, annexe A) change des identifiants
  et des clefs de save : à faire **avant** la bascule des deux côtés, ou
  **après** dans Unity seul, jamais entre les deux.
- **[P] La version d'Unity** est fixée à 6000.0 LTS dans
  `ProjectSettings/ProjectVersion.txt` ; toute montée de version relance
  l'étape 2.
- **La licence des polices** : Newsreader et JetBrains Mono sont sous OFL, à
  vérifier avant de les committer.
