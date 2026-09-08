# IdlePond — jalon v0.1 : « le noyau tourne »

Tier 2. Ce document rend compte du jalon v0.1 tel que le §12 du *prompt de
lancement v1.0* le définit, et de rien d'autre. Il ne crée aucun canon.

---

## 1. Ce qui a été livré

### La passe de suppression (§9)

Faite en une seule passe et commitée seule, avant la première ligne neuve.
Ont disparu sans remplacement : `gemmes`, `perles`, `research.ts`,
`pearlUpgrades.ts`, `prestigeUpgrades.ts`, `PearlMarket.tsx`, `Research.tsx`,
`PrestigeUpgrades.tsx`, `BoostOverlay.tsx`, `challenges.ts`, `Challenges.tsx`,
`Shop.tsx` et la logique d'achat par exemplaire, plus toute monétisation.

Ont suivi, parce que le §9 les classe en réécriture ou parce qu'ils n'existaient
qu'en dépendance des précédents : l'ancien modèle de données (`fishTypes`,
`zones`, `runUpgrades`), l'ancienne structure de succès, `PondScene`, le store,
les managers et les utils.

Ce que le §9 déclare « gardé » est **porté**, pas conservé en l'état : le tick à
100 ms, le plafond hors ligne et la sérialisation `Decimal` reviennent dans
`adaptateurs/`. Le pool d'événements narratifs, le journal et le bestiaire sont
du contenu ; ils reviennent en v0.2, pool réécrit, et restent récupérables à
partir du commit `663b1b0`.

### Le découpage du dépôt (§5.3)

```
src/
├── noyau/          constantes, types, noyau, economie, population,
│                   densite, eclosion, technique, benedictions, succes
├── donnees/        assises, paliers, especes, echelles,
│                   noeuds-technique, benedictions, succes/
├── adaptateurs/    horloge, persistance, telemetrie
└── simulateur/     simulateur, calibreur
```

`etat/`, `ui/` et `scene/` sont créés vides : ils appartiennent au jalon v0.2.

Un fichier n'était pas au plan : `donnees/echelles.ts`, qui tabule `g^p`, `D^p`
et `1.15^n`. Ce sont des constantes dérivées de constantes, pas un cache d'état —
rien n'y dépend d'une partie, rien ne s'y met à jour. Sans elles, quinze cycles
de simulation passaient de 8 s à 17 s, et un simulateur lent est un simulateur
qu'on ne lance pas.

### Les tests

| Test | Ce qu'il garantit |
|---|---|
| `architecture` | `noyau/` n'importe rien hors de `noyau/` et `donnees/` ; ni horloge, ni hasard, ni DOM ; `Date.now` n'existe que dans `adaptateurs/horloge.ts` |
| `determinisme` | même graine + même séquence de `dt` ⇒ même état, bit pour bit ; le chemin continu ne tire jamais le PRNG |
| `equivalence-de-pas` | 480 × 60 s = 1 × 8 h ; 6000 × 100 ms = 1 × 600 s ; le plafonnement par la contenance compose aussi |
| `persistance` | aller-retour `Decimal` exact ; `versionSave` ; chaîne de migrations, et refus bruyant d'une version inconnue |
| `canon` | frontière technique/bénédiction (§4.3), source unique d'une capacité et budget de verbes (§7.5), dérivations du §13, lexique du §3 appliqué au code |
| `horloge` | plafond hors ligne, recul d'horloge ignoré, heures créditées |
| `simulateur` | 15 cycles ; densité jamais décroissante ; acquis permanents jamais reperdus |

`npm test` : 42 tests, tous verts. `tsc -b`, `eslint .` et `vite build` : propres.

### Critère d'acceptation

> « Accepté quand : le simulateur tourne 15 cycles sans jouer, et le test
> d'équivalence de pas passe. »

Les deux passent.

---

## 2. Les décisions de modélisation, et pourquoi

Le contrat fixe des ratios et des invariants, pas un modèle. Quatre points
demandaient une décision ; aucune n'invente une valeur du §13.

