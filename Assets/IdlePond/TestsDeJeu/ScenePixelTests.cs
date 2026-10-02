using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
            Assert.That(scene.Rendu.Camera.targetTexture, Is.SameAs(scene.Rendu.Texture));
            // Sans carte graphique (-nographics), aucune texture ne se crée : les dimensions
            // et la caméra se vérifient quand même, la texture seulement avec un affichage.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            Assert.That(scene.Rendu.Texture.width, Is.EqualTo(216));
            Assert.That(scene.Rendu.Texture.height, Is.EqualTo(153));
            Assert.That(scene.Rendu.Texture.filterMode, Is.EqualTo(FilterMode.Point));
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
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
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
    }
}
