# IdlePond — la roadmap

**Tenue à jour le** : 2026-10-02
**Objet** : ce qui reste à faire, dans l'ordre, et pourquoi cet ordre.
**Ce document churne.** Le Codex ne bouge presque jamais ; celui-ci bouge à chaque
chantier fini. Ne jamais y écrire de définition — elle irait dans `CODEX.md`.

---

## Où on en est

**Le jeu est dans Unity** ; la version web est archivée dans `archive/web/`. La Noue est
jouable de bout en bout : creuser, convaincre, monter, grandir, insuffler, renaître. Sa
scène est en **pixel art** (URP 2D, lumières 2D) avec des sprites **provisoires** générés
par script ; le cahier des charges du vrai dessin est `docs/da/gabarits.md`. Les chiffres
du tableau ci-dessous et la section *Tests* datent du 2026-09-18, du temps du web : ils
n'ont pas été remesurés depuis le portage.

| | Livré | Sur le papier |
|---|---|---|
| Assises | 1 (la Noue) | 6 |
| Paliers | 6 | 62 |
| Espèces | 3 | 21 |
| Axes permanents | insufflations, densité, succès | + technique |

**Tests** : 196 verts sur 197. Le rouge est `plancher-de-cadence.test.ts`, parqué au canon
et hors périmètre de tout chantier listé ici (voir *Gelé*).

**Ce que le README dit encore** : « Sans technique ». C'est le chantier n°3 qui lève ça.

---

## Les chantiers, dans l'ordre

### 0. La direction artistique, ce qui reste

Le socle est posé (scène en pixel art, gabarits, sprites provisoires). Restent, en tête
des chantiers :

- **L'habillage hybride de l'UI** : le décor et l'interface partagent une même eau, il
  faut l'habiller dans le même registre.
- **Le vrai dessin** : remplacer les provisoires fichier par fichier, au nom exact, selon
  `docs/da/gabarits.md`, sans toucher au code.
- **Les assises II à VI et leurs milieux** : lave et électricité comprises (GDD §15.2,
  amendement du 2026-10-02), chacune laissant sa marque sur le héros.

---

### 1. Le renommage transverse — `souffle` / `insuffler` / `renaissance`

**Ce que c'est.** Appliquer le lexique du Codex §5 au code : identifiants, types, termes
de formule, textes affichés, et migration de sauvegarde **v7 → v8**.

**Pourquoi il passe devant tout le reste.** Deux raisons, et la seconde est la vraie :

- Chaque texte écrit avant lui devra être réécrit après lui. Écrire les 18 espèces
  restantes dans l'ancien vocabulaire, c'est signer pour les réécrire deux fois.
- Le code est encore petit. Ce ne sera **jamais** moins cher qu'aujourd'hui, et le dépôt
  s'était déjà noté la dette : `src/noyau/types.ts` porte depuis le 2026-09-08 la mention
  « *en attendant le renommage transverse qui touche les identifiants et les
  sauvegardes* ».

**Périmètre mesuré.** 18 fichiers portent `foi`, 13 portent `benediction`, 3 portent
`ponte`. Plus une migration et le balayage lexical de `canon.test.ts` à retourner (les
anciens mots deviennent des mots morts).

**Exception figée** : les trois identifiants de succès en `eclosion` ne bougent pas
(Codex §7).

**Taille** : moyenne. Beaucoup de fichiers, peu de jugement — sauf la migration.
**Ce que ça lève** : la contradiction entre ce que le jeu dit et ce que le code nomme.

---

### 2. La politique de dépense du Souffle

**Ce que c'est.** `insufflerAuMieux` (ex-`benirAuMieux`) achète au moins cher d'abord, tant
qu'il peut payer. Résultat mesuré : **684 rangs achetés au cycle 5**, une insufflation
ciblée atteignant le rang ≈31 — soit un multiplicateur d'environ ×5,6·10⁵ sur une seule
espèce.

