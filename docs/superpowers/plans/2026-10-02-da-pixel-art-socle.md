# La DA pixel art, le socle et la Noue — plan d'implémentation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remplacer la coupe au trait de la Mare par une scène en pixel art (URP 2D, rendu en basse résolution, vraies lumières 2D), avec un héros composé qui mue, et des sprites provisoires générés par script que de vrais dessins remplaceront sans code.

**Architecture:** La projection pure `VueDeScene` reste la seule entrée du dessin. Des données pures (`Gabarits`, `RegistreDArt`, `Cadrage`, `Nage`) fixent formats, couleurs, lumière et trajectoires ; des classes de rendu minces (`RenduPixel`, `Decor`, `Eclairage`, `Nageurs`, `Voile`, `HerosEnPixels`) les posent dans une scène rendue par une caméra dédiée dans une `RenderTexture` à facteur entier, affichée en fond de `#scene`. Côté éditeur, un générateur dessine les PNG provisoires, un post-processeur impose l'import, et un `CatalogueDArt` relie les sprites à la scène.

**Tech Stack:** Unity 6000.6.3f1 · C# 9 · URP 17.6.0 (moteur de rendu 2D, `Light2D`) · UI Toolkit · NUnit (Unity Test Framework) · tests en batch par `outils/unity.sh`.

**Spec:** `docs/superpowers/specs/2026-10-02-da-pixel-art-socle-design.md`

## Global Constraints