### L'effectif converge, il ne se compte pas

`production = effectif × taux_base × rendement_acclimatation` (§6.1). Le niveau
d'un banc fixe son **effectif cible** ; l'effectif réel y converge par une
exponentielle, à une vitesse qui dépend de la densité du palier — c'est là que
« le gain de densité retourne dans la vitesse de repeuplement » (§6.5) atterrit.

Le choix de l'exponentielle n'est pas esthétique : sa primitive est fermée, donc
`∫ effectif dt` se calcule exactement pour n'importe quel `dt`, et les deux
propriétés du §5.2 — un seul pas pour 8 h, et le même code pour le jeu et le
simulateur — tombent ensemble. C'est ce que le test d'équivalence de pas
vérifie.

### La contenance est un état permanent, pas une quantité dérivée

§6.5 la range dans les conservés ; §6.4 veut qu'elle finisse par ne plus porter
le coût du palier suivant. Une contenance dérivée de la production courante
grandirait à chaque niveau acheté et le blocage doux n'arriverait **jamais** —
et, accessoirement, elle rendrait le plafonnement du stock non composable, donc
casserait l'équivalence de pas. Elle est donc portée en état permanent et
progresse à l'éclosion, indexée sur la production de pic comme la densité.

**La loi de croissance de la contenance n'est fixée par aucun document.** Elle
est une graine `[P]`, à trancher en v0.3.

Le blocage reste **doux** parce que le coût de creusement part 7,5 fois au-dessus
du premier niveau du palier le plus profond : quand la descente ferme, il reste
une quinzaine de niveaux à monter avant que le stock ne ferme aussi celle-là.
C'est exactement « le joueur peut continuer à monter des niveaux et à accumuler
de la Foi ; il ne peut simplement plus descendre ».

### Un banc = une espèce sur un palier

Le §6.3 avertit que `D` se mesure sur la production **totale d'un palier**,
jamais par espèce. La structure porte donc une **liste** de bancs par palier, et
le taux du palier se partage entre eux. Le placement provisoire en pose un seul
par palier ; l'attribution définitive est autoriale (§4.2) et n'a rien à
réécrire pour en poser plusieurs.

Le mot *banc* n'est pas inventé : il vient du §7.3 (« Le banc suit », « Veille
des bancs », « Compter les bancs »).

### Le hasard ne touche pas le chemin continu

Le PRNG est à graine et vit dans l'état, comme le §5.1 l'exige — mais le tick ne
le consomme jamais. Un tirage par tick ferait diverger 480 pas de 60 s d'un pas
de 8 h, et emporterait le hors ligne et le simulateur avec lui. Un test le
vérifie explicitement. Le hasard n'aura droit de cité que sur des événements
discrets.

---

## 3. Ce que le simulateur mesure, et ce qu'il réfute

Quinze cycles, politique par défaut (check-in 4 h, patience 4 h, achat du moins
cher disponible).

| Cycle | Durée active | Paliers | Redescente | Pic mana/s | Foi |
|---:|---:|---:|---:|---:|---:|
| 1 | 4,4 h | 3 | 0 % | 2,2e2 | 14 |
| 2 | 5,5 h | 10 | 72 % | 1,7e5 | 4,1e2 |
| 3 | 7,0 h | 18 | 59 % | 1,2e8 | 1,1e4 |
| 5 | 9,8 h | 32 | 49 % | 1,7e13 | 4,2e6 |
| 8 | 15,8 h | 52 | 47 % | 2,6e20 | 1,6e10 |
| 10 | 21,5 h | 62 | 52 % | 2,4e24 | 1,6e12 |
| 12 | 89,5 h | 62 | 15 % | 1,5e25 | 3,9e12 |
| 15 | 104,0 h | 62 | 13 % | 1,6e25 | 4,0e12 |

**Total actif : 552 h.**

