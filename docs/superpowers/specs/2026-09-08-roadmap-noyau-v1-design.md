# Roadmap — passage au noyau v1.0

**Date** : 2026-09-08
**Objet** : la direction du projet après le versement des documents `idlepond-noyau-v1_0.md`, `idlepond-histoire-v0.1.md` et `RESULTATS.md` dans `docs/`.
**Statut** : conception validée. Le plan d'implémentation en découle et vit dans `docs/superpowers/plans/`.

---

## 1. Pourquoi ce document existe

Trois documents sont arrivés dans `docs/` sans passer par git, et ils ne raffinent pas la direction en cours — ils la remplacent.

`idlepond-noyau-v1_0.md` déclare morts la population simulée, la maturation, les deux canaux de captation, la capacité de palier et le tarif réduit de redescente. Or le commit `9b44490`, du même jour, venait précisément de les réintroduire dans le code au nom du GDD v2.4.

**Le dépôt se contredit donc lui-même aujourd'hui.** `canon.test.ts` verrouille `f = 1` pendant que `redescente.test.ts` vérifie qu'« un palier déjà atteint se paie `f` fois moins » — un test qui ne teste plus rien. `deux-canaux.test.ts` et `saturation.test.ts` verrouillent des systèmes que le noyau v1.0 déclare morts.

Ce document tranche la préséance une fois, par écrit, et en déduit l'ordre des travaux.

## 2. Préséance — tranchée

| Domaine | Document directif |
|---|---|
| Mécanique du noyau, économie, courbe, arbre technique, état | **`idlepond-noyau-v1_0.md`** |
| Fiction, monde, géographie nommée, voix, succès, direction artistique | **`docs/GDD.md` v2.4** |
| Mesure — amende le noyau v1.0 sur ses cinq findings | **`docs/RESULTATS.md`** |
| Comble les `[P]` que le noyau laisse ouverts (`θ`, loi de contenance) | `docs/amendement-v1.1.md`, là où il ne contredit pas le noyau |
| Périmé | `docs/jalon-v0.1.md` §5.1, qui donnait la préséance au prompt de lancement |

**Règle de lecture.** Quand le GDD v2.4 décrit une mécanique que le noyau v1.0 a tuée, le noyau gagne. Quand le noyau v1.0 est muet sur le monde, la voix ou un succès, le GDD gagne. Quand `RESULTATS.md` mesure un chiffre que le noyau v1.0 avait posé en graine, la mesure gagne.

Cette règle est recopiée dans `docs/PRESEANCE.md` pour être trouvable sans lire ce document.

## 3. Ce que `RESULTATS.md` a réfuté

Cinq findings amendent le noyau v1.0 avant même son implémentation.

1. **La boucle est invariante d'échelle.** Avec `f = 1`, `g = 2.4` et `D = 2.31`, chaque cycle est le même problème à une échelle près : sa durée est donc constante par construction. Les 182 h à ×1.18 sont structurellement exclues, pas mal réglées. Le calibreur pousse `θ` contre sa borne basse et n'y arrive pas.
2. **La croissance ×1.18 vient du joueur, pas de l'économie.** Un joueur absent plafonne sa contenance et perd la production au-delà, de plus en plus souvent. 38 h actives, environ 25 jours calendaires à deux relevés par jour.
3. **Le noyau v1.0 n'avait pas d'amorçage.** Production nulle, donc mana nul, donc aucune espèce jamais débloquée. `DEBIT_HEROS` corrige : le héros capte l'ambiant seul, ce qui est sa mutation, donc canonique.
4. **21 espèces, pas 24.** La règle « une tous les 3 paliers depuis le premier de l'assise » sur 6/12/12/12/12/8 donne 2+4+4+4+4+3.
5. **La loi de l'exposant.** L'exposant du multiplicateur de densité vaut `θ / α`, où `θ` est la part du besoin que la densité compense. C'est le seul bouton qui agisse sur la forme de la courbe.

