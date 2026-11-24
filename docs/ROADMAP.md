# IdlePond — la roadmap

**Tenue à jour le** : 2026-11-24
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
| Absence | le mana monte jusqu'à la contenance du départ ; le séjour ne bouge pas | — |
| Prototype | WebGL dans le navigateur du téléphone, Windows | APK Android (module absent) |

**Tests** : 516 EditMode et 35 PlayMode, tous verts (2026-11-15), plus cinq mesures à la demande.
**Pas de CI** depuis le 2026-11-23 (retirée faute de licence Unity fournie, avec la publication
WebGL sur Vercel) : les tests se lancent en local, `outils/unity.sh tests`. Les références de
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

### 0. La direction artistique et l'esprit du jeu

Le socle est posé (scène en pixel art, gabarits, sprites provisoires), l'écran mobile
aussi (spec `2026-10-07-ecran-mobile-mare-au-centre-design.md`). Restent :

- **Tranché le 2026-11-22 : un monde parallèle** (Codex v1.2, §2). La fantaisie existe —
  peuples, magie, dieux — et le vivant est **dérivé du réel** : reconnaissable, mais pas
  réel. Couleurs franches dès la Noue, merveilleux qui croît avec la profondeur, révélations
  de l'arc inchangées. **Ce qui reste, dans l'ordre** :
  1. ~~La bible du monde~~ — **validée le 2026-11-22 et passée au Codex v1.3** (§2) : six
     magies, tribus, quinze espèces à venir, peuples de l'eau, panthéon, faune d'ambiance.
  2. ~~La dérivation des six espèces livrées~~ — **validée le 2026-11-23** (Codex v1.4, §2 ;
     `docs/da/gabarits.md`) : couleurs, accent, comportement, texte et représentant de chaque
     tribu. **Dans le jeu depuis le 2026-11-23, à 240 px** : les palettes et l'accent de chaque
     espèce. **Depuis le 2026-11-24, les comportements** (`Tribus.cs`, pur et testé ;
     `ComportementsDesTribus.cs` et `Nageurs.cs` pour le dessin) : la vague de rouge du Chœur à
     chaque seuil du vairon, les loches qui frissonnent quand l'eau va se troubler ou la descente
     se rouvrir, un nid-forteresse par seuil de l'épinoche, les chabots pétrifiés dans l'eau
     trouble, les lamproies accrochées aux parois et le fil de mana qui leur vient d'une tribu qui
     monte, le mirage du nouveau venu laissé par une ombre. Provisoire : fait de l'éclat teinté et
     des images d'espèce. **Écrit hors de l'éditeur** : la partie pure passe ses tests sous .NET,
     le dessin n'a pas encore été vu dans Unity — à lancer (`outils/unity.sh tests`, puis l'œil
     sur l'atelier Espèces). **Depuis le 2026-11-24, les poissons de troupe à 24 px**, dessinés
     d'après la maquette validée (`outils/da/especes.py`), dans un cadre commun de 39 × 27.
     **Et les représentants** (le Coryphée, la Doyenne, le Capitaine de la Garde, le Vieux
     Gardien, la Mère pèlerine, le Grand Héraut) : 60 px, quatre images, en tête de leur troupe ;
     ils entrent dans leur bande à la conviction, et leur portrait est dans la fiche du tiroir
     Espèces. **Reste** : la bibliothèque (le bestiaire du GDD §14.8), où le Codex les montre aussi.
  3. ~~Les palettes de la Noue, du Gour et du héros~~ — **validées le 2026-11-23** (Codex v1.5,
     §2 ; `docs/da/gabarits.md`) et **dans le jeu à 240 px** : le héros en or, ses marques à la
     couleur du lieu, l'eau et le décor de chaque lieu, un calque d'ambiance par bande.
  - **En cours : le héros qui s'éveille** (bible du monde v0.4, d'après les références de
    l'auteur dans `docs/da/reference/`). À l'essai dans le prototype : la lignée `da-2` pour les
    quatre stades, quatre éveils en calques (runes, voile, étoiles, aura) aux renaissances 1, 3,
    6 et 10, et **le renouveau** joué à chaque renaissance (l'œuf, la larve, l'alevin). Produit
    par `outils/da/heros.py`. Reste à trancher : les seuils d'éveil, le partage du corps entre
    runes et marques, le dessin final. Le héros est retiré à l'échelle de 360 px (2026-11-24).
  - **La scène est à 360 px de large depuis le 2026-11-24** (`Gabarits.LARGEUR_VISEE`, retenu le
    2026-11-23 au lieu de 240). Toutes les mesures ont suivi d'un facteur 1,5 (bande de 84 px,
    décor, héros, nage) : la mise en page ne bouge pas à l'écran, le dessin a une fois et demie
    plus de pixels. Le décor provisoire est régénéré à cette échelle ; le générateur encode
    désormais ses PNG lui-même (`Editeur/Png.cs`), pour des octets identiques dans l'éditeur et
    hors de lui. **Fait hors de l'éditeur** : les tests purs passent sous .NET, les assets sont
    vérifiés par script ; à lancer dans Unity (tests EditMode et PlayMode, captures) et à mesurer
    sur un vrai téléphone — repli sur 320 px s'il ne suit pas.
  4. **La faune d'ambiance** (ce qui vit dans la mare sans produire) et **une mare qui change
     à chaque renaissance** (chantier 6).
  Le constat qui a mené à la décision est gardé ci-dessous.
