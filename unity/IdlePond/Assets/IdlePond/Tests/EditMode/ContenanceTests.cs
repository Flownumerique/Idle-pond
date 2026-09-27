/*
 * La contenance et l'acquis de séjour — amendement v1.1 §2.B. Port de
 * `tests/contenance.test.ts`.
 *
 * « Un cycle nominal multiplie la contenance par ≈47,1 (tolérance 2 %). » Ce
 * facteur n'est écrit nulle part dans le code de l'éclosion : il doit ÉMERGER
 * de `A∞` et `τ₀`.
 */
using System;
using System.Linq;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class ContenanceTests
    {
        private const double H = OutilsDeTest.H;

        [Test]
        public void UnCycleNominalLaMultipliePar47Virgule1()
        {
            var depart = Reducteur.EtatInitial(1);
            var apres = Reducteur.Eclore(Reducteur.Tick(depart, Constantes.DureeDuCycle1Heures * H));
            var rapport = apres.Permanent.ContenanceMana.Div(depart.Permanent.ContenanceMana).ToNumber();
            Assert.AreEqual(Constantes.ContenanceParEclosion, rapport, 0.5);
            Assert.Less(Math.Abs(rapport / Constantes.ContenanceParEclosion - 1), 0.02);
        }

        [Test]
        public void LaCibleDeriveeVautBienGPuissance4Virgule4()
        {
            Assert.AreEqual(47.1, Constantes.ContenanceParEclosion, 0.05);
        }

        [Test]
        public void LAcquisSatureVersAInfiniEtSonT90TombeADeuxHeures()
        {
            var depart = Reducteur.EtatInitial(1);
            var a90 = Reducteur.Tick(depart, 2 * H).Cycle.AcquisDeSejour;
            Assert.AreEqual(0.9, a90 / Constantes.AcquisMax, 0.005);
            var aLInfini = Reducteur.Tick(depart, 200 * H).Cycle.AcquisDeSejour;
            Assert.AreEqual(Constantes.AcquisMax, aLInfini, 0.0005);
        }

        [Test]
        public void ResterAuDelaDeLaSaturationNeRapportePlusDeProfondeur()
        {
            // Bornée par le délai du GDD §2.4 : au-delà il y aurait deux vies.
            var longSejour = (Constantes.DelaiDeDivergenceNonChoisieHeures - 8) * H;
            Assert.Greater(longSejour / H, 10 * Constantes.DureeDuCycle1Heures);

            var depart = Reducteur.EtatInitial(1);
            var nominal = Reducteur.Eclore(Reducteur.Tick(depart, Constantes.DureeDuCycle1Heures * H)).Permanent.ContenanceMana;
            var bienPlusLong = Reducteur.Eclore(Reducteur.Tick(depart, longSejour)).Permanent.ContenanceMana;

            Assert.AreEqual(0, Reducteur.Tick(depart, longSejour).Permanent.NombreEclosions, "aucune divergence subie");
            Assert.Less(bienPlusLong.Div(nominal).ToNumber(), 1.04);
        }

        [Test]
        public void LAcquisSeDepenseEntierementALEclosion()
        {
            var apres = Reducteur.Eclore(Reducteur.Tick(Reducteur.EtatInitial(1), 3 * H));
            Assert.AreEqual(0, apres.Cycle.AcquisDeSejour);
        }

        [Test]
        public void LaDensiteRaccourcitLeSejourElleNeLeRallongeJamais()
        {
            Assert.AreEqual(1, Densite.MultiplicateurDensite(0));
            Assert.AreEqual(1, Densite.MultiplicateurDensite(1));
            Assert.Greater(Densite.MultiplicateurDensite(10), 1);
            var depart = Reducteur.EtatInitial(1);
            var dense = depart with { Permanent = depart.Permanent with { Densites = depart.Permanent.Densites.Select(_ => 10.0).ToArray() } };
            Assert.Greater(Reducteur.Tick(dense, H).Cycle.AcquisDeSejour, Reducteur.Tick(depart, H).Cycle.AcquisDeSejour);
        }

        [Test]
        public void Tau0EstBienLeTempsCaracteristiqueADensiteNeutre()
        {
            var apres = Reducteur.Tick(Reducteur.EtatInitial(1), Constantes.TauSejourHeures * H);
            Assert.AreEqual(1 - Math.Exp(-1), apres.Cycle.AcquisDeSejour / Constantes.AcquisMax, 5e-7);
        }
    }
}