**Décision de durée.** Le contenu porte 38 h actives ; on découple les chapitres des éclosions. Environ 45 éclosions de 2,7 h font 120 h actives, avec un chapitre toutes les 3 éclosions. Le premier cycle reste à 3 h, aucun contenu n'est perdu, et la cadence narrative devient réglable indépendamment de l'économie. C'est la seule des trois sorties qui donne la durée voulue sans casser le premier cycle.

## 4. Ce qui meurt, ce qui vit

**Meurt** — parce que le noyau v1.0 §10 le déclare mort :

| Ce qui sort | Où |
|---|---|
| Population, effectif, place, repeuplement, `EtatBanc`, `Banc`, `BancId` | `noyau/population.ts`, `noyau/types.ts`, `noyau/economie.ts` |
| Maturation, `part_mure`, `partsMures`, plafond de maturation | `noyau/maturation.ts`, `noyau/types.ts` |
| Second canal : `debit_acclimate`, `rendement_acclimatation`, `acclimatations` | `noyau/economie.ts`, `ui/Captation.tsx` |
| Capacité de palier, `cout_place`, `place_de_depart` | `noyau/types.ts`, `noyau/economie.ts` |
| Tarif réduit de redescente, puits « aménager », `cout_reconviction` | `noyau/economie.ts`, `tests/redescente.test.ts` |

**Vit** — parce que le noyau v1.0 le reprend ou ne le touche pas :

- **Les adaptateurs.** `persistance.ts` et sa chaîne `saveVersion`, `horloge.ts`, `hors-ligne.ts` et son plafond, `boucle.ts` à 100 ms, `telemetrie.ts`. Le noyau v1.0 §8.2 et §8.3 décrivent exactement ce qu'ils font déjà.
- **Les tests d'invariant.** `architecture.test.ts` (le noyau reste pur), `determinisme.test.ts`, `equivalence-de-pas.test.ts` — cette dernière est ce qui vérifie la règle « tout se calcule en un pas pour `dt = 8 h` ».
- **La voix et les succès.** `voix.test.ts` verrouille déjà les quatre paliers et le registre figé du GDD §14.5, que le noyau v1.0 §8.1 reprend tel quel dans `succes: Record<SuccesId, { obtenu, registre: PalierDeVoix }>`.
- **`especesAyantAtteintCent`**, qui devient le support du bonus global permanent de +3 % au niveau 100 (noyau v1.0 §2.1).
- **`acquisDeSejour`**, qui reste le seul moteur de la contenance.
- **`CapaciteId`, `SourceDeCapacite`, `TermeDeFormule`** — les registres existent et le test « une capacité a exactement une source » passe déjà. Seuls les membres liés au modèle mort sont retirés.

## 5. Le modèle cible, en une page

```
production(espèce) = débit_base × niveau × mult_seuil × mult_bénédictions
production totale   = DEBIT_HEROS
                    + Σ espèces × mult_densité × (1 + 0.03 × |espèces à 100|)
```

> **`mult_technique` est retiré de cette formule, et c'est une décision, pas un oubli.** Le noyau v1.0 se contredit : sa §2 place `mult_technique` dans la production totale et sa §6.2 donne au nœud « Réputation » un `+5 % de production par espèce débloquée`, alors que sa §6.3 pose la règle dure inverse — « la technique baisse les **coûts**, la bénédiction monte la **production**, aucun nœud ne franchit cette ligne ». La règle dure est explicitement désignée comme telle et `canon.test.ts` la verrouille déjà. On la garde et on réécrit « Réputation » en réduction de coût, comme la §6.3 l'ordonne elle-même pour ce cas exact.

Trois achats et rien d'autre : **creuser** (×2.4), **débloquer une espèce** (une fois), **améliorer** (×1.15). Seuils cumulés aux niveaux 10 / 25 / 50 / 100 pour ×2 / ×4 / ×8 / ×16 — jamais ×1024.

La **contenance** limite le stock, pas la production ni les dépenses. Le palier suivant finit par coûter plus qu'elle ne contient : c'est ce qui force l'éclosion, et le blocage est doux — on peut rester pour la Foi.

