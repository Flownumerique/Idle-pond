# IdlePond — application de l'amendement v1.1

Tier 2. Compte rendu de l'application des cinq décisions de l'amendement v1.1
au code des jalons v0.1 et v0.2. Ne crée aucun canon.

---

## 1. La vérification ordonnée par le §2.C, d'abord

> « Ouvre `constantes.ts`, confirme que la table y est cumulée. **Si elle valait
> ×1024, arrête-toi et signale-le** — tout le calibrage serait à refaire. »

**La table était cumulée.** Le jalon v0.1 avait retenu la lecture « ×16 au
seuil 100 » — la plus littérale — derrière une constante nommée, et l'avait
signalée comme décision ouverte. L'amendement la confirme.

`D = 2.31` est donc sauf, et aucun recalibrage n'est nécessaire de ce chef. Un
test le verrouille désormais : `seuils.test.ts` affirme la table, et affirme
explicitement que cent individus ne valent **jamais** ×1024.

---

## 2. Cadrage

Le §6 de l'amendement décrit une session à froid et demande « le jalon v0.1, et
rien d'autre ». Les jalons v0.1 et v0.2 étaient déjà livrés et poussés. Les
cinq décisions ont donc été appliquées **au code existant**, ce qui change du
travail déjà fait plutôt que d'en refaire ; supprimer l'assise I jouable pour la
reconstruire à l'identique n'aurait servi personne.

Les cinq tests du §6 existent tous : trois étaient déjà là (pureté,
déterminisme, équivalence de pas), deux sont neufs (seuils, contenance), et le
critère « une partie sans UI atteint l'éclosion 2 en headless » est vérifié sur
le monde **livré** — la Noue et ses six paliers.

**75 tests, tous verts.** `tsc -b`, `eslint`, `vite build` propres.

---

## 3. Ce que chaque décision a changé

### 2.A — `θ` porté au canon

`densiteExposant()` rend `θ / α`, avec la chaîne de dérivation recopiée en
commentaire. `θ = 0.8` en graine, borné `[0, 1]` et vérifié par un test.

Deux corrections de fond en découlent :

- **Le gain de densité vaut `pointe^α`**, pas le logarithme que la v0.1 avait
  mis en graine faute de `θ`. La chaîne complète tient maintenant : la pointe
  ×`g^paliers` par éclosion, la densité ×`g^(paliers × α)`, le multiplicateur
  ×`g^(paliers × θ)`.
- La densité se pose par `max`, jamais par affectation : un cycle plus court que
  le précédent laisse moins de charge derrière lui et ne doit pas pouvoir
  défaire l'acquis. **La densité ne redescend jamais.**

`[P]` — le §6.5 de la v1.0 fait retourner la densité dans la vitesse de
repeuplement, l'amendement §2.B la fait retourner dans l'acquis de séjour. Les
deux canaux coexistent dans les documents et aucun n'annule l'autre. Le
repeuplement porte donc un exposant **nommé et volontairement doux**, distinct
de `θ/α` : appliquer le multiplicateur plein des deux côtés compterait deux fois
la même compensation. À trancher en v0.3.

> **Périmé le 2026-09-11.** La densité ne retourne plus ni dans le
> repeuplement (V11, §6) ni dans l'acquis de séjour (§3, « 2.B », sous-section
> « Amendé le 2026-09-11 ») : elle agit sur la production seule. La question
> du double compte ne se pose plus.

### 2.B — la contenance monte par l'acquis de séjour

`EtatCycle.acquisDeSejour`, accumulation saturante vers `A∞` dont le temps
caractéristique décroît quand la densité monte. Le facteur ×47,1 **n'est écrit
nulle part** dans le code de l'éclosion : il émerge de `A∞ = 47.6` et
`τ₀ = 0.87 h`, et le test le vérifie à 2 % près.

> **Amendé le 2026-09-11** : le temps caractéristique ne décroît plus avec la
> densité, il vaut `τ₀`, constant. Voir la sous-section datée ci-dessous.

Les deux graines se tiennent l'une l'autre, et c'est vérifié : trois heures de
séjour portent l'acquis à 46,1, donc la contenance à ×47,09 contre ×47,10 visé.
Le `t₉₀` tombe à 2,00 h. Changer l'une sans l'autre casse la cible.

