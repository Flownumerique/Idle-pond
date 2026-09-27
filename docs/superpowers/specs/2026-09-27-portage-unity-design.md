# Le portage Unity — design

Écrit le 2026-09-27. Validé section par section en conversation le même jour.

## Intention

IdlePond quitte le web pour Unity 6 (`6000.6.3f1`). Il cible **le mobile (Android/iOS)
et le PC** avec une seule interface, qui s'adapte au portrait et au paysage. Unity
**remplace** la version web : à la fin du chantier, le C# est l'unique source de vérité
de la mécanique, et le code TypeScript est archivé.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| Cible | Mobile **et** PC, UI adaptative |
| La version web | Remplacée ; le C# devient la seule vérité |
| Le renommage transverse en cours | Porté directement au **lexique final** du Codex (Souffle, insufflation, renaissance). Il n'est pas fini en TypeScript |
| La sauvegarde | Repart en **version 1**, avec une chaîne de migrations vide mais présente. Les sauvegardes du navigateur ne sont pas reprises |
| La scène | **Portage fidèle** de la coupe au trait. L'habillage (DA, sprites, URP) est un chantier séparé |
| L'architecture | Approche A : noyau C# pur et testé, adaptateurs Unity minces, scènes générées par script |

### Ce qui est supposé, pas dit

- Les planches animées de `public/` ne sont pas reprises dans ce chantier. Elles décrivent
  une ancienne liste d'espèces.
- Le simulateur et le calibreur sont portés avec le noyau, parce que des tests de mécanique
  s'appuient dessus (`simulateur.test`, plancher de cadence). Ils vivent hors du build
  joueur.
- Aucune mécanique ne change. C'est un portage : les formules, les constantes et `D` par
  palier restent identiques au chiffre près. Le seul écart admis est le lexique.

### Critère de réussite

1. Tous les tests NUnit sont verts en batch, y compris le test de **parité** contre les
   parties de référence produites par le TypeScript (§3).
2. `Demarrage.unity` puis `Mare.unity` se jouent de bout en bout : creuser, convaincre,
   monter, grandir, insuffler, renaître, quitter, revenir et recevoir le crédit hors ligne.
3. L'interface est lisible en portrait 1080×1920 et en paysage 1920×1080.
4. Le dossier web est archivé et ne sert plus à rien pour jouer ni pour tester.

## §1. Arborescence et assemblies

```
Assets/IdlePond/
  Noyau/                 IdlePond.Noyau.asmdef  (noEngineReferences: true)
    Nombres/Decimal.cs     portage de break_infinity.js : seulement les opérations utilisées
    Nombres/Prng.cs        portage bit-exact du PRNG (arithmétique uint)
    Types.cs, Constantes.cs, Noyau.cs, Economie.cs, Densite.cs,
    Renaissance.cs (ex-eclosion.ts), Succes.cs, Technique.cs, Voix.cs
    Donnees/               Assises, Paliers, Especes, Echelles, Insufflations,
                           NoeudsTechnique, Succes/{Actes,Franchissements,Seuils},
                           Textes (ex-textes-provisoires.ts)
    Simulateur/            Simulateur.cs, Calibreur.cs
  Jeu/                   IdlePond.Jeu.asmdef → Noyau
    Persistance.cs         JSON, version 1, MIGRATIONS vide ; fichier dans persistentDataPath
    Horloge.cs, HorsLigne.cs, Telemetrie.cs
    Partie.cs              l'équivalent du magasin zustand : état, actions, événements C#
    Boucle.cs              MonoBehaviour : tick à PERIODE_DE_TICK_MS, pause et reprise
    Amorce.cs              MonoBehaviour de Demarrage.unity
    Scene/VueDeScene.cs    portage de vue.ts (pur, testé)
    Scene/SceneDeLaMare.cs le rendu. C'est la seule classe que la DA remplacera
    UI/                    un contrôleur par panneau, Mare.uxml, Theme.uss, Mare.uss
    Polices/               Newsreader et JetBrains Mono (OFL)
  Editeur/               IdlePond.Editeur.asmdef (Editor seulement)
    GenerateurDeScenes.cs  menu « IdlePond ▸ Générer les scènes » et point d'entrée batch
  Tests/                 IdlePond.Tests.asmdef (EditMode, NUnit) → Noyau, Jeu
    Reference/             parties de référence JSON (§3)
  TestsDeJeu/            IdlePond.TestsDeJeu.asmdef (PlayMode)
  Scenes/                Demarrage.unity, Mare.unity (générées, jamais retouchées à la main)
```

