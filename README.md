# IdlePond

Jeu idle/incrémental narratif. Le joueur **descend** dans des assises
sous-marines de plus en plus profondes, **convainc** des espèces qui deviennent
ses générateurs, et accumule de la **Foi** émise par ses fidèles. Quand le
palier suivant coûte plus que ce que sa contenance peut porter, il retourne dans
l'œuf : c'est l'**éclosion**.

React 19 · TypeScript · Vite · Phaser 3 · Zustand 5 · `break_infinity.js` ·
Tailwind 4.

## État : jalon v0.1 — « le noyau tourne »

Le noyau, ses trois tests et le versionnage de save. Pas d'UI, pas de Phaser,
pas de contenu — c'est ce que le jalon prescrit.

Le compte rendu du jalon, ses mesures et ses décisions ouvertes :
[`docs/jalon-v0.1.md`](docs/jalon-v0.1.md).

## Commandes

```sh
npm install
npm test          # 42 tests : architecture, déterminisme, équivalence de pas,
                  # persistance, canon, horloge, simulateur
npm run build     # tsc -b && vite build
npm run lint
npm run dev       # banc d'essai : la sortie du simulateur, pas le jeu
```

## Le contrat

Le document de référence est le **prompt de lancement v1.0** (Tier 2). Il n'est
pas dans le dépôt ; il est à relire avant chaque session de build et il remplace
toute mémoire de session. Quatre règles s'y vérifient en premier, et un test
existe pour chacune :

1. `noyau/` est pur — aucun `Date.now`, aucun `Math.random`, aucun import React
   ou Phaser (`tests/architecture.test.ts`).
2. Toute mécanique du cœur se calcule en **un seul pas** pour `dt = 8 h`
   (`tests/equivalence-de-pas.test.ts`).
3. La technique baisse les **coûts** et automatise ; la bénédiction monte la
   **production**. Aucun nœud ne franchit cette ligne (`tests/canon.test.ts`).
4. Aucun paramètre « à mesurer » n'est inventé : il est une constante nommée,
   commentée `// [P] graine`, dans `src/noyau/constantes.ts` — un seul endroit.

Le lexique s'applique **au code, aux identifiants et à l'UI**, pas seulement à
la prose : *assise*, *palier*, *banc*, *éclosion*, *densité*, *Foi*,
*bénédiction*, *technique*, *acclimatation*, *conviction*. Jamais « ponte », ni
« prestige », ni un nom générique de couche à l'écran. Un test le vérifie sur le
code de `src/`.

## Découpage

```
src/
├── noyau/          PUR. tick(state, dt) -> state. Aucune dépendance à React,
│                   Phaser, DOM ou horloge.
├── donnees/        Contenu pur, sans logique.
├── adaptateurs/    Le monde impur vit ici, et nulle part ailleurs.
├── etat/           Zustand — v0.2
├── ui/             React + Tailwind — v0.2
├── scene/          Phaser — v0.2
└── simulateur/     Réutilise noyau/ tel quel.
```

## Note sur les documents à la racine

`DOCUMENTATION.md`, `AMELIORATIONS.md`, `BIOMES.md` et `POISSONS.md` décrivent
l'ancien projet — gemmes, perles, zones, Corail de Prestige — dont le §9 du
prompt de lancement a supprimé tous les systèmes. Ils sont conservés en l'état
mais **ne font plus autorité**. À archiver ou réécrire avant la v0.2.
