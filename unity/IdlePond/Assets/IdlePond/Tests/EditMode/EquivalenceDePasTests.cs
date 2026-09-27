/*
 * Test d'équivalence de pas (§12, jalon v0.1). Port de
 * `tests/equivalence-de-pas.test.ts`.
 *
 * « 480 appels à dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la
 * tolérance flottante près. C'est le test qui garantit le hors ligne et le
 * simulateur d'un seul coup. »
 */
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class EquivalenceDePasTests
    {
        private const double HuitHeures = 8 * 3600;
        private const double Pas = 60;
        private const int NombreDePas = (int)(HuitHeures / Pas);

        private static EtatJeu ParPetitsPas(EtatJeu depart, int nombre, double pas)
        {
            var etat = depart;
            for (var i = 0; i < nombre; i += 1) etat = Reducteur.Tick(etat, pas);
            return etat;
        }

        [Test]
        public void QuatreCentQuatreVingtsPasDeSoixanteSecondesValentUnPasDeHuitHeures()
        {
            var depart = EtatDeTravail.Creer();
            OutilsDeTest.ComparerAToleranceFlottante(ParPetitsPas(depart, NombreDePas, Pas), Reducteur.Tick(depart, HuitHeures));
        }

        [Test]
        public void LaCadenceDeJeuACentMillisecondesVautElleAussiUnSeulPas()
        {
            var depart = EtatDeTravail.Creer();
            const double duree = 600;
            OutilsDeTest.ComparerAToleranceFlottante(ParPetitsPas(depart, (int)(duree * 10), 0.1), Reducteur.Tick(depart, duree));
        }

        [Test]
        public void LePlafonnementDuStockParLaContenanceComposeLuiAussi()
        {
            // Contenance basse : le mana sature en cours d'intervalle.
            var depart = EtatDeTravail.Creer(999, "1e5");
            var enUnPas = Reducteur.Tick(depart, HuitHeures);
            Assert.IsTrue(enUnPas.Cycle.ManaCourant.Eq(enUnPas.Permanent.ContenanceMana));
            Assert.IsTrue(enUnPas.Permanent.ManaAmbiant.Gt(0));
            OutilsDeTest.ComparerAToleranceFlottante(ParPetitsPas(depart, NombreDePas, Pas), enUnPas);
        }

        [Test]
        public void LeHorsLigneASixHeuresSeCrediteEnUnSeulAppel()
        {
            var depart = EtatDeTravail.Creer();
            OutilsDeTest.ComparerAToleranceFlottante(ParPetitsPas(depart, 6 * 60, 60), Reducteur.Tick(depart, 6 * 3600));
        }

        [Test]
        public void LEtatDeDepartProduitBienQuelqueChose()
        {
            Assert.IsTrue(Economie.ProductionTotaleParSeconde(EtatDeTravail.Creer()).Gt(0), "sinon le test ne prouve rien");
        }

        [Test]
        public void UnIntervalleQuiFranchitDesSeuilsComposeEncore()
        {
            // Le cas qui a cassé à l'amendement v1.1 §2.C, et le seul qui prouve la
            // partition analytique.
            var depart = Reducteur.EtatInitial(1);
            var banc = Paliers.Liste[0].Bancs[0];
            var vide = depart with
            {
                Cycle = depart.Cycle with
                {
                    ManaCourant = GrandNombre.Zero,
                    Bancs = TableOrdonnee<EtatBanc>.Vide.Avec(banc.Id, new EtatBanc(120, 0)),
                },
                Permanent = depart.Permanent with { ContenanceMana = GrandNombre.Lire("1e30") },
            };

            var parPetitsPas = ParPetitsPas(vide, NombreDePas, Pas);
            var enUnPas = Reducteur.Tick(vide, HuitHeures);

            Assert.Greater(parPetitsPas.Cycle.Bancs[banc.Id].Effectif, 100, "les seuils ont bien été traversés");
            Assert.AreEqual(1, Economie.MultiplicateurDeSeuil(vide.Cycle.Bancs[banc.Id].Effectif));
            OutilsDeTest.ComparerAToleranceFlottante(parPetitsPas, enUnPas);
        }

        [Test]
        public void UnDrapeauPermanentFranchiEnCoursDIntervalleComposeAussi()
        {
            // Le drapeau des cent individus est GLOBAL : il ne s'intègre pas banc par
            // banc, il coupe le pas.
            var depart = Reducteur.EtatInitial(1);
            var banc = Paliers.Liste[0].Bancs[0];
            var proche = depart with
            {
                Cycle = depart.Cycle with
                {
                    ManaCourant = GrandNombre.Zero,
                    Bancs = TableOrdonnee<EtatBanc>.Vide.Avec(banc.Id, new EtatBanc(300, 90)),
                },
                Permanent = depart.Permanent with { ContenanceMana = GrandNombre.Lire("1e30") },
            };

            var parPetitsPas = ParPetitsPas(proche, NombreDePas, Pas);
            var enUnPas = Reducteur.Tick(proche, HuitHeures);

            CollectionAssert.Contains(enUnPas.Permanent.EspecesAyantAtteintCent, banc.Espece);
            OutilsDeTest.ComparerAToleranceFlottante(parPetitsPas, enUnPas);
        }
    }
}