Ce sont des mesures sur graines, pas un calibrage. Elles disent quatre choses,
et les quatre sont des entrées pour v0.3 — aucune ne se corrige au §13.

1. **~7 paliers par cycle au lieu de 4,4.** Les 62 paliers sont épuisés au cycle
   10 sur 15. Les cinq derniers cycles n'ont plus de profondeur à prendre et
   dégénèrent en montée de niveaux. Le levier est la loi de croissance de la
   contenance, qui est justement le `[P]` ouvert ci-dessus.

2. **La redescente occupe ~50 % de chaque cycle.** C'est le risque n° 1 du §15,
   « la redescente devient le jeu », et il se déclenche dès les graines. La
   métrique était en place avant le contenu, ce qui est précisément la raison
   pour laquelle elle peut le dire aujourd'hui plutôt qu'en v0.5.

3. **Les durées de cycle ne sont pas plates en temps actif** (4,4 h → 104 h). Le
   §13.4 prédit des durées plates *sous jeu optimal* ; la politique simulée est
   gloutonne et bornée par une patience de 4 h, donc l'écart ne réfute pas le
   §13.4 — il dit que la politique du simulateur doit être affinée avant de
   pouvoir le tester. À faire en v0.3, avant toute conclusion.

4. **552 h actives contre ~38 h visées.** Attendu : technique et bénédictions
   arrivent en v0.4, et le §7.2 rappelle que c'est le calendrier des verbes qui
   règle la durée réelle, pas les nœuds chiffres. Ce chiffre n'aura de sens
   qu'après v0.4.

---

## 4. Décisions ouvertes ajoutées par ce jalon

Elles s'ajoutent aux `[P]` du §14, aucune ne bloquait la v0.1.

| # | Question | Où |
|---|---|---|
| V1 | **Les seuils de jalon se cumulent-ils ?** Le §6.2 apparie 10/25/50/100 à ×2/×4/×8/×16 sans dire si le ×16 est le multiplicateur **total** au niveau 100 ou le quatrième facteur d'un produit qui vaut ×1024. Le code porte la première lecture, la plus littérale, derrière une constante `LECTURE_DES_SEUILS_DE_JALON`. Un basculement est une ligne. | `constantes.ts` |
| V2 | **`θ` n'est défini nulle part.** Le §13.2 dérive l'exposant de densité de `θ / α`, mais aucun document disponible ne définit `θ`. La dérivation est donc impossible ; le noyau applique la graine du §13.3 telle quelle. | `constantes.ts` |
| V3 | **Loi de croissance de la contenance.** Aucun document ne la fixe, et c'est elle qui décide du nombre de paliers par cycle. | `eclosion.ts` |
| V4 | **`g/D` = 1,0383, pas 1,039.** La dérivation exacte de `1.18^(1/4,4)` donne 1,03833 ; le §6.3 cite 1,039. `D` vaut 2,3115 dans les deux cas, soit le 2,31 du document. Le §13.2 classant `g/D` en « recalculer, ne pas saisir », c'est la dérivation qui fait foi et le 1,039 est un arrondi — signalé, pas corrigé. | `canon.test.ts` |
| V5 | **Les graines d'échelle économique.** Le contrat fixe les ratios (`g`, `D`, ×1,15) mais aucune origine : coût du premier creusement, taux de base au palier 0, coût du premier niveau, mana porté à la sortie de l'œuf. Toutes nommées et commentées `[P]`. | `constantes.ts` |
| V6 | **L'acquisition de l'acclimatation est du contenu v0.5.** Le Tier 0 impose qu'elle soit conservée à vie et jamais repayée — c'est fait, elle est en état permanent. Son mécanisme d'acquisition n'est pas inventé : d'ici v0.5 le rendement est plein sur tous les types, ce qui permet au simulateur de traverser les six assises sans qu'aucune règle soit devinée. | `constantes.ts` |

---

## 5. Contradictions relevées avec le canon

Le §16 demande de signaler toute contradiction entre le prompt de lancement et
un fichier de canon. Deux relevés, aucun arbitré ici.

