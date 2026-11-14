# IdlePond — le backend : ce qui existe, ce qui reste, dans quel ordre

**Écrit le** : 2026-11-13, à partir de la lecture de toute la doc (`idlepond-noyau-v1_0.md`,
`CODEX.md`, `GDD.md`, `amendement-v1.1.md`, `RESULTATS.md`, `ROADMAP.md`, `PRESEANCE.md`, les
specs de `docs/superpowers/specs/`) et du code de `Assets/IdlePond/`.
**Objet** : un seul endroit qui dit ce que le backend fait aujourd'hui, ce qu'il lui manque,
ce qu'il faut décider, et l'ordre de construction. « Backend » veut dire ici : `Noyau/`
(la mécanique pure), `Simulateur/` (la mesure) et la partie de `Jeu/` qui n'est pas de
l'écran (partie, sauvegarde, horloge, hors ligne, réglages, télémétrie).
**Ce document ne crée aucun canon.** Quand il contredit le noyau v1.0, le Codex ou
`RESULTATS.md`, ce sont eux qui gagnent (`PRESEANCE.md`).
**Non vérifié dans cette passe** : les tests n'ont pas été relancés. Le dernier compte connu
est celui de la roadmap (340 EditMode, 16 PlayMode, verts le 2026-10-07), avant l'onglet
Débloquer.

---

## 1. Le projet en dix lignes

Un idle incrémental narratif. Le joueur est un poisson qui **creuse** vers le bas, **convainc**
des espèces qui produisent du mana, **monte** leurs crans, **grandit** lui-même. Il ne peut
stocker que sa **contenance** ; quand le creux suivant coûte plus que ce qu'il peut porter, il
**renaît** : mana, creux, espèces et crans repartent de zéro, et il garde sa contenance
relevée, la **densité** (multiplicateur global), le **Souffle** (monnaie permanente) et ses
**succès**. Six lieux, 62 creux, 21 espèces sur le papier ; deux lieux livrés (la Noue, le
Gour : 18 creux, 6 espèces). Le récit est une chronique à chemin unique en neuf actes ; la
seule décision du joueur est *combien de temps rester*.

Trois contraintes d'architecture tiennent tout le reste :

1. `Noyau/` est **pur** : `Reducteur.Tick(etat, dt) → etat`, sans horloge, sans hasard hors de
   la graine stockée, sans Unity.
2. Toute mécanique se calcule **en un seul pas pour `dt = 8 h`** (c'est ce qui rend le hors
   ligne exact et gratuit).
3. **Qui monte quoi** : les améliorations de renaissance (Souffle) montent la production ; les
   techniques et les succès baissent des coûts, relèvent des plafonds ou automatisent ; seule
   exception, un bonus de lieu monte la production de son lieu et se perd à la renaissance.

---

## 2. Ce qui est livré

### 2.1 L'état (`Noyau/Types.cs`)

| Bloc | Champs | Sort à la renaissance |
|---|---|---|
| `EtatCycle` | mana courant, creux ouverts, espèces (convaincue, cran), pic de production, durée du cycle, acquis de séjour, niveau du héros, rangs des bonus | **Remis à zéro** |
| `EtatPermanent` | densités par creux, Souffle, contenance, couches (marques), profondeur maximale atteinte, compteurs de technique, nœuds de technique, succès, nombre de renaissances, espèces ayant atteint cent, mana ambiant, heures hors ligne créditées, rangs des améliorations de renaissance | **Gardé** |
| `EtatTelemetrie` | mesures par cycle, intervalles entre succès | Gardé |
| Racine | version de save (2), graine, temps de jeu, limite de contenu, réglage de courbe | — |

### 2.2 Les achats (`Noyau/Reducteur.cs`, exposés par `Jeu/Partie.cs`)

