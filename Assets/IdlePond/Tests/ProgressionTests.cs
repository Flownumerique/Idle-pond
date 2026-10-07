using IdlePond.Jeu.UI;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// La part d'un prix déjà réunie, que les boutons d'achat montrent en se remplissant.
    public class ProgressionTests
    {
        [TestCase(0, 100, 0)]
        [TestCase(25, 100, 0.25)]
        [TestCase(100, 100, 1)]
        [TestCase(500, 100, 1)]
        public void La_part_va_de_zero_a_un(double reserve, double prix, double attendue)
        {
            Assert.That(Progression.Part(new Decimal(reserve), new Decimal(prix)), Is.EqualTo(attendue).Within(1e-9));
        }

        [Test]
        public void Un_prix_nul_est_deja_reuni()
        {
            Assert.That(Progression.Part(new Decimal(0), new Decimal(0)), Is.EqualTo(1));
        }

        [Test]
        public void Les_tres_grands_nombres_ne_debordent_pas()
        {
            Assert.That(Progression.Part(Decimal.Pow10(400), Decimal.Pow10(401)), Is.EqualTo(0.1).Within(1e-9));
        }
    }
}