- Unity **6000.6.3f1** ; URP **17.6.0** (`com.unity.render-pipelines.universal`, livré avec l'éditeur).
- **1 unité Unity = 1 pixel** du dessin ; toute position posée dans la scène est un **entier**.
- Le facteur d'agrandissement **k est un entier ≥ 1** : `k = max(1, round(largeur px / 240))`, arrondi « au plus loin de zéro ».
- Largeur visée **240 px** ; bande de palier **56 px** ; corps du héros **21 / 28 / 42 / 63 px** ; seuils de stade **4 / 16 / 256**.
- **Lexique (LexiqueTests, ne pas contourner)** : dans les **identifiants** C# de `Assets/IdlePond/Jeu/`, jamais `banc(s)`, `layer(s)`, `place(s)`, `zone(s)`, `etage(s)`, `strate(s)`, `biome(s)` (ni aucun mot mort du Codex §5) — donc **aucune API Unity contenant « Layer »** dans `Jeu/` (`sortingLayerName`, `SortingLayer`, `gameObject.layer`, `targetSortingLayers`). Dans les **chaînes littérales** de `Jeu/`, jamais `palier(s)`, `assise(s)`, `couche(s)`, `zone(s)`, `étage(s)`, `strate(s)`, `biome(s)`. Le dossier `Editeur/` n'est pas balayé.
- **Architecture (ArchitectureTests)** : dans `Jeu/`, aucun `UnityEngine.Random`, `System.Random`, `DateTime.Now` ; le hasard visuel vient d'un hachage d'entiers déterministe.
- Les fichiers **purs** (`VueDeScene.cs`, `Palette.cs`, `Gabarits.cs`, `RegistreDArt.cs`, `Cadrage.cs`, `Nage.cs`) n'ont **aucun** `using UnityEngine`.
- Les scènes, l'asset URP, le catalogue et les sprites provisoires sont **produits par script** et versionnés ; rien ne se règle à la main.
- Commentaires en français, qui disent **pourquoi**. Un commit par tâche au minimum, message en français au format du dépôt, terminé par :
  ```
  Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01SDMTjxnPELsi5RZDuUGsmM
  ```
- **L'éditeur Unity doit être fermé** pendant chaque commande `outils/unity.sh`.

## Écarts assumés par rapport à la spec

| Spec | Plan | Pourquoi |
|---|---|---|
| Sorting Layers nommées (§4) | `sortingOrder` par constantes, dans `OrdreDeRendu` | « layer » est un mot mort : aucune API `*Layer*` dans `Jeu/` |
| Classe `Bancs` (§6) | `Nageurs` | « banc » est un mot mort |
| `Art/Assises/<id>/` (§5) | `Art/Fonds/<id>/` | le chemin vit dans l'éditeur, mais le même mot finirait dans des chaînes de `Jeu/` |
| Planches multi-images découpées (§5) | **un PNG par image** (`corps-s2-i0.png`…) | aucune découpe d'import à maintenir ; un export d'Aseprite image par image fait l'affaire |
| `StadePrecedent` dans la vue (§6) | la scène compare au stade **déjà affiché** (`VueDeScene.EstUneMue`) | une projection pure d'un seul état ne connaît pas le précédent |
| Étiquette `userData` (§5) | étiquette **et empreinte** SHA-1 du PNG produit | un vrai dessin déposé à la place d'un provisoire garde le `.meta` du provisoire : seule l'empreinte le distingue |
| Bancs de vairon, loche, épinoche (§1) | vairon et loche | l'épinoche vit au palier 6 (assise II) : hors de la Noue livrée |
| Texture de ⌈largeur/k⌉ pixels (§2) | **⌊largeur/k⌋** (`Cadrage.Dimensions`) | ⌈⌉ ferait déborder la texture de #scene ; le reste (moins de k pixels d'écran) prend le fond de #scene |
| Captures de contrôle plein écran, 1080×1920 et 1920×1080 (§7) | captures de la **texture de #scene**, agrandie de k, à 1080×768 et 1600×430 | la scène n'occupe que 40 % de l'écran ; la capture d'écran réelle est vide en batch (essayée : `CaptureScreenshotAsTexture` après `WaitForEndOfFrame`), le contrôle de l'affichage dans #scene reste manuel |

## Review Focus

1. **L'écran tourne ou la fenêtre change de taille en jeu** → la texture est recréée au nouveau k et l'ancienne est libérée (Tâche 4, `La_texture_suit_la_taille_et_l_ancienne_est_liberee`).
2. **`#scene` minuscule ou pas encore mis en page (largeur 0, fenêtre réduite)** → aucune texture, aucune exception ; sous 240 px, k = 1 (Tâche 4, `CadrageTests`).
3. **Un palier d'une assise sans dessin** (contenu au-delà de la Noue, partie de simulateur à 62 paliers) → décor de la Noue, plus sombre, sans erreur (Tâche 2, `Une_assise_sans_dessin_emprunte_la_Noue_assombrie` ; Tâche 3, `Un_catalogue_incomplet_ne_leve_jamais`).
4. **Niveau du héros à 0, négatif ou `int.MaxValue`** → stade borné à 0..3 (Tâche 2, `Le_stade_est_borne`).
5. **Un vrai dessin manquant ou un catalogue vide** → la scène ne dessine rien à cet endroit, sans `NullReferenceException` (Tâche 3, `Un_catalogue_incomplet_ne_leve_jamais`).

---

## Structure des fichiers

```
Assets/IdlePond/
  Rendu/                                   (nouveau, produit par ConfigurationURP)
    PipelineIdlePond.asset, Rendu2D.asset
  Art/                                     (nouveau, produit par GenerateurDeSprites)
    Catalogue.asset
    Heros/corps-s{0..3}-i{0..3}.png
    Heros/marques/noue-s{0..3}.png
    Especes/{vairon,loche}-i{0,1}.png
    Fonds/noue/{fond,rayons,berge}.png
    Eau/{voile,eclat}.png
  Jeu/Scene/
    VueDeScene.cs        modifié : Stade remplace Echelle ; EstUneMue
    Palette.cs           élagué (Tâche 8)
    Gabarits.cs          nouveau, pur : formats, stades, ancrages
    RegistreDArt.cs      nouveau, pur : une assise → décor, marque, lumière
    Cadrage.cs           nouveau, pur : k, taille de texture, champ de la caméra
    Nage.cs              nouveau, pur : trajectoires des nageurs
    CatalogueDArt.cs     nouveau : ScriptableObject des sprites
    OrdreDeRendu.cs      nouveau : l'ordre d'affichage
    Briques.cs           nouveau : poser un sprite, paver, vider, couleur
    RenduPixel.cs        nouveau : caméra dédiée et RenderTexture
    Decor.cs, Eclairage.cs, Nageurs.cs, Voile.cs, HerosEnPixels.cs   nouveaux
    SceneDeLaMare.cs     réécrit : chef d'orchestre
  Jeu/UI/Mare.uss        .scene reçoit un fond abysse
  Editeur/
    ConfigurationURP.cs  nouveau
    Chemins.cs, Toile.cs, GenerateurDeSprites.cs, ImportDArt.cs, Catalogue.cs   nouveaux
    GenerateurDeScenes.cs modifié
  Tests/
    RenduURPTests.cs, GabaritsTests.cs, GenerateurDeSpritesTests.cs, CadrageTests.cs, NageTests.cs   nouveaux
    VueDeSceneTests.cs   modifié
  TestsDeJeu/
    ScenePixelTests.cs, CapturesDeControle.cs   nouveaux
outils/unity.sh          commande `captures`
docs/da/gabarits.md      nouveau
```

---

### Task 1: URP 2D, par script

**Files:**
- Modify: `Packages/manifest.json`
- Modify: `Assets/IdlePond/Editeur/IdlePond.Editeur.asmdef`, `Assets/IdlePond/Jeu/IdlePond.Jeu.asmdef`, `Assets/IdlePond/Tests/IdlePond.Tests.asmdef`, `Assets/IdlePond/TestsDeJeu/IdlePond.TestsDeJeu.asmdef`
- Create: `Assets/IdlePond/Editeur/ConfigurationURP.cs`
- Modify: `Assets/IdlePond/Editeur/GenerateurDeScenes.cs`
- Test: `Assets/IdlePond/Tests/RenduURPTests.cs`

**Interfaces:**
- Produces: `IdlePond.Editeur.ConfigurationURP.Appliquer()` (idempotent), constantes `ConfigurationURP.PIPELINE = "Assets/IdlePond/Rendu/PipelineIdlePond.asset"`, `ConfigurationURP.RENDU_2D = "Assets/IdlePond/Rendu/Rendu2D.asset"`. Les asmdefs `Jeu`, `Tests`, `TestsDeJeu`, `Editeur` voient `UnityEngine.Rendering.Universal` ; `Tests` voit `IdlePond.Editeur`.

- [ ] **Step 1: Ajouter le paquet et les références d'assembly**

Dans `Packages/manifest.json`, ajouter dans `dependencies` (ordre alphabétique conservé) :

```json
    "com.unity.render-pipelines.universal": "17.6.0",
```

Dans **chacun** des quatre `.asmdef` listés, ajouter au tableau `references` :

```json
"Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Core.Runtime"
```

Dans `IdlePond.Tests.asmdef`, ajouter aussi `"IdlePond.Editeur"` au tableau `references`.

- [ ] **Step 2: Écrire le test qui échoue**

`Assets/IdlePond/Tests/RenduURPTests.cs` :

```csharp
using IdlePond.Editeur;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Spec DA §2 : la scène est rendue par URP et son moteur de rendu 2D — c'est lui qui
    /// porte les `Light2D`. L'asset est produit par `ConfigurationURP`, jamais réglé à la main.
    /// </summary>
    public class RenduURPTests
    {
        [Test, Description("le pipeline du projet est l'asset URP d'IdlePond, avec le moteur de rendu 2D")]
        public void Le_pipeline_du_projet_est_URP_avec_le_rendu_2D()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            Assert.That(pipeline, Is.Not.Null, "le projet n'est pas sous URP : lancer « IdlePond ▸ Générer les scènes »");
            Assert.That(AssetDatabase.GetAssetPath(pipeline), Is.EqualTo(ConfigurationURP.PIPELINE));

            var rendus = new SerializedObject(pipeline).FindProperty("m_RendererDataList");
            Assert.That(rendus.arraySize, Is.GreaterThan(0));
            Assert.That(rendus.GetArrayElementAtIndex(0).objectReferenceValue, Is.InstanceOf<Renderer2DData>());
        }

        [Test, Description("aucun niveau de qualité ne remplace le pipeline par un autre")]
        public void Aucun_niveau_de_qualite_ne_remplace_le_pipeline()
        {
            for (var i = 0; i < QualitySettings.count; i++)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                Assert.That(asset == null || asset == GraphicsSettings.defaultRenderPipeline, Is.True, $"niveau {i}");
            }
        }
    }
}
```

- [ ] **Step 3: Lancer le test et le voir échouer**

Run: `outils/unity.sh tests EditMode RenduURPTests`
Expected: la compilation échoue (`ConfigurationURP` n'existe pas) — « Aucun résultat : la compilation a probablement échoué » avec `error CS0246` sur `ConfigurationURP`. (Le premier lancement importe URP : plusieurs minutes.)

- [ ] **Step 4: Écrire `ConfigurationURP`**

`Assets/IdlePond/Editeur/ConfigurationURP.cs` :

```csharp
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — le pipeline de rendu, par script (spec DA §6). URP et son moteur de rendu
    /// 2D portent les `Light2D` dont la scène a besoin. Comme les scènes, l'asset est produit
    /// ici et versionné : relancé, rien ne change.
    /// </summary>
    public static class ConfigurationURP
    {
        public const string DOSSIER = "Assets/IdlePond/Rendu";
        public const string PIPELINE = DOSSIER + "/PipelineIdlePond.asset";
        public const string RENDU_2D = DOSSIER + "/Rendu2D.asset";

        [MenuItem("IdlePond/Configurer le rendu (URP 2D)")]
        public static void Configurer()
        {
            try
            {
                Appliquer();
                Debug.Log("IdlePond : rendu URP 2D configuré.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static void Appliquer()
        {
            var pipeline = CreerOuCharger();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Chaque niveau de qualité peut surcharger le pipeline : on les aligne tous, sinon
            // un téléphone en qualité basse retomberait sur le pipeline intégré.
            var courant = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(courant, false);
            AssetDatabase.SaveAssets();
        }

        static UniversalRenderPipelineAsset CreerOuCharger()
        {
            Directory.CreateDirectory(DOSSIER);
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PIPELINE);
            if (pipeline != null) return pipeline;
            var rendu = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RENDU_2D) ?? CreerLeRendu2D();
            pipeline = UniversalRenderPipelineAsset.Create(rendu);
            AssetDatabase.CreateAsset(pipeline, PIPELINE);
            return pipeline;
        }

        /// <summary>
        /// Le menu « Create ▸ Rendering ▸ URP 2D Renderer » passe par une méthode interne
        /// d'URP qui remplit les ressources du moteur de rendu (shaders, données de
        /// post-traitement). On l'appelle telle quelle plutôt que d'en recopier le contenu,
        /// qui change d'une version d'URP à l'autre.
        /// </summary>
        static Renderer2DData CreerLeRendu2D()
        {
            var menus = Type.GetType("UnityEditor.Rendering.Universal.Renderer2DMenus, Unity.RenderPipelines.Universal.Editor", true);
            var creer = menus.GetMethod("CreateRendererAsset", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new MissingMethodException("Renderer2DMenus.CreateRendererAsset (URP a changé de forme)");
            return (Renderer2DData)creer.Invoke(null, new object[] { RENDU_2D, RendererType._2DRenderer, false, "Renderer" });
        }
    }
}
```

- [ ] **Step 5: Le générateur de scènes applique la configuration**

Dans `Assets/IdlePond/Editeur/GenerateurDeScenes.cs`, méthode `Generer()`, juste après `AssetDatabase.Refresh();` :

```csharp
                ConfigurationURP.Appliquer();
```

- [ ] **Step 6: Produire l'asset et relancer les tests**

Run: `outils/unity.sh methode IdlePond.Editeur.GenerateurDeScenes.Generer`
Expected: code de sortie 0 ; `Assets/IdlePond/Rendu/PipelineIdlePond.asset` et `Rendu2D.asset` existent ; `ProjectSettings/GraphicsSettings.asset` et `QualitySettings.asset` modifiés.

Run: `outils/unity.sh tests EditMode RenduURPTests`
Expected: `total 2 · réussis 2`.

Si `Renderer2DMenus.CreateRendererAsset` a changé de signature (exception `MissingMethodException` ou `TargetParameterCountException`), lire `Library/PackageCache/com.unity.render-pipelines.universal*/Editor/2D/Renderer2DMenus.cs` et ajuster les arguments ; ne pas remplacer par `ScriptableObject.CreateInstance<Renderer2DData>()`, qui laisse le moteur de rendu sans ressources.

- [ ] **Step 7: Le reste du projet passe sous URP**

Run: `outils/unity.sh tests EditMode` puis `outils/unity.sh tests PlayMode`
Expected: EditMode `réussis 273` (271 + 2), PlayMode `réussis 1`. La coupe au trait actuelle n'est pas testée visuellement ; elle est remplacée à la Tâche 4.

- [ ] **Step 8: Commit**

```bash
git add Packages/manifest.json Packages/packages-lock.json Assets/IdlePond/Rendu* Assets/IdlePond/Editeur Assets/IdlePond/Jeu/IdlePond.Jeu.asmdef Assets/IdlePond/Tests Assets/IdlePond/TestsDeJeu/IdlePond.TestsDeJeu.asmdef ProjectSettings
git commit -m "feat(rendu): URP 2D configuré par script, appliqué par le générateur de scènes"
```

---

### Task 2: Les données pures — stades, gabarits, registre d'art

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/Gabarits.cs`, `Assets/IdlePond/Jeu/Scene/RegistreDArt.cs`
- Modify: `Assets/IdlePond/Jeu/Scene/VueDeScene.cs`, `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs` (une ligne, provisoire)
- Test: `Assets/IdlePond/Tests/VueDeSceneTests.cs` (modifié), `Assets/IdlePond/Tests/GabaritsTests.cs` (nouveau)

**Interfaces:**
- Produces (namespace `IdlePond.Jeu.Scene`) :
  - `enum AncrageId { Branchies, Dos, Flanc, Ventre, Tete, Queue }`
  - `sealed record Cadre(int Largeur, int Hauteur)` ; `sealed record Pixel(int X, int Y)`
  - `static class Gabarits` : constantes `LARGEUR_VISEE=240`, `HAUTEUR_DE_BANDE=56`, `LARGEUR_DU_DECOR=384`, `GAUCHE_DU_DECOR=-72`, `IMAGES_DE_NAGE=4`, `IMAGES_D_ESPECE=2`, `LARGEUR_DE_FOND=32`, `LARGEUR_DE_BERGE=256`, `HAUTEUR_DES_RAYONS=112`, `COTE_DU_VOILE=8`, `X_DU_HEROS=150` ; `LONGUEURS_DU_CORPS`, `SEUILS_DE_STADE` (`IReadOnlyList<int>`) ; `int StadeDuNiveau(int)`, `Cadre CadreDUnPoisson(int longueur)`, `Cadre CadreDuCorps(int stade)`, `int LongueurDEspece(int rang)`, `Cadre CadreDEspece(int rang)`, `Tronc TroncDUnPoisson(int longueur)` avec `sealed record Tronc(double Cx, double Cy, double A, double B)` et `bool Contient(double x, double y)`, `Pixel Ancrage(int stade, AncrageId)`.
  - `sealed record LumiereEmise(int Couleur, double Intensite, int Rayon)` ; `sealed record MarqueDArt(AncrageId Ancrage, IReadOnlyList<int> Couleurs, int? Teinte, LumiereEmise Lumiere)` ; `sealed record DecorDArt(string Assise, IReadOnlyList<double> Lumieres, int TeinteDeLumiere, IReadOnlyList<int> Eau, int Vase, IReadOnlyList<int> Racines, int Rayon, bool Berge, int Voile)` ; `sealed record CouleursDePoisson(int Corps, int Dos, int Ventre)`.
  - `static class RegistreDArt` : `CONTOUR`, `BLANC`, `LUMIERE_AMBIANTE`, `HEROS`, `ASSISES_DESSINEES`, `CouleursDEspece(int rang)`, `DecorDe(string)`, `MarqueDe(string)` (null si aucune), `LumiereDuPalier(int index)`, `PaletteDuHeros()`, `PaletteDesEspeces()`, `PaletteDuDecor(string)`.
  - `VueDuHeros(int Niveau, int Stade, IReadOnlyList<string> Couches)` ; `static bool VueDeScene.EstUneMue(int? stadeAffiche, int nouveauStade)`.

- [ ] **Step 1: Écrire les tests qui échouent**

Dans `Assets/IdlePond/Tests/VueDeSceneTests.cs`, **remplacer** les deux tests `Le_heros_porte_son_niveau_son_echelle_et_ses_couches` et `L_echelle_vaut_1_plus_un_quart_de_log2_du_niveau` par :

```csharp
        [Test, Description("DA §3 — la vue : le héros porte son niveau, son stade et ses couches")]
        public void Le_heros_porte_son_niveau_son_stade_et_ses_couches()
        {
            var base_ = EtatDeTravail.Creer();
            var etat = base_ with
            {
                Cycle = base_.Cycle with { NiveauDuHeros = 16 },
                Permanent = base_.Permanent with { Couches = new[] { "noue" } },
            };
            var heros = VueDeScene.Depuis(etat).Heros;
            Assert.That(heros.Niveau, Is.EqualTo(16));
            Assert.That(heros.Stade, Is.EqualTo(2));
            Assert.That(heros.Couches, Is.EqualTo(new[] { "noue" }));
        }

        [Test, Description("DA §3 — le stade change aux niveaux 4, 16 et 256")]
        public void Le_stade_change_aux_niveaux_4_16_et_256()
        {
            var attendus = new[] { (1, 0), (3, 0), (4, 1), (15, 1), (16, 2), (255, 2), (256, 3), (100000, 3) };
            foreach (var (niveau, stade) in attendus)
                Assert.That(Gabarits.StadeDuNiveau(niveau), Is.EqualTo(stade), $"niveau {niveau}");
        }

        [Test, Description("DA §3 — le stade est borné à 0..3, même pour un niveau absurde")]
        public void Le_stade_est_borne()
        {
            Assert.That(Gabarits.StadeDuNiveau(0), Is.EqualTo(0));
            Assert.That(Gabarits.StadeDuNiveau(-5), Is.EqualTo(0));
            Assert.That(Gabarits.StadeDuNiveau(int.MaxValue), Is.EqualTo(3));
        }

        [Test, Description("DA §3 — après une renaissance : stade 0, et la marque reste")]
        public void Apres_une_renaissance_le_heros_redevient_petit_et_garde_ses_marques()
        {
            var etat = Reducteur.Tick(EtatDeTravail.Creer(777), 3600);
            etat = etat with
            {
                Cycle = etat.Cycle with { NiveauDuHeros = 20 },
                Permanent = etat.Permanent with { Couches = new[] { "noue" } },
            };
            Assert.That(VueDeScene.Depuis(etat).Heros.Stade, Is.EqualTo(2));
            var apres = VueDeScene.Depuis(Renaissance.Renaitre(etat)).Heros;
            Assert.That(apres.Stade, Is.EqualTo(0));
            Assert.That(apres.Couches, Has.Member("noue"));
        }

        [Test, Description("DA §3 — une mue : le stade monte ; jamais au premier dessin ni en rapetissant")]
        public void Une_mue_est_un_stade_qui_monte()
        {
            Assert.That(VueDeScene.EstUneMue(null, 2), Is.False, "une partie chargée n'a pas mué");
            Assert.That(VueDeScene.EstUneMue(0, 1), Is.True);
            Assert.That(VueDeScene.EstUneMue(1, 3), Is.True);
            Assert.That(VueDeScene.EstUneMue(2, 2), Is.False);
            Assert.That(VueDeScene.EstUneMue(3, 0), Is.False, "la renaissance n'est pas une mue");
        }
```

Dans le dernier test du fichier (`VueDeScene_et_Palette_ne_referencent_pas_UnityEngine`), étendre la liste :

```csharp
            foreach (var f in new[]
                     {
                         "Assets/IdlePond/Jeu/Scene/VueDeScene.cs", "Assets/IdlePond/Jeu/Scene/Palette.cs",
                         "Assets/IdlePond/Jeu/Scene/Gabarits.cs", "Assets/IdlePond/Jeu/Scene/RegistreDArt.cs",
                     })
```

Créer `Assets/IdlePond/Tests/GabaritsTests.cs` (les tests sur fichiers s'y ajouteront à la Tâche 3) :

```csharp
using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les gabarits du dessin (spec DA §5) : ce qu'un sprite, provisoire ou vrai, doit
    /// respecter pour prendre sa place sans code.
    /// </summary>
    public class GabaritsTests
    {
        [Test, Description("le cadre d'un corps : sa longueur plus le contour, et une hauteur impaire")]
        public void Le_cadre_d_un_corps()
        {
            Assert.That(Gabarits.CadreDuCorps(0), Is.EqualTo(new Cadre(23, 15)));
            Assert.That(Gabarits.CadreDuCorps(1), Is.EqualTo(new Cadre(30, 17)));
            Assert.That(Gabarits.CadreDuCorps(2), Is.EqualTo(new Cadre(44, 23)));
            Assert.That(Gabarits.CadreDuCorps(3), Is.EqualTo(new Cadre(65, 31)));
        }

        [Test, Description("une espèce mesure de 7 à 12 px selon son rang")]
        public void Une_espece_mesure_de_7_a_12_px()
        {
            Assert.That(Gabarits.LongueurDEspece(0), Is.EqualTo(7));
            Assert.That(Gabarits.LongueurDEspece(20), Is.EqualTo(12));
            Assert.That(Enumerable.Range(0, 21).Select(Gabarits.LongueurDEspece).All(l => l >= 7 && l <= 12), Is.True);
        }

        [Test, Description("chaque ancrage tombe dans le tronc, à chaque stade")]
        public void Chaque_ancrage_tombe_dans_le_tronc()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                var tronc = Gabarits.TroncDUnPoisson(Gabarits.LONGUEURS_DU_CORPS[stade]);
                foreach (AncrageId ancrage in System.Enum.GetValues(typeof(AncrageId)))
                {
                    var p = Gabarits.Ancrage(stade, ancrage);
                    Assert.That(tronc.Contient(p.X, p.Y), Is.True, $"stade {stade}, {ancrage} en ({p.X}, {p.Y})");
                }
            }
        }

        [Test, Description("la Noue : six paliers éclairés, la lumière baisse, et ne dépasse jamais 1 avec l'ambiante")]
        public void La_lumiere_de_la_Noue_baisse_et_reste_sous_1()
        {
            var noue = Assises.Toutes[0];
            var lumieres = Enumerable.Range(noue.IndexPremierPalier, noue.NombreDePaliers).Select(RegistreDArt.LumiereDuPalier).ToList();
            for (var i = 1; i < lumieres.Count; i++) Assert.That(lumieres[i], Is.LessThan(lumieres[i - 1]));
            Assert.That(lumieres.All(l => l > 0 && l + RegistreDArt.LUMIERE_AMBIANTE <= 1.0), Is.True);
        }

        [Test, Description("une assise sans dessin emprunte le décor de la Noue, plus sombre, sans erreur")]
        public void Une_assise_sans_dessin_emprunte_la_Noue_assombrie()
        {
            var profonde = Assises.Toutes[2].Id;
            var decor = RegistreDArt.DecorDe(profonde);
            Assert.That(decor.Assise, Is.EqualTo(profonde));
            Assert.That(decor.Berge, Is.False);
            Assert.That(RegistreDArt.LumiereDuPalier(Assises.Toutes[2].IndexPremierPalier),
                Is.LessThan(RegistreDArt.LumiereDuPalier(Assises.Toutes[0].IndexPremierPalier + 5)));
            Assert.That(() => RegistreDArt.LumiereDuPalier(61), Throws.Nothing);
            Assert.That(RegistreDArt.MarqueDe(profonde), Is.Null);
            Assert.That(RegistreDArt.MarqueDe(null), Is.Null);
        }

        [Test, Description("chaque assise dessinée a sa marque et sa courbe de lumière")]
        public void Chaque_assise_dessinee_a_sa_marque_et_sa_lumiere()
        {
            Assert.That(RegistreDArt.ASSISES_DESSINEES, Is.EqualTo(new[] { "noue" }));
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                Assert.That(RegistreDArt.MarqueDe(assise), Is.Not.Null, assise);
                Assert.That(RegistreDArt.DecorDe(assise).Lumieres.Count,
                    Is.EqualTo(Assises.Toutes.First(a => a.Id == assise).NombreDePaliers), assise);
            }
        }
    }
}
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests EditMode VueDeSceneTests`
Expected: échec de compilation (`Gabarits`, `RegistreDArt`, `Stade`, `EstUneMue` inconnus).

- [ ] **Step 3: Écrire `Gabarits.cs`**

```csharp
using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Les six points d'ancrage du corps (GDD §10.3, §15.3) : un par marque possible.
    public enum AncrageId { Branchies, Dos, Flanc, Ventre, Tete, Queue }

    public sealed record Cadre(int Largeur, int Hauteur);

    /// Un pixel d'un sprite, origine en bas à gauche — celle des textures Unity.
    public sealed record Pixel(int X, int Y);

    /// <summary>
    /// IdlePond — les gabarits du dessin (spec DA §3, §4, §5). Données pures : le générateur
    /// de provisoires, les tests et le cahier des charges du vrai dessin les lisent tous les
    /// trois. Changer une valeur ici, c'est changer ce qu'un dessinateur doit livrer.
    ///
    /// Un poisson regarde à droite. Son cadre fait sa longueur plus un pixel de contour de
    /// chaque côté, et une hauteur impaire : le tronc a une ligne médiane exacte.
    /// </summary>
    public static class Gabarits
    {
        public const int LARGEUR_VISEE = 240;
        public const int HAUTEUR_DE_BANDE = 56;
        /// Le décor déborde de la largeur visée des deux côtés : un écran plus large que 240
        /// (k arrondi vers le bas) en montre davantage, jamais un vide.
        public const int LARGEUR_DU_DECOR = 384;
        public const int GAUCHE_DU_DECOR = -72;
        public const int IMAGES_DE_NAGE = 4;
        public const int IMAGES_D_ESPECE = 2;
        public const int LARGEUR_DE_FOND = 32;
        public const int LARGEUR_DE_BERGE = 256;
        public const int HAUTEUR_DES_RAYONS = 112;
        public const int COTE_DU_VOILE = 8;
        /// Le milieu du héros, en x : à droite du centre, pour laisser nager les autres.
        public const int X_DU_HEROS = 150;

        /// Spec DA §3 : quatre tailles dessinées, pas un agrandissement — le pixel art ne
        /// supporte pas les facteurs non entiers.
        public static readonly IReadOnlyList<int> LONGUEURS_DU_CORPS = new[] { 21, 28, 42, 63 };
        public static readonly IReadOnlyList<int> SEUILS_DE_STADE = new[] { 4, 16, 256 };

        public static int StadeDuNiveau(int niveau)
        {
            var stade = 0;
            foreach (var seuil in SEUILS_DE_STADE) if (niveau >= seuil) stade += 1;
            return stade;
        }

        public static Cadre CadreDUnPoisson(int longueur) =>
            new Cadre(longueur + 2, 2 * (int)Math.Ceiling(longueur * 0.2) + 5);

        public static Cadre CadreDuCorps(int stade) => CadreDUnPoisson(LONGUEURS_DU_CORPS[stade]);

        /// De 7 px (rang 0) à 12 px (rang 20) : une espèce plus profonde est un peu plus grande.
        public static int LongueurDEspece(int rang) => 7 + Math.Min(5, Math.Max(0, rang) / 4);

        public static Cadre CadreDEspece(int rang) => CadreDUnPoisson(LongueurDEspece(rang));

        /// Le tronc : une ellipse. Il porte les ancrages et ne bouge pas d'une image de nage
        /// à l'autre — seule la queue bat (spec DA §3).
        public sealed record Tronc(double Cx, double Cy, double A, double B)
        {
            public bool Contient(double x, double y)
            {
                var u = (x - Cx) / A;
                var v = (y - Cy) / B;
                return u * u + v * v <= 1;
            }
        }

        public static Tronc TroncDUnPoisson(int longueur)
        {
            var cadre = CadreDUnPoisson(longueur);
            return new Tronc(1 + 0.65 * longueur, (cadre.Hauteur - 1) / 2.0, 0.35 * longueur, 0.2 * longueur);
        }

        public static Pixel Ancrage(int stade, AncrageId ancrage)
        {
            var longueur = LONGUEURS_DU_CORPS[stade];
            var t = TroncDUnPoisson(longueur);
            double x = t.Cx, y = t.Cy;
            switch (ancrage)
            {
                case AncrageId.Branchies: x += 0.18 * longueur; break;
                case AncrageId.Tete: x += 0.26 * longueur; break;
                case AncrageId.Dos: y += 0.6 * t.B; break;
                case AncrageId.Ventre: y -= 0.6 * t.B; break;
                case AncrageId.Flanc: x -= 0.1 * longueur; break;
                case AncrageId.Queue: x -= 0.26 * longueur; break;
            }
            return new Pixel((int)Math.Round(x), (int)Math.Round(y));
        }
    }
}
```

- [ ] **Step 4: Écrire `RegistreDArt.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    public sealed record LumiereEmise(int Couleur, double Intensite, int Rayon);

    /// Ce qu'une assise laisse sur le corps (GDD §15.1) : un calque posé à un ancrage, et en
    /// option une teinte de tout le corps (le rouge de la lave) et une lumière (la
    /// luminescence, une décharge).
    public sealed record MarqueDArt(AncrageId Ancrage, IReadOnlyList<int> Couleurs, int? Teinte, LumiereEmise Lumiere);

    public sealed record DecorDArt(
        string Assise,
        IReadOnlyList<double> Lumieres,
        int TeinteDeLumiere,
        IReadOnlyList<int> Eau,
        int Vase,
        IReadOnlyList<int> Racines,
        int Rayon,
        bool Berge,
        int Voile);

    public sealed record CouleursDePoisson(int Corps, int Dos, int Ventre);

    /// <summary>
    /// IdlePond — le registre d'art : une assise → son décor, sa courbe de lumière, sa marque
    /// sur le héros (spec DA §1). Ajouter la lave plus tard, c'est ajouter une entrée ici et
    /// des PNG — pas du code. Couleurs en 0xRRGGBB : ce fichier reste pur.
    /// </summary>
    public static class RegistreDArt
    {
        public const int CONTOUR = 0x121C1A;
        /// Le voile et les éclats sont dessinés en blanc et teintés au rendu.
        public const int BLANC = 0xFFFFFF;
        /// Ce qui éclaire encore là où aucune lumière de palier n'arrive.
        public const double LUMIERE_AMBIANTE = 0.15;

        public static readonly CouleursDePoisson HEROS = new CouleursDePoisson(0xC49C5C, 0x8C683A, 0xE2CE96);

        static readonly CouleursDePoisson[] ESPECES_PAR_RANG =
        {
            new CouleursDePoisson(0x96AAA0, 0x566C64, 0xC8D4CC),
            new CouleursDePoisson(0xA08A5E, 0x6A5A3A, 0xCCB88A),
        };

        public static CouleursDePoisson CouleursDEspece(int rang) => ESPECES_PAR_RANG[Math.Max(0, rang) % ESPECES_PAR_RANG.Length];

        /// Les assises qui ont leurs dessins, dans l'ordre. Les autres empruntent la Noue.
        public static readonly IReadOnlyList<string> ASSISES_DESSINEES = new[] { "noue" };

        static readonly Dictionary<string, DecorDArt> DECORS = new Dictionary<string, DecorDArt>
        {
            // La Noue : eau douce, racines de la berge, la lumière du jour qui baisse palier
            // par palier (GDD §15.2 : « la lumière est la variable de progression »).
            ["noue"] = new DecorDArt(
                "noue",
                new[] { 0.85, 0.77, 0.69, 0.61, 0.53, 0.45 },
                0xD8F0DC,
                new[] { 0x4A8070, 0x34645C, 0x244A48 },
                0x28221A,
                new[] { 0x3A2C20, 0x5C4630 },
                0x96BE96,
                true,
                0x8A7A3A),
        };

        static readonly Dictionary<string, MarqueDArt> MARQUES = new Dictionary<string, MarqueDArt>
        {
            // Les branchies : la première adaptation, sans teinte ni lumière.
            ["noue"] = new MarqueDArt(AncrageId.Branchies, new[] { 0x5E4426 }, null, null),
        };

        public static DecorDArt DecorDe(string assise)
        {
            if (assise != null && DECORS.TryGetValue(assise, out var decor)) return decor;
            // Une assise sans dessin : le décor de la Noue, sous la lumière de sa palette —
            // plus bas, plus sombre. La berge n'existe qu'en surface.
            var noue = DECORS[ASSISES_DESSINEES[0]];
            return noue with
            {
                Assise = assise ?? "",
                Lumieres = new[] { 0.5 * Palette.PaletteDe(assise).Lumiere },
                Berge = false,
            };
        }

        public static MarqueDArt MarqueDe(string assise) =>
            assise != null && MARQUES.TryGetValue(assise, out var marque) ? marque : null;

        public static double LumiereDuPalier(int index)
        {
            var assise = Assises.DuPalier(index);
            var lumieres = DecorDe(assise.Id).Lumieres;
            return lumieres[Math.Min(index - assise.IndexPremierPalier, lumieres.Count - 1)];
        }

        public static IReadOnlyList<int> PaletteDuHeros() =>
            new[] { HEROS.Corps, HEROS.Dos, HEROS.Ventre, CONTOUR }
                .Concat(MARQUES.Values.SelectMany(m => m.Couleurs)).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDesEspeces() =>
            ESPECES_PAR_RANG.SelectMany(c => new[] { c.Corps, c.Dos, c.Ventre }).Append(CONTOUR).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDuDecor(string assise)
        {
            var d = DecorDe(assise);
            return d.Eau.Concat(d.Racines).Append(d.Vase).Append(d.Rayon).Distinct().ToList();
        }
    }
}
```

- [ ] **Step 5: `VueDeScene` passe au stade**

Dans `Assets/IdlePond/Jeu/Scene/VueDeScene.cs` :

Remplacer la déclaration de `VueDuHeros` et son commentaire par :

```csharp
    /// `Stade` : 0 à 3, la taille dessinée du corps (spec DA §3). Il remplace l'échelle
    /// continue de la spec 2026-09-17 [D12] : le pixel art ne s'agrandit pas.
    public sealed record VueDuHeros(int Niveau, int Stade, IReadOnlyList<string> Couches);
