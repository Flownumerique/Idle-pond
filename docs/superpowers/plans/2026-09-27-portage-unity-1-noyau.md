# Portage Unity, plan 1 — le noyau C# et sa parité

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Porter le noyau TypeScript d'IdlePond (`src/noyau/`, `src/donnees/`, `src/simulateur/`) en C# pur dans le projet Unity 6 à la racine du dépôt. On porte au lexique final du Codex, les tests sont portés en NUnit, et une parité mesurée contre des parties de référence produites par le TypeScript fait foi.

**Architecture:** Trois assemblies. `IdlePond.Noyau` (runtime, `noEngineReferences`) contient le `Decimal`, le PRNG, les types, les constantes, les données, la mécanique et la télémétrie pure. `IdlePond.Simulateur` (éditeur seulement, `noEngineReferences`) contient le simulateur et le calibreur. `IdlePond.Tests` (EditMode, NUnit) contient les tests. Avant de toucher au C#, un script TypeScript fige des fichiers JSON de référence (Decimal, PRNG, constantes, données, parties jouées, simulation). Chaque étape C# est vérifiée contre eux.

**Tech Stack:** Unity 6000.6.3f1 (C# 9), `com.unity.test-framework` 1.8.0 (NUnit), `com.unity.nuget.newtonsoft-json` 3.2.2 (tests seulement). Côté référence : TypeScript 5.9, `tsx`, `break_infinity.js` 2.2.0.

**Spec:** `docs/superpowers/specs/2026-09-27-portage-unity-design.md`, étapes 0 à 3 de son §5. Les étapes 4 à 8 (jeu, scènes, UI, revue, archivage) feront l'objet du **plan 2**. Il sera écrit quand ce plan-ci sera vert, sur les noms C# réellement produits.

**Ce que « porter » veut dire ici.** Pour le code de mécanique, **le TypeScript est la source**. Chaque tâche nomme le fichier `.ts` à traduire ligne à ligne, la table de renommage et les signatures C# attendues. Le plan ne recopie pas les 5 000 lignes : il donne le code complet de tout ce qui est **nouveau** (socle, `Decimal`, PRNG, types, références, outils de test, parité) et, pour le reste, les règles de traduction et les interfaces exactes. Les commentaires TypeScript sont portés avec le code : ils disent pourquoi une formule a cette forme.

## Global Constraints

- Unity **6000.6.3f1**, exécutable `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`. **L'éditeur doit être fermé sur ce projet** pendant chaque commande batch.
- Paquets épinglés : `"com.unity.test-framework": "1.8.0"`, `"com.unity.nuget.newtonsoft-json": "3.2.2"`.
- `IdlePond.Noyau` et `IdlePond.Simulateur` ont `"noEngineReferences": true`. Aucun `UnityEngine`, `DateTime.Now`, `DateTime.UtcNow`, `System.Random`, `Environment.TickCount` ni `Stopwatch` dans leurs sources.
- **Aucune mécanique ne change.** Formules, constantes et `D` par palier restent identiques au chiffre près. Seul le lexique change.
- Lexique (Codex §5) : `souffle` ; `Insufflation`, `Insuffler`, `insufflation-globale`, `insufflation-<espece>` ; `Renaissance`, `Renaitre`, `NombreDeRenaissances`. Aucun identifiant C# ne contient `foi`, `fidele`, `benediction`, `benir`, `eclore`, `eclosion`, `ponte`, `pondre`, `population`, `maturation`, `acclimatation`, `prestige`, `rebirth`, `gemme`, `perle`, `corail`, `layer`, `zone`, `biome`, `etage`, `strate`.
- **Exceptions figées** (Codex §7) : les identifiants de succès `franchissement-premiere-eclosion`, `franchissement-deuxieme-eclosion`, `franchissement-troisieme-eclosion` et `acte-premier-banc-de-cinq` restent tels quels. Ce sont des chaînes du registre, pas des identifiants C#.
- Tout texte formaté ou lu passe par `CultureInfo.InvariantCulture`. La machine de l'utilisateur est en `fr-FR`, où `double.Parse("2.4")` échoue.
- `Math.round` de JavaScript n'est **pas** `Math.Round` de .NET, qui arrondit les .5 au pair. On utilise partout `Decimal.JsRound`.
- Les dictionnaires d'état sont reconstruits dans l'**ordre du registre** (espèces, succès, insufflations), comme en TypeScript. Tout ce qui les énumère pour produire une sortie suit l'ordre du registre, jamais celui du dictionnaire.
- Code, commentaires et messages de commit en français. Constantes en `MAJUSCULES_SOULIGNEES`, comme dans `constantes.ts`, pour que l'audit ligne à ligne reste possible. Méthodes et propriétés en `PascalCase`.
- Nom des méthodes de test : le titre TypeScript exact, dans lequel tout caractère qui n'est ni une lettre (accents compris) ni un chiffre devient `_`. Le titre original est répété dans `[Description("…")]`.
- La sauvegarde C# repart en **version 1** : `Constantes.VERSION_SAVE = 1`.
- Tolérance de parité : **1e-9 relatif** pour les nombres. Égalité exacte pour les entiers, les booléens, les identifiants et l'état du PRNG.
- Chaque commit se termine par :
  ```
  Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01LV6Qfa1C7iiWEDFbp72Q3H
  ```

## Review Focus

- **Culture française.** Un `Decimal.Parse` ou un `ToString()` sans `InvariantCulture` casse sur la machine de l'utilisateur. Épinglé par `DecimalTests.Aller_retour_sous_la_culture_fr_FR` (tâche 3).
- **Arrondi des .5.** `Decimal.Add` arrondit `1e14 × mantisse` avec le `Math.round` de JavaScript ; un `Math.Round` .NET dériverait en silence sur les additions d'entiers. Épinglé par `DecimalTests.JsRound_arrondit_les_demis_vers_plus_l_infini` (tâche 3) et par `decimal.json`.
- **Ordre des achats.** Deux parties qui achètent les mêmes niveaux dans un ordre différent doivent produire le même instantané. Épinglé par `DeterminismeTests.L_ordre_des_achats_ne_change_pas_l_instantane` (tâche 7).
- **Actes impossibles.** Un identifiant inconnu, un `dt` nul, négatif ou `NaN` doivent rendre **le même** état, sans exception. Épinglé par `ReducteurTests.Un_acte_impossible_rend_le_meme_etat` et `Un_dt_non_positif_ou_NaN_ne_fait_rien` (tâche 7).
- **Chaînes JavaScript à relire.** Les références contiennent des `Decimal` sous forme `mantisse e exposant`, écrits par JavaScript (`1.5e25`, `9.999999999999998e2`, `0e0`). `Decimal.Parse` doit les relire exactement. Épinglé par `DecimalTests.Relit_les_chaines_ecrites_par_JavaScript` (tâche 3).

---

## Structure des fichiers

```
.gitignore                                        (modifié : entrées Unity)
Packages/manifest.json                            (modifié : 2 paquets)
outils/unity.sh                                   (nouveau : tests et méthodes en batch)
tests/parite/generer-references.ts                (nouveau : TypeScript → JSON de référence)
Assets/test.unity, Assets/test.unity.meta         (supprimés)
Assets/IdlePond/
  Noyau/IdlePond.Noyau.asmdef
  Noyau/IsExternalInit.cs
  Noyau/Nombres/Decimal.cs                         portage de break_infinity.js 2.2.0
  Noyau/Nombres/Prng.cs                            mulberry32, bit-exact
  Noyau/Types.cs                                   ← src/noyau/types.ts
  Noyau/Termes.cs                                  registres de termes et leurs identifiants
  Noyau/Constantes.cs                              ← src/noyau/constantes.ts
  Noyau/Densite.cs                                 ← src/noyau/densite.ts
  Noyau/Technique.cs                               ← src/noyau/technique.ts
  Noyau/Voix.cs                                    ← src/noyau/voix.ts
  Noyau/Economie.cs                                ← src/noyau/economie.ts
  Noyau/RegleDesSucces.cs                          ← src/noyau/succes.ts
  Noyau/Renaissance.cs                             ← src/noyau/eclosion.ts
  Noyau/Reducteur.cs                               ← src/noyau/noyau.ts
  Noyau/Telemetrie.cs                              ← src/adaptateurs/telemetrie.ts (partie pure)
  Noyau/Donnees/Assises.cs                         ← src/donnees/assises.ts
  Noyau/Donnees/Especes.cs                         ← src/donnees/especes.ts
  Noyau/Donnees/Paliers.cs                         ← src/donnees/paliers.ts
  Noyau/Donnees/Insufflations.cs                   ← src/donnees/benedictions.ts
  Noyau/Donnees/Echelles.cs                        ← src/donnees/echelles.ts
  Noyau/Donnees/NoeudsTechnique.cs                 ← src/donnees/noeuds-technique.ts
  Noyau/Donnees/Succes/Actes.cs                    ← src/donnees/succes/actes.ts
  Noyau/Donnees/Succes/Seuils.cs                   ← src/donnees/succes/seuils.ts
  Noyau/Donnees/Succes/Franchissements.cs          ← src/donnees/succes/franchissements.ts
  Noyau/Donnees/Succes/RegistreDesSucces.cs        ← src/donnees/succes/index.ts
  Noyau/Donnees/Textes.cs                          ← src/donnees/textes-provisoires.ts
  Simulateur/IdlePond.Simulateur.asmdef
  Simulateur/IsExternalInit.cs
  Simulateur/Simulateur.cs                         ← src/simulateur/simulateur.ts
  Simulateur/Calibreur.cs                          ← src/simulateur/calibreur.ts
  Tests/IdlePond.Tests.asmdef
  Tests/IsExternalInit.cs
  Tests/Reference/{decimal,prng,constantes,donnees,parties}.json   (générés par la tâche 2)
  Tests/Outils/References.cs                       lecture des JSON de référence
  Tests/Outils/Comparateur.cs                      ← tests/outils.ts (comparaison à tolérance)
  Tests/Outils/Instantane.cs                       état → JObject, mêmes clefs que le générateur
  Tests/Outils/EtatDeTravail.cs                    ← tests/etat-de-travail.ts
  Tests/Outils/Joueur.cs                           ← tests/joueur.ts
  Tests/Outils/SourceCSharp.cs                     retire commentaires et chaînes d'un .cs
  Tests/SocleTests.cs, DecimalTests.cs, PrngTests.cs, ConstantesTests.cs, DonneesTests.cs,
  Tests/EspecesTests.cs, SeuilsTests.cs, CaptationTests.cs, VoixTests.cs, ReducteurTests.cs,
  Tests/EquivalenceDePasTests.cs, DeterminismeTests.cs, ContenanceTests.cs, RedescenteTests.cs,
  Tests/HerosTests.cs, InsufflationsTests.cs, RenaissanceTests.cs, AmorcageTests.cs,
  Tests/CanonTests.cs, SimulateurTests.cs, PlancherDeCadenceTests.cs, PartieHeadlessTests.cs,
  Tests/PariteTests.cs, ArchitectureTests.cs, LexiqueTests.cs
```

**Espaces de noms.** `IdlePond.Noyau` pour tout le noyau, y compris `Decimal` et `Prng`. Dans `namespace IdlePond.Noyau { … }` et `IdlePond.Noyau.Donnees`, les types de l'espace de noms passent avant les `using` : `Decimal` y désigne le nôtre même quand le fichier importe `System`. `IdlePond.Noyau.Donnees` pour les données. `IdlePond.Simulateur` et `IdlePond.Tests`. **Tout fichier hors de `IdlePond.Noyau` qui importe à la fois `System` et `IdlePond.Noyau` écrit `using Decimal = IdlePond.Noyau.Decimal;`**, sinon `CS0104 : 'Decimal' est ambigu`. Un alias l'emporte sur un import d'espace de noms. Dans le doute, ajouter l'alias partout.

**Renommages (TypeScript → C#).** Ils s'appliquent partout : identifiants, clefs de référence, textes.

| TypeScript | C# |
|---|---|
| `Benediction`, `BenedictionId`, `PorteeDeBenediction` | `Insufflation`, `string`, `PorteeDInsufflation` |
| `BENEDICTIONS`, `benedictionParId`, `benedictionCibleeDe` | `Insufflations.Toutes`, `Insufflations.ParId`, `Insufflations.CibleeDe` |
| `BENEDICTION_GLOBALE_ID = 'benediction-globale'` | `Insufflations.GLOBALE_ID = "insufflation-globale"` |
| `` `benediction-${espece.id}` `` | `$"insufflation-{espece.Id}"` |
| `benir`, `benirAuMieux` | `Reducteur.Insuffler`, `Simulateur.InsufflerAuMieux` |
| `rangDeBenediction`, `debitBeni`, `multiplicateurDeBenediction`, `coutDeBenediction` | `RangDInsufflation`, `DebitInsuffle`, `MultiplicateurDInsufflation`, `CoutDInsufflation` |
| `permanent.benedictions` | `Permanent.Insufflations` |
| termes `multiplicateur_benediction`, `benediction_globale`, `cout_benediction` | `MultiplicateurInsufflation` (`multiplicateur_insufflation`), `InsufflationGlobale` (`insufflation_globale`), `CoutInsufflation` (`cout_insufflation`) |
| source de terme `'benediction'` | `QuoiSource.Insufflation` |
| `BENEDICTION_CIBLEE_PAR_RANG`, `BENEDICTION_GLOBALE_PAR_RANG` | `INSUFFLATION_CIBLEE_PAR_RANG`, `INSUFFLATION_GLOBALE_PAR_RANG` |
| `SOUFFLE_COUT_DE_BENEDICTION_CIBLEE`, `…_GLOBALE`, `RATIO_COUT_DE_BENEDICTION` | `SOUFFLE_COUT_D_INSUFFLATION_CIBLEE`, `…_GLOBALE`, `RATIO_COUT_D_INSUFFLATION` |
| `TEXTE_DE_LA_BENEDICTION_GLOBALE`, `TEXTE_DE_BENEDICTION_CIBLEE` | `Textes.INSUFFLATION_GLOBALE`, `Textes.INSUFFLATION_CIBLEE` |
| `eclore`, `doitEclore` | `Renaissance.Renaitre`, `Simulateur.DoitRenaitre` |
| `permanent.nombreEclosions`, `Releve.nombreEclosions` | `NombreDeRenaissances` |
| déclencheur `'eclosions'` | `QuoiDeclencheur.Renaissances` |
| branche technique `'eclosion'` | `BrancheTechnique.Renaissance` (clef `renaissance`) |
| `NOMBRE_D_ECLOSIONS_VISE`, `CONTENANCE_PAR_ECLOSION` | `NOMBRE_DE_RENAISSANCES_VISE`, `CONTENANCE_PAR_RENAISSANCE` |
| `Politique.fractionDeSaturationPourEclore` | `FractionDeSaturationPourRenaitre` |
| module `noyau.ts` | classe `Reducteur` (une classe `Noyau` masquerait l'espace de noms) |
| module `succes.ts` | classe `RegleDesSucces` (le record `Succes` existe déjà) |
| `palierDeVoix(etat)`, `palierDeVoixApres(n)`, `voixAuMoins` | `Voix.PalierDe(etat)`, `Voix.PalierApres(n)`, `Voix.AuMoins` |

**Traduction des formes.**

| TypeScript | C# |
|---|---|
| `{ ...etat, cycle: { ...etat.cycle, x } }` | `etat with { Cycle = etat.Cycle with { X = x } }` |
| `Readonly<Record<string, T>>` | `IReadOnlyDictionary<string, T>` (dictionnaire neuf à chaque modification) |
| `readonly T[]` | `IReadOnlyList<T>` (tableau neuf à chaque modification) |
| `x ?? y` sur un index hors borne | test de borne explicite (un index hors borne lève en C#) |
| `number` compteur ou index | `int` ; `number` quantité | `double` |
| `Math.pow`, `Math.exp`, `Math.log`, `Math.floor` | `Math.Pow`, `Math.Exp`, `Math.Log`, `Math.Floor` |
| `Math.round` | `Decimal.JsRound` |
| `new Decimal(n)`, `new Decimal('1e12')` | `new Decimal(n)`, `Decimal.Parse("1e12")` |
| `d.mul(2)` (nombre) et `d.mul(autre)` (Decimal) | `d.Mul(2.0)` et `d.Mul(autre)` : deux surcharges distinctes, comme en JS |
| `Decimal.min(a, b)`, `Decimal.max(a, b)` | `Decimal.Min(a, b)`, `Decimal.Max(a, b)` |
| `throw new Error(msg)` à la construction des données | `throw new InvalidOperationException(msg)` |
| `undefined` pour « pas d'espèce » | `null` |

---

### Task 1: Le socle Unity et la commande de tests

**Files:**
- Modify: `.gitignore`, `Packages/manifest.json`
- Delete: `Assets/test.unity`, `Assets/test.unity.meta`
- Create: `outils/unity.sh`
- Create: `Assets/IdlePond/Noyau/IdlePond.Noyau.asmdef`, `Assets/IdlePond/Noyau/IsExternalInit.cs`
- Create: `Assets/IdlePond/Simulateur/IdlePond.Simulateur.asmdef`, `Assets/IdlePond/Simulateur/IsExternalInit.cs`
- Create: `Assets/IdlePond/Tests/IdlePond.Tests.asmdef`, `Assets/IdlePond/Tests/IsExternalInit.cs`, `Assets/IdlePond/Tests/SocleTests.cs`

**Interfaces:**
- Produces: `outils/unity.sh tests [EditMode|PlayMode] [filtre]` imprime un résumé (`total`, `réussis`, `échoués`, et chaque échec avec son message). Il sort en 0 si tout passe et en 1 sinon, y compris sur une erreur de compilation, dont il imprime les lignes `error CS`. `outils/unity.sh methode Espace.Classe.Methode` lance une méthode statique en batch.
- Produces: les trois assemblies `IdlePond.Noyau`, `IdlePond.Simulateur`, `IdlePond.Tests`.

- [ ] **Step 1: Compléter le `.gitignore`**

Ajouter à la fin de `.gitignore` :

```gitignore

# Unity — généré, jamais versionné
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Mm]emoryCaptures/
*.csproj
*.slnx
*.sln
*.pidb
*.booproj
*.unityproj
.vscode/
```

Run: `git status --short`
Expected: `Library/`, `Temp/`, `UserSettings/`, `.vscode/`, `Idle-pond.slnx` ont disparu de la liste. `Assets/`, `Packages/` et `ProjectSettings/` y restent.

- [ ] **Step 2: Supprimer la scène d'essai et ajouter les paquets**

```bash
rm Assets/test.unity Assets/test.unity.meta
```

Dans `Packages/manifest.json`, ajouter en tête de `"dependencies"` :

```json
    "com.unity.nuget.newtonsoft-json": "3.2.2",
    "com.unity.test-framework": "1.8.0",
```

- [ ] **Step 3: Écrire `outils/unity.sh`**

```bash
#!/usr/bin/env bash
# IdlePond — Unity en batch, sans ouvrir l'éditeur.
#
#   outils/unity.sh tests [EditMode|PlayMode] [filtre]
#   outils/unity.sh methode Espace.Classe.Methode
#
# Unity refuse d'ouvrir en batch un projet déjà ouvert : fermer l'éditeur avant.
set -uo pipefail

UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe}"
RACINE="$(cd "$(dirname "$0")/.." && pwd)"
PROJET="$(cd "$RACINE" && pwd -W 2>/dev/null || echo "$RACINE")"
SORTIE="$RACINE/Logs/batch"
mkdir -p "$SORTIE"
JOURNAL="$SORTIE/unity.log"

erreurs_de_compilation() {
  if grep -q "another Unity instance is running\|It looks like another Unity instance" "$JOURNAL" 2>/dev/null; then
    echo "L'éditeur Unity a ce projet ouvert : ferme-le puis relance." >&2
  fi
  grep -E "error CS[0-9]+" "$JOURNAL" 2>/dev/null | sort -u >&2
}

case "${1:-}" in
  tests)
    PLATEFORME="${2:-EditMode}"
    FILTRE="${3:-}"
    RESULTATS="$SORTIE/resultats-$PLATEFORME.xml"
    rm -f "$RESULTATS"
    ARGS=(-batchmode -nographics -projectPath "$PROJET" -runTests -testPlatform "$PLATEFORME"
          -testResults "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/resultats-$PLATEFORME.xml"
          -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log")
    [ -n "$FILTRE" ] && ARGS+=(-testFilter "$FILTRE")
    "$UNITY" "${ARGS[@]}"
    if [ ! -f "$RESULTATS" ]; then
      echo "Aucun résultat : la compilation a probablement échoué." >&2
      erreurs_de_compilation
      exit 1
    fi
    python - "$RESULTATS" <<'PY'
import sys, xml.etree.ElementTree as ET
racine = ET.parse(sys.argv[1]).getroot()
print(f"total {racine.get('total')} · réussis {racine.get('passed')} · échoués {racine.get('failed')} · ignorés {racine.get('skipped')}")
echecs = 0
for cas in racine.iter('test-case'):
    if cas.get('result') == 'Failed':
        echecs += 1
        message = (cas.findtext('failure/message') or '').strip()
        print(f"ÉCHEC {cas.get('fullname')}\n    {message[:2000]}")
sys.exit(1 if echecs or racine.get('result', '').startswith('Failed') else 0)
PY
    ;;
  methode)
    "$UNITY" -batchmode -nographics -quit -projectPath "$PROJET" -executeMethod "$2" \
      -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log"
    CODE=$?
    [ $CODE -ne 0 ] && erreurs_de_compilation
    exit $CODE
    ;;
  *)
    echo "usage : outils/unity.sh tests [EditMode|PlayMode] [filtre] | methode Espace.Classe.Methode" >&2
    exit 2
    ;;
esac
```

Run: `chmod +x outils/unity.sh`

- [ ] **Step 4: Écrire les trois asmdefs et le shim des records**

`Assets/IdlePond/Noyau/IdlePond.Noyau.asmdef` :

```json
{
    "name": "IdlePond.Noyau",
    "rootNamespace": "IdlePond.Noyau",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/IdlePond/Simulateur/IdlePond.Simulateur.asmdef` : l'outil de mesure ne va pas dans le build joueur.

```json
{
    "name": "IdlePond.Simulateur",
    "rootNamespace": "IdlePond.Simulateur",
    "references": ["IdlePond.Noyau"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
}
```

`Assets/IdlePond/Tests/IdlePond.Tests.asmdef` :

```json
{
    "name": "IdlePond.Tests",
    "rootNamespace": "IdlePond.Tests",
    "references": ["UnityEngine.TestRunner", "UnityEditor.TestRunner", "IdlePond.Noyau", "IdlePond.Simulateur"],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll", "Newtonsoft.Json.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`IsExternalInit.cs`, identique dans les trois dossiers. `internal`, donc chaque assembly a le sien sans collision :

```csharp
// C# 9 : les records et `init` exigent ce type, que la bibliothèque de base
// d'Unity ne fournit pas. Il n'a aucun comportement.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
```

- [ ] **Step 5: Écrire un test qui prouve le socle**

`Assets/IdlePond/Tests/SocleTests.cs` :

```csharp
using System.Globalization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class SocleTests
    {
        [Test, Description("le socle compile et NUnit tourne en batch")]
        public void Le_socle_compile_et_NUnit_tourne_en_batch()
        {
            Assert.That(JObject.Parse("{\"a\":2.4}")["a"].Value<double>(), Is.EqualTo(2.4));
            Assert.That(2.4.ToString(CultureInfo.InvariantCulture), Is.EqualTo("2.4"));
        }
    }
}
```

- [ ] **Step 6: Lancer les tests en batch**

Run: `outils/unity.sh tests EditMode`
Expected: `total 1 · réussis 1 · échoués 0 · ignorés 0`, code de sortie 0. La première ouverture importe le projet et résout les paquets ; compter plusieurs minutes et un délai de 600 s.

- [ ] **Step 7: Commit**

Unity a créé des `.meta` à côté de chaque fichier et dossier de `Assets/` : ils se versionnent.

```bash
git add .gitignore Packages ProjectSettings Assets outils/unity.sh
git status --short   # vérifier : ni Library/ ni Temp/ ni *.csproj
git commit -m "chore(unity): le socle — asmdefs, paquets de test, commande batch"
```

---

### Task 2: Les références TypeScript

Le TypeScript est encore la vérité. Cette tâche en extrait **une fois** tout ce que le C# devra reproduire, sous les noms du nouveau lexique.

**Files:**
- Create: `tests/parite/generer-references.ts`
- Create (générés): `Assets/IdlePond/Tests/Reference/decimal.json`, `prng.json`, `constantes.json`, `donnees.json`, `parties.json`

**Interfaces:**
- Produces: les cinq fichiers JSON ci-dessous. Leurs clefs sont celles que `Instantane.De` (tâche 7) produit en C#.
  - `decimal.json` : `{ valeurs: string[], cas: { op, a, b?, x?, r }[] }`. `a` et `b` sont des index dans `valeurs`, `x` est un nombre, `r` est le résultat : `"m e"` pour un Decimal, un nombre ou un booléen sinon.
  - `prng.json` : `{ suites: { graine: number, tirages: [valeur, graineSuivante][] }[] }`.
  - `constantes.json` : `{ nombres: { NOM: valeur }, SEUILS_DE_JALON, COUTS_DE_NOEUD, multiplicateurDePalier, densiteExposant }`.
  - `donnees.json` : `{ paliersLivres, assises, especes, paliers, insufflations, succes }`.
  - `parties.json` : `{ scenarios: { nom, instants: Instantane[] }[], simulation: { instant, cyclesAcheves, secondesActives, secondesEcoulees } }`.

- [ ] **Step 1: Écrire le générateur**

`tests/parite/generer-references.ts` :

```ts
/**
 * Parties de référence pour le portage Unity — spec 2026-09-27 §3.
 *
 * Lancé UNE fois, pendant que le TypeScript est encore la vérité :
 *   npx tsx tests/parite/generer-references.ts
 *
 * Tout ce qui sort d'ici porte déjà les noms du Codex (Souffle, insufflation,
 * renaissance) : c'est le C# qui doit s'y conformer, pas l'inverse. Un Decimal
 * s'écrit « mantisse e exposant », exactement, sans passer par toString()
 * qui arrondit les quasi-entiers.
 */
import { mkdirSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import Decimal from 'break_infinity.js'
import type { EtatJeu } from '../../src/noyau/types'
import * as C from '../../src/noyau/constantes'
import {
  ameliorer,
  benir,
  contenance,
  creuser,
  debloquer,
  eauTroublee,
  eclore,
  estBloque,
  gainDeSoufflePrevu,
  grandir,
  partDeContenance,
  productionTotaleParSeconde,
  tick,
  tirer,
} from '../../src/noyau/noyau'
import { ASSISES, PALIERS_LIVRES } from '../../src/donnees/assises'
import { ESPECES } from '../../src/donnees/especes'
import { PALIERS } from '../../src/donnees/paliers'
import { BENEDICTIONS, BENEDICTION_GLOBALE_ID } from '../../src/donnees/benedictions'
import { SUCCES } from '../../src/donnees/succes/index'
import { simuler } from '../../src/simulateur/simulateur'
import { etatDeTravail } from '../etat-de-travail'
import { rejoue } from '../joueur'

const DOSSIER = fileURLToPath(new URL('../../Assets/IdlePond/Tests/Reference/', import.meta.url))

/* ─── Le lexique du Codex ───────────────────────────────────────────────── */

const insufflation = (id: string) => id.replace(/^benediction-/, 'insufflation-')
const terme = (t: string) =>
  ({ multiplicateur_benediction: 'multiplicateur_insufflation', benediction_globale: 'insufflation_globale', cout_benediction: 'cout_insufflation' })[t] ?? t
const constante = (nom: string) =>
  ({
    BENEDICTION_CIBLEE_PAR_RANG: 'INSUFFLATION_CIBLEE_PAR_RANG',
    BENEDICTION_GLOBALE_PAR_RANG: 'INSUFFLATION_GLOBALE_PAR_RANG',
    SOUFFLE_COUT_DE_BENEDICTION_CIBLEE: 'SOUFFLE_COUT_D_INSUFFLATION_CIBLEE',
    SOUFFLE_COUT_DE_BENEDICTION_GLOBALE: 'SOUFFLE_COUT_D_INSUFFLATION_GLOBALE',
    RATIO_COUT_DE_BENEDICTION: 'RATIO_COUT_D_INSUFFLATION',
    NOMBRE_D_ECLOSIONS_VISE: 'NOMBRE_DE_RENAISSANCES_VISE',
    CONTENANCE_PAR_ECLOSION: 'CONTENANCE_PAR_RENAISSANCE',
  })[nom] ?? nom

/** Exact : la mantisse et l'exposant tels quels, jamais arrondis. */
const d = (x: Decimal) => `${x.mantissa}e${x.exponent}`

/* ─── L'instantané d'un état ────────────────────────────────────────────── */

function instantane(etat: EtatJeu) {
  const c = etat.cycle
  const p = etat.permanent
  const t = etat.telemetrie
  return {
    tempsJeuSecondes: etat.tempsJeuSecondes,
    limiteDeContenu: etat.limiteDeContenu,
    prng: { graine: etat.prng.graine },
    cycle: {
      manaCourant: d(c.manaCourant),
      paliersOuverts: c.paliersOuverts,
      especes: Object.fromEntries(
        ESPECES.filter((e) => c.especes[e.id] !== undefined).map((e) => [
          e.id,
          { debloquee: c.especes[e.id].debloquee, niveau: c.especes[e.id].niveau },
        ]),
      ),
      productionPicParSeconde: d(c.productionPicParSeconde),
      dureeSecondes: c.dureeSecondes,
      acquisDeSejour: c.acquisDeSejour,
      niveauDuHeros: c.niveauDuHeros,
    },
    permanent: {
      densites: [...p.densites],
      souffle: d(p.souffle),
      contenanceMana: d(p.contenanceMana),
      couches: [...p.couches],
      profondeurMaxAtteinte: p.profondeurMaxAtteinte,
      compteursTechnique: {
        creusement: p.compteursTechnique.creusement,
        amelioration: p.compteursTechnique.amelioration,
        recrutement: p.compteursTechnique.recrutement,
        entretien: p.compteursTechnique.entretien,
        construction: p.compteursTechnique.construction,
        renaissance: p.compteursTechnique.eclosion,
      },
      noeudsTechnique: [...p.noeudsTechnique],
      succes: Object.fromEntries(
        SUCCES.filter((s) => p.succes[s.id] !== undefined).map((s) => [
          s.id,
          { obtenuAuCycle: p.succes[s.id].obtenuAuCycle, registre: p.succes[s.id].registre },
        ]),
      ),
      nombreDeRenaissances: p.nombreEclosions,
      especesAyantAtteintCent: [...p.especesAyantAtteintCent],
      manaAmbiant: d(p.manaAmbiant),
      heuresHorsLigneCreditees: p.heuresHorsLigneCreditees,
      insufflations: Object.fromEntries(
        BENEDICTIONS.filter((b) => p.benedictions[b.id] !== undefined).map((b) => [
          insufflation(b.id),
          p.benedictions[b.id],
        ]),
      ),
    },
    telemetrie: {
      cycles: t.cycles.map((m) => ({
        index: m.index,
        dureeEcouleeSecondes: m.dureeEcouleeSecondes,
        paliersOuverts: m.paliersOuverts,
        productionPicParSeconde: d(m.productionPicParSeconde),
        souffleGagne: d(m.souffleGagne),
      })),
      secondesDepuisDernierSucces: t.secondesDepuisDernierSucces,
      intervallesEntreSucces: [...t.intervallesEntreSucces],
    },
    derives: {
      production: d(productionTotaleParSeconde(etat)),
      contenance: d(contenance(etat)),
      partDeContenance: partDeContenance(etat),
      eauTroublee: eauTroublee(etat),
      estBloque: estBloque(etat),
      gainDeSoufflePrevu: d(gainDeSoufflePrevu(etat)),
    },
  }
}

/* ─── Les scénarios — le C# les rejoue à l'identique (PariteTests) ──────── */

function scenarioSequence() {
  const SEQUENCE = [0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900]
  let etat = etatDeTravail(4242)
  const instants = [instantane(etat)]
  for (const dt of SEQUENCE) {
    etat = tick(etat, dt)
    instants.push(instantane(etat))
  }
  return { nom: 'sequence', instants }
}

function scenarioJoueur() {
  return { nom: 'joueur', instants: [60, 600, 1800, 3600].map((s) => instantane(rejoue(s))) }
}

function scenarioRenaissances() {
  let etat = etatDeTravail(777)
  const instants = [instantane(etat)]
  for (let cycle = 0; cycle < 3; cycle += 1) {
    etat = tick(etat, 3600)
    for (let i = 0; i < 3; i += 1) etat = grandir(etat)
    etat = eclore(etat)
    instants.push(instantane(etat))
    etat = benir(etat, BENEDICTION_GLOBALE_ID)
    etat = benir(etat, 'benediction-vairon')
    etat = creuser(etat)
    etat = debloquer(etat, 'vairon')
    for (let n = 0; n < 20; n += 1) etat = ameliorer(etat, 'vairon')
    etat = tick(etat, 600)
    instants.push(instantane(etat))
  }
  return { nom: 'renaissances', instants }
}

/* ─── Decimal ──────────────────────────────────────────────────────────── */

function referencesDecimal() {
  const valeurs = [
    '0', '1', '-1', '0.5', '2.4', '116', '299', '18', '0.1', '0.2', '1e-7', '123456789.123',
    '1e15', '1e21', '5e-324', '1.7976931348623157e308', '1e500', '-3.5e-400', '9.99999999999999e99',
    '47.6', '60', '1e12', '1e14', '3.3333333333333335', '-2.5', '1234.5', '1e-14', '2.5e-14',
  ].map((s) => new Decimal(s))
  const nombres = [0, 1, -1, 0.5, 2, 3.7, -1.5, 62, 1.15, 2.4, 1e300, 1e308, -1e308, 0.03]
  const cas: unknown[] = []
  // JSON ne sait pas écrire l'infini ni NaN : ils passent en chaîne, que le C# relit.
  const r = (x: Decimal | number | boolean) =>
    x instanceof Decimal ? d(x) : typeof x === 'number' && !Number.isFinite(x) ? String(x) : x
  valeurs.forEach((a, i) => {
    for (const op of ['neg', 'abs', 'recip', 'floor', 'round', 'ceil', 'toNumber', 'log10', 'exp'] as const) {
      if (op === 'exp' && Math.abs(a.toNumber()) > 1000) continue
      cas.push({ op, a: i, r: r((a[op] as () => Decimal | number)()) })
    }
    valeurs.forEach((b, j) => {
      for (const op of ['add', 'sub', 'mul', 'div', 'eq', 'lt', 'gt', 'lte', 'gte', 'max', 'min', 'cmp'] as const) {
        cas.push({ op, a: i, b: j, r: r((a[op] as (v: Decimal) => Decimal | number | boolean)(b)) })
      }
    })
    for (const x of nombres) {
      cas.push({ op: 'mulNombre', a: i, x, r: r(a.mul(x)) })
      cas.push({ op: 'addNombre', a: i, x, r: r(a.add(x)) })
      cas.push({ op: 'pow', a: i, x, r: r(a.pow(x)) })
    }
  })
  for (const base of [10, 2.4, 1.15, 2, 0.5]) {
    for (const x of [0, 1, 2, 3.5, 62, 2047, -3]) cas.push({ op: 'powStatique', base, x, r: r(Decimal.pow(base, x)) })
  }
  return { valeurs: valeurs.map(d), cas }
}

/* ─── PRNG ─────────────────────────────────────────────────────────────── */

function referencesPrng() {
  return {
    suites: [0, 1, 12345, 4242, 4294967295, 2654435769].map((graine) => {
      let prng = { graine }
      const tirages: [number, number][] = []
      for (let i = 0; i < 20; i += 1) {
        const [valeur, suivant] = tirer(prng)
        tirages.push([valeur, suivant.graine])
        prng = suivant
      }
      return { graine, tirages }
    }),
  }
}

/* ─── Constantes et données ────────────────────────────────────────────── */

function referencesConstantes() {
  const nombres = Object.fromEntries(
    Object.entries(C)
      .filter(([nom, v]) => typeof v === 'number' && nom !== 'VERSION_SAVE')
      .map(([nom, v]) => [constante(nom), v]),
  )
  return {
    nombres,
    SEUILS_DE_JALON: C.SEUILS_DE_JALON,
    COUTS_DE_NOEUD: C.COUTS_DE_NOEUD,
    multiplicateurDePalier: C.multiplicateurDePalier(),
    densiteExposant: C.densiteExposant(),
  }
}

function referencesDonnees() {
  return {
    paliersLivres: PALIERS_LIVRES,
    assises: ASSISES,
    especes: ESPECES,
    paliers: PALIERS,
    insufflations: BENEDICTIONS.map((b) => ({ id: insufflation(b.id), portee: b.portee, espece: b.espece })),
    succes: SUCCES.map((s) => ({
      id: s.id,
      famille: s.famille,
      visibilite: s.visibilite,
      assise: s.assise,
      declencheur: { ...s.declencheur, quoi: s.declencheur.quoi === 'eclosions' ? 'renaissances' : s.declencheur.quoi },
      effet:
        s.effet === null
          ? null
          : s.effet.genre === 'verbe'
            ? { genre: 'verbe', capacite: s.effet.capacite }
            : { genre: s.effet.genre, terme: terme(s.effet.terme), part: s.effet.part },
    })),
  }
}

/* ─── Écriture ─────────────────────────────────────────────────────────── */

function ecrire(nom: string, contenu: unknown) {
  writeFileSync(`${DOSSIER}${nom}.json`, JSON.stringify(contenu, null, 1) + '\n', 'utf8')
  console.log(`écrit ${nom}.json`)
}

mkdirSync(DOSSIER, { recursive: true })
ecrire('decimal', referencesDecimal())
ecrire('prng', referencesPrng())
ecrire('constantes', referencesConstantes())
ecrire('donnees', referencesDonnees())
const simulation = simuler(15, undefined, 7)
ecrire('parties', {
  scenarios: [scenarioSequence(), scenarioJoueur(), scenarioRenaissances()],
  simulation: {
    instant: instantane(simulation.etat),
    cyclesAcheves: simulation.cyclesAcheves,
    secondesActives: simulation.secondesActives,
    secondesEcoulees: simulation.secondesEcoulees,
  },
})
```

- [ ] **Step 2: Lancer le générateur**

Run: `npx tsx tests/parite/generer-references.ts`
Expected: cinq lignes `écrit ….json`, sans exception.

Run: `grep -c "null" Assets/IdlePond/Tests/Reference/decimal.json Assets/IdlePond/Tests/Reference/parties.json`
Expected: `0` pour les deux. Un `null` veut dire qu'un nombre non fini a échappé à l'encodage en chaîne ; le trouver et l'encoder, ne pas retirer le cas.

- [ ] **Step 3: Vérifier que le générateur est déterministe**

Run: `npx tsx tests/parite/generer-references.ts && git diff --stat -- Assets/IdlePond/Tests/Reference/`
Expected: aucune différence entre deux exécutions. Au premier passage, les fichiers ne sont pas encore suivis : les ajouter d'abord avec `git add`, puis relancer et vérifier que `git diff` est vide.

- [ ] **Step 4: Vérifier que la suite TypeScript n'a pas bougé**

Run: `npx vitest run 2>&1 | tail -5`
Expected: `196 passed`, `1 failed` : c'est l'état de départ. L'échec est `plancher-de-cadence › le silence après le dernier succès atteignable ne dépasse pas cinq minutes — PARQUÉ`, un `it.fails` qui passe désormais. Le générateur ne doit rien changer à ce résultat.

- [ ] **Step 5: Commit**

```bash
git add tests/parite/generer-references.ts Assets/IdlePond/Tests/Reference
git commit -m "test(parite): les références TypeScript — Decimal, PRNG, constantes, données, parties"
```

---

### Task 3: `Decimal`, le portage de `break_infinity.js`

**Files:**
- Create: `Assets/IdlePond/Noyau/Nombres/Decimal.cs`
- Create: `Assets/IdlePond/Tests/Outils/References.cs`
- Test: `Assets/IdlePond/Tests/DecimalTests.cs`

**Interfaces:**
- Consumes: `Reference/decimal.json` (tâche 2).
- Produces: `IdlePond.Noyau.Decimal`, un `readonly struct` :
  - `Decimal(double valeur)` ; `static Decimal Parse(string)` ; `static readonly Decimal Zero, Un` ;
  - `double Mantisse`, `double Exposant` ;
  - `Add(Decimal)`, `Add(double)`, `Sub(Decimal)`, `Sub(double)`, `Mul(Decimal)`, `Mul(double)`, `Div(Decimal)`, `Div(double)`, `Recip()`, `Neg()`, `Abs()`, `Pow(double)`, `Pow(Decimal)`, `Floor()`, `Round()`, `Ceil()`, `Exp()` → `Decimal` ;
  - `static Pow(double base, double x)`, `static Pow(Decimal base, double x)`, `static Pow10(double)`, `static Min(Decimal, Decimal)`, `static Max(Decimal, Decimal)` ;
  - `Eq`, `Lt`, `Gt`, `Lte`, `Gte` (surcharges `Decimal` et `double`) → `bool` ; `Cmp(Decimal)` → `int` ; `Max(Decimal)`, `Min(Decimal)` ;
  - `ToNumber()`, `Log10()`, `AbsLog10()`, `Log(double base)` → `double` ;
  - `ToString()` (forme de `break_infinity`, invariante) et `EnMantisseExposant()` (exacte : `"m e"`) ;
  - `static double JsRound(double)`.
- Produces: `References.Lire(string nom)` → `JObject`, qui lit `Assets/IdlePond/Tests/Reference/<nom>.json`.

- [ ] **Step 1: Écrire le lecteur de références**

`Assets/IdlePond/Tests/Outils/References.cs` :

```csharp
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Les fichiers produits par `tests/parite/generer-references.ts` pendant que
    /// le TypeScript était la vérité. Le répertoire courant d'un test EditMode est
    /// la racine du projet Unity.
    /// </summary>
    public static class References
    {
        public static JObject Lire(string nom)
        {
            var chemin = Path.GetFullPath(Path.Combine("Assets", "IdlePond", "Tests", "Reference", nom + ".json"));
            using var lecteur = new JsonTextReader(new StreamReader(chemin, Encoding.UTF8))
            {
                // Un double reste un double : jamais un System.Decimal arrondi.
                FloatParseHandling = FloatParseHandling.Double,
            };
            return JObject.Load(lecteur);
        }
    }
}
```

- [ ] **Step 2: Écrire les tests qui échouent**

`Assets/IdlePond/Tests/DecimalTests.cs` :

```csharp
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    public class DecimalTests
    {
        static Decimal[] Valeurs(JObject reference) =>
            reference["valeurs"].Select(v => Decimal.Parse((string)v)).ToArray();

        /// Exact pour tout ce qui ne passe que par des multiplications, des additions
        /// et la table de puissances de 10 ; à 1e-12 relatif pour ce qui appelle
        /// Math.Log10, Math.Pow ou Math.Exp, dont V8 et .NET peuvent différer au dernier bit.
        static readonly string[] OperationsTranscendantes = { "pow", "powStatique", "log10", "exp", "div", "recip" };

        static void Verifier(string contexte, JToken attendu, object obtenu, bool exact)
        {
            switch (obtenu)
            {
                case bool b:
                    Assert.That(b, Is.EqualTo((bool)attendu), contexte);
                    break;
                case int i:
                    Assert.That(i, Is.EqualTo((int)attendu), contexte);
                    break;
                case double x:
                    // « Infinity », « -Infinity », « NaN » arrivent en chaîne (voir le générateur).
                    var cibleNombre = attendu.Type == JTokenType.String
                        ? double.Parse((string)attendu, NumberStyles.Float, CultureInfo.InvariantCulture)
                        : (double)attendu;
                    ComparerNombres(contexte, cibleNombre, x, exact);
                    break;
                case Decimal dec:
                    var cible = Decimal.Parse((string)attendu);
                    if (exact && dec.Eq(cible)) return;
                    ComparerNombres(contexte, cible.ToNumber(), dec.ToNumber(), exact: false,
                        detail: $"{dec.EnMantisseExposant()} vs {cible.EnMantisseExposant()}");
                    break;
            }
        }

        static void ComparerNombres(string contexte, double attendu, double obtenu, bool exact, string detail = "")
        {
            if (attendu.Equals(obtenu)) return;
            Assert.That(exact, Is.False, $"{contexte} : {obtenu} au lieu de {attendu} {detail}");
            var echelle = Math.Max(Math.Max(Math.Abs(attendu), Math.Abs(obtenu)), 1e-300);
            Assert.That(Math.Abs(attendu - obtenu) / echelle, Is.LessThanOrEqualTo(1e-12), $"{contexte} {detail}");
        }

        [Test, Description("chaque opération rend ce que break_infinity.js rendait")]
        public void Chaque_operation_rend_ce_que_break_infinity_js_rendait()
        {
            var reference = References.Lire("decimal");
            var v = Valeurs(reference);
            foreach (JObject cas in reference["cas"])
            {
                var op = (string)cas["op"];
                var exact = !OperationsTranscendantes.Contains(op);
                var a = cas["a"] != null ? v[(int)cas["a"]] : Decimal.Zero;
                var b = cas["b"] != null ? v[(int)cas["b"]] : Decimal.Zero;
                var x = cas["x"] != null ? (double)cas["x"] : 0.0;
                object r = op switch
                {
                    "neg" => a.Neg(), "abs" => a.Abs(), "recip" => a.Recip(),
                    "floor" => a.Floor(), "round" => a.Round(), "ceil" => a.Ceil(),
                    "toNumber" => a.ToNumber(), "log10" => a.Log10(), "exp" => a.Exp(),
                    "add" => a.Add(b), "sub" => a.Sub(b), "mul" => a.Mul(b), "div" => a.Div(b),
                    "eq" => a.Eq(b), "lt" => a.Lt(b), "gt" => a.Gt(b), "lte" => a.Lte(b), "gte" => a.Gte(b),
                    "max" => a.Max(b), "min" => a.Min(b), "cmp" => a.Cmp(b),
                    "mulNombre" => a.Mul(x), "addNombre" => a.Add(x), "pow" => a.Pow(x),
                    "powStatique" => Decimal.Pow((double)cas["base"], x),
                    _ => throw new InvalidOperationException(op),
                };
                Verifier($"{op}({cas})", cas["r"], r, exact);
            }
        }

        [Test, Description("JsRound arrondit les demis vers plus l’infini, comme Math.round")]
        public void JsRound_arrondit_les_demis_vers_plus_l_infini()
        {
            Assert.That(Decimal.JsRound(2.5), Is.EqualTo(3.0));
            Assert.That(Decimal.JsRound(-2.5), Is.EqualTo(-2.0));
            Assert.That(Decimal.JsRound(0.49999999999999994), Is.EqualTo(0.0));
            Assert.That(Decimal.JsRound(1.5), Is.EqualTo(2.0));
            Assert.That(double.IsNaN(Decimal.JsRound(double.NaN)), Is.True);
        }

        [Test, Description("relit les chaînes écrites par JavaScript")]
        public void Relit_les_chaines_ecrites_par_JavaScript()
        {
            Assert.That(Decimal.Parse("1.5e25").Mantisse, Is.EqualTo(1.5));
            Assert.That(Decimal.Parse("1.5e25").Exposant, Is.EqualTo(25.0));
            Assert.That(Decimal.Parse("1.5e+25").Exposant, Is.EqualTo(25.0));
            Assert.That(Decimal.Parse("0e0").Eq(Decimal.Zero), Is.True);
            Assert.That(Decimal.Parse("9.999999999999998e2").Mantisse, Is.EqualTo(9.999999999999998));
            Assert.That(Decimal.Parse("123.45").ToNumber(), Is.EqualTo(123.45));
            Assert.That(Decimal.Parse("1E+21").Exposant, Is.EqualTo(21.0));
            Assert.That(Decimal.Parse("Infinity").Exposant, Is.EqualTo(9e15));
            Assert.That(double.IsNaN(Decimal.Parse("NaN").Mantisse), Is.True);
            Assert.Throws<FormatException>(() => Decimal.Parse("abc"));
        }

        [Test, Description("aller-retour sous la culture fr-FR")]
        public void Aller_retour_sous_la_culture_fr_FR()
        {
            var avant = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                foreach (var texte in new[] { "2.4", "1e12", "123456789.123", "-3.5e-400", "1e500", "0.1" })
                {
                    var x = Decimal.Parse(texte);
                    Assert.That(Decimal.Parse(x.EnMantisseExposant()).Eq(x), Is.True, texte);
                    Assert.That(x.ToString(), Does.Not.Contain(","), texte);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = avant;
            }
        }

        [Test, Description("un Decimal par défaut vaut zéro, comme new Decimal()")]
        public void Un_Decimal_par_defaut_vaut_zero()
        {
            Assert.That(default(Decimal).Eq(Decimal.Zero), Is.True);
            Assert.That(default(Decimal).ToString(), Is.EqualTo("0"));
        }
    }
}
```

- [ ] **Step 3: Lancer les tests pour les voir échouer**

Run: `outils/unity.sh tests EditMode DecimalTests`
Expected: échec de compilation, `error CS0234` ou `CS0246` sur `IdlePond.Noyau.Decimal`.

- [ ] **Step 4: Écrire `Decimal.cs`**

`Assets/IdlePond/Noyau/Nombres/Decimal.cs`. C'est une traduction de `node_modules/break_infinity.js/dist/break_infinity.js` (v2.2.0), lignes 48-73 et 425-1380 : relire la source en cas de doute, pas la mémoire.

```csharp
using System;
using System.Globalization;

namespace IdlePond.Noyau
{
    /// <summary>
    /// Un grand nombre : mantisse × 10^exposant. Portage fidèle de
    /// break_infinity.js 2.2.0, limité aux opérations que le jeu emploie.
    ///
    /// Fidèle veut dire jusque dans ses bizarreries : `Add` arrondit à 1e14 près
    /// avec le Math.round de JavaScript, `ToNumber` recolle les quasi-entiers,
    /// `Mul(double)` ne suit pas le même chemin que `Mul(Decimal)`. Le TypeScript a
    /// été calibré sur ces nombres-là ; la parité (PariteTests) ne tient que s'ils
    /// sont reproduits, pas améliorés.
    ///
    /// Ce n'est pas System.Decimal. Dans l'espace de noms IdlePond.Noyau, `Decimal`
    /// désigne ce type-ci ; ailleurs, écrire `using Decimal = IdlePond.Noyau.Decimal;`.
    /// </summary>
    public readonly struct Decimal : IEquatable<Decimal>
    {
        const int MAX_SIGNIFICANT_DIGITS = 17;
        const double EXP_LIMIT = 9e15;
        const int NUMBER_EXP_MAX = 308;
        const int NUMBER_EXP_MIN = -324;
        const double ROUND_TOLERANCE = 1e-10;
        const double LN10 = 2.302585092994046;

        /// La table de break_infinity : Math.Pow(10, n) est inexact pour les grands |n|.
        static readonly double[] PuissancesDe10 = ConstruirePuissancesDe10();

        static double[] ConstruirePuissancesDe10()
        {
            var table = new double[NUMBER_EXP_MAX - NUMBER_EXP_MIN];
            for (var i = NUMBER_EXP_MIN + 1; i <= NUMBER_EXP_MAX; i++)
                table[i - (NUMBER_EXP_MIN + 1)] = double.Parse("1e" + i, CultureInfo.InvariantCulture);
            return table;
        }

        static double PuissanceDe10(double puissance) => PuissancesDe10[(int)puissance + 323];

        public static readonly Decimal Zero = default;
        public static readonly Decimal Un = new Decimal(1.0);

        public readonly double Mantisse;
        public readonly double Exposant;

        Decimal(double mantisse, double exposant, bool brut)
        {
            Mantisse = mantisse;
            Exposant = exposant;
        }

        /* ─── Construction ─────────────────────────────────────────────────── */

        /// `new Decimal(number)` — fromNumber.
        public Decimal(double valeur)
        {
            if (double.IsNaN(valeur)) { Mantisse = double.NaN; Exposant = double.NaN; return; }
            if (double.IsPositiveInfinity(valeur)) { Mantisse = 1; Exposant = EXP_LIMIT; return; }
            if (double.IsNegativeInfinity(valeur)) { Mantisse = -1; Exposant = EXP_LIMIT; return; }
            if (valeur == 0) { Mantisse = 0; Exposant = 0; return; }
            var e = Math.Floor(Math.Log10(Math.Abs(valeur)));
            // 5e-324 et -5e-324 à part, comme la source.
            var m = e == NUMBER_EXP_MIN ? valeur * 10 / 1e-323 : valeur / PuissanceDe10(e);
            var n = Normaliser(m, e);
            Mantisse = n.Mantisse;
            Exposant = n.Exposant;
        }

        /// normalize(). Garde en plus contre une mantisse non finie, que la source
        /// transforme en NaN par un index hors table ; ici l'index lèverait.
        static Decimal Normaliser(double m, double e)
        {
            if (m >= 1 && m < 10) return new Decimal(m, e, true);
            if (m == 0) return new Decimal(0, 0, true);
            if (double.IsNaN(m) || double.IsInfinity(m)) return new Decimal(double.NaN, double.NaN, true);
            var temp = Math.Floor(Math.Log10(Math.Abs(m)));
            m = temp == NUMBER_EXP_MIN ? m * 10 / 1e-323 : m / PuissanceDe10(temp);
            return new Decimal(m, e + temp, true);
        }

        /// fromMantissaExponent — ME(). La source, sur une entrée non finie, rend
        /// `this` inchangé : un `new Decimal()` qui vaut zéro. Reproduit tel quel.
        static Decimal ME(double mantisse, double exposant)
        {
            if (double.IsNaN(mantisse) || double.IsInfinity(mantisse) || double.IsNaN(exposant) || double.IsInfinity(exposant))
                return Zero;
            return Normaliser(mantisse, exposant);
        }

        /// fromMantissaExponent_noNormalize — ME_NN().
        static Decimal MENN(double mantisse, double exposant) => new Decimal(mantisse, exposant, true);

        /// fromString. Accepte en plus « E » majuscule, qu'écrit le .NET.
        public static Decimal Parse(string texte)
        {
            if (texte == null) throw new FormatException("Decimal nul");
            var s = texte.Trim();
            var indexE = s.IndexOfAny(new[] { 'e', 'E' });
            if (indexE >= 0 && !s.EndsWith("Infinity", StringComparison.Ordinal))
            {
                var m = double.Parse(s.Substring(0, indexE), NumberStyles.Float, CultureInfo.InvariantCulture);
                var e = double.Parse(s.Substring(indexE + 1), NumberStyles.Float, CultureInfo.InvariantCulture);
                return Normaliser(m, e);
            }
            if (s == "NaN") return new Decimal(double.NaN, double.NaN, true);
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur))
                throw new FormatException("[DecimalError] Invalid argument: " + texte);
            return new Decimal(valeur);
        }

        /* ─── Arrondis de JavaScript ───────────────────────────────────────── */

        /// Math.round : les demis vont vers +∞. Math.Round de .NET les enverrait au pair.
        public static double JsRound(double x)
        {
            var plancher = Math.Floor(x);
            return x - plancher >= 0.5 ? plancher + 1 : plancher;
        }

        static bool EstEntierSur(double x) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && Math.Floor(x) == x && Math.Abs(x) <= 9007199254740991;

        static bool EstEntier(double x) => !double.IsNaN(x) && !double.IsInfinity(x) && Math.Floor(x) == x;

        /* ─── Conversions ──────────────────────────────────────────────────── */

        public double ToNumber()
        {
            if (double.IsNaN(Exposant) || double.IsInfinity(Exposant)) return double.NaN;
            if (Exposant > NUMBER_EXP_MAX) return Mantisse > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            if (Exposant < NUMBER_EXP_MIN) return 0;
            if (Exposant == NUMBER_EXP_MIN) return Mantisse > 0 ? 5e-324 : -5e-324;
            var resultat = Mantisse * PuissanceDe10(Exposant);
            if (double.IsInfinity(resultat) || double.IsNaN(resultat) || Exposant < 0) return resultat;
            var arrondi = JsRound(resultat);
            return Math.Abs(arrondi - resultat) < ROUND_TOLERANCE ? arrondi : resultat;
        }

        /// La forme de break_infinity, en culture invariante. Pour l'affichage et le
        /// débogage ; pour un aller-retour exact, `EnMantisseExposant`.
        public override string ToString()
        {
            if (double.IsNaN(Mantisse) || double.IsNaN(Exposant)) return "NaN";
            if (Exposant >= EXP_LIMIT) return Mantisse > 0 ? "Infinity" : "-Infinity";
            if (Exposant <= -EXP_LIMIT || Mantisse == 0) return "0";
            if (Exposant < 21 && Exposant > -7) return FormaterDouble(ToNumber());
            return FormaterDouble(Mantisse) + "e" + (Exposant >= 0 ? "+" : "") + FormaterDouble(Exposant);
        }

        /// Exacte : « mantisse e exposant », relue telle quelle par `Parse`.
        public string EnMantisseExposant() => FormaterDouble(Mantisse) + "e" + FormaterDouble(Exposant);

        /// Le « R » du Mono d'Unity ne garantit pas l'aller-retour ; G17 si besoin.
        static string FormaterDouble(double x)
        {
            var court = x.ToString("R", CultureInfo.InvariantCulture);
            return double.Parse(court, NumberStyles.Float, CultureInfo.InvariantCulture).Equals(x)
                ? court
                : x.ToString("G17", CultureInfo.InvariantCulture);
        }

        /* ─── Signe ─────────────────────────────────────────────────────────── */

        public Decimal Abs() => MENN(Math.Abs(Mantisse), Exposant);
        public Decimal Neg() => MENN(-Mantisse, Exposant);
        int Signe() => Math.Sign(Mantisse);

        /* ─── Arrondis ──────────────────────────────────────────────────────── */

        public Decimal Round()
        {
            if (Exposant < -1) return Zero;
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(JsRound(ToNumber()));
            return this;
        }

        public Decimal Floor()
        {
            if (Exposant < -1) return Signe() >= 0 ? Zero : new Decimal(-1.0);
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(Math.Floor(ToNumber()));
            return this;
        }

        public Decimal Ceil()
        {
            if (Exposant < -1) return Signe() > 0 ? Un : Zero;
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(Math.Ceiling(ToNumber()));
            return this;
        }

        /* ─── Arithmétique ─────────────────────────────────────────────────── */

        public Decimal Add(Decimal valeur)
        {
            if (Mantisse == 0) return valeur;
            if (valeur.Mantisse == 0) return this;
            Decimal grand, petit;
            if (Exposant >= valeur.Exposant) { grand = this; petit = valeur; }
            else { grand = valeur; petit = this; }
            if (grand.Exposant - petit.Exposant > MAX_SIGNIFICANT_DIGITS) return grand;
            // « 299 + 18 » : additionner des mantisses mises à l'échelle perd des entiers.
            var mantisse = JsRound(1e14 * grand.Mantisse + 1e14 * petit.Mantisse * PuissanceDe10(petit.Exposant - grand.Exposant));
            return ME(mantisse, grand.Exposant - 14);
        }

        public Decimal Add(double valeur) => Add(new Decimal(valeur));
        public Decimal Sub(Decimal valeur) => Add(valeur.Neg());
        public Decimal Sub(double valeur) => Add(new Decimal(valeur).Neg());

        /// Le chemin « number » de la source : la mantisse absorbe le facteur.
        public Decimal Mul(double valeur)
        {
            if (valeur < 1e307 && valeur > -1e307) return ME(Mantisse * valeur, Exposant);
            return ME(Mantisse * 1e-307 * valeur, Exposant + 307);
        }

        public Decimal Mul(Decimal valeur) => ME(Mantisse * valeur.Mantisse, Exposant + valeur.Exposant);
        public Decimal Recip() => ME(1 / Mantisse, -Exposant);
        public Decimal Div(Decimal valeur) => Mul(valeur.Recip());
        public Decimal Div(double valeur) => Mul(new Decimal(valeur).Recip());

        /* ─── Comparaisons ─────────────────────────────────────────────────── */

        public int Cmp(Decimal v)
        {
            if (Mantisse == 0)
            {
                if (v.Mantisse == 0) return 0;
                if (v.Mantisse < 0) return 1;
                if (v.Mantisse > 0) return -1;
            }
            if (v.Mantisse == 0)
            {
                if (Mantisse < 0) return -1;
                if (Mantisse > 0) return 1;
            }
            if (Mantisse > 0)
            {
                if (v.Mantisse < 0) return 1;
                if (Exposant > v.Exposant) return 1;
                if (Exposant < v.Exposant) return -1;
                if (Mantisse > v.Mantisse) return 1;
                if (Mantisse < v.Mantisse) return -1;
                return 0;
            }
            if (Mantisse < 0)
            {
                if (v.Mantisse > 0) return -1;
                if (Exposant > v.Exposant) return -1;
                if (Exposant < v.Exposant) return 1;
                if (Mantisse > v.Mantisse) return 1;
                if (Mantisse < v.Mantisse) return -1;
                return 0;
            }
            throw new InvalidOperationException("Unreachable code");
        }

        public bool Eq(Decimal v) => Exposant == v.Exposant && Mantisse == v.Mantisse;
        public bool Eq(double v) => Eq(new Decimal(v));

        public bool Lt(Decimal v)
        {
            if (Mantisse == 0) return v.Mantisse > 0;
            if (v.Mantisse == 0) return Mantisse <= 0;
            if (Exposant == v.Exposant) return Mantisse < v.Mantisse;
            if (Mantisse > 0) return v.Mantisse > 0 && Exposant < v.Exposant;
            return v.Mantisse > 0 || Exposant > v.Exposant;
        }

        public bool Gt(Decimal v)
        {
            if (Mantisse == 0) return v.Mantisse < 0;
            if (v.Mantisse == 0) return Mantisse > 0;
            if (Exposant == v.Exposant) return Mantisse > v.Mantisse;
            if (Mantisse > 0) return v.Mantisse < 0 || Exposant > v.Exposant;
            return v.Mantisse < 0 && Exposant < v.Exposant;
        }

        public bool Lte(Decimal v) => !Gt(v);
        public bool Gte(Decimal v) => !Lt(v);
        public bool Lt(double v) => Lt(new Decimal(v));
        public bool Gt(double v) => Gt(new Decimal(v));
        public bool Lte(double v) => Lte(new Decimal(v));
        public bool Gte(double v) => Gte(new Decimal(v));

        public Decimal Max(Decimal v) => Lt(v) ? v : this;
        public Decimal Min(Decimal v) => Gt(v) ? v : this;
        public static Decimal Max(Decimal a, Decimal b) => a.Max(b);
        public static Decimal Min(Decimal a, Decimal b) => a.Min(b);

        /* ─── Logarithmes, puissances ──────────────────────────────────────── */

        public double Log10() => Exposant + Math.Log10(Mantisse);
        public double AbsLog10() => Exposant + Math.Log10(Math.Abs(Mantisse));
        public double Log(double @base) => LN10 / Math.Log(@base) * Log10();

        public static Decimal Pow10(double valeur)
        {
            if (EstEntier(valeur)) return MENN(1, valeur);
            return ME(Math.Pow(10, valeur % 1), Math.Truncate(valeur));
        }

        public Decimal Pow(Decimal valeur) => Pow(valeur.ToNumber());

        public Decimal Pow(double valeur)
        {
            var temp = Exposant * valeur;
            double nouvelleMantisse;
            if (EstEntierSur(temp))
            {
                nouvelleMantisse = Math.Pow(Mantisse, valeur);
                if (!double.IsInfinity(nouvelleMantisse) && !double.IsNaN(nouvelleMantisse) && nouvelleMantisse != 0)
                    return ME(nouvelleMantisse, temp);
            }
            var nouvelExposant = Math.Truncate(temp);
            var residu = temp - nouvelExposant;
            nouvelleMantisse = Math.Pow(10, valeur * Math.Log10(Mantisse) + residu);
            if (!double.IsInfinity(nouvelleMantisse) && !double.IsNaN(nouvelleMantisse) && nouvelleMantisse != 0)
                return ME(nouvelleMantisse, nouvelExposant);
            var resultat = Pow10(valeur * AbsLog10());
            if (Signe() == -1)
            {
                if (Math.Abs(valeur % 2) == 1) return resultat.Neg();
                if (Math.Abs(valeur % 2) == 0) return resultat;
                return new Decimal(double.NaN);
            }
            return resultat;
        }

        /// Decimal.pow(value, other) : 10^entier par la voie rapide, sinon D(value).pow(other).
        public static Decimal Pow(double @base, double x)
        {
            if (@base == 10 && EstEntier(x)) return MENN(1, x);
            return new Decimal(@base).Pow(x);
        }

        public static Decimal Pow(Decimal @base, double x) => @base.Pow(x);

        public Decimal Exp()
        {
            var x = ToNumber();
            if (-706 < x && x < 709) return new Decimal(Math.Exp(x));
            return Pow(Math.E, x);
        }

        /* ─── Égalité ──────────────────────────────────────────────────────── */

        public bool Equals(Decimal autre) => Eq(autre);
        public override bool Equals(object obj) => obj is Decimal autre && Eq(autre);
        public override int GetHashCode() => Mantisse.GetHashCode() ^ (Exposant.GetHashCode() * 397);
    }
}
```

- [ ] **Step 5: Lancer les tests**

Run: `outils/unity.sh tests EditMode DecimalTests`
Expected: `total 5 · réussis 5 · échoués 0`. Si un cas exact échoue, afficher les deux `EnMantisseExposant()` et comparer ligne à ligne avec la source JS. **Ne jamais relâcher la tolérance pour faire passer.**

- [ ] **Step 6: Commit**

```bash
git add Assets/IdlePond/Noyau/Nombres Assets/IdlePond/Tests/Outils Assets/IdlePond/Tests/DecimalTests.cs*
git commit -m "feat(noyau): Decimal, portage fidèle de break_infinity.js 2.2.0"
```

---

### Task 4: PRNG, types et constantes

**Files:**
- Create: `Assets/IdlePond/Noyau/Nombres/Prng.cs`, `Assets/IdlePond/Noyau/Types.cs`, `Assets/IdlePond/Noyau/Termes.cs`, `Assets/IdlePond/Noyau/Constantes.cs`
- Test: `Assets/IdlePond/Tests/PrngTests.cs`, `Assets/IdlePond/Tests/ConstantesTests.cs`

**Interfaces:**
- Consumes: `Decimal` (tâche 3), `prng.json`, `constantes.json`.
- Produces: `Prng.Tirer(EtatPrng)` → `(double Valeur, EtatPrng Suivant)`. Tous les types de `Types.cs` ci-dessous. `Termes.DE_PRODUCTION`, `DE_COUT`, `DE_CONFORT`, `Termes.EstDeProduction(t)`, `EstDeCout(t)`, `EstDeConfort(t)`, `Termes.Identifiant(t)` (le nom `snake_case` du lexique, par exemple `"cout_insufflation"`). `Constantes.*` : chaque constante de `constantes.ts` sous son nom renommé, plus `Constantes.MultiplicateurDePalier()`, `Constantes.DensiteExposant()`, `Constantes.SEUILS_DE_JALON` (`IReadOnlyList<SeuilDeJalon>`), `Constantes.COUTS_DE_NOEUD` (`IReadOnlyList<int>`) et `Constantes.REGLAGE_CANONIQUE`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/IdlePond/Tests/PrngTests.cs` :

