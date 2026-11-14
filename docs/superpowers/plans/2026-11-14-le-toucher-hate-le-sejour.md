# Le toucher hâte le séjour — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** un toucher fait avancer l'acquis de séjour de 0,25 s (plus 10 % par rang de l'amélioration du toucher, 10 rangs au plus), en plus de son mana.

**Architecture :** tout passe par `Reducteur.Toucher(etat, fois)` dans le noyau pur. Le jeu (`Partie`) et le simulateur l'appellent déjà ; ils n'ont rien à recalculer. L'écran lit ce que le toucher a ajouté à la contenance pour l'envol « mare pleine ».

**Tech Stack :** Unity 6, C# 9, UI Toolkit, NUnit (EditMode et PlayMode), outillage `outils/unity.sh` (éditeur fermé).

**Spec :** `docs/superpowers/specs/2026-11-14-le-toucher-hate-le-sejour-design.md`

## Global Constraints

- `SECONDES_DE_SEJOUR_PAR_TOUCHER = 0.25` ; `PART_DE_SEJOUR_PAR_RANG_DU_TOUCHER = 0.1` ; `RANG_MAX_DE_L_AMELIORATION_DU_TOUCHER = 10` — graines `[P]` dans `Noyau/Constantes.cs`.
- L'acquis avance par la même courbe que le temps : `A∞ + (A − A∞)·exp(−s/τ)`, `τ = Reducteur.TauDuSejourSecondes(etat)`.
- Un toucher ne change ni `TempsJeuSecondes`, ni `DureeSecondes`, ni `ProductionPicParSeconde`, ni le Souffle.
- Tout texte à l'écran vit dans `Noyau/Donnees/Textes.cs` et passe `LexiqueTests` ; « place » est un mot mort (ni texte, ni identifiant).
- Parité : la politique historique du simulateur ne touche pas → `parties.json` et `hors-ligne.json` ne doivent pas bouger. `donnees.json` ne porte pas de rang max : il ne bouge pas non plus (la spec disait le contraire ; c'est une erreur de la spec, à noter dans sa ligne d'implémentation).
- Commits terminés par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Fins de ligne LF.
- Lancer les suites : `outils/unity.sh tests EditMode [filtre]`, `outils/unity.sh tests PlayMode [filtre]`. Sortie attendue : `échoués 0`.

## Review Focus

1. **Mare pleine et acquis déjà saturé** — un toucher ne doit rien changer (même référence) : sinon `Partie` émet des changements à vide dix fois par seconde. → Tâche 1, test `Acquis_au_plafond_et_mare_pleine_rien_ne_change`.
2. **n touchers d'un coup = n touchers un à un, acquis compris** (le simulateur touche par lots de 60 à 600) : sans ça, la mesure ment. → Tâche 1, le test existant `N_touchers_d_un_coup_valent_n_touchers_un_a_un` compare aussi l'acquis.
3. **Le mana est plafonné par la contenance APRÈS l'avance de l'acquis** — sinon un toucher sur mare pleine ne rapporterait jamais de mana alors que la contenance vient de monter. → Tâche 1, test `Mare_pleine_le_toucher_fait_porter_plus`.
4. **L'amélioration du toucher au rang 10 ne s'achète plus**, y compris par « Tout améliorer » (`Reducteur.AcheterToutesLesAmeliorations`, qui saute déjà ce que `AmeliorationAuMaximum` dit plein). → Tâche 1, test `L_amelioration_du_toucher_s_arrete_au_rang_10`.
5. **L'envol « mare pleine »** ne doit pas afficher `+0` : gain de mana nul → `+N à porter` ; les deux nuls → rien. → Tâche 2, test PlayMode.

---

### Task 1 : le noyau — un toucher fait avancer le séjour

**Files :**
- Modify : `Assets/IdlePond/Noyau/Constantes.cs` (bloc « Toucher la mare »)
- Modify : `Assets/IdlePond/Noyau/Economie.cs` (`AmeliorationAuMaximum` ; nouvelle `SejourDuToucher` près de `ValeurDuToucher`)
- Modify : `Assets/IdlePond/Noyau/Reducteur.cs` (`Toucher`)
- Test : `Assets/IdlePond/Tests/ToucherTests.cs`

