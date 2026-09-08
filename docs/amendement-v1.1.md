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

### 2.B — la contenance monte par l'acquis de séjour

`EtatCycle.acquisDeSejour`, accumulation saturante vers `A∞` dont le temps
caractéristique décroît quand la densité monte. Le facteur ×47,1 **n'est écrit
nulle part** dans le code de l'éclosion : il émerge de `A∞ = 47.6` et
`τ₀ = 0.87 h`, et le test le vérifie à 2 % près.

Les deux graines se tiennent l'une l'autre, et c'est vérifié : trois heures de
séjour portent l'acquis à 46,1, donc la contenance à ×47,09 contre ×47,10 visé.
Le `t₉₀` tombe à 2,00 h. Changer l'une sans l'autre casse la cible.

L'effet secondaire recherché tient aussi : cent heures de séjour au lieu de
trois ne rapportent que 3 % de contenance en plus. Passé la saturation, rester
ne rapporte plus de profondeur, seulement de la Foi — la seule vraie décision du
joueur est réelle, et un test l'affirme.

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