```csharp
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class PrngTests
    {
        [Test, Description("le PRNG rend, bit pour bit, les suites du TypeScript")]
        public void Le_PRNG_rend_bit_pour_bit_les_suites_du_TypeScript()
        {
            foreach (var suite in References.Lire("prng")["suites"])
            {
                var prng = new EtatPrng((uint)(long)suite["graine"]);
                foreach (var tirage in suite["tirages"])
                {
                    var (valeur, suivant) = Prng.Tirer(prng);
                    Assert.That(valeur, Is.EqualTo((double)tirage[0]), $"graine {suite["graine"]}");
                    Assert.That(suivant.Graine, Is.EqualTo((uint)(long)tirage[1]));
                    prng = suivant;
                }
            }
        }
    }
}
```

`Assets/IdlePond/Tests/ConstantesTests.cs` :

```csharp
using System;
using System.Reflection;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ConstantesTests
    {
        static double Lire(string nom)
        {
            var champ = typeof(Constantes).GetField(nom, BindingFlags.Public | BindingFlags.Static);
            Assert.That(champ, Is.Not.Null, $"Constantes.{nom} manque");
            return Convert.ToDouble(champ.GetValue(null));
        }

        /// 1e-14 relatif : les constantes dérivées passent par Math.Pow, qui peut
        /// différer d'un ulp entre V8 et .NET. Une graine saisie doit, elle, être exacte.
        static void Proche(string nom, double attendu, double obtenu)
        {
            if (attendu.Equals(obtenu)) return;
            var ecart = Math.Abs(attendu - obtenu) / Math.Max(Math.Abs(attendu), 1e-300);
            Assert.That(ecart, Is.LessThanOrEqualTo(1e-14), $"{nom} : {obtenu} au lieu de {attendu}");
        }

        [Test, Description("chaque constante vaut ce qu’elle valait en TypeScript")]
        public void Chaque_constante_vaut_ce_qu_elle_valait_en_TypeScript()
        {
            var reference = References.Lire("constantes");
            foreach (var (nom, valeur) in (JObject)reference["nombres"])
                Proche(nom, (double)valeur, Lire(nom));
            Proche("multiplicateurDePalier", (double)reference["multiplicateurDePalier"], Constantes.MultiplicateurDePalier());
            Proche("densiteExposant", (double)reference["densiteExposant"], Constantes.DensiteExposant());
            var seuils = (JArray)reference["SEUILS_DE_JALON"];
            Assert.That(Constantes.SEUILS_DE_JALON.Count, Is.EqualTo(seuils.Count));
            for (var i = 0; i < seuils.Count; i++)
            {
                Assert.That(Constantes.SEUILS_DE_JALON[i].Seuil, Is.EqualTo((int)seuils[i]["seuil"]));
                Assert.That(Constantes.SEUILS_DE_JALON[i].MultiplicateurCumule, Is.EqualTo((double)seuils[i]["multiplicateurCumule"]));
            }
            Assert.That(Constantes.COUTS_DE_NOEUD, Is.EqualTo(reference["COUTS_DE_NOEUD"].ToObject<int[]>()));
        }

        [Test, Description("la sauvegarde Unity repart à la version 1")]
        public void La_sauvegarde_Unity_repart_a_la_version_1()
        {
            Assert.That(Constantes.VERSION_SAVE, Is.EqualTo(1));
        }

        [Test, Description("chaque terme a un identifiant, et un seul registre")]
        public void Chaque_terme_a_un_identifiant_et_un_seul_registre()
        {
            foreach (TermeDeFormule terme in Enum.GetValues(typeof(TermeDeFormule)))
            {
                var registres = (Termes.EstDeProduction(terme) ? 1 : 0) + (Termes.EstDeCout(terme) ? 1 : 0) + (Termes.EstDeConfort(terme) ? 1 : 0);
                Assert.That(registres, Is.EqualTo(1), terme.ToString());
                Assert.That(Termes.Identifiant(terme), Does.Match("^[a-z_]+$"));
            }
            Assert.That(Termes.Identifiant(TermeDeFormule.CoutInsufflation), Is.EqualTo("cout_insufflation"));
        }
    }
}
```