1. **Les fichiers de canon ne sont pas dans le dépôt.** Ni `tier-0-immuable.md`,
   ni `especes-cadre.md`, ni `mana-typologie.md`, ni `idlepond-gdd-v2_4.md`. Le
   travail s'est appuyé sur le seul prompt de lancement, qui prime sur le GDD
   partout où ils divergent.

   > **Correction du 2026-09-08 — la préséance est inversée.** Le GDD v2.4 est
   > entré au dépôt (`docs/GDD.md`) et devient le document directif : c'est lui
   > qui prime, partout. La phrase ci-dessus reste vraie de son époque — elle
   > décrit la seule règle tenable quand le canon était absent — mais elle ne
   > décrit plus la règle en vigueur, et une session qui la lirait sans cette
   > note rétablirait exactement l'ordre qu'on vient de défaire. Les `§x.y` des
   > commentaires de `src/` renvoient toujours au prompt de lancement, dont la
   > numérotation ne correspond pas à celle du GDD ; les renvois au GDD sont
   > désormais écrits « GDD §x.y ». Deux conséquences directes : les types de mana
   portent des identifiants provisoires (`type-mana-1`…), et rien de ce qui
   relève de `especes-cadre.md` n'a été supposé.

2. **Les documents de l'ancien projet sont périmés à la racine.**
   `DOCUMENTATION.md`, `AMELIORATIONS.md`, `BIOMES.md` et `POISSONS.md`
   décrivent gemmes, perles, zones et Corail de Prestige — tous supprimés par le
   §9. Ils n'ont pas été touchés : ce sont des documents d'auteur, pas du code.
   Ils devraient être archivés ou réécrits avant la v0.2, sinon une session
   future les lira comme du canon.

---

## 6. Ce qui n'a pas été fait, et pourquoi

Rien de ce qui suit n'est un oubli : le §12 les place plus loin, et le §16
interdit de faire du contenu avant que le jalon précédent soit accepté.

- **Aucune UI, aucun Phaser.** `src/main.ts` est un banc d'essai qui imprime la
  sortie du simulateur ; il n'affiche aucun nom de lieu ni d'espèce, donc jamais
  de nom générique de couche.
- **Aucun nom propre.** Le `[P] P3` du §14 déclare les conventions phonétiques
  bloquantes pour tout nom d'assise, d'espèce et de bâtiment. Les identifiants
  sont des identifiants de code.
- **Registres de technique, de bénédictions et de succès vides.** Les gardes qui
  les parcourent sont en place — frontière du §4.3, source unique d'une
  capacité, budget de verbes, typage par famille et visibilité. Le §8 note que
  ces structures « ne se rétrofitent pas » ; un test de frontière ajouté après
  le contenu ne fait que constater les dégâts.
- **Aucun couple `(A, B)`, aucune table de seuils.** Le §13.3 ne leur donne
  aucune graine. Tant qu'un couple manque, la branche rend zéro point : un arbre
  muet, jamais un arbre deviné.

---

## 7. Pour la v0.2

Le §12 la définit : assise I complète (3 espèces, ses paliers, la mare nommée),
la boucle débloquer → améliorer → éclore sans technique ni bénédictions, densité
persistante, repeuplement, et les succès de l'assise I **avec le plancher de
cadence du §8.4**.

Deux préalables :

- **`[P] P3` d'abord.** Les conventions phonétiques bloquent le nom de la mare,
  les trois espèces et tout ce qui s'affichera.
- **Une contrainte technique à porter en v0.4 :** les achats automatiques
  (`achat_auto`, `achat_auto_max`, « Le peuple continue ») changent les niveaux
  *pendant* un intervalle. Le noyau les traite aujourd'hui comme constants sur
  le pas, ce qui est vrai tant que les achats sont des actes du joueur entre
  deux ticks. Un automatisme naïf ferait tomber l'équivalence de pas et, avec
  elle, le hors ligne. Il faudra une forme fermée, pas une boucle.