| Écran | Code | Monnaie | Prix | Effet |
|---|---|---|---|---|
| Creuser | `Creuser` | mana | `60 × 2,4^creux`, moins les remises | Ouvre le creux suivant ; refusé au-delà de la contenance ou du contenu livré |
| Convaincre | `Debloquer` | mana | 0,6 × le coût du creux de l'espèce | Une fois par espèce et par vie |
| Monter | `Ameliorer` | mana | ×1,15 par cran | +1 cran ; paliers ×2 / ×4 / ×8 / ×16 aux crans 10 / 25 / 50 / 100 ; à 100, +3 % global définitif |
| Grandir | `Grandir` | mana | `60 × 1,75 × 2,4^(n−1)` | Niveau du héros : ×1,15 par niveau sur toute la production, et ce que rapporte un toucher |
| Bonus de lieu / technique | `AcheterUnBonus` | mana | coût du creux d'ancrage × `2^rang` | Voir 2.5 |
| Améliorer | `AcheterUneAmelioration` | **Souffle** | `3 × 4^rang` (ciblée), `2 × 4^rang` (globale) | Voir 2.4 |
| Renaître | `Renaissance.Renaitre` | — | — | Voir 2.3 |

### 2.3 La production, la contenance, la renaissance

- **Production** (`Economie.cs`) : par espèce, débit de base × cran × palier de cran ×
  amélioration ciblée × bonus de lieu ; puis, globalement, × profondeur × densité × héros ×
  drapeaux des cent. L'eau donne un ambiant fixe (`DEBIT_AMBIANT`), ce qui amorce la partie ; le héros, lui, prend quand on touche la mare (`Reducteur.Toucher`, `Economie.ValeurDuToucher`).
- **Détail de captation** : `Economie.DetailDeCaptation` rend, par espèce, chaque terme actif et
  sa source. C'est une structure, pas un texte : l'écran la met en phrases.
- **Contenance** : `contenance = contenance gardée × (1 + acquis de séjour)`. L'acquis sature
  vers 47,6 avec un temps `τ = 0,87 h × 1,02^profondeur maximale atteinte`. Le plafond monte
  donc **pendant** la vie, et de plus en plus lentement à mesure qu'on a été profond.
