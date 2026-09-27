/*
 * Les nombres du port : grands nombres, écriture JavaScript, expm1.
 *
 * La parité avec break_infinity.js est vérifiée sur la référence web
 * (`PariteAvecLeWebTests`) ; ceux-ci fixent les cas limites qu'une référence
 * générée ne couvre pas par hasard.
 */
using System;
using IdlePond.Nombres;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class NombresTests
    {
        [Test]
        public void UnNombreSEcritCommeStringDeJavaScript()
        {
            var cas = new (double Valeur, string Texte)[]
            {
                (0, "0"), (-0.0, "0"), (1, "1"), (-2.5, "-2.5"), (0.1, "0.1"), (1e21, "1e+21"), (1e20, "100000000000000000000"),
                (1.5e-7, "1.5e-7"), (0.000001, "0.000001"), (123456789.125, "123456789.125"), (5e-324, "5e-324"),
                (double.MaxValue, "1.7976931348623157e+308"), (double.NaN, "NaN"), (double.PositiveInfinity, "Infinity"),
            };
            foreach (var (valeur, texte) in cas) Assert.AreEqual(texte, NombreJs.VersTexte(valeur), "String(" + valeur + ")");
        }

        [Test]
        public void ToFixedArronditSurLaValeurBinaireExacte()
        {
            Assert.AreEqual("1.00", NombreJs.ToFixed(1.005, 2), "1,005 vaut 1,00499… en binaire");
            Assert.AreEqual("0.3", NombreJs.ToFixed(0.25, 1), "égalité exacte : vers le haut");
            Assert.AreEqual("-0.3", NombreJs.ToFixed(-0.25, 1));
            Assert.AreEqual("0.0", NombreJs.ToFixed(-0.01, 1), "zéro négatif s'écrit sans signe");
            Assert.AreEqual("1e+21", NombreJs.ToFixed(1e21, 2));
            Assert.AreEqual("12", NombreJs.ToFixed(11.5, 0));
        }

        [Test]
        public void ToExponentialDonneLesChiffresDeJavaScript()
        {
            Assert.AreEqual("1.23e+30", NombreJs.ToExponential(1.2345e30, 2));
            Assert.AreEqual("1.00e+3", NombreJs.ToExponential(999.9999, 2));
            Assert.AreEqual("5.00e-7", NombreJs.ToExponential(5e-7, 2));
            Assert.AreEqual("0.00e+0", NombreJs.ToExponential(0, 2));
        }

        [Test]
        public void Expm1GardeSesChiffresPresDeZero()
        {
            // La référence est la série entière près de zéro — `exp(x) - 1`, justement,
            // y perd ses chiffres — et la soustraction ailleurs, où elle est exacte.
            foreach (var x in new[] { 1e-300, 3e-4, -5e-6, 1e-10, 0.5, -3, 20 })
            {
                double attendu;
                if (Math.Abs(x) < 1e-2)
                {
                    double terme = x, somme = x;
                    for (var n = 2; n <= 10; n += 1)
                    {
                        terme *= x / n;
                        somme += terme;
                    }
                    attendu = somme;
                }
                else attendu = Math.Exp(x) - 1;
                Assert.AreEqual(attendu, MathJs.Expm1(x), Math.Abs(attendu) * 1e-14, "expm1(" + x + ")");
            }
            Assert.AreEqual(-1, MathJs.Expm1(double.NegativeInfinity));
        }

        [Test]
        public void UnGrandNombreSeLitEtSEcritCommeBreakInfinity()
        {
            Assert.AreEqual("1.2345e+30", GrandNombre.Lire("1.2345e+30").ToString());
            Assert.AreEqual("1e+21", GrandNombre.DepuisNombre(1e21).ToString());
            Assert.AreEqual("123456", GrandNombre.Lire("123456").ToString());
            Assert.AreEqual("12", GrandNombre.Lire("12abc").ToString(), "parseFloat lit le préfixe");
            // Sans « e », la bibliothèque passe par parseFloat et lève ; avec, elle
            // découpe la chaîne et rend NaN sans rien dire. Les deux sont reproduits.
            Assert.Throws<FormatException>(() => GrandNombre.Lire("abc"));
            Assert.IsTrue(GrandNombre.Lire("pas un nombre").EstNaN);
            Assert.IsTrue(GrandNombre.Lire("NaN").EstNaN);
        }

        [Test]
        public void DiviserParZeroRendZeroCommeLaBibliotheque()
        {
            // La bizarrerie reproduite : une mantisse non finie rend zéro.
            Assert.IsTrue(GrandNombre.DepuisNombre(5).Div(0).Eq(GrandNombre.Zero));
        }

        [Test]
        public void LesComparaisonsTiennentLeSigneEtLOrdreDeGrandeur()
        {
            GrandNombre a = 1e30, b = -1e30, c = 2;
            Assert.IsTrue(b.Lt(c) && c.Lt(a) && b.Lt(a));
            Assert.IsTrue(a.Gt(c) && !a.Lt(a) && a.Gte(a) && a.Lte(a));
            Assert.IsTrue(GrandNombre.Zero.Lt(c) && b.Lt(GrandNombre.Zero));
            Assert.IsTrue(GrandNombre.Max(a, c).Eq(a) && GrandNombre.Min(a, c).Eq(c));
        }

        [Test]
        public void AuDelaDuDoubleLeGrandNombreContinue()
        {
            var enorme = GrandNombre.Lire("1e400").Mul(GrandNombre.Lire("1e400"));
            Assert.AreEqual("1e+800", enorme.ToString());
            Assert.AreEqual(double.PositiveInfinity, enorme.ToNumber());
            Assert.IsTrue(enorme.Gt(GrandNombre.DepuisNombre(double.MaxValue)));
        }
    }
}