L'effet secondaire recherché tient aussi : cent heures de séjour au lieu de
trois ne rapportent que 3 % de contenance en plus. Passé la saturation, rester
ne rapporte plus de profondeur, seulement de la Foi — la seule vraie décision du
joueur est réelle, et un test l'affirme.

#### Amendé le 2026-09-11 — le temps du séjour ne dépend plus de la densité

Le temps caractéristique de l'acquis était `τ₀ / mult_densité(densité)`. La
densité vaut `pointe^α` et croît sans borne ; `τ` s'effondrait donc avec elle.
Avec `θ/α = 4/3`, `d₀ = 1` et `τ₀ = 0,87 h`,
`t₉₀ = τ₀ × ln 10 / (1 + densité)^(4/3)` :

| densité | 0 | 1 | 3 | 10 | 100 | 10⁶ |
|---|---|---|---|---|---|---|
| `t₉₀` du séjour | 2,00 h | 0,80 h | 0,32 h | 0,082 h | 0,0043 h | ~0 |
| pointe qui la laisse | — | 1 /s | 6 /s | 46 /s | 2 154 /s | 10¹⁰ /s |

Mesuré sur quinze cycles, politique par défaut du simulateur : le premier
cycle culmine à 27,8 mana/s et laisse une densité de 7,3, qui met le `t₉₀` du
deuxième cycle à 0,12 h ; le deuxième culmine à 2,6·10⁶ mana/s et laisse une
densité de 7 100, soit un `t₉₀` de 0,05 s au troisième. Les deux premiers
cycles durent 4 h, **tous les suivants de 4 à 14 secondes**, et l'acquis vaut
exactement `A∞` à chaque éclosion à partir de la deuxième (0,990 à la
première).

Un premier relevé, pris avant le 2026-09-10 sous la forme
`max(1, densité)^(θ/α)` que le code avait alors, donnait 0,09 h de `t₉₀` sur
les cycles 2 à 6. Recalculé sous la loi actuelle, l'effondrement est de même
nature : 0,082 h à densité 10, 0,12 h à la densité que laisse réellement le
premier cycle. La loi de contenance ne lisait plus la durée du séjour : elle
rendait un forfait de ×48,6 par éclosion, sous le nom de séjour. Le pilier
Tier 0 « le plafond ne monte que par séjour prolongé » était devenu décoratif.

**L'erreur.** Le §6 ci-dessous (V11) gardait ce canal au motif que la densité
y passe « par un rapport que la saturation borne ». La saturation borne la
VALEUR de l'acquis — `A∞` —, elle ne borne pas le TEMPS pour l'atteindre. Et le
canal masquait son propre effondrement : la contenance ne lit l'acquis qu'à
l'éclosion, donc toujours après saturation, alors que le repeuplement retiré
montrait le sien dans un temps de convergence observable.

**Décision : `τ = τ₀`, constant.** La durée de cycle visée est constante par
construction — coûts et production montent tous deux en `g^paliers` —, et le
temps qui la jauge doit l'être aussi. Même simulation après correction : chaque
cycle dure un intervalle de check-in (4 h), l'acquis vaut 0,990 de `A∞` à
l'éclosion, ×48,1 par cycle, 4,4 paliers par cycle en moyenne.

L'effet secondaire recherché est conservé — cent heures au lieu de trois
rapportent toujours 3 % de contenance —, mais il faut dire à quoi il tient :
**il ne tient QUE parce que `τ₀` est calibré sur la durée de cycle.** Avec un
acquis qui sature en quelques secondes, il était vrai à vide : rester trois
heures au lieu de quarante secondes ne rapportait rien, et la décision « rester
ou partir » se réduisait à « partir tout de suite ».

**Une loi exponentielle a été mesurée et rejetée le 2026-09-11.** La loi
`contenance × C^(acquis / A∞)`, avec `C = g^4.4`, a été essayée par-dessus
`τ = τ₀`. Elle ne vise `C` qu'à saturation : à 3 h de séjour elle rend ×41,66
contre ×47,09 visé, et 200 h rapportent 1,130 fois ce que rapportent 3 h,
contre 1,032 pour la loi linéaire. Sur quinze cycles simulés, elle donne 4,356
paliers de marge par cycle au lieu de 4,4. La raison tient en une ligne : avec
`τ₀` calibré sur le cycle, un séjour nominal ne sature pas, donc une loi qui
n'atteint sa cible qu'à saturation la manque à chaque cycle réel. La loi reste
linéaire, `× (1 + acquis)`.

