using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les gabarits du dessin (spec DA §5) : ce qu'un sprite, provisoire ou vrai, doit
    /// respecter pour prendre sa place sans code.
    /// </summary>
    public class GabaritsTests
    {
        [Test, Description("le cadre d'un corps : sa longueur plus le contour, et une hauteur impaire")]
        public void Le_cadre_d_un_corps()
        {
            Assert.That(Gabarits.CadreDuCorps(0), Is.EqualTo(new Cadre(23, 15)));
            Assert.That(Gabarits.CadreDuCorps(1), Is.EqualTo(new Cadre(30, 17)));
            Assert.That(Gabarits.CadreDuCorps(2), Is.EqualTo(new Cadre(44, 23)));
            Assert.That(Gabarits.CadreDuCorps(3), Is.EqualTo(new Cadre(65, 31)));
        }

        [Test, Description("une espèce mesure de 7 à 12 px selon son rang")]
        public void Une_espece_mesure_de_7_a_12_px()
        {
            Assert.That(Gabarits.LongueurDEspece(0), Is.EqualTo(7));
            Assert.That(Gabarits.LongueurDEspece(20), Is.EqualTo(12));
            Assert.That(Enumerable.Range(0, 21).Select(Gabarits.LongueurDEspece).All(l => l >= 7 && l <= 12), Is.True);
        }

        [Test, Description("chaque ancrage tombe dans le tronc, à chaque stade")]
        public void Chaque_ancrage_tombe_dans_le_tronc()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                var tronc = Gabarits.TroncDUnPoisson(Gabarits.LONGUEURS_DU_CORPS[stade]);
                foreach (AncrageId ancrage in System.Enum.GetValues(typeof(AncrageId)))
                {
                    var p = Gabarits.Ancrage(stade, ancrage);
                    Assert.That(tronc.Contient(p.X, p.Y), Is.True, $"stade {stade}, {ancrage} en ({p.X}, {p.Y})");
                }
            }
        }

        [Test, Description("la Noue : six paliers éclairés, la lumière baisse, et ne dépasse jamais 1 avec l'ambiante")]
        public void La_lumiere_de_la_Noue_baisse_et_reste_sous_1()
        {
            var noue = Assises.Toutes[0];
            var lumieres = Enumerable.Range(noue.IndexPremierPalier, noue.NombreDePaliers).Select(RegistreDArt.LumiereDuPalier).ToList();
            for (var i = 1; i < lumieres.Count; i++) Assert.That(lumieres[i], Is.LessThan(lumieres[i - 1]));
            Assert.That(lumieres.All(l => l > 0 && l + RegistreDArt.LUMIERE_AMBIANTE <= 1.0), Is.True);
        }

        [Test, Description("une assise sans dessin emprunte le décor de la Noue, plus sombre, sans erreur")]
        public void Une_assise_sans_dessin_emprunte_la_Noue_assombrie()
        {
            var profonde = Assises.Toutes[2].Id;
            var decor = RegistreDArt.DecorDe(profonde);
            Assert.That(decor.Assise, Is.EqualTo(profonde));
            Assert.That(decor.Berge, Is.False);
            Assert.That(RegistreDArt.LumiereDuPalier(Assises.Toutes[2].IndexPremierPalier),
                Is.LessThan(RegistreDArt.LumiereDuPalier(Assises.Toutes[0].IndexPremierPalier + 5)));
            Assert.That(() => RegistreDArt.LumiereDuPalier(61), Throws.Nothing);
            Assert.That(RegistreDArt.MarqueDe(profonde), Is.Null);
            Assert.That(RegistreDArt.MarqueDe(null), Is.Null);
        }

        [Test, Description("chaque assise dessinée a sa marque et sa courbe de lumière")]
        public void Chaque_assise_dessinee_a_sa_marque_et_sa_lumiere()
        {
            Assert.That(RegistreDArt.ASSISES_DESSINEES, Is.EqualTo(new[] { "noue" }));
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                Assert.That(RegistreDArt.MarqueDe(assise), Is.Not.Null, assise);
                Assert.That(RegistreDArt.DecorDe(assise).Lumieres.Count,
                    Is.EqualTo(Assises.Toutes.First(a => a.Id == assise).NombreDePaliers), assise);
            }
        }
    }
}