```

Supprimer `EchelleDuHeros` et son commentaire. Ajouter à sa place :

```csharp
        /// <summary>
        /// La mue (spec DA §3) : le corps passe à un stade plus grand. Jamais au premier
        /// dessin — une partie chargée n'a pas mué —, jamais en rapetissant — la renaissance
        /// n'est pas une mue. La vue ne connaît qu'un état : c'est la scène qui retient le
        /// stade qu'elle affiche.
        /// </summary>
        public static bool EstUneMue(int? stadeAffiche, int nouveauStade) =>
            stadeAffiche.HasValue && nouveauStade > stadeAffiche.Value;
```

Dans `Depuis`, remplacer la construction du héros par :

```csharp
                new VueDuHeros(etat.Cycle.NiveauDuHeros, Gabarits.StadeDuNiveau(etat.Cycle.NiveauDuHeros), etat.Permanent.Couches),
```

Dans `Clef`, remplacer `.Append(':').Append(Heros.Echelle.ToString("R", c))` par `.Append(':').Append(Heros.Stade)`. Si `c` n'est plus utilisé, supprimer `var c = CultureInfo.InvariantCulture;` et le `using System.Globalization;` devenu inutile.

- [ ] **Step 6: Garder l'ancienne scène compilable**

Dans `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs`, méthode `DessinerLeHeros`, remplacer :

```csharp
            var e = (float)v.Heros.Echelle;
```

par :

```csharp
            var e = 1f + 0.5f * v.Heros.Stade; // provisoire : cette scène est réécrite à la Tâche 4
```

- [ ] **Step 7: Lancer et voir passer**

Run: `outils/unity.sh tests EditMode`
Expected: tout vert (les deux tests remplacés + 4 nouveaux dans `VueDeSceneTests`, 6 dans `GabaritsTests`).

- [ ] **Step 8: Commit**

```bash
git add Assets/IdlePond/Jeu/Scene Assets/IdlePond/Tests
git commit -m "feat(scene): stades du héros, gabarits et registre d'art, données pures"
```

---

### Task 3: Les sprites provisoires — générateur, import, catalogue

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/CatalogueDArt.cs`
- Create: `Assets/IdlePond/Editeur/Chemins.cs`, `Toile.cs`, `GenerateurDeSprites.cs`, `ImportDArt.cs`, `Catalogue.cs`
- Create (produits, puis commités): `Assets/IdlePond/Art/**`
- Test: `Assets/IdlePond/Tests/GenerateurDeSpritesTests.cs` (nouveau), `Assets/IdlePond/Tests/GabaritsTests.cs` (ajouts)

**Interfaces:**
- Consumes: `Gabarits`, `RegistreDArt` (Tâche 2).
- Produces:
  - `IdlePond.Jeu.Scene.CatalogueDArt : ScriptableObject` — champs publics `Images[] Corps`, `MarqueDAssise[] Marques`, `ImagesDEspece[] Especes`, `DecorDAssise[] Decors`, `Sprite Voile`, `Sprite Eclat` ; méthodes `Sprite CorpsDe(int stade, int image)`, `Sprite MarqueDe(string assise, int stade)`, `Sprite EspeceDe(string espece, int image)`, `DecorDAssise DecorDe(string assise)` (repli sur le premier) — toutes **null-safe**.
  - `IdlePond.Editeur.Chemins` : `ART`, `CATALOGUE`, `VOILE`, `ECLAT`, `CorpsDuHeros(int, int)`, `MarqueDAssise(string, int)`, `ImageDEspece(string, int)`, `Fond(string)`, `Rayons(string)`, `Berge(string)`, `EspecesLivrees()`.
  - `IdlePond.Editeur.GenerateurDeSprites` : `Generer()` (menu et batch), `SortedDictionary<string, byte[]> Produire()`, `string Etiquette(byte[])`, `bool PeutEcrire(bool existe, string userData, byte[] actuel)`.
  - `IdlePond.Editeur.Catalogue.Reconstruire()` ; asset `Assets/IdlePond/Art/Catalogue.asset`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/IdlePond/Tests/GenerateurDeSpritesTests.cs` :

```csharp
using System.Linq;
using IdlePond.Editeur;
using IdlePond.Jeu.Scene;
using NUnit.Framework;
using UnityEngine;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le générateur de provisoires (spec DA §5) : déterministe, et il ne touche jamais à un
    /// vrai dessin.
    /// </summary>
    public class GenerateurDeSpritesTests
    {
        [Test, Description("deux passages produisent les mêmes fichiers, aux mêmes octets")]
        public void Deux_passages_produisent_les_memes_octets()
        {
            var a = GenerateurDeSprites.Produire();
            var b = GenerateurDeSprites.Produire();
            Assert.That(b.Keys, Is.EqualTo(a.Keys));
            foreach (var chemin in a.Keys) Assert.That(b[chemin], Is.EqualTo(a[chemin]), chemin);
        }

        [Test, Description("un fichier absent s'écrit ; un provisoire intact se réécrit ; un vrai dessin est protégé")]
        public void Un_vrai_dessin_est_protege()
        {
            var provisoire = new byte[] { 1, 2, 3 };
            var dessin = new byte[] { 9, 9, 9 };
            Assert.That(GenerateurDeSprites.PeutEcrire(false, null, null), Is.True);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, GenerateurDeSprites.Etiquette(provisoire), provisoire), Is.True);
            // Un dessin déposé à la place d'un provisoire garde son .meta : l'empreinte le trahit.
            Assert.That(GenerateurDeSprites.PeutEcrire(true, GenerateurDeSprites.Etiquette(provisoire), dessin), Is.False);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, null, dessin), Is.False);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, "autre chose", dessin), Is.False);
        }

        [Test, Description("un catalogue vide ou incomplet ne lève jamais : il rend null")]
        public void Un_catalogue_incomplet_ne_leve_jamais()
        {
            var catalogue = ScriptableObject.CreateInstance<CatalogueDArt>();
            try
            {
                Assert.That(catalogue.CorpsDe(0, 0), Is.Null);
                Assert.That(catalogue.CorpsDe(9, -1), Is.Null);
                Assert.That(catalogue.MarqueDe("noue", 0), Is.Null);
                Assert.That(catalogue.MarqueDe(null, 0), Is.Null);
                Assert.That(catalogue.EspeceDe("vairon", 0), Is.Null);
                Assert.That(catalogue.DecorDe("assise-4"), Is.Null);
                catalogue.Corps = new[] { new CatalogueDArt.Images { Sprites = null } };
                Assert.That(catalogue.CorpsDe(0, 0), Is.Null);
            }
            finally { Object.DestroyImmediate(catalogue); }
        }
    }
}
```

Ajouter à `Assets/IdlePond/Tests/GabaritsTests.cs` (en tête : `using System.Collections.Generic; using System.IO; using IdlePond.Editeur; using UnityEditor; using UnityEngine;`, et `using Object = UnityEngine.Object;`) :

```csharp
        /* ─── Les fichiers d'Art/ : ce qui est livré respecte son gabarit ──────────── */

        static Color32[] LirePng(string chemin, out int largeur, out int hauteur)
        {
            Assert.That(File.Exists(chemin), Is.True, chemin + " manque : lancer « IdlePond ▸ Générer les sprites provisoires »");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(chemin)), Is.True, chemin);
                largeur = texture.width;
                hauteur = texture.height;
                return texture.GetPixels32();
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static int Rgb(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        static string Art(string relatif) => Chemins.ART + "/" + relatif;

        [Test, Description("le corps : quatre images par stade, aux dimensions du gabarit")]
        public void Le_corps_a_ses_images_aux_dimensions_du_gabarit()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++)
                {
                    LirePng(Art(Chemins.CorpsDuHeros(stade, image)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDuCorps(stade)), $"stade {stade}, image {image}");
                }
        }

        [Test, Description("les ancrages sont peints, et du même pixel dans les quatre images : seul la queue bat")]
        public void Les_ancrages_ne_bougent_pas_d_une_image_a_l_autre()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                var images = Enumerable.Range(0, Gabarits.IMAGES_DE_NAGE)
                    .Select(i => LirePng(Art(Chemins.CorpsDuHeros(stade, i)), out var l, out _)).ToList();
                var largeur = Gabarits.CadreDuCorps(stade).Largeur;
                foreach (AncrageId ancrage in System.Enum.GetValues(typeof(AncrageId)))
                {
                    var p = Gabarits.Ancrage(stade, ancrage);
                    var pixels = images.Select(px => px[p.Y * largeur + p.X]).ToList();
                    Assert.That(pixels.All(c => c.a == 255), Is.True, $"stade {stade}, {ancrage} transparent");
                    Assert.That(pixels.Select(Rgb).Distinct().Count(), Is.EqualTo(1), $"stade {stade}, {ancrage} bouge");
                }
            }
        }

        [Test, Description("une marque a le cadre du corps et ne déborde jamais du corps")]
        public void Une_marque_reste_sur_le_corps()
        {
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
                for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
                {
                    var marque = LirePng(Art(Chemins.MarqueDAssise(assise, stade)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDuCorps(stade)), $"{assise}, stade {stade}");
                    var corps = LirePng(Art(Chemins.CorpsDuHeros(stade, 0)), out _, out _);
                    for (var i = 0; i < marque.Length; i++)
                        if (marque[i].a > 0) Assert.That(corps[i].a, Is.EqualTo(255), $"{assise}, stade {stade} : la marque déborde au pixel {i}");
                }
        }

        [Test, Description("chaque espèce livrée a ses deux images, aux dimensions de son rang")]
        public void Chaque_espece_livree_a_ses_images()
        {
            var livrees = Chemins.EspecesLivrees().ToList();
            Assert.That(livrees.Select(e => e.Id), Is.EqualTo(new[] { "vairon", "loche" }));
            foreach (var espece in livrees)
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++)
                {
                    LirePng(Art(Chemins.ImageDEspece(espece.Id, image)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDEspece(espece.Rang)), espece.Id);
                }
        }

        [Test, Description("chaque assise dessinée a son fond, ses rayons, et sa berge si elle en déclare une")]
        public void Chaque_assise_dessinee_a_son_decor()
        {
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                LirePng(Art(Chemins.Fond(assise)), out var l, out var h);
                Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE)));
                LirePng(Art(Chemins.Rayons(assise)), out l, out h);
                Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DES_RAYONS)));
                if (RegistreDArt.DecorDe(assise).Berge)
                {
                    LirePng(Art(Chemins.Berge(assise)), out l, out h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE)));
                }
            }
        }

        [Test, Description("chaque pixel opaque d'Art/ appartient à la palette de sa famille, sans demi-transparence")]
        public void Chaque_pixel_appartient_a_sa_palette()
        {
            IReadOnlyList<int> PaletteDe(string relatif)
            {
                if (relatif.StartsWith("Heros/")) return RegistreDArt.PaletteDuHeros();
                if (relatif.StartsWith("Especes/")) return RegistreDArt.PaletteDesEspeces();
                if (relatif.StartsWith("Fonds/")) return RegistreDArt.PaletteDuDecor(relatif.Split('/')[1]);
                if (relatif.StartsWith("Eau/")) return new[] { RegistreDArt.BLANC };
                Assert.Fail("famille inconnue : " + relatif);
                return null;
            }
            var fichiers = Directory.GetFiles(Chemins.ART, "*.png", SearchOption.AllDirectories);
            Assert.That(fichiers, Is.Not.Empty);
            foreach (var fichier in fichiers)
            {
                var relatif = fichier.Replace('\\', '/').Substring(Chemins.ART.Length + 1);
                var palette = new HashSet<int>(PaletteDe(relatif));
                foreach (var c in LirePng(fichier, out _, out _))
                {
                    Assert.That(c.a == 0 || c.a == 255, Is.True, relatif + " : demi-transparence");
                    if (c.a == 255) Assert.That(palette.Contains(Rgb(c)), Is.True, $"{relatif} : #{Rgb(c):X6} hors palette");
                }
            }
        }

        [Test, Description("le catalogue référence chaque sprite livré")]
        public void Le_catalogue_reference_chaque_sprite_livre()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueDArt>(Chemins.CATALOGUE);
            Assert.That(catalogue, Is.Not.Null, Chemins.CATALOGUE);
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++) Assert.That(catalogue.CorpsDe(stade, image), Is.Not.Null);
                foreach (var assise in RegistreDArt.ASSISES_DESSINEES) Assert.That(catalogue.MarqueDe(assise, stade), Is.Not.Null);
            }
            foreach (var espece in Chemins.EspecesLivrees())
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++) Assert.That(catalogue.EspeceDe(espece.Id, image), Is.Not.Null);
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                Assert.That(catalogue.DecorDe(assise).Fond, Is.Not.Null);
                Assert.That(catalogue.DecorDe(assise).Rayons, Is.Not.Null);
            }
            Assert.That(catalogue.Voile, Is.Not.Null);
            Assert.That(catalogue.Eclat, Is.Not.Null);
        }

        [Test, Description("un sprite d'Art/ est importé net : 1 px par unité, filtrage Point, sans compression")]
        public void Un_sprite_est_importe_net()
        {
            var importeur = (TextureImporter)AssetImporter.GetAtPath(Art(Chemins.CorpsDuHeros(0, 0)));
            Assert.That(importeur.spritePixelsPerUnit, Is.EqualTo(1f));
            Assert.That(importeur.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importeur.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importeur.mipmapEnabled, Is.False);
        }
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests EditMode GenerateurDeSpritesTests`
Expected: échec de compilation (`GenerateurDeSprites`, `CatalogueDArt`, `Chemins` inconnus).

- [ ] **Step 3: Écrire `CatalogueDArt.cs`**

`Assets/IdlePond/Jeu/Scene/CatalogueDArt.cs` :

```csharp
using System;
using System.Linq;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les sprites du jeu, rangés par ce qu'ils montrent. Rempli par l'éditeur
    /// (« IdlePond ▸ Générer les sprites provisoires »), jamais à la main ; la scène le lit et
    /// ne charge aucun fichier. Toutes les lectures sont sûres : un dessin manquant rend
    /// null, et la scène ne dessine rien à cet endroit.
    /// </summary>
    public sealed class CatalogueDArt : ScriptableObject
    {
        [Serializable] public sealed class Images { public Sprite[] Sprites = Array.Empty<Sprite>(); }
        [Serializable] public sealed class MarqueDAssise { public string Assise; public Sprite[] ParStade = Array.Empty<Sprite>(); }
        [Serializable] public sealed class ImagesDEspece { public string Espece; public Sprite[] Images = Array.Empty<Sprite>(); }
        [Serializable] public sealed class DecorDAssise { public string Assise; public Sprite Fond; public Sprite Berge; public Sprite Rayons; }

        public Images[] Corps = Array.Empty<Images>();
        public MarqueDAssise[] Marques = Array.Empty<MarqueDAssise>();
        public ImagesDEspece[] Especes = Array.Empty<ImagesDEspece>();
        public DecorDAssise[] Decors = Array.Empty<DecorDAssise>();
        public Sprite Voile;
        public Sprite Eclat;

        static T A<T>(T[] tableau, int i) where T : class =>
            tableau != null && i >= 0 && i < tableau.Length ? tableau[i] : null;

        public Sprite CorpsDe(int stade, int image) => A(A(Corps, stade)?.Sprites, image);

        public Sprite MarqueDe(string assise, int stade) =>
            A(Marques?.FirstOrDefault(m => m != null && m.Assise == assise)?.ParStade, stade);

        public Sprite EspeceDe(string espece, int image) =>
            A(Especes?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images, image);

        /// Une assise sans dessin emprunte le décor de la première.
        public DecorDAssise DecorDe(string assise) =>
            Decors?.FirstOrDefault(d => d != null && d.Assise == assise) ?? A(Decors, 0);
    }
}
```

- [ ] **Step 4: Écrire `Chemins.cs` et `Toile.cs`**

`Assets/IdlePond/Editeur/Chemins.cs` :

```csharp
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Editeur
{
    /// Où vit chaque dessin, relatif à `Art/`. Un vrai dessin prend la place d'un provisoire
    /// en portant le même nom (spec DA §5).
    public static class Chemins
    {
        public const string ART = "Assets/IdlePond/Art";
        public const string CATALOGUE = ART + "/Catalogue.asset";
        public const string VOILE = "Eau/voile.png";
        public const string ECLAT = "Eau/eclat.png";

        public static string CorpsDuHeros(int stade, int image) => $"Heros/corps-s{stade}-i{image}.png";
        public static string MarqueDAssise(string assise, int stade) => $"Heros/marques/{assise}-s{stade}.png";
        public static string ImageDEspece(string espece, int image) => $"Especes/{espece}-i{image}.png";
        public static string Fond(string assise) => $"Fonds/{assise}/fond.png";
        public static string Rayons(string assise) => $"Fonds/{assise}/rayons.png";
        public static string Berge(string assise) => $"Fonds/{assise}/berge.png";

        /// Les espèces des paliers livrés : la Noue s'arrête au palier 5, l'épinoche vit au 6.
        public static IEnumerable<Espece> EspecesLivrees() => Especes.Toutes.Where(e => e.Palier < Assises.PALIERS_LIVRES);
    }
}
```

`Assets/IdlePond/Editeur/Toile.cs` :

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// Une petite toile de pixels, origine en bas à gauche. Une case vide est transparente ;
    /// une case peinte est opaque — le pixel art n'a pas de demi-teinte d'alpha.
    public sealed class Toile
    {
        public readonly int Largeur, Hauteur;
        readonly int?[] pixels;

        /// Le tramage ordonné 4×4 : il remplace les dégradés, que le pixel art n'a pas.
        static readonly int[,] BAYER = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

        public Toile(int largeur, int hauteur)
        {
            Largeur = largeur;
            Hauteur = hauteur;
            pixels = new int?[largeur * hauteur];
        }

        public static double Seuil(int x, int y) => (BAYER[y & 3, x & 3] + 0.5) / 16.0;

        public bool Dedans(int x, int y) => x >= 0 && y >= 0 && x < Largeur && y < Hauteur;

        public void Poser(int x, int y, int couleur)
        {
            if (Dedans(x, y)) pixels[y * Largeur + x] = couleur;
        }

        public int? Lire(int x, int y) => Dedans(x, y) ? pixels[y * Largeur + x] : null;

        /// Le contour d'un pixel : toute case vide qui touche par un côté une case peinte
        /// d'une autre couleur que le contour.
        public void Contourner(int couleur)
        {
            var aPeindre = new List<(int X, int Y)>();
            var voisins = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            for (var y = 0; y < Hauteur; y++)
                for (var x = 0; x < Largeur; x++)
                {
                    if (Lire(x, y).HasValue) continue;
                    foreach (var (dx, dy) in voisins)
                    {
                        var v = Lire(x + dx, y + dy);
                        if (v.HasValue && v.Value != couleur) { aPeindre.Add((x, y)); break; }
                    }
                }
            foreach (var (x, y) in aPeindre) Poser(x, y, couleur);
        }

        public byte[] EnPng()
        {
            var texture = new Texture2D(Largeur, Hauteur, TextureFormat.RGBA32, false);
            var couleurs = new Color32[Largeur * Hauteur];
            for (var i = 0; i < couleurs.Length; i++)
            {
                var c = pixels[i];
                couleurs[i] = c.HasValue
                    ? new Color32((byte)(c.Value >> 16), (byte)(c.Value >> 8), (byte)c.Value, 255)
                    : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(couleurs);
            texture.Apply();
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }
    }
}
```