**Interfaces :**
- Produces : `public static double Economie.SejourDuToucher(EtatJeu etat)` — secondes de séjour d'un toucher, `0.25 × (1 + 0.1 × min(rang, 10))`.
- Produces : `Reducteur.Toucher(EtatJeu etat, int fois = 1)` garde sa signature `(EtatJeu Etat, Decimal Gain)` ; `Gain` reste le mana gagné.
- Produces : constantes `SECONDES_DE_SEJOUR_PAR_TOUCHER`, `PART_DE_SEJOUR_PAR_RANG_DU_TOUCHER`, `RANG_MAX_DE_L_AMELIORATION_DU_TOUCHER`.

- [ ] **Step 1 : écrire les tests qui échouent** (dans `ToucherTests`, section « toucher ») :

```csharp
[Test] public void Un_toucher_fait_avancer_le_sejour()
{   // acquis de départ 0, mana 0
    var etat = AvecMana(EtatDeTravail.Creer(), new Decimal(0));
    var tau = Reducteur.TauDuSejourSecondes(etat);
    var attendu = Constantes.ACQUIS_MAX + (etat.Cycle.AcquisDeSejour - Constantes.ACQUIS_MAX) * Math.Exp(-0.25 / tau);
    var (apres, _) = Reducteur.Toucher(etat);
    Assert.That(apres.Cycle.AcquisDeSejour, Is.EqualTo(attendu).Within(1e-15));
    Assert.That(apres.Cycle.DureeSecondes, Is.EqualTo(etat.Cycle.DureeSecondes));
    Assert.That(apres.TempsJeuSecondes, Is.EqualTo(etat.TempsJeuSecondes));
    Assert.That(apres.Cycle.ProductionPicParSeconde, Is.SameAs(etat.Cycle.ProductionPicParSeconde));
}
[Test] public void Le_sejour_d_un_toucher_monte_de_dix_pour_cent_par_rang()
    // rangs 0, 3, 10, 12 → 0.25, 0.325, 0.5, 0.5 (borné)
[Test] public void Mare_pleine_le_toucher_fait_porter_plus()
    // ContenanceMana = 1000, mana = Contenance(etat) : après, Contenance(apres) > Contenance(etat),
    // ManaCourant == Contenance(apres) (le toucher remplit la place neuve), apres pas SameAs etat
[Test] public void Acquis_au_plafond_et_mare_pleine_rien_ne_change()
    // AcquisDeSejour = ACQUIS_MAX, mana = Contenance : Is.SameAs, gain nul
[Test] public void L_amelioration_du_toucher_s_arrete_au_rang_10()
    // AuRangDuToucher(.., 10) → AmeliorationAuMaximum vrai ; Souffle 1e30 →
    // Reducteur.AcheterUneAmelioration(etat, TOUCHER_ID) Is.SameAs etat ; au rang 9 → faux
```

Modifier aussi :
- `N_touchers_d_un_coup_valent_n_touchers_un_a_un` : comparer en plus `AcquisDeSejour` (`Within(1e-12)`).
- `Mare_pleine_rien_ne_change` : à supprimer, remplacé par les deux tests « mare pleine » ci-dessus.
- `Le_gain_est_plafonne_par_la_contenance` et `N_touchers_d_un_coup_restent_plafonnes` : le plafond est `Economie.Contenance(apres)`, pas `Contenance(etat)`.

- [ ] **Step 2 : lancer** `outils/unity.sh tests EditMode Toucher` — attendu : erreurs de compilation (`SejourDuToucher`, constantes manquantes).

