# IdlePond — la politique du simulateur, et une correction

Tier 2. Ne crée aucun canon.

---

## 1. La correction, d'abord

**Les jalons v0.1 et v0.2 ont rapporté « 514 h » puis « 248 h de jeu actif »
contre les ~38 h visées, et concluaient que le jeu était six à treize fois trop
long. C'était faux, et l'erreur était une confusion de grandeurs.**

Ce que le noyau mesure est le temps **écoulé** en jeu — le temps calendaire du
§5.4, dont la cible est **~600 h**. Les ~38 h actives en sont le sous-ensemble
pendant lequel le joueur est devant l'écran, et le noyau ne peut pas le
connaître : il ne sait pas quand quelqu'un regarde. Le §11 demandait pourtant
les deux, nommément — « durée de cycle, active et calendaire », « intervalle
réel entre deux sessions | distingue temps actif et temps calendaire ».

Rapporté correctement, le jeu n'est pas trop long : il est **court des deux
côtés, dans les mêmes proportions**.

| | mesuré | cible §5.4 | part |
|---|---|---|---|
| Temps actif | **11,9 h** | ~38 h | 31 % |
| Temps calendaire | **233 h** | ~600 h | 39 % |

Le champ `tempsJeuActifSecondes` a été renommé `tempsEcouleSecondes`, et
`MesureDeCycle.dureeActiveSecondes` en `dureeEcouleeSecondes`. Un test interdit
désormais de les reconfondre.

---

## 2. La croissance par cycle est celle qui était dessinée

Durées écoulées mesurées, quinze cycles : 4,1 h → 28,0 h, croissance moyenne
**×1,15 par cycle** contre ×1,18 visé.

Ce n'est pas un accident de politique, c'est `g/D` qui travaille. Le §6.3 de la
v1.0 le dit dans l'autre sens : « si `D = g`, ouvrir un palier prend toujours le
même temps et la durée d'un cycle est plate ». On a choisi `D ≠ g`, donc les
durées écoulées croissent — et `(g/D)^4,4 = 1,18` est exactement la croissance
par cycle.

Il n'y a donc **aucune contradiction** entre « les durées sont plates (~2,7 h) »
du §5.4 et le ×1,18 du §6.3 : la première parle du temps **actif**, la seconde
du temps **écoulé**. Le rapport visé entre les deux — 600 / 38 ≈ 16 — est une
propriété de la politique de check-in, pas de l'économie.

---

## 3. Ce que la politique fait maintenant

Trois changements, tous du côté simulateur ; le noyau n'a pas bougé d'une règle.

### Des sessions, plutôt qu'un joueur omniprésent

Le joueur revient toutes les quatre heures, agit tant que les achats
s'enchaînent, et repart au plus tard au bout d'un quart d'heure. Entre deux
sessions la mare tourne sans lui : le mana plafonne à la contenance et le
surplus expire vers l'ambiant. L'absence n'est pas punie, elle est bornée — et
c'est ce qui donnera un sens à la branche Entretien.

Le temps actif est la somme des sessions ; le temps écoulé, tout le reste
compris.

### Le retour sur investissement, plutôt que le moins cher

L'ancienne règle achetait la place la moins chère, donc systématiquement dans
les eaux hautes, sans regarder ce qu'elle rapportait. La nouvelle demande à
chaque place de **se rembourser avant le creusement qu'on attend** :
`coût / gain < temps restant avant de pouvoir creuser`. Au-delà, une place
retarde ce qu'elle prétend hâter.

Le cycle se termine quand on ne peut plus creuser : la seule question qui vaille
est donc « est-ce que cet achat me fait creuser plus tôt ? ».

### La saturation, plutôt qu'un minuteur de patience

L'ancienne règle attendait quatre heures d'ennui avant d'éclore — elle mesurait
l'impatience du simulateur, pas une décision. La nouvelle éclot quand il n'y a
plus de profondeur à prendre **et** que l'acquis de séjour a saturé (95 % de
`A∞`).

C'est la forme opérationnelle de la seule vraie décision du §6.4, et c'est le
§2.B qui la rend réelle : passé la saturation, une heure de plus dans la même
vie n'achète que de la Foi, alors qu'une éclosion achète de la profondeur.

**Effet mesuré** : 248 h → 233 h écoulées, et la simulation de quinze cycles
passe de 46 s à 26 s.

---

## 4. Le check-in est bien le seul bouton de temps calendaire

Le §5.4 l'affirme ; le voici mesuré, à économie strictement identique.