- **Eau troublée** à 85 % de la contenance ; **saturation** à 100 % (le stock est borné, rien
  n'est puni) ; **bloqué** quand le creux suivant dépasse la contenance.
- **Renaissance** : fixe la contenance, ajoute la densité `pic^0,6` (jamais en baisse), crédite
  le Souffle `(pic)^0,5`, écrit une couche par lieu traversé, incrémente les compteurs.
- **Densité** : `(1 + densité)^(0,8 / 0,6)`, sur la production seule.

### 2.4 Les améliorations de renaissance

Une **globale** (ajoute `0,05 × rang` au débit de base de toutes les espèces) et une
**ciblée par espèce** (×1,5 par rang). Achetables à tout moment, jamais perdues. Le registre est
engendré depuis les espèces : une espèce nouvelle reçoit sa ciblée sans code.

### 2.5 L'onglet Débloquer : bonus de lieu et techniques (`RegistreDesBonus.cs`)

Quinze entrées : quatre bonus pour la Noue, quatre pour le Gour, sept techniques. Quatre genres :
réduction de coût `(1 − part)^rang`, production du lieu `(1 + part)^rang`, confort `1 + part ×
rang` (la patience : plafond hors ligne), verbe (une capacité). Ouverts par la **maîtrise** (creux
ouverts dans le lieu ; en tout, pour une technique). **Tout se perd à la renaissance.**

Deux verbes tournent (`Automatismes.cs`, après chaque pas en ligne, jamais hors ligne) : *la main
sûre* (monte le cran le moins cher s'il coûte moins d'un dixième du mana) et *la sonde* (creuse
dès que possible).

### 2.6 Les succès (`RegleDesSucces.cs`, `Donnees/Succes/`)

54 succès en trois familles (franchissements, seuils, actes), trois visibilités (ouvert, fermé,
secret), listés seulement quand leur lieu est atteint. Constatés à la fin du pas, jamais
réclamés, acquis pour toujours. Effets : remise de coût (2 % chacun), plafond, verbe — **jamais
de production**. Le registre de voix (pente, signes, directives) est figé à l'obtention.
Progression vers un succès ouvert : `ProgressionVersLeSucces`.

### 2.7 La voix (`Voix.cs`)

Dérivée du nombre de renaissances : pente (0), signes (1), directives (3). `Dialogue` existe dans
le type et **n'est pas atteignable**. Le noyau ne fait que dire le palier ; ce que l'écran en
fait lui appartient.

### 2.8 Le monde impur (`Jeu/`)

| Module | Ce qu'il fait |
|---|---|
| `Partie` | Tient l'état, applique les actions, avance le temps, passe les automatismes, garde les annonces de succès et le retour d'absence |
| `Boucle` / `Amorce` | Pas de 100 ms, durée de vie de la partie entre les scènes |
| `Persistance` | `idlepond.json`, version 2, chaîne de migrations, Decimal en mantisse/exposant, écriture atomique, copie avant réinitialisation, lecture tolérante |
| `Horloge` / `HorsLigne` | Crédit en **un seul pas**, borné à 6 h (jusqu'à 24 h par la patience), zéro si l'horloge recule ; les heures créditées alimentent le compteur Entretien |
| `Reglages/` | Réglages purs et bornés, magasin `reglages.json`, volumes par canal, format téléphone / tablette / PC, notation des nombres |
| `Telemetrie` | Relevé pur dans le noyau ; côté jeu, une interface `ICollecteur` dont la seule implémentation est muette |
| `ServicesDePartie` | Redirection de la sauvegarde (ateliers, tests) |

### 2.9 La mesure (`Simulateur/`)

Le même noyau, joué par une politique : achats au mieux, quatre politiques de dépense du Souffle,
règle de renaissance à 95 % de l'acquis. `Calibreur` sait résoudre un couple (A, B) de technique
contre une table d'ouvertures visées. Mesures de référence : descente complète au **cycle 13**,
**≈ 82 à 91 h** de jeu actif, cycles de 3,1 h à 8,9 h.

### 2.10 Les gardes

Pureté du noyau, déterminisme, équivalence de pas (480 pas de 60 s = un pas de 8 h), parité à
1e-9 contre des parties figées, canon (qui monte quoi), lexique (mots morts, mots interdits à
l'écran), contenance (le ×47,1 émerge, il n'est écrit nulle part).

---

## 3. Ce qui est posé mais vide ou débranché

| Quoi | État réel |
|---|---|
| **Arbre de technique permanent** (noyau v1.0 §6) | **Transcrit le 2026-11-13** : voir le lot 2 plus bas. Cette ligne décrivait l'état d'avant |
| **Capacités** | Onze dans l'énumération ; deux seulement font quelque chose (achat auto, creusement auto), et elles viennent des techniques au mana |
| **Bonus et simulateur** | Le simulateur n'achète aucun bonus : les quinze entrées sont des graines jamais mesurées |
| **Lieux III à VI** | Géométrie présente (identifiants `assise-3`…, `espece-3-1`…, `type-mana-N`), aucun nom, aucun texte, aucun bonus, aucun succès. `PALIERS_LIVRES = 18` |
| **Couches** | Une marque par lieu traversé ; la variante « interrompue » du GDD §10.3 n'est pas suivie |
| **Télémétrie** | Relevée, jamais publiée ni écrite nulle part |
| **Son** | Volumes, canaux et `SourceSonore` prêts ; **aucun fichier audio** dans le projet |
| **Récit** | Les textes des succès existent ; il n'y a ni chapitres, ni actes joués, ni événement de récit dans l'état |
| **Test parqué** | `PlancherDeCadenceTests` échoue exprès (cadence des succès de la première demi-heure). Ne pas le réparer sans décider des nombres |

---

## 4. Ce que la mesure dit et que le design n'a pas encore absorbé

1. **Le Souffle ne fait rien gagner en temps** (`RESULTATS.md`, Finding 6). Quatre façons de le
   dépenser donnent les mêmes durées au dixième d'heure ; seule l'échelle des nombres change (84
   ordres de grandeur). La longueur d'un cycle vient de l'acquis de séjour, qui ne dépend que du
   temps.
2. **Passé le cycle 13, plus rien ne s'ouvre.** Le fond est atteint, les crans seuls rendent ×1,1
   par cycle. Les deux axes censés porter la suite sont le Souffle (qui ne change pas le rythme)
   et l'arbre permanent (vide).
3. **Deux « techniques » coexistent.** Celles de l'onglet (au mana, perdues à la renaissance) et
   l'arbre du noyau (à points d'usage, permanent). Elles se disputent déjà deux capacités :
   `AchatAuto` et `CreusementAuto` sont données par les techniques au mana, alors que le noyau les
   place dans l'arbre, et que la règle veut **une seule source par capacité**.
4. **Le blocage est long.** Selon le cycle, 37 % à 83 % de la vie se passe sans pouvoir creuser
   (amendement v1.3). Les verbes et les bonus sont ce qui occupe ce temps ; ils ne sont pas
   mesurés.
5. **Aucune vraie partie n'a été mesurée**, ni sur la Noue ni sur le Gour : tout vient du
   simulateur, et le Gour a été livré en dérogation à la règle d'engagement.
6. **Codex et GDD divergent sur la pierre.** Le Codex (acte VII) : « plus il en dédie à la pierre,
   plus l'eau qu'elle rend est pure ». Le GDD §6.7 : aucune pureté à fabriquer, le verrou est la
   contenance. Le Codex gagne sur la fiction ; la mécanique de fin n'est écrite nulle part.

---

## 5. Les décisions à prendre (avec une recommandation)

| # | Question | Recommandation | Ce que ça débloque |
|---|---|---|---|
| D1 | Que fait gagner le Souffle **en temps** ? | Une petite famille d'améliorations « de séjour », à rangs finis, qui raccourcit `τ` (l'acquis monte plus vite) ; le calcul reste en un pas, puisque `τ` ne change que sur un achat. Demande d'amender la règle n° 3 (« l'amélioration ne monte que la production ») | Le rythme, donc tout calibrage |
| D2 | Les deux techniques : fusion, ou deux étages ? | Deux étages nommés différemment. Au mana : des *gestes* de la vie courante (remises, patience). Permanent : l'arbre, qui porte **tous** les verbes. La main sûre et la sonde passent dans l'arbre ; en attendant, leurs versions au mana restent, et on note l'exception | L'arbre, la règle « une source par capacité » |
| D3 | Qu'est-ce qui porte la partie après le fond ? | L'arbre d'abord (il rend les vies tardives indolores), puis la pierre comme puits de fin : dédier du mana, par paliers de contenance (voir D6) | La fin de partie |
| D4 | Durée visée : accepter ≈ 85 h, ou découpler chapitres et renaissances ? | Accepter la durée mesurée ; découpler seulement si le récit a plus de chapitres que de cycles | Le système de récit |
| D5 | La réserve du GDD (portails, temples et alliés, miracles, égarés) : dedans ou dehors pour une première version complète ? | Dehors, sauf les égarés comme contenu des lieux IV et V. Le noyau v1.0 a fermé le monde à « une espèce est un générateur » ; portails et temples rouvrent placement et population | Le périmètre |
| D6 | La pierre : barre à remplir (Codex) ou verrou de contenance (GDD) ? | Les deux en une : la pierre accepte du mana **jusqu'à la contenance du moment**, par offrandes ; le compte d'offrandes fait avancer les actes VII à IX. Aucune production en retour | L'acte final |
| D7 | Le `PlancherDeCadence` parqué | Le régler en ajoutant des succès d'actes tôt (les bonus et les techniques en fournissent une dizaine) plutôt qu'en touchant au débit du héros | La première demi-heure |