**Règles de traduction TypeScript → C#.**

- `readonly interface` devient un `record` ou une `sealed class` immuable. `{ ...etat, x }`
  devient une expression `with`. Unity 6 compile en C# 9 : un shim
  `System.Runtime.CompilerServices.IsExternalInit` est ajouté dans le Noyau.
- Les unions de chaînes fermées (`TermeDeProduction`, `TermeDeCout`, `TermeDeConfort`,
  `PorteeDInsufflation`) deviennent des `enum`. Les registres `TERMES_DE_*` deviennent des
  tableaux de ces enums.
- Les identifiants de données (`EspeceId`, `SuccesId`, `AssiseId`, `InsufflationId`,
  `NoeudTechniqueId`) restent des `string`, parce qu'ils entrent dans la sauvegarde.
- Les collections sont exposées en `IReadOnlyList` / `IReadOnlyDictionary`. Les copies
  modifiées passent par `ImmutableArray` / `ImmutableDictionary` si le paquet est
  disponible, sinon par copie de tableau.
- Les nombres : `number` devient `double`, sauf les compteurs entiers (`int`) et le PRNG
  (`uint`). `Decimal` (break_infinity) devient `IdlePond.Noyau.Nombres.Decimal`, un
  `readonly struct` (mantisse `double`, exposant `long`). Ce n'est pas `System.Decimal`, et
  le nom complet est utilisé là où les deux se côtoient.
- Le lexique suit le Codex §5, colonne « code » : `souffle`, `Insufflation`, `insuffler()`,
  `renaissance`, `renaitre()`, `nombreDeRenaissances`. Les trois identifiants de succès
  `franchissement-*-eclosion` restent intacts (Codex §7).
- Les commentaires restent en français et sont portés avec le code. Ils disent **pourquoi**
  une formule a cette forme, et c'est ce qui la rend auditable.

**Hors du dépôt Unity.** `test.unity` est supprimée. Le `.gitignore` reçoit les entrées
Unity (`/Library/`, `/Temp/`, `/Logs/`, `/UserSettings/`, `/obj/`, `*.csproj`, `*.slnx`,
`*.sln`) ; `Assets/`, `Packages/` et `ProjectSettings/` sont versionnés.

**Paquets ajoutés.** `com.unity.test-framework`. `com.unity.nuget.newtonsoft-json` pour la
persistance et les fichiers de référence : `JsonUtility` ne sait ni les dictionnaires ni
les records.

## §2. Flux de données

```
Demarrage.unity : Amorce → Persistance.Charger() → HorsLigne.Crediter() → Partie
                  → SceneManager.LoadScene("Mare")
Mare.unity      : Boucle.Update cumule dt ; à chaque PERIODE_DE_TICK_MS :
                  Partie.Avancer(dt) = Noyau.TickDetaille + EnregistrerIntervalleDeSucces
                  Partie émet EtatChange, SuccesDeclenches, RetourAffiche
                  → les contrôleurs UI lisent Partie.Etat et appellent
                    Partie.Creuser() / Convaincre(espece) / Monter(espece) /
                    Grandir() / Insuffler(id) / Renaitre()
                  → SceneDeLaMare reçoit VueDeScene.Depuis(etat) et ne redessine
                    que si la vue a changé
Sauvegarde      : toutes les 10 s, puis OnApplicationPause(true) et OnApplicationQuit
Retour          : OnApplicationPause(false) → HorsLigne.Crediter, la même fonction
                  qu'au démarrage
```