L'**éclosion** est un reset complet, `f = 1`. Elle conserve la contenance, la densité, la Foi et les bénédictions, la technique, et les drapeaux permanents. Le gain de densité vaut `pointe ^ α`, et `mult_densité = (1 + densité / d₀) ^ (θ / α)`.

## 6. Les six phases

Chaque phase se termine sur un critère vérifiable. Aucune ne commence avant que la précédente soit verte.

**Portée du premier plan d'implémentation : les phases 0 à 3.** Elles vont du versement des documents jusqu'aux nombres mesurés, et elles forment un tout dont la sortie est une décision chiffrée. Les phases 4 et 5 sont décrites ici parce qu'elles fixent la direction, mais elles seront re-planifiées après la phase 3 — dont le résultat peut les modifier.

### Phase 0 — Verser et trancher

Les sept fichiers non suivis de `docs/` entrent dans git. `docs/PRESEANCE.md` est écrit : une page, la table du §2 ci-dessus, et la mention que `jalon-v0.1.md` §5.1 est périmé. Les quatre `.ts` du spike (`docs/noyau.ts`, `constantes.ts`, `simulateur.ts`, `calibreur.ts`) reçoivent un en-tête « harnais de mesure, non intégré au jeu » — ils ont produit `RESULTATS.md` et servent de référence de lecture, pas de code de production.

**Critère** : `git status` propre, `docs/PRESEANCE.md` présent.

### Phase 1 — Purge

Suppression de `noyau/population.ts` et `noyau/maturation.ts`. Retrait des symboles du §4 dans `types.ts`, `economie.ts`, `noyau.ts`, `eclosion.ts`, `densite.ts`, `succes.ts`, `technique.ts`, `donnees/especes.ts`, `adaptateurs/persistance.ts`, et dans `ui/Captation.tsx`, `ui/Mare.tsx`, `ui/format.ts`. Suppression de `tests/deux-canaux.test.ts` et `tests/saturation.test.ts`. `tests/redescente.test.ts` est réduit à une seule affirmation : `f = 1`, aucun tarif réduit n'existe, et un palier retraversé se paie plein tarif.

`saveVersion` est incrémenté. Conformément au noyau v1.0 §8.3, la migration **ne supprime aucun champ** : elle marque morts `bancs`, `partsMures` et `acclimatations`, et les ignore à la lecture.

**Critère** : `tsc -b`, `eslint`, `vite build` propres ; la suite de tests restante verte ; aucune occurrence de `population`, `maturation`, `acclimat`, `partMure`, `cout_place` dans `src/`.

### Phase 2 — Le cœur v1.0

Réécriture de `noyau/economie.ts` et `noyau/noyau.ts` sur le modèle du §5, en s'appuyant sur la lecture de `docs/noyau.ts`. Les trois achats, la production, les seuils cumulés, le bonus global permanent, `DEBIT_HEROS`, la contenance comme seul frein, l'éclosion en reset complet. `tick(state, dt)` reste pur : aucun `Date.now()`, PRNG à graine dans l'état, aucun état hors du reducer.

Le contenu — 62 paliers, 21 espèces, une espèce tous les 3 paliers depuis le premier de l'assise — se construit depuis les seules constantes, comme `construireContenu()` le fait dans le spike.

**Critère** : `equivalence-de-pas.test.ts` vert pour `dt = 8 h` contre 480 pas de 60 s ; `determinisme.test.ts` vert ; `canon.test.ts` vert sur 62 paliers, 6 assises, 21 espèces, `f = 1`, seuils cumulés ; une partie headless atteint l'éclosion 2 sans UI.

### Phase 3 — Mesurer

`simulateur/simulateur.ts` et `simulateur/calibreur.ts` rebranchés sur ce cœur — même code, `dt = 60 s`. Le calibreur résout `α`, `θ` et l'échelle, puis les six couples `(A, B)` de l'arbre technique. Sortie : un `docs/RESULTATS.md` v2 qui remplace le v1 et devient la référence chiffrée.

