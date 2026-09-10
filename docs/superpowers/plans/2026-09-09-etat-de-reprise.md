# Noyau v1.0 — état de reprise

**Écrit le 2026-09-09.** À lire avant de reprendre l'exécution de
`docs/superpowers/plans/2026-09-08-noyau-v1-phases-0-3.md`.

Ce fichier existe parce que le journal d'exécution vit dans
`.superpowers/sdd/2026-09-08-noyau-v1-phases-0-3/progress.md`, qui est ignoré par
git et détruit à la fin du plan. Ce qui suit est ce qui doit survivre.

- **Spec** : `docs/superpowers/specs/2026-09-08-roadmap-noyau-v1-design.md`
- **Plan** : `docs/superpowers/plans/2026-09-08-noyau-v1-phases-0-3.md`
- **Préséance des documents** : `docs/PRESEANCE.md`
- **Branche** : `roadmap-noyau-v1`, partie de `e167eed`

---

## 1. Où en est le travail

**Sept tâches closes, revue propre à chaque fois.** La huitième était en boucle
de correction au moment d'écrire.

| # | Tâche | Commit | État |
|---|---|---|---|
| 1 | Verser les documents, écrire la préséance | `58123a6` | close |
| 2 | Retirer la divergence non choisie | `75ba401`, `de14a40` | close |
| 3 | Retirer la maturation | `fc12151` | close |
| 4 | Retirer le second canal de revenu | `60e9120` | close |
| 5 | **Le banc devient une espèce à niveau** | `42f0bb0` | close |
| 6 | `f = 1`, plus de tarif de redescente | `af27f8c` | close |
| 7 | Migration de sauvegarde 4 → 5 | `23dd32c` | close |
| 8 | Verrouiller la purge par balayage | `9889b11` | **1 Important en cours de correction** |

État mesuré au dernier point vert : **108 tests sur 14 fichiers**, `npx tsc -b`,
`npx eslint .` et `npm run build` propres, sans exclusion.

**La phase 1 — la purge — est donc faite.** La population simulée, la
maturation, le second canal, la capacité de palier et le tarif réduit de
redescente ont quitté le code. Une espèce est un générateur avec un niveau.

### Ce qui reste ouvert sur la tâche 8

Un point **Important** de relecture, en cours de correction quand ce fichier a
été écrit — à vérifier avant de continuer :

> `sansChaines` (`tests/outils.ts`) efface le contenu des interpolations
> `${…}` d'un gabarit, qui sont du **code** et non de la prose. Un des douze
> motifs morts revenant comme *usage* à l'intérieur d'une interpolation
> échapperait au balayage. Correctif demandé : ne blanchir que les segments de
> texte statique, préserver les `${…}` — **avec un test qui le prouve.**

Si `git log` montre un commit `fix(test): sansChaines préserve les
interpolations`, c'est fait. Sinon, c'est le premier travail à reprendre.

---

## 2. Ce qui reste à faire

### Phase 2 — le cœur v1.0

**Tâche 9 — l'amorçage.** `DEBIT_HEROS`, pour que la partie démarre : sans débit
propre au héros, production nulle → mana nul → aucune espèce jamais débloquée.
**Son périmètre a diminué** : la tâche 5 a déjà remonté `multiplicateurDePalier`
et `multiplicateurDeProfondeur` (voir R8). Il reste `DEBIT_HEROS`,
`densiteTotale` et les tests.

**Tâche 10 — l'éclosion.** Reset complet, gain de densité indexé sur la
production de pointe. **C'est la tâche la plus chargée de ce qui reste**, parce
qu'elle porte aussi le ruling R9 : le découplage de l'acquis de séjour. Voir §4.

**Tâche 11 — la partie headless.** Une partie sans interface atteint la
deuxième éclosion. C'est le portail de sortie de la phase 2.

### Phase 3 — mesurer

**Tâche 12 — rebrancher le simulateur.** Politique d'achat par gain marginal
analytique, deux temps distincts (actif et écoulé). Porte aussi le ruling R5
(`NOMBRE_D_ECLOSIONS_VISE` : 15 → 45) et la dette de la tâche 2 (`aDivergeSeul`,
code mort).

**Tâche 13 — résoudre les nombres.** `θ`, l'échelle, puis les six couples
`(A, B)` de l'arbre technique. Porte le ruling R4 : le test du plan compare des
points à un *nombre* de nœuds (5) au lieu de leur coût total (167), et une de ses
assertions ne peut pas échouer — à corriger en écrivant la tâche.

**Tâche 14 — `RESULTATS.md` v2.** La référence chiffrée qui gouvernera les
phases 4 et 5.

### Puis

Relecture finale de toute la branche, sur le modèle le plus capable, en lui
donnant la liste des mineurs différés du §5. Ensuite seulement, la question de
l'intégration dans `main`.

Les **phases 4 et 5** du spec — découpler le récit des éclosions, l'arbre
technique et l'assise I jouable — ne sont pas planifiées, et c'est volontaire :
leur contenu dépend des nombres de la tâche 14.