- `Partie` est un objet C# ordinaire, sans `MonoBehaviour`, et elle est testable sans
  Unity. Elle reçoit une `IHorloge` : `HorlogeSysteme` en jeu, `HorlogeFigee` en test.
  Un singleton `ServicesDePartie` (objet `DontDestroyOnLoad` créé par l'Amorce) la porte
  d'une scène à l'autre. Si l'on lance `Mare.unity` directement dans l'éditeur, elle crée
  sa partie elle-même, pour que la scène reste jouable seule.
- `Partie.Etat` est une référence vers un état immuable. Les actions remplacent la
  référence, et une action refusée par le noyau (pas assez de mana) rend le même état sans
  lever d'exception, comme aujourd'hui.
- **Recul d'horloge** : il est ignoré, jamais rattrapé à l'envers. Le hors-ligne est
  plafonné par `capHorsLigneCourantHeures`, et le compteur Entretien lit les heures
  **créditées**, jamais l'écoulé.
- **Sauvegarde illisible** : on démarre une nouvelle partie et le fichier fautif est
  renommé en `idlepond.corrompue-<horodatage>.json` au lieu d'être écrasé. L'écriture passe
  par un fichier temporaire puis un remplacement, pour qu'une coupure au milieu de
  l'écriture ne détruise pas la sauvegarde précédente.
- `dernierInstantMs` est écrit dans la sauvegarde, à côté de l'état, comme aujourd'hui.

## §3. Tests et critère de parité

**Port un pour un en NUnit (EditMode).** Chaque fichier de `tests/` devient une classe de
même nom en C# (`CanonTests`, `EquivalenceDePasTests`…), avec les mêmes titres en français
et les mêmes assertions. `outils.ts`, `joueur.ts` et `etat-de-travail.ts` deviennent des
aides partagées dans `Tests/Outils/`.

Quatre tests changent de nature :

| Test TypeScript | Devient |
|---|---|
| `architecture.test` | La pureté est garantie par l'asmdef. Il reste un balayage textuel des sources du Noyau contre `DateTime.Now`, `DateTime.UtcNow`, `System.Random`, `UnityEngine` et `Environment.TickCount` |
| `canon.test` (balayage lexical) | Il balaie les `.cs` de `Noyau/` et `Jeu/`, les `.uxml` et les chaînes de `Textes.cs` contre les mots morts et les mots interdits à l'écran du Codex §5, avec les exceptions figées du §7. Les autres assertions du canon (partition production/coût, registre de succès figé, `D` par palier) sont portées telles quelles |
| `persistance.test` | Aller-retour de la version 1, `Decimal` exact, sauvegarde corrompue mise de côté, champ inconnu ignoré, champ absent remis par défaut. Les tests des migrations v1 → v7 disparaissent avec elles |
| `scene.test` | Il devient `VueDeSceneTests`, sur la fonction pure. Rien ne teste le dessin |

S'y ajoutent `DecimalTests` (opérations, comparaisons, `ToString` / `Parse` en aller-retour,
valeurs extrêmes) et `PrngTests` (suites connues, produites par le TypeScript).

**La parité.** On renomme en portant, donc une parité mesurée remplace la relecture :

1. Un script `tests/parite/generer-references.ts`, lancé une fois par `npx vitest run` sur
   un fichier dédié, joue des **parties de référence** à graines fixes. Il utilise le
   joueur scripté de `tests/joueur.ts` et le simulateur, et couvre l'amorçage, trois
   renaissances, un hors-ligne de 8 h en un pas, les achats de croissance du héros et
   d'insufflations, et une partie du simulateur sur quinze cycles. À des instants fixés,
   il écrit des instantanés JSON (mana, Souffle, contenance, densités, niveaux, niveau du
   héros, insufflations, succès obtenus, état du PRNG, temps de jeu) sous les **noms du
   nouveau lexique**.