**Pourquoi c'est ici et pas plus tard.** La tâche B4 a prouvé par la mesure que le ratio de
coût **ne peut pas** brider ça (4→684, 6→511, 8→435, 10→391, 12→357 : décroissant, jamais
convergent) sans rendre la première insufflation impayable au premier cycle. Le vrai levier
est la politique elle-même. Tant qu'il n'est pas actionné, **toute mesure d'équilibrage
faite en aval est suspecte** — ce qui inclut les six couples (A, B) du chantier suivant.

La conclusion et la table sont déjà consignées dans le commentaire de
`RATIO_COUT_DE_BENEDICTION`, dans `constantes.ts`.

**Taille** : petite. Une politique de simulateur, pas une mécanique de noyau.
**Ce que ça lève** : la confiance dans les chiffres d'équilibrage.

---

### 3. L'arbre de technique

**Ce que c'est.** Le troisième axe de persistance, et le seul qui manque. Le noyau v1.0 §6
et §7 en posent les règles ; les **six couples (A, B)** qui décident du rythme d'ouverture
d'une branche ne sont pas résolus.

**L'échafaudage est déjà là.** `src/donnees/noeuds-technique.ts` existe et est **vide
délibérément** : le registre a été créé tôt pour que les tests de canon qui le parcourent —
frontière technique/insufflation, source unique d'une capacité, budget de verbes — soient
en place **avant** le contenu. Les trente nœuds du §7.3 restent à transcrire, avec la table
`verbe → cycle d'ouverture visé` en entrée du calibreur (§7.2). Le chantier est donc moins
risqué qu'un départ à blanc : les gardes existent, c'est le contenu qui manque.

**Une dette de conception à signaler.** Le §12 voulait technique et insufflations livrées
« *en même temps, puis recalibrées ensemble* ». On a livré les insufflations seules. La
recalibration commune n'a donc jamais eu lieu — raison de plus pour ne pas laisser cet
écart s'agrandir.

**La règle dure, déjà testée** : la technique baisse les **coûts** et automatise. Elle ne
monte jamais la production — c'est le métier de l'insufflation. `canon.test.ts` verrouille
les deux sens, et la contradiction interne du noyau sur le nœud « Réputation » est déjà
tranchée dans `PRESEANCE.md` (à réécrire en réduction de coût).

**Pourquoi maintenant.** C'est l'axe qui porte la partie **après le cycle 13**, quand la
profondeur est épuisée et que rien ne monte plus. L'amendement v1.2 le dit :
« *passé la treizième, la partie tient sur d'AUTRES axes* ». Aujourd'hui, un seul des deux
existe.

**Dépend de** : le chantier 2, pour que les couples (A, B) soient résolus contre des
chiffres fiables.

**Taille** : grande. Mécanique neuve, arbre de données, écran, et six paramètres à
résoudre par la mesure.
**Ce que ça lève** : « Sans technique » dans le README.

---

### 4. Le contenu des assises II à VI

**Ce que c'est.** 56 paliers et 18 espèces à écrire, sur cinq assises qui n'existent que
comme nombres.

**Ce qui est déjà prêt — davantage qu'il n'y paraît.** Les six assises **existent déjà
dans la donnée**, avec leur géométrie complète : `6 / 12 / 12 / 12 / 12 / 8 = 62 paliers`,
et cette répartition n'est pas arbitraire — c'est la seule à 62 paliers où une espèce
s'ancre exactement sur `3 × rang`, condition pour que son débit suive `D` palier par
palier. C'est cette économie-là que le simulateur mesure depuis le début. La scène, les six
palettes et les six marques du corps sont construites pour les 62 paliers (tâches C2–C3).

Ce chantier est donc du **contenu**, pas de la structure : nommer, écrire, peupler.

**Ce qui manque vraiment** :