---

## 3. Les décisions prises, et ce qu'elles coûtent si elles sont fausses

Dix rulings, dans l'ordre. Ce sont des décisions prises sans arbitrage humain :
elles sont toutes révocables, et c'est ici qu'on les retrouve pour les révoquer.

**R1 — les champs morts sortent des types en mémoire.** `partsMures`,
`acclimatations`, `bancs`, `secondesEnSaturation` quittent `EtatCycle` et
`EtatPermanent` ; ils ne survivent que dans le **format de sauvegarde**, connu du
seul `src/adaptateurs/persistance.ts`, qui désérialise par spread générique.
*Pourquoi* : le canon (« une migration ne supprime jamais un champ ») parle du
schéma de save, pas du type runtime ; les garder aurait fait échouer les
balayages qui sont le critère de sortie de la phase 1.
*Coût si faux* : une save v4 perdrait une donnée qu'on voudrait relire ;
récupérable, la save reste sur disque.

**R2 — la liste de fichiers de la tâche 5 était incomplète.** Ajout de
`src/donnees/succes/*.ts` et `textes-provisoires.ts`, qui citent les termes de
coût renommés. *Coût si faux* : nul, extension mécanique.

**R3 — `epinoche` reste au canon**, déplacée en tête de l'assise II, nommée
explicitement. La répartition passe à 2/4/4/4/4/3 et la sort de l'assise I, mais
l'amendement v1.1 §2.E la nomme et la justifie. *Coût si faux* : un nom
d'espèce à déplacer, sans effet mécanique.

**R4 — le test de l'arbre technique de la tâche 13 est faux dans le plan.** Il
compare des points à `COUTS_DE_NOEUD.length` (5) au lieu de leur somme (167), et
`expect(TOTAL).toBeGreaterThan(0)` n'affirme rien. *Coût si faux* : nul, le
test devient plus strict.

**R5 — `NOMBRE_D_ECLOSIONS_VISE` passe de 15 à 45**, en tâche 12. La décision de
durée du spec découple les chapitres des éclosions. *Coût si faux* : un nombre,
que la mesure de la tâche 14 réfuterait aussitôt.

**R6 — `docs/` sort du périmètre d'ESLint.** En versant le harnais de mesure
dans git, la tâche 1 l'a rendu analysable et il échouait sur deux
`no-unused-vars`. Ces quatre `.ts` sont déclarés « non intégrés au jeu », gelés
comme reflet exact de ce qui a produit `RESULTATS.md`. *Coût si faux* : une
faute réelle dans le harnais passerait inaperçue — il ne s'exécute qu'à la main.

**R7 — la tâche 3 a laissé le canal acclimaté dans un état transitoire** (débit
constant, sans part mûre) plutôt que de le supprimer, sa suppression étant le
travail de la tâche 4. *Coût si faux* : si la tâche 4 avait été abandonnée, le
jeu gardait un second canal plat non calibré. Elle ne l'a pas été.

**R8 — `multiplicateurDeProfondeur` remonte de la tâche 9 vers la tâche 5.**
Sans lui, la production croît de 26 % par palier contre 140 % pour le coût, et le
simulateur ne converge plus : la tâche 5 aurait validé un jeu mort. *Coût si
faux* : le facteur est écrit dans la forme exacte prévue ; il se corrige à un
endroit.

**R9 — l'acquis de séjour est découplé de la densité, en tâche 10.** Voir §4,
c'est le point le plus important de ce fichier.

**R10 — `reduction_technique` sort de `TermeDeCout`**, et les trois succès qui la
ciblaient visent désormais `cout_creuser`. Elle ne s'appliquait qu'à la formule
d'aménagement, supprimée : la laisser aurait rendu trois effets chiffrés
silencieusement inertes, et la relecture a confirmé qu'**aucun test ne l'aurait
détecté**. *Coût si faux* : trois succès rendent le creusement un peu moins
cher qu'avant sur les paliers neufs ; réfutable au calibrage.

---

## 4. Le vrai sujet : l'acquis de séjour est dégénéré

C'est la découverte de la journée, et elle vaut plus que le compte de tâches.

La mesure de la tâche 5 a montré que le temps caractéristique de l'acquis de
séjour est **divisé par la densité**, laquelle croît sans borne. Mesuré :

| | Durée |
|---|---|
| Cycle 1 | 4 h |
| Cycles 2 à 6 | **0,09 h** |

L'acquis sature en quarante secondes. C'est exactement la dégénérescence que V11
avait corrigée le 2026-09-08 sur le repeuplement, sur l'autre canal — et elle
était masquée jusqu'ici par le temps de convergence de la population, qui vient
de disparaître.

**Pourquoi ça compte.** La loi de contenance décidée pour la tâche 10 est
`contenance × CONTENANCE_PAR_ECLOSION ^ (acquisDeSejour / ACQUIS_MAX)`. Avec un
acquis qui sature toujours, le rapport vaut 1 partout : la loi dégénère en
forfait plat, et le pilier Tier 0 — « le plafond ne monte que par séjour prolongé
en mana dense » — redevient décoratif. On aurait alors le forfait `g⁴` du spike,
en plus compliqué.

