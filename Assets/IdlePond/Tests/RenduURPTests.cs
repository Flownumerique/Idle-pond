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
