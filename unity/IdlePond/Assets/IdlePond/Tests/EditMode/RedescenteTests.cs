/*
 * Les deux puits de la descente, et le coût de conviction — GDD §4.1, §6.4,
 * §7.1. Port de `tests/redescente.test.ts`.
 *
 * « Un puits, un levier. » Ce que ces tests protègent n'est pas une valeur — `f`
 * est une graine — mais la FORME.
 */
using System;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class RedescenteTests
    {
        private static EtatJeu AyantDejaAtteint(int profondeur)
        {
            var etat = Reducteur.EtatInitial(1);
            return etat with { Permanent = etat.Permanent with { ProfondeurMaxAtteinte = profondeur } };
        }

        [Test]
        public void UnPalierJamaisAtteintSePaiePleinTarif()
        {
            var etat = AyantDejaAtteint(0);
            Assert.IsFalse(Economie.EstUnAmenagement(etat, 3));
            Assert.IsTrue(Economie.CoutDeDescente(etat, 3).Eq(Economie.CoutBaseDuPalier(3)));
        }

        [Test]
        public void UnPalierDejaAtteintDansUneViePasseeSePaieFFoisMoins()
        {
            var etat = AyantDejaAtteint(10);
            Assert.IsTrue(Economie.EstUnAmenagement(etat, 3));
            Assert.IsTrue(Economie.CoutDeDescente(etat, 3).Eq(Economie.CoutBaseDuPalier(3).Mul(Constantes.FFractionDAmenagement)));
        }

        [Test]
        public void LaFrontiereEstExactementLaProfondeurMaximaleAtteinte()
        {
            var etat = AyantDejaAtteint(10);
            Assert.IsTrue(Economie.EstUnAmenagement(etat, 9));
            Assert.IsFalse(Economie.EstUnAmenagement(etat, 10));
            Assert.IsFalse(Economie.EstUnAmenagement(etat, 11));
        }

        [Test]
        public void LaPremiereVieNeConnaitQueLeCreusement()
        {
            var neuf = Reducteur.EtatInitial(1);
            for (var palier = 0; palier < 8; palier += 1) Assert.IsFalse(Economie.EstUnAmenagement(neuf, palier));
        }

        private static readonly Banc BancDeTest = Paliers.Bancs[0];

        private static EtatJeu AvecDensite(double densite)
        {
            var etat = Reducteur.EtatInitial(1);
            return etat with
            {
                Permanent = etat.Permanent with
                {
                    Densites = etat.Permanent.Densites.Select((_, i) => i == BancDeTest.Palier ? densite : 0).ToArray(),
                },
            };
        }

        [Test]
        public void ADensiteNulleLaFormuleEstNeutre()
        {
            var etat = Reducteur.EtatInitial(1);
            Assert.AreEqual(0, etat.Permanent.Densites[BancDeTest.Palier]);
            Assert.IsTrue(Economie.CoutDeConviction(etat, BancDeTest).Gt(0));
        }

        [Test]
        public void UneEauDejaChargeeRendLeBancMoinsCherAReconvaincre()
        {
            var nu = Economie.CoutDeConviction(Reducteur.EtatInitial(1), BancDeTest);
            var memoire = Economie.CoutDeConviction(AvecDensite(99), BancDeTest);
            Assert.IsTrue(memoire.Lt(nu));
            var attendu = nu.Div(Math.Pow(1 + 99, Constantes.ExposantReconvictionDensite));
            Assert.Less(memoire.Sub(attendu).Abs().Div(attendu).ToNumber(), 1e-9);
        }

        [Test]
        public void ElleDecroitAvecLaDensiteSansJamaisAtteindreZero()
        {
            GrandNombre A(double densite) => Economie.CoutDeConviction(AvecDensite(densite), BancDeTest);
            Assert.IsTrue(A(10).Lt(A(0)));
            Assert.IsTrue(A(1e6).Lt(A(10)));
            Assert.IsTrue(A(1e30).Gt(0));
        }

        [Test]
        public void AucunAcquisNeSAjouteALaDensiteSurCeCout()
        {
            var etat = Reducteur.EtatInitial(1);
            var succes = TableOrdonnee<EntreeDeSucces>.Vide;
            foreach (var id in new[] { "a", "b", "c" }) succes = succes.Avec(id, new EntreeDeSucces(0, PalierDeVoix.Pente));
            var avecTout = etat with
            {
                Permanent = etat.Permanent with { NoeudsTechnique = new[] { "tout", "ce", "qui", "existe" }, Succes = succes },
            };
            Assert.IsTrue(Economie.CoutDeConviction(avecTout, BancDeTest).Eq(Economie.CoutDeConviction(etat, BancDeTest)));
        }
    }
}