**Ce que la tâche 10 doit faire** : découpler l'acquis de séjour de la densité,
comme V11 l'a fait pour le repeuplement, et poser un test qui l'attrape. Deux
dettes s'y rattachent :

- la mesure de durée de cycle du simulateur est **fausse** tant que ce n'est pas
  corrigé, et aucun test ne l'attrape aujourd'hui ;
- `CONTENANCE_INITIALE` est dérivée de `COUT_CREUSER_AU_PALIER_1 × g^4.4`, où 4.4
  est `PALIERS_PAR_CYCLE_VISE`, alors que la tâche 10 introduit
  `PALIERS_GAGNES_PAR_ECLOSION = 4`. Réconcilier les deux, ou dire pourquoi ils
  diffèrent.

---

## 5. Mineurs différés, à trier à la relecture finale

Aucun ne bloque. Le premier et le deuxième sont les seuls à porter une vraie
question de conception.

1. **`palier_au_complet` dilue les succès secrets.** Deux paliers sur trois
   n'ouvrent plus d'espèce, et sont donc déclarés « au complet » dès leur
   ouverture, sans aucun achat. Quatre des cinq succès secrets
   `seuil-palier-sature-*` de la Noue deviennent gratuits et instantanés. Le
   compromis est assumé en commentaire — le registre figé interdit de supprimer
   un identifiant — mais **aucun test ne le protège d'une régression**.
2. **`bancs_convaincus` → `especes_debloquees` n'est pas un pur renommage.** Une
   espèce pouvait porter plusieurs bancs ; `acte-deux-bancs` pouvait donc tomber
   sur deux bancs de la même espèce. Il exige maintenant deux espèces
   distinctes, ce qui le retarde dans la Noue. Sans effet mesuré sur le plancher
   de cadence.
3. **`aDivergeSeul()`** (`src/simulateur/simulateur.ts`) est du code mort depuis
   la tâche 2, avec un commentaire devenu faux. La tâche 12 réécrit ce fichier.
4. **`coutDeDescente`** (`src/noyau/economie.ts`) duplique le corps de
   `facteurDeCout` en deux `.mul()` chaînés au lieu de l'appeler, contrairement à
   `coutDeDeblocage` et `coutDeNiveau` dans le même fichier. C'est le plan qui
   prescrivait la forme longue.
5. **Tests de migration** : `especes.toEqual({})` est masqué par le repli dans
   deux des trois tests ; seul celui qui appelle `MIGRATIONS[4]` directement
   discrimine une migration no-op. La couverture est correcte à trois, la
   dépendance croisée n'est pas commentée.
6. **En-tête de `src/ui/Mare.tsx`** encore en « bancs » alors que le corps est
   réécrit en « espèces ». Cosmétique.
7. **Accès par clé de chaîne** (`etat['bancs']`) hors du balayage de la tâche 8.
   Improbable dans le style du dépôt.

---

## 6. Ce qu'il faut savoir avant de reprendre

**Défauts trouvés dans le plan lui-même.** Trois, dont deux sérieux. Le balayage
de préflight en avait attrapé cinq autres avant l'exécution. Il faut lire ce plan
en se méfiant de ses tests :

- le test de migration de la tâche 7 écrivait `version: 4` au lieu de
  `versionSave: 4` : il serait passé au vert **sans jamais exécuter la
  migration** ;
- le helper `fichiersDuNoyau()` que le plan invoque partout **n'existe pas** ;
  le vrai s'appelle `fichiersTs(racine)` ;
- le test de l'arbre technique de la tâche 13 est faux (voir R4).

**Un piège de méthode, découvert en tâche 4.** Une suppression qui paraît
complète dans `src/noyau/economie.ts` peut laisser une **seconde implémentation
indépendante dans `src/noyau/noyau.ts`** — c'est là que vit le vrai moteur, pas
dans les fonctions de lecture. La tâche 4 y a trouvé un canal entier qui
créditait réellement du mana. Vérifier les deux fichiers, toujours.

**Interdire les worktrees aux implémenteurs.** L'un d'eux en a créé un de son
propre chef puis l'a supprimé avec `--force` ; une jonction Windows vers
`node_modules` a fait vider le vrai `node_modules`. Réparé par `npm ci`, aucun
fichier suivi touché — mais à ne pas revivre.

**Le plafond de session a coupé deux relectures en cours.** Sans dégât : les
artefacts sont sur disque, une relecture se relance à l'identique.

**Reprendre proprement** : relire le journal
`.superpowers/sdd/2026-09-08-noyau-v1-phases-0-3/progress.md` s'il existe encore,
sinon ce fichier plus `git log`. Les briefs de toutes les tâches sont extraits
dans le même répertoire ; ils se régénèrent avec
`scripts/task-brief PLAN_FILE N` du skill `subagent-driven-development`.
