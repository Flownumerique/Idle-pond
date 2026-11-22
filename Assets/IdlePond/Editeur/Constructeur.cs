using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// Les builds de prototype, à tester hors de l'éditeur. Chacun va dans `Build/&lt;cible&gt;/`
    /// (jamais versionné) et prend les scènes des Build Settings, dans leur ordre.
    ///
    ///   outils/unity.sh build webgl     — à ouvrir dans le navigateur du téléphone
    ///   outils/unity.sh build windows
    ///   outils/unity.sh build android   — demande le module Android de Unity Hub
    /// </summary>
    public static class Constructeur
    {
        const string RACINE = "Build";

        [MenuItem("IdlePond/Build/WebGL (navigateur, téléphone)")]
        public static void WebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            // Le navigateur décompresse lui-même : n'importe quel serveur statique suffit,
            // sans en-tête `Content-Encoding` à régler.
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            // Le gabarit du projet (Assets/WebGLTemplates/IdlePond) : la toile prend toute la
            // fenêtre, sans zoom ni défilement, comme sur un téléphone.
            PlayerSettings.WebGL.template = "PROJECT:IdlePond";
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;            Construire(BuildTarget.WebGL, BuildTargetGroup.WebGL, Path.Combine(RACINE, "webgl"));
        }

        [MenuItem("IdlePond/Build/Windows")]
        public static void Windows() =>
            Construire(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone,
                Path.Combine(RACINE, "windows", "IdlePond.exe"));

        [MenuItem("IdlePond/Build/Android (APK)")]
        public static void Android()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "fr.flownumerique.idlepond");
            Construire(BuildTarget.Android, BuildTargetGroup.Android, Path.Combine(RACINE, "android", "IdlePond.apk"));
        }

        static void Construire(BuildTarget cible, BuildTargetGroup groupe, string sortie)
        {
            if (!BuildPipeline.IsBuildTargetSupported(groupe, cible))
                Echouer($"Le module {cible} n'est pas installé : l'ajouter dans Unity Hub (Installs ▸ 6000.6.3f1 ▸ Add modules).");

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) Echouer("Aucune scène dans les Build Settings : lancer « IdlePond ▸ Générer les scènes ».");

            var rapport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = sortie,
                target = cible,
                targetGroup = groupe,
                options = BuildOptions.None,
            });

            var resume = rapport.summary;
            Debug.Log($"[Constructeur] {cible} : {resume.result} — {resume.totalSize / (1024 * 1024)} Mo, " +
                      $"{resume.totalErrors} erreur(s), {resume.totalTime.TotalSeconds:F0} s → {sortie}");
            if (resume.result != BuildResult.Succeeded) Echouer($"Le build {cible} a échoué ({resume.result}).");
        }

        /// En batch, un code de sortie non nul ; dans l'éditeur, une exception lisible.
        static void Echouer(string message)
        {
            Debug.LogError("[Constructeur] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }
    }
}