- [ ] **Step 2: Lancer les tests pour les voir échouer**

Run: `outils/unity.sh tests EditMode "PrngTests|ConstantesTests"`
Expected: échec de compilation sur `EtatPrng`, `Prng`, `Constantes`, `Termes`.

- [ ] **Step 3: Écrire `Prng.cs`**

```csharp
namespace IdlePond.Noyau
{
    /// <summary>
    /// mulberry32, bit pour bit celui de `tirer` dans noyau.ts. `Math.imul` y rend
    /// les 32 bits bas du produit : c'est exactement la multiplication `uint`
    /// non vérifiée de C#. Tirage pur : rend la valeur ET l'état suivant.
    ///
    /// Jamais tiré sur le chemin continu (§5.2) : un tirage par tick ferait
    /// diverger 480 pas de 60 s d'un pas de 8 h.
    /// </summary>
    public static class Prng
    {
        public static (double Valeur, EtatPrng Suivant) Tirer(EtatPrng prng)
        {
            unchecked
            {
                var graine = prng.Graine + 0x6d2b79f5u;
                var x = graine;
                x = (x ^ (x >> 15)) * (x | 1u);
                x ^= x + (x ^ (x >> 7)) * (x | 61u);
                return ((x ^ (x >> 14)) / 4294967296.0, new EtatPrng(graine));
            }
        }
    }
}
```