- [ ] **Step 5: Écrire `GenerateurDeSprites.cs`**

`Assets/IdlePond/Editeur/GenerateurDeSprites.cs` :

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — les sprites provisoires (spec DA §5), dessinés par code : formes, contour
    /// d'un pixel, tramage ordonné, couleurs du registre d'art. Déterministe : deux passages
    /// rendent les mêmes octets. Il ne remplace JAMAIS un vrai dessin : chaque fichier qu'il
    /// écrit porte dans son `.meta` l'empreinte de ce qu'il a écrit ; un fichier dont le
    /// contenu ne correspond plus à cette empreinte est un dessin déposé, et il est protégé.
    ///
    /// Menu « IdlePond ▸ Générer les sprites provisoires », ou en batch :
    /// `outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer`.
    /// </summary>
    public static class GenerateurDeSprites
    {
        public const string PREFIXE = "idlepond:provisoire:";

        [MenuItem("IdlePond/Générer les sprites provisoires")]
        public static void Generer()
        {
            try
            {
                Ecrire(Produire());
                Catalogue.Reconstruire();
                AssetDatabase.SaveAssets();
                Debug.Log("IdlePond : sprites provisoires générés.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static string Etiquette(byte[] octets)
        {
            using (var sha = SHA1.Create())
                return PREFIXE + BitConverter.ToString(sha.ComputeHash(octets)).Replace("-", "");
        }

        public static bool PeutEcrire(bool existe, string userData, byte[] actuel) =>
            !existe || (userData != null && actuel != null && userData == Etiquette(actuel));

        /// Chemin relatif à `Art/` → octets PNG.
        public static SortedDictionary<string, byte[]> Produire()
        {
            var sortie = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++)
                    sortie[Chemins.CorpsDuHeros(stade, image)] = DessinerCorps(stade, image).EnPng();
                foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
                    sortie[Chemins.MarqueDAssise(assise, stade)] = DessinerMarque(assise, stade).EnPng();
            }
            foreach (var espece in Chemins.EspecesLivrees())
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++)
                    sortie[Chemins.ImageDEspece(espece.Id, image)] = DessinerEspece(espece, image).EnPng();
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                var decor = RegistreDArt.DecorDe(assise);
                sortie[Chemins.Fond(assise)] = DessinerFond(decor).EnPng();
                sortie[Chemins.Rayons(assise)] = DessinerRayons(decor).EnPng();
                if (decor.Berge) sortie[Chemins.Berge(assise)] = DessinerBerge(decor).EnPng();
            }
            sortie[Chemins.VOILE] = DessinerVoile().EnPng();
            sortie[Chemins.ECLAT] = DessinerEclat().EnPng();
            return sortie;
        }

        static void Ecrire(SortedDictionary<string, byte[]> fichiers)
        {
            foreach (var fichier in fichiers)
            {
                var chemin = Chemins.ART + "/" + fichier.Key;
                var existe = File.Exists(chemin);
                var actuel = existe ? File.ReadAllBytes(chemin) : null;
                var importeur = existe ? AssetImporter.GetAtPath(chemin) : null;
                if (!PeutEcrire(existe, importeur?.userData, actuel))
                {
                    Debug.Log($"IdlePond : {chemin} est un vrai dessin, laissé tel quel.");
                    continue;
                }
                if (existe && actuel.SequenceEqual(fichier.Value)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(chemin));
                File.WriteAllBytes(chemin, fichier.Value);
                AssetDatabase.ImportAsset(chemin, ImportAssetOptions.ForceUpdate);
                importeur = AssetImporter.GetAtPath(chemin);
                importeur.userData = Etiquette(fichier.Value);
                importeur.SaveAndReimport();
            }
        }

        /* ─── Les poissons ───────────────────────────────────────────────────────── */

        /// Le battement de la queue, image par image : droite, haut, droite, bas.
        static readonly int[] BATTEMENTS = { 0, 1, 0, -1 };

        static Toile DessinerCorps(int stade, int image) =>
            DessinerPoisson(Gabarits.LONGUEURS_DU_CORPS[stade], RegistreDArt.HEROS, BATTEMENTS[image]);

        static Toile DessinerEspece(Espece espece, int image) =>
            DessinerPoisson(Gabarits.LongueurDEspece(espece.Rang), RegistreDArt.CouleursDEspece(espece.Rang), image == 0 ? 1 : -1);

        /// Un poisson qui regarde à droite : un tronc elliptique (dos sombre, flanc, ventre
        /// clair), une queue en éventail échancré qui bat d'un pixel, un œil, un contour.
        static Toile DessinerPoisson(int longueur, CouleursDePoisson c, int battement)
        {
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var toile = new Toile(cadre.Largeur, cadre.Hauteur);
            var naissance = t.Cx - 0.85 * t.A;
            var queue = 0.3 * longueur;
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var dy = y - t.Cy;
                    if (t.Contient(x, y))
                    {
                        toile.Poser(x, y, dy > 0.25 * t.B ? c.Dos : dy < -0.3 * t.B ? c.Ventre : c.Corps);
                        continue;
                    }
                    var q = naissance - x;
                    if (q <= 0 || q > queue) continue;
                    var decale = dy - battement * (q / queue);
                    var ouverture = 0.2 * t.B + q * 0.55;
                    var echancrure = q > 0.6 * queue && Math.Abs(decale) < q * 0.35;
                    if (Math.Abs(decale) <= ouverture && !echancrure) toile.Poser(x, y, c.Dos);
                }
            var oeilX = (int)Math.Round(t.Cx + 0.28 * longueur);
            var oeilY = (int)Math.Round(t.Cy + 0.25 * t.B);
            toile.Poser(oeilX, oeilY, RegistreDArt.CONTOUR);
            if (longueur > 18) toile.Poser(oeilX - 1, oeilY, RegistreDArt.CONTOUR);
            toile.Contourner(RegistreDArt.CONTOUR);
            return toile;
        }

        /// Une marque, dans le cadre du corps de son stade, seulement sur le tronc.
        static Toile DessinerMarque(string assise, int stade)
        {
            var marque = RegistreDArt.MarqueDe(assise);
            var longueur = Gabarits.LONGUEURS_DU_CORPS[stade];
            var cadre = Gabarits.CadreDuCorps(stade);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var toile = new Toile(cadre.Largeur, cadre.Hauteur);
            var a = Gabarits.Ancrage(stade, marque.Ancrage);
            switch (marque.Ancrage)
            {
                case AncrageId.Branchies:
                    // Une fente par stade : les branchies se creusent à mesure qu'il grandit.
                    var demi = (int)Math.Round(0.55 * t.B);
                    for (var fente = 0; fente <= stade; fente++)
                    {
                        var x = a.X - 2 * fente;
                        for (var y = a.Y - demi; y <= a.Y + demi; y++)
                            if (t.Contient(x, y)) toile.Poser(x, y, marque.Couleurs[0]);
                    }
                    break;
                default:
                    throw new NotSupportedException($"aucun provisoire pour une marque à l'ancrage {marque.Ancrage} ({assise})");
            }
            return toile;
        }

        /* ─── Le décor ───────────────────────────────────────────────────────────── */

        /// Une bande d'eau qui se répète en largeur : l'eau claire, tramée vers le sombre en
        /// bas, et un liseré de vase. La lumière, elle, vient des Light2D.
        static Toile DessinerFond(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var t = y / (double)(toile.Hauteur - 1);
                    var couleur = d.Eau[0];
                    if (t < 0.35 && Toile.Seuil(x, y) < (0.35 - t) / 0.35) couleur = d.Eau[1];
                    if (t < 0.12 && Toile.Seuil(x, y) < (0.12 - t) / 0.12) couleur = d.Eau[2];
                    if (y <= 1 || (y == 2 && x % 2 == 0)) couleur = d.Vase;
                    toile.Poser(x, y, couleur);
                }
            return toile;
        }

        /// Les rayons du jour sur les deux premières bandes, tramés : des pixels pleins, jamais
        /// de demi-transparence.
        static Toile DessinerRayons(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DES_RAYONS);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var haut = y / (double)(toile.Hauteur - 1);
                    var centre = 0.3 + (1 - haut) * 0.2;
                    var force = Math.Max(0, 1 - Math.Abs(x / (double)(toile.Largeur - 1) - centre) * 3.2) * haut;
                    if (force * 0.45 > Toile.Seuil(x, y)) toile.Poser(x, y, d.Rayon);
                }
            return toile;
        }

        /// Les racines de la berge, qui pendent du haut : l'inversion d'échelle (GDD §15.2).
        static Toile DessinerBerge(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE);
            uint graine = 7;
            double Hasard()
            {
                graine = graine * 1664525u + 1013904223u;
                return (graine >> 8) / 16777216.0;
            }
            const int RACINES = 12;
            for (var k = 0; k < RACINES; k++)
            {
                var x = (k + 0.5) * toile.Largeur / RACINES + (Hasard() - 0.5) * 6;
                var longueur = (int)(toile.Hauteur * (0.35 + Hasard() * 0.5));
                var epaisseur = Hasard() < 0.4 ? 2 : 1;
                for (var i = 0; i < longueur; i++)
                {
                    x += (Hasard() - 0.5) * 0.9;
                    for (var e = 0; e < epaisseur; e++)
                        toile.Poser((int)Math.Round(x) + e, toile.Hauteur - 1 - i, e == 0 ? d.Racines[1] : d.Racines[0]);
                }
            }
            return toile;
        }

        /// Le voile de l'eau trouble : un damier, teinté et dosé au rendu.
        static Toile DessinerVoile()
        {
            var toile = new Toile(Gabarits.COTE_DU_VOILE, Gabarits.COTE_DU_VOILE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                    if ((x + y) % 2 == 0) toile.Poser(x, y, RegistreDArt.BLANC);
            return toile;
        }

        /// Un éclat de mue : un carré de 2 px.
        static Toile DessinerEclat()
        {
            var toile = new Toile(2, 2);
            for (var y = 0; y < 2; y++)
                for (var x = 0; x < 2; x++)
                    toile.Poser(x, y, RegistreDArt.BLANC);
            return toile;
        }
    }
}
```

- [ ] **Step 6: Écrire `ImportDArt.cs` et `Catalogue.cs`**

`Assets/IdlePond/Editeur/ImportDArt.cs` :

```csharp
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — l'import d'un dessin est imposé, jamais réglé à la main (spec DA §5) : un
    /// pixel par unité, filtrage net, sans compression ni mipmaps, pivot en bas à gauche pour
    /// que toute position entière tombe sur la grille. Un vrai dessin déposé dans `Art/` est
    /// donc juste du premier coup.
    /// </summary>
    public sealed class ImportDArt : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Chemins.ART + "/")) return;
            var importeur = (TextureImporter)assetImporter;
            importeur.textureType = TextureImporterType.Sprite;
            importeur.spriteImportMode = SpriteImportMode.Single;
            importeur.spritePixelsPerUnit = 1;
            importeur.filterMode = FilterMode.Point;
            importeur.textureCompression = TextureImporterCompression.Uncompressed;
            importeur.mipmapEnabled = false;
            importeur.alphaIsTransparency = true;
            importeur.wrapMode = TextureWrapMode.Clamp;
            importeur.isReadable = false;
            var reglages = new TextureImporterSettings();
            importeur.ReadTextureSettings(reglages);
            reglages.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            reglages.spriteMeshType = SpriteMeshType.FullRect;
            reglages.spriteGenerateFallbackPhysicsShape = false;
            importeur.SetTextureSettings(reglages);
        }
    }
}
```

`Assets/IdlePond/Editeur/Catalogue.cs` :

```csharp
using System.Linq;
using IdlePond.Jeu.Scene;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — remplit `Art/Catalogue.asset` à partir des chemins imposés. Un vrai dessin
    /// déposé sous le même nom garde le GUID du provisoire : le catalogue n'a pas à changer.
    /// Il se reconstruit quand la LISTE change (une assise, une espèce de plus).
    /// </summary>
    public static class Catalogue
    {
        [MenuItem("IdlePond/Reconstruire le catalogue d'art")]
        public static CatalogueDArt Reconstruire()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueDArt>(Chemins.CATALOGUE);
            if (catalogue == null)
            {
                catalogue = ScriptableObject.CreateInstance<CatalogueDArt>();
                AssetDatabase.CreateAsset(catalogue, Chemins.CATALOGUE);
            }
            Sprite S(string relatif) => AssetDatabase.LoadAssetAtPath<Sprite>(Chemins.ART + "/" + relatif);
            var stades = Enumerable.Range(0, Gabarits.LONGUEURS_DU_CORPS.Count).ToList();

            catalogue.Corps = stades.Select(s => new CatalogueDArt.Images
            {
                Sprites = Enumerable.Range(0, Gabarits.IMAGES_DE_NAGE).Select(i => S(Chemins.CorpsDuHeros(s, i))).ToArray(),
            }).ToArray();
            catalogue.Marques = RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.MarqueDAssise
            {
                Assise = a,
                ParStade = stades.Select(s => S(Chemins.MarqueDAssise(a, s))).ToArray(),
            }).ToArray();
            catalogue.Especes = Chemins.EspecesLivrees().Select(e => new CatalogueDArt.ImagesDEspece
            {
                Espece = e.Id,
                Images = Enumerable.Range(0, Gabarits.IMAGES_D_ESPECE).Select(i => S(Chemins.ImageDEspece(e.Id, i))).ToArray(),
            }).ToArray();
            catalogue.Decors = RegistreDArt.ASSISES_DESSINEES.Select(a => new CatalogueDArt.DecorDAssise
            {
                Assise = a,
                Fond = S(Chemins.Fond(a)),
                Berge = S(Chemins.Berge(a)),
                Rayons = S(Chemins.Rayons(a)),
            }).ToArray();
            catalogue.Voile = S(Chemins.VOILE);
            catalogue.Eclat = S(Chemins.ECLAT);

            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            return catalogue;
        }
    }
}
```

- [ ] **Step 7: Produire les provisoires**

Run: `outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer`
Expected: code 0 ; `Assets/IdlePond/Art/` contient 16 corps, 4 marques, 4 images d'espèces, `Fonds/noue/{fond,rayons,berge}.png`, `Eau/{voile,eclat}.png`, `Catalogue.asset`.

Relancer la même commande : `git status --short Assets/IdlePond/Art` ne doit montrer **aucune** modification par rapport au premier passage (vérifie l'idempotence sur disque).

- [ ] **Step 8: Lancer les tests**

Run: `outils/unity.sh tests EditMode`
Expected: tout vert, dont `GenerateurDeSpritesTests` (3) et les 8 nouveaux de `GabaritsTests`.

Si `Les_ancrages_ne_bougent_pas_d_une_image_a_l_autre` échoue, c'est que la queue empiète sur le tronc : réduire `naissance` (par exemple `t.Cx - 0.9 * t.A`) dans `DessinerPoisson`, jamais assouplir le test.

- [ ] **Step 9: Relire les provisoires**

Ouvrir `Assets/IdlePond/Art/Heros/corps-s3-i0.png` et `Heros/marques/noue-s3.png` dans une visionneuse avec un zoom ×8. Le corps doit se lire comme un poisson qui regarde à droite, et les branchies tomber derrière l'œil.

- [ ] **Step 10: Commit**

```bash
git add Assets/IdlePond/Art Assets/IdlePond/Art.meta Assets/IdlePond/Jeu/Scene/CatalogueDArt.cs* Assets/IdlePond/Editeur Assets/IdlePond/Tests
git commit -m "feat(art): sprites provisoires générés, import imposé et catalogue d'art"
```

---

### Task 4: Le rendu pixel et le décor

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/Cadrage.cs`, `OrdreDeRendu.cs`, `Briques.cs`, `RenduPixel.cs`, `Decor.cs`
- Rewrite: `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs`
- Modify: `Assets/IdlePond/Jeu/UI/Mare.uss` (`.scene`), `Assets/IdlePond/Editeur/GenerateurDeScenes.cs`, `outils/unity.sh`
- Regenerate: `Assets/IdlePond/Scenes/Mare.unity`
- Test: `Assets/IdlePond/Tests/CadrageTests.cs`, `Assets/IdlePond/TestsDeJeu/ScenePixelTests.cs`, `Assets/IdlePond/TestsDeJeu/CapturesDeControle.cs`

