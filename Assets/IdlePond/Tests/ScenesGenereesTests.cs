using System.IO;
using System.Linq;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Ce que les scènes générées portent vraiment, lu dans leur YAML. Du 2026-10-02 au
    /// 2026-10-07, le `UIDocument` de la Mare a été enregistré sans PanelSettings : l'interface
    /// n'avait aucun panneau, donc aucune taille, et la scène dessinée n'avait aucun cadre où
    /// s'afficher. L'écran restait vide, et les tests PlayMode, qui lisent des textes de
    /// labels, ne le voyaient pas.
    /// </summary>
    public class ScenesGenereesTests
    {
        static string[] ScenesAvecInterface() =>
            new[] { "Assets/IdlePond/Scenes/Mare.unity" }
                .Concat(Directory.Exists("Assets/IdlePond/Scenes/Ateliers")
                    ? Directory.GetFiles("Assets/IdlePond/Scenes/Ateliers", "*.unity").Select(p => p.Replace('\\', '/'))
                    : Enumerable.Empty<string>())
                .ToArray();

        [Test]
        public void Chaque_interface_generee_a_ses_PanelSettings()
        {
            var scenes = ScenesAvecInterface();
            Assert.That(scenes.Length, Is.GreaterThanOrEqualTo(5), "la Mare et ses quatre ateliers");
            var fautes = scenes.Where(p => File.ReadAllText(p).Contains("m_PanelSettings: {fileID: 0}")).ToList();
            Assert.That(fautes, Is.Empty, "UIDocument sans PanelSettings : relancer « IdlePond ▸ Générer les scènes »");
        }
    }
}