- [ ] **Step 4: Écrire `Types.cs`**

C'est la traduction de `src/noyau/types.ts`. Les commentaires de ce fichier (§7.5, §14.5, `Reglage` non persisté…) sont portés sur les types correspondants.

```csharp
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    /* ─── Termes de formule (§7.5 règle 3) ─────────────────────────────────
     * Un seul enum ; la partition production / coût / confort vit dans Termes.cs
     * et un test la vérifie, là où TypeScript la portait par trois types. */
    public enum TermeDeFormule
    {
        // production
        TauxBase, Niveau, MultiplicateurJalon, MultiplicateurDrapeau, MultiplicateurProfondeur,
        MultiplicateurDensite, DebitHeros, MultiplicateurHeros, MultiplicateurInsufflation, InsufflationGlobale,
        // coût
        CoutCreuser, CoutNiveau, CoutDeblocage, CoutCroissance, CoutInsufflation, CoutTemple, CoutPortail, CoutReouverture,
        // confort
        CapHorsLigne, DensiteConservee, ContenanceDeDepart, NiveauDeDepart, ChargeAllieeParReponse,
    }

    public enum CapaciteId
    {
        FileDeDescente, CreusementAuto, AchatAuto, AchatAutoMax, DeblocageAuto, LectureDebits,
        AutomatismesHorsLigne, RapportDeRetour, NavigationDirecte, LectureEau, RetourRapide,
    }

    public enum SourceDeCapacite { Technique, Succes }

    public enum BrancheTechnique { Creusement, Amelioration, Recrutement, Entretien, Construction, Renaissance }

    public enum RegimeCompteur { NonBorne, Borne }

    public enum NatureDEffet { Chiffre, Verbe }

    /// Un nœud chiffre cible un terme de coût ou de confort ; un nœud verbe ouvre une capacité.
    public sealed record EffetDeNoeud(NatureDEffet Nature, TermeDeFormule? Terme, double Facteur, CapaciteId? Capacite)
    {
        public static EffetDeNoeud Chiffre(TermeDeFormule terme, double facteur) => new(NatureDEffet.Chiffre, terme, facteur, null);
        public static EffetDeNoeud Verbe(CapaciteId capacite) => new(NatureDEffet.Verbe, null, 0, capacite);
    }

    public sealed record NoeudTechnique(string Id, BrancheTechnique Branche, int Rang, int Cout, EffetDeNoeud Effet);

    public enum PalierDeVoix { Pente, Signes, Directives, Dialogue }

    /* ─── Succès (§8) ─────────────────────────────────────────────────────── */

    public enum FamilleDeSucces { Franchissement, Seuil, Acte }
    public enum VisibiliteDeSucces { Ouvert, Ferme, Secret }

    public enum QuoiDeclencheur
    {
        Renaissances, PaliersOuverts, ProfondeurMax, EspecesDebloquees, NiveauDEspece,
        NiveauxCumules, ProductionParSeconde, Souffle, DensiteDePalier, PalierAuComplet,
    }

    /// Toujours un SEUIL relu sur l'état de fin de tick, jamais un événement
    /// consommé au vol (voir types.ts). `Espece` et `Palier` ne valent que pour
    /// les déclencheurs qui les nomment.
    public sealed record DeclencheurDeSucces(QuoiDeclencheur Quoi, double Seuil = 0, string Espece = null, int Palier = -1);

    public enum GenreDEffetDeSucces { ReductionCout, Plafond, Verbe }

    /// Amendement v1.1 §2.D : JAMAIS de production. Aucun genre ne la porte.
    public sealed record EffetDeSucces(GenreDEffetDeSucces Genre, TermeDeFormule? Terme, double Part, CapaciteId? Capacite)
    {
        public static EffetDeSucces ReductionCout(TermeDeFormule terme, double part) => new(GenreDEffetDeSucces.ReductionCout, terme, part, null);
        public static EffetDeSucces Plafond(TermeDeFormule terme, double part) => new(GenreDEffetDeSucces.Plafond, terme, part, null);
        public static EffetDeSucces Verbe(CapaciteId capacite) => new(GenreDEffetDeSucces.Verbe, null, 0, capacite);
    }

    /// §14.5 : `Registre` FIGE la langue de l'entrée, jamais réécrite.
    public sealed record EntreeDeSucces(int ObtenuAuCycle, PalierDeVoix Registre);

    public sealed record Succes(string Id, FamilleDeSucces Famille, VisibiliteDeSucces Visibilite, string Assise,
        DeclencheurDeSucces Declencheur, EffetDeSucces Effet);

    /* ─── Contenu structurel ──────────────────────────────────────────────── */

    public sealed record Assise(string Id, int Rang, string TypeMana, int IndexPremierPalier, int NombreDePaliers);
    public sealed record Espece(string Id, string Assise, int Rang, int Palier);
    public sealed record Palier(int Index, string Assise, string Espece);

    public enum PorteeDInsufflation { Ciblee, Globale }

    /// Noyau v1.0 §4.2 : la ciblée MULTIPLIE une espèce, la globale ADDITIONNE
    /// au débit de base de toutes. `Espece` est null pour la globale.
    public sealed record Insufflation(string Id, PorteeDInsufflation Portee, string Espece);

    /* ─── État ────────────────────────────────────────────────────────────── */

    public sealed record EtatPrng(uint Graine);

    public sealed record EtatEspece(bool Debloquee, int Niveau);

    public sealed record EtatCycle(
        Decimal ManaCourant,
        int PaliersOuverts,
        IReadOnlyDictionary<string, EtatEspece> Especes,
        Decimal ProductionPicParSeconde,
        double DureeSecondes,
        double AcquisDeSejour,
        int NiveauDuHeros);

    public sealed record EtatPermanent(
        IReadOnlyList<double> Densites,
        Decimal Souffle,
        Decimal ContenanceMana,
        IReadOnlyList<string> Couches,
        int ProfondeurMaxAtteinte,
        IReadOnlyDictionary<BrancheTechnique, double> CompteursTechnique,
        IReadOnlyList<string> NoeudsTechnique,
        IReadOnlyDictionary<string, EntreeDeSucces> Succes,
        int NombreDeRenaissances,
        IReadOnlyList<string> EspecesAyantAtteintCent,
        Decimal ManaAmbiant,
        double HeuresHorsLigneCreditees,
        IReadOnlyDictionary<string, int> Insufflations);

    public sealed record MesureDeCycle(int Index, double DureeEcouleeSecondes, int PaliersOuverts,
        Decimal ProductionPicParSeconde, Decimal SouffleGagne);

    public sealed record EtatTelemetrie(IReadOnlyList<MesureDeCycle> Cycles, double SecondesDepuisDernierSucces,
        IReadOnlyList<double> IntervallesEntreSucces);

    /// Les boutons de la courbe. Jamais persistés (R41) : une propriété de la
    /// VERSION du jeu, pas de la partie.
    public sealed record Reglage(double CroissanceDuSejourParPalier);

    public sealed record EtatJeu(
        int VersionSave,
        EtatPrng Prng,
        double TempsJeuSecondes,
        int LimiteDeContenu,
        Reglage Reglage,
        EtatCycle Cycle,
        EtatPermanent Permanent,
        EtatTelemetrie Telemetrie);

    /* ─── Détail de captation (§8.2) ──────────────────────────────────────── */

    public enum QuoiSource { Niveau, Palier, DrapeauxPermanents, Profondeur, Densite, Heros, Insufflation }

    /// Une structure, jamais une phrase : le noyau ne fabrique aucun texte d'écran.
    /// `Valeur` est le niveau, le palier, le nombre d'espèces, les paliers ouverts,
    /// la densité ou le rang, selon `Quoi`.
    public sealed record SourceDeTerme(QuoiSource Quoi, double Valeur);

    public sealed record LigneDeCaptation(TermeDeFormule Terme, double Valeur, SourceDeTerme Source);

    public sealed record SeuilDeJalon(int Seuil, double MultiplicateurCumule);
}
```