**Interfaces:**
- Consumes: `Gabarits`, `RegistreDArt`, `CatalogueDArt`, `Chemins.CATALOGUE`, `ConfigurationURP.Appliquer()`.
- Produces:
  - `sealed record DimensionsDuRendu(int Facteur, int Largeur, int Hauteur)` ; `sealed record ChampDeCamera(int Gauche, int Haut, int Largeur, int Hauteur)` avec `int Bas` ; `static class Cadrage` : `DimensionsDuRendu Dimensions(int largeurPx, int hauteurPx)` (null si une dimension < 1), `ChampDeCamera Cadrer(DimensionsDuRendu, int nombreDeBandes)`, `(int Premiere, int Derniere) BandesVisibles(ChampDeCamera, int nombreDeBandes)`.
  - `static class OrdreDeRendu` : `FOND=0, RAYONS=10, BORD=20, NAGEURS=30, CORPS=40, MARQUES=50, PARTICULES=70, VOILE=80`.
  - `static class Briques` : `Vider(Transform)`, `SpriteRenderer Poser(Transform parent, string nom, Sprite, int x, int y, int ordre)`, `SpriteRenderer Paver(Transform parent, string nom, Sprite, int x, int y, int largeur, int hauteur, int ordre)`, `Color Couleur(int rgb, float alpha = 1f)`.
  - `sealed class RenduPixel : IDisposable` : `Camera Camera`, `RenderTexture Texture`, `DimensionsDuRendu Dimensions`, `ChampDeCamera Champ`, `bool Redimensionner(int largeurPx, int hauteurPx)`, `void Cadrer(int nombreDeBandes)`.
  - `sealed class Decor` : `Decor(Transform parent, CatalogueDArt)`, `void Dessiner(VueDeScene)`.
  - `SceneDeLaMare` : `[SerializeField] CatalogueDArt catalogue`, `RenduPixel Rendu`, `VueDeScene Vue`, `void ImposerLaTaille(int largeurPx, int hauteurPx)`.

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/IdlePond/Tests/CadrageTests.cs` :

```csharp
using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// Le rendu pixel (spec DA §2) : un facteur entier, une texture qui tient dans #scene, et
    /// une caméra qui garde visible la bande du héros.
    public class CadrageTests
    {
        [Test, Description("k est l'entier le plus proche de largeur / 240, et la texture tient dans le cadre")]
        public void Le_facteur_est_entier_et_la_texture_tient()
        {
            Assert.That(Cadrage.Dimensions(1080, 768), Is.EqualTo(new DimensionsDuRendu(5, 216, 153)));
            Assert.That(Cadrage.Dimensions(1600, 430), Is.EqualTo(new DimensionsDuRendu(7, 228, 61)));
            Assert.That(Cadrage.Dimensions(240, 170), Is.EqualTo(new DimensionsDuRendu(1, 240, 170)));
        }

        [Test, Description("un cadre étroit garde k = 1 ; un cadre vide ne rend rien, sans erreur")]
        public void Un_cadre_etroit_ou_vide()
        {
            Assert.That(Cadrage.Dimensions(100, 50), Is.EqualTo(new DimensionsDuRendu(1, 100, 50)));
            Assert.That(Cadrage.Dimensions(0, 300), Is.Null);
            Assert.That(Cadrage.Dimensions(300, 0), Is.Null);
            Assert.That(Cadrage.Dimensions(-4, -4), Is.Null);
        }

        [Test, Description("tout tient : la coupe s'accroche en haut ; sinon le bas de la coupe reste en bas du cadre")]
        public void La_camera_garde_la_bande_du_heros()
        {
            var portrait = new DimensionsDuRendu(5, 216, 153);
            Assert.That(Cadrage.Cadrer(portrait, 2), Is.EqualTo(new ChampDeCamera(12, 0, 216, 153)));
            var six = Cadrage.Cadrer(portrait, 6);
            Assert.That(six, Is.EqualTo(new ChampDeCamera(12, 153 - 336, 216, 153)));
            Assert.That(six.Bas, Is.EqualTo(-336));
        }

        [Test, Description("les bandes visibles : celles que le champ touche, et aucune s'il n'y en a pas")]
        public void Les_bandes_visibles()
        {
            var portrait = new DimensionsDuRendu(5, 216, 153);
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 6), 6), Is.EqualTo((3, 5)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 2), 2), Is.EqualTo((0, 1)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 0), 0), Is.EqualTo((0, -1)));
        }
    }
}
```

Ajouter `"Assets/IdlePond/Jeu/Scene/Cadrage.cs"` à la liste du test `VueDeScene_et_Palette_ne_referencent_pas_UnityEngine` (`VueDeSceneTests.cs`).

`Assets/IdlePond/TestsDeJeu/ScenePixelTests.cs` :

```csharp
using System.Collections;
using System.Collections.Generic;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// La scène en pixel art, en PlayMode (spec DA §7). On force la taille du cadre plutôt
    /// que de dépendre de la fenêtre du batch ; le dessin lui-même se relit sur les captures.
    /// </summary>
    public class ScenePixelTests
    {
        Partie partie;

        [SetUp]
        public void Preparer()
        {
            ServicesDePartie.Oublier();
            partie = new Partie(new HorlogeFigee(1_700_000_000_000L));
            ServicesDePartie.Installer(partie);
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        internal static EtatJeu EtatDeLaNoue(int niveauDuHeros, bool trouble)
        {
            var e = Reducteur.EtatInitial(1);
            var especes = new Dictionary<string, EtatEspece>(e.Cycle.Especes)
            {
                ["vairon"] = new EtatEspece(true, 40),
                ["loche"] = new EtatEspece(true, 12),
            };
            e = e with
            {
                Cycle = e.Cycle with { PaliersOuverts = Assises.PALIERS_LIVRES, NiveauDuHeros = niveauDuHeros, Especes = especes },
                Permanent = e.Permanent with { Couches = new[] { "noue" } },
            };
            return e with { Cycle = e.Cycle with { ManaCourant = Economie.Contenance(e).Mul(trouble ? 0.95 : 0.1) } };
        }

        internal static IEnumerator ChargerLaMare(System.Action<SceneDeLaMare> recevoir)
        {
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;
            SceneDeLaMare scene = null;
            for (var i = 0; i < 60 && scene == null; i++)
            {
                yield return null;
                scene = Object.FindFirstObjectByType<SceneDeLaMare>();
            }
            Assert.That(scene, Is.Not.Null, "Mare n'a pas de SceneDeLaMare");
            recevoir(scene);
        }

        [UnityTest]
        public IEnumerator La_scene_rend_dans_une_texture_a_facteur_entier()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            scene.ImposerLaTaille(1080, 768);
            yield return null;
            yield return null;
            Assert.That(scene.Rendu.Dimensions, Is.EqualTo(new DimensionsDuRendu(5, 216, 153)));
            Assert.That(scene.Rendu.Texture.width, Is.EqualTo(216));
            Assert.That(scene.Rendu.Texture.height, Is.EqualTo(153));
            Assert.That(scene.Rendu.Texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(scene.Rendu.Camera.targetTexture, Is.SameAs(scene.Rendu.Texture));
        }

        [UnityTest]
        public IEnumerator La_texture_suit_la_taille_et_l_ancienne_est_liberee()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            scene.ImposerLaTaille(1080, 768);
            yield return null;
            var premiere = scene.Rendu.Texture;
            scene.ImposerLaTaille(1600, 430);
            yield return null;
            yield return null;
            Assert.That(scene.Rendu.Dimensions, Is.EqualTo(new DimensionsDuRendu(7, 228, 61)));
            Assert.That(premiere == null, Is.True, "l'ancienne texture n'a pas été détruite");
        }

        [UnityTest]
        public IEnumerator Le_decor_pose_une_bande_par_palier_ouvert()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            var decor = scene.transform.Find("Decor");
            Assert.That(decor, Is.Not.Null);
            var bandes = 0;
            foreach (Transform enfant in decor) if (enfant.name.StartsWith("Bande ")) bandes++;
            Assert.That(bandes, Is.EqualTo(Assises.PALIERS_LIVRES));
        }
    }
}
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests EditMode CadrageTests`
Expected: échec de compilation (`Cadrage` inconnu).

- [ ] **Step 3: Écrire `Cadrage.cs`, `OrdreDeRendu.cs`, `Briques.cs`**

`Assets/IdlePond/Jeu/Scene/Cadrage.cs` :

```csharp
using System;

namespace IdlePond.Jeu.Scene
{
    public sealed record DimensionsDuRendu(int Facteur, int Largeur, int Hauteur);

    /// Ce que voit la caméra, en pixels du monde (y monte). La bande i occupe
    /// [−(i+1)·56 ; −i·56].
    public sealed record ChampDeCamera(int Gauche, int Haut, int Largeur, int Hauteur)
    {
        public int Bas => Haut - Hauteur;
    }

    /// <summary>
    /// IdlePond — le cadrage du rendu pixel (spec DA §2). Pur : k, la taille de la texture et
    /// le champ de la caméra se calculent sans Unity, et se testent.
    /// </summary>
    public static class Cadrage
    {
        /// k est ENTIER : tous les pixels du dessin ont la même taille à l'écran. La texture
        /// tient dans le cadre ; le reste (moins de k pixels) prend le fond de #scene.
        public static DimensionsDuRendu Dimensions(int largeurPx, int hauteurPx)
        {
            if (largeurPx < 1 || hauteurPx < 1) return null;
            var k = Math.Max(1, (int)Math.Round(largeurPx / (double)Gabarits.LARGEUR_VISEE, MidpointRounding.AwayFromZero));
            return new DimensionsDuRendu(k, Math.Max(1, largeurPx / k), Math.Max(1, hauteurPx / k));
        }

        /// Tout tient : le haut de la coupe en haut du cadre. Sinon, le bas de la coupe en bas
        /// du cadre — c'est là que vit le héros. Le champ est centré sur la largeur visée.
        public static ChampDeCamera Cadrer(DimensionsDuRendu d, int nombreDeBandes)
        {
            var gauche = Gabarits.LARGEUR_VISEE / 2 - d.Largeur / 2;
            var total = Math.Max(0, nombreDeBandes) * Gabarits.HAUTEUR_DE_BANDE;
            var haut = total <= d.Hauteur ? 0 : d.Hauteur - total;
            return new ChampDeCamera(gauche, haut, d.Largeur, d.Hauteur);
        }

