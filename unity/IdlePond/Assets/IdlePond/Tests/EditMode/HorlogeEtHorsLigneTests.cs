/*
 * L'horloge (§10) et le crédit hors ligne. Port de `tests/horloge.test.ts` et
 * `tests/hors-ligne.test.ts`.
 *
 * Le compteur Entretien ne lit que les heures CRÉDITÉES, jamais le temps
 * écoulé : c'est la seule protection anti-triche nécessaire dans tout le jeu.
 */
using IdlePond.Adaptateurs;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class HorlogeTests
    {
        private const double H = 3600_000;

        [Test]
        public void CrediteLeTempsEcouleTantQuIlResteSousLePlafond()
        {
            Assert.AreEqual(2 * 3600, Horloge.SecondesHorsLigneCreditees(0, 2 * H, 6));
        }

        [Test]
        public void PlafonneLAvanceUneHorlogePousseeNeRapportePasPlus()
        {
            Assert.AreEqual(6 * 3600, Horloge.SecondesHorsLigneCreditees(0, 40 * H, 6));
            Assert.AreEqual(24 * 3600, Horloge.SecondesHorsLigneCreditees(0, 400 * H, 24));
        }

        [Test]
        public void IgnoreLeReculDHorlogePlutotQueDeRetirerQuoiQueCeSoit()
        {
            Assert.AreEqual(0, Horloge.SecondesHorsLigneCreditees(10 * H, 2 * H, 6));
            Assert.AreEqual(0, Horloge.SecondesHorsLigneCreditees(10 * H, 10 * H, 6));
        }

        [Test]
        public void LePlafondResteEntreSixEtVingtQuatreHeures()
        {
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial * 3600, Horloge.CapHorsLigneSecondes(1));
            Assert.AreEqual(Constantes.CapHorsLigneHeuresMaximum * 3600, Horloge.CapHorsLigneSecondes(999));
        }

        [Test]
        public void LHorlogeFigeeRendLesTestsDeterministes()
        {
            var horloge = new HorlogeFigee(1000);
            Assert.AreEqual(1000, horloge.MaintenantMs());
            horloge.AvancerMs(500);
            Assert.AreEqual(1500, horloge.MaintenantMs());
        }

        [Test]
        public void LaGraineDUneNouvellePartieEstLHeureModuloDeuxPuissanceTrenteDeux()
        {
            // `Date.now() >>> 0`, à l'identique du web.
            Assert.AreEqual(1234u, Horloge.GrainePourNouvellePartie(new HorlogeFigee(4294967296d + 1234)));
        }
    }

    [TestFixture]
    public sealed class HorsLigneTests
    {
        private const double H = 3600_000;

        [Test]
        public void UnSeulAppelATickSuffitPourTouteLAbsence()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.CrediterHorsLigne(depart, 0, 3 * H);
            Assert.AreEqual(3 * 3600, credit.SecondesCreditees);
            var sansCompteur = credit.Etat with { Permanent = credit.Etat.Permanent with { HeuresHorsLigneCreditees = 0 } };
            OutilsDeTest.ComparerAToleranceFlottante(sansCompteur, Reducteur.Tick(depart, 3 * 3600), 0);
        }

        [Test]
        public void LePlafondBorneCeQueLAbsenceRapporteIlNeRetireRien()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.CrediterHorsLigne(depart, 0, 40 * H);
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial * 3600, credit.SecondesCreditees);
            Assert.IsTrue(credit.Etat.Cycle.ManaCourant.Gte(depart.Cycle.ManaCourant));
        }

        [Test]
        public void LeCompteurEntretienNeLitQueLesHeuresCreditees()
        {
            var depart = EtatDeTravail.Creer();
            var honnete = HorsLigne.CrediterHorsLigne(depart, 0, 3 * H);
            var tricheur = HorsLigne.CrediterHorsLigne(depart, 0, 400 * H);
            Assert.AreEqual(3, honnete.Etat.Permanent.HeuresHorsLigneCreditees, 5e-7);
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial, tricheur.Etat.Permanent.HeuresHorsLigneCreditees, 5e-7);
        }

        [Test]
        public void UnReculDHorlogeNeCrediteRienEtNeRetireRien()
        {
            var depart = EtatDeTravail.Creer();
            var credit = HorsLigne.CrediterHorsLigne(depart, 10 * H, 2 * H);
            Assert.AreEqual(0, credit.SecondesCreditees);
            Assert.AreSame(depart, credit.Etat);
        }

        [Test]
        public void LePlafondDemarreASixHeuresAvantTouteTechnique()
        {
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial, HorsLigne.CapHorsLigneCourantHeures(EtatDeTravail.Creer()));
        }
    }
}
