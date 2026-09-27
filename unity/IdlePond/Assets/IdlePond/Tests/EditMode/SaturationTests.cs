/*
 * La saturation de la jauge — GDD §2.4. Port de `tests/saturation.test.ts`.
 *
 * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
 * partie. C'est la seule pénalité du jeu, et elle est douce. »
 */
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class SaturationTests
    {
        private const double H = OutilsDeTest.H;
        private const double Delai = Constantes.DelaiDeDivergenceNonChoisieHeures * H;

        private static EtatJeu JaugePleine()
        {
            var etat = EtatDeTravail.Creer();
            return etat with { Cycle = etat.Cycle with { ManaCourant = etat.Permanent.ContenanceMana } };
        }

        private static EtatJeu ParPetitsPas(EtatJeu depart, int nombre, double pas)
        {
            var etat = depart;
            for (var i = 0; i < nombre; i += 1) etat = Reducteur.Tick(etat, pas);
            return etat;
        }

        [Test]
        public void LEauSeTroubleAuSeuilEtPasAvant()
        {
            var etat = EtatDeTravail.Creer();
            var plafond = etat.Permanent.ContenanceMana;
            EtatJeu A(double part) => etat with { Cycle = etat.Cycle with { ManaCourant = plafond.Mul(part) } };
            Assert.IsFalse(Economie.EauTroublee(A(Constantes.SeuilDAlerteDeContenance - 0.01)));
            Assert.IsTrue(Economie.EauTroublee(A(Constantes.SeuilDAlerteDeContenance)));
            Assert.IsTrue(Economie.EauTroublee(A(1)));
        }

        [Test]
        public void LaPartDeContenanceNeDepasseJamaisUn()
        {
            var etat = JaugePleine();
            Assert.AreEqual(1, Economie.PartDeContenance(etat));
            Assert.IsTrue(Economie.EstSature(etat));
            Assert.AreEqual(1, Economie.PartDeContenance(Reducteur.Tick(etat, H)));
        }

        [Test]
        public void SatureLeStockNeMontePlus()
        {
            var etat = JaugePleine();
            Assert.IsTrue(Reducteur.Tick(etat, 4 * H).Cycle.ManaCourant.Eq(etat.Permanent.ContenanceMana));
        }

        [Test]
        public void LaDivergenceSeDeclencheSeuleAuBoutDuDelaiEtPasAvant()
        {
            var etat = JaugePleine();
            var avant = Reducteur.Tick(etat, Delai - H);
            Assert.AreEqual(0, avant.Permanent.NombreEclosions);
            Assert.IsFalse(Economie.DivergenceNonChoisieEstDue(avant));

            var apres = Reducteur.Tick(etat, Delai + H);
            Assert.AreEqual(1, apres.Permanent.NombreEclosions);
            Assert.AreEqual(0, apres.Cycle.SecondesEnSaturation);
            Assert.AreEqual(1, apres.Cycle.PaliersOuverts);
        }

        [Test]
        public void ElleFixeMoinsDAcquisQuUneEclosionChoisie()
        {
            var auBord = Reducteur.Tick(JaugePleine(), Delai - 1);
            var choisie = Reducteur.Eclore(auBord).Permanent.ContenanceMana;
            var subie = Reducteur.Tick(auBord, 2).Permanent.ContenanceMana;

            Assert.IsTrue(subie.Lt(choisie));
            Assert.IsTrue(subie.Gt(auBord.Permanent.ContenanceMana));

            var gainChoisi = choisie.Div(auBord.Permanent.ContenanceMana).Sub(1).ToNumber();
            var gainSubi = subie.Div(auBord.Permanent.ContenanceMana).Sub(1).ToNumber();
            Assert.AreEqual(Constantes.PartDAcquisFixeeParDivergenceNonChoisie, gainSubi / gainChoisi, 5e-7);
        }

        [Test]
        public void LeCompteurRepartAZeroDesQuOnDepense()
        {
            var presque = Reducteur.Tick(JaugePleine(), Delai - H);
            Assert.AreEqual(Delai - H, presque.Cycle.SecondesEnSaturation, 5e-7);

            var apresDepense = Reducteur.Creuser(presque);
            Assert.IsTrue(apresDepense.Cycle.ManaCourant.Lt(apresDepense.Permanent.ContenanceMana));

            var repris = Reducteur.Tick(apresDepense, 60).Cycle.SecondesEnSaturation;
            Assert.Less(repris, presque.Cycle.SecondesEnSaturation);
            Assert.LessOrEqual(repris, 60);
        }

        [Test]
        public void AucuneAbsenceMemeAuPlafondMaximalNeSuffitALaDeclencher()
        {
            Assert.AreEqual(0, Reducteur.Tick(JaugePleine(), Constantes.CapHorsLigneHeuresMaximum * H).Permanent.NombreEclosions);
        }

        [Test]
        public void ElleTombeAuMemeInstantQuelleQueSoitLaTailleDuPas()
        {
            var depart = JaugePleine();
            var total = Delai + 2 * H;
            var enUnPas = Reducteur.Tick(depart, total);
            var parPetitsPas = ParPetitsPas(depart, 300, total / 300);
            Assert.AreEqual(1, enUnPas.Permanent.NombreEclosions);
            Assert.AreEqual(1, parPetitsPas.Permanent.NombreEclosions);
            OutilsDeTest.ComparerAToleranceFlottante(parPetitsPas, enUnPas);
        }

        [Test]
        public void LInstantDeSaturationEstCoupeExactementMemeEnCoursDeRemplissage()
        {
            var etat = EtatDeTravail.Creer();
            var total = 6 * H;
            var aVide = etat with { Cycle = etat.Cycle with { ManaCourant = GrandNombre.Zero } };
            var produitSurLaFenetre = Reducteur.Tick(
                aVide with { Permanent = aVide.Permanent with { ContenanceMana = GrandNombre.Lire("1e300") } },
                total).Cycle.ManaCourant;
            var aMoitie = aVide with { Permanent = aVide.Permanent with { ContenanceMana = produitSurLaFenetre.Div(2) } };

            var enUnPas = Reducteur.Tick(aMoitie, total);
            var parPetitsPas = ParPetitsPas(aMoitie, 360, total / 360);

            Assert.Greater(enUnPas.Cycle.SecondesEnSaturation, 0);
            Assert.Less(enUnPas.Cycle.SecondesEnSaturation, total);
            OutilsDeTest.ComparerAToleranceFlottante(parPetitsPas, enUnPas);
        }
    }
}