- **L'esprit : fantaisie, couleur, diversité — le constat du 2026-11-22.**
  Constat de l'auteur (2026-11-22) : le jeu a perdu l'esprit fantaisiste, les couleurs et la
  diversité de la première version (l'Étang des Merveilles, web, juin 2026 : 42 espèces sur
  douze profondeurs, du Lac de Surface à la Dimension Quantique, poissons Rubis, Saphir,
  Cristal, un premium et un légendaire par profondeur, l'étang qui change à chaque prestige).
  Aujourd'hui : deux lieux, six espèces, une palette de verts, de bruns et de vase
  (`docs/da/gabarits.md`), et le Codex §6 qui veut un monde « pas exotique, **vieux** ».
  - **Ce que le canon permet déjà, et que l'écran ne montre pas.** Le merveilleux est dans
    la fiction mais caché jusqu'à la fin : une pierre qui fait de l'eau « comme un miracle »,
    une marraine qui voit presque tout, un esprit sorti d'un œuf, une mer scellée, des
    égarés remontés de l'en-dessous, des marques de luminescence et de minéralisation. Et
    l'eau douce réelle n'est pas terne : gorge rouge de l'épinoche au frai, ventre rouge du
    vairon mâle, dorsale irisée de l'ombre, libellules, martin-pêcheur.
  - **Trois directions** :
    1. *Le merveilleux monte des profondeurs* — la surface reste réelle et sobre, chaque
       assise plus bas est plus étrange, plus lumineuse, plus saturée (luminescence au
       III, minéral au IV, chaleur au V, mer relique au VI). Garde le Codex intact ; mais
       les 18 paliers jouables aujourd'hui restent sobres.
    2. *Le réel, vu par un conte* — on garde les vraies espèces et les noms, mais en
       couleurs franches dès la Noue, une faune d'ambiance qui ne produit rien
       (libellules, gerris, grenouilles, écrevisses), et une mare qui change à chaque
       renaissance. Demande d'amender le Codex sur la palette, pas sur les mots.
    3. *Retour à l'Étang des Merveilles* — créatures fantastiques, pierres précieuses,
       profondeurs magiques. Contredit le Codex (§2, §6, mots morts) : il faudrait le
       réécrire.
  - **Recommandation (avant décision)** : 2 puis 1 — de la couleur et de la vie dès la surface, et le
    merveilleux qui grandit à mesure qu'on descend. Tant que ce n'est pas tranché, le vrai
    dessin attend : il dessinerait la mauvaise palette.
  - **Ce qui peut revenir de la version web sans casser le canon** : le bestiaire en
    silhouettes (la bibliothèque du GDD §14.8), une mare qui change visiblement à chaque
    renaissance (les strates de la version web, proches des couches et des marques), plus
    d'espèces par lieu sous forme de faune d'ambiance, et les saisons cosmétiques (frai, gel,
    crue) décidées le 2026-11-18.
- **Le vrai dessin** : remplacer les provisoires fichier par fichier, au nom exact, selon
  `docs/da/gabarits.md`, sans toucher au code. Les traits de courant du Gour, très
  réguliers, sont les premiers à reprendre.
- **Le budget de lumières** : la spec DA vise quatre lumières à l'écran au plus ; quand
  toute la Noue est ouverte en portrait, six bandes sont visibles, donc six lumières.
  À mesurer sur un vrai téléphone avant de trancher.
- **Le build téléphone** : les modules Android / iOS ne sont pas installés dans Unity Hub. En
  attendant, un **prototype WebGL** se joue dans le navigateur du téléphone
  (`outils/unity.sh build webgl`, voir le README) ; l'APK sort de `outils/unity.sh build android`
  dès que le module est ajouté.

---

### 1. Jouer et mesurer une vraie partie

**Aucune vraie partie n'a été jouée.** Tout ce qui précède vient du simulateur. Le
prototype WebGL existe depuis le 2026-11-22 : le jouer sur un téléphone jusqu'à la
première renaissance, puis sur plusieurs jours, et lire `telemetrie.jsonl`. C'est la
condition de l'assise III (règle d'engagement), du réglage du séjour, de la règle
d'absence, de la cadence de toucher et de l'arbre. À relever aussi : la lisibilité sur
téléphone (encoche, barre de geste, taille des boutons) et le budget de lumières.

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

