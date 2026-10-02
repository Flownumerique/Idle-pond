# IdlePond — la version web, archivée

React 19 · TypeScript · Vite · Phaser 3 · Zustand 5 · `break_infinity.js` · Tailwind 4.

Archivée le 2026-10-02, à la fin du portage Unity : le C# d'`Assets/IdlePond/` est
désormais la seule vérité de la mécanique. Rien ici ne sert plus à jouer ni à tester le
jeu ; c'est conservé pour relire d'où vient une formule.

Elle reste exécutable, depuis ce dossier :

```sh
npm install
npm test          # 196 sur 197 : plancher-de-cadence est parqué (it.fails) et passe
npm run dev
```

## Les références de parité

`tests/parite/generer-references.ts` et `tests/parite/generer-hors-ligne.ts` ont produit
les fichiers de `Assets/IdlePond/Tests/Reference/`, que `PariteTests` et
`PariteHorsLigneTests` rejouent en C#. Ces fichiers sont **figés** : on ne relance pas
les générateurs. Une mécanique changée volontairement côté C# régénère ses références
depuis le C#, pas d'ici.