- [ ] **Step 5: Écrire `Termes.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau
{
    /// <summary>
    /// La partition des termes (§7.5 règle 3) : elle rend vérifiable par un test,
    /// plutôt que par une relecture, qu'aucun système gratuit ne monte la production.
    /// </summary>
    public static class Termes
    {
        public static readonly IReadOnlyList<TermeDeFormule> DE_PRODUCTION = new[]
        {
            TermeDeFormule.TauxBase, TermeDeFormule.Niveau, TermeDeFormule.MultiplicateurJalon,
            TermeDeFormule.MultiplicateurDrapeau, TermeDeFormule.MultiplicateurProfondeur,
            TermeDeFormule.MultiplicateurDensite, TermeDeFormule.DebitHeros, TermeDeFormule.MultiplicateurHeros,
            TermeDeFormule.MultiplicateurInsufflation, TermeDeFormule.InsufflationGlobale,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DE_COUT = new[]
        {
            TermeDeFormule.CoutCreuser, TermeDeFormule.CoutNiveau, TermeDeFormule.CoutDeblocage,
            TermeDeFormule.CoutCroissance, TermeDeFormule.CoutInsufflation, TermeDeFormule.CoutTemple,
            TermeDeFormule.CoutPortail, TermeDeFormule.CoutReouverture,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DE_CONFORT = new[]
        {
            TermeDeFormule.CapHorsLigne, TermeDeFormule.DensiteConservee, TermeDeFormule.ContenanceDeDepart,
            TermeDeFormule.NiveauDeDepart, TermeDeFormule.ChargeAllieeParReponse,
        };

        public static bool EstDeProduction(TermeDeFormule t) => DE_PRODUCTION.Contains(t);
        public static bool EstDeCout(TermeDeFormule t) => DE_COUT.Contains(t);
        public static bool EstDeConfort(TermeDeFormule t) => DE_CONFORT.Contains(t);

        /// Le nom du lexique, en snake_case : celui des références et du détail de captation.
        public static string Identifiant(TermeDeFormule t)
        {
            var nom = t.ToString();
            var sortie = new System.Text.StringBuilder();
            for (var i = 0; i < nom.Length; i++)
            {
                if (i > 0 && char.IsUpper(nom[i])) sortie.Append('_');
                sortie.Append(char.ToLowerInvariant(nom[i]));
            }
            return sortie.ToString();
        }
    }
}
```

Vérification mentale : `MultiplicateurInsufflation` → `multiplicateur_insufflation` ; `CapHorsLigne` → `cap_hors_ligne` ; `ChargeAllieeParReponse` → `charge_alliee_par_reponse`. Ce sont les noms de `types.ts`, renommés.

- [ ] **Step 6: Écrire `Constantes.cs`**

