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
                    for (var longueur = 7; longueur <= 30; longueur++)
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

        [Test, Description("un poisson de troupe, dans son cadre commun, reste dans sa bande et la largeur visée")]
        public void Un_poisson_de_troupe_reste_dans_sa_bande()
        {
            var cadre = Gabarits.CADRE_D_ESPECE;
            for (var palier = 0; palier < 18; palier++)
                for (var numero = 0; numero < VueDeScene.POISSONS_MAX_PAR_GROUPE; numero++)
                {
                    var t = Nage.TrajetDe(palier, numero, cadre);
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
    

        [Test, Description("dans la vasque du Gour, tout nage sous la membrane de mana, jamais dans l'eau de la Noue")]
        public void Rien_ne_nage_au_dessus_de_la_membrane()
        {
            const int VASQUE = 6;
            Assert.That(Lieux.ACassure(VASQUE), Is.True);
            var haut = -VASQUE * Gabarits.HAUTEUR_DE_BANDE;
            void SousLaMembrane(Trajet t, Cadre cadre, string qui)
            {
                for (var s = 0.0; s < 20; s += 0.37)
                {
                    var (x, _) = Nage.Position(t, s);
                    for (var px = x; px < x + cadre.Largeur; px++)
                        Assert.That(t.Y + cadre.Hauteur, Is.LessThanOrEqualTo(haut - Lieux.CreuxDeLaMembrane(px) - 5), $"{qui} à x = {px}");
                }
            }
            for (var numero = 0; numero < VueDeScene.POISSONS_MAX_PAR_GROUPE; numero++)
            {
                SousLaMembrane(Nage.TrajetDe(VASQUE, numero, Gabarits.CADRE_D_ESPECE), Gabarits.CADRE_D_ESPECE, $"poisson {numero}");
                SousLaMembrane(Nage.TrajetAccroche(VASQUE, numero, Gabarits.CADRE_D_ESPECE), Gabarits.CADRE_D_ESPECE, $"lamproie {numero}");
            }
            SousLaMembrane(Nage.TrajetDe(VASQUE, Tribus.NUMERO_DU_REPRESENTANT, Gabarits.CADRE_DE_REPRESENTANT), Gabarits.CADRE_DE_REPRESENTANT, "représentant");
            // Ailleurs, rien ne change : la marge ordinaire.
            Assert.That(Lieux.PlafondDeNage(VASQUE + 1, 0, Gabarits.LARGEUR_VISEE), Is.EqualTo(9));
        }
    }
}
