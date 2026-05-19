# Tâches — Refonte du design d'Idle Pond

## État actuel
- Design glassmorphique sombre (slate/dark + blur)
- Couleurs hardcodées en classes Tailwind inline
- Pas de système de design (pas de token CSS, pas de thème)
- Emojis utilisés comme langage visuel principal
- Pas de typographie custom (police système)
- Titre de la page : "temp-app" (placeholder)

---

## 1. Fondations du système de design

- [ ] Définir la palette de couleurs principale (remplacer le slate générique)
- [ ] Créer des tokens CSS dans `index.css` (`--color-*`, `--spacing-*`, `--radius-*`)
- [ ] Choisir et intégrer une ou deux polices custom (Google Fonts ou locale)
- [ ] Définir une hiérarchie typographique (h1 → h4, body, label, caption)
- [ ] Configurer Tailwind v4 pour utiliser les tokens CSS comme couleurs sémantiques
- [ ] Changer le titre de la page (`<title>` dans `index.html`) : "temp-app" → "Étang des Merveilles"
- [ ] Choisir une direction artistique générale (ex : aquatique organique, pixel art, minimaliste...)

---

## 2. Layout global

- [ ] Repenser la barre d'activité gauche (icônes custom au lieu d'emojis, largeur, style)
- [ ] Repenser le panneau latéral secondaire (320px fixe → responsive ? drawer animé ?)
- [ ] Revoir le panneau d'infos en haut à droite (Stats) — intégration dans un header fixe ?
- [ ] Décider si le canvas Phaser reste plein écran en arrière-plan ou devient une zone dédiée
- [ ] Gérer l'adaptation mobile (actuellement non optimisé pour petit écran)

---

## 3. Composants à redesigner

### Boutique (`Shop.tsx`)
- [ ] Redesigner les cartes de poissons (layout, info affichée, état acheté/verrouillé)
- [ ] Revoir la distinction poissons normaux / légendaires
- [ ] Améliorer l'affichage des jalons (10/25/50/100)

### Améliorations (`Ameliorations.tsx`)
- [ ] Redesigner les cartes d'améliorations par zone
- [ ] Repenser la barre/indicateur de profondeur
- [ ] Améliorer la modale de confirmation de prestige

### Corail de Prestige (`Research.tsx`)
- [ ] Redesigner l'arbre de recherche (nœuds, connexions, branches par couleur)
- [ ] Améliorer la lisibilité des prérequis et de l'état débloqué/verrouillé

### Marché des Perles (`PearlMarket.tsx`) & Améliorations Prestige (`PrestigeUpgrades.tsx`)
- [ ] Harmoniser les deux panels (même style de carte)
- [ ] Clarifier visuellement les chaînes de prérequis

### Succès (`Achievements.tsx`)
- [ ] Redesigner les cartes de succès (3 états : réclamable, réclamé, verrouillé)
- [ ] Ajouter une barre de progression globale des succès

### Défis quotidiens (`Challenges.tsx`)
- [ ] Repenser l'affichage du compte à rebours de reset
- [ ] Redesigner les barres de progression des défis

### Boost (`BoostOverlay.tsx`)
- [ ] Repenser l'affichage du timer et de l'activation du boost ×2

### Stats (`Stats.tsx`)
- [ ] Intégrer dans le layout général plutôt que panneau flottant indépendant

### Guide (`Guide.tsx`) & Lore (`Lore.tsx`)
- [ ] Améliorer la lisibilité (typographie, espacement, tags colorés)
- [ ] Harmoniser le style des deux onglets Journal / Bestiaire

### Notifications (`UnlockNotification.tsx`, `EventNotification.tsx`, `WelcomeBackNotification.tsx`)
- [ ] Unifier les trois composants dans un système de toast cohérent
- [ ] Positionner, animer et styler les toasts de manière uniforme

---

## 4. Assets visuels

- [ ] Remplacer les emojis par des icônes SVG custom (navigation, ressources, actions)
- [ ] Définir un style cohérent pour les sprites de poissons (pixel art ? illustration ? 3D ?)
- [ ] Créer ou remplacer le favicon (`favicon.svg`)
- [ ] Revoir les sprites existants (`Poisson-clown.png`, `Grenouille cristalline.png`) pour coller au nouveau style
- [ ] Décider si `icons.svg` (sprite sheet) est maintenu ou remplacé

---

## 5. Animations & micro-interactions

- [ ] Définir une politique d'animation (durées, easing — ex : 150ms ease-out pour hover)
- [ ] Ajouter des transitions sur l'ouverture/fermeture du panneau latéral
- [ ] Ajouter des feedbacks visuels sur les achats (flash, bounce, shake si fonds insuffisants)
- [ ] Animer les compteurs de ressources (increment animé)
- [ ] Revoir les animations du canvas Phaser (poissons, bulles, profondeur)

---

## 6. Accessibilité & qualité

- [ ] Vérifier les ratios de contraste (WCAG AA minimum)
- [ ] Ajouter des `aria-label` sur les boutons icônes
- [ ] Tester le rendu sur différentes tailles d'écran
- [ ] Tester le rendu en mode clair si applicable

---

## Ordre suggéré

1. **Tokens CSS + choix de direction artistique** (bloque tout le reste)
2. **Layout global + navigation**
3. **Composants cœur de gameplay** (Boutique, Améliorations)
4. **Composants de progression** (Corail, Marchés, Succès)
5. **Composants secondaires** (Guide, Lore, Notifications)
6. **Assets visuels** (icônes, sprites)
7. **Animations & polish**
8. **Accessibilité & QA**