2. Ces fichiers sont versionnés dans `Assets/IdlePond/Tests/Reference/`. `PariteTests`
   rejoue les mêmes parties en C# et compare. Les entiers, les ensembles d'identifiants et
   l'état du PRNG doivent être **identiques**. Les `double` et `Decimal` doivent concorder
   à **1e-9 en relatif**, parce que les `Math.pow` de V8 et de .NET peuvent différer au
   dernier bit.
3. **Une parité verte est la condition pour archiver le web.** Ensuite, les références
   restent comme tests de non-régression. Une mécanique changée volontairement plus tard
   les régénère depuis le C#, par un outil éditeur.

**PlayMode.** `MareJouableTests` charge `Demarrage.unity` avec une horloge figée, avance de
60 s simulées, vérifie qu'un premier achat a eu lieu par l'API de `Partie`, que l'UI affiche
un Souffle et que la sauvegarde a été écrite.

**Exécution.** Tout passe en batch :
`Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <fichier>`,
puis la même commande en PlayMode. L'éditeur doit être fermé sur ce projet pendant ce temps.

## §4. Scènes et interface

**`GenerateurDeScenes`** : menu « IdlePond ▸ Générer les scènes » ou
`-executeMethod IdlePond.Editeur.GenerateurDeScenes.Generer`. Il est idempotent : relancé,
il réécrit les mêmes scènes, et on ne retouche jamais une scène à la main.

- `Demarrage.unity` : un objet `Amorce`, et rien d'autre.
- `Mare.unity` : une `Main Camera` orthographique à fond `eau-abysse`, la racine
  `SceneDeLaMare`, un objet `Boucle` et un `UIDocument` relié à `Mare.uxml` et à
  `PanelSettings.asset`.
- Il crée `PanelSettings.asset` en *Scale With Screen Size*, avec une résolution de
  référence de 1080×1920 et un appariement de 0,5 entre largeur et hauteur.
- Il inscrit `Demarrage` (index 0) puis `Mare` dans les Build Settings.

**La scène au trait.** C'est le portage fidèle de `SceneDeLaMare.ts` :

- une bande par palier ouvert, colorée par son assise, et la lumière qui baisse ;
- dans chaque bande qui porte une espèce, un banc dont l'effectif dessiné croît avec le
  logarithme du niveau, jusqu'à 14 poissons ;
- le héros dans la bande la plus basse, à l'échelle de son niveau, avec une marque par
  couche ;
- un voile qui monte en 0,9 s quand `eauTroublee`, sans aucun texte.

Le dessin utilise des sprites générés à l'exécution (un carré blanc et un disque blanc
teintés) et des `LineRenderer` pour les traits. Il n'y a aucune image importée. La palette
est portée depuis `palette.ts`, et les couleurs `oklch` de `index.css` sont converties une
fois en sRGB dans `Theme.uss` et dans `Palette.cs`. La caméra borne son `rect` au rectangle
de l'élément UI `#scene` et défile pour garder la bande la plus basse visible.

**L'interface (UI Toolkit).** Un `Mare.uxml` et un contrôleur par panneau, portés depuis
les composants React. Tous les textes viennent de `Textes.cs`.

| React | Unity | Écran (Codex) |
|---|---|---|
| `App.tsx` en-tête | `EnTete` | Souffle, retours dans l'œuf |
| `Retour.tsx` | `Retour` | le crédit hors ligne |
| `Scene.tsx` | `#scene` (zone réservée) | — |
| `Contenance.tsx` | `Contenance` | la contenance, la jauge |
| `Heros.tsx` | `Heros` | **Grandir**, « grandi *n* fois » |
| `Captation.tsx` | `Captation` | le détail auditable d'une espèce |
| `Mare.tsx` | `Mare` | **Creuser**, **Convaincre**, **Monter** |
| `Eclosion.tsx` | `Renaissance` | **la renaissance** |
| `Succes.tsx` | `Succes` | les succès |
| `Benedictions.tsx` | `Insufflations` | **Ce que tu insuffles**, **Insuffler** |
| `Annonces.tsx` | `Annonces` | une ligne qui apparaît et s'efface |
| `format.ts` | `Format.cs` | la mise en forme des montants |