        public static (int Premiere, int Derniere) BandesVisibles(ChampDeCamera champ, int nombreDeBandes)
        {
            if (nombreDeBandes <= 0) return (0, -1);
            var h = (double)Gabarits.HAUTEUR_DE_BANDE;
            var premiere = Math.Max(0, (int)Math.Floor(-champ.Haut / h));
            var derniere = Math.Min(nombreDeBandes - 1, (int)Math.Ceiling(-champ.Bas / h) - 1);
            return (premiere, derniere);
        }
    }
}
```

`Assets/IdlePond/Jeu/Scene/OrdreDeRendu.cs` :

```csharp
namespace IdlePond.Jeu.Scene
{
    /// L'ordre d'affichage, de l'arrière vers l'avant (spec DA §4). Des `sortingOrder` sur
    /// une seule couche de tri : le lexique interdit toute API dont le nom porte l'ancien mot.
    public static class OrdreDeRendu
    {
        public const int FOND = 0;
        public const int RAYONS = 10;
        public const int BORD = 20;
        public const int NAGEURS = 30;
        public const int CORPS = 40;
        /// Une marque par couche : MARQUES + son rang dans l'ordre des assises traversées.
        public const int MARQUES = 50;
        public const int PARTICULES = 70;
        public const int VOILE = 80;
    }
}
```

`Assets/IdlePond/Jeu/Scene/Briques.cs` :

```csharp
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// Les briques du dessin : poser un sprite à une position ENTIÈRE, paver une surface,
    /// vider un conteneur.
    public static class Briques
    {
        public static void Vider(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) Object.Destroy(parent.GetChild(i).gameObject);
        }

        public static SpriteRenderer Poser(Transform parent, string nom, Sprite sprite, int x, int y, int ordre)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var rendu = go.AddComponent<SpriteRenderer>();
            rendu.sprite = sprite;
            rendu.sortingOrder = ordre;
            return rendu;
        }

        /// Un sprite répété sur une surface : le pivot en bas à gauche fait partir la
        /// surface de (x, y).
        public static SpriteRenderer Paver(Transform parent, string nom, Sprite sprite, int x, int y, int largeur, int hauteur, int ordre)
        {
            var rendu = Poser(parent, nom, sprite, x, y, ordre);
            rendu.drawMode = SpriteDrawMode.Tiled;
            rendu.size = new Vector2(largeur, hauteur);
            return rendu;
        }

        public static Color Couleur(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }
}
```

- [ ] **Step 4: Écrire `RenduPixel.cs` et `Decor.cs`**

`Assets/IdlePond/Jeu/Scene/RenduPixel.cs` :

```csharp
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la caméra de la scène et sa texture (spec DA §2). La caméra dessine dans
    /// une `RenderTexture` de ≈240 px de large, sans lissage ; l'interface l'agrandit d'un
    /// facteur entier dans #scene. Une nouvelle taille de cadre recrée la texture et détruit
    /// l'ancienne.
    /// </summary>
    public sealed class RenduPixel : IDisposable
    {
        public Camera Camera { get; }
        public RenderTexture Texture { get; private set; }
        public DimensionsDuRendu Dimensions { get; private set; }
        public ChampDeCamera Champ { get; private set; }

        public RenduPixel(Transform parent)
        {
            var go = new GameObject("Camera pixel");
            go.transform.SetParent(parent, false);
            Camera = go.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Briques.Couleur(Palette.EAU_ABYSSE);
            Camera.enabled = false;
        }

        /// Vrai si la texture a été recréée.
        public bool Redimensionner(int largeurPx, int hauteurPx)
        {
            var d = Cadrage.Dimensions(largeurPx, hauteurPx);
            if (d == null || d == Dimensions) return false;
            Liberer();
            Texture = new RenderTexture(d.Largeur, d.Hauteur, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Rendu pixel",
            };
            Texture.Create();
            Camera.targetTexture = Texture;
            Camera.orthographicSize = d.Hauteur / 2f;
            Camera.enabled = true;
            Dimensions = d;
            return true;
        }

        /// La position de la caméra suit le champ : des demi-pixels seulement quand une
        /// dimension est impaire, ce qui laisse les bords du champ sur la grille.
        public void Cadrer(int nombreDeBandes)
        {
            if (Dimensions == null) return;
            Champ = Cadrage.Cadrer(Dimensions, nombreDeBandes);
            Camera.transform.position = new Vector3(Champ.Gauche + Champ.Largeur / 2f, Champ.Haut - Champ.Hauteur / 2f, -10f);
        }

        void Liberer()
        {
            if (Texture == null) return;
            Camera.targetTexture = null;
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }

        public void Dispose() => Liberer();
    }
}
```

`Assets/IdlePond/Jeu/Scene/Decor.cs` :

```csharp
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le décor (spec DA §4) : une bande d'eau par palier ouvert, pavée du fond de
    /// son assise ; sur la première, la berge et ses racines (l'inversion d'échelle) et les
    /// rayons du jour. Une assise sans dessin emprunte celui de la Noue ; la lumière, elle,
    /// vient d'`Eclairage`.
    /// </summary>
    public sealed class Decor
    {
        readonly Transform racine;
        readonly CatalogueDArt catalogue;

        public Decor(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Decor").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            Briques.Vider(racine);
            if (catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                var art = catalogue.DecorDe(p.Assise);
                if (art == null) continue;
                var bas = -(p.Index + 1) * Gabarits.HAUTEUR_DE_BANDE;
                Briques.Paver(racine, "Bande " + p.Index, art.Fond, Gabarits.GAUCHE_DU_DECOR, bas,
                    Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.FOND);
                if (p.Index != 0) continue;
                if (RegistreDArt.DecorDe(p.Assise).Berge && art.Berge != null)
                    Briques.Paver(racine, "Berge", art.Berge, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.BORD);
                if (art.Rayons != null)
                    Briques.Poser(racine, "Rayons", art.Rayons, (Gabarits.LARGEUR_VISEE - Gabarits.LARGEUR_DE_BERGE) / 2,
                        -Gabarits.HAUTEUR_DES_RAYONS, OrdreDeRendu.RAYONS);
            }
        }
    }
}
```

- [ ] **Step 5: Réécrire `SceneDeLaMare.cs`**

Remplacer **tout** le contenu de `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs` par :

```csharp
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la scène de la mare, en pixel art (spec DA). Elle ne connaît pas l'état du
    /// jeu : elle écoute `Partie.EtatChange`, en tire une `VueDeScene`, et ne redessine que si
    /// la vue a changé. Elle distribue le dessin à des pièces qui ont chacune un rôle, et
    /// affiche la texture du rendu pixel en fond de l'élément #scene de l'interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneDeLaMare : MonoBehaviour
    {
        [SerializeField] CatalogueDArt catalogue;

        public RenduPixel Rendu { get; private set; }
        public VueDeScene Vue { get; private set; }

        Partie partie;
        bool vueAJour;
        Decor decor;

        UIDocument document;
        VisualElement cadre;
        float prochaineRecherche;
        DimensionsDuRendu affichee;
        Vector2Int? tailleImposee;

        void Awake()
        {
            Rendu = new RenduPixel(transform);
            decor = new Decor(transform, catalogue);
        }

        void OnEnable()
        {
            partie = Boucle.ObtenirOuCreerLaPartie();
            partie.EtatChange += SurEtatChange;
            vueAJour = false;
        }

        void OnDisable()
        {
            if (partie != null) partie.EtatChange -= SurEtatChange;
        }

        void OnDestroy() => Rendu?.Dispose();

        void SurEtatChange(EtatJeu _) => vueAJour = false;

        /// Pour les tests et les captures : une taille de cadre en pixels d'écran, à la place
        /// de celle de #scene.
        public void ImposerLaTaille(int largeurPx, int hauteurPx) => tailleImposee = new Vector2Int(largeurPx, hauteurPx);

        // Après tous les `Update` : la partie a fini d'avancer pour cette image, et
        // l'interface a eu sa mise en page.
        void LateUpdate()
        {
            if (!vueAJour && partie != null)
            {
                vueAJour = true;
                var nouvelle = VueDeScene.Depuis(partie.Etat);
                if (Vue == null || nouvelle.Clef != Vue.Clef)
                {
                    Vue = nouvelle;
                    Redessiner(nouvelle);
                }
            }
            Cadrer();
        }

        void Redessiner(VueDeScene v)
        {
            decor.Dessiner(v);
        }

        void Cadrer()
        {
            if (!TailleDuCadre(out var largeur, out var hauteur)) return;
            if (Rendu.Redimensionner(largeur, hauteur)) affichee = null;
            if (Rendu.Dimensions == null) return;
            Rendu.Cadrer(Vue != null ? Vue.Paliers.Count : 0);
            if (cadre != null && cadre.panel != null && affichee != Rendu.Dimensions) Afficher();
        }

        /// La taille de #scene en pixels d'écran. Le panneau couvre tout l'écran, quelle que
        /// soit son échelle : une fraction du panneau est une fraction de l'écran.
        bool TailleDuCadre(out int largeur, out int hauteur)
        {
            largeur = hauteur = 0;
            if (cadre == null || cadre.panel == null) TrouverLeCadre();
            if (tailleImposee.HasValue)
            {
                largeur = tailleImposee.Value.x;
                hauteur = tailleImposee.Value.y;
                return true;
            }
            if (cadre == null || cadre.panel == null) return false;
            var panneau = cadre.panel.visualTree.worldBound;
            var b = cadre.worldBound;
            if (!(panneau.width > 0f && b.width > 0f && b.height > 0f)) return false;
            var parUnite = Screen.width / panneau.width;
            largeur = Mathf.FloorToInt(b.width * parUnite);
            hauteur = Mathf.FloorToInt(b.height * parUnite);
            return true;
        }

        void TrouverLeCadre()
        {
            // Une recherche par demi-seconde : l'élément n'existe qu'après l'activation du
            // UIDocument, qui peut venir après la nôtre.
            if (Time.unscaledTime < prochaineRecherche) return;
            prochaineRecherche = Time.unscaledTime + 0.5f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            cadre = document != null && document.rootVisualElement != null
                ? document.rootVisualElement.Q<VisualElement>("scene")
                : null;
        }

        /// La texture en fond de #scene, à sa taille exacte de k × texture, centrée : chaque
        /// pixel du dessin couvre k × k pixels d'écran.
        void Afficher()
        {
            var d = Rendu.Dimensions;
            var parPixel = cadre.panel.visualTree.worldBound.width / Screen.width;
            cadre.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Rendu.Texture));
            cadre.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(
                new Length(d.Largeur * d.Facteur * parPixel), new Length(d.Hauteur * d.Facteur * parPixel)));
            cadre.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            cadre.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            cadre.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            affichee = d;
        }
    }
}
```

- [ ] **Step 6: L'interface et le générateur de scènes**

Dans `Assets/IdlePond/Jeu/UI/Mare.uss`, remplacer la règle `.scene` par :

```css
.scene {
    height: 40%;
    flex-shrink: 0;
    /* La texture du rendu pixel est posée en fond par SceneDeLaMare ; ce fond couvre les
       moins de k pixels qui restent autour. */
    background-color: var(--couleur-abysse);
}
```

Mettre à jour le commentaire d'en-tête de `Mare.uss` qui dit que `#scene` « reste transparent pour laisser voir la caméra » : la scène est désormais une image de fond, et l'élément n'est plus transparent.

Dans `Assets/IdlePond/Editeur/GenerateurDeScenes.cs`, méthode `GenererLaMare` :
- après `camera.AddComponent<AudioListener>();`, ajouter :

```csharp
            // La caméra principale ne fait plus que nettoyer l'écran : la scène a sa propre
            // caméra, qui dessine dans une texture (spec DA §2).
            cam.cullingMask = 0;
```

- remplacer les trois lignes qui posent `cameraDeScene` par :

```csharp
            var serialise = new SerializedObject(racine);
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueDArt>(Chemins.CATALOGUE);
            if (catalogue == null) Debug.LogWarning("IdlePond : " + Chemins.CATALOGUE + " est absent, la scène se dessinera vide. Lancer « IdlePond ▸ Générer les sprites provisoires ».");
            serialise.FindProperty("catalogue").objectReferenceValue = catalogue;
            serialise.ApplyModifiedPropertiesWithoutUndo();
```

- [ ] **Step 7: Les captures de contrôle**

`Assets/IdlePond/TestsDeJeu/CapturesDeControle.cs` :

```csharp
using System;
using System.Collections;
using System.IO;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Les captures de contrôle (spec DA §7) : des PNG de la scène, en portrait et en paysage,
    /// héros aux stades 0 à 3, avec et sans voile, dans `Logs/captures/`. Une capture se relit,
    /// elle ne s'assertit pas. Lancées seulement par `outils/unity.sh captures`, qui fournit un
    /// affichage ; ignorées partout ailleurs.
    /// </summary>
    public class CapturesDeControle
    {
        static readonly (string Nom, int Largeur, int Hauteur)[] CADRES = { ("portrait", 1080, 768), ("paysage", 1600, 430) };
        static readonly int[] NIVEAUX = { 1, 4, 16, 256 };

        [UnityTest]
        public IEnumerator Ecrire_les_captures()
        {
            if (Environment.GetEnvironmentVariable("IDLEPOND_CAPTURES") != "1") Assert.Ignore("captures : outils/unity.sh captures");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("captures : il faut un affichage (pas -nographics)");

            ServicesDePartie.Oublier();
            var partie = new Partie(new HorlogeFigee(1_700_000_000_000L));
            ServicesDePartie.Installer(partie);
            SceneDeLaMare scene = null;
            yield return ScenePixelTests.ChargerLaMare(s => scene = s);

            var dossier = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
            Directory.CreateDirectory(dossier);
            foreach (var cadre in CADRES)
            {
                scene.ImposerLaTaille(cadre.Largeur, cadre.Hauteur);
                foreach (var niveau in NIVEAUX)
                    foreach (var trouble in new[] { false, true })
                    {
                        partie.Remplacer(ScenePixelTests.EtatDeLaNoue(niveau, trouble));
                        // Le voile monte en 0,9 s ; une mue dure moins d'une seconde.
                        yield return new WaitForSeconds(1.2f);
                        Ecrire(scene.Rendu, Path.Combine(dossier, $"{cadre.Nom}-niveau{niveau}{(trouble ? "-trouble" : "")}.png"));
                    }
            }
            ServicesDePartie.Oublier();
        }

        /// La texture, agrandie de k au plus proche voisin : la capture montre ce que voit le
        /// joueur, pixel pour pixel.
        static void Ecrire(RenduPixel rendu, string chemin)
        {
            var rt = rendu.Texture;
            var k = rendu.Dimensions.Facteur;
            var precedente = RenderTexture.active;
            RenderTexture.active = rt;
            var lue = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            lue.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            lue.Apply();
            RenderTexture.active = precedente;

            var grande = new Texture2D(rt.width * k, rt.height * k, TextureFormat.RGBA32, false);
            var source = lue.GetPixels32();
            var cible = new Color32[grande.width * grande.height];
            for (var y = 0; y < grande.height; y++)
                for (var x = 0; x < grande.width; x++)
                    cible[y * grande.width + x] = source[(y / k) * rt.width + x / k];
            grande.SetPixels32(cible);
            grande.Apply();
            File.WriteAllBytes(chemin, grande.EncodeToPNG());
            Object.Destroy(lue);
            Object.Destroy(grande);
        }
    }
}
```

Dans `outils/unity.sh`, ajouter à la ligne d'usage `| outils/unity.sh captures`, et ce cas avant `*)` :

```bash
  captures)
    # Les captures de contrôle (spec DA §7) : PlayMode AVEC affichage, pas de -nographics.
    RESULTATS="$SORTIE/resultats-captures.xml"
    rm -f "$RESULTATS"
    IDLEPOND_CAPTURES=1 "$UNITY" -batchmode -projectPath "$PROJET" -runTests -testPlatform PlayMode \
      -testFilter CapturesDeControle \
      -testResults "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/resultats-captures.xml" \
      -logFile "$(cd "$SORTIE" && pwd -W 2>/dev/null || pwd)/unity.log"
    if [ ! -f "$RESULTATS" ]; then
      echo "Aucun résultat : la compilation a probablement échoué." >&2
      erreurs_de_compilation
      exit 1
    fi
    ls -1 "$RACINE/Logs/captures"
    ;;
```

- [ ] **Step 8: Régénérer la scène et lancer les tests**

Run: `outils/unity.sh methode IdlePond.Editeur.GenerateurDeScenes.Generer`
Expected: code 0, aucun avertissement sur le catalogue.

Run: `outils/unity.sh tests EditMode` puis `outils/unity.sh tests PlayMode`
Expected: tout vert ; PlayMode : `MareJouableTests` (1) + `ScenePixelTests` (3), `CapturesDeControle` **ignoré**.

Si `La_scene_rend_dans_une_texture_a_facteur_entier` échoue en `-nographics` parce que la `RenderTexture` ne se crée pas sans carte graphique, garder l'assertion sur `Dimensions` et sur `targetTexture`, et déplacer les assertions de taille de texture derrière `if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)` — ne pas supprimer le test.

- [ ] **Step 9: Les captures**

Run: `outils/unity.sh captures`
Expected: 16 PNG dans `Logs/captures/`. Relire `portrait-niveau1.png` : six bandes d'eau de la Noue, la berge et ses racines en haut, les rayons tramés, le bas de la coupe en bas du cadre. Pas encore de lumière par palier, de poissons ni de héros.

- [ ] **Step 10: Commit**

```bash
git add Assets/IdlePond/Jeu Assets/IdlePond/Editeur Assets/IdlePond/Scenes Assets/IdlePond/Tests Assets/IdlePond/TestsDeJeu outils/unity.sh
git commit -m "feat(scene): rendu pixel à facteur entier dans #scene, décor de la Noue, captures de contrôle"
```

---

### Task 5: L'éclairage

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/Eclairage.cs`
- Modify: `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs`
- Test: `Assets/IdlePond/TestsDeJeu/ScenePixelTests.cs` (ajout)

**Interfaces:**
- Consumes: `RegistreDArt.LumiereDuPalier`, `RegistreDArt.DecorDe(..).TeinteDeLumiere`, `RegistreDArt.LUMIERE_AMBIANTE`, `Cadrage.BandesVisibles`, `RenduPixel.Champ`.
- Produces: `sealed class Eclairage` : `Eclairage(Transform parent)`, `Light2D Ambiante`, `IReadOnlyList<Light2D> ParBande`, `void Dessiner(VueDeScene)`, `void Activer(int premiere, int derniere)` ; `SceneDeLaMare.Eclairage` (propriété).

- [ ] **Step 1: Écrire le test qui échoue**

Ajouter à `ScenePixelTests` (avec `using System.Linq;` et `using UnityEngine.Rendering.Universal;`) :

```csharp
        [UnityTest]
        public IEnumerator Seules_les_lumieres_des_bandes_visibles_sont_allumees()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            scene.ImposerLaTaille(1080, 768);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            Assert.That(scene.Eclairage.Ambiante.lightType, Is.EqualTo(Light2D.LightType.Global));
            Assert.That(scene.Eclairage.ParBande.Count, Is.EqualTo(Assises.PALIERS_LIVRES));
            // Portrait : 153 px de haut, six bandes de 56 → les bandes 3, 4 et 5.
            var allumees = scene.Eclairage.ParBande.Select((l, i) => (l, i)).Where(x => x.l.enabled).Select(x => x.i).ToList();
            Assert.That(allumees, Is.EqualTo(new[] { 3, 4, 5 }));
            Assert.That(scene.Eclairage.ParBande[5].intensity, Is.LessThan(scene.Eclairage.ParBande[3].intensity));
        }
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests PlayMode ScenePixelTests`
Expected: échec de compilation (`SceneDeLaMare.Eclairage` inconnu).

- [ ] **Step 3: Écrire `Eclairage.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la lumière (spec DA §2). Une ambiante faible, et une lumière par palier,
    /// rectangle de la largeur du décor, dont l'intensité suit la courbe de son assise : la
    /// lumière baisse en descendant (GDD §15.2). Seules les lumières des bandes visibles sont
    /// allumées — quatre au plus à l'écran, le budget d'un téléphone.
    /// </summary>
    public sealed class Eclairage
    {
        readonly Transform racine;
        readonly List<Light2D> parBande = new List<Light2D>();

        public Light2D Ambiante { get; }
        public IReadOnlyList<Light2D> ParBande => parBande;

        public Eclairage(Transform parent)
        {
            racine = new GameObject("Eclairage").transform;
            racine.SetParent(parent, false);
            var go = new GameObject("Ambiante");
            go.transform.SetParent(racine, false);
            Ambiante = go.AddComponent<Light2D>();
            Ambiante.lightType = Light2D.LightType.Global;
            Ambiante.intensity = (float)RegistreDArt.LUMIERE_AMBIANTE;
            Ambiante.color = Color.white;
        }

        public void Dessiner(VueDeScene vue)
        {
            foreach (var lumiere in parBande) if (lumiere != null) Object.Destroy(lumiere.gameObject);
            parBande.Clear();
            var l = Gabarits.LARGEUR_DU_DECOR;
            var h = Gabarits.HAUTEUR_DE_BANDE;
            foreach (var p in vue.Paliers)
            {
                var go = new GameObject("Lumiere " + p.Index);
                go.transform.SetParent(racine, false);
                go.transform.localPosition = new Vector3(Gabarits.GAUCHE_DU_DECOR, -(p.Index + 1) * h, 0f);
                var lumiere = go.AddComponent<Light2D>();
                lumiere.lightType = Light2D.LightType.Freeform;
                lumiere.SetShapePath(new[] { new Vector3(0, 0), new Vector3(l, 0), new Vector3(l, h), new Vector3(0, h) });
                lumiere.intensity = (float)RegistreDArt.LumiereDuPalier(p.Index);
                lumiere.color = Briques.Couleur(RegistreDArt.DecorDe(p.Assise).TeinteDeLumiere);
                parBande.Add(lumiere);
            }
        }

        public void Activer(int premiere, int derniere)
        {
            for (var i = 0; i < parBande.Count; i++)
                if (parBande[i] != null) parBande[i].enabled = i >= premiere && i <= derniere;
        }
    }
}
```

- [ ] **Step 4: Brancher dans `SceneDeLaMare`**

Ajouter le champ et la propriété :

```csharp
        public Eclairage Eclairage { get; private set; }
