# Polices

L'écran web compose le texte en **Newsreader** (serif) et les chiffres en
monospace. Unity n'embarque aucune des deux : sans fichier ici, l'écran utilise
la police du thème par défaut, et reste lisible.

Pour retrouver la typographie du web, déposer dans ce dossier :

| Fichier            | Rôle                                        |
|--------------------|---------------------------------------------|
| `Texte.ttf`        | le texte courant — Newsreader, par exemple  |
| `Chiffres.ttf`     | les montants — JetBrains Mono, par exemple  |

Ils sont chargés par `Interface/Polices.cs` au démarrage, et appliqués aux
éléments marqués `texte` et `chiffre`. Vérifier la licence de chaque police
avant de l'ajouter au dépôt (Newsreader et JetBrains Mono sont sous OFL).
