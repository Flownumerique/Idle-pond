using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ReducteurTests
    {
        [Test, Description("un acte impossible rend le même état")]
        public void Un_acte_impossible_rend_le_meme_etat()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Reducteur.Debloquer(etat, "inconnue"), Is.SameAs(etat));
            Assert.That(Reducteur.Ameliorer(etat, "inconnue"), Is.SameAs(etat));
            Assert.That(Reducteur.AcheterUneAmelioration(etat, "inconnue"), Is.SameAs(etat));
            var pauvre = Reducteur.EtatInitial(1);
            pauvre = pauvre with { Cycle = pauvre.Cycle with { ManaCourant = Decimal.Zero } };
            Assert.That(Reducteur.Creuser(pauvre), Is.SameAs(pauvre));
            Assert.That(Reducteur.Grandir(pauvre), Is.SameAs(pauvre));
        }

        [Test, Description("un dt non positif ou NaN ne fait rien")]
        public void Un_dt_non_positif_ou_NaN_ne_fait_rien()
        {
            var etat = EtatDeTravail.Creer();
            foreach (var dt in new[] { 0.0, -1.0, double.NaN, double.NegativeInfinity })
            {
                var resultat = Reducteur.TickDetaille(etat, dt);
                Assert.That(resultat.Etat, Is.SameAs(etat), dt.ToString(System.Globalization.CultureInfo.InvariantCulture));
                Assert.That(resultat.Declenches, Is.Empty);
            }
        }
    }
}