| Politique | Actif | Calendaire | Rapport |
|---|---|---|---|
| check-in 2 h, session 15 min | 13,5 h | **133 h** | 9,8 |
| check-in 4 h, session 15 min | 11,9 h | **233 h** | 19,5 |
| check-in 4 h, session 30 min | 15,9 h | **170 h** | 10,7 |
| check-in 8 h, session 15 min | 11,0 h | **441 h** | 40,1 |

Le calendaire varie d'un facteur 3,3 ; l'actif ne bouge quasiment pas. Un test
le vérifie désormais à chaque exécution.

C'est aussi ce qui donne toute sa portée au §7.2 : un nœud **verbe** supprime
des intervalles de check-in, c'est-à-dire la colonne qui varie ; un nœud
**chiffre** ne touche que la colonne qui ne bouge pas.

---

## 5. Deux constats que la mesure impose

### La redescente occupe 85 % d'un cycle, en moyenne

Fractions relevées, cycles 2 à 15 : 96 %, 52 %, 100 %, 97 %, 68 %, 99 %, 75 %,
99 %, 80 %, 100 %, 83 %, 99 %, 86 %, 100 %.

C'est le risque n° 1 du §15, et il est franchement réalisé. La cause est
structurelle et connue : rejoindre la profondeur `p` coûte `g^p` alors que la
production n'y vaut que `D^p`, et le rapport `(g/D)^p` grandit à chaque cycle.
Aucune politique n'y peut quoi que ce soit — j'ai essayé, la nouvelle ne fait
gagner que 6 %.

**Le contrat porte déjà sa propre réponse** : « Galeries connues →
`creusement_auto` sur les paliers déjà ouverts dans une vie précédente » (§7.3,
Creusement 5), et « La file → `file_de_descente` ». Ce sont des VERBES, donc du
jalon v0.4, et le §7.2 explique pourquoi ils sont le bon outil là où un nœud
chiffre ne servirait à rien.

Ce qu'il faut en retenir pour v0.4 : le calendrier d'ouverture de ces deux
verbes-là ne règle pas seulement la durée du jeu, il décide si la redescente
reste le jeu.

### Le repeuplement a cessé d'exister — décision ouverte V11

Temps caractéristique du repeuplement, mesuré au premier creux :

| | τ |
|---|---|
| Cycle 1 | 300 s |
| Cycle 6 | < 1 s |
| Cycle 15 | 1,1 × 10⁻⁴ s |

La population est instantanée dès le deuxième cycle. La mécanique est morte, et
avec elle le délai entre l'achat d'une place et son effet — c'est-à-dire ce qui
fait le jeu.

La cause est nette. Depuis l'amendement v1.1 §2.A, la densité vaut `pointe^α` :
elle croît avec la production, **sans borne**. Le §2.B la fait passer par un
rapport que la saturation borne, donc il tient. Le canal du §6.5 de la v1.0 — la
densité qui retourne dans la vitesse de repeuplement — la met à un exposant nu,
et rien ne la retient.

Je n'ai pas tranché : c'est un canal de canon, et le §7 dit de demander. Le code
garde le couplage, la télémétrie relève `τ`, et un test empêche la
dégénérescence de passer inaperçue. **Trois issues possibles**, dans l'ordre où
je les recommanderais :

1. **Découpler.** La densité travaille par l'acquis de séjour (§2.B) et rien
   d'autre. C'est cohérent avec l'amendement, qui définit une chaîne complète
   sans jamais mentionner le repeuplement.
2. **Borner le couplage** par un plafond nommé, pour garder un `k` qui monte
   mais ne s'évanouit pas.
3. **Coupler à autre chose** que la densité brute — sa profondeur, son rang,
   quelque chose de borné par construction.

---

## 6. Où en sont les cibles

| | mesuré | cible | reste à faire |
|---|---|---|---|
| Croissance par cycle | ×1,15 | ×1,18 | rien : c'est `g/D`, et il est juste |
| Paliers par cycle | 4,4 | 4,4 | rien |
| Temps calendaire | 233 h | ~600 h | le check-in réel, à mesurer en v0.3 |
| Temps actif | 11,9 h | ~38 h | la durée réelle d'une session, idem |
| Redescente | 85 % | — | les verbes de Creusement, v0.4 |

Les deux écarts de temps ne se règlent pas par les paramètres de l'économie —
ils dépendent de la fréquence à laquelle un vrai joueur revient et du temps
qu'il reste. C'est exactement ce que le jalon v0.3 doit mesurer « sur des
sessions réelles, pas sur des graines », et la télémétrie est maintenant en état
de le relever.
