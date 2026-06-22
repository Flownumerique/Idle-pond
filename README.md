# 🐟 Étang des Merveilles

Un jeu **idle / incremental** où l'on fait grandir un étang vivant : on achète et fait monter en niveau des poissons, on creuse vers des zones de plus en plus profondes, et on prestige pour repartir plus fort. Chaque espèce est illustrée par un sprite animé qui nage dans le bassin.

## Aperçu du jeu

- **42 poissons** répartis sur **11 zones de profondeur**, du Lac de Surface jusqu'à la Dimension Quantique.
- **Creusage** : dépenser du Mana pour atteindre de nouvelles zones et débloquer de nouvelles espèces.
- **3 monnaies** : Mana (revenu courant), Gemmes 💎 et Perles 🪸.
- **47 améliorations permanentes** réparties en 3 systèmes :
  - 🧬 **Corail de Prestige** (21 nœuds, payés en Gemmes)
  - 💎 **Marché des Perles** (6 améliorations, payées en Gemmes)
  - 🪸 **Améliorations de Prestige** (12 améliorations, payées en Perles)
- **Prestige** : réinitialiser sa partie pour gagner des Perles et des bonus définitifs.
- **Gains hors-ligne**, sauvegarde automatique, succès, défis quotidiens, événements narratifs et un bestiaire avec lore.

Le détail du contenu est documenté dans [`AMELIORATIONS.md`](AMELIORATIONS.md), [`BIOMES.md`](BIOMES.md), [`POISSONS.md`](POISSONS.md) et [`DOCUMENTATION.md`](DOCUMENTATION.md).

## Stack technique

- **React 19** + **TypeScript** + **Vite**
- **Phaser 3** pour le rendu et l'animation du bassin (spritesheets 8 frames)
- **Zustand** (avec persistance) comme source unique de l'état du jeu
- **break_infinity.js** pour les très grands nombres
- **Tailwind CSS** pour l'interface

## Architecture

L'état du jeu vit entièrement dans `src/store/useGameStore.ts` (Zustand). `App.tsx` n'est qu'une coquille : initialisation des managers, HUD, navigation et notifications.

```
main.tsx
└── App.tsx (shell)
      ├── GameLoopManager   – tick du jeu toutes les 100 ms
      ├── OfflineManager    – calcul des gains hors-ligne au lancement
      ├── PhaserContainer   – bassin animé (PondScene)
      ├── HUD haut / bas    – monnaies, revenu/s, creusage, boost
      └── Panneaux          – Boutique, Corail, Perles, Profil, Journal
```

- `src/data/` : données du jeu (poissons, succès, défis, recherche, améliorations, lore, événements).
- `src/game/scenes/PondScene.ts` : scène Phaser, chargement et animation des poissons.
- `src/managers/` : boucle de jeu et gains hors-ligne.
- `src/utils/` : calcul du revenu, des bonus, formatage des nombres.
- `public/` : sprites statiques · `public/anim/` : spritesheets d'animation (1024×128, 8 frames).

## Démarrer

```bash
npm install
npm run dev        # serveur de dev (http://localhost:5173)
npm run build      # build de production -> dist/
npm run preview    # prévisualiser le build
npm run lint       # ESLint
```

## Docker

```bash
docker compose up --build
```
