using System;
using System.IO;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — le générateur de scènes. Les scènes ne sont jamais retouchées à la main :
    /// un `.unity` est du YAML qu'on ne relit pas, alors que ce fichier dit, en clair, ce
    /// qu'elles contiennent. Elles sont pourtant versionnées, avec leurs `.meta`, les
    /// réglages du panneau et les Build Settings : un clone neuf (et la CI) doit pouvoir
    /// jouer et lancer les tests PlayMode sans passer par ce menu. Relancé, il réécrit les
    /// mêmes scènes — les GUID, eux, ne changent pas, puisque les `.meta` existent déjà.
    ///
    /// Menu « IdlePond ▸ Générer les scènes », ou en batch :
    /// `Unity -batchmode -projectPath . -executeMethod IdlePond.Editeur.GenerateurDeScenes.Generer -quit`.
    /// </summary>
    public static class GenerateurDeScenes
    {
        const string DOSSIER_DES_SCENES = "Assets/IdlePond/Scenes";
        const string SCENE_DE_DEMARRAGE = DOSSIER_DES_SCENES + "/Demarrage.unity";
        const string SCENE_DE_LA_MARE = DOSSIER_DES_SCENES + "/Mare.unity";
        const string MISE_EN_PAGE = "Assets/IdlePond/Jeu/UI/Mare.uxml";
        const string REGLAGES_DU_PANNEAU = "Assets/IdlePond/Jeu/UI/PanelSettings.asset";
        /// Le thème d'exécution par défaut, s'il a été créé (« UI Toolkit ▸ Default Runtime Theme »).
        const string THEME_PAR_DEFAUT = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        /// L'interface vit dans le même assembly, mais elle est livrée à part : on la
        /// cherche par son nom, pour que ce générateur compile avant elle.
        const string TYPE_DE_L_INTERFACE = "IdlePond.Jeu.UI.RacineDeLInterface, IdlePond.Jeu";

        [MenuItem("IdlePond/Générer les scènes")]
        public static void Generer()
        {
            try
            {
                // En batch, personne ne répond à la question ; dans l'éditeur, on ne
                // referme pas la scène ouverte sur du travail non enregistré.
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

                Directory.CreateDirectory(DOSSIER_DES_SCENES);
                Directory.CreateDirectory(Path.GetDirectoryName(REGLAGES_DU_PANNEAU));
                AssetDatabase.Refresh();

                var reglages = CreerOuMettreAJourLesReglagesDuPanneau();
                GenererDemarrage();
                GenererLaMare(reglages);
                InscrireDansLesBuildSettings();

                AssetDatabase.SaveAssets();
                Debug.Log("IdlePond : scènes générées (Demarrage, Mare).");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                // En batch, un échec doit se voir au code de sortie.
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        /// <summary>
        /// L'échelle suit la taille de l'écran : une interface écrite pour 1080×1920 reste
        /// lisible sur un téléphone comme sur un moniteur. L'appariement à 0,5 partage
        /// également entre largeur et hauteur : le portrait ne s'écrase pas, le paysage non plus.
        /// L'asset existant est modifié en place, pas recréé : son GUID, que la scène
        /// référence, ne change pas.
        /// </summary>
        static PanelSettings CreerOuMettreAJourLesReglagesDuPanneau()
        {
            var reglages = AssetDatabase.LoadAssetAtPath<PanelSettings>(REGLAGES_DU_PANNEAU);
            if (reglages == null)
            {
                reglages = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(reglages, REGLAGES_DU_PANNEAU);
            }
            reglages.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            reglages.referenceResolution = new Vector2Int(1080, 1920);
            reglages.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            reglages.match = 0.5f;
            if (reglages.themeStyleSheet == null)
                reglages.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(THEME_PAR_DEFAUT);
            EditorUtility.SetDirty(reglages);
            return reglages;
        }

        /// `Demarrage.unity` : un objet `Amorce`, et rien d'autre.
        static void GenererDemarrage()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Amorce").AddComponent<Amorce>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_DE_DEMARRAGE))
                throw new InvalidOperationException("Impossible d'écrire " + SCENE_DE_DEMARRAGE);
        }

        static void GenererLaMare(PanelSettings reglages)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Fond eau-abysse : ce qu'on voit là où l'eau n'a pas encore de bande.
            var camera = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camera.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Eau();
            cam.transform.position = new Vector3(3.6f, -2f, -10f);
            camera.AddComponent<AudioListener>();

            var racine = new GameObject("SceneDeLaMare").AddComponent<SceneDeLaMare>();
            var serialise = new SerializedObject(racine);
            serialise.FindProperty("cameraDeScene").objectReferenceValue = cam;
            serialise.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("Boucle").AddComponent<Boucle>();

            var interfaceGo = new GameObject("Interface");
            var document = interfaceGo.AddComponent<UIDocument>();
            document.panelSettings = reglages;
            var miseEnPage = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MISE_EN_PAGE);
            if (miseEnPage != null) document.visualTreeAsset = miseEnPage;
            else Debug.LogWarning("IdlePond : " + MISE_EN_PAGE + " est absent, l'objet Interface est créé sans mise en page. Relancez le générateur une fois l'interface livrée.");

            var typeDeLInterface = Type.GetType(TYPE_DE_L_INTERFACE);
            if (typeDeLInterface != null) interfaceGo.AddComponent(typeDeLInterface);
            else Debug.LogWarning("IdlePond : " + TYPE_DE_L_INTERFACE + " est introuvable, la scène se jouera sans le contrôleur de l'interface.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, SCENE_DE_LA_MARE))
                throw new InvalidOperationException("Impossible d'écrire " + SCENE_DE_LA_MARE);
        }

        /// Demarrage en index 0 : c'est la scène qu'un build lance. Les scènes que le
        /// projet inscrirait par ailleurs sont gardées, après les nos.
        static void InscrireDansLesBuildSettings()
        {
            var nos = new[] { SCENE_DE_DEMARRAGE, SCENE_DE_LA_MARE };
            var autres = EditorBuildSettings.scenes.Where(s => !nos.Contains(s.path));
            EditorBuildSettings.scenes = nos
                .Select(chemin => new EditorBuildSettingsScene(chemin, true))
                .Concat(autres)
                .ToArray();
        }

        static Color Eau()
        {
            var rgb = Palette.EAU_ABYSSE;
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }
}