```

Dans `Awake`, après `decor = ...` : `Eclairage = new Eclairage(transform);`

Dans `Redessiner`, après `decor.Dessiner(v);` : `Eclairage.Dessiner(v);`

Dans `Cadrer`, après `Rendu.Cadrer(...)` :

```csharp
            var (premiere, derniere) = Cadrage.BandesVisibles(Rendu.Champ, Vue != null ? Vue.Paliers.Count : 0);
            Eclairage.Activer(premiere, derniere);
```

- [ ] **Step 5: Lancer les tests et les captures**

Run: `outils/unity.sh tests PlayMode` puis `outils/unity.sh tests EditMode`
Expected: tout vert.

Run: `outils/unity.sh captures`
Expected: dans `portrait-niveau1.png`, les bandes du bas sont plus sombres que celles du haut ; rien n'est noir ni blanc saturé. Si tout est noir, le moteur de rendu 2D n'est pas actif (revoir la Tâche 1) ; si tout est saturé, l'ambiante et la courbe dépassent 1.

- [ ] **Step 6: Commit**

```bash
git add Assets/IdlePond/Jeu/Scene Assets/IdlePond/TestsDeJeu
git commit -m "feat(scene): une lumière par palier selon la courbe de l'assise, seules les visibles allumées"
```

---

### Task 6: Les nageurs et le voile

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/Nage.cs`, `Nageurs.cs`, `Voile.cs`
- Modify: `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs`
- Test: `Assets/IdlePond/Tests/NageTests.cs` (nouveau), `Assets/IdlePond/Tests/VueDeSceneTests.cs` (liste des fichiers purs), `Assets/IdlePond/TestsDeJeu/ScenePixelTests.cs` (ajouts)

**Interfaces:**
- Consumes: `Gabarits`, `VueDeScene.EffectifDesPoissons`, `CatalogueDArt.EspeceDe`, `CatalogueDArt.Voile`, `RegistreDArt.DecorDe(..).Voile`, `RenduPixel.Champ`.
- Produces:
  - `sealed record Trajet(int X0, int Y, int Amplitude, double Periode, double Phase)` ; `static class Nage` : `Trajet TrajetDe(int palier, int numero, int longueur)`, `(int X, bool VersLaDroite) Position(Trajet, double secondes)`.
  - `sealed class Nageurs` : `Nageurs(Transform, CatalogueDArt)`, `int Nombre`, `void Dessiner(VueDeScene)`, `void Animer(float secondes)`.
  - `sealed class Voile` : `const float OPACITE = 0.56f`, `const float DUREE_S = 0.9f`, `Voile(Transform, CatalogueDArt)`, `float Opacite`, `void Viser(bool trouble, int teinte)`, `void Couvrir(ChampDeCamera)`, `void Animer(float dt)`.
  - `SceneDeLaMare.Nageurs`, `SceneDeLaMare.Voile` (propriétés).

- [ ] **Step 1: Écrire les tests qui échouent**

`Assets/IdlePond/Tests/NageTests.cs` :

```csharp
using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// La nage (spec DA §4) : déterministe — même état, même image —, et jamais hors de sa
    /// bande ni du champ visé.
    public class NageTests
    {
        [Test, Description("même palier, même numéro, même trajet")]
        public void Le_trajet_est_deterministe()
        {
            Assert.That(Nage.TrajetDe(3, 7, 9), Is.EqualTo(Nage.TrajetDe(3, 7, 9)));
            Assert.That(Nage.TrajetDe(3, 7, 9), Is.Not.EqualTo(Nage.TrajetDe(3, 8, 9)));
        }

        [Test, Description("un nageur reste dans sa bande et dans la largeur visée, à tout instant")]
        public void Un_nageur_reste_dans_sa_bande()
        {
            for (var palier = 0; palier < 6; palier++)
                for (var numero = 0; numero < VueDeScene.POISSONS_MAX_PAR_GROUPE; numero++)
                    for (var longueur = 7; longueur <= 12; longueur++)
                    {
                        var t = Nage.TrajetDe(palier, numero, longueur);
                        var cadre = Gabarits.CadreDUnPoisson(longueur);
                        var bas = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE;
                        Assert.That(t.Y, Is.GreaterThanOrEqualTo(bas));
                        Assert.That(t.Y + cadre.Hauteur, Is.LessThanOrEqualTo(bas + Gabarits.HAUTEUR_DE_BANDE));
                        for (var s = 0.0; s < 20; s += 0.37)
                        {
                            var (x, _) = Nage.Position(t, s);
                            Assert.That(x, Is.GreaterThanOrEqualTo(0));
                            Assert.That(x + cadre.Largeur, Is.LessThanOrEqualTo(Gabarits.LARGEUR_VISEE));
                        }
                    }
        }

        [Test, Description("à l'aller il regarde à droite, au retour à gauche")]
        public void Le_sens_suit_le_mouvement()
        {
            var t = new Trajet(10, 0, 20, 2.0, 0.0);
            Assert.That(Nage.Position(t, 0.5).VersLaDroite, Is.True);
            Assert.That(Nage.Position(t, 2.5).VersLaDroite, Is.False);
            Assert.That(Nage.Position(t, 0).X, Is.EqualTo(10));
            Assert.That(Nage.Position(t, 2.0).X, Is.EqualTo(30));
        }
    }
}
```

Ajouter `"Assets/IdlePond/Jeu/Scene/Nage.cs"` à la liste des fichiers purs de `VueDeSceneTests`.

Ajouter à `ScenePixelTests` :

```csharp
        [UnityTest]
        public IEnumerator Chaque_espece_nage_avec_l_effectif_de_son_niveau()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            // vairon niveau 40 → 1 + ⌊log₂ 40⌋ = 6 ; loche niveau 12 → 4.
            Assert.That(scene.Nageurs.Nombre, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator Le_voile_monte_quand_l_eau_se_trouble()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            scene.ImposerLaTaille(1080, 768);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return new WaitForSeconds(0.2f);
            Assert.That(scene.Voile.Opacite, Is.EqualTo(0f));
            partie.Remplacer(EtatDeLaNoue(1, true));
            yield return new WaitForSeconds(1.2f);
            Assert.That(scene.Voile.Opacite, Is.EqualTo(Voile.OPACITE).Within(1e-4));
        }
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests EditMode NageTests`
Expected: échec de compilation (`Nage`, `Trajet` inconnus).

- [ ] **Step 3: Écrire `Nage.cs`**

```csharp
using System;

namespace IdlePond.Jeu.Scene
{
    public sealed record Trajet(int X0, int Y, int Amplitude, double Periode, double Phase);

    /// <summary>
    /// IdlePond — la nage des espèces (spec DA §4). Pur et déterministe : un hachage
    /// d'entiers, jamais un tirage — même état, même image (et `ArchitectureTests` interdit
    /// tout hasard hors de l'état). Un nageur fait l'aller-retour dans sa bande.
    /// </summary>
    public static class Nage
    {
        static uint Melanger(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return x;
        }

        public static Trajet TrajetDe(int palier, int numero, int longueur)
        {
            var h = Melanger((uint)(palier * 7919 + numero * 104729 + 1));
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var amplitude = 8 + (int)(h % 24u);
            // Aller et retour compris, le nageur tient dans [0 ; 240 − largeur de son cadre].
            var marge = Gabarits.LARGEUR_VISEE - 16 - amplitude - cadre.Largeur;
            var x0 = 8 + (int)((h >> 5) % (uint)marge);
            var hauteurLibre = Gabarits.HAUTEUR_DE_BANDE - 12 - cadre.Hauteur;
            var y = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE + 6 + (int)((h >> 11) % (uint)hauteurLibre);
            var periode = 2.0 + ((h >> 17) % 20u) / 10.0;
            var phase = ((h >> 23) % 100u) / 100.0 * periode * 2;
            return new Trajet(x0, y, amplitude, periode, phase);
        }

        /// Aller-retour en cosinus : de X0 à X0 + amplitude en une période, retour en une
        /// autre. Arrondi au pixel : rien ne se pose entre deux pixels.
        public static (int X, bool VersLaDroite) Position(Trajet t, double secondes)
        {
            var phase = ((secondes + t.Phase) / t.Periode) % 2.0;
            if (phase < 0) phase += 2.0;
            var p = phase <= 1 ? phase : 2 - phase;
            var x = t.X0 + (int)Math.Round(t.Amplitude * 0.5 * (1 - Math.Cos(Math.PI * p)));
            return (x, phase < 1);
        }
    }
}
```

- [ ] **Step 4: Écrire `Nageurs.cs` et `Voile.cs`**

`Assets/IdlePond/Jeu/Scene/Nageurs.cs` :

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les espèces qui nagent dans leur bande (spec DA §4). L'effectif dessiné est
    /// le logarithme du niveau, plafonné : le joueur doit voir le groupe grossir, pas le
    /// compter. Un sprite retourné garde son pied : on le décale de la largeur de son cadre.
    /// </summary>
    public sealed class Nageurs
    {
        sealed class Nageur
        {
            public SpriteRenderer Rendu;
            public Trajet Trajet;
            public string Espece;
            public int Largeur;
        }

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<Nageur> tous = new List<Nageur>();

        public Nageurs(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Nageurs").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public int Nombre => tous.Count;

        public void Dessiner(VueDeScene vue)
        {
            Briques.Vider(racine);
            tous.Clear();
            if (catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                if (p.Espece == null) continue;
                var longueur = Gabarits.LongueurDEspece(p.Espece.Rang);
                var largeur = Gabarits.CadreDUnPoisson(longueur).Largeur;
                var effectif = VueDeScene.EffectifDesPoissons(p.Espece.Niveau);
                for (var numero = 0; numero < effectif; numero++)
                {
                    var trajet = Nage.TrajetDe(p.Index, numero, longueur);
                    var rendu = Briques.Poser(racine, p.Espece.Id, catalogue.EspeceDe(p.Espece.Id, 0), trajet.X0, trajet.Y, OrdreDeRendu.NAGEURS);
                    tous.Add(new Nageur { Rendu = rendu, Trajet = trajet, Espece = p.Espece.Id, Largeur = largeur });
                }
            }
        }

        public void Animer(float secondes)
        {
            foreach (var n in tous)
            {
                if (n.Rendu == null) continue;
                var (x, versLaDroite) = Nage.Position(n.Trajet, secondes);
                n.Rendu.flipX = !versLaDroite;
                n.Rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + n.Largeur, n.Trajet.Y, 0f);
                var image = (int)(secondes * 4 + n.Trajet.Phase * 3) % Gabarits.IMAGES_D_ESPECE;
                var sprite = catalogue.EspeceDe(n.Espece, image);
                if (sprite != null) n.Rendu.sprite = sprite;
            }
        }
    }
}
```

`Assets/IdlePond/Jeu/Scene/Voile.cs` :

```csharp
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — l'eau qui se trouble (GDD §2.4) : un damier pavé sur tout le champ, dans la
    /// couleur de l'assise, qui monte en 0,9 s. Un voile, jamais un texte. Le damier ne
    /// couvre qu'un pixel sur deux : l'opacité est doublée pour retrouver la densité de
    /// l'ancien voile (0,28).
    /// </summary>
    public sealed class Voile
    {
        public const float OPACITE = 0.56f;
        public const float DUREE_S = 0.9f;

        readonly SpriteRenderer rendu;
        float visee;
        int teinte;

        public float Opacite { get; private set; }

        public Voile(Transform parent, CatalogueDArt catalogue)
        {
            rendu = Briques.Paver(parent, "Voile", catalogue != null ? catalogue.Voile : null, 0, 0, 1, 1, OrdreDeRendu.VOILE);
            rendu.enabled = false;
        }

        public void Viser(bool trouble, int teinteDeLAssise)
        {
            visee = trouble ? OPACITE : 0f;
            teinte = teinteDeLAssise;
        }

        public void Couvrir(ChampDeCamera champ)
        {
            rendu.transform.localPosition = new Vector3(champ.Gauche, champ.Bas, 0f);
            rendu.size = new Vector2(champ.Largeur, champ.Hauteur);
        }

        public void Animer(float dt)
        {
            Opacite = Mathf.MoveTowards(Opacite, visee, OPACITE / DUREE_S * dt);
            rendu.enabled = Opacite > 0.0001f && rendu.sprite != null;
            rendu.color = Briques.Couleur(teinte, Opacite);
        }
    }
}
```

- [ ] **Step 5: Brancher dans `SceneDeLaMare`**

Propriétés :

```csharp
        public Nageurs Nageurs { get; private set; }
        public Voile Voile { get; private set; }
```

Dans `Awake` : `Nageurs = new Nageurs(transform, catalogue);` et `Voile = new Voile(transform, catalogue);`

Ajouter :

```csharp
        void Update()
        {
            Nageurs?.Animer(Time.time);
            Voile?.Animer(Time.deltaTime);
        }
```

Dans `Redessiner`, après l'éclairage :

```csharp
            Nageurs.Dessiner(v);
            var assiseDuBas = v.Paliers.Count > 0 ? v.Paliers[v.Paliers.Count - 1].Assise : RegistreDArt.ASSISES_DESSINEES[0];
            Voile.Viser(v.EauTroublee, RegistreDArt.DecorDe(assiseDuBas).Voile);
```

Dans `Cadrer`, après `Eclairage.Activer(...)` : `Voile.Couvrir(Rendu.Champ);`

- [ ] **Step 6: Lancer les tests et les captures**

Run: `outils/unity.sh tests EditMode` puis `outils/unity.sh tests PlayMode`
Expected: tout vert.

Run: `outils/unity.sh captures`
Expected: dans `portrait-niveau1.png`, des vairons et des loches dans leurs bandes ; dans `portrait-niveau1-trouble.png`, le damier du voile sur tout le champ.

- [ ] **Step 7: Commit**

```bash
git add Assets/IdlePond/Jeu/Scene Assets/IdlePond/Tests Assets/IdlePond/TestsDeJeu
git commit -m "feat(scene): les espèces nagent dans leur bande, le voile tramé de l'eau trouble"
```

---

### Task 7: Le héros composé et la mue

**Files:**
- Create: `Assets/IdlePond/Jeu/Scene/HerosEnPixels.cs`
- Modify: `Assets/IdlePond/Jeu/Scene/SceneDeLaMare.cs`
- Test: `Assets/IdlePond/TestsDeJeu/ScenePixelTests.cs` (ajouts)

**Interfaces:**
- Consumes: `VueDeScene.EstUneMue`, `Gabarits.CadreDuCorps`, `Gabarits.Ancrage`, `Gabarits.X_DU_HEROS`, `RegistreDArt.MarqueDe`, `CatalogueDArt.CorpsDe/MarqueDe/Eclat`.
- Produces: `sealed class HerosEnPixels` : `HerosEnPixels(Transform, CatalogueDArt)`, `int? StadeAffiche`, `int MuesJouees`, `int NombreDeMarques`, `void Dessiner(VueDeScene)`, `void Animer(float secondes)` ; `SceneDeLaMare.Heros` (propriété).

- [ ] **Step 1: Écrire les tests qui échouent**

Ajouter à `ScenePixelTests` :

```csharp
        [UnityTest]
        public IEnumerator Une_partie_chargee_n_a_pas_mue()
        {
            partie.Remplacer(EtatDeLaNoue(16, false));
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            yield return null;
            yield return null;
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(2));
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(0));
            Assert.That(scene.Heros.NombreDeMarques, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Le_heros_mue_en_grandissant_jamais_en_renaissant()
        {
            partie.Remplacer(EtatDeLaNoue(3, false));
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            yield return null;
            yield return null;
            partie.Remplacer(EtatDeLaNoue(4, false));
            yield return null;
            yield return null;
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(1), "le passage au niveau 4 est une mue");
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(1));
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(1), "redevenir petit n'est pas une mue");
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(0));
            Assert.That(scene.Heros.NombreDeMarques, Is.EqualTo(1), "la marque survit");
        }
