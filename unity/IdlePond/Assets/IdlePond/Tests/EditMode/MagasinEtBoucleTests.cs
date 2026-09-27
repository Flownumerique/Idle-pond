/*
 * Le magasin et la boucle — ce que le port ajoute autour du noyau.
 *
 * La version web n'avait pas de test pour son store Zustand ni pour sa boucle :
 * `setInterval` et `localStorage` ne se testent pas sans navigateur. Ici les deux
 * passent par des interfaces (`IStockage`, `IHorloge`), et le contrat se vérifie
 * enfin : un acte est sauvegardé tout de suite, une absence est créditée en un
 * pas et plafonnée, et une pause de l'application ne court-circuite jamais le
 * plafond.
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Etat;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class MagasinTests
    {
        private const double H = 3600_000;
        private const double Depart = 1_700_000_000_000;

        private static (Magasin Magasin, StockageEnMemoire Stockage, HorlogeFigee Horloge) Nouveau()
        {
            var stockage = new StockageEnMemoire();
            var horloge = new HorlogeFigee(Depart);
            return (new Magasin(stockage, horloge), stockage, horloge);
        }

        [Test]
        public void UnePartieNeuveSortDeLOeufSurLeMondeLivre()
        {
            var (magasin, _, horloge) = Nouveau();
            Assert.AreEqual(Assises.PaliersLivres, magasin.Etat.LimiteDeContenu);
            Assert.AreEqual(Horloge.GrainePourNouvellePartie(horloge), magasin.Etat.Prng.Graine);
            Assert.AreEqual(Depart, magasin.DernierInstantMs);
        }

        [Test]
        public void UnActeEstSauvegardeTourDeSuite()
        {
            var (magasin, stockage, _) = Nouveau();
            Assert.IsNull(stockage.Lire(Magasin.ClefDeSauvegarde));
            magasin.Convaincre(Paliers.Bancs[0].Id);
            Assert.AreEqual(1, magasin.Etat.Cycle.Bancs[Paliers.Bancs[0].Id].Place);
            Assert.AreEqual(magasin.Exporter(), stockage.Lire(Magasin.ClefDeSauvegarde));
        }

        [Test]
        public void UnActeImpayableNEcritRien()
        {
            var (magasin, stockage, _) = Nouveau();
            var avant = magasin.Etat;
            magasin.Creuser();
            Assert.AreSame(avant, magasin.Etat);
            Assert.IsNull(stockage.Lire(Magasin.ClefDeSauvegarde));
        }

        [Test]
        public void LesPasDeLaBoucleSontSauvegardesAuRythmeDeLHote()
        {
            var (magasin, stockage, _) = Nouveau();
            Assert.IsFalse(magasin.SauvegarderSiModifie());
            magasin.Remplacer(Reducteur.Tick(magasin.Etat, 1));
            Assert.IsNull(stockage.Lire(Magasin.ClefDeSauvegarde), "un pas ne touche pas au disque");
            Assert.IsTrue(magasin.SauvegarderSiModifie());
            Assert.IsFalse(magasin.SauvegarderSiModifie());
            Assert.IsNotNull(stockage.Lire(Magasin.ClefDeSauvegarde));
        }

        [Test]
        public void UneSaveRelueRendLaMemePartieEtLeMemeInstant()
        {
            var (magasin, stockage, horloge) = Nouveau();
            magasin.Convaincre(Paliers.Bancs[0].Id);
            magasin.Remplacer(Reducteur.Tick(magasin.Etat, 120));
            magasin.Sauvegarder();

            var relu = new Magasin(stockage, horloge);
            OutilsDeTest.ComparerAToleranceFlottante(relu.Etat, magasin.Etat, 0);
            Assert.AreEqual(magasin.DernierInstantMs, relu.DernierInstantMs);
        }

        [Test]
        public void UneSaveIllisibleNeBloquePasLeJeu()
        {
            var stockage = new StockageEnMemoire();
            stockage.Ecrire(Magasin.ClefDeSauvegarde, "{ceci n'est pas une save");
            var magasin = new Magasin(stockage, new HorlogeFigee(Depart));
            Assert.AreEqual(0, magasin.Etat.TempsJeuSecondes);
            Assert.AreEqual(0, magasin.Etat.Cycle.Bancs.Count);
        }

        [Test]
        public void UnImportIllisibleNeToucheARien()
        {
            var (magasin, _, _) = Nouveau();
            var avant = magasin.Etat;
            Assert.IsFalse(magasin.EssayerDImporter("[1, 2"));
            Assert.IsFalse(magasin.EssayerDImporter("{\"versionSave\":-3,\"contenu\":{}}"));
            Assert.AreSame(avant, magasin.Etat);
        }

        [Test]
        public void LeRetourCrediteLAbsenceEnUnPasEtLAnnonce()
        {
            var (magasin, _, horloge) = Nouveau();
            var avant = magasin.Etat;
            horloge.AvancerMs(2 * H);
            magasin.Reprendre();
            Assert.IsNotNull(magasin.Retour);
            Assert.AreEqual(7200, magasin.Retour.SecondesCreditees);
            OutilsDeTest.ComparerAToleranceFlottante(
                magasin.Etat with { Permanent = magasin.Etat.Permanent with { HeuresHorsLigneCreditees = 0 } },
                Reducteur.Tick(avant, 7200), 0);

            magasin.OublierRetour();
            Assert.IsNull(magasin.Retour);
        }

        [Test]
        public void UnRechargementNEstPasUnRetour()
        {
            var (magasin, _, horloge) = Nouveau();
            horloge.AvancerMs(30_000);
            magasin.Reprendre();
            Assert.IsNull(magasin.Retour, "« la mare a tourné sans toi pendant 30 s » n'apprend rien à personne");
            Assert.AreEqual(30, magasin.Etat.TempsJeuSecondes, 1e-9);
        }

        [Test]
        public void LesAnnoncesSEmpilentEtSOublientUneAUne()
        {
            var (magasin, _, _) = Nouveau();
            var changements = 0;
            magasin.Change += () => changements += 1;
            magasin.Annoncer(new[] { "a", "b" });
            magasin.Annoncer(new string[0]);
            CollectionAssert.AreEqual(new[] { "a", "b" }, magasin.AAnnoncer.ToList());
            magasin.OublierAnnonce("a");
            CollectionAssert.AreEqual(new[] { "b" }, magasin.AAnnoncer.ToList());
            magasin.OublierAnnonce("inconnue");
            Assert.AreEqual(2, changements);
        }

        [Test]
        public void ReinitialiserEffaceLaSaveEtRepartDeLOeuf()
        {
            var (magasin, stockage, _) = Nouveau();
            magasin.Convaincre(Paliers.Bancs[0].Id);
            magasin.Reinitialiser();
            Assert.IsNull(stockage.Lire(Magasin.ClefDeSauvegarde));
            Assert.AreEqual(0, magasin.Etat.Cycle.Bancs.Count);
        }
    }

    [TestFixture]
    public sealed class BoucleTests
    {
        private static (Boucle Boucle, Magasin Magasin, HorlogeFigee Horloge, List<IReadOnlyList<string>> Annonces) Nouvelle(
            bool avecGardeDAbsence = true)
        {
            var horloge = new HorlogeFigee(1_000_000);
            var magasin = new Magasin(new StockageEnMemoire(), horloge);
            var annonces = new List<IReadOnlyList<string>>();
            var boucle = new Boucle(
                () => magasin.Etat, magasin.Remplacer, d => annonces.Add(d), horloge,
                avecGardeDAbsence ? magasin.Reprendre : (System.Action)null);
            return (boucle, magasin, horloge, annonces);
        }

        [Test]
        public void UneBoucleArreteeNAvanceRien()
        {
            var (boucle, magasin, horloge, _) = Nouvelle();
            horloge.AvancerMs(5000);
            boucle.Pas();
            Assert.AreEqual(0, magasin.Etat.TempsJeuSecondes);
        }

        [Test]
        public void LeDtVientDeLHorlogePasDuNombreDAppels()
        {
            var (boucle, magasin, horloge, _) = Nouvelle();
            boucle.Demarrer();
            horloge.AvancerMs(100);
            boucle.Pas();
            horloge.AvancerMs(250);
            boucle.Pas();
            boucle.Pas();
            Assert.AreEqual(0.35, magasin.Etat.TempsJeuSecondes, 1e-12);
        }

        [Test]
        public void UnReculDHorlogeEstIgnore()
        {
            var (boucle, magasin, horloge, _) = Nouvelle();
            boucle.Demarrer();
            horloge.AvancerMs(-60_000);
            boucle.Pas();
            Assert.AreEqual(0, magasin.Etat.TempsJeuSecondes);
            horloge.AvancerMs(100);
            boucle.Pas();
            Assert.AreEqual(0.1, magasin.Etat.TempsJeuSecondes, 1e-12, "on repart de l'instant reculé, sans rien rattraper");
        }

        [Test]
        public void LesSuccesSontAnnoncesEtLeurIntervalleEstObserve()
        {
            var (boucle, magasin, horloge, annonces) = Nouvelle();
            magasin.Convaincre(Paliers.Bancs[0].Id);
            boucle.Demarrer();
            horloge.AvancerMs(100);
            boucle.Pas();
            Assert.AreEqual(1, annonces.Count);
            CollectionAssert.Contains(annonces[0], "acte-premiere-conviction");
            Assert.AreEqual(1, magasin.Etat.Telemetrie.IntervallesEntreSucces.Count);
            Assert.AreEqual(0, magasin.Etat.Telemetrie.SecondesDepuisDernierSucces);
        }

        [Test]
        public void UnTrouEntreDeuxPasEstCrediteCommeUneAbsencePlafonnee()
        {
            // La machine se met en veille dix heures pendant la partie, ou quelqu'un
            // avance son horloge : la boucle tourne encore, mais son pas suivant ne
            // doit pas jouer dix heures d'un bloc.
            var (boucle, magasin, horloge, _) = Nouvelle();
            boucle.Demarrer();
            horloge.AvancerMs(100);
            boucle.Pas();
            horloge.AvancerMs(10 * 3600_000);
            boucle.Pas();
            Assert.AreEqual(0.1 + Constantes.CapHorsLigneHeuresInitial * 3600, magasin.Etat.TempsJeuSecondes, 1e-6);
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial, magasin.Etat.Permanent.HeuresHorsLigneCreditees, 1e-9);
            Assert.IsNotNull(magasin.Retour, "le retour est annoncé comme n'importe quelle absence");
        }

        [Test]
        public void SansGardeLaBoucleJoueLEcartCommeLeWeb()
        {
            var (boucle, magasin, horloge, _) = Nouvelle(false);
            boucle.Demarrer();
            horloge.AvancerMs(10 * 3600_000);
            boucle.Pas();
            Assert.AreEqual(10 * 3600, magasin.Etat.TempsJeuSecondes, 1e-6);
        }

        [Test]
        public void UneFrameLenteNEstPasUneAbsence()
        {
            var (boucle, magasin, horloge, _) = Nouvelle();
            boucle.Demarrer();
            horloge.AvancerMs(2000);
            boucle.Pas();
            Assert.AreEqual(2, magasin.Etat.TempsJeuSecondes, 1e-9);
            Assert.AreEqual(0, magasin.Etat.Permanent.HeuresHorsLigneCreditees);
        }

        [Test]
        public void UnePauseDeLApplicationPasseParLeCreditPlafonneJamaisParLaBoucle()
        {
            // Le scénario mobile : l'application passe en arrière-plan dix heures.
            // L'hôte arrête la boucle, crédite l'absence au retour, puis la relance.
            var (boucle, magasin, horloge, _) = Nouvelle();
            boucle.Demarrer();
            boucle.Arreter();
            horloge.AvancerMs(10 * 3600_000);
            magasin.Reprendre();
            boucle.Demarrer();
            horloge.AvancerMs(100);
            boucle.Pas();
            Assert.AreEqual(Constantes.CapHorsLigneHeuresInitial * 3600 + 0.1, magasin.Etat.TempsJeuSecondes, 1e-6);
        }
    }
}
