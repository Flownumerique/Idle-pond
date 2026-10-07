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
            SauvegardeDeTest.Rediriger();
            partie = new Partie(new HorlogeFigee(1_700_000_000_000L));
            ServicesDePartie.Installer(partie);
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        /// La Noue seule, ouverte jusqu'au fond : depuis le Gour (2026-10-07), les paliers
        /// livrés vont plus bas que ce que ces tests regardent.
        internal static readonly int PALIERS_DE_LA_NOUE = Assises.Toutes[0].NombreDePaliers;

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
                Cycle = e.Cycle with { PaliersOuverts = PALIERS_DE_LA_NOUE, NiveauDuHeros = niveauDuHeros, Especes = especes },
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
                scene = Object.FindAnyObjectByType<SceneDeLaMare>();
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
            Assert.That(bandes, Is.EqualTo(PALIERS_DE_LA_NOUE));
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
            Assert.That(scene.Eclairage.ParBande.Count, Is.EqualTo(PALIERS_DE_LA_NOUE));
            // Portrait : 153 px de haut, six bandes de 56 → les bandes 3, 4 et 5.
            var allumees = scene.Eclairage.ParBande.Select((l, i) => (l, i)).Where(x => x.l.enabled).Select(x => x.i).ToList();
            Assert.That(allumees, Is.EqualTo(new[] { 3, 4, 5 }));
            Assert.That(scene.Eclairage.ParBande[5].intensity, Is.LessThan(scene.Eclairage.ParBande[3].intensity));
        }

        [UnityTest]
        public IEnumerator Chaque_espece_nage_avec_l_effectif_de_son_niveau()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            // vairon niveau 40 → 1 + ⌊log₂ 40⌋ = 6 ; loche niveau 12 → 4.
            Assert.That(scene.Nageurs.Nombre, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator Le_voile_monte_quand_l_eau_se_trouble()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            scene.ImposerLaTaille(1080, 768);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return new WaitForSeconds(0.2f);
            Assert.That(scene.Voile.Opacite, Is.EqualTo(0f));
            partie.Remplacer(EtatDeLaNoue(1, true));
            yield return new WaitForSeconds(1.2f);
            Assert.That(scene.Voile.Opacite, Is.EqualTo(Voile.OPACITE).Within(1e-4));
        }

        [UnityTest]
        public IEnumerator Une_partie_chargee_n_a_pas_mue()
        {
            partie.Remplacer(EtatDeLaNoue(16, false));
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            yield return null;
            yield return null;
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(2));
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(0));
            Assert.That(scene.Heros.NombreDeMarques, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Le_heros_mue_en_grandissant_jamais_en_renaissant()
        {
            partie.Remplacer(EtatDeLaNoue(3, false));
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            yield return null;
            yield return null;
            partie.Remplacer(EtatDeLaNoue(4, false));
            yield return null;
            yield return null;
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(1), "le passage au niveau 4 est une mue");
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(1));
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            Assert.That(scene.Heros.MuesJouees, Is.EqualTo(1), "redevenir petit n'est pas une mue");
            Assert.That(scene.Heros.StadeAffiche, Is.EqualTo(0));
            Assert.That(scene.Heros.NombreDeMarques, Is.EqualTo(1), "la marque survit");
        }

        [UnityTest]
        public IEnumerator Un_nageur_hors_champ_n_est_pas_anime()
        {
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            // Portrait : six bandes ne tiennent pas, la première sort du cadre ; les nageurs
            // vivent dans les bandes 0 et 3, la seconde reste visible.
            scene.ImposerLaTaille(1080, 768);
            partie.Remplacer(EtatDeLaNoue(1, false));
            yield return null;
            yield return null;
            var (premiere, derniere) = Cadrage.BandesVisibles(scene.Rendu.Champ, PALIERS_DE_LA_NOUE);
            Assert.That(premiere, Is.GreaterThan(0), "la bande du haut doit être hors champ");
            int cache = -1, visible = -1;
            for (var i = 0; i < scene.Nageurs.Nombre; i++)
            {
                var b = scene.Nageurs.BandeDe(i);
                if (b < premiere && cache < 0) cache = i;
                if (b >= premiere && b <= derniere && visible < 0) visible = i;
            }
            Assert.That(cache, Is.GreaterThanOrEqualTo(0), "un nageur hors champ");
            Assert.That(visible, Is.GreaterThanOrEqualTo(0), "un nageur dans le champ");
            var avantCache = scene.Nageurs.PositionDe(cache);
            var avantVisible = scene.Nageurs.PositionDe(visible);
            yield return new WaitForSeconds(1.5f);
            Assert.That(scene.Nageurs.PositionDe(cache), Is.EqualTo(avantCache), "hors champ : immobile");
            Assert.That(scene.Nageurs.PositionDe(visible), Is.Not.EqualTo(avantVisible), "dans le champ : il nage");
        }

        [UnityTest]
        public IEnumerator L_eclair_de_la_mue_s_eteint_apres_son_flash()
        {
            partie.Remplacer(EtatDeLaNoue(3, false));
            SceneDeLaMare scene = null;
            yield return ChargerLaMare(s => scene = s);
            yield return null;
            partie.Remplacer(EtatDeLaNoue(4, false));
            yield return null;
            yield return null;
            Assert.That(scene.Heros.EclairAllume, Is.True, "l'éclair brille pendant la mue");
            yield return new WaitForSeconds(0.7f);
            Assert.That(scene.Heros.EclairAllume, Is.False, "puis la lumière est éteinte");
        }
    }
}