**Rien de la phase 4 ne commence avant que ces nombres tiennent.** C'est la leçon directe du finding 1 : une cible chiffrée peut être structurellement inatteignable, et on ne le sait qu'en mesurant.

**Critère** : le simulateur tourne sur 45 éclosions ; durée du cycle 1 proche de 3 h ; total actif dans une fourchette annoncée avant la mesure ; les 30 nœuds de l'arbre sont finançables sur une partie, et pas avant l'assise V.

### Phase 4 — Découpler le récit

`EtatPermanent` reçoit un compteur de chapitre distinct de `nombreEclosions`. Un chapitre toutes les 3 éclosions, réglable par une constante unique. Les quatre paliers de voix — la pente, les signes, la voix, le dialogue — se rebranchent sur ce compteur et non sur le compte d'éclosions. Les neuf actes de `idlepond-histoire-v0.1.md` sont mappés sur les six assises.

La règle absolue du §13 de l'histoire tient : **la marraine n'explique rien avant l'acte VIII.** Le dialogue reste inatteignable par compteur — il vient du relais de l'esprit, et `voix.test.ts` le vérifie déjà.

**Critère** : `voix.test.ts` étendu et vert ; la cadence narrative change quand on change la seule constante de cadence, sans toucher à l'économie ; le registre figé continue de tenir sur 45 éclosions.

### Phase 5 — L'arbre et le jouable

Les six branches de l'arbre, points par `floor(A · ln(1 + compteur / B))`, coûts 5 / 12 / 25 / 45 / 80. Les dix verbes, sous les trois règles dures du noyau v1.0 §6.3 : une capacité a exactement une source, le budget de verbes est commun, et la technique baisse les coûts sans jamais monter la production.

Le compteur Entretien lit les heures **créditées**, jamais écoulées — c'est la seule protection nécessaire contre l'avance d'horloge, et `heuresHorsLigneCreditees` la porte déjà.

Puis l'assise I jouable de bout en bout, télémétrie incluse.

**Critère** : les tests « une capacité a exactement une source » et « le budget de verbes est tenu » verts ; aucun nœud ne cible un terme de production ; l'assise I se joue du premier palier à la première éclosion dans le navigateur.

## 7. Risques

**Le calibrage de la phase 3 peut réfuter la phase 4.** Si 45 éclosions ne donnent pas 120 h, la cadence narrative bouge, pas l'économie — c'est précisément ce que le découplage achète. Le risque est absorbé par construction.

**La purge de la phase 1 touche l'interface.** `Captation.tsx` affiche le détail des deux canaux ; il devient l'affichage d'un revenu unique. C'est une simplification, pas une réécriture, mais elle est visible.

**`docs/GDD.md` v2.4 reste écrit contre le modèle mort.** On ne le réécrit pas : `PRESEANCE.md` dit lesquelles de ses sections sont dépassées — §3 la captation, §7 le vivant, §10 la ponte, §16 l'équilibrage. Une v2.5 réconciliée est souhaitable un jour ; elle n'est pas sur ce chemin.

**Le budget de contenu.** 62 paliers et 21 espèces restent à écrire. Étang des Merveilles portait 38 espèces sur 11 zones, donc le budget est prouvé faisable par l'historique du projet — c'est la seule donnée réelle du noyau v1.0.

## 8. Questions laissées ouvertes

| # | Question | Statut |
|---|---|---|
| P26 | Les miracles, et leur articulation avec les bénédictions | **Gelé** — ne bloque rien, à reprendre quand le noyau tourne |
| — | La réécriture du nœud « Réputation » en réduction de coût (voir §5) | Tranché ici par la règle dure ; la valeur exacte est à poser en phase 5 |
| — | La valeur exacte de la cadence chapitre / éclosion | Ouvert — sortie de la phase 3, graine à 3 |
| — | Ce que l'esprit veut en échange, à l'acte VIII | Ouvert — écriture, pas mécanique |
| P7, P13–P19, P21, P24, P27 | Registre du noyau v1.0 §9 | Ouverts, non bloquants |