On traduit `src/noyau/constantes.ts` **dans son ordre**, avec **tous** ses commentaires. Règles :
- une valeur littérale donne `public const double NOM = 2.4;` (ou `const int` pour les nombres entiers qui servent de compte ou d'index : `SEUIL_DU_DRAPEAU_PERMANENT`, `ESPECE_TOUS_LES_N_PALIERS`, `NOMBRE_DE_PALIERS`, `NOMBRE_D_ASSISES`, `NOMBRE_D_ECLOSIONS_VISE`→`NOMBRE_DE_RENAISSANCES_VISE`, `NOMBRE_D_ESPECES_DE_BASE`, `NOMBRE_DE_DIVERGENCES`, `NIVEAU_DU_HEROS_AU_DEPART`, `PALIERS_OUVERTS_AU_DEPART`, `FRANCHISSEMENTS_POUR_LES_SIGNES`, `FRANCHISSEMENTS_POUR_LES_DIRECTIVES`, `BUDGET_DE_VERBES_TOTAL`, `BUDGET_DE_VERBES_ARBRE`, `PERIODE_DE_TICK_MS`, `VERSION_SAVE`) ;
- une expression calculée donne `public static readonly double NOM = …;` avec l'expression **telle quelle** (`Math.Pow(CROISSANCE_PAR_CYCLE_VISEE, 1 / PALIERS_PAR_CYCLE_VISE)`), jamais sa valeur recopiée ;
- `5 * 60` reste `5 * 60` ;
- les renommages de la table globale s'appliquent ;
- `VERSION_SAVE = 1`, avec ce commentaire : `// La sauvegarde Unity repart à 1 (spec 2026-09-27) : les saves du navigateur ne sont pas reprises.`

Les trois formes non scalaires :

```csharp
public static readonly IReadOnlyList<SeuilDeJalon> SEUILS_DE_JALON = new[]
{
    new SeuilDeJalon(10, 2), new SeuilDeJalon(25, 4), new SeuilDeJalon(50, 8), new SeuilDeJalon(100, 16),
};

public static readonly IReadOnlyList<int> COUTS_DE_NOEUD = new[] { 5, 12, 25, 45, 80 };

public static readonly Reglage REGLAGE_CANONIQUE = new Reglage(CROISSANCE_DU_SEJOUR_PAR_PALIER);
```

et les deux fonctions :

```csharp
public static double MultiplicateurDePalier()
{
    var parPalier = Math.Pow(
        Math.Pow(D_PRODUCTION_PAR_PALIER, ESPECE_TOUS_LES_N_PALIERS) / DEBIT_RATIO_ESPECE,
        1.0 / ESPECE_TOUS_LES_N_PALIERS);
    return parPalier / (1 + BONUS_PAR_NIVEAU_DU_HEROS);
}

public static double DensiteExposant() => THETA_PART_COMPENSEE / ALPHA_GAIN_DE_DENSITE;
```

**Piège de l'initialisation statique.** Un `static readonly` se calcule dans l'ordre du fichier. `REGLAGE_CANONIQUE` doit donc venir après `CROISSANCE_DU_SEJOUR_PAR_PALIER`, et tout `static readonly` après les `const` qu'il lit. Une constante `const` n'a pas ce problème. `1 / ESPECE_TOUS_LES_N_PALIERS` entre deux `int` vaudrait 0 : écrire `1.0 /`.

- [ ] **Step 7: Lancer les tests**

Run: `outils/unity.sh tests EditMode "PrngTests|ConstantesTests"`
Expected: `total 4 · réussis 4 · échoués 0`.

- [ ] **Step 8: Commit**

```bash
git add Assets/IdlePond/Noyau Assets/IdlePond/Tests/PrngTests.cs* Assets/IdlePond/Tests/ConstantesTests.cs*
git commit -m "feat(noyau): PRNG, types et constantes au lexique du Codex"
```

---

### Task 5: Les données

**Files:**
- Create: `Assets/IdlePond/Noyau/Donnees/{Assises,Especes,Paliers,Insufflations,Echelles,NoeudsTechnique,Textes}.cs`
- Create: `Assets/IdlePond/Noyau/Donnees/Succes/{Actes,Seuils,Franchissements,RegistreDesSucces}.cs`
- Test: `Assets/IdlePond/Tests/DonneesTests.cs`

**Interfaces:**
- Consumes: types et constantes (tâche 4), `donnees.json`.
- Produces (espace de noms `IdlePond.Noyau.Donnees`) :
  - `Assises.Toutes : IReadOnlyList<Assise>`, `Assises.DuPalier(int) : Assise`, `Assises.PALIERS_LIVRES : int` ;
  - `Especes.Toutes : IReadOnlyList<Espece>`, `Especes.ParId(string) : Espece` (null si inconnue), `Especes.DeLAssise(string)`, `Especes.ESPECE_RESERVEE = "tanche"` ;
  - `Paliers.Tous : IReadOnlyList<Palier>`, `Paliers.EspeceDuPalier(int) : Espece` (null si aucune ou hors borne) ;
  - `Insufflations.Toutes`, `Insufflations.GLOBALE_ID`, `Insufflations.ParId(string)` (null si inconnue), `Insufflations.CibleeDe(string espece)` (lève si absente) ;
  - `Echelles.PuissanceDeG(int)`, `PuissanceDuCoutDeNiveau(int)`, `PuissanceDuMultiplicateurDePalier(int)`, `DebitBaseDuRang(int)` → `Decimal` ;
  - `NoeudsTechnique.Tous : IReadOnlyList<NoeudTechnique>` (vide) ;
  - `Actes.Tous`, `Seuils.Tous`, `Franchissements.Tous`, `RegistreDesSucces.Tous : IReadOnlyList<Succes>` (Actes, puis Seuils, puis Franchissements) ;
  - `Textes` : `NOM_DES_ASSISES`, `NOM_DES_ESPECES` (`IReadOnlyDictionary<string,string>`), `INSUFFLATION_GLOBALE` et `INSUFFLATION_CIBLEE` (records `TexteDInsufflation(string Nom, string Effet)`), `TexteDeSucces(string Nom, string Condition, string Rapport)`, `Textes.DuSucces(string id)`, `Textes.SUCCES_INCONNU`, `Textes.TousLesTextesAffiches() : IEnumerable<(string Ou, string Texte)>`.

- [ ] **Step 1: Écrire le test qui échoue**

`Assets/IdlePond/Tests/DonneesTests.cs` :

```csharp
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class DonneesTests
    {
        static readonly JObject Reference = References.Lire("donnees");

        static string Snake(string pascal) => string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

        /// Une chaîne nulle doit devenir un null JSON, pas une JValue de type String.
        static JToken Chaine(string s) => s == null ? JValue.CreateNull() : new JValue(s);

        static void Egal(string quoi, System.Collections.Generic.IEnumerable<JObject> obtenus, JToken attendus)
        {
            var tableau = new JArray(obtenus);
            Assert.That(JToken.DeepEquals(tableau, attendus), Is.True, $"{quoi} :\n{tableau}\n≠\n{attendus}");
        }

        [Test, Description("les assises, espèces et paliers sont ceux du TypeScript")]
        public void Les_assises_especes_et_paliers_sont_ceux_du_TypeScript()
        {
            Assert.That(Assises.PALIERS_LIVRES, Is.EqualTo((int)Reference["paliersLivres"]));
            Egal("assises", Assises.Toutes.Select(a => new JObject
            {
                ["id"] = a.Id, ["rang"] = a.Rang, ["typeMana"] = a.TypeMana,
                ["indexPremierPalier"] = a.IndexPremierPalier, ["nombreDePaliers"] = a.NombreDePaliers,
            }), Reference["assises"]);
            Egal("especes", Especes.Toutes.Select(e => new JObject
            {
                ["id"] = e.Id, ["assise"] = e.Assise, ["rang"] = e.Rang, ["palier"] = e.Palier,
            }), Reference["especes"]);
            Egal("paliers", Paliers.Tous.Select(p => new JObject
            {
                ["index"] = p.Index, ["assise"] = p.Assise, ["espece"] = Chaine(p.Espece),
            }), Reference["paliers"]);
        }

        [Test, Description("les insufflations sont les bénédictions renommées")]
        public void Les_insufflations_sont_les_benedictions_renommees()
        {
            Egal("insufflations", Insufflations.Toutes.Select(i => new JObject
            {
                ["id"] = i.Id, ["portee"] = i.Portee == PorteeDInsufflation.Globale ? "globale" : "ciblee", ["espece"] = Chaine(i.Espece),
            }), Reference["insufflations"]);
        }

        [Test, Description("le registre des succès est celui du TypeScript, dans le même ordre")]
        public void Le_registre_des_succes_est_celui_du_TypeScript_dans_le_meme_ordre()
        {
            var attendus = (JArray)Reference["succes"];
            Assert.That(RegistreDesSucces.Tous.Count, Is.EqualTo(attendus.Count));
            for (var i = 0; i < attendus.Count; i++)
            {
                var s = RegistreDesSucces.Tous[i];
                var a = attendus[i];
                Assert.That(s.Id, Is.EqualTo((string)a["id"]));
                Assert.That(Snake(s.Famille.ToString()), Is.EqualTo((string)a["famille"]), s.Id);
                Assert.That(Snake(s.Visibilite.ToString()), Is.EqualTo((string)a["visibilite"]), s.Id);
                Assert.That(s.Assise, Is.EqualTo((string)a["assise"]), s.Id);
                var dec = a["declencheur"];
                Assert.That(Snake(s.Declencheur.Quoi.ToString()), Is.EqualTo((string)dec["quoi"]), s.Id);
                if (dec["seuil"] != null) Assert.That(s.Declencheur.Seuil, Is.EqualTo((double)dec["seuil"]), s.Id);
                if (dec["espece"] != null) Assert.That(s.Declencheur.Espece, Is.EqualTo((string)dec["espece"]), s.Id);
                if (dec["palier"] != null) Assert.That(s.Declencheur.Palier, Is.EqualTo((int)dec["palier"]), s.Id);
                var effet = a["effet"];
                if (effet.Type == JTokenType.Null) { Assert.That(s.Effet, Is.Null, s.Id); continue; }
                Assert.That(Snake(s.Effet.Genre.ToString()), Is.EqualTo((string)effet["genre"]), s.Id);
                if (effet["terme"] != null) Assert.That(Termes.Identifiant(s.Effet.Terme.Value), Is.EqualTo((string)effet["terme"]), s.Id);
                if (effet["part"] != null) Assert.That(s.Effet.Part, Is.EqualTo((double)effet["part"]), s.Id);
            }
        }

        [Test, Description("les échelles tabulées valent le calcul direct au-delà de la table")]
        public void Les_echelles_tabulees_valent_le_calcul_direct_au_dela_de_la_table()
        {
            Assert.That(Echelles.PuissanceDeG(0).Eq(Decimal.Un), Is.True);
            Assert.That(Echelles.PuissanceDeG(200).Eq(Decimal.Pow(Constantes.G_COUT_PALIER, 200)), Is.True);
            Assert.That(Echelles.PuissanceDuCoutDeNiveau(5000).Eq(Decimal.Pow(Constantes.RATIO_COUT_NIVEAU, 5000)), Is.True);
            Assert.That(Echelles.DebitBaseDuRang(0).ToNumber(), Is.EqualTo(Constantes.TAUX_BASE_AU_PALIER_0));
        }

        [Test, Description("les textes n’ont aucune clef d’espèce ou de succès orpheline")]
        public void Les_textes_n_ont_aucune_clef_orpheline()
        {
            foreach (var id in Textes.NOM_DES_ESPECES.Keys) Assert.That(Especes.ParId(id), Is.Not.Null, id);
            foreach (var s in RegistreDesSucces.Tous) Assert.That(Textes.DuSucces(s.Id), Is.Not.SameAs(Textes.SUCCES_INCONNU), s.Id);
        }
    }
}
```

`Snake` rend `NiveauDEspece` → `niveau_d_espece` et `ReductionCout` → `reduction_cout` : ce sont les chaînes du TypeScript.

- [ ] **Step 2: Lancer le test pour le voir échouer**

Run: `outils/unity.sh tests EditMode DonneesTests`
Expected: échec de compilation (`IdlePond.Noyau.Donnees` inconnu).

- [ ] **Step 3: Porter les données**

Chaque fichier se traduit ligne à ligne depuis sa source, commentaires compris :

| Source | C# | Points d'attention |
|---|---|---|
| `src/donnees/assises.ts` | `Assises.cs` | `IDENTIFIANTS_D_ASSISE[rang] ?? $"assise-{rang + 1}"` : tester la borne. `DuPalier` lève `InvalidOperationException($"Palier hors des assises : {index}")` |
| `src/donnees/especes.ts` | `Especes.cs` | `ESPECES_NOMMEES[rangAssise]?.[i]` : double test de borne. `ParId` s'appuie sur un `Dictionary` construit après `Toutes` |
| `src/donnees/paliers.ts` | `Paliers.cs` | `EspeceDuPalier` rend null hors borne |
| `src/donnees/benedictions.ts` | `Insufflations.cs` | renommages de la table globale ; message : `$"Aucune insufflation ciblée pour l'espèce {espece}"` |
| `src/donnees/echelles.ts` | `Echelles.cs` | `table[i] ?? calcul` devient `i >= 0 && i < table.Length ? table[i] : calcul`. `tabuler` multiplie par un `double` : `table[i - 1].Mul(ratio)` (surcharge `double`). `puissanceDuPalier` : `Decimal.Pow(Constantes.MultiplicateurDePalier(), exposant)`. `debitBaseDuRangCalcule` : `new Decimal(TAUX_BASE_AU_PALIER_0).Mul(Math.Pow(DEBIT_RATIO_ESPECE, rang))` |
| `src/donnees/noeuds-technique.ts` | `NoeudsTechnique.cs` | `Tous = Array.Empty<NoeudTechnique>()` |
| `src/donnees/succes/actes.ts`, `seuils.ts`, `franchissements.ts`, `index.ts` | `Succes/*.cs` | déclencheur `eclosions` → `QuoiDeclencheur.Renaissances`. Les identifiants de succès sont recopiés **à l'octet près**, y compris `…-eclosion` et `acte-premier-banc-de-cinq`. Le gabarit des seuils reste un gabarit (boucle sur les espèces), pas une liste dépliée |
| `src/donnees/textes-provisoires.ts` | `Textes.cs` | voir ci-dessous |

**`Textes.cs`.** La traduction est fidèle, avec deux changements imposés par le Codex :
- `TEXTE_DE_LA_BENEDICTION_GLOBALE` devient `INSUFFLATION_GLOBALE = new TexteDInsufflation("Insuffler l’eau", "tout ce qui vit ici capte un peu plus, et tout ce qui viendra")` ;
- `TEXTE_DE_BENEDICTION_CIBLEE` devient `INSUFFLATION_CIBLEE = new TexteDInsufflation(null, "ils te donnent moitié plus, à chaque fois")`.

Toute autre phrase qui contient un mot mort (Foi, bénir, bénédiction, éclore, ponte…) se réécrit avec le mot du Codex : « renaître » pour l'acte, « Souffle », « insuffler ». Relire chaque phrase ; le test de la tâche 9 les balaie toutes. Les apostrophes restent **courbes** (`’`). `GABARITS` (des `RegExp`) devient une liste de `(Regex Motif, Func<int, TexteDeSucces> Texte)` avec `RegexOptions.CultureInvariant`. `tour(liste, n)` devient `liste[((n % liste.Count) + liste.Count) % liste.Count]`, même sémantique que `((n % l) + l) % l` si la source l'écrit ainsi ; sinon, copier son expression telle quelle.

`TousLesTextesAffiches()` énumère, dans cet ordre : chaque nom d'assise, chaque nom d'espèce, les deux textes d'insufflation (noms non nuls et effets), puis, pour chaque succès du registre, le `Nom`, la `Condition` et le `Rapport` de `DuSucces(id)`. `Ou` est une étiquette lisible (`"succes seuil-vairon-10.rapport"`).

- [ ] **Step 4: Lancer les tests**

Run: `outils/unity.sh tests EditMode DonneesTests`
Expected: `total 5 · réussis 5 · échoués 0`.

- [ ] **Step 5: Commit**

```bash
git add Assets/IdlePond/Noyau/Donnees Assets/IdlePond/Tests/DonneesTests.cs*
git commit -m "feat(noyau): les données — assises, espèces, paliers, insufflations, succès, textes"
```

---

### Task 6: Économie, densité, technique, voix

**Files:**
- Create: `Assets/IdlePond/Noyau/{Densite,Technique,Voix,Economie}.cs`
- Test: `Assets/IdlePond/Tests/SeuilsTests.cs`, `Assets/IdlePond/Tests/CaptationTests.cs`, `Assets/IdlePond/Tests/EspecesTests.cs`

**Interfaces:**
- Consumes: tâches 3 à 5.
- Produces (espace de noms `IdlePond.Noyau`, classes statiques) :
  - `Densite.DuPalier(EtatJeu, int)`, `DuSejour(EtatJeu)`, `Multiplicateur(double densite)`, `LaisseeParLeCycle(Decimal pic)` → `double` ; `AppliquerGain(EtatJeu, int paliersOuverts, Decimal pic)` → `IReadOnlyList<double>` ;
  - `Technique.REGIME_PAR_BRANCHE : IReadOnlyDictionary<BrancheTechnique, RegimeCompteur>`, `COUPLES_A_B : IReadOnlyDictionary<BrancheTechnique, (double A, double B)>` (vide), `SEUILS_PAR_BRANCHE_BORNEE : IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>>` (vide), `PointsDeBranche(BrancheTechnique, double compteur) : int`, `PointsDisponibles(EtatJeu, BrancheTechnique) : int`, `FacteurDeTechnique(EtatJeu, TermeDeFormule) : double`, `CapacitesDeLArbre(EtatJeu) : IReadOnlyCollection<CapaciteId>`, `CreditCompteur(IReadOnlyDictionary<BrancheTechnique, double>, BrancheTechnique, double) : IReadOnlyDictionary<BrancheTechnique, double>` ;
  - `Voix.PALIERS : IReadOnlyList<PalierDeVoix>`, `Voix.PalierDe(EtatJeu)`, `Voix.PalierApres(int)`, `Voix.AuMoins(PalierDeVoix atteint, PalierDeVoix requis) : bool` ;
  - `Economie` : `MultiplicateurDeSeuil(int) : double`, `MultiplicateurDesDrapeaux(EtatJeu) : double`, `DebitBaseDeLEspece(Espece) : Decimal`, `RangDInsufflation(EtatJeu, string) : int`, `DebitInsuffle(EtatJeu, Espece) : Decimal`, `MultiplicateurDInsufflation(EtatJeu, Espece) : double`, `MultiplicateurDeProfondeur(EtatJeu) : Decimal`, `MultiplicateurDuHeros(EtatJeu) : double`, `MultiplicateursGlobaux(EtatJeu) : Decimal`, `ProductionDeLEspece(EtatJeu, Espece) : Decimal`, `ProductionDuHeros(EtatJeu) : Decimal`, `ProductionTotaleParSeconde(EtatJeu) : Decimal`, `DetailDuHeros(EtatJeu)` et `DetailDeCaptation(EtatJeu, Espece)` : `IReadOnlyList<LigneDeCaptation>`, `FacteurDeSucces(EtatJeu, TermeDeFormule) : double`, `CoutBaseDuPalier(int)`, `CoutDeDescente(EtatJeu, int)`, `CoutDeDeblocage(EtatJeu, Espece)`, `CoutDeNiveau(EtatJeu, Espece, int)`, `CoutDeCroissance(EtatJeu, int)`, `CoutDInsufflation(EtatJeu, Insufflation)`, `Contenance(EtatJeu)` : `Decimal`, `PartDeContenance(EtatJeu) : double`, `EauTroublee`, `EstSature`, `ToutEstCreuse`, `EstBloque(EtatJeu) : bool`.
- Produces, pour les tests : `Reducteur.EtatInitial` n'existe pas encore. Les tests de cette tâche construisent leurs états avec `EtatDeTravail` (tâche 7), donc **seuls les tests purs** sont portés ici (voir step 1). Le reste de `seuils`, `captation` et `especes` est porté à la tâche 7.

- [ ] **Step 1: Écrire les tests purs qui échouent**

Porter depuis `tests/seuils.test.ts`, `tests/captation.test.ts` et `tests/especes.test.ts` **les seuls cas qui n'appellent ni `etatInitial`, ni `tick`, ni un acte, ni `etatDeTravail`**. Chaque cas porté garde son titre (règle des noms de méthode). Au minimum, ajouter ceux-ci à `SeuilsTests.cs` :

```csharp
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class SeuilsTests
    {
        [Test, Description("les seuils sont lus sur le niveau et CUMULÉS : ×2, ×4, ×8, ×16")]
        public void Les_seuils_sont_lus_sur_le_niveau_et_CUMULES()
        {
            Assert.That(Economie.MultiplicateurDeSeuil(9), Is.EqualTo(1));
            Assert.That(Economie.MultiplicateurDeSeuil(10), Is.EqualTo(2));
            Assert.That(Economie.MultiplicateurDeSeuil(25), Is.EqualTo(4));
            Assert.That(Economie.MultiplicateurDeSeuil(50), Is.EqualTo(8));
            Assert.That(Economie.MultiplicateurDeSeuil(100), Is.EqualTo(16));
            Assert.That(Economie.MultiplicateurDeSeuil(1000), Is.EqualTo(16));
        }

        [Test, Description("la densité nulle ne multiplie rien, et la voix suit les franchissements")]
        public void La_densite_nulle_ne_multiplie_rien_et_la_voix_suit_les_franchissements()
        {
            Assert.That(Densite.Multiplicateur(0), Is.EqualTo(1));
            Assert.That(Voix.PalierApres(0), Is.EqualTo(PalierDeVoix.Pente));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES), Is.EqualTo(PalierDeVoix.Signes));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES), Is.EqualTo(PalierDeVoix.Directives));
            Assert.That(Voix.AuMoins(PalierDeVoix.Directives, PalierDeVoix.Signes), Is.True);
        }

        [Test, Description("le coût de base d’un palier est g puissance palier moins un, fois le premier creusement")]
        public void Le_cout_de_base_d_un_palier()
        {
            Assert.That(Economie.CoutBaseDuPalier(0).ToNumber(), Is.EqualTo(Constantes.COUT_CREUSER_AU_PALIER_1));
            Assert.That(Economie.CoutBaseDuPalier(1).ToNumber(), Is.EqualTo(Constantes.COUT_CREUSER_AU_PALIER_1));
            Assert.That(Economie.CoutBaseDuPalier(3).Eq(Echelles.PuissanceDeG(2).Mul(Constantes.COUT_CREUSER_AU_PALIER_1)), Is.True);
        }
    }
}
```

`using IdlePond.Noyau.Donnees;` est nécessaire pour `Echelles`.

- [ ] **Step 2: Lancer les tests pour les voir échouer**

Run: `outils/unity.sh tests EditMode SeuilsTests`
Expected: échec de compilation sur `Economie`, `Densite`, `Voix`.

- [ ] **Step 3: Porter les quatre modules**

| Source | C# | Points d'attention |
|---|---|---|
| `src/noyau/densite.ts` | `Densite.cs` | `etat.permanent.densites[palier] ?? 0` : tester la borne. `AppliquerGain` rend une **nouvelle** liste |
| `src/noyau/technique.ts` | `Technique.cs` | `Math.floor(a * Math.log(1 + c / b))` devient `(int)Math.Floor(…)`. `CreditCompteur` copie le dictionnaire puis ajoute, en commençant à 0 si la branche manque |
| `src/noyau/voix.ts` | `Voix.cs` | `indexOf` → `IndexOf` sur `PALIERS` |
| `src/noyau/economie.ts` | `Economie.cs` | voir ci-dessous |

`Economie.cs`, traduction ligne à ligne de `src/noyau/economie.ts` (lignes 1 à 457), commentaires compris :
- `.mul(nombre)` → `.Mul(double)` ; `.mul(decimal)` → `.Mul(Decimal)`. Respecter le type de l'argument **tel qu'il est dans la source** : `multiplicateursGlobaux` enchaîne `Decimal.Mul(double)` pour la densité, le héros, les drapeaux et l'échelle ;
- `ESPECES.reduce((s, e) => s.add(…), new Decimal(0))` devient une boucle `foreach` sur `Especes.Toutes`, **dans l'ordre du registre** : l'addition de `Decimal` n'est pas associative au bit près ;
- `etat.cycle.especes[id]` absent → `TryGetValue` ;
- `facteurDeSucces` parcourt `RegistreDesSucces.Tous` et lit `etat.Permanent.Succes.ContainsKey(id)` ;
- `detailDeCaptation` produit des `LigneDeCaptation(TermeDeFormule.X, valeur, new SourceDeTerme(QuoiSource.Y, v))` dans **le même ordre** que la source ;
- `coutDeBenediction` → `CoutDInsufflation` : `new Decimal(@base).Mul(Decimal.Pow(RATIO_COUT_D_INSUFFLATION, rang)).Mul(FacteurDeCout(etat, TermeDeFormule.CoutInsufflation))`.

- [ ] **Step 4: Lancer les tests**

Run: `outils/unity.sh tests EditMode "SeuilsTests|DonneesTests|DecimalTests|PrngTests|ConstantesTests"`
Expected: tous verts.

- [ ] **Step 5: Commit**

```bash
git add Assets/IdlePond/Noyau Assets/IdlePond/Tests
git commit -m "feat(noyau): économie, densité, technique et voix"
```

---

### Task 7: Succès, renaissance, réducteur, et les tests de mécanique

C'est la tâche centrale. Après elle, tout le noyau tourne en C#, et chaque test de mécanique TypeScript a son jumeau NUnit vert.

**Files:**
- Create: `Assets/IdlePond/Noyau/{RegleDesSucces,Renaissance,Reducteur,Telemetrie}.cs`
- Create: `Assets/IdlePond/Tests/Outils/{Comparateur,Instantane,EtatDeTravail}.cs`
- Test: `Assets/IdlePond/Tests/{ReducteurTests,EquivalenceDePasTests,DeterminismeTests,ContenanceTests,RedescenteTests,HerosTests,InsufflationsTests,RenaissanceTests,AmorcageTests,VoixTests,CanonTests}.cs`, et compléter `SeuilsTests`, `CaptationTests` et `EspecesTests`

**Interfaces:**
- Consumes: tâches 3 à 6.
- Produces :
  - `RegleDesSucces` : `record ResultatDeSucces(EtatJeu Etat, IReadOnlyList<string> Declenches)`, `EstAtteint(EtatJeu, DeclencheurDeSucces)`, `VerifierSucces(EtatJeu) : ResultatDeSucces`, `EstAcquis(EtatJeu, string)`, `EnregistrerIntervalleDeSucces(EtatJeu, IReadOnlyList<string>) : EtatJeu`, `CapacitesDesSucces(EtatJeu)`, `AssisesAtteintes(EtatJeu) : IReadOnlyCollection<string>`, `record SuccesAffichable(Succes Succes, bool Acquis, VisibiliteDeSucces Visibilite, EntreeDeSucces Entree)`, `SuccesListables(EtatJeu, string assise)`, `ProgressionVersLeSucces(EtatJeu, Succes) : double?` ;
  - `Renaissance` : `GainDeSoufflePrevu(EtatJeu) : Decimal`, `CycleInitial() : EtatCycle`, `CouchesApres(EtatJeu, int) : IReadOnlyList<string>`, `Renaitre(EtatJeu) : EtatJeu` ;
  - `Reducteur` : `EtatInitial(long graine, int limiteDeContenu = Constantes.NOMBRE_DE_PALIERS, Reglage reglage = null) : EtatJeu` (null → `REGLAGE_CANONIQUE`), `TauDuSejourSecondes(EtatJeu) : double`, `record ResultatDeTick(EtatJeu Etat, IReadOnlyList<string> Declenches)`, `TickDetaille(EtatJeu, double dt) : ResultatDeTick`, `Tick(EtatJeu, double) : EtatJeu`, `Creuser(EtatJeu)`, `Debloquer(EtatJeu, string)`, `Ameliorer(EtatJeu, string)`, `Grandir(EtatJeu)`, `Insuffler(EtatJeu, string)` : `EtatJeu` ;
  - `Telemetrie` : `record ReleveDeCycle(int Index, double DureeEcouleeSecondes, int PaliersOuverts, double ProductionPicParSeconde, double SouffleGagne)`, `record Releve(int NombreDeRenaissances, IReadOnlyList<ReleveDeCycle> Cycles, double? IntervalleMoyenEntreSuccesSecondes, IReadOnlyDictionary<BrancheTechnique, int> PointsDeTechniqueRendus, double TempsEcouleSecondes)`, `Relever(EtatJeu) : Releve` ;
  - outils de test : `Instantane.De(EtatJeu, bool avecDerives = true) : JObject`, `Comparateur.ComparerATolerance(JToken obtenu, JToken attendu, double tolerance = 1e-9, string chemin = "")`, `Comparateur.TOLERANCE_RELATIVE`, `EtatDeTravail.Creer(long graine = 12345, string contenance = "1e14") : EtatJeu`.

- [ ] **Step 1: Écrire les outils de test**

`Assets/IdlePond/Tests/Outils/Instantane.cs`. Il produit **les mêmes clefs** que `instantane()` dans `tests/parite/generer-references.ts` :

```csharp
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Newtonsoft.Json.Linq;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Un état de jeu en arbre JSON, clef pour clef celui du générateur de
    /// références. Les dictionnaires sont énumérés dans l'ORDRE DU REGISTRE, jamais
    /// dans celui du dictionnaire : c'est ce qui rend deux instantanés comparables
    /// à la chaîne près.
    /// </summary>
    public static class Instantane
    {
        static string D(Decimal x) => x.EnMantisseExposant();
        static string Registre(PalierDeVoix p) => p.ToString().ToLowerInvariant();

        public static JObject De(EtatJeu etat, bool avecDerives = true)
        {
            var c = etat.Cycle;
            var p = etat.Permanent;
            var t = etat.Telemetrie;

            var especes = new JObject();
            foreach (var e in Especes.Toutes)
                if (c.Especes.TryGetValue(e.Id, out var v))
                    especes[e.Id] = new JObject { ["debloquee"] = v.Debloquee, ["niveau"] = v.Niveau };

            var succes = new JObject();
            foreach (var s in RegistreDesSucces.Tous)
                if (p.Succes.TryGetValue(s.Id, out var entree))
                    succes[s.Id] = new JObject { ["obtenuAuCycle"] = entree.ObtenuAuCycle, ["registre"] = Registre(entree.Registre) };

            var insufflations = new JObject();
            foreach (var i in Insufflations.Toutes)
                if (p.Insufflations.TryGetValue(i.Id, out var rang))
                    insufflations[i.Id] = rang;

            double Compteur(BrancheTechnique b) => p.CompteursTechnique.TryGetValue(b, out var x) ? x : 0;

            var racine = new JObject
            {
                ["tempsJeuSecondes"] = etat.TempsJeuSecondes,
                ["limiteDeContenu"] = etat.LimiteDeContenu,
                ["prng"] = new JObject { ["graine"] = etat.Prng.Graine },
                ["cycle"] = new JObject
                {
                    ["manaCourant"] = D(c.ManaCourant),
                    ["paliersOuverts"] = c.PaliersOuverts,
                    ["especes"] = especes,
                    ["productionPicParSeconde"] = D(c.ProductionPicParSeconde),
                    ["dureeSecondes"] = c.DureeSecondes,
                    ["acquisDeSejour"] = c.AcquisDeSejour,
                    ["niveauDuHeros"] = c.NiveauDuHeros,
                },
                ["permanent"] = new JObject
                {
                    ["densites"] = new JArray(p.Densites.Select(x => (object)x)),
                    ["souffle"] = D(p.Souffle),
                    ["contenanceMana"] = D(p.ContenanceMana),
                    ["couches"] = new JArray(p.Couches),
                    ["profondeurMaxAtteinte"] = p.ProfondeurMaxAtteinte,
                    ["compteursTechnique"] = new JObject
                    {
                        ["creusement"] = Compteur(BrancheTechnique.Creusement),
                        ["amelioration"] = Compteur(BrancheTechnique.Amelioration),
                        ["recrutement"] = Compteur(BrancheTechnique.Recrutement),
                        ["entretien"] = Compteur(BrancheTechnique.Entretien),
                        ["construction"] = Compteur(BrancheTechnique.Construction),
                        ["renaissance"] = Compteur(BrancheTechnique.Renaissance),
                    },
                    ["noeudsTechnique"] = new JArray(p.NoeudsTechnique),
                    ["succes"] = succes,
                    ["nombreDeRenaissances"] = p.NombreDeRenaissances,
                    ["especesAyantAtteintCent"] = new JArray(p.EspecesAyantAtteintCent),
                    ["manaAmbiant"] = D(p.ManaAmbiant),
                    ["heuresHorsLigneCreditees"] = p.HeuresHorsLigneCreditees,
                    ["insufflations"] = insufflations,
                },
                ["telemetrie"] = new JObject
                {
                    ["cycles"] = new JArray(t.Cycles.Select(m => new JObject
                    {
                        ["index"] = m.Index,
                        ["dureeEcouleeSecondes"] = m.DureeEcouleeSecondes,
                        ["paliersOuverts"] = m.PaliersOuverts,
                        ["productionPicParSeconde"] = D(m.ProductionPicParSeconde),
                        ["souffleGagne"] = D(m.SouffleGagne),
                    })),
                    ["secondesDepuisDernierSucces"] = t.SecondesDepuisDernierSucces,
                    ["intervallesEntreSucces"] = new JArray(t.IntervallesEntreSucces.Select(x => (object)x)),
                },
            };
            if (avecDerives)
            {
                racine["derives"] = new JObject
                {
                    ["production"] = D(Economie.ProductionTotaleParSeconde(etat)),
                    ["contenance"] = D(Economie.Contenance(etat)),
                    ["partDeContenance"] = Economie.PartDeContenance(etat),
                    ["eauTroublee"] = Economie.EauTroublee(etat),
                    ["estBloque"] = Economie.EstBloque(etat),
                    ["gainDeSoufflePrevu"] = D(Renaissance.GainDeSoufflePrevu(etat)),
                };
            }
            return racine;
        }
    }
}
```

`Assets/IdlePond/Tests/Outils/Comparateur.cs`, portage de `tests/outils.ts` (`comparerAToleranceFlottante`) sur des `JToken` :

```csharp
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// « À la tolérance flottante près » (§12), et rien de plus permissif. Un
    /// Decimal s'écrit « m e » dans un instantané : deux chaînes de cette forme se
    /// comparent en nombres ; toute autre chaîne se compare à l'identique.
    /// </summary>
    public static class Comparateur
    {
        public const double TOLERANCE_RELATIVE = 1e-9;

        static readonly Regex FormeDecimal = new Regex(@"^-?[0-9.]+(e[+-]?[0-9.]+)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        static bool Proche(double a, double b, double tolerance)
        {
            if (a.Equals(b)) return true;
            if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b)) return false;
            var echelle = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1e-300);
            return Math.Abs(a - b) / echelle <= tolerance;
        }

        public static void ComparerATolerance(JToken obtenu, JToken attendu, double tolerance = TOLERANCE_RELATIVE, string chemin = "")
        {
            if (attendu is JValue va && va.Type == JTokenType.String && obtenu is JValue vo && vo.Type == JTokenType.String
                && FormeDecimal.IsMatch((string)va) && FormeDecimal.IsMatch((string)vo))
            {
                var a = Decimal.Parse((string)vo);
                var b = Decimal.Parse((string)va);
                if (a.Eq(b)) return;
                var ecart = a.Sub(b).Abs().Div(Decimal.Max(a.Abs(), b.Abs()).Add(1e-300)).ToNumber();
                Assert.That(ecart, Is.LessThanOrEqualTo(tolerance), $"Decimal divergent en {chemin} : {vo} vs {va}");
                return;
            }
            if (attendu.Type is JTokenType.Integer or JTokenType.Float && obtenu.Type is JTokenType.Integer or JTokenType.Float)
            {
                var a = obtenu.Value<double>();
                var b = attendu.Value<double>();
                Assert.That(Proche(a, b, tolerance), Is.True, $"nombre divergent en {chemin} : {a.ToString("R", CultureInfo.InvariantCulture)} vs {b.ToString("R", CultureInfo.InvariantCulture)}");
                return;
            }
            if (attendu is JArray ta)
            {
                Assert.That(obtenu, Is.InstanceOf<JArray>(), $"tableau attendu en {chemin}");
                var to = (JArray)obtenu;
                Assert.That(to.Count, Is.EqualTo(ta.Count), $"longueur divergente en {chemin}");
                for (var i = 0; i < ta.Count; i++) ComparerATolerance(to[i], ta[i], tolerance, $"{chemin}[{i}]");
                return;
            }
            if (attendu is JObject oa)
            {
                Assert.That(obtenu, Is.InstanceOf<JObject>(), $"objet attendu en {chemin}");
                var oo = (JObject)obtenu;
                var clefs = oa.Properties().Select(p => p.Name).Union(oo.Properties().Select(p => p.Name));
                foreach (var clef in clefs)
                {
                    var suite = chemin == "" ? clef : $"{chemin}.{clef}";
                    Assert.That(oo.ContainsKey(clef), Is.True, $"clef absente de l'obtenu : {suite}");
                    Assert.That(oa.ContainsKey(clef), Is.True, $"clef en trop dans l'obtenu : {suite}");
                    ComparerATolerance(oo[clef], oa[clef], tolerance, suite);
                }
                return;
            }
            Assert.That(JToken.DeepEquals(obtenu, attendu), Is.True, $"valeur divergente en {chemin} : {obtenu} vs {attendu}");
        }
    }
}
```

`Assets/IdlePond/Tests/Outils/EtatDeTravail.cs`, portage de `tests/etat-de-travail.ts` avec son commentaire d'en-tête :

```csharp
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    public static class EtatDeTravail
    {
        public static EtatJeu Creer(long graine = 12345, string contenance = "1e14")
        {
            var etat = Reducteur.EtatInitial(graine);
            etat = etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e12") },
                Permanent = etat.Permanent with
                {
                    ContenanceMana = Decimal.Parse(contenance),
                    Densites = etat.Permanent.Densites.Select((_, index) => index * 0.13).ToArray(),
                    ProfondeurMaxAtteinte = 9,
                },
            };
            for (var i = 0; i < 6; i++) etat = Reducteur.Creuser(etat);
            var ouvertes = Especes.Toutes.Where(e => e.Palier < etat.Cycle.PaliersOuverts).ToList();
            for (var index = 0; index < ouvertes.Count; index++)
            {
                var espece = ouvertes[index];
                etat = Reducteur.Debloquer(etat, espece.Id);
                var niveauCible = index == 0 ? Constantes.SEUIL_DU_DRAPEAU_PERMANENT : 12 + espece.Palier;
                for (var n = 0; n < niveauCible; n++) etat = Reducteur.Ameliorer(etat, espece.Id);
            }
            return etat;
        }
    }
}
```

`index * 0.13` : en JavaScript, `index` est un `number` ; en C#, `int * double` donne le même `double`.

- [ ] **Step 2: Écrire les tests du réducteur (Review Focus)**

`Assets/IdlePond/Tests/ReducteurTests.cs` :

```csharp
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ReducteurTests
    {
        [Test, Description("un acte impossible rend le même état")]
        public void Un_acte_impossible_rend_le_meme_etat()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Reducteur.Debloquer(etat, "inconnue"), Is.SameAs(etat));
            Assert.That(Reducteur.Ameliorer(etat, "inconnue"), Is.SameAs(etat));
            Assert.That(Reducteur.Insuffler(etat, "inconnue"), Is.SameAs(etat));
            var pauvre = Reducteur.EtatInitial(1);
            pauvre = pauvre with { Cycle = pauvre.Cycle with { ManaCourant = Decimal.Zero } };
            Assert.That(Reducteur.Creuser(pauvre), Is.SameAs(pauvre));
            Assert.That(Reducteur.Grandir(pauvre), Is.SameAs(pauvre));
        }

        [Test, Description("un dt non positif ou NaN ne fait rien")]
        public void Un_dt_non_positif_ou_NaN_ne_fait_rien()
        {
            var etat = EtatDeTravail.Creer();
            foreach (var dt in new[] { 0.0, -1.0, double.NaN, double.NegativeInfinity })
            {
                var resultat = Reducteur.TickDetaille(etat, dt);
                Assert.That(resultat.Etat, Is.SameAs(etat), dt.ToString());
                Assert.That(resultat.Declenches, Is.Empty);
            }
        }
    }
}
```

`Decimal` vient ici de `IdlePond.Noyau` par le `using`.

- [ ] **Step 3: Porter les tests de mécanique**

Chaque fichier devient une classe NUnit. Chaque `it(...)` devient une méthode `[Test, Description("titre exact")]`, avec la même mise en place et les mêmes assertions. `describe` imbriqués : une seule classe par fichier, les titres portent seuls. `expect(x).toBe(y)` → `Assert.That(x, Is.EqualTo(y))` ; `toBeCloseTo(y, n)` → `Is.EqualTo(y).Within(0.5 * Math.Pow(10, -n))` ; `toBeGreaterThan` → `Is.GreaterThan` ; `toEqual([])` → `Is.Empty` ; `comparerAToleranceFlottante(a, b)` sur des états → `Comparateur.ComparerATolerance(Instantane.De(a, false), Instantane.De(b, false))`.

| Source TypeScript | Classe C# | Particularités |
|---|---|---|
| `tests/equivalence-de-pas.test.ts` (7) | `EquivalenceDePasTests` | `{ ...etatDeTravail(), reglage: { … } }` → `EtatDeTravail.Creer() with { Reglage = new Reglage(1.2) }` |
| `tests/determinisme.test.ts` (4) | `DeterminismeTests` | `JSON.stringify(serialiser(x))` → `Instantane.De(x).ToString(Formatting.None)`. Le cas « deux simulations de même graine » vient à la tâche 8. Ajouter le test ci-dessous |
| `tests/contenance.test.ts` (13) | `ContenanceTests` | — |
| `tests/redescente.test.ts` (2) | `RedescenteTests` | — |
| `tests/heros.test.ts` (14) | `HerosTests` | `eclore` → `Renaissance.Renaitre` |
| `tests/benedictions.test.ts` (14) | `InsufflationsTests` | titres renommés : « bénédiction » → « insufflation », « bénir » → « insuffler », « béni » → « insufflé » |
| `tests/eclosion.test.ts` (11) | `RenaissanceTests` | titres renommés : « éclosion » → « renaissance », « éclore » → « renaître » |
| `tests/amorcage.test.ts` (3) | `AmorcageTests` | — |
| `tests/voix.test.ts` (9) | `VoixTests` | — |
| `tests/seuils.test.ts` (6) | `SeuilsTests` (compléter) | les cas non portés à la tâche 6 |
| `tests/captation.test.ts` (2) | `CaptationTests` | `terme: 'benediction_globale'` → `TermeDeFormule.InsufflationGlobale` |
| `tests/especes.test.ts` (7) | `EspecesTests` | — |
| `tests/canon.test.ts` (25) | `CanonTests` | **seulement** les tests de donnée et de chiffre : lignes 88 à 251 et 419 à 429 de la source (nœuds, insufflations, succès, capacités, budget de verbes, `g/D`, `D = 2.31`, `D ≠ g`, `f = 1`, seuils cumulés, `θ`, 62/6/21, paliers, ancrage des espèces, multiplicateur de palier, `tanche`, textes de succès). « `f = 1` : la constante n'existe plus » se vérifie par réflexion : `typeof(Constantes).GetField("F_…")` est null. Les tests lexicaux (lignes 255 à 415) sont à la tâche 9 |

Test supplémentaire (Review Focus), dans `DeterminismeTests` :

```csharp
[Test, Description("l’ordre des achats ne change pas l’instantané")]
public void L_ordre_des_achats_ne_change_pas_l_instantane()
{
    var depart = Reducteur.EtatInitial(9);
    depart = depart with { Cycle = depart.Cycle with { ManaCourant = Decimal.Parse("1e9") } };
    depart = Reducteur.Creuser(Reducteur.Creuser(Reducteur.Creuser(Reducteur.Creuser(depart))));
    var a = Reducteur.Debloquer(Reducteur.Debloquer(depart, "vairon"), "loche");
    var b = Reducteur.Debloquer(Reducteur.Debloquer(depart, "loche"), "vairon");
    for (var i = 0; i < 99; i++) { a = Reducteur.Ameliorer(a, "vairon"); a = Reducteur.Ameliorer(a, "loche"); }
    for (var i = 0; i < 99; i++) b = Reducteur.Ameliorer(b, "loche");
    for (var i = 0; i < 99; i++) b = Reducteur.Ameliorer(b, "vairon");
    // Même dépense totale, dans un autre ordre : seuls le mana et les compteurs
    // peuvent différer d'arrondi ; l'ORDRE des clefs, jamais.
    Assert.That(Instantane.De(a)["cycle"]["especes"].ToString(), Is.EqualTo(Instantane.De(b)["cycle"]["especes"].ToString()));
    Assert.That(Instantane.De(a)["permanent"]["especesAyantAtteintCent"].ToString(),
        Is.EqualTo(Instantane.De(b)["permanent"]["especesAyantAtteintCent"].ToString()));
    Assert.That(Instantane.De(a)["permanent"]["succes"].ToString(), Is.EqualTo(Instantane.De(b)["permanent"]["succes"].ToString()));
}
```

- [ ] **Step 4: Lancer les tests pour les voir échouer**

Run: `outils/unity.sh tests EditMode`
Expected: échec de compilation sur `Reducteur`, `Renaissance`, `RegleDesSucces`.

- [ ] **Step 5: Porter les quatre modules**

| Source | C# | Points d'attention |
|---|---|---|
| `src/noyau/succes.ts` | `RegleDesSucces.cs` | `enOrdreDuRegistre` reconstruit un `Dictionary` en parcourant `RegistreDesSucces.Tous`. `especesDebloquees` et `niveauxCumules` parcourent `c.Especes.Values` (la somme d'entiers ne dépend pas de l'ordre). `switch` sur `QuoiDeclencheur` avec un `default` qui lève `InvalidOperationException` |
| `src/noyau/eclosion.ts` | `Renaissance.cs` | `new Set` + filtre → `HashSet` puis `Assises.Toutes.Where(...)` pour garder l'ordre des assises. `creditCompteur(…, 'eclosion', 1)` → `BrancheTechnique.Renaissance` |
| `src/noyau/noyau.ts` | `Reducteur.cs` | `etatInitial` : `new EtatPrng((uint)(graine & 0xFFFFFFFF))` reproduit `graine >>> 0` ; `densites` = `new double[NOMBRE_DE_PALIERS]` ; `compteursTechnique` = les six branches à 0 dans l'ordre de l'enum. `tickDetaille` : `if (!(dt > 0))` s'écrit **tel quel**, parce que c'est ce qui rejette `NaN`. `ameliorer` : `ESPECES.filter(...)` → `Especes.Toutes.Where(...).Select(e => e.Id).ToArray()`. `benir` → `Insuffler`. Les ré-exports (`export { … } from`) ne se portent pas : les appelants vont chercher `Economie.*` ou `Renaissance.*` directement. `tirer` est `Prng.Tirer` |
| `src/adaptateurs/telemetrie.ts` | `Telemetrie.cs` | la partie pure seulement : `Releve`, `ReleveDeCycle`, `Relever`. Le `Collecteur` appartient au plan 2 |

- [ ] **Step 6: Lancer toute la suite**

Run: `outils/unity.sh tests EditMode`
Expected: tous verts. En cas d'échec numérique, comparer au TypeScript en ajoutant un `console.log` dans le test `.ts` jumeau, **jamais** en relâchant une tolérance.

- [ ] **Step 7: Commit**

```bash
git add Assets/IdlePond
git commit -m "feat(noyau): succès, renaissance et réducteur — les tests de mécanique portés"
```

---

### Task 8: Simulateur, calibreur et joueur scripté

**Files:**
- Create: `Assets/IdlePond/Simulateur/Simulateur.cs`, `Assets/IdlePond/Simulateur/Calibreur.cs`
- Create: `Assets/IdlePond/Tests/Outils/Joueur.cs`
- Test: `Assets/IdlePond/Tests/{SimulateurTests,PlancherDeCadenceTests,PartieHeadlessTests}.cs`, et compléter `DeterminismeTests`

**Interfaces:**
- Consumes: tout le noyau (tâche 7).
- Produces (espace de noms `IdlePond.Simulateur`, classe statique `Simulateur`) : `record Politique(double SecondesEntreReleves, double Pas, double FractionDeSaturationPourRenaitre, double DureeMaxParCycleSecondes)`, `Simulateur.POLITIQUE_PAR_DEFAUT`, `enum TypeDAchat { Creuser, Grandir, Debloquer, Niveau }`, `record Achat(TypeDAchat Type, Espece Espece, Decimal Cout, Decimal Gain)`, `AchatsDisponibles(EtatJeu, Decimal? budget = null) : IReadOnlyList<Achat>`, `DoitRenaitre(EtatJeu, Politique) : bool`, `record ResultatDeSimulation(EtatJeu Etat, Releve Releve, IReadOnlyList<MesureDeCycle> Cycles, int CyclesDemandes, int CyclesAcheves, int? CycleNonConvergent, double SecondesActives, double SecondesEcoulees)`, `InsufflerAuMieux(EtatJeu) : EtatJeu`, `Simuler(int cycles, Politique politique = null, long graine = 1, Action<EtatJeu> observer = null, int? limiteDeContenu = null, Reglage reglage = null) : ResultatDeSimulation`. Classe `Calibreur` : `record CibleDOuverture(BrancheTechnique Branche, IReadOnlyList<int> CyclesVises)`, `record CoupleAB(double A, double B, double Erreur)`, `CyclesDOuverture(double a, double b, IReadOnlyList<double> trajectoire, int nombreDeRangs) : IReadOnlyList<int?>`, `ResoudreCoupleAB(CibleDOuverture, IReadOnlyList<double>) : CoupleAB`.
- Produces (tests) : `Joueur.JoueUneDemiHeure(long graine = 1) : IReadOnlyList<(string Id, double InstantSecondes)>`, `Joueur.Rejoue(double secondes, int limite = Assises.PALIERS_LIVRES) : EtatJeu`.

Le nom de classe `Simulateur` dans l'espace de noms `IdlePond.Simulateur` impose d'écrire `IdlePond.Simulateur.Simulateur.Simuler(...)`, ou d'ajouter `using static IdlePond.Simulateur.Simulateur;` dans les tests.

- [ ] **Step 1: Porter les tests qui échouent**

| Source | Classe C# | Particularités |
|---|---|---|
| `tests/simulateur.test.ts` (19) | `SimulateurTests` | titres et politiques renommés (`fractionDeSaturationPourEclore` → `FractionDeSaturationPourRenaitre`) |
| `tests/plancher-de-cadence.test.ts` (5) | `PlancherDeCadenceTests` | le cas `it.fails` « le silence après le dernier succès atteignable ne dépasse pas cinq minutes — PARQUÉ » **passe** aujourd'hui en TypeScript (état de départ constaté le 2026-09-27). Le porter en test **normal** ; on garde le commentaire « PARQUÉ » et on ajoute : `// Porté comme test normal le 2026-09-27 : il passait en TypeScript, l'it.fails était périmé.` |
| `tests/partie-headless.test.ts` (1) | `PartieHeadlessTests` | — |
| `tests/determinisme.test.ts`, cas « deux simulations de même graine sont identiques » | `DeterminismeTests` | `Simuler(3, null, 7)` deux fois ; comparer `Instantane.De(a.Etat).ToString(Formatting.None)` |

- [ ] **Step 2: Lancer les tests pour les voir échouer**

Run: `outils/unity.sh tests EditMode "SimulateurTests|PlancherDeCadenceTests|PartieHeadlessTests"`
Expected: échec de compilation sur `IdlePond.Simulateur`.

- [ ] **Step 3: Porter**

| Source | C# | Points d'attention |
|---|---|---|
| `src/simulateur/simulateur.ts` | `Simulateur.cs` | `for (let reste = intervalle, present = true; reste > 1e-9; present = false)` se traduit **tel quel** en `for (double reste = intervalle; …)` avec un booléen `present` déclaré avant la boucle et remis à `false` en fin d'itération. `for (;;)` → `while (true)`. Les `throw new Error` deviennent `InvalidOperationException`, messages renommés. `benirAuMieux` → `InsufflerAuMieux`, qui parcourt `Insufflations.Toutes` dans l'ordre et compare avec `Reducteur.Insuffler(...)` par référence (`ReferenceEquals`) |
| `src/simulateur/calibreur.ts` | `Calibreur.cs` | `for (let exposant = -6; exposant <= 12; exposant += 0.05)` : garder l'accumulation flottante **à l'identique**, pas un compteur entier, sinon la grille change |
| `tests/joueur.ts` | `Tests/Outils/Joueur.cs` | mêmes boucles, même ordre d'achat, même cadence d'une seconde |

- [ ] **Step 4: Lancer toute la suite**

Run: `outils/unity.sh tests EditMode`
Expected: tous verts. Les simulations de quinze cycles prennent quelques secondes.

- [ ] **Step 5: Commit**

```bash
git add Assets/IdlePond
git commit -m "feat(simulateur): simulateur, calibreur et joueur scripté portés"
```

---

### Task 9: La parité, l'architecture et le lexique

**Files:**
- Create: `Assets/IdlePond/Tests/Outils/SourceCSharp.cs`
- Test: `Assets/IdlePond/Tests/PariteTests.cs`, `Assets/IdlePond/Tests/ArchitectureTests.cs`, `Assets/IdlePond/Tests/LexiqueTests.cs`

**Interfaces:**
- Consumes: `parties.json`, tout le noyau, le simulateur, `Joueur`, `EtatDeTravail`, `Instantane`, `Comparateur`.
- Produces: `SourceCSharp.SansCommentairesNiChaines(string) : string`, `SourceCSharp.Identifiants(string code) : IEnumerable<string>`, `SourceCSharp.Fichiers(string dossier) : IEnumerable<string>`.

- [ ] **Step 1: Écrire `PariteTests`**

Les scénarios C# sont le miroir exact de ceux du générateur (tâche 2).

```csharp
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// La parité avec le TypeScript — spec 2026-09-27 §3. Tant qu'elle est verte, le
    /// portage n'a rien changé à la mécanique. Une parité rouge ne se corrige ni en
    /// relâchant la tolérance, ni en régénérant les références : on cherche l'écart.
    /// </summary>
    public class PariteTests
    {
        static readonly JObject Parties = References.Lire("parties");

        static JArray Instants(string nom) =>
            (JArray)Parties["scenarios"].First(s => (string)s["nom"] == nom)["instants"];

        static void Comparer(string nom, JToken obtenu, JToken attendu) =>
            Comparateur.ComparerATolerance(obtenu, attendu, Comparateur.TOLERANCE_RELATIVE, nom);

        [Test, Description("la séquence de dt de déterminisme rend les états du TypeScript")]
        public void La_sequence_de_dt_rend_les_etats_du_TypeScript()
        {
            var attendus = Instants("sequence");
            var etat = EtatDeTravail.Creer(4242);
            Comparer("sequence[0]", Instantane.De(etat), attendus[0]);
            var sequence = new[] { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 };
            for (var i = 0; i < sequence.Length; i++)
            {
                etat = Reducteur.Tick(etat, sequence[i]);
                Comparer($"sequence[{i + 1}]", Instantane.De(etat), attendus[i + 1]);
            }
        }

        [Test, Description("le joueur scripté rend les états du TypeScript")]
        public void Le_joueur_scripte_rend_les_etats_du_TypeScript()
        {
            var attendus = Instants("joueur");
            var secondes = new[] { 60.0, 600, 1800, 3600 };
            for (var i = 0; i < secondes.Length; i++)
                Comparer($"joueur[{secondes[i]}]", Instantane.De(Joueur.Rejoue(secondes[i])), attendus[i]);
        }

        [Test, Description("trois renaissances rendent les états du TypeScript")]
        public void Trois_renaissances_rendent_les_etats_du_TypeScript()
        {
            var attendus = Instants("renaissances");
            var etat = EtatDeTravail.Creer(777);
            var k = 0;
            Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
            for (var cycle = 0; cycle < 3; cycle++)
            {
                etat = Reducteur.Tick(etat, 3600);
                for (var i = 0; i < 3; i++) etat = Reducteur.Grandir(etat);
                etat = Renaissance.Renaitre(etat);
                Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
                etat = Reducteur.Insuffler(etat, IdlePond.Noyau.Donnees.Insufflations.GLOBALE_ID);
                etat = Reducteur.Insuffler(etat, "insufflation-vairon");
                etat = Reducteur.Creuser(etat);
                etat = Reducteur.Debloquer(etat, "vairon");
                for (var n = 0; n < 20; n++) etat = Reducteur.Ameliorer(etat, "vairon");
                etat = Reducteur.Tick(etat, 600);
                Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
            }
        }

        [Test, Description("quinze cycles simulés rendent l’état du TypeScript")]
        public void Quinze_cycles_simules_rendent_l_etat_du_TypeScript()
        {
            var attendu = Parties["simulation"];
            var resultat = Simuler(15, null, 7);
            Assert.That(resultat.CyclesAcheves, Is.EqualTo((int)attendu["cyclesAcheves"]));
            Comparer("simulation.secondesActives", resultat.SecondesActives, attendu["secondesActives"]);
            Comparer("simulation.secondesEcoulees", resultat.SecondesEcoulees, attendu["secondesEcoulees"]);
            Comparer("simulation", Instantane.De(resultat.Etat), attendu["instant"]);
        }
    }
}
```

- [ ] **Step 2: Écrire l'outil de lecture de sources**

`Assets/IdlePond/Tests/Outils/SourceCSharp.cs` :

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Les tests d'architecture et de lexique portent sur le CODE, pas sur la prose
    /// ni sur les données : commentaires et littéraux de chaîne sont blanchis avant
    /// d'être lus. Les chaînes affichées ont leur propre test (LexiqueTests).
    /// </summary>
    public static class SourceCSharp
    {
        public static IEnumerable<string> Fichiers(string dossier) =>
            Directory.GetFiles(Path.GetFullPath(dossier), "*.cs", SearchOption.AllDirectories).OrderBy(f => f);

        public static string SansCommentairesNiChaines(string source)
        {
            var sortie = new StringBuilder(source.Length);
            var i = 0;
            while (i < source.Length)
            {
                var c = source[i];
                var suivant = i + 1 < source.Length ? source[i + 1] : '\0';
                if (c == '/' && suivant == '/')
                {
                    while (i < source.Length && source[i] != '\n') i++;
                    continue;
                }
                if (c == '/' && suivant == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }
                var verbatim = (c == '@' && suivant == '"') || (c == '$' && suivant == '@') || (c == '@' && suivant == '$');
                if (verbatim || c == '"' || (c == '$' && suivant == '"'))
                {
                    while (source[i] != '"') i++;
                    i++;
                    while (i < source.Length)
                    {
                        if (!verbatim && source[i] == '\\') { i += 2; continue; }
                        if (source[i] == '"')
                        {
                            if (verbatim && i + 1 < source.Length && source[i + 1] == '"') { i += 2; continue; }
                            break;
                        }
                        i++;
                    }
                    i++;
                    sortie.Append("\"\"");
                    continue;
                }
                if (c == '\'')
                {
                    var fin = source.IndexOf('\'', i + (suivant == '\\' ? 3 : 2));
                    if (fin > i && fin - i <= 8) { sortie.Append("' '"); i = fin + 1; continue; }
                }
                sortie.Append(c);
                i++;
            }
            return sortie.ToString();
        }

        static readonly Regex Identifiant = new Regex(@"[\p{L}_][\p{L}\p{Nd}_]*", RegexOptions.CultureInvariant);

        /// Chaque identifiant découpé en mots : `CoutDInsufflation` → cout, d, insufflation ;
        /// `SOUFFLE_BASE` → souffle, base. Minuscules, sans accents.
        public static IEnumerable<string> Mots(string code)
        {
            foreach (Match m in Identifiant.Matches(code))
            {
                foreach (var morceau in Regex.Split(m.Value, @"_|(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})"))
                    if (morceau.Length > 0) yield return SansAccents(morceau.ToLowerInvariant());
            }
        }

        public static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            var sortie = new StringBuilder(decompose.Length);
            foreach (var c in decompose)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sortie.Append(c);
            return sortie.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
```

