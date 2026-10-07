# IdlePond — la roadmap

**Tenue à jour le** : 2026-10-07
**Objet** : ce qui reste à faire, dans l'ordre, et pourquoi cet ordre.
**Ce document churne.** Le Codex ne bouge presque jamais ; celui-ci bouge à chaque
chantier fini. Ne jamais y écrire de définition — elle irait dans `CODEX.md`.

---

## Où on en est

**Le jeu est dans Unity** ; la version web est archivée dans `archive/web/`. **La Noue et
le Gour** sont jouables de bout en bout : creuser, convaincre, monter, grandir, insuffler,
renaître. La scène est en **pixel art** (URP 2D, lumières 2D), sprites **provisoires**
générés par script (cahier des charges : `docs/da/gabarits.md`). L'écran est celui du
téléphone en portrait — barre, mare au centre, dock, tiroirs — et tient en paysage.

| | Livré | Sur le papier |
|---|---|---|
| Assises | 2 (la Noue, le Gour) | 6 |
| Paliers | 18 | 62 |
| Espèces | 6 | 21 |
| Succès | 54 | — |
| Axes permanents | insufflations, densité, succès | + technique |

**Tests** : 340 EditMode et 16 PlayMode, tous verts (2026-10-07). Les références de
parité sont produites par le C# depuis le Gour (voir `RESULTATS.md`, journal des
références).

**Ce que le README dit encore** : « Sans technique ». C'est le chantier n°3 qui lève ça.

**Outillage** : MCP for Unity (l'éditeur se pilote depuis Claude Code) et les
**ateliers**, des bacs à sable à sauvegarde redirigée — voir le README.

---

## Les chantiers, dans l'ordre

### 0. La direction artistique, ce qui reste

Le socle est posé (scène en pixel art, gabarits, sprites provisoires), l'écran mobile
aussi (spec `2026-10-07-ecran-mobile-mare-au-centre-design.md`). Restent :

- **Le vrai dessin** : remplacer les provisoires fichier par fichier, au nom exact, selon
  `docs/da/gabarits.md`, sans toucher au code. Les traits de courant du Gour, très
  réguliers, sont les premiers à reprendre.
- **Le budget de lumières** : la spec DA vise quatre lumières à l'écran au plus ; quand
  toute la Noue est ouverte en portrait, six bandes sont visibles, donc six lumières.
  À mesurer sur un vrai téléphone avant de trancher.
- **Le build téléphone** : les modules Android / iOS ne sont pas installés dans Unity Hub.

---

### 2. Ce que le Souffle doit faire gagner

**Mesuré le 2026-10-07** (`RESULTATS.md`, Finding 6). Quatre politiques de dépense du
Souffle — la moins chère d'abord, la moitié en réserve, la globale seule, la plus profonde —
donnent **exactement le même rythme** : mêmes durées de cycle, même fond au cycle 13, même
jeu actif. Elles ne changent que l'échelle des nombres (de 1e108 à 1e193 /s au cycle 15).
La politique de dépense n'était donc pas le levier : l'ancienne inquiétude (« toute mesure
d'équilibrage en aval est suspecte ») ne tient pas pour le rythme.

**Ce qui reste, et c'est une décision de design.** Le joueur rentre dans l'œuf quand
l'acquis de séjour plafonne, et l'acquis monte avec le temps seul. Le Souffle ne raccourcit
donc rien. Pour qu'une insufflation se sente, il faut qu'elle touche le temps : l'acquis,
le seuil de renaissance, ou le coût de la descente. À trancher avec l'auteur avant le
chantier 3, qui calibre contre ce rythme.

**L'outil est en place** : `Politique.Insuffler` dans le simulateur, et la mesure
`MesurePolitiquesDInsufflationTests` à relancer après toute décision.

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

**Dépend de** : la décision du chantier 2 — les couples (A, B) se calibrent contre le
rythme des cycles, et c'est ce rythme que cette décision peut changer.

**Taille** : grande. Mécanique neuve, arbre de données, écran, et six paramètres à
résoudre par la mesure.
**Ce que ça lève** : « Sans technique » dans le README.

---

### 4. Le contenu des assises III à VI

**Où on en est.** L'assise II, **le Gour**, est livrée (spec
`2026-10-07-assise-ii-le-gour-design.md`) : nom, quatre espèces (épinoche, chabot,
lamproie, ombre), la marque des membranes, son décor, ses succès. **Elle l'a été en
dérogation à la règle d'engagement** : la Noue n'avait été mesurée qu'au simulateur.
Mesurer la Noue et le Gour en vraie partie est donc la condition de l'assise III.

**Ce qui est déjà prêt.** Les six assises **existent dans la donnée**, avec leur géométrie
complète : `6 / 12 / 12 / 12 / 12 / 8 = 62 paliers` — la seule répartition à 62 paliers
où une espèce s'ancre exactement sur `3 × rang`. Le registre d'art, la roche, les marques
et l'interface (un groupe par lieu livré) suivent `PALIERS_LIVRES` sans changement de code.

**Ce qui manque vraiment** :

- **Les noms** des assises III à VI, par la charte phonétique du Codex §6 (de plus en plus
  sombre). `[P] P3` reste ouvert pour elles.
- **La typologie du mana.** `mana-typologie.md` (Tier 1) **n'est toujours pas dans le
  dépôt** ; les types restent provisoires (`type-mana-2`…), y compris pour le Gour.
- **Quatorze espèces** et leurs succès ; les marques luminescence, minéralisation,
  épaississement (GDD §15.1).

**La règle d'engagement** : « *aucune assise n'est produite avant que la précédente ait
été mesurée* » (§12). Assise par assise, jamais en bloc.

**Dépend de** : la mesure du Gour, et pour l'équilibrage, du chantier 3.

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

Cinq broutilles laissées par la revue finale du 2026-09-18, du temps du TypeScript — les
noms de fichiers sont ceux du web ; chacune est à revérifier dans le C# avant d'agir.
Aucune ne mérite un chantier : **elles montent dans celui qui touche le fichier concerné.**

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