```

- [ ] **Step 2: Lancer et voir échouer**

Run: `outils/unity.sh tests PlayMode ScenePixelTests`
Expected: échec de compilation (`SceneDeLaMare.Heros` inconnu).

- [ ] **Step 3: Écrire `HerosEnPixels.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le héros (spec DA §3) : son corps au stade de son niveau, et par-dessus une
    /// marque par assise traversée, dans l'ordre. Une marque peut teinter tout le corps et
    /// émettre une lumière. Quand le stade monte, il mue : un éclair et des écailles qui
    /// tombent. Il ne mue ni au chargement ni à la renaissance (`VueDeScene.EstUneMue`).
    /// </summary>
    public sealed class HerosEnPixels
    {
        const int ECLATS = 8;
        const float DUREE_DE_L_ECLAIR = 0.4f;
        const float DUREE_DES_ECLATS = 0.8f;
        const float INTENSITE_DE_L_ECLAIR = 3f;

        sealed class Eclat
        {
            public SpriteRenderer Rendu;
            public Vector2Int Depart;
            public int Sens;
        }

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<SpriteRenderer> marques = new List<SpriteRenderer>();
        readonly List<Light2D> lueurs = new List<Light2D>();
        readonly List<Eclat> eclats = new List<Eclat>();
        SpriteRenderer corps;
        Light2D eclair;
        bool muePendante;
        float debutDeLaMue = -10f;
        int stade;
        int x, y;

        public int? StadeAffiche { get; private set; }
        public int MuesJouees { get; private set; }
        public int NombreDeMarques => marques.Count;

        public HerosEnPixels(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Heros").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            var heros = vue.Heros;
            if (VueDeScene.EstUneMue(StadeAffiche, heros.Stade))
            {
                MuesJouees += 1;
                muePendante = true;
            }
            StadeAffiche = heros.Stade;
            stade = heros.Stade;

            foreach (var m in marques) if (m != null) Object.Destroy(m.gameObject);
            marques.Clear();
            foreach (var l in lueurs) if (l != null) Object.Destroy(l.gameObject);
            lueurs.Clear();
            if (corps == null) corps = Briques.Poser(racine, "Corps", null, 0, 0, OrdreDeRendu.CORPS);

            // Dans la bande la plus basse, centré verticalement : c'est là qu'il vit.
            var cadre = Gabarits.CadreDuCorps(stade);
            var bandeDuBas = Mathf.Max(0, vue.Paliers.Count - 1);
            x = Gabarits.X_DU_HEROS - cadre.Largeur / 2;
            y = -(bandeDuBas + 1) * Gabarits.HAUTEUR_DE_BANDE + (Gabarits.HAUTEUR_DE_BANDE - cadre.Hauteur) / 2;

            var teinte = Color.white;
            var rang = 0;
            foreach (var assise in heros.Couches ?? new string[0])
            {
                var art = RegistreDArt.MarqueDe(assise);
                if (art != null)
                {
                    if (art.Teinte.HasValue) teinte *= Briques.Couleur(art.Teinte.Value);
                    var sprite = catalogue != null ? catalogue.MarqueDe(assise, stade) : null;
                    if (sprite != null) marques.Add(Briques.Poser(racine, "Marque " + assise, sprite, 0, 0, OrdreDeRendu.MARQUES + rang));
                    if (art.Lumiere != null) lueurs.Add(Lueur(assise, art));
                }
                rang += 1;
            }
            corps.color = teinte;
        }

        Light2D Lueur(string assise, MarqueDArt art)
        {
            var a = Gabarits.Ancrage(stade, art.Ancrage);
            var go = new GameObject("Lueur " + assise);
            go.transform.SetParent(racine, false);
            go.transform.localPosition = new Vector3(a.X + 0.5f, a.Y + 0.5f, 0f);
            var lueur = go.AddComponent<Light2D>();
            lueur.lightType = Light2D.LightType.Point;
            lueur.color = Briques.Couleur(art.Lumiere.Couleur);
            lueur.intensity = (float)art.Lumiere.Intensite;
            lueur.pointLightOuterRadius = art.Lumiere.Rayon;
            return lueur;
        }

        public void Animer(float secondes)
        {
            if (corps == null) return;
            var image = (int)(secondes * 6) % Gabarits.IMAGES_DE_NAGE;
            corps.sprite = catalogue != null ? catalogue.CorpsDe(stade, image) : null;
            // Il respire d'un pixel, sur 2,2 s : jamais d'un demi-pixel.
            var monte = Mathf.Repeat(secondes, 2.2f) < 1.1f ? 1 : 0;
            racine.localPosition = new Vector3(x, y + monte, 0f);
            AnimerLaMue(secondes);
        }

        void AnimerLaMue(float secondes)
        {
            if (muePendante)
            {
                muePendante = false;
                debutDeLaMue = secondes;
                var cadre = Gabarits.CadreDuCorps(stade);
                if (eclair == null)
                {
                    var go = new GameObject("Eclair");
                    go.transform.SetParent(racine, false);
                    eclair = go.AddComponent<Light2D>();
                    eclair.lightType = Light2D.LightType.Point;
                    eclair.color = Color.white;
                }
                eclair.transform.localPosition = new Vector3(cadre.Largeur / 2f, cadre.Hauteur / 2f, 0f);
                eclair.pointLightOuterRadius = cadre.Largeur;
                foreach (var e in eclats) if (e.Rendu != null) Object.Destroy(e.Rendu.gameObject);
                eclats.Clear();
                for (var i = 0; i < ECLATS; i++)
                {
                    // Des écailles réparties sur le dos, sans hasard : (7i + 3) mod largeur.
                    var depart = new Vector2Int((i * 7 + 3) % cadre.Largeur, cadre.Hauteur / 2 + (i * 5) % (cadre.Hauteur / 2));
                    var rendu = Briques.Poser(racine, "Eclat", catalogue != null ? catalogue.Eclat : null, depart.x, depart.y, OrdreDeRendu.PARTICULES);
                    eclats.Add(new Eclat { Rendu = rendu, Depart = depart, Sens = i % 2 == 0 ? -1 : 1 });
                }
            }
            var age = secondes - debutDeLaMue;
            if (eclair != null) eclair.intensity = Mathf.Max(0f, INTENSITE_DE_L_ECLAIR * (1f - age / DUREE_DE_L_ECLAIR));
            for (var i = eclats.Count - 1; i >= 0; i--)
            {
                var e = eclats[i];
                if (e.Rendu == null) { eclats.RemoveAt(i); continue; }
                if (age > DUREE_DES_ECLATS)
                {
                    Object.Destroy(e.Rendu.gameObject);
                    eclats.RemoveAt(i);
                    continue;
                }
                var t = age / DUREE_DES_ECLATS;
                e.Rendu.transform.localPosition = new Vector3(e.Depart.x + e.Sens * Mathf.RoundToInt(t * 4), e.Depart.y - Mathf.RoundToInt(t * 14), 0f);
                e.Rendu.color = new Color(1f, 1f, 1f, 1f - t);
            }
        }
    }
}
```

- [ ] **Step 4: Brancher dans `SceneDeLaMare`**

Propriété : `public HerosEnPixels Heros { get; private set; }`

Dans `Awake` : `Heros = new HerosEnPixels(transform, catalogue);`

Dans `Update`, ajouter : `Heros?.Animer(Time.time);`

Dans `Redessiner`, après `Nageurs.Dessiner(v);` : `Heros.Dessiner(v);`

- [ ] **Step 5: Lancer les tests**

Run: `outils/unity.sh tests PlayMode` puis `outils/unity.sh tests EditMode`
Expected: tout vert.

- [ ] **Step 6: Les captures, et la bande de 56 px**

Run: `outils/unity.sh captures`
Expected: dans `portrait-niveau{1,4,16,256}.png`, le héros grandit d'une capture à l'autre, toujours dans la bande du bas, avec les branchies derrière l'œil. Mesurer sur `portrait-niveau256.png` : le corps de 31 px de haut doit tenir dans la bande de 56 sans toucher la vase. S'il est trop serré, changer `HAUTEUR_DE_BANDE` dans `Gabarits` (spec DA, risque n° 3), relancer le générateur de sprites, les tests et les captures, et le noter dans le commit.

- [ ] **Step 7: Commit**

```bash
git add Assets/IdlePond/Jeu/Scene Assets/IdlePond/TestsDeJeu
git commit -m "feat(scene): le héros composé — stades, marque des branchies, mue à chaque stade franchi"
```

---

### Task 8: Ménage, cahier des gabarits, documents

**Files:**
- Modify: `Assets/IdlePond/Jeu/Scene/Palette.cs`, `Assets/IdlePond/Tests/VueDeSceneTests.cs`
- Create: `docs/da/gabarits.md`
- Modify: `docs/GDD.md` (§15.2), `docs/superpowers/specs/2026-09-17-axe-heros-benedictions-scene-design.md` ([D12]), `docs/ROADMAP.md`, `README.md`

- [ ] **Step 1: Élaguer `Palette.cs`**

Run: `grep -rn "MarqueId\|MarqueParAssise\|CouleurDesPoissons\|CORPS_DU_HEROS\|OEIL_DU_HEROS\|LUMIERE_DU_JOUR" Assets/IdlePond --include=*.cs`
Expected: seuls `Palette.cs` et `VueDeSceneTests.cs` les citent encore (l'ancienne scène a été réécrite à la Tâche 4).

Dans `Palette.cs`, supprimer : l'`enum MarqueId`, `MARQUES_PAR_RANG`, `MarqueParAssise`, `CouleurDesPoissons`, `Octet`, `CORPS_DU_HEROS`, `OEIL_DU_HEROS`, `LUMIERE_DU_JOUR`, et les paragraphes du commentaire de classe qui parlent des marques et de « aucune image ». Garder les jetons d'eau, `VOILE_DE_TROUBLE`, les six palettes et `PaletteDe` (utilisées par `RegistreDArt` et le générateur de scènes). Ajouter au commentaire de classe : « Les marques et les couleurs des poissons vivent désormais dans `RegistreDArt` (spec DA). »

Dans `VueDeSceneTests.cs`, supprimer `Une_marque_par_assise_toutes_differentes` et `Les_poissons_d_un_rang_ont_une_couleur_valide_et_elle_change_avec_le_rang`.

- [ ] **Step 2: Lancer les tests**

Run: `outils/unity.sh tests EditMode`
Expected: tout vert.

- [ ] **Step 3: Écrire le cahier des gabarits**

`docs/da/gabarits.md` — le cahier des charges du vrai dessin, tiré de `Gabarits.cs` et `RegistreDArt.cs` :

```markdown
# IdlePond — le cahier des gabarits

Ce que doit respecter un dessin pour remplacer un provisoire **sans toucher au code**.
Source de vérité : `Assets/IdlePond/Jeu/Scene/Gabarits.cs` et `RegistreDArt.cs` ; les tests
`GabaritsTests` vérifient chaque fichier livré.

## Règles communes

- PNG, **un fichier par image**, déposé sous `Assets/IdlePond/Art/` au **nom exact** du
  provisoire. L'import est automatique (1 px par unité, filtrage net, sans compression).
- Pixels **opaques ou transparents**, jamais de demi-transparence.
- Couleurs prises dans la **palette de la famille** (ci-dessous) ; une couleur nouvelle
  s'ajoute d'abord dans `RegistreDArt.cs`.
- Le générateur de provisoires ne remplace jamais un fichier déposé à la main.

## Le héros — `Heros/corps-s{stade}-i{image}.png`

Le poisson regarde **à droite**. Quatre images de nage par stade : **seules la queue et les
nageoires bougent** ; le tronc est identique d'une image à l'autre (les marques s'y posent).

| Stade | Niveau | Longueur | Cadre (l × h) |
|---|---|---|---|
| 0 | 1 à 3 | 21 | 23 × 15 |
| 1 | 4 à 15 | 28 | 30 × 17 |
| 2 | 16 à 255 | 42 | 44 × 23 |
| 3 | 256 et plus | 63 | 65 × 31 |

Points d'ancrage (origine **en bas à gauche**), un par marque possible :

| Stade | Branchies | Tête | Dos | Ventre | Flanc | Queue |
|---|---|---|---|---|---|---|
| 0 | 18, 7 | 20, 7 | 15, 10 | 15, 4 | 13, 7 | 9, 7 |
| 1 | 24, 8 | 26, 8 | 19, 11 | 19, 5 | 16, 8 | 12, 8 |
| 2 | 36, 11 | 39, 11 | 28, 16 | 28, 6 | 24, 11 | 17, 11 |
| 3 | 53, 15 | 58, 15 | 42, 23 | 42, 7 | 36, 15 | 26, 15 |

Chaque ancrage est un pixel **opaque du tronc**, de la même couleur dans les quatre images.
Si un calcul de `Gabarits.Ancrage` diffère de ce tableau, c'est le code qui fait foi :
corriger le tableau.

## Les marques — `Heros/marques/{assise}-s{stade}.png`

Même cadre que le corps de son stade ; ne peint **que sur le corps**. Une marque par assise
traversée, empilées dans l'ordre des assises. GDD §10.3 prévoit deux variantes (acclimatation
complète ou interrompue) : le noyau ne les suit pas encore, une seule est livrée.

| Assise | Ancrage | Teinte du corps | Lumière |
|---|---|---|---|
| noue | Branchies | — | — |

## Les espèces — `Especes/{id}-i{0,1}.png`

Deux images de nage, regard à droite. Longueur `7 + min(5, rang / 4)` : 7 px (rang 0) à
12 px (rang 20) ; cadre `(longueur + 2) × (2·⌈0,2·longueur⌉ + 5)`. Livrées : vairon, loche
(7 px, cadre 9 × 9).

## Le décor — `Fonds/{assise}/`

| Fichier | Taille | Rôle |
|---|---|---|
| `fond.png` | 32 × 56 | une bande d'eau, répétée en largeur ; la vase en bas |
| `rayons.png` | 256 × 112 | les rayons du jour sur les deux premières bandes, tramés |
| `berge.png` | 256 × 56 | les racines qui pendent de la première bande (si l'assise a une berge) |

## L'eau — `Eau/`

`voile.png` (8 × 8, damier blanc, teinté au rendu) ; `eclat.png` (2 × 2, blanc).

## Palettes

- **Héros** : `#C49C5C` corps, `#8C683A` dos, `#E2CE96` ventre, `#121C1A` contour ;
  marque de la Noue `#5E4426`.
- **Espèces** : vairon `#96AAA0` `#566C64` `#C8D4CC` ; loche `#A08A5E` `#6A5A3A` `#CCB88A` ;
  contour `#121C1A`.
- **Noue** : eau `#4A8070` `#34645C` `#244A48`, vase `#28221A`, racines `#3A2C20` `#5C4630`,
  rayon `#96BE96`.
- **Eau** : blanc `#FFFFFF`.
```

Vérifier les valeurs du tableau des ancrages contre le code : écrire un test jetable, ou relire `Ancrage` à la main pour les stades 0 et 3. S'il y a un écart, corriger le tableau.

- [ ] **Step 4: Le GDD, la spec du 2026-09-17, la roadmap, le README**

Dans `docs/GDD.md`, section **15.2 Registre**, ajouter à la fin :

```markdown
Amendement du 2026-10-02 (spec DA pixel art) : les assises profondes peuvent être des milieux
élémentaires — lave, électricité —, toujours rendus par des phénomènes réels : volcanisme
sous-marin sans flamme (coulées en coussins, fumeurs, eau qui rougeoie), bioélectricité
(organes électriques, décharges). « Dominante vitale », « pas magique » et « pas de combustion
sous l'eau » restent la règle. Chaque milieu laisse son adaptation sur le corps du héros
(§15.1) : une marque, une teinte, une lumière.
```

Dans `docs/superpowers/specs/2026-09-17-axe-heros-benedictions-scene-design.md`, sous **[D12]**, ajouter : `> Remplacé le 2026-10-02 par les quatre stades dessinés de la spec DA pixel art (§3) : 21 / 28 / 42 / 63 px aux niveaux 1 / 4 / 16 / 256.`

Dans `docs/ROADMAP.md`, mettre « Tenue à jour le » au 2026-10-02, et réécrire **Où on en est** : le jeu est dans Unity (le web archivé dans `archive/web/`), la Noue est en pixel art avec des sprites provisoires ; ajouter en tête des chantiers les sous-projets DA restants (habillage hybride de l'UI, vrai dessin, assises II à VI et leurs milieux) sans retirer les chantiers existants.

Dans `README.md`, section **État**, remplacer la phrase sur la coupe au trait par : « La scène est en **pixel art** (URP 2D, ≈240 px de large, lumières 2D) avec des sprites **provisoires** générés par script ; le cahier des charges du vrai dessin est `docs/da/gabarits.md`. » Ajouter aux commandes :

```sh
outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer   # les sprites provisoires
outils/unity.sh captures                                              # PNG de contrôle dans Logs/captures/
```

- [ ] **Step 5: Vérification finale**

Run: `outils/unity.sh tests EditMode` ; `outils/unity.sh tests PlayMode` ; `outils/unity.sh captures`
Expected: tout vert ; 16 captures. Les montrer à l'utilisateur pour la relecture (critère de réussite n° 3) : portrait et paysage, niveaux 1, 4, 16, 256, avec et sans voile.

- [ ] **Step 6: Commit**

```bash
git add Assets/IdlePond docs README.md
git commit -m "docs(da): cahier des gabarits, amendement du registre au GDD, roadmap et README"
```