- [ ] **Step 3: Écrire `ArchitectureTests` et `LexiqueTests`**

`Assets/IdlePond/Tests/ArchitectureTests.cs`, portage de l'esprit de `tests/architecture.test.ts` :

```csharp
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ArchitectureTests
    {
        static readonly string[] Dossiers = { "Assets/IdlePond/Noyau", "Assets/IdlePond/Simulateur" };

        static readonly (string Motif, string Quoi)[] Interdits =
        {
            (@"\bDateTime\s*\.\s*(Now|UtcNow|Today)\b", "horloge système"),
            (@"\bDateTimeOffset\s*\.\s*(Now|UtcNow)\b", "horloge système"),
            (@"\bEnvironment\s*\.\s*TickCount", "horloge système"),
            (@"\bStopwatch\b", "horloge système"),
            (@"\bSystem\s*\.\s*Random\b|\bnew\s+Random\s*\(", "hasard hors de l'état"),
            (@"\bGuid\s*\.\s*NewGuid\b", "hasard hors de l'état"),
            (@"\bUnityEngine\b|\bUnityEditor\b", "moteur dans le noyau"),
            (@"\bFile\s*\.|\bDirectory\s*\.|\bConsole\s*\.", "entrée/sortie dans le noyau"),
        };

        [Test, Description("le noyau contient des fichiers à vérifier")]
        public void Le_noyau_contient_des_fichiers_a_verifier()
        {
            Assert.That(SourceCSharp.Fichiers("Assets/IdlePond/Noyau").Count(), Is.GreaterThan(10));
        }

        [Test, Description("le noyau et le simulateur ne touchent ni horloge, ni hasard, ni moteur, ni fichier")]
        public void Le_noyau_ne_touche_ni_horloge_ni_hasard_ni_moteur_ni_fichier()
        {
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from interdit in Interdits
                          where Regex.IsMatch(code, interdit.Motif)
                          select $"{Path.GetFileName(fichier)} : {interdit.Quoi}").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("aucun état hors du réducteur : pas de champ statique modifiable")]
        public void Aucun_etat_hors_du_reducteur()
        {
            var champModifiable = new Regex(@"^\s*(public|private|internal|protected)?\s*static\s+(?!readonly\b|class\b|partial\b|extern\b)[\w<>,\[\]\s?.()]+?\s+\w+\s*(=[^>]|;)", RegexOptions.Multiline);
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from Match m in champModifiable.Matches(code)
                          where !m.Value.Contains("(")
                          select $"{Path.GetFileName(fichier)} : {m.Value.Trim()}").ToList();
            Assert.That(fautes, Is.Empty);
        }
    }
}
```