**Absent, le séjour ne bouge plus** (tranché le 2026-11-15, `RESULTATS.md`, Finding 11). Avant,
une vie se finissait pendant une absence, et quinze vies demandaient trois à huit heures de
présence. Maintenant la mare se remplit à ce qu'on portait en partant, et il faut être là pour
porter plus. **Ce qui reste, et c'est le plus gros point ouvert du jeu** : l'absence ne vaut plus
que quelques minutes de production ; quinze vies demandent 92 h de présence (47 h à deux touchers
par seconde), soit 281 jours à deux sessions de dix minutes par jour ; et cette présence se passe
devant une mare pleine. Soit on dose (le séjour avance en absence à une fraction de sa vitesse),
soit on donne quelque chose à faire pendant qu'on attend. Les **expéditions** (décidées le
2026-11-18, chantier 6) sont faites pour la seconde piste ; elles ne règlent pas la première.
**Conséquence pour la publicité** (décidée le 2026-11-19 : une avance rapide « calculée comme une
absence ») : sous la règle actuelle, une absence ne vaut presque rien, donc l'avance rapide non
plus. Elle est à redéfinir en même temps que la règle d'absence.

**L'outil est en place** : `MesurePolitiquesDAmeliorationTests` et `MesureDesBonusTests`, à
relancer après tout réglage.

---

### 3. L'arbre de technique, ce qui reste

**Transcrit le 2026-11-13** (`RESULTATS.md`, Finding 9) : trente nœuds, six branches, appris
par l'usage. Les verbes tournent, y compris au retour d'absence. La main sûre et la sonde ont
quitté l'onglet « Débloquer ».

**Ce qui reste.**

