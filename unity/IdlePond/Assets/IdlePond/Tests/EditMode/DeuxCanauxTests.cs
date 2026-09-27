/*
 * Les deux canaux de captation et le plafond de maturation — GDD §3 et §3.0.
 * Port de `tests/deux-canaux.test.ts`.
 *
 * Ce que ces tests protègent avant tout : la somme reste ADDITIVE et le second
 * terme reste BORNÉ.
 */
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class DeuxCanauxTests
    {
        private const double H = OutilsDeTest.H;

        /// <summary>Une partie neuve, avec de quoi acheter.</summary>
        private static EtatJeu AvecDeQuoiAcheter()
        {
            var etat = Reducteur.EtatInitial(1);
            return etat with
            {
                Cycle = etat.Cycle with { ManaCourant = GrandNombre.Lire("1e9") },
                Permanent = etat.Permanent with { ContenanceMana = GrandNombre.Lire("1e12") },
            };
        }

        private static EtatJeu PalierPeuple(int places)
        {
            var etat = Reducteur.Convaincre(AvecDeQuoiAcheter(), Paliers.Bancs[0].Id);
            for (var i = 0; i < places; i += 1) etat = Reducteur.AcheterPlace(etat, Paliers.Bancs[0].Id);
            return etat;
        }

        [Test]
        public void LaCaptationTotaleEstExactementLaSommeDesDeux()
        {
            var etat = Reducteur.Tick(Reducteur.Convaincre(Reducteur.EtatInitial(1), Paliers.Bancs[0].Id), 600);
            var natif = Economie.ProductionDuBanc(etat, Paliers.Bancs[0]);
            var acclimate = Economie.ProductionAcclimateeDuPalier(etat, 0);
            Assert.IsTrue(natif.Gt(0));
            Assert.IsTrue(acclimate.Gt(0));
            var total = Economie.ProductionTotaleParSeconde(etat);
            Assert.Less(total.Sub(natif.Add(acclimate)).Abs().Div(total).ToNumber(), 1e-12);
        }

        [Test]
        public void LAcclimateNeDependDAucunVivantIlTombeSurUneEauVide()
        {
            var neuf = Reducteur.EtatInitial(1);
            Assert.AreEqual(0, neuf.Cycle.Bancs.Count);
            Assert.IsTrue(Economie.ProductionAcclimateeDuPalier(neuf, 0).Gt(0));
            Assert.IsTrue(Economie.ProductionTotaleParSeconde(neuf).Gt(0));
        }

        [Test]
        public void IlResteBorneTresBasFaceAUnNatifACentPourCent()
        {
            const int palier = 0;
            var plafond = Economie.TauxBaseDuPalier(palier).Mul(Constantes.IndividusEquivalentsDuCanalAcclimate);
            var neuf = Reducteur.EtatInitial(1);
            var sature = neuf with
            {
                Permanent = neuf.Permanent with
                {
                    Densites = neuf.Permanent.Densites.Select(_ => 1e30).ToArray(),
                    PartsMures = neuf.Permanent.PartsMures.Select(_ => 1.0).ToArray(),
                },
            };
            Assert.IsTrue(Economie.ProductionAcclimateeDuPalier(sature, palier).Lte(plafond));
            var millefois = sature with { Permanent = sature.Permanent with { Densites = sature.Permanent.Densites.Select(_ => 1e33).ToArray() } };
            Assert.IsTrue(Economie.ProductionAcclimateeDuPalier(millefois, palier).Eq(Economie.ProductionAcclimateeDuPalier(sature, palier)));
        }

        [Test]
        public void UneEauQueRienNHabiteEstMureEtLeReste()
        {
            var neuf = Reducteur.EtatInitial(1);
            Assert.AreEqual(Maturation.PartMureDUneEauIntouchee, Economie.PartMureDuPalier(neuf, 0));
            Assert.AreEqual(1, Economie.PartMureDuPalier(Reducteur.Tick(neuf, 100 * H), 0), 5e-7);
        }

        [Test]
        public void PeuplerUnPalierDiluSonTypeDoncEcraseSonRendementAcclimate()
        {
            var etat = PalierPeuple(30);
            Assert.Greater(Economie.PlaceDuPalier(etat, 0), Constantes.PlaceQuiDilueAMoitie);
            var avant = Economie.PartMureDuPalier(etat, 0);
            // Six temps caractéristiques : il reste e^-6 de l'écart initial.
            var apres = Economie.PartMureDuPalier(Reducteur.Tick(etat, 6 * Constantes.TauMaturationHeures * H), 0);
            Assert.Less(apres, avant);
            Assert.AreEqual(Maturation.CibleDeMaturation(Economie.PlaceDuPalier(etat, 0)), apres, 0.005);
        }

        [Test]
        public void LaisseMaigreLeTypeMuritEtLeRendementRemonte()
        {
            var etat = Reducteur.Tick(PalierPeuple(30), 4 * Constantes.TauMaturationHeures * H);
            var dilue = Economie.PartMureDuPalier(etat, 0);
            var apresEclosion = Reducteur.Tick(Reducteur.Eclore(etat), 4 * Constantes.TauMaturationHeures * H);
            Assert.Greater(Economie.PartMureDuPalier(apresEclosion, 0), dilue);
        }

        [Test]
        public void LaCibleNeDependQueDeLaPlaceEtVautLaMoitieAuSeuilNomme()
        {
            Assert.AreEqual(1, Maturation.CibleDeMaturation(0));
            Assert.AreEqual(0.5, Maturation.CibleDeMaturation(Constantes.PlaceQuiDilueAMoitie), 5e-13);
            Assert.Less(Maturation.CibleDeMaturation(1e9), 1e-6);
            Assert.Greater(Maturation.CibleDeMaturation(1e9), 0);
        }

        [Test]
        public void LaPartMureSurvitALEclosionCEstUneProprieteDeLEau()
        {
            var etat = Reducteur.Tick(PalierPeuple(30), 2 * Constantes.TauMaturationHeures * H);
            var avant = Economie.PartMureDuPalier(etat, 0);
            Assert.Less(avant, 1);
            Assert.AreEqual(avant, Economie.PartMureDuPalier(Reducteur.Eclore(etat), 0), 5e-13);
        }

        [Test]
        public void LArbitrageEstReelPeuplerGagneEnDebitEtPerdEnEauMure()
        {
            var duree = 4 * Constantes.TauMaturationHeures * H;
            var baseDeComparaison = Reducteur.Convaincre(AvecDeQuoiAcheter(), Paliers.Bancs[0].Id);
            var peuple = Reducteur.Tick(PalierPeuple(30), duree);
            var maigre = Reducteur.Tick(baseDeComparaison, duree);
            Assert.IsTrue(Economie.ProductionDuBanc(peuple, Paliers.Bancs[0]).Gt(Economie.ProductionDuBanc(maigre, Paliers.Bancs[0])));
            Assert.IsTrue(Economie.ProductionAcclimateeDuPalier(peuple, 0).Lt(Economie.ProductionAcclimateeDuPalier(maigre, 0)));
        }
    }
}
