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