**Adaptation.** Le contrôleur racine écoute `GeometryChangedEvent`. Il pose `.paysage` si la
largeur est au moins 1,2 fois la hauteur, sinon `.portrait`.

- **Paysage** : la colonne principale (scène, contenance, héros, captation, mare,
  renaissance) à gauche, et une colonne de 20 rem à droite pour les succès et les
  insufflations.
- **Portrait** : la scène occupe les 40 % du haut, puis une colonne unique qui défile.
  Succès et insufflations passent derrière deux onglets en bas de l'écran.
- Les boutons font au moins 48 px de haut, pour être touchés au doigt.

**Polices.** Newsreader pour le texte et JetBrains Mono pour les chiffres, en OFL,
importées en `FontAsset` UI Toolkit. Si l'import échoue, on retombe sur la police par défaut
d'Unity sans bloquer le reste.

## §5. Découpage du chantier

L'ordre compte : chaque étape est vérifiable avant la suivante.

| # | Étape | Fini quand |
|---|---|---|
| 0 | Mise en place : `.gitignore`, paquets, asmdefs, suppression de `test.unity`, commande batch de tests | un test NUnit vide passe en batch |
| 1 | `Decimal`, `Prng`, `Constantes`, `Types` | `DecimalTests`, `PrngTests` et les tests de constantes du canon sont verts |
| 2 | Noyau et données : économie, densité, technique, voix, succès, renaissance, tick | tous les tests de mécanique portés sont verts |
| 3 | Simulateur, calibreur, puis les références TypeScript et `PariteTests` | la parité est verte |
| 4 | Jeu : `Partie`, `Persistance`, `Horloge`, `HorsLigne`, `Telemetrie` | les tests de persistance et de hors-ligne sont verts |
| 5 | `VueDeScene`, `SceneDeLaMare`, `GenerateurDeScenes` | les scènes sont générées en batch, `VueDeSceneTests` est vert |
| 6 | UI Toolkit : les panneaux, l'adaptation, les polices | le test PlayMode est vert |
| 7 | Revue visuelle en portrait et en paysage | captures relues (MCP Unity) ou retour de l'utilisateur |
| 8 | Archivage du web : `src/`, `tests/`, `public/`, la configuration Vite et Docker vont dans `archive/web/`, et README et DOCUMENTATION sont réécrits | le dépôt ne contient plus rien d'exécutable côté web |

## Hors du périmètre

- La direction artistique : sprites, planches animées, particules, URP, lumière.
- Toute évolution de mécanique ou d'équilibrage.
- Les systèmes que seul le GDD décrit (temples, alliés, portails, miracles).
- Les builds signés pour les boutiques, les icônes et les écrans de lancement. Les cibles
  Android et iOS doivent **compiler** ; la publication est un chantier à part.
- La reprise des sauvegardes du navigateur.

## Risques

- **Écart numérique entre V8 et .NET** sur `Math.Pow`, `Math.Log` et `Math.Exp`.
  Parade : la tolérance relative de la parité et un PRNG en entiers. Si un écart dépasse la
  tolérance, on l'examine, on ne l'élargit pas.
- **Sémantique de `break_infinity.js`** (normalisation, arrondis de `toString`) : portée
  depuis la source de la bibliothèque, pas de mémoire, et verrouillée par des valeurs
  produites en JavaScript.
- **Batch et éditeur ouvert** : Unity refuse d'ouvrir en batch un projet déjà ouvert. Chaque
  étape qui compile ou teste demande que l'éditeur soit fermé, ou passe par un MCP Unity
  s'il est branché.
- **Rendu non vérifiable en batch** : l'étape 7 a besoin d'yeux, ceux du MCP ou ceux de
  l'utilisateur.
