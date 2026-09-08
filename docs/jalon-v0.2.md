# IdlePond — jalon v0.2 : « l'assise I est jouable »

Tier 2. Compte rendu du jalon v0.2 tel que le §12 le définit. Ne crée aucun canon.

---

## 1. Critère d'acceptation

> « Accepté quand : une première éclosion se fait sans lire de documentation, et
> le premier succès tombe en moins de deux minutes. »

**Premier succès : à la première seconde.** Convaincre le premier banc est le
premier geste possible et le déclenche. Mesuré, pas estimé — `tests/plancher-de-cadence.test.ts`.

**Première éclosion sans documentation :** vérifiée en pilotant l'application
réelle dans un navigateur. Un agent qui ne fait que cliquer les boutons actifs
convainc, monte des niveaux, creuse, atteint le blocage doux et rentre dans
l'œuf — aucune console d'erreur, `retours dans l'œuf` passe à 1, et le coût de
creusement descend de 60 à 59 sous l'effet du franchissement obtenu.

Le §12 demandait aussi le plancher complet du §8.4. Il est tenu :

| Garantie du §8.4 | Mesuré |
|---|---|
| Premier succès dans les deux premières minutes | **1 s** |
| Un déclenchement toutes les 3 à 5 minutes sur la première demi-heure | **écart maximal 3,5 min**, 19 déclenchements |
| Un franchissement à la première éclosion, obligatoirement | **oui** |

---

## 2. Ce que la mesure a réfuté, et qu'il a fallu corriger

C'est le fait marquant du jalon, et il vient du plancher du §8.4.

**Avec les graines de la v0.1, le premier cycle se figeait au bout de dix
minutes.** Passé ce point, plus rien n'était achetable : la contenance
plafonnait le stock sous le prix du moindre niveau, la production asymptotait,
et les quatre heures suivantes du « cycle de 4,4 h » mesuré en v0.1 n'étaient
que l'expiration du délai de patience du simulateur. Aucun jeu de succès ne
pouvait tenir une cadence de 3 à 5 minutes sur trente minutes : il n'y avait
rien à observer.

Le §8.4 a donc fonctionné exactement comme une garantie doit fonctionner — il
n'a pas révélé un manque de contenu, il a révélé une graine fausse.

Deux graines d'échelle ont été recalées, et deux seulement :

| Graine | v0.1 | v0.2 | Pourquoi |
|---|---|---|---|
| `TAUX_BASE_AU_PALIER_0` | 1 | **0,2** | À 1 mana/s par individu, tout coûtait moins que quelques secondes de production : rien n'avait de prix. |
| `CONTENANCE_INITIALE` | 200 | **1200** | À 200, le plafond de stock fermait la montée en niveau presque en même temps que la descente. Le blocage n'était pas doux, il était total. |

Ce sont des graines `[P]`, pas des valeurs de canon, et elles ont été recalées
contre des valeurs FIXES — le plancher du §8.4 et les ~4,4 paliers par cycle du
§13.1. C'est du calibrage partiel, forcé par un critère d'acceptation ; le
calibrage complet reste le jalon v0.3, avec la télémétrie de sessions réelles.

**Correction à la v0.1 :** le rapport du jalon précédent annonçait « durée du
cycle 1 : 4,43 h ». Le chiffre était exact et la lecture incomplète — l'essentiel
de cette durée était du temps mort. Ce que le simulateur mesurait n'était pas la
durée du cycle mais celle de la politique de patience.

---

## 3. Ce qui a été livré

### Le système de succès (§8), posé avant le contenu

Trois familles, trois états de visibilité, un registre figé, un verrouillage par
assise. Le §8 range ces propriétés parmi celles qui « ne se rétrofitent pas » :
elles sont donc dans le typage, pas dans une convention.

Le déclencheur est **toujours** une lecture de seuil sur l'état de fin de tick,
jamais un événement consommé au vol. Ce n'est pas un choix de commodité : un
déclencheur qui observerait l'intérieur d'un intervalle ferait diverger un pas
de 8 h de 480 pas de 60 s, et emporterait le hors ligne avec lui.