- **Amélioration est réglée** (2026-11-14, `RESULTATS.md`, Finding 9) : « Tout le monde suit »
  tombe à la vie 10, en assise V, et « L'élan » à la vie 5. Creusement n'a pas bougé :
  « Galeries connues » tombe déjà en assise V, là où le noyau §6.1 le veut (« pas avant
  l'assise V »). **À trancher si on veut ces deux verbes plus tôt** : il faudrait amender le
  §6.1, pas régler un couple.
- **Tous les verbes de l'arbre agissent** depuis le 2026-11-14 : « La file » (trois creux
  demandés d'avance, payés seuls dès que le mana arrive, même hors ligne), « Lire l'eau »
  (sous la jauge, ce que rentrer vaudrait), « Compter les siens » (les débits par espèce ;
  sans lui, une pente sans chiffre). « Compter les siens » est passé en tête d'Entretien ;
  ces calendriers restent des graines non mesurées.
- **Entretien est mesurée, pas réglée** (2026-11-15, `RESULTATS.md`, Finding 11) : son compteur
  est l'absence créditée, qui a changé d'échelle avec le séjour figé. Le dernier nœud tombe
  avant l'assise V pour trois rythmes de joueur sur quatre. À régler quand la règle d'absence
  sera arrêtée (`MesureDeLEntretienTests`).
- **Mesurer tout l'arbre en vraie partie.**
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
- **La typologie du mana** est écrite depuis le 2026-11-22 (Codex v1.3, §2 « Les six
  magies » : Sève, Courant, Éclat, Gel, Braise, Sel). Reste à la porter dans le code, où les
  types sont encore `type-mana-2`…, y compris pour le Gour.
- **Quinze espèces** (21 moins les 6 livrées), retenues au Codex v1.3 sous noms provisoires,
  et leurs succès ; les marques luminescence, minéralisation, épaississement (GDD §15.1).

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

### 6. Les fonctionnalités qui restent à construire

Hors économie et hors contenu des lieux. Elles peuvent avancer en parallèle des chantiers
1 à 4, sauf mention contraire.

| Fonctionnalité | Ce que c'est | Dépend de |
|---|---|---|
| **Expéditions** (décidé 2026-11-18) | Une espèce convaincue part explorer un palier déjà ouvert pendant l'absence et cesse de produire le temps du voyage ; elle rapporte du récit et des points d'arbre, jamais de mana. À calculer en un seul pas | Le récit dans l'état ; la règle d'absence (chantier 2) |
| **Le récit dans l'état** (BACKEND lot 3) | Les chapitres, le relais qui ouvre `Dialogue`, la pierre trouvée, l'esprit venu ; le journal qui raconte, séparé de la bibliothèque qui inventorie | — |
| **La bibliothèque / le bestiaire** | Les espèces en silhouette puis en fiche, la faune rencontrée, les textes des égarés | Le récit dans l'état ; la direction du chantier 0 |
| **La fin** (BACKEND lot 5, D3 et D6) | La pierre : offrandes de mana jusqu'à la contenance, qui font avancer les actes VII à IX ; décider l'après-acte IX | Le lieu VI |
| **Les saisons cosmétiques** (décidé 2026-11-18) | Frai, gel, crue changent l'aspect de la mare, sans rien à gagner ni à rater | Le chantier 0 |
| **Une mare qui change à chaque renaissance** | Des changements visuels permanents et cumulés (végétation, lumière, particules), lus sur le nombre de renaissances | Le chantier 0 |
| **Les retours visuels et le son** | Brancher le flux `Partie.EvenementsDeJeu` : chiffres qui jaillissent, roche qui cède, éclat d'un seuil, eau troublée plus visible ; puis l'audio (une ambiance par lieu, cinq ou six effets) | — |
| **Le confort d'application** (BACKEND lot 6) | Export et import de la save, copie tournante, notifications locales qui ne punissent pas l'absence, rapport de retour détaillé | — |
| **La monétisation** | Modèle à choisir (premium, cosmétique F2P, épisodique) ; jamais de puissance ; une seule exception décidée le 2026-11-19, la pub contre avance rapide, plafonnée et obtenable sans pub — à redéfinir avec la règle d'absence. Le GDD §17 n'est pas encore amendé | La durée totale (chantier 2), une vraie partie (chantier 1) |

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
