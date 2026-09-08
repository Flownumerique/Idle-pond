# IdlePond

Jeu idle/incrémental narratif. Le joueur **descend** dans des assises
sous-marines de plus en plus profondes, **convainc** des espèces qui deviennent
ses générateurs, et accumule de la **Foi** émise par ses fidèles. Quand le
palier suivant coûte plus que ce que sa contenance peut porter, il retourne dans
l'œuf : c'est l'**éclosion**.

React 19 · TypeScript · Vite · Phaser 3 · Zustand 5 · `break_infinity.js` ·
Tailwind 4.

## État : jalon v0.2 amendé v1.1 — « la Noue est jouable »

La Noue, ses six paliers, le vairon, la loche et l'épinoche ; la boucle complète
débloquer → améliorer → éclore ; les succès avec le plancher de cadence du §8.4,
mesuré et non promis. Sans technique ni bénédictions : c'est le jalon v0.4.

Comptes rendus, mesures et décisions ouvertes :
[`docs/jalon-v0.1.md`](docs/jalon-v0.1.md) ·
[`docs/jalon-v0.2.md`](docs/jalon-v0.2.md) ·
[`docs/amendement-v1.1.md`](docs/amendement-v1.1.md) ·
[`docs/politique-du-simulateur.md`](docs/politique-du-simulateur.md).

> **Temps actif ≠ temps écoulé.** Le noyau ne mesure que l'écoulé — le
> calendaire du §5.4, cible ~600 h. L'actif est le sous-ensemble où le joueur
> est là, cible ~38 h, et seul le simulateur peut le connaître : il sait quand
> son joueur revient. Les jalons v0.1 et v0.2 les ont confondus ; c'est corrigé,
> et un test l'interdit.

> Les **noms** de la Noue sont du canon (amendement v1.1 §2.E). Les **phrases**
> restent provisoires et vivent toutes dans
> `src/donnees/textes-provisoires.ts` ; aucune n'entre dans un identifiant ni
> dans une sauvegarde. `[P] P3` reste ouvert pour les assises II à VI.

## Commandes

```sh
npm install
npm test          # 78 tests : architecture, déterminisme, équivalence de pas,
                  # seuils, contenance, persistance, canon, horloge, hors
                  # ligne, plancher de cadence, simulateur
npm run build     # tsc -b && vite build
npm run lint
npm run dev       # le jeu
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
   **production**. Aucun nœud, **aucun succès** ne franchit cette ligne
   (`tests/canon.test.ts`).
4. Cent individus d'une espèce valent **×16**, jamais ×1024 — `D = 2.31` est
   calibré contre cette lecture (`tests/seuils.test.ts`).
5. Le plafond ne monte **que** par séjour prolongé en mana dense : le ×47,1 par
   éclosion émerge de `A∞` et `τ₀`, il n'est écrit nulle part
   (`tests/contenance.test.ts`).
6. Aucun paramètre « à mesurer » n'est inventé : il est une constante nommée,
   commentée `// [P] graine`, dans `src/noyau/constantes.ts` — un seul endroit.

Le lexique s'applique **au code, aux identifiants et à l'UI**, pas seulement à
la prose : *assise*, *palier*, *banc*, *place*, *éclosion*, *densité*, *Foi*,
*bénédiction*, *technique*, *acclimatation*, *conviction*, *franchissement*.
Jamais « ponte », ni « prestige », ni « niveau », ni un nom générique de couche à
l'écran. Deux tests le vérifient — l'un sur le code de `src/`, l'autre sur les
chaînes réellement affichées.

## Découpage

```
src/
├── noyau/          PUR. tick(state, dt) -> state. Aucune dépendance à React,
│                   Phaser, DOM ou horloge.
├── donnees/        Contenu pur, sans logique.
├── adaptateurs/    Le monde impur vit ici, et nulle part ailleurs.
├── etat/           Zustand : miroir de l'état, aucune logique métier.
├── ui/             React + Tailwind.
├── scene/          Phaser — pas avant que l'assise I soit mesurée.
└── simulateur/     Réutilise noyau/ tel quel.
```

## Note sur les documents à la racine

`DOCUMENTATION.md`, `AMELIORATIONS.md`, `BIOMES.md` et `POISSONS.md` décrivent
l'ancien projet — gemmes, perles, zones, Corail de Prestige — dont le §9 du
prompt de lancement a supprimé tous les systèmes. Ils sont conservés en l'état
mais **ne font plus autorité**. À archiver ou réécrire avant la v0.2.