---

## 6. Ce qu'il reste à construire

Taille : S (une séance), M (quelques séances), L (un chantier avec mesure).

### Lot 0 — Remettre la mesure d'aplomb · S — **fait le 2026-11-13**, sauf les deux dernières lignes

Résultat : `RESULTATS.md`, Finding 7. Reste la dette courante à revérifier dans le C#.

- Relancer tous les tests ; consigner le compte.
- Une politique du simulateur qui **achète les bonus et les techniques** ; mesurer leur effet sur
  la durée des cycles et la part bloquée ; régler les graines de `RegistreDesBonus`.
- Écrire la télémétrie sur disque (un `ICollecteur` qui ajoute une ligne JSON par renaissance, à
  côté de la save) pour pouvoir enfin **mesurer une vraie partie**.
- Les cinq broutilles de la « dette courante » de la roadmap, à revérifier dans le C#.
- Mettre `ROADMAP.md` et le README à jour (ils datent d'avant Débloquer).

### Lot 1 — Le Souffle touche le temps (décision D1) · M — **fait le 2026-11-13**

Résultat : `RESULTATS.md`, Finding 8 (91 h → 45 h). Le terme s'appelle `TempsDuSejour`, la portée
`Sejour`, l'identifiant `amelioration-sejour`. Reste à trancher la durée totale visée.

- Nouveau terme de confort `TempsDuSejour` ; une portée d'amélioration de plus (`Sejour`), rangs
  bornés, prix en Souffle.
- `Reducteur.TauDuSejourSecondes` le lit. Test d'équivalence de pas avec un achat entre deux pas.
- Amender `CanonTests` et le Codex §5 (la ligne dure).
- Relancer `MesurePolitiquesDAmeliorationTests` : les politiques doivent enfin donner des durées
  différentes. Régénérer la parité seulement si une partie de référence achète l'amélioration.

### Fait le 2026-11-13, hors lots — ce qui manquait pour un bon idle

- **Achats en lot** : `Reducteur.AmeliorerJusquAuSeuil` (monter jusqu’au prochain seuil) et
  `Reducteur.AcheterToutesLesAmeliorations` (tout le Souffle payable) ; `Economie.LotDeNiveaux`
  dit ce qu’un lot achèterait. Exposés par `Partie.MonterJusquAuSeuil` et `Partie.ToutAmeliorer`.
- **Rapport de retour** : `Partie.Rapport` (`RapportDeRetour`) — mana gagné, contenance gagnée,
  stock plein, descente rouverte, absence plafonnée, succès tombés pendant l’absence (désormais
  annoncés au retour).
- **Flux d’événements** : `Partie.EvenementsDeJeu`, déduit de deux états par `Jeu/Evenements.cs` —
  creux ouvert, espèce convaincue, crans montés, seuil franchi, héros grandi, bonus, amélioration,
  succès, eau troublée, contenance pleine, descente bloquée ou rouverte, renaissance ; ceux d’un
  verbe sont marqués `Automatique`. Rien ne s’y abonne encore : c’est la prise pour le son et
  les effets.

### Lot 2 — L'arbre de technique permanent (décision D2) · L — **transcrit le 2026-11-13**

Résultat : `RESULTATS.md`, Finding 9.

- **Données** : trente nœuds, six branches (`NoeudsTechnique.cs`). Trois lectures du noyau sont
  notées en tête du fichier : les remises d'une branche sont des paliers, « Réputation » est une
  remise sur le cran, « Pondre à l'heure » passe par le terme `GainDeDensite`.
- **Pas d'action d'achat** : `Technique.Apprendre` est appelée par `Creuser`, `Ameliorer`,
  `Grandir`, `Debloquer`, `Renaitre` et le crédit hors ligne. Dans une branche l'ordre est celui
  des rangs et les points ne servent qu'à elle : il n'y a rien à choisir.
- **Points** : calendriers pour Recrutement et Renaissance (`CALENDRIER_PAR_BRANCHE_BORNEE`),
  couples (A, B) pour Creusement, Amélioration et Entretien (`COUPLES_A_B`). Construction : rien.
  Le compteur d'Entretien est `HeuresHorsLigneCreditees` (`Technique.CompteurDe`).
- **Verbes** (`Automatismes.cs`) : reconvaincre une espèce connue, rouvrir une galerie connue,
  la main sûre, tout le monde suit, et le chemin connu, qui vaut les autres et rachète les bonus.
  « Connu » : le palier porte la densité d'une vie passée.
- **Hors ligne** : avec « Le peuple continue », les verbes jouent AU RETOUR, après le pas unique,
  sur le stock amassé (`HorsLigne.LePeupleContinue`). Le pas reste homogène ; rien ne s'est
  composé en route.
- **Effets chiffrés** : `Economie.NiveauDeDepart` (l'élan), `Economie.Contenance` ×2 par la
  coquille (hors de `ContenanceAFixer`, pour ne pas se composer), `Densite.AlphaCourant`.
- **Reste** : voir `ROADMAP.md`, chantier 3.

### Lot 3 — Le récit dans l'état · M

- Un registre `Chapitres` (acte, déclencheur, texte dans `Textes.cs`), relu en fin de pas comme
  les succès, consigné dans l'état permanent avec son registre de voix figé.
- Les événements qui ne se déduisent pas : le relais (ouvre `Dialogue`), la pierre trouvée,
  l'esprit venu. Ce sont eux qui entrent dans l'état, pas le palier de voix.
- Le journal (qui raconte) séparé de la bibliothèque (qui inventorie), comme le veut le GDD §14.8.
- Si D4 choisit le découplage : la cadence des chapitres devient une donnée du registre.

### Lot 4 — Les lieux III à VI, un par un · L chacun, surtout du contenu

Pour chaque lieu, la même liste, et rien dans le code hors des registres :

- Nom (charte phonétique, de plus en plus sombre), type de mana, quatre espèces nommées (trois
  pour le VI), textes.
- Sa marque sur le héros (luminescence, minéralisation, épaississement…), sa palette, son décor.
- Quatre bonus de lieu, ses succès (seuils engendrés par gabarit, franchissements et actes à la
  main).
- `PALIERS_LIVRES` relevé, parité régénérée et consignée dans `RESULTATS.md`.
- **Condition** : le lieu précédent mesuré en vraie partie (Lot 0).
- Lieux IV et V : les **égarés** (des espèces qui coûtent et produisent presque rien, avec leurs
  textes) et deux ou trois bêtes du lieu VI en avance.
- Les ~6 divergences prévues par le noyau v1.0 restent hors périmètre tant que les temples le sont.

### Lot 5 — La fin (décisions D3, D6) · M

- La pierre : action `Dedier(mana)`, état permanent `offrandes`, paliers qui déclenchent les
  actes VII à IX ; aucun effet sur la production.
- Le relais ouvre `Dialogue` ; la correction d'échelle est un chapitre.
- Ce que le jeu fait **après** l'acte IX (continuer, ou générique) est à décider.

### Lot 6 — Le confort de vraie application · S à M

- Rapport de retour détaillé (ce qui a été gagné, borné, automatisé).
- Export et import de la save (texte), et une copie de secours tournante.
- Notifications locales (« ta contenance est pleine »), en respectant « ne jamais punir l'absence ».
- Modules Android et iOS, premier build téléphone, budget de lumières mesuré sur un vrai appareil.
- Les 5 verbes réservés aux succès (budget de 15, dont 10 à l'arbre) : toujours pas alloués.

### Hors périmètre proposé (réserve du GDD)

Portails, temples et alliés, miracles, défiscalisation jouée, fidèles arrêtés, mortalité et legs,
monétisation. Conçus, jamais construits, et en tension avec le noyau v1.0 (pas de population, pas
de placement). À rouvrir après une première version complète.

### L'ordre, et pourquoi

```
Lot 0 (mesure)  →  Lot 1 (Souffle)  →  Lot 2 (arbre)  →  Lot 4, lieu III  →  …
                         │                                    ▲
                         └── fixe le rythme contre lequel ────┘
                             l'arbre et les lieux se calibrent
Lot 3 (récit) et Lot 6 (confort) : en parallèle, ils ne touchent pas à l'économie.
Lot 5 (fin) : après le lieu VI.
```

---

## 7. Ce que l'écran peut lire et appeler aujourd'hui

Pour travailler le frontend sans ouvrir le noyau.

| Besoin | Appel |
|---|---|
| Faire une action | `Partie.Creuser()`, `Convaincre(id)`, `Monter(id)`, `Grandir()`, `AcheterUnBonus(id)`, `AcheterUneAmelioration(id)`, `Renaitre()`, `Toucher()` (rend le gain ; dix par seconde au plus) |
| Débit total, par espèce | `Economie.ProductionTotaleParSeconde`, `ProductionDeLEspece` |
| D'où vient un débit | `Economie.DetailDeCaptation`, `DetailDuHeros`, `DetailDeLAmbiant`, `DetailDuToucher` |
| Tous les prix | `Economie.CoutDeDescente`, `CoutDeDeblocage`, `CoutDeNiveau`, `CoutDeCroissance`, `CoutDeBonus`, `CoutDAmelioration` |
| La jauge | `Economie.Contenance`, `PartDeContenance`, `EauTroublee`, `EstSature`, `EstBloque`, `ToutEstCreuse` |
| Ce que renaître rapporterait | `Renaissance.GainDeSoufflePrevu` |
| Bonus et maîtrise | `Economie.MaitriseDuLieu`, `BonusOuvert`, `BonusAuMaximum`, `RangDeBonus` |
| Succès | `RegleDesSucces.SuccesListables`, `ProgressionVersLeSucces`, `AssisesAtteintes` ; les annonces en attente sont dans `Partie` |
| Voix | `Voix.PalierDe`, `Voix.AuMoins` |
| Retour d'absence | `Partie.Retour` (secondes créditées), `OublierRetour()` |
| Scène | `Jeu/Scene/VueDeScene` (pure) : ce que la scène doit montrer pour un état donné |

Ce que l'écran **ne peut pas encore** demander : un événement « il vient de se passer ceci »
(achat automatique, cran de palier franchi, creux ouvert). Aujourd'hui il compare deux états.
Un petit flux d'événements rendu par `Partie.Avancer` serait le meilleur service que le backend
puisse rendre à l'habillage (retours visuels, sons).

---

## 8. Pour que le jeu ait de la gueule : ce que le backend peut fournir

Ce sont des suggestions, pas du périmètre décidé.

- **Un flux d'événements de jeu** (voir §7) : c'est ce qui permet les nombres qui jaillissent, la
  roche qui cède, l'éclat d'un palier de cran, et tout le son.
- **La voix pilote déjà l'écran** : pente sans chiffres, signes avec ordres de grandeur,
  directives avec chiffres complets. Le noyau le dit ; à vérifier que chaque panneau le respecte.
- **L'eau troublée à 85 %** est un état exposé : c'est le signal visuel le plus important du jeu
  (« tu peux rentrer »), il mérite mieux qu'un voile.
- **Le son** : la tuyauterie est prête et vide. Une boucle d'ambiance par lieu et cinq ou six
  effets changeraient plus la sensation que n'importe quel sprite.
- **Les marques du héros** : une seule existe (branchies). Celle du Gour (membranes) est prévue ;
  la suivre côté données ne coûte rien.
- **Les captures de contrôle** (`Logs/captures/`) datent du 2026-10-08, avant l'onglet Débloquer :
  à régénérer avant tout travail d'habillage.
