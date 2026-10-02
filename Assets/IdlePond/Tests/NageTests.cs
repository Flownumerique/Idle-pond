using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// La nage (spec DA §4) : déterministe — même état, même image —, et jamais hors de sa
    /// bande ni du champ visé.
    public class NageTests
    {
        [Test, Description("même palier, même numéro, même trajet")]
        public void Le_trajet_est_deterministe()
        {
            Assert.That(Nage.TrajetDe(3, 7, 9), Is.EqualTo(Nage.TrajetDe(3, 7, 9)));
            Assert.That(Nage.TrajetDe(3, 7, 9), Is.Not.EqualTo(Nage.TrajetDe(3, 8, 9)));
        }

        [Test, Description("un nageur reste dans sa bande et dans la largeur visée, à tout instant")]
        public void Un_nageur_reste_dans_sa_bande()
        {
            for (var palier = 0; palier < 6; palier++)
                for (var numero = 0; numero < VueDeScene.POISSONS_MAX_PAR_GROUPE; numero++)
                    for (var longueur = 7; longueur <= 12; longueur++)
                    {
                        var t = Nage.TrajetDe(palier, numero, longueur);
                        var cadre = Gabarits.CadreDUnPoisson(longueur);
                        var bas = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE;
                        Assert.That(t.Y, Is.GreaterThanOrEqualTo(bas));
                        Assert.That(t.Y + cadre.Hauteur, Is.LessThanOrEqualTo(bas + Gabarits.HAUTEUR_DE_BANDE));
                        for (var s = 0.0; s < 20; s += 0.37)
                        {
                            var (x, _) = Nage.Position(t, s);
                            Assert.That(x, Is.GreaterThanOrEqualTo(0));
                            Assert.That(x + cadre.Largeur, Is.LessThanOrEqualTo(Gabarits.LARGEUR_VISEE));
                        }
                    }
        }

        [Test, Description("à l'aller il regarde à droite, au retour à gauche")]
        public void Le_sens_suit_le_mouvement()
        {
            var t = new Trajet(10, 0, 20, 2.0, 0.0);
            Assert.That(Nage.Position(t, 0.5).VersLaDroite, Is.True);
            Assert.That(Nage.Position(t, 2.5).VersLaDroite, Is.False);
            Assert.That(Nage.Position(t, 0).X, Is.EqualTo(10));
            Assert.That(Nage.Position(t, 2.0).X, Is.EqualTo(30));
        }
    }
}
