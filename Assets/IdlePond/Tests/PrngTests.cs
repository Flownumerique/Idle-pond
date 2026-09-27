using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class PrngTests
    {
        [Test, Description("le PRNG rend, bit pour bit, les suites du TypeScript")]
        public void Le_PRNG_rend_bit_pour_bit_les_suites_du_TypeScript()
        {
            foreach (var suite in References.Lire("prng")["suites"])
            {
                var prng = new EtatPrng((uint)(long)suite["graine"]);
                foreach (var tirage in suite["tirages"])
                {
                    var (valeur, suivant) = Prng.Tirer(prng);
                    Assert.That(valeur, Is.EqualTo(References.Double(tirage[0])), $"graine {suite["graine"]}");
                    Assert.That(suivant.Graine, Is.EqualTo((uint)(long)tirage[1]));
                    prng = suivant;
                }
            }
        }
    }
}
