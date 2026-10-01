using IdlePond.Jeu.UI;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// La seule décision de mise en page que le code prend : paysage si la largeur vaut au
    /// moins 1,2 fois la hauteur (spec §4, « Adaptation »). Le reste est dans le .uss.
    /// </summary>
    public class AdaptationTests
    {
        [Test, Description("1920 x 1080 est du paysage, 1080 x 1920 du portrait")]
        public void Les_deux_references_se_classent_comme_la_spec_le_dit()
        {
            Assert.That(Adaptation.EstPaysage(1920, 1080), Is.True);
            Assert.That(Adaptation.EstPaysage(1080, 1920), Is.False);
        }

        [Test, Description("le seuil est inclusif à 1,2 : une fenêtre presque carrée reste en portrait")]
        public void Le_seuil_est_inclusif_a_un_virgule_deux()
        {
            Assert.That(Adaptation.EstPaysage(1200, 1000), Is.True);
            Assert.That(Adaptation.EstPaysage(1199, 1000), Is.False);
            Assert.That(Adaptation.EstPaysage(1000, 1000), Is.False);
        }

        [Test, Description("une taille nulle ou illisible n'est pas du paysage")]
        public void Une_taille_illisible_n_est_pas_du_paysage()
        {
            Assert.That(Adaptation.EstPaysage(0, 0), Is.False);
            Assert.That(Adaptation.EstPaysage(1920, 0), Is.False);
            Assert.That(Adaptation.EstPaysage(double.NaN, 1080), Is.False);
        }
    }
}
