/*
 * IdlePond — la scène de build et les constructions.
 *
 * Le jeu n'a besoin d'aucune scène pour tourner dans l'éditeur (voir `Amorce`),
 * mais un build en demande une. `IdlePond ▸ Préparer la scène de jeu` la crée
 * — une scène vide et un objet `Jeu` — et l'inscrit dans les Build Settings.
 *
 * Les méthodes `Construire*` servent aussi à la CI, sans interface :
 *
 *   Unity -batchmode -quit -projectPath unity/IdlePond \
 *         -executeMethod IdlePond.Editeur.Construction.ConstruireWebGL
 */
using System;
using System.IO;
using System.Linq;
using IdlePond.Interface;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdlePond.Editeur
{
    public static class Construction
    {
        public const string CheminDeLaScene = "Assets/IdlePond/Scenes/LaNoue.unity";
        public const string NomDuProduit = "IdlePond";
        public const string Identifiant = "fr.flownumerique.idlepond";

        [MenuItem("IdlePond/Préparer la scène de jeu", priority = 60)]
        public static void PreparerLaScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/IdlePond/Scenes")) AssetDatabase.CreateFolder("Assets/IdlePond", "Scenes");

            if (!File.Exists(CheminDeLaScene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("IdlePond").AddComponent<Jeu>();
                EditorSceneManager.SaveScene(scene, CheminDeLaScene);
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != CheminDeLaScene).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(CheminDeLaScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            PlayerSettings.productName = NomDuProduit;
            PlayerSettings.companyName = "Flownumerique";
            foreach (var cible in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.WebGL })
            {
                PlayerSettings.SetApplicationIdentifier(cible, Identifiant);
            }
            // Un jeu idle se lit en portrait sur un téléphone.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            // La mare tourne quand la fenêtre n'a pas le focus.
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("IdlePond : scène prête (" + CheminDeLaScene + ") et inscrite dans les Build Settings.");
        }

        private static void Construire(BuildTarget cible, string sortie)
        {
            PreparerLaScene();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { CheminDeLaScene },
                locationPathName = sortie,
                target = cible,
                options = BuildOptions.None,
            };
            var rapport = BuildPipeline.BuildPlayer(options);
            var reussi = rapport.summary.result == BuildResult.Succeeded;
            Debug.Log("IdlePond : construction " + cible + " — " + rapport.summary.result + " (" + rapport.summary.totalSize + " octets).");
            if (Application.isBatchMode) EditorApplication.Exit(reussi ? 0 : 1);
            if (!reussi) throw new BuildFailedException("La construction " + cible + " a échoué.");
        }

        private static string Sortie(string plateforme, string fichier) =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Builds", plateforme, fichier);

        [MenuItem("IdlePond/Construire/WebGL", priority = 80)]
        public static void ConstruireWebGL() => Construire(BuildTarget.WebGL, Sortie("WebGL", ""));

        [MenuItem("IdlePond/Construire/Windows", priority = 81)]
        public static void ConstruireWindows() => Construire(BuildTarget.StandaloneWindows64, Sortie("Windows", NomDuProduit + ".exe"));

        [MenuItem("IdlePond/Construire/macOS", priority = 82)]
        public static void ConstruireMacOS() => Construire(BuildTarget.StandaloneOSX, Sortie("macOS", NomDuProduit + ".app"));

        [MenuItem("IdlePond/Construire/Linux", priority = 83)]
        public static void ConstruireLinux() => Construire(BuildTarget.StandaloneLinux64, Sortie("Linux", NomDuProduit + ".x86_64"));

        [MenuItem("IdlePond/Construire/Android", priority = 84)]
        public static void ConstruireAndroid() => Construire(BuildTarget.Android, Sortie("Android", NomDuProduit + ".apk"));

        [MenuItem("IdlePond/Construire/iOS (projet Xcode)", priority = 85)]
        public static void ConstruireIOS() => Construire(BuildTarget.iOS, Sortie("iOS", ""));
    }
}
