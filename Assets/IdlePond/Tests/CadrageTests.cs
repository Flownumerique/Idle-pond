using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// Le rendu pixel (spec DA §2) : un facteur entier, une texture qui tient dans #scene, et
    /// une caméra qui garde visible la bande du héros.
    public class CadrageTests
    {
        [Test, Description("k est l'entier le plus proche de largeur / 360, et la texture tient dans le cadre")]
        public void Le_facteur_est_entier_et_la_texture_tient()
        {
            Assert.That(Cadrage.Dimensions(1080, 768), Is.EqualTo(new DimensionsDuRendu(3, 360, 256)));
            Assert.That(Cadrage.Dimensions(1600, 430), Is.EqualTo(new DimensionsDuRendu(4, 400, 107)));
            Assert.That(Cadrage.Dimensions(360, 170), Is.EqualTo(new DimensionsDuRendu(1, 360, 170)));
            Assert.That(Cadrage.Dimensions(1440, 3120), Is.EqualTo(new DimensionsDuRendu(4, 360, 780)), "un téléphone de 1440 px de large");
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
            var portrait = new DimensionsDuRendu(3, 360, 256);
            Assert.That(Cadrage.Cadrer(portrait, 2), Is.EqualTo(new ChampDeCamera(0, 0, 360, 256)));
            var six = Cadrage.Cadrer(portrait, 6);
            Assert.That(six, Is.EqualTo(new ChampDeCamera(0, 256 - 504, 360, 256)));
            Assert.That(six.Bas, Is.EqualTo(-504));
            // Un cadre plus étroit que la largeur visée reste centré sur elle.
            Assert.That(Cadrage.Cadrer(new DimensionsDuRendu(5, 216, 153), 1).Gauche, Is.EqualTo(72));
        }

        [Test, Description("la roche sous l'eau : la coupe part de la surface tant que l'eau creusée tient dans le cadre")]
        public void La_roche_remplit_le_dessous_sans_cacher_l_eau_creusee()
        {
            var portrait = new DimensionsDuRendu(3, 360, 256);
            // Une bande d'eau, cinq de roche : 504 px, plus que le cadre ; l'eau tient, on part du haut.
            Assert.That(Cadrage.Cadrer(portrait, 1, 6), Is.EqualTo(new ChampDeCamera(0, 0, 360, 256)));
            // Quatre bandes d'eau (336 px) ne tiennent plus : le bas de l'eau creusée reste en bas.
            Assert.That(Cadrage.Cadrer(portrait, 4, 6), Is.EqualTo(new ChampDeCamera(0, 256 - 336, 360, 256)));
            // Sans roche, le cadrage d'avant.
            Assert.That(Cadrage.Cadrer(portrait, 6, 6), Is.EqualTo(Cadrage.Cadrer(portrait, 6)));
        }

        [Test, Description("les bandes visibles : celles que le champ touche, et aucune s'il n'y en a pas")]
        public void Les_bandes_visibles()
        {
            var portrait = new DimensionsDuRendu(3, 360, 256);
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 6), 6), Is.EqualTo((2, 5)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 2), 2), Is.EqualTo((0, 1)));
            Assert.That(Cadrage.BandesVisibles(Cadrage.Cadrer(portrait, 0), 0), Is.EqualTo((0, -1)));
        }
    }
}
