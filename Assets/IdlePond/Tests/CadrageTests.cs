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
            // Quatre bandes d'eau (336 px) ne tiennent plus : sous le bas de l'eau creusée, une
            // rive de roche d'une demi-bande, pour voir ce qui vient.
            Assert.That(Cadrage.Cadrer(portrait, 4, 6), Is.EqualTo(new ChampDeCamera(0, 256 - 336 - Cadrage.RIVE_DE_ROCHE, 360, 256)));
            // Sans roche, le cadrage d'avant.
            Assert.That(Cadrage.Cadrer(portrait, 6, 6), Is.EqualTo(Cadrage.Cadrer(portrait, 6)));
        }

        [Test, Description("voir venir (2026-11-25) : la Noue au complet laisse voir la roche du Gour, sans couper la bande du héros")]
        public void La_rive_montre_le_lieu_suivant_sans_couper_le_heros()
        {
            Assert.That(Cadrage.RIVE_DE_ROCHE, Is.EqualTo(Gabarits.HAUTEUR_DE_BANDE / 2));
            var portrait = new DimensionsDuRendu(3, 360, 256);
            // Six bandes d'eau, la septième de roche (le premier palier du Gour) : sa moitié haute se voit.
            var champ = Cadrage.Cadrer(portrait, 6, 7);
            Assert.That(champ.Bas, Is.EqualTo(-6 * Gabarits.HAUTEUR_DE_BANDE - Cadrage.RIVE_DE_ROCHE));
            // Moins de roche dessinée que la rive : on n'en montre pas plus qu'il n'y en a.
            Assert.That(Cadrage.Cadrer(portrait, 4, 4).Bas, Is.EqualTo(-4 * Gabarits.HAUTEUR_DE_BANDE));
            // Un cadre trop bas pour la bande du héros ET la rive : la bande du héros d'abord.
            var bas = new DimensionsDuRendu(3, 360, 100);
            Assert.That(Cadrage.Cadrer(bas, 6, 7).Bas, Is.EqualTo(-6 * Gabarits.HAUTEUR_DE_BANDE - (100 - Gabarits.HAUTEUR_DE_BANDE)));
            // L'eau tient tout juste, la rive ne tient plus : la coupe reste accrochée en haut.
            var juste = new DimensionsDuRendu(3, 360, 2 * Gabarits.HAUTEUR_DE_BANDE);
            Assert.That(Cadrage.Cadrer(juste, 2, 3).Haut, Is.EqualTo(0));
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