- [ ] **Step 3 : implémenter.**
  - Constantes : les trois graines, avec un commentaire `[P]` qui renvoie à la spec du 2026-11-14 (0,25 s : ×1,5 à deux touchers par seconde).
  - `AmeliorationAuMaximum` : portée `Sejour` → `RANG_MAX_DE_L_AMELIORATION_DE_SEJOUR` ; portée `Toucher` → `RANG_MAX_DE_L_AMELIORATION_DU_TOUCHER` ; les autres jamais. Mettre à jour son commentaire (« Seule l'amélioration de séjour… » devient faux).
  - `MultiplicateurDuToucher` : borner aussi le rang à 10 (`Math.Min`), comme `FacteurDuSejour`.
  - `Reducteur.Toucher` : `fois <= 0` → inchangé. Sinon avancer l'acquis de `fois × SejourDuToucher` s par la formule du Tick, lire la contenance sur l'état avancé, ajouter `min(valeur × fois, reste)`. Si ni l'acquis ni le mana ne changent, rendre la même référence. Le commentaire dit les deux effets et que le crédit hors ligne ne touche toujours pas.

- [ ] **Step 4 : lancer** `outils/unity.sh tests EditMode` — attendu : `échoués 0`. Si `PlancherDeCadenceTests` bouge (le joueur scripté touche toutes les deux secondes), relever le nouveau chiffre et mettre à jour le tableau en commentaire du test, sans desserrer la limite de 300 s.

- [ ] **Step 5 : commit** — `feat(toucher): un toucher fait avancer le séjour`.

### Task 2 : le jeu et l'écran

**Files :**
- Modify : `Assets/IdlePond/Jeu/Partie.cs` (`Toucher`)
- Modify : `Assets/IdlePond/Jeu/UI/CoucheDuToucher.cs`, `Assets/IdlePond/Jeu/UI/RacineDeLInterface.cs` (l'appel)
- Modify : `Assets/IdlePond/Jeu/UI/FicheDuHeros.cs` (tuile « Par toucher »)
- Modify : `Assets/IdlePond/Noyau/Donnees/Textes.cs`
- Test : `Assets/IdlePond/TestsDeJeu/ToucherLaMareTests.cs`, `Assets/IdlePond/Tests/LexiqueTests.cs` (rien à ajouter si les textes passent par les listes existantes)

**Interfaces :**
- Consumes : `Reducteur.Toucher`, `Economie.SejourDuToucher`, `Economie.Contenance`.
- Produces : `public readonly record struct GainDuToucher(Decimal Mana, Decimal APorter)` dans `Jeu/Partie.cs` ; `public GainDuToucher Partie.Toucher()`. `APorter = Contenance(après) − Contenance(avant)`. Toucher ignoré par la cadence ou sans effet → les deux à zéro.
- Produces : `CoucheDuToucher(VisualElement racine, Func<GainDuToucher> toucher)`.

- [ ] **Step 1 : tests PlayMode qui échouent** (dans `ToucherLaMareTests`) :
  - `Mare_pleine_le_toucher_dit_ce_qu_on_porte_de_plus` : mana mis à `Contenance`, acquis à 0 ; après un toucher sur la couche, un envol au texte `"+" + Format.Montant(apres − avant) + " à porter"` (lire le texte via `E.GAIN_A_PORTER`).
  - `Mare_pleine_et_acquis_au_plafond_rien_ne_s_envole` : acquis à `ACQUIS_MAX`, mana plein → aucun envol visible.
  - Le test existant `Toucher_la_mare_monte_le_mana_et_montre_le_gain` reste vert tel quel (mare pas pleine → `+gain`).

- [ ] **Step 2 : lancer** `outils/unity.sh tests PlayMode Toucher` — attendu : compilation ou assertion en échec.

- [ ] **Step 3 : implémenter.**
  - `Textes.Ecran` : `GAIN_A_PORTER = "+{0} à porter"` ; `SEJOUR_PAR_TOUCHER = "et {0} s de séjour"` (libellé proposé, ajustable s'il ne passe pas le lexique). `AMELIORATION_DU_TOUCHER.Effet` : dire les deux effets, par ex. « quand tu touches l’eau, tu emportes davantage, et tu t’y fais plus vite ».
  - `Partie.Toucher` : rend `GainDuToucher` ; un toucher compte dans la fenêtre de cadence dès que l'état change (plus seulement quand le mana monte).
  - `CoucheDuToucher` : `Mana > 0` → `+mana` (inchangé) ; sinon `APorter > 0` → `GAIN_A_PORTER` ; sinon rien.
  - `FicheDuHeros` : la tuile « Par toucher » montre `+mana`, puis `SEJOUR_PAR_TOUCHER` avec `SejourDuToucher` formaté à deux décimales au plus, virgule française (`Format` a déjà de quoi ; sinon une petite fonction `Format.Secondes(double)` testée dans `FormatTests` : `0.25 → "0,25"`, `0.5 → "0,5"`).

- [ ] **Step 4 : lancer** `outils/unity.sh tests EditMode` puis `outils/unity.sh tests PlayMode` — attendu : `échoués 0` partout.

- [ ] **Step 5 : captures** `outils/unity.sh captures` ; regarder `Logs/captures/ui-telephone-toucher.png` et `ui-telephone-toi.png` (tuile lisible, rien ne déborde). Si la vue « toucher » ne montre pas la mare pleine, ajouter une vue `toucher-plein` dans `CapturesDeControleDesReglages.cs` sur le modèle de la vue « toucher ».

- [ ] **Step 6 : commit** — `feat(toucher): l'écran dit ce que le toucher fait porter`.

### Task 3 : la mesure et les documents

**Files :**
- Test : `Assets/IdlePond/Tests/SimulateurTests.cs`
- Run : `Assets/IdlePond/Tests/MesureDuToucherTests.cs` (à la demande)
- Modify : `docs/RESULTATS.md` (Finding 10), `docs/ROADMAP.md` (paragraphe du toucher), `Assets/IdlePond/Noyau/Constantes.cs` (commentaire du bloc toucher), la spec (ligne « Implémenté le … » avec l'écart `donnees.json`), `README.md` si le toucher y est décrit.

**Interfaces :**
- Consumes : `Politique.ToucherParSeconde`, `AcheterAuMieuxEnTouchant` (déjà là).

- [ ] **Step 1 : test du simulateur** `Toucher_raccourcit_la_premiere_vie` :

```csharp
var sans = Simuler(1, POLITIQUE_PAR_DEFAUT, 1).Cycles[0].DureeEcouleeSecondes;
var avec = Simuler(1, POLITIQUE_PAR_DEFAUT with { ToucherParSeconde = 2 }, 1).Cycles[0].DureeEcouleeSecondes;
Assert.That(avec, Is.LessThan(sans * 0.8));  // ×1,5 visé : environ 0,67
```

Le lancer **avant** la tâche 1 n'a pas de sens (tâches dans l'ordre) : il doit passer ici d'emblée ; c'est une garde du rythme voulu, pas un test de développement. Si `Toucher_avance_le_premier_blocage` casse, relire le chiffre et l'ajuster en le documentant.

- [ ] **Step 2 : lancer** `outils/unity.sh tests EditMode Simulateur` — `échoués 0`.

- [ ] **Step 3 : mesurer** `outils/unity.sh tests EditMode MesureDuToucher` ; lire `Logs/mesures/toucher.md`.

- [ ] **Step 4 : documents.** Finding 10 : garder la mesure d'avant comme « avant le 2026-11-14 », ajouter le tableau d'après (durées des vies 1 / 2 / 5 / 15, jeu actif sur quinze vies, rang du toucher, Souffle de la vie 15) et ce qu'il montre. ROADMAP : le « à trancher » devient « tranché le 2026-11-14 », avec ce qui reste (la durée totale visée croise maintenant la cadence de toucher). Spec : ligne d'implémentation et écarts.

- [ ] **Step 5 : suites complètes** EditMode et PlayMode — `échoués 0` ; vérifier `git diff --stat Assets/IdlePond/Tests/Reference` vide.

- [ ] **Step 6 : commit** — `docs+mesure: le toucher hâte le séjour, mesuré (Finding 10)`.
