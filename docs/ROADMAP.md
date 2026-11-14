# IdlePond — la roadmap

**Tenue à jour le** : 2026-11-14
**Objet** : ce qui reste à faire, dans l'ordre, et pourquoi cet ordre.
**Ce document churne.** Le Codex ne bouge presque jamais ; celui-ci bouge à chaque
chantier fini. Ne jamais y écrire de définition — elle irait dans `CODEX.md`.

---

## Où on en est

**Le jeu est dans Unity** ; la version web est archivée dans `archive/web/`. **La Noue et
le Gour** sont jouables de bout en bout : creuser, convaincre, monter, grandir, améliorer,
renaître. La scène est en **pixel art** (URP 2D, lumières 2D), sprites **provisoires**
générés par script (cahier des charges : `docs/da/gabarits.md`). L'écran est celui du
téléphone en portrait — barre, mare au centre, dock, tiroirs — et tient en paysage.

| | Livré | Sur le papier |
|---|---|---|
| Assises | 2 (la Noue, le Gour) | 6 |
| Paliers | 18 | 62 |
| Espèces | 6 | 21 |
| Succès | 54 | — |
| Axes permanents | améliorations de renaissance (dont le séjour), densité, succès, arbre de technique (5 branches sur 6 actives) | la branche Construction |
| Achats d'une vie | creuser, convaincre, monter, grandir, bonus de lieu, techniques au mana | — |
| Gestes | toucher la mare (le héros prend ; dix touchers par seconde au plus) | — |

**Tests** : 492 EditMode et 33 PlayMode, tous verts (2026-11-14), plus trois mesures à la demande. Les références de
parité sont produites par le C# depuis le Gour (voir `RESULTATS.md`, journal des
références).

**Le descriptif complet du backend** (ce qui existe, ce qui reste, les décisions ouvertes) :
`docs/BACKEND.md`.

**Mesurer une vraie partie** : la boucle écrit une ligne par renaissance dans
`telemetrie.jsonl`, à côté de la save.

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

### 2. Régler ce que le Souffle fait gagner

**Tranché le 2026-11-13** (`RESULTATS.md`, Finding 8) : une amélioration de renaissance, **le
séjour**, raccourcit le temps que met la contenance à monter. C'est le premier achat qui change
la durée d'une vie : la descente simulée passe de 91 h à 45 h quand on l'achète en priorité.
Les bonus de lieu ont été renchéris le même jour (Finding 7).

**Ce qui reste, et c'est encore une décision de design.**

- **La durée totale visée.** Avec les dix rangs, les vies redeviennent presque plates (2,4 à
  3,3 h). Choisir entre ≈ 45 h, ≈ 91 h ou un entre-deux, puis régler la part par rang et le rang
  maximal (`Constantes.cs`).
- **Les bonus après la troisième vie.** Tout est tenu dès la quatrième, quel que soit le prix :
  il faut soit de la profondeur (chantier 4), soit un verbe qui les rachète (chantier 3).
- **Une vraie partie.** Rien de tout cela n'a été joué ; `telemetrie.jsonl` est là pour ça.

**Le toucher hâte le séjour** (tranché le 2026-11-14, `RESULTATS.md`, Finding 10). Mesuré seul,
il mourait à la deuxième vie : la contenance bloquait la mare en 1 à 3 minutes. Un toucher fait
désormais avancer l'acquis de 0,25 s (plus 10 % par rang de son amélioration, bornée à 10). À deux
touchers par seconde, quinze vies prennent 47 h de jeu actif au lieu de 92 ; à la borne de dix
par seconde, 16 h. **Ce qui reste** : la durée totale visée croise maintenant la cadence de
toucher et l'amélioration de séjour ; décider si un autoclicker peut diviser la partie par six,
ou borner le séjour gagné par seconde. Le plancher de cadence suppose un toucher toutes les deux
secondes ; un joueur qui ne touche jamais le troue de 9 s (`PlancherDeCadenceTests`).

**L'outil est en place** : `MesurePolitiquesDAmeliorationTests` et `MesureDesBonusTests`, à
relancer après tout réglage.

---

### 3. L'arbre de technique, ce qui reste

**Transcrit le 2026-11-13** (`RESULTATS.md`, Finding 9) : trente nœuds, six branches, appris
par l'usage. Les verbes tournent, y compris au retour d'absence. La main sûre et la sonde ont
quitté l'onglet « Débloquer ».

**Ce qui reste.**

- **Resserrer deux branches.** « Galeries connues » tombe à la vie 11 et « Tout le monde suit »
  jamais en quinze vies : les couples (A, B) de Creusement et d'Amélioration sont à régler
  (`Technique.COUPLES_A_B`), puis `MesureDeLArbreTests` à relancer.
- **Tous les verbes de l'arbre agissent** depuis le 2026-11-14 : « La file » (trois creux
  demandés d'avance, payés seuls dès que le mana arrive, même hors ligne), « Lire l'eau »
  (sous la jauge, ce que rentrer vaudrait), « Compter les siens » (les débits par espèce ;
  sans lui, une pente sans chiffre). « Compter les siens » est passé en tête d'Entretien ;
  ces calendriers restent des graines non mesurées.
- **Mesurer Entretien**, que le simulateur ne voit pas, et tout l'arbre en vraie partie.
- **La branche Construction** est muette tant que temples et portails n'existent pas (chantier 5).
- **Les ~4 verbes réservés aux succès** (budget de 15, dont 11 à l'arbre) ne sont pas alloués.

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
| **Miracles** | Ce que le Souffle achetait dans l'ancienne conception | **Gelé** `[P26]`. Coexisteront avec les améliorations de renaissance le jour où ils seront tranchés |
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
| `canon.test.ts` | Les textes d'amélioration ne sont pas dans la garde lexicale de l'écran |
| `economie.ts` | `detailDeCaptation` construit sa liste en trois morceaux — fossile d'un correctif |
| `SceneDeLaMare.ts` | `NaN` si le canevas fait exactement 144 px de large |

---

## Gelé

**`plancher-de-cadence.test.ts`, le test `PARQUÉ`.** Il échoue, c'est voulu, et il ne
ferme qu'à une « tâche 13 » qui résoudra les nombres décidant de l'atteignabilité des
succès — hors de tout ce qui est listé ici. Son propre commentaire démontre par la mesure
qu'aucune valeur de `DEBIT_HEROS` (aujourd'hui `DEBIT_AMBIANT`) ne peut le satisfaire, et que jouer *mieux* aggrave le
trou au lieu de le combler. **Ne pas le « réparer ».**

---

## Comment cette roadmap se tient à jour

Un chantier fini est **retiré**, pas coché — son récit vit dans l'historique git et dans
`amendement-v1.1.md`. Ce qu'on apprend en le faisant (une mesure, un dead end, un lever
qui ne marche pas) va dans le commentaire de la constante qu'il contraint, jamais
seulement ici : un résultat négatif enterré dans un document de planning se refait.