La densité garde un débouché, et un seul : la production, par
`mult_densité = (1 + densité / d₀)^(θ/α)`, au même titre que le multiplicateur
de profondeur. C'est la grandeur que la chaîne de dérivation du §2.A écrit pour
`θ`.

#### Amendé le 2026-09-16 — le temps du séjour croît avec la profondeur

`τ` n'est plus constant : `τ = τ₀ × c^profondeur_max_atteinte`, où `c` est
`CROISSANCE_DU_SEJOUR_PAR_PALIER`.

**Ce que `τ` constant produisait, mesuré.** La tâche 12 a simulé 45 cycles sous
la politique du joueur optimal. Les 45 durent **exactement 2,617 h**, sans
exception — soit `τ₀ ln 20`, le temps qu'il faut à l'acquis pour atteindre les
95 % de `A∞` qui décident du départ, plus un pas de 60 s. Le joueur est bloqué
au bout de 21 min au premier cycle et d'une à deux minutes ensuite : **98 à
99 % de chaque cycle se passe à attendre l'acquis**, et l'économie ne décide
plus de la durée d'aucun cycle.

La conséquence est celle qui force cet amendement : le rapport entre la durée
du dernier cycle et celle du premier vaut **1,000 pour toute valeur de `θ`, de
l'échelle, ou de n'importe quel autre réglage d'économie**. La durée est
plafonnée par le séjour, pas par l'économie. Le calibreur du §12 n'avait donc
aucune prise sur la forme de la courbe — il aurait poussé `θ` contre sa borne
sans rien régler, ce que le spec de la feuille de route prédisait déjà.

**Ce que ce n'est pas.** La loi révoquée cinq jours plus tôt (sous-section
ci-dessus) DIVISAIT `τ` par le multiplicateur de densité, donc par une grandeur
qui vaut `pointe^α` et croît sans borne : `t₉₀` tombait de 2 h à 0,05 s en trois
cycles et la contenance dégénérait en forfait. Ici `τ` est MULTIPLIÉ, et par
l'exponentielle d'une quantité **entière et bornée** — les 62 paliers du monde.
Le risque est l'autre, et il est borné de la même façon : des cycles tardifs
trop longs. C'est pourquoi `c` est un réglage résolu contre une cible de durée
totale, et non une graine posée à la main.

**Pourquoi la profondeur ATTEINTE, et pas celle qui est ouverte.** C'est un
acquis de l'être, pas de la plongée : on ne redevient pas jeune en remontant.
Et `profondeurMaxAtteinte` est monotone (Tier 0), donc `τ` l'est aussi — un `τ`
qui pourrait redescendre ferait de l'éclosion un moyen d'accélérer l'acquis, et
la décision du §6.4 deviendrait dégénérée. Elle ne bouge que sur un ACTE du
joueur, jamais pendant un tick : la forme exponentielle de l'acquis reste exacte
pour n'importe quel `dt`, le §5.2 tient, et un test d'équivalence de pas le
garde.

**La croissance s'arrête au fond du monde.** `profondeurMaxAtteinte` plafonne à
62 paliers, atteints vers le quatorzième cycle. La courbe s'allonge donc pendant
la DESCENTE, puis redevient plate pour les cycles suivants, à `τ₀ × c^62`. Ce
n'est pas un défaut de la loi, c'est la forme qu'elle a : le jeu s'allonge tant
qu'il reste du monde à ouvrir.

**Ce qui ne change pas.** `A∞` et la loi de contenance sont intacts : `τ` change
le TEMPS, jamais la VALEUR. L'acquis sature toujours vers le même plafond, et
l'effet secondaire du §2.B tient toujours — passé la saturation, rester ne
rapporte plus que de la Foi, à n'importe quelle profondeur, puisque la
saturation arrive toujours au bout de trois `τ`.

**`c` vaut 1,02**, fixé le 2026-09-16 par décision contre cette table mesurée
sur `simuler(45)` — joueur optimal, donc temps actif égal au temps écoulé :

| `c` | cycle 1 | cycles 15 à 45 | dernier/premier | partie complète |
|---|---|---|---|---|
| 1,000 | 2,62 h | 2,62 h | ×1,0 | 118 h |
| **1,020** | **2,92 h** | **8,90 h** | **×3,1** | **353 h** |
| 1,030 | 3,08 h | 16,30 h | ×5,3 | 621 h |
| 1,050 | 3,43 h | 53,68 h | ×15,6 | 1 941 h |

