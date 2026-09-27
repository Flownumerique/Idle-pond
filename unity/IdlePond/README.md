# IdlePond — Unity

Le portage complet d'IdlePond vers **Unity 6** (6000.0 LTS), en C# 9 et
UI Toolkit. Décisions, correspondances et plan de bascule :
[`docs/migration-unity.md`](../../docs/migration-unity.md).

> Le web reste la **référence** jusqu'à la bascule. Toute modification du
> noyau, des données ou des textes se fait des deux côtés, puis
> `npm run reference:unity` à la racine du dépôt.

## Ouvrir et jouer

1. Unity Hub ▸ *Add* ▸ ce dossier.
2. À la première ouverture, Unity génère les fichiers `.meta` : **les committer**.
3. *Play*, dans n'importe quelle scène — `Amorce` crée le jeu.

## Découpage

```
Assets/IdlePond/
├── Coeur/          PUR, sans UnityEngine. Grands nombres, noyau, données.
├── Adaptateurs/    Horloge, JSON, persistance, hors ligne, télémétrie, boucle.
├── Etat/           Le magasin : miroir de l'état, aucune logique métier.
├── Presentation/   Ce que l'écran affiche, en chaînes. Testable sans éditeur.
├── Simulateur/     Réutilise le Cœur tel quel.
├── Interface/      MonoBehaviour et UI Toolkit. Ne décide de rien.
├── Editeur/        Menu IdlePond : simulateur, sauvegardes, scène, builds.
└── Tests/EditMode/ 178 tests, dont la parité avec le web.
```

Seuls `Interface/` et `Editeur/` voient Unity ; tout le reste est déclaré
`noEngineReferences` et se vérifie aussi hors de l'éditeur.

## Tester

| Où                  | Comment                                                        |
|---------------------|----------------------------------------------------------------|
| Dans l'éditeur      | *Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All*          |
| Sans Unity (.NET 8) | `dotnet run -c Release --project Verification~` (ou `npm run verification:unity` à la racine) |
| Un seul groupe      | `dotnet run -c Release --project Verification~ -- Parite`      |

`Verification~/` (ignoré par Unity) compile les assemblages purs et les tests
en C# 9 et les exécute avec un NUnit minimal, sans NuGet.
`Verification~/CompilationUnity/` compile à blanc `Interface/` et `Editeur/`
contre des signatures d'API Unity écrites à la main : il attrape les fautes de
frappe, pas les API mal comprises — l'autorité reste l'éditeur.

## Le menu IdlePond

| Entrée                                  | Rôle                                                         |
|-----------------------------------------|--------------------------------------------------------------|
| Simulateur                              | Le simulateur du §12, relevé cycle par cycle, export CSV     |
| Sauvegarde ▸ Importer depuis le presse-papiers | Une save web (valeur `idlepond` du localStorage), migrations comprises |
| Sauvegarde ▸ Copier dans le presse-papiers | La save courante, recollable dans le web                  |
| Sauvegarde ▸ Ouvrir le dossier / Effacer | —                                                           |
| Préparer la scène de jeu                | Crée `Scenes/LaNoue.unity` et l'inscrit dans les Build Settings |
| Construire ▸ WebGL, Windows, macOS, Linux, Android, iOS | Aussi par `-executeMethod IdlePond.Editeur.Construction.Construire…` |
