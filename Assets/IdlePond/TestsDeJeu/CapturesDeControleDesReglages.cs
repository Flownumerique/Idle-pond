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
            ("mare", Tiroir.Aucun), ("toi", Tiroir.Toi), ("especes", Tiroir.Especes), ("reglages", Tiroir.Reglages),
        };

        [UnityTest]
        public IEnumerator Ecrire_les_captures_des_formats()
        {
            if (Environment.GetEnvironmentVariable("IDLEPOND_CAPTURES") != "1") Assert.Ignore("captures : outils/unity.sh captures");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("captures : il faut un affichage (pas -nographics)");

            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            var partie = new Partie(new HorlogeFigee(1_700_000_000_000L));
            ServicesDePartie.Installer(partie);
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
                var entre = false;
                foreach (var cadre in CADRES)
                {
                    var rt = new RenderTexture(cadre.Largeur, cadre.Hauteur, 24, RenderTextureFormat.ARGB32);
                    panneau.targetTexture = rt;
                    magasin.Modifier(r => r with { Affichage = cadre.Format });
                    if (!entre)
                    {
                        // On entre dans la mare panneau posé sur sa texture : détaché, le doigt
                        // ne trouvait pas « Continuer », et l'accueil restait sur toutes les vues.
                        yield return new WaitForSeconds(0.6f);
                        yield return Doigt.Toucher(document.rootVisualElement.Q<VisualElement>("accueil-principal"));
                        yield return new WaitForSeconds(0.6f);
                        entre = true;
                    }
                    foreach (var vue in VUES)
                    {
                        interfaceDeLaMare.Ouvrir(vue.Tiroir);
                        yield return new WaitForSeconds(0.6f);
                        Ecrire(rt, Path.Combine(dossier, $"ui-{cadre.Nom}-{vue.Nom}.png"));
                        if (vue.Tiroir == Tiroir.Aucun) yield return CapturerUnToucher(document, rt, Path.Combine(dossier, $"ui-{cadre.Nom}-toucher.png"));
                    }
                    panneau.targetTexture = null;
                    Object.Destroy(rt);
                }

                // « Lire l'eau » appris, un pic de production : la pastille dit ce que rentrer vaudrait.
                {
                    var rt = new RenderTexture(CADRES[0].Largeur, CADRES[0].Hauteur, 24, RenderTextureFormat.ARGB32);
                    panneau.targetTexture = rt;
                    magasin.Modifier(r => r with { Affichage = CADRES[0].Format });
                    interfaceDeLaMare.Ouvrir(Tiroir.Aucun);
                    var etat = partie.Etat;
                    partie.Remplacer(etat with
                    {
                        Cycle = etat.Cycle with { ProductionPicParSeconde = IdlePond.Noyau.Decimal.Parse("1e6") },
                        Permanent = etat.Permanent with { NoeudsTechnique = new[] { "renaissance-lire-l-eau" } },
                    });
                    yield return new WaitForSeconds(0.6f);
                    Ecrire(rt, Path.Combine(dossier, $"ui-{CADRES[0].Nom}-lire-l-eau.png"));

                    // « Compter les siens » : une espèce convaincue, sa part en pente, puis son débit.
                    partie.Convaincre(IdlePond.Noyau.Donnees.Especes.Toutes[0].Id);
                    interfaceDeLaMare.Ouvrir(Tiroir.Especes);
                    yield return new WaitForSeconds(0.6f);
                    Ecrire(rt, Path.Combine(dossier, $"ui-{CADRES[0].Nom}-pente.png"));
                    partie.Remplacer(partie.Etat with
                    {
                        Permanent = partie.Etat.Permanent with { NoeudsTechnique = new[] { "renaissance-lire-l-eau", "entretien-compte" } },
                    });
                    yield return new WaitForSeconds(0.6f);
                    Ecrire(rt, Path.Combine(dossier, $"ui-{CADRES[0].Nom}-compte.png"));

                    // « La file » : deux creux demandés d'avance, sans de quoi les payer.
                    interfaceDeLaMare.Ouvrir(Tiroir.Aucun);
                    partie.Remplacer(partie.Etat with
                    {
                        Cycle = partie.Etat.Cycle with { ManaCourant = IdlePond.Noyau.Decimal.Zero, CreuxDemandes = 2 },
                        Permanent = partie.Etat.Permanent with { NoeudsTechnique = new[] { "creusement-file" } },
                    });
                    yield return new WaitForSeconds(0.6f);
                    Ecrire(rt, Path.Combine(dossier, $"ui-{CADRES[0].Nom}-file.png"));
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

        /// Trois touchers sur la mare, capturés en plein vol (spec du toucher, §2).
        static IEnumerator CapturerUnToucher(UIDocument document, RenderTexture rt, string chemin)
        {
            var couche = document.rootVisualElement.Q<VisualElement>("toucher");
            var cadre = couche.worldBound;
            foreach (var (x, y) in new[] { (0.3f, 0.3f), (0.6f, 0.4f), (0.45f, 0.55f) })
                yield return Doigt.ToucherEn(couche, new Vector2(cadre.x + cadre.width * x, cadre.y + cadre.height * y));
            yield return new WaitForSeconds(0.15f);
            Ecrire(rt, chemin);
            yield return new WaitForSeconds(0.8f);
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
