/*
 * Les paliers de voix et le registre figé — GDD §13.1 et §14.5. Port de
 * `tests/voix.test.ts`.
 *
 * « Une entrée est rédigée dans la langue que le héros avait au moment où il
 * l'a obtenue, et n'est jamais réécrite. »
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class VoixTests
    {
        private const double H = OutilsDeTest.H;

        private static EtatJeu ApresFranchissements(int n)
        {
            var etat = Reducteur.EtatInitial(1);
            for (var i = 0; i < n; i += 1) etat = Reducteur.Eclore(Reducteur.Tick(etat, 3 * H));
            return etat;
        }

        [Test]
        public void LaPenteAuDepartLesSignesAuPremierLesDirectivesAuTroisieme()
        {
            Assert.AreEqual(PalierDeVoix.Pente, Voix.PalierDeVoixApres(0));
            Assert.AreEqual(PalierDeVoix.Pente, Voix.PalierDeVoixApres(Constantes.FranchissementsPourLesSignes - 1));
            Assert.AreEqual(PalierDeVoix.Signes, Voix.PalierDeVoixApres(Constantes.FranchissementsPourLesSignes));
            Assert.AreEqual(PalierDeVoix.Signes, Voix.PalierDeVoixApres(Constantes.FranchissementsPourLesDirectives - 1));
            Assert.AreEqual(PalierDeVoix.Directives, Voix.PalierDeVoixApres(Constantes.FranchissementsPourLesDirectives));
        }

        [Test]
        public void LeDialogueResteInatteignable()
        {
            Assert.AreNotEqual(PalierDeVoix.Dialogue, Voix.PalierDeVoixApres(1000));
        }

        [Test]
        public void LaVoixNeRedescendJamais()
        {
            var precedent = -1;
            for (var n = 0; n < 20; n += 1)
            {
                var rang = (int)Voix.PalierDeVoixApres(n);
                Assert.GreaterOrEqual(rang, precedent);
                precedent = rang;
            }
        }

        [Test]
        public void VoixAuMoinsOrdonneLesPaliers()
        {
            Assert.IsTrue(Voix.VoixAuMoins(PalierDeVoix.Directives, PalierDeVoix.Signes));
            Assert.IsTrue(Voix.VoixAuMoins(PalierDeVoix.Signes, PalierDeVoix.Signes));
            Assert.IsFalse(Voix.VoixAuMoins(PalierDeVoix.Pente, PalierDeVoix.Signes));
        }

        [Test]
        public void UneEntreeObtenueSousLaPenteResteSousLaPente()
        {
            var etat = Reducteur.Tick(Reducteur.Convaincre(Reducteur.EtatInitial(1), Paliers.Bancs[0].Id), 1);
            var premiers = etat.Permanent.Succes.ToList();
            Assert.Greater(premiers.Count, 0);
            foreach (var paire in premiers)
            {
                Assert.AreEqual(PalierDeVoix.Pente, paire.Value.Registre);
                Assert.AreEqual(0, paire.Value.ObtenuAuCycle);
            }

            for (var i = 0; i < Constantes.FranchissementsPourLesDirectives; i += 1) etat = Reducteur.Eclore(Reducteur.Tick(etat, 3 * H));
            Assert.AreEqual(PalierDeVoix.Directives, Voix.PalierCourant(etat));

            foreach (var paire in premiers) Assert.AreEqual(paire.Value, etat.Permanent.Succes[paire.Key], "rien n'a été réécrit");
        }

        [Test]
        public void UneEntreeTombeePlusTardPorteLaLangueDeCeMomentLa()
        {
            var etat = ApresFranchissements(Constantes.FranchissementsPourLesSignes);
            Assert.AreEqual(PalierDeVoix.Signes, Voix.PalierCourant(etat));

            var avant = new HashSet<string>(etat.Permanent.Succes.Clefs);
            etat = Reducteur.Tick(Reducteur.Convaincre(etat, Paliers.Bancs[0].Id), 1);
            var nouveaux = etat.Permanent.Succes.Where(p => !avant.Contains(p.Key)).ToList();

            Assert.Greater(nouveaux.Count, 0);
            foreach (var paire in nouveaux)
            {
                Assert.AreEqual(PalierDeVoix.Signes, paire.Value.Registre);
                Assert.AreEqual(Constantes.FranchissementsPourLesSignes, paire.Value.ObtenuAuCycle);
            }
        }

        [Test]
        public void UnSuccesAcquisNeSeReobtientJamais()
        {
            var etat = Reducteur.Tick(Reducteur.Convaincre(Reducteur.EtatInitial(1), Paliers.Bancs[0].Id), 1);
            var premiere = etat.Permanent.Succes.ToList();
            etat = Reducteur.Eclore(Reducteur.Tick(etat, 3 * H));
            etat = Reducteur.Tick(Reducteur.Convaincre(etat, Paliers.Bancs[0].Id), 1);
            foreach (var paire in premiere) Assert.AreEqual(paire.Value, etat.Permanent.Succes[paire.Key]);
        }

        [Test]
        public void LaTableEstOrdonneeParLeRegistreJamaisParLArrivee()
        {
            var etat = Reducteur.Tick(EtatDeTravail.Creer(), 600);
            var obtenus = etat.Permanent.Succes.Clefs.ToList();
            Assert.Greater(obtenus.Count, 1);
            CollectionAssert.AreEqual(
                RegistreDesSucces.Liste.Where(s => etat.Permanent.Succes.Contient(s.Id)).Select(s => s.Id).ToList(),
                obtenus);
        }

        [Test]
        public void LOrdreDesEntreesNeDependPasDeLaTailleDuPas()
        {
            var depart = EtatDeTravail.Creer();
            var enUnPas = Reducteur.Tick(depart, 600);
            var parPetitsPas = depart;
            for (var i = 0; i < 600; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, 1);
            CollectionAssert.AreEqual(enUnPas.Permanent.Succes.Clefs, parPetitsPas.Permanent.Succes.Clefs);
        }
    }
}
