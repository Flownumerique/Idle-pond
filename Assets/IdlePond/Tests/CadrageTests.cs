using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// Le rendu pixel (spec DA §2) : un facteur entier, une texture qui tient dans #scene, et
    /// une caméra qui garde visible la bande du héros.
    public class CadrageTests
    {
        [Test, Description("k est l'entier le plus proche de largeur / 240, et la texture tient dans le cadre")]
        public void Le_facteur_est_entier_et_la_texture_tient()
        {
            Assert.That(Cadrage.Dimensions(1080, 768), Is.EqualTo(new DimensionsDuRendu(5, 216, 153)));
            Assert.That(Cadrage.Dimensions(1600, 430), Is.EqualTo(new DimensionsDuRendu(7, 228, 61)));
            Assert.That(Cadrage.Dimensions(240, 170), Is.EqualTo(new DimensionsDuRendu(1, 240, 170)));
        }

        [Test, Description("un cadre étroit garde k = 1 ; un cadre vide ne rend rien, sans erreur")]
        public void Un_cadre_etroit_ou_vide()
        {
            Assert.That(Cadrage.Dimensions(100, 50), Is.EqualTo(new DimensionsDuRendu(1, 100, 50)));
            Assert.That(Cadrage.Dimensions(0, 300), Is.Null);
            Assert.That(Cadrage.Dimensions(300, 0), Is.Null);
            Assert.That(Cadrage.Dimensions(-4, -4), Is.Null);
        }

        [Test, Description("tout tient : la coupe s'accroche en haut ; sinon le bas de la coupe reste en bas du cadre")]
        public void La_camera_garde_la_bande_du_heros()
        {
            var portrait = new DimensionsDuRendu(5, 216, 153);
            Assert.That(Cadrage.Cadrer(portrait, 2), Is.EqualTo(new ChampDeCamera(12, 0, 216, 153)));
            var six = Cadrage.Cadrer(portrait, 6);
            Assert.That(six, Is.EqualTo(new ChampDeCamera(12, 153 - 336, 216, 153)));
            Assert.That(six.Bas, Is.EqualTo(-336));
        }

        [Test, Description("les bandes visibles : celles que le champ touche, et aucune s'il n'y en a pas")]
        public void Les_bandes_visibles()
        {
            var portrait = new DimensionsDuRendu(5, 216, 153);
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 6), 6), Is.EqualTo((3, 5)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 2), 2), Is.EqualTo((0, 1)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 0), 0), Is.EqualTo((0, -1)));
        }
    }
}