Registre de l'assise I : **46 succès** — 9 actes écrits à la main, 32 seuils
engendrés par gabarit (§8.1 : « générés par gabarit »), 5 franchissements.

### Le jeu

Boucle complète débloquer → améliorer → éclore, sans technique ni bénédictions
comme le §12 le prescrit. Écran React + Tailwind, typographique : aucun art
définitif, aucun texte définitif.

- **La contenance** dit « l'eau déborde » quand elle sature, et le blocage doux
  est énoncé pour ce qu'il est : on ne peut plus descendre, on peut encore
  faire venir du monde.
- **Le détail de la captation** est consultable sur chaque banc, chaque terme
  attribuable à sa source — la contrepartie obligatoire d'un effet appliqué
  silencieusement (§8.2).
- **Les annonces de succès** sont une ligne qui apparaît et s'efface. Jamais une
  fenêtre, jamais un focus volé.
- **Le gain d'éclosion ne s'affiche pas en permanence.** « Lire l'eau » est un
  nœud verbe de la branche Éclosion (§7.3) : l'afficher en continu dès
  maintenant viderait ce nœud avant de l'avoir écrit. Il se lit au moment de
  décider, et nulle part ailleurs.

### Les adaptateurs

`boucle.ts` (le tick à 100 ms, porté du `GameLoopManager` sans son singleton ni
sa logique métier), `hors-ligne.ts` (un seul appel à `tick`, plafond à 6 h,
compteur Entretien alimenté par les heures **créditées**), et le magasin Zustand
— miroir de l'état, aucune règle de jeu.

### La porte de jalon

`EtatJeu.limiteDeContenu` : le monde est dessiné sur 62 paliers parce que c'est
l'économie que le simulateur doit mesurer ; le jeu n'en offre que ce qui a du
contenu. Elle vit dans l'état plutôt qu'en constante pour que le jeu et le
simulateur puissent différer **sans jamais forker le reducer**. Un seul code,
deux mondes — « aucune assise n'est produite avant que la précédente ait été
mesurée » (§12).

### Tests ajoutés

| Test | Ce qu'il garantit |
|---|---|
| `plancher-de-cadence` | Les trois garanties du §8.4, mesurées sur trente minutes de jeu réel |
| `hors-ligne` | Le crédit d'absence est exactement un `tick` de la même durée ; le plafond borne le gain ; le compteur ne lit que les heures créditées |
| `canon` (§3, ajout) | **Aucun terme de couche ne sort dans un texte affiché** |

53 tests, tous verts. `tsc -b`, `eslint`, `vite build` propres.

---

## 4. Deux fautes que les tests n'auraient pas vues seules

**Le noyau écrivait du texte d'écran.** `detailDeCaptation` produisait la chaîne
`palier 0` comme source d'un terme. C'est une violation directe du §3 —
« `assise` et `palier` sont des termes de code et de GDD, pas d'écran » — et
elle n'est apparue qu'à la capture d'écran. La source est devenue une structure
(`SourceDeTerme`) que l'UI met en mots : le noyau ne fabrique plus aucun texte.
Le nouveau test du §3 porte sur les chaînes **réellement produites**, pas sur le
source, parce que c'est là que la faute était.

**L'ordre des succès dépendait de la taille du pas.** Deux seuils franchis dans
le même intervalle arrivent ensemble sous un pas de 8 h et l'un après l'autre
sous 480 pas de 60 s. Concaténer dans l'ordre d'arrivée faisait diverger deux
états par ailleurs identiques. `succesDebloques` est désormais reconstruit dans
l'ordre du registre, qui ne dépend de rien.

Corollaire : l'**intervalle** entre deux succès a quitté le tick. L'instant
précis où un seuil a été franchi à l'intérieur d'un intervalle n'est pas
connaissable d'un seul pas — c'est une observation, pas une mécanique. Le jeu
l'enregistre à 100 ms et mesure l'intervalle vrai ; le crédit hors ligne ne
l'enregistre pas, parce qu'il ne peut pas prétendre savoir qu'un succès est
tombé il y a six heures.

