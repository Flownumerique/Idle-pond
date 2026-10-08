using System;
using System.Collections;
using System.IO;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Les captures de l'INTERFACE par format (spec du 2026-10-08) : téléphone, tablette en
    /// portrait et en paysage, PC — la mare, le tiroir des espèces et celui des réglages.
    /// Le panneau est rendu dans une texture de la taille voulue (la capture d'écran rend une
    /// image vide en batch) : la scène dessinée n'y est pas, seulement l'interface.
    /// Lancées par `outils/unity.sh captures`, ignorées ailleurs.
    /// </summary>
    public class CapturesDeControleDesReglages
    {
        static readonly (string Nom, FormatDAffichage Format, int Largeur, int Hauteur)[] CADRES =
        {
            ("telephone", FormatDAffichage.Telephone, 1080, 2340),
            ("tablette-portrait", FormatDAffichage.Tablette, 1640, 2360),
            ("tablette-paysage", FormatDAffichage.Tablette, 2360, 1640),
            ("pc", FormatDAffichage.Pc, 1920, 1080),
        };

        static readonly (string Nom, Tiroir Tiroir)[] VUES =
        {
            ("mare", Tiroir.Aucun), ("especes", Tiroir.Especes), ("reglages", Tiroir.Reglages),
        };

        [UnityTest]
        public IEnumerator Ecrire_les_captures_des_formats()
        {
            if (Environment.GetEnvironmentVariable("IDLEPOND_CAPTURES") != "1") Assert.Ignore("captures : outils/unity.sh captures");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("captures : il faut un affichage (pas -nographics)");

            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            ServicesDePartie.Installer(new Partie(new HorlogeFigee(1_700_000_000_000L)));
            ServicesDePartie.DemanderLAccueil(premiereFois: false);
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;
            UIDocument document = null;
            for (var i = 0; i < 120 && document?.rootVisualElement?.Q<VisualElement>("racine") == null; i++)
            {
                yield return null;
                document = Object.FindAnyObjectByType<UIDocument>();
            }
            Assert.That(document, Is.Not.Null);
            var interfaceDeLaMare = document.GetComponent<RacineDeLInterface>();
            var magasin = Boucle.ObtenirOuCreerLesReglages();
            var panneau = document.panelSettings;

            var dossier = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
            Directory.CreateDirectory(dossier);
            var effacement = panneau.clearColor;
            var couleur = panneau.colorClearValue;
            panneau.clearColor = true;
            panneau.colorClearValue = new Color(0.04f, 0.09f, 0.09f, 1f);
            try
            {
                // D'abord l'accueil, dans chaque format ; puis on entre dans la mare.
                foreach (var cadre in CADRES)
                {
                    var rt = new RenderTexture(cadre.Largeur, cadre.Hauteur, 24, RenderTextureFormat.ARGB32);
                    panneau.targetTexture = rt;
                    magasin.Modifier(r => r with { Affichage = cadre.Format });
                    yield return new WaitForSeconds(0.6f);
                    Ecrire(rt, Path.Combine(dossier, $"ui-{cadre.Nom}-accueil.png"));
                    panneau.targetTexture = null;
                    Object.Destroy(rt);
                }
                yield return Doigt.Toucher(document.rootVisualElement.Q<VisualElement>("accueil-principal"));
                yield return new WaitForSeconds(0.6f);

                foreach (var cadre in CADRES)
                {
                    var rt = new RenderTexture(cadre.Largeur, cadre.Hauteur, 24, RenderTextureFormat.ARGB32);
                    panneau.targetTexture = rt;
                    magasin.Modifier(r => r with { Affichage = cadre.Format });
                    foreach (var vue in VUES)
                    {
                        interfaceDeLaMare.Ouvrir(vue.Tiroir);
                        yield return new WaitForSeconds(0.6f);
                        Ecrire(rt, Path.Combine(dossier, $"ui-{cadre.Nom}-{vue.Nom}.png"));
                    }
                    panneau.targetTexture = null;
                    Object.Destroy(rt);
                }
            }
            finally
            {
                panneau.targetTexture = null;
                panneau.clearColor = effacement;
                panneau.colorClearValue = couleur;
                ServicesDePartie.Oublier();
            }
        }

        static void Ecrire(RenderTexture rt, string chemin)
        {
            var precedente = RenderTexture.active;
            RenderTexture.active = rt;
            var lue = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            lue.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            lue.Apply();
            RenderTexture.active = precedente;
            File.WriteAllBytes(chemin, lue.EncodeToPNG());
            Object.Destroy(lue);
        }
    }
}