La courbe triple, le cycle 1 tombe à 2,92 h — la cible des 3 h du §12, atteinte
sans toucher à l'échelle. Au-delà de 1,03, un cycle de fin dépasse 16 h : ce
n'est plus un cycle.

> **Note du 2026-09-16, après l'amendement v1.3 et la décision des 14 cycles.**
> La colonne « partie complète » de cette table a été mesurée sur 45 éclosions,
> le chiffre d'alors. La descente s'arrête maintenant au cycle 13, et la partie
> complète fait **~82 h** à `c = 1,02`, contre ~37 h à `c = 1`. Ce qui a motivé
> le choix de `c` est intact : le rapport de la dernière durée à la première
> vaut ×2,85 sur la descente (3,12 h → 8,90 h) au lieu de ×1,0.

**Ce que la courbe v1.2 déplace, mesuré.** Un seul chiffre du dépôt bouge
vraiment, et c'est le finding 2 : l'absence de 4 h ne double plus le temps
calendaire, parce qu'elle tient désormais DANS un cycle au lieu de le dépasser.
Sur 13 cycles — optimale 67,9 h ; relâchée à 4 h 104,0 h (×1,53), à 8 h 184,0 h
(×2,71), à 24 h 504,0 h (×7,43). Le finding lui-même est intact : l'intervalle
de relevé reste le seul réglage qui gonfle le temps calendaire, et il le gonfle
d'autant plus qu'il est long. La Foi et la densité à 45 cycles ne bougent, elles,
qu'à la quatrième décimale (1,6512e+66 → 1,6505e+66) : l'économie est invariante
d'échelle, et rallonger les cycles ne la déplace pas.

#### Amendé le 2026-09-16 (v1.3) — le plafond monte PENDANT le cycle

`contenance = contenanceMana × (1 + acquis_de_séjour)`, lue à chaque instant.
L'éclosion ne fait plus que FIXER ce que le cycle portait déjà — la valeur
banquée est identique à celle d'avant, au bit près.

**Le défaut, mesuré.** Le plafond ne montait qu'à l'éclosion : il était donc
gelé pendant toute la vie, et le palier suivant coûtait plus que ce que le héros
pouvait PORTER — pas plus que ce qu'il avait. Rien dans la vie courante ne
pouvait changer cela. Ce que le joueur vivait, par cycle :

| cycle | durée | dernier palier ouvert | part du cycle gelée | production gagnée ensuite |
|---|---|---|---|---|
| 1 | 2,92 h | 21 min | 88,0 % | ×8,4 |
| 5 | 4,20 h | 2 min | 99,2 % | ×7,0 |
| 15 | 8,90 h | 1 min | **99,8 %** | **×1,1** |
| 30 | 8,90 h | 1 min | 99,8 % | ×1,0 |

Passé la première minute : aucun palier, aucune espèce, et à partir du cycle 15
plus de production non plus. Ce n'est pas un jeu incrémental, c'est un minuteur
avec un décor. Le §2.2 promet un blocage DOUX — « il peut continuer à jouer
indéfiniment » —, et il était devenu dur.

**Après.** Le plafond monte au fil du séjour, donc le joueur descend au fil du
cycle :

| cycle | durée | dernier palier ouvert | part du cycle gelée | production gagnée ensuite |
|---|---|---|---|---|
| 1 | 3,12 h | 31 min | 83,4 % | ×36 |
| 2 | 3,48 h | 86 min | 58,9 % | ×203 |
| 4 | 4,13 h | 156 min | 37,1 % | ×170 |
| 6 | 4,90 h | 54 min | 81,6 % | ×131 |

**Ce qui ne change pas.** Tier 0 §8 tient : le plafond ne monte QUE par séjour
prolongé — il monte simplement au fil du séjour au lieu d'être versé en bloc à
la sortie. Le ×47,1 du cycle nominal reste un RÉSULTAT de `A∞` et `τ₀`, et la
valeur banquée à l'éclosion est inchangée. La décision du §6.4 reste réelle et
devient même plus nette : quand l'acquis sature, le plafond cesse de monter, la
descente s'arrête pour de bon, et rester ne rapporte plus que de la Foi.