`Assets/IdlePond/Tests/LexiqueTests.cs`, portage de `canon.test.ts` lignes 255 à 415 (§3), au lexique complet du Codex :

```csharp
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class LexiqueTests
    {
        static readonly string[] Dossiers = { "Assets/IdlePond/Noyau", "Assets/IdlePond/Simulateur" };

        /// Codex §5, « Les mots morts » : ni en identifiant, ni à l'écran.
        static readonly string[] MotsMorts =
        {
            "foi", "fidele", "fideles", "benediction", "benedictions", "benir", "beni", "eclore", "eclosion", "eclosions",
            "ponte", "pondre", "population", "maturation", "acclimatation", "banc", "bancs", "place", "places",
            "prestige", "rebirth", "gemme", "gemmes", "perle", "perles", "corail", "layer", "layers",
            "zone", "zones", "biome", "biomes", "etage", "etages", "strate", "strates",
        };

        /// Codex §5, « Les mots interdits à l'écran seulement ».
        static readonly Regex InterditsALEcran = new Regex(@"\b(paliers?|assises?|couches?|zones?|[ée]tages?|strates?|biomes?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static readonly Regex MotsMortsALEcran = new Regex(
            @"\b(foi|fid[èe]les?|b[ée]n[ée]dictions?|b[ée]nir|b[ée]ni[est]?|[ée]clore|ponte|pondre|population|maturation|acclimatation|prestige|rebirth|gemmes?|perles?|corail)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// Codex §7 : « bénédiction » survit une fois, comme terme de fiction.
        const string ExceptionDeFiction = "la bénédiction de l’esprit";

        [Test, Description("aucun mot mort dans un identifiant du noyau ou du simulateur")]
        public void Aucun_mot_mort_dans_un_identifiant()
        {
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from mot in SourceCSharp.Mots(code).Distinct()
                          where MotsMorts.Contains(mot)
                          select $"{Path.GetFileName(fichier)} : {mot}").Distinct().ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("aucun terme de couche ni mot mort ne sort dans un texte affiché")]
        public void Aucun_terme_de_couche_ni_mot_mort_dans_un_texte_affiche()
        {
            var fautes = Textes.TousLesTextesAffiches()
                .Where(t => t.Texte != null)
                .Where(t => InterditsALEcran.IsMatch(t.Texte)
                            || MotsMortsALEcran.IsMatch(t.Texte.Replace(ExceptionDeFiction, "")))
                .Select(t => $"{t.Ou} : « {t.Texte} »").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("les identifiants de succès figés par le Codex §7 sont intacts")]
        public void Les_identifiants_figes_sont_intacts()
        {
            var ids = RegistreDesSucces.Tous.Select(s => s.Id).ToList();
            Assert.That(ids, Does.Contain("franchissement-premiere-eclosion"));
            Assert.That(ids, Does.Contain("franchissement-deuxieme-eclosion"));
            Assert.That(ids, Does.Contain("franchissement-troisieme-eclosion"));
        }
    }
}
```

Les autres tests de la section §3 de `canon.test.ts` (« la maturation ne survit nulle part dans le noyau », « un seul canal de revenu », « le modèle à population ne subsiste pas », « le modèle mort ne subsiste nulle part ») se portent dans `LexiqueTests` : même intention, appliquée à `SourceCSharp.SansCommentairesNiChaines` des `.cs` du noyau. Relire chaque liste de motifs de la source et la traduire en mots C# (`PascalCase` découpé par `SourceCSharp.Mots`).

- [ ] **Step 4: Lancer la suite complète**

Run: `outils/unity.sh tests EditMode`
Expected: tous verts, dont `PariteTests` (4). Si `Aucun_mot_mort_dans_un_identifiant` échoue sur un identifiant légitime (un mot qui contient « place » par hasard, par exemple), le renommer ; on n'ajoute pas d'exception.

- [ ] **Step 5: Commit**

```bash
git add Assets/IdlePond
git commit -m "test(parite): parité avec le TypeScript, architecture et lexique du Codex"
```

---

### Task 10: Clôture du plan 1

**Files:**
- Modify: `README.md` (section « Commandes »)

- [ ] **Step 1: Documenter la commande Unity**

Dans `README.md`, sous le bloc `sh` de « Commandes », ajouter :

````markdown
### Unity (portage en cours, spec `docs/superpowers/specs/2026-09-27-portage-unity-design.md`)

L'éditeur Unity 6000.6.3f1 doit être **fermé** sur ce projet :

```sh
outils/unity.sh tests EditMode            # noyau C#, simulateur, parité avec le TypeScript
outils/unity.sh tests EditMode PariteTests
npx tsx tests/parite/generer-references.ts # ne se relance pas : les références sont figées
```
````

- [ ] **Step 2: Vérification finale**

Run: `outils/unity.sh tests EditMode`
Expected: tous verts ; noter le total dans le message de commit.

Run: `npx vitest run 2>&1 | tail -3`
Expected: toujours `196 passed | 1 failed` : le TypeScript n'a pas été touché, hors `tests/parite/`.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs(readme): la commande de tests Unity et la parité"
```

À la fin de ce plan, on écrit le **plan 2** : `Partie`, persistance, hors-ligne, boucle, scènes générées, UI Toolkit, revue visuelle et archivage du web. Il s'appuie sur les noms C# réellement produits ici.