- **Les noms.** `IDENTIFIANTS_D_ASSISE` ne contient que `noue` ; les cinq autres tombent
  sur un repli neutre (`assise-2`…). Il faut descendre la charte phonétique du Codex §6.
  `[P] P3` reste ouvert.
- **La typologie du mana.** Chaque assise a son type propre, et les identifiants sont
  provisoires (`type-mana-2`…) parce que `mana-typologie.md` (Tier 1) **n'est pas dans le
  dépôt**. À retrouver ou à réécrire avant de peupler l'assise II.
- **18 espèces** et leurs succès.

**La règle d'engagement, qui impose le découpage** : « *aucune assise n'est produite avant
que la précédente ait été mesurée. On coupe au milieu, jamais à la fin* » (§12). Ce
chantier se fait donc **assise par assise**, chacune mesurée avant la suivante — jamais en
un bloc.

`PALIERS_LIVRES` (dérivé de la première assise) est le verrou qui décide de ce que le jeu
livre ; les 62 paliers, eux, existent déjà pour le simulateur.

**Dépend de** : le chantier 1 (ne pas écrire 18 espèces dans le vocabulaire mort) et,
pour l'équilibrage, du 3.

**Taille** : la plus grande du lot, mais fractionnable assise par assise.

---

### 5. Non ordonnés — la réserve du GDD

Conçus, jamais construits. Le GDD les porte ; aucun n'est bloquant.

| | Ce que c'est | Note |
|---|---|---|
| **Temples et alliés** | Des divinités de seconde zone qui accordent quelque chose contre une **taxe permanente** sur le revenu de Souffle | Le frein est déjà conçu : chaque allié redirige une part définitivement |
| **Portails** | Relier les bassins voisins | Acte IV s'y appuie narrativement |
| **Découplage des chapitres** | Séparer le récit de la profondeur | Repoussé depuis la phase 4 |
| **Miracles** | Ce que le Souffle achetait dans l'ancienne conception | **Gelé** `[P26]`. Coexisteront avec les insufflations le jour où ils seront tranchés |
| **Défiscalisation** | — | Le moins spécifié des cinq |

---

## La dette courante

Cinq broutilles laissées par la revue finale du 2026-09-18. Aucune ne mérite un chantier :
**elles montent dans celui qui touche le fichier concerné.**

| Où | Quoi |
|---|---|
| `economie.ts` | `ECHELLE_DE_PRODUCTION` n'apparaît dans aucune ligne de `detailDeCaptation` — le test d'invariant ne passe que parce qu'elle vaut 1 aujourd'hui |
| `canon.test.ts` | `src/scene/` est exempté du balayage des mots morts **en entier**, pour un seul mot (`bancs`) |
| `canon.test.ts` | Les textes d'insufflation ne sont pas dans la garde lexicale de l'écran |
| `economie.ts` | `detailDeCaptation` construit sa liste en trois morceaux — fossile d'un correctif |
| `SceneDeLaMare.ts` | `NaN` si le canevas fait exactement 144 px de large |

---

## Gelé

**`plancher-de-cadence.test.ts`, le test `PARQUÉ`.** Il échoue, c'est voulu, et il ne
ferme qu'à une « tâche 13 » qui résoudra les nombres décidant de l'atteignabilité des
succès — hors de tout ce qui est listé ici. Son propre commentaire démontre par la mesure
qu'aucune valeur de `DEBIT_HEROS` ne peut le satisfaire, et que jouer *mieux* aggrave le
trou au lieu de le combler. **Ne pas le « réparer ».**

---

## Comment cette roadmap se tient à jour

Un chantier fini est **retiré**, pas coché — son récit vit dans l'historique git et dans
`amendement-v1.1.md`. Ce qu'on apprend en le faisant (une mesure, un dead end, un lever
qui ne marche pas) va dans le commentaire de la constante qu'il contraint, jamais
seulement ici : un résultat négatif enterré dans un document de planning se refait.
