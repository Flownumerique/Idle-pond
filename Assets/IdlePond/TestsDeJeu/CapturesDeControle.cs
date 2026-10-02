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

            // Pas de capture d'écran ici : essayée (CaptureScreenshotAsTexture après
            // WaitForEndOfFrame, depuis une coroutine d'objet de jeu), elle rend une image vide
            // en batch. Le chemin réel de #scene (mesure, fond posé par l'interface) se
            // contrôle donc À LA MAIN, éditeur ouvert.

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

            // Une mue en cours : le niveau 3 s'installe, puis le 4 franchit le stade — l'éclair et
            // les écailles, 0,15 s après.
            scene.ImposerLaTaille(1080, 768);
            partie.Remplacer(ScenePixelTests.EtatDeLaNoue(3, false));
            yield return new WaitForSeconds(1.2f);
            partie.Remplacer(ScenePixelTests.EtatDeLaNoue(4, false));
            yield return new WaitForSeconds(0.15f);
            Ecrire(scene.Rendu, Path.Combine(dossier, "portrait-mue.png"));

            // La surface, deux paliers ouverts : la berge, les racines et les rayons, en haut de la section.
            scene.ImposerLaTaille(1080, 768);
            var surface = ScenePixelTests.EtatDeLaNoue(1, false);
            partie.Remplacer(surface with { Cycle = surface.Cycle with { PaliersOuverts = 2 } });
            yield return new WaitForSeconds(1.2f);
            Ecrire(scene.Rendu, Path.Combine(dossier, "portrait-surface.png"));
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