**L'équivalence de pas tient, et ce n'est pas une chance.** Le plafond varie
maintenant DANS le pas, ce que le §5.2 regarde de près. Le stock reste exact en
un seul pas parce que la production est constante pendant le pas et que le
plafond est concave croissant : la droite du stock ne croise la courbe du
plafond qu'une fois, donc `min(stock + production × dt, plafond(fin du pas))`
est la solution exacte, et non une approximation. Le tick lit donc le plafond
APRÈS avoir intégré l'acquis. Le test d'équivalence le vérifie sur une fixture
où le plafond mord en cours d'intervalle.

**Un défaut latent trouvé par la même occasion.** Le joueur headless de
`tests/partie-headless.test.ts` lisait `permanent.contenanceMana` au lieu
d'appeler `contenance()` : tant que les deux valaient la même chose, la copie
passait inaperçue. Elle a cessé de valoir la même chose, et la partie ne pouvait
plus éclore du tout. La règle vit dans le noyau ; on l'appelle, on ne la recopie
pas.

#### Décidé le 2026-09-16 — la descente n'est pas toute la partie

Le plafond continu règle le corps du cycle ; il ne crée pas de monde. Mesuré
sous la loi v1.3 : le héros gagne 10 paliers au premier cycle puis 4 à 5 par
cycle, et le fond des 62 paliers tombe au **cycle 13**. Le quatorzième n'ouvre
plus rien — c'est la moisson, la vie qui convertit en Foi et en densité la
profondeur que la treizième a atteinte. Au-delà, plus rien ne s'ouvrirait : les
niveaux seuls ne rendent que ×1,1 de production par cycle.

Deux décisions en découlent.

**`NOMBRE_D_ECLOSIONS_VISE` vaut 14, et mesure la DESCENTE, pas la partie.** Il
a valu 15 — le nombre de chapitres du récit, confondu avec les éclosions —, puis
45, une décision de durée prise avant qu'on mesure ce que le monde peut nourrir.
45 était un chiffre qui mentait dans toutes les mesures : le monde est épuisé
trois fois plus tôt, et les 31 cycles restants ne voyaient plus rien s'ouvrir.
La descente complète fait ~82 h de jeu actif sous le joueur optimal.

**Passé la treizième, la partie tient sur d'AUTRES axes : la Foi et l'arbre de
technique** (phases 4 et 5). C'est la réponse d'un incrémental — quand un axe
sature, la monnaie de prestige en ouvre un autre —, et le plan la contient déjà.
Ce qui reste à faire n'est donc pas une loi de contenance : c'est de rendre ces
deux axes réels, à commencer par les six couples (A, B) qui décident à quel
rythme une branche de technique s'ouvre. Ils doivent être résolus contre une
partie de 14 cycles, et non de 45.

### 2.C — seuils cumulés, sur l'effectif

La colonne est renommée `multiplicateurCumule` partout. Le joueur achète de la
**place** — `EtatBanc.place`, `coutDePlace`, `acheterPlace`, `cout_place`,
`place_de_depart`, `place_de_banc` — et « niveau » a disparu des identifiants.

Le drapeau permanent des cent individus est un état conservé
(`especesAyantAtteintCent`), distinct du multiplicateur de seuil, qui se
reperd à l'éclosion. Un test vérifie les deux comportements côte à côte.

**C'est cette décision qui a coûté le plus de travail, et pour une raison qui
n'était pas visible depuis le document.** Le multiplicateur se lisant désormais
sur l'effectif, il change **à l'intérieur** d'un intervalle — et l'intégrale de
production perd sa forme fermée. Le figer au début du pas fait diverger 480 pas
de 60 s d'un pas de 8 h : les petits pas franchissent le seuil tôt et
produisent davantage. **L'équivalence de pas est tombée.**

Elle est rétablie sans rien concéder, par deux partitions analytiques :

- **Par banc**, l'effectif est monotone, donc chaque seuil est franchi au plus
  une fois, à un instant qu'on résout à la main
  (`t = −ln((s − C)/(e₀ − C))/k`). L'intervalle se découpe en au plus cinq
  morceaux à multiplicateur constant, chacun d'intégrale fermée.
- **Le drapeau des cent individus est global** : il change le taux de tous les
  bancs, y compris d'autres espèces, donc il ne s'intègre pas banc par banc. Le
  pas est **coupé** à l'instant exact où il tombe. L'effectif d'une espèce est
  une somme d'exponentielles, qui ne s'inverse pas ; l'instant est donc résolu
  par dichotomie — au plus une fois par espèce et par partie.

