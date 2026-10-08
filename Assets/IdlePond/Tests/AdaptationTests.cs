using IdlePond.Jeu;
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
    
        [Test, Description("sur mobile, sous 7 pouces de diagonale c'est un téléphone, à partir de 7 une tablette")]
        public void Sur_mobile_la_diagonale_decide()
        {
            // 414 x 552 à 100 dpi : 6,9 pouces ; 420 x 560 : 7,0 pouces.
            Assert.That(Adaptation.Detecter(414, 552, 100, true), Is.EqualTo(FormatDAffichage.Telephone));
            Assert.That(Adaptation.Detecter(420, 560, 100, true), Is.EqualTo(FormatDAffichage.Tablette));
            Assert.That(Adaptation.Detecter(1080, 2340, 400, true), Is.EqualTo(FormatDAffichage.Telephone));
            Assert.That(Adaptation.Detecter(1640, 2360, 264, true), Is.EqualTo(FormatDAffichage.Tablette));
        }

        [Test, Description("sans dpi lisible, la plus petite dimension décide, à 1200 px")]
        public void Sans_dpi_la_plus_petite_dimension_decide()
        {
            Assert.That(Adaptation.Detecter(1199, 2400, 0, true), Is.EqualTo(FormatDAffichage.Telephone));
            Assert.That(Adaptation.Detecter(2400, 1200, 0, true), Is.EqualTo(FormatDAffichage.Tablette));
            Assert.That(Adaptation.Detecter(1199, 2400, double.NaN, true), Is.EqualTo(FormatDAffichage.Telephone));
        }

        [Test, Description("hors mobile, c'est toujours un PC, même dans une petite fenêtre")]
        public void Hors_mobile_c_est_un_PC()
        {
            Assert.That(Adaptation.Detecter(1920, 1080, 96, false), Is.EqualTo(FormatDAffichage.Pc));
            Assert.That(Adaptation.Detecter(400, 700, 96, false), Is.EqualTo(FormatDAffichage.Pc));
        }

        [Test, Description("Auto prend le format détecté ; un format forcé l'emporte")]
        public void Auto_prend_le_detecte_un_format_force_l_emporte()
        {
            Assert.That(Adaptation.Resoudre(FormatDAffichage.Auto, FormatDAffichage.Tablette), Is.EqualTo(FormatDAffichage.Tablette));
            Assert.That(Adaptation.Resoudre(FormatDAffichage.Telephone, FormatDAffichage.Pc), Is.EqualTo(FormatDAffichage.Telephone));
            Assert.That(Adaptation.Resoudre(FormatDAffichage.Auto, FormatDAffichage.Auto), Is.EqualTo(FormatDAffichage.Telephone));
        }

        [Test, Description("l'échelle est celle du format, multipliée par la taille choisie")]
        public void L_echelle_est_celle_du_format_fois_la_taille()
        {
            Assert.That(Adaptation.FacteurDEchelle(FormatDAffichage.Telephone, 1), Is.EqualTo(1).Within(1e-9));
            Assert.That(Adaptation.FacteurDEchelle(FormatDAffichage.Tablette, 1.1), Is.EqualTo(1.1).Within(1e-9));
            Assert.That(Adaptation.FacteurDEchelle(FormatDAffichage.Pc, 1.2), Is.EqualTo(Adaptation.ECHELLE_PC * 1.2).Within(1e-9));
        }

        [Test, Description("chaque format a sa classe, et une seule")]
        public void Chaque_format_a_sa_classe()
        {
            Assert.That(Adaptation.ClasseDu(FormatDAffichage.Telephone), Is.EqualTo("telephone"));
            Assert.That(Adaptation.ClasseDu(FormatDAffichage.Tablette), Is.EqualTo("tablette"));
            Assert.That(Adaptation.ClasseDu(FormatDAffichage.Pc), Is.EqualTo("pc"));
            Assert.That(Adaptation.CLASSES_DE_FORMAT, Is.EquivalentTo(new[] { "telephone", "tablette", "pc" }));
        }
    }
}