---

## 5. État de la courbe, quinze cycles

| Cycle | 1 | 3 | 6 | 9 | 12 | 15 |
|---|---|---|---|---|---|---|
| Durée active | 4,8 h | 7,2 h | 12,8 h | 22,8 h | 40,8 h | 131,5 h |
| Paliers | 5 | 17 | 35 | 50 | 62 | 62 |
| Redescente | 0 % | 61 % | 54 % | 60 % | 69 % | 25 % |

**Total actif : 514 h.**

Le recalage a rapproché la profondeur par cycle de sa cible (≈ 5–6 contre ≈ 7 en
v0.1, pour 4,13 visés) et repoussé l'épuisement des 62 paliers du cycle 10 au
cycle 12. Les deux écarts qui restent sont les mêmes qu'en v0.1, et restent des
entrées pour v0.3 :

- **la redescente occupe toujours ~55 à 65 % de chaque cycle** — c'est le risque
  n° 1 du §15, et il n'a pas bougé ;
- **514 h actives contre ~38 h visées** — attendu tant que la technique et les
  bénédictions ne sont pas là (v0.4), et le §7.2 rappelle que c'est le
  calendrier des verbes qui règle la durée réelle.

---

## 6. Décisions ouvertes ajoutées par ce jalon

| # | Question | Où |
|---|---|---|
| V7 | **De quel côté de la frontière du §4.3 tombent les succès ?** La phrase nomme les succès — « aucun nœud, aucun succès, aucun système ne franchit cette ligne » — sans dire s'ils peuvent monter une production ou si cela reste l'exclusivité de la bénédiction achetée en Foi. Le typage porte la lecture la plus restrictive : un succès ne cible que des termes de coût ou de confort. Elle est retenue parce qu'elle est la seule des deux qui ne puisse pas être fausse par excès. | `types.ts` |
| V8 | **Les cinq verbes de succès ne sont pas alloués.** Le §8.2 pose « verbe pour les franchissements » comme défaut orientatif ; il n'est pas suivi, et pas par facilité : les onze `CapaciteId` du §7.3 appartiennent toutes à l'arbre, et le §7.5 règle 1 veut qu'une capacité ait exactement une source. En donner un à un franchissement demanderait d'inventer une capacité. À trancher avec le budget du §7.6, en v0.4. | `franchissements.ts` |
| V9 | **Les noms sont provisoires.** `donnees/textes-provisoires.ts` porte tout le texte affiché — un lieu, trois espèces, quarante-six succès. Seule « la mare » vient du contrat (§3, §12). Rien de ce fichier n'entre dans un identifiant ni dans une sauvegarde : le remplacer par les noms canoniques ne touche aucune autre ligne. **`[P] P3` reste ouvert et bloque la v0.5.** | `textes-provisoires.ts` |
| V10 | **Le placement provisoire met une espèce sur quatre profondeurs d'affilée**, ce qui affiche quatre fois le même nom à l'écran. Mécaniquement juste, visuellement pauvre. Le placement est autorial (§4.2) et le corrigera. | `paliers.ts` |

---

## 7. Une contrainte à porter en v0.4

Les achats automatiques (`achat_auto`, `achat_auto_max`, « Le peuple continue »)
changent les niveaux **pendant** un intervalle. Le noyau les traite aujourd'hui
comme constants sur le pas, ce qui est vrai tant que les achats sont des actes
du joueur entre deux ticks. Un automatisme écrit naïvement ferait tomber
l'équivalence de pas et, avec elle, le hors ligne. Il faudra une forme fermée,
pas une boucle.

C'est déjà signalé au jalon v0.1 ; le jalon v0.2 n'a rien fait qui le rende plus
facile, et le §12 place les deux systèmes en v0.4 « en même temps » précisément
pour que ce genre de chose se voie tout de suite.