Ni l'une ni l'autre n'est l'itération sur une file d'événements que le §1
interdit : ce sont des partitions bornées, dont le coût ne dépend ni de `dt` ni
de l'histoire de la partie. Deux tests neufs les prouvent sur des intervalles
qui franchissent effectivement les seuils — sans quoi ils ne prouveraient rien.

### 2.D — les succès entièrement du côté technique

`EffetDeSucces` porte les trois variants de l'amendement, et **aucun variant
`production`**. Le typage rend la faute inexprimable ; un test la vérifie tout
de même sur le registre, parce qu'un registre peut un jour venir d'ailleurs que
du compilateur.

C'est la lecture que le jalon v0.2 avait retenue en la signalant (`V7`), au
motif qu'elle était la seule des deux qui ne puisse pas être fausse par excès.
Elle est confirmée. Les vingt-neuf effets chiffrés de la Noue sont passés de
`facteur: 0.98` à `part: 0.02`, et `reduction_technique` existe comme terme.

`[P]` — les ~5 verbes de succès restent **non alloués**. Les onze `CapaciteId`
du §7.3 appartiennent toutes à l'arbre, et le §7.5 règle 1 veut qu'une capacité
ait exactement une source : en donner un à un franchissement demanderait
d'inventer une capacité. À trancher en v0.4 avec le budget du §7.6.

### 2.E — la charte phonétique et les noms de la Noue

`la Noue` (identifiant `noue`), `le vairon`, `la loche`, `l'épinoche`. Les
textes provisoires ont suivi, les identifiants de succès aussi
(`seuil-vairon-100`), et une migration de save les reprend.

`tanche` est **réservée** : un test vérifie qu'elle n'est assignée à aucun
générateur, et la construction du registre d'espèces lève si elle l'était.

Le placement s'améliore au passage : trois espèces sur six paliers font deux
bancs chacune, là où dix paliers en donnaient quatre au vairon et affichaient
quatre fois le même nom (`V10` du jalon v0.2, refermé).

---

## 4. Ce que l'amendement a corrigé dans la courbe

Mesures avant / après, quinze cycles, même politique de simulateur.

| | v0.2 | après v1.1 | cible |
|---|---|---|---|
| Paliers par cycle | ≈ 5–6 | **≈ 4,4** | 4,4 |
| 62 paliers atteints au cycle | 12 | **14** | 15 |
| Cycle 1 | 4,8 h | **4,9 h** | 3 h |
| Jeu actif total | 514 h | **248 h** | ~38 h |
| Fraction en redescente | 55–65 % | 55–65 % | — |

**La loi de contenance dérivée du §2.B fait ce qu'elle promet** : la profondeur
par cycle tombe sur sa cible et les 62 paliers durent presque les quinze
éclosions visées. Le temps actif total est divisé par deux.

Deux écarts demeurent, et ce sont les mêmes qu'aux deux jalons précédents :

- **248 h actives contre ~38 h visées.** Le §5.4 rappelle que sous jeu optimal
  les durées sont plates (~2,7 h) et que 15 × 2,7 ≈ 40 h. Les durées mesurées
  ici ne sont pas plates (4,9 h → 52,7 h) parce que la politique du simulateur
  n'est pas optimale : elle broie de la place dans la queue de chaque cycle et
  attend quatre heures de patience avant d'éclore, au lieu d'arbitrer entre
  partir pour la profondeur et rester pour la Foi. **L'écart mesure la
  politique, pas l'économie**, et le corriger est un chantier de v0.3.
- **La redescente occupe toujours 55 à 65 % de chaque cycle** — risque n° 1 du
  §15, inchangé. Il tient probablement à la même cause.

---

## 5. Décisions ouvertes

Refermées par l'amendement : `V1` (seuils cumulés), `V2` (`θ`), `V3` (loi de
contenance), `V7` (frontière des succès), `V9` pour l'assise I (noms), `V10`
(placement).

Restent ouvertes :

| # | Question | Jalon |
|---|---|---|
| V8 | Les ~5 verbes de succès ne sont pas alloués — toutes les capacités nommées appartiennent à l'arbre | v0.4 |
| V9′ | `[P] P3` pour les assises II à VI | avant v0.5 |
| ~~V11~~ | ~~La densité agit-elle sur la vitesse de repeuplement **en plus** de l'acquis de séjour ?~~ **Tranché le 2026-09-08 : non, découplée.** Voir la note ci-dessous | ~~v0.3~~ |
| V12 | Le +3 % des drapeaux s'additionne-t-il entre espèces ou se compose-t-il ? L'addition est retenue : elle ne surprend pas à vingt et une espèces | v0.3 |
| V13 | Six paliers à la Noue contre « 62, distribution plate » : les deux ne se tiennent que si « plate » qualifie la distribution **par cycle**. C'est la lecture retenue, avec 56 paliers sur cinq assises | avant v0.5 |

