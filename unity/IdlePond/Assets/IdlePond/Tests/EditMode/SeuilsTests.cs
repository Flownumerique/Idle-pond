/*
 * Les seuils — amendement v1.1 §2.C. Port de `tests/seuils.test.ts`.
 *
 * « Une espèce à 100 individus produit exactement ×16 sa base, et pose le
 * drapeau +3 % global. »
 */
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class SeuilsTests
    {
        private static readonly Banc BancDeTest = Paliers.Liste[0].Bancs[0];

        private static EtatJeu AvecEffectif(double effectif, int place = (int)Constantes.SeuilDuDrapeauPermanent)
        {
            var depart = Reducteur.EtatInitial(1);
            return depart with
            {
                Cycle = depart.Cycle with { Bancs = TableOrdonnee<EtatBanc>.Vide.Avec(BancDeTest.Id, new EtatBanc(place, effectif)) },
            };
        }

        [Test]
        public void LaTableEstCumuleeOnLitLeMultiplicateurDuSeuilFranchi()
        {
            Assert.AreEqual(1, Economie.MultiplicateurDeSeuil(0));
            Assert.AreEqual(1, Economie.MultiplicateurDeSeuil(9));
            Assert.AreEqual(2, Economie.MultiplicateurDeSeuil(10));
            Assert.AreEqual(2, Economie.MultiplicateurDeSeuil(24));
            Assert.AreEqual(4, Economie.MultiplicateurDeSeuil(25));
            Assert.AreEqual(8, Economie.MultiplicateurDeSeuil(50));
            Assert.AreEqual(8, Economie.MultiplicateurDeSeuil(99));
            Assert.AreEqual(16, Economie.MultiplicateurDeSeuil(100));
            Assert.AreEqual(16, Economie.MultiplicateurDeSeuil(10_000));
        }

        [Test]
        public void CentIndividusNeValentJamaisMilleVingtQuatre()
        {
            Assert.AreNotEqual(1024, Economie.MultiplicateurDeSeuil(100));
        }

        [Test]
        public void UneEspeceACentIndividusProduitExactementSeizeFoisSaBase()
        {
            var etat = AvecEffectif(Constantes.SeuilDuDrapeauPermanent);
            var baseNue = Economie.TauxBaseDuBanc(BancDeTest).Mul(Constantes.SeuilDuDrapeauPermanent);
            Assert.AreEqual(1, Economie.MultiplicateurDesDrapeaux(etat), "aucun drapeau posé tant que le tick n'a pas relu l'état");
            Assert.AreEqual(16, Economie.ProductionDuBanc(etat, BancDeTest).Div(baseNue).ToNumber(), 5e-10);
        }

        [Test]
        public void LeSeuilSeLitSurLEffectifPasSurLaPlaceAchetee()
        {
            var beaucoupDePlaceVide = AvecEffectif(9, 1000);
            Assert.AreEqual(1, Economie.ProductionDuBanc(beaucoupDePlaceVide, BancDeTest).Div(Economie.TauxBaseDuBanc(BancDeTest).Mul(9)).ToNumber(), 5e-10);
        }

        [Test]
        public void CentIndividusPosentLeDrapeauPermanentEtIlVautTroisPourCent()
        {
            var apres = Reducteur.Tick(AvecEffectif(Constantes.SeuilDuDrapeauPermanent), 0.1);
            CollectionAssert.Contains(apres.Permanent.EspecesAyantAtteintCent, BancDeTest.Espece);
            Assert.AreEqual(1 + Constantes.BonusGlobalACentIndividus, Economie.MultiplicateurDesDrapeaux(apres), 5e-10);
        }

        [Test]
        public void LeDrapeauSurvitALEclosionLeMultiplicateurDeSeuilNon()
        {
            var apres = Reducteur.Tick(AvecEffectif(Constantes.SeuilDuDrapeauPermanent), 0.1);
            var apresEclosion = Reducteur.Eclore(apres);
            CollectionAssert.Contains(apresEclosion.Permanent.EspecesAyantAtteintCent, BancDeTest.Espece);
            Assert.AreEqual(0, apresEclosion.Cycle.Bancs.Count);
            Assert.IsTrue(Economie.ProductionDuBanc(apresEclosion, BancDeTest).Eq(GrandNombre.Zero));
        }
    }
}
