# L'assise II — le Gour — design

Écrit le 2026-10-07. Validé en conversation le même jour.

## Intention

Donner une suite à la Noue : la deuxième assise, « les galeries noyées » du GDD §6.2,
écrite au canon et **livrée jouable**.

### Ce que l'utilisateur a tranché

| Question | Réponse |
|---|---|
| Le nom du lieu | **le Gour** (`gour`) |
| Les trois espèces après l'épinoche | **le chabot, la lamproie, l'ombre** |
| La portée | **Livrer jouable** — les douze paliers s'ouvrent dans le jeu |

**Dérogation consignée.** Le GDD §6.2 pose la règle d'engagement : « aucune assise n'est
produite avant que la précédente ait été mesurée ». La Noue n'a été mesurée qu'au
simulateur (RESULTATS.md), pas en jeu réel. L'utilisateur choisit de livrer le Gour
quand même ; la mesure de la Noue reste à faire, et celle du Gour avec.

## §1. La grille obligatoire (GDD §6.2)

| Ligne | Le Gour |
|---|---|
| **Nom** | le Gour, /guʁ/. Hydronyme réel : la vasque que l'eau creuse dans la roche des grottes, et d'où elle déborde vers la suivante. Monosyllabe comme la Noue, voyelle plus sourde — la charte phonétique descend (Codex §6). C'est le nom qu'un habitant lui donnerait (noyau v1.0 §414) |
| **Type de mana** | `[P]` — `mana-typologie.md` (Tier 1) n'est toujours pas au dépôt. L'identifiant reste `type-mana-2`. Rien ne s'affiche à l'écran |
| **Lumière** | Plus de jour : ce qui filtre encore par les pertes. Une courbe froide qui part sous la dernière de la Noue (0,42) et descend jusqu'à 0,20 au douzième palier |
| **Contrainte physique** | **Le courant.** Fiction et image seulement : le noyau v1.0 ne lui donne aucun effet de jeu. Il se lit dans le fond (des stries d'eau qui file) et dans la marque du héros |
| **Faune propre** | l'épinoche (palier 6, déjà au canon), le chabot (9), la lamproie (12), l'ombre (15). Trois façons de tenir dans le courant : sous les pierres, accrochée à la roche, en le remontant |
| **Faune mutée** | Les divergences sont du contenu v0.5 : pas ici |
| ~~Acclimatation~~ | Mot mort (Codex §5) : la ligne tombe avec le noyau v1.0. Ce qui en reste à l'écran est la **marque** |

**Paliers : douze**, comme le noyau le fixe (`6 / 12 / 12 / 12 / 12 / 8`), et non les
huit de la proposition du GDD §6.3 — la préséance va au noyau pour la mécanique.

## §2. La marque sur le héros

Après les branchies de la Noue, **les membranes** (GDD §15.1 : « branchies, membranes,
luminescence, minéralisation, épaississement ») : une crête membraneuse sur le dos, qui
s'étend à chaque stade — de quoi tenir dans l'eau qui file. Ancrage `Dos`, sans teinte
ni lumière, comme les branchies.

## §3. L'art provisoire

Généré par script, comme celui de la Noue, et remplaçable fichier par fichier :

- **Le fond** : une eau de galerie froide, bleu-gris, avec des stries horizontales — le
  courant. Pas de berge, pas de racines, pas de rayons du jour.
- **La roche** : du calcaire plus froid et plus sombre que celui de la Noue.
- **Les espèces** : une couleur par espèce (chabot brun marbré, épinoche vert argent,
  lamproie olive sombre, ombre gris violacé).
- **Le voile** de l'eau trouble : un limon gris-vert.

## §4. Les textes

Noms : « le Gour », « le chabot », « la lamproie », « l'ombre ». Succès, au registre
(les identifiants entrent, ils n'en sortiront plus) :

- les seuils de niveau du chabot, de la lamproie et de l'ombre (10 / 25 / 50 / 100), par
  le gabarit existant ;
- `franchissement-fond-du-gour` : le Gour ouvert jusqu'au fond.

Le rapport de `franchissement-fond-de-la-mare` (« Il n'y a plus de roche à ouvrir ici »)
devient faux avec le Gour en dessous : la phrase est réécrite, l'identifiant ne bouge pas.

## §5. Ce qui change dans le code

- `PALIERS_LIVRES` : 6 → 18 (la Noue et le Gour).
- **La sauvegarde** : la limite de contenu y est écrite. Au chargement, elle est relevée
  à ce que la version livre — sans quoi une partie existante resterait bloquée à la Noue.
- L'interface ne suppose plus une seule assise : le tiroir Espèces montre chaque lieu
  atteint puis le suivant verrouillé ; le Journal liste le lieu où l'on est.
- Les ateliers prennent toutes les espèces livrées.

## Critère de réussite

1. Tests verts, dont : `PALIERS_LIVRES` vaut 18 ; une sauvegarde à limite 6 se charge à
   18 ; chaque succès du Gour a ses textes ; la marque du Gour a ses quatre stades.
2. Dans l'atelier, on creuse sous la Noue : le Gour s'ouvre, ses espèces arrivent, le
   tiroir Espèces montre les deux lieux, le héros porte la crête.

## §6. La parité (décision du même jour)

Les références de parité venaient du TypeScript archivé. Le Gour s'en écarte volontairement
(identifiants, limite livrée, 13 succès dont les remises changent l'économie simulée).
L'utilisateur a choisi de **basculer la référence en C#** : `GenerateurDeReferences` rejoue
les scénarios des tests et réécrit `donnees.json`, `parties.json`, `hors-ligne.json`. La
comparaison avant / après n'a montré que les écarts dus au Gour ; ils sont consignés dans
`docs/RESULTATS.md`, § Journal des références de parité.