---

## 6. V11, tranché le 2026-09-08 — et ce que la mesure a réfuté au passage

**Décision : la densité est découplée du repeuplement.** Elle travaille par
l'acquis de séjour (§2.B), et par rien d'autre. `vitesseDeRepeuplement` rend
`k` seul ; `EXPOSANT_REPEUPLEMENT_DENSITE` est supprimé.

Le canal retiré était bien celui de trop. La densité vaut `pointe^α`, donc elle
croît avec la production **sans borne** ; le §2.B la fait passer par un rapport
que la saturation borne, lui tient. À exposant nu, `τ` tombait de 300 s à
10⁻⁴ s en quinze cycles : la population devenait instantanée dès le deuxième, et
avec elle disparaissait le délai entre l'achat d'une place et son effet —
c'est-à-dire ce que le GDD §7.2 décrit comme la boucle elle-même.

> **Corrigé le 2026-09-11 : le canal gardé ne tenait pas davantage.** La
> saturation borne la valeur de l'acquis, pas le temps pour l'atteindre ; le
> `τ` du séjour s'effondrait exactement comme celui du repeuplement — `t₉₀` de
> 2 h à 0,12 h au deuxième cycle, à 0,05 s au troisième —, et l'effondrement
> était masqué parce
> que la contenance ne lit l'acquis qu'à l'éclosion. Par le critère même de
> V11, la densité sort aussi du temps du séjour : `τ = τ₀`, constant. Mesure et
> décision au §3, sous « 2.B ». La phrase « elle travaille par l'acquis de
> séjour, et par rien d'autre » ci-dessus est donc périmée : la densité
> travaille désormais par la production, et par rien d'autre.

### Ce que le balayage a montré, et qui contredit le GDD §6.4

`k` étant désormais le seul réglage du repeuplement, il devient balayable. Sur
quinze cycles, `f = 0,25`, politique par défaut :

| τ = 1/k | 30 s | 60 s | 300 s | 1200 s |
|---|---|---|---|---|
| fraction en redescente | 76 % | 73 % | **73 %** | 73 % |
| temps actif | 8,6 h | 9,0 h | 11,7 h | 15,4 h |

**`k` ne déplace pas la fraction.** Quarante fois plus lent la laisse à trois
points près, tout en multipliant le temps actif par 1,8. Il règle une DURÉE, pas
un RAPPORT — et c'est prévisible après coup : il ralentit dans la même
proportion la phase de redescente et la phase de terrain neuf, donc il sort du
quotient.

Le GDD §6.4 affirme pourtant : « c'est `k`, le taux de repeuplement, qui produit
réellement les 20–25 % », et « `f` donne un puits au mana d'après-ponte ». La
première moitié est réfutée par la mesure. La seconde tient, mais `f` est borné :
c'est un diviseur constant opposé à une exponentielle, il achète
`log_g(1/f)` paliers d'avance — 1,6 palier à `f = 0,25`, 3,4 à `f = 0,05` — et
le balayage complet ne descend que de 80 % à 60 %.

**Aucun des deux leviers nommés par le §6.4 ne peut atteindre la cible dure.**
La cause est celle que le §5 de `politique-du-simulateur.md` avait déjà isolée :
rejoindre la profondeur `p` coûte `g^p` quand la production n'y vaut que `D^p`,
et `(g/D)^p` grandit à chaque cycle. Ce qui change un rapport est ce qui rend la
retraversée catégoriquement plus courte en TEMPS sans toucher au terrain neuf :
les verbes `creusement_auto` (« galeries connues ») et `file_de_descente` du
§7.3, qui suppriment des allers-retours de check-in d'un seul côté du quotient.

À porter au GDD : le §6.4 doit cesser de désigner `k` comme le pilote des
20–25 %, et le §16.1 doit ranger la cible parmi ce que les verbes produisent, non
parmi ce que le calibrage produit. C'est un constat de mesure, pas une décision
de conception — la décision reste à l'auteur.
