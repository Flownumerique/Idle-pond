/*
 * Critère d'acceptation du jalon v0.1 : « le simulateur tourne 15 cycles sans
 * jouer, et le test d'équivalence de pas passe. » Port de
 * `tests/simulateur.test.ts`.
 *
 * Le simulateur n'a pas de moteur à lui : il appelle le même Tick que le jeu.
 * Ce test vérifie que quinze cycles s'enchaînent, et que les invariants du
 * Tier 0 tiennent sur toute la durée, pas seulement à l'arrivée.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Noyau;
using IdlePond.Simulateur;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class SimulateurTests
    {
        private static ResultatDeSimulation _quinze;

        /// <summary>Quinze cycles coûtent quelques secondes : on ne les paie qu'une fois.</summary>
        private static ResultatDeSimulation Quinze => _quinze ??= Simulation.Simuler(Constantes.NombreDEclosionsVise);

        [Test]
        public void UnePartieSansUIAtteintLEclosionDeuxEnHeadless()
        {
            var partie = Simulation.Simuler(2, null, 1, null, Assises.PaliersLivres);
            Assert.IsNull(partie.CycleNonConvergent);
            Assert.AreEqual(2, partie.Etat.Permanent.NombreEclosions);
            Assert.IsTrue(partie.Etat.Permanent.ContenanceMana.Gt(Constantes.ContenanceInitiale));
            Assert.Greater(partie.Etat.Permanent.Densites[0], 0);
        }

        [Test]
        public void EnchaineQuinzeCyclesSansJouer()
        {
            var resultat = Quinze;
            Assert.IsNull(resultat.CycleNonConvergent, "un cycle n'a pas convergé");
            Assert.AreEqual(Constantes.NombreDEclosionsVise, resultat.CyclesAcheves);
            Assert.AreEqual(Constantes.NombreDEclosionsVise, resultat.Releve.Cycles.Count);
            foreach (var cycle in resultat.Releve.Cycles)
            {
                Assert.Greater(cycle.DureeEcouleeSecondes, 0);
                Assert.Greater(cycle.PaliersOuverts, 0);
            }
        }

        [Test]
        public void SepareLeTempsActifDuTempsEcoule()
        {
            var resultat = Simulation.Simuler(5);
            Assert.Greater(resultat.TempsActifSecondes, 0);
            Assert.Greater(resultat.TempsEcouleSecondes, resultat.TempsActifSecondes);
            Assert.Greater(resultat.Sessions.Count, 0);
            Assert.AreEqual(resultat.TempsActifSecondes, resultat.Sessions.Sum(s => s.SecondesActives), 5e-7);
        }

        [Test]
        public void LIntervalleDeCheckInEstLeSeulReglageDeTempsCalendaire()
        {
            var court = Simulation.Simuler(6, Politique.ParDefaut with { IntervalleDeCheckInSecondes = 2 * 3600 });
            var longue = Simulation.Simuler(6, Politique.ParDefaut with { IntervalleDeCheckInSecondes = 8 * 3600 });
            Assert.Greater(longue.TempsEcouleSecondes, court.TempsEcouleSecondes * 2);
            Assert.Less(Math.Abs(longue.TempsActifSecondes / court.TempsActifSecondes - 1), 0.5);
        }

        [Test]
        public void LeJoueurRentreDansLOeufSurLaSaturationPasSurUnMinuteur()
        {
            var cycles = 0;
            Simulation.Simuler(4, null, 1, etat =>
            {
                if (etat.Permanent.NombreEclosions == cycles) return;
                cycles = etat.Permanent.NombreEclosions;
                Assert.AreEqual(0, etat.Cycle.AcquisDeSejour);
            });
            var partie = Simulation.Simuler(4);
            var attendu = Math.Pow(1 + Constantes.AcquisMax * Politique.ParDefaut.FractionDeSaturationPourEclore, 4);
            Assert.Greater(partie.Etat.Permanent.ContenanceMana.Div(Constantes.ContenanceInitiale).ToNumber(), attendu);
        }

        [Test]
        public void LaDensiteNeRedescendJamais()
        {
            IReadOnlyList<double> precedentes = null;
            Simulation.Simuler(Constantes.NombreDEclosionsVise, null, 1, etat =>
            {
                if (precedentes != null)
                {
                    for (var palier = 0; palier < etat.Permanent.Densites.Count; palier += 1)
                    {
                        Assert.GreaterOrEqual(etat.Permanent.Densites[palier], precedentes[palier], "densité du palier " + palier);
                    }
                }
                precedentes = etat.Permanent.Densites;
            });
            Assert.IsNotNull(precedentes);
            Assert.IsTrue(precedentes.Any(d => d > 0));
        }

        [Test]
        public void LesAcquisPermanentsNeSeReperdentJamais()
        {
            var eclosions = 0;
            double contenance = 0, foi = 0, compteurs = 0;
            Simulation.Simuler(Constantes.NombreDEclosionsVise, null, 1, etat =>
            {
                Assert.AreEqual(1, etat.Permanent.Acclimatations[Assises.TypeManaNatal]);
                Assert.GreaterOrEqual(etat.Permanent.NombreEclosions, eclosions);
                Assert.GreaterOrEqual(etat.Permanent.ContenanceMana.ToNumber(), contenance);
                Assert.GreaterOrEqual(etat.Permanent.Foi.ToNumber(), foi);
                var somme = etat.Permanent.CompteursTechnique.Valeurs.Sum();
                Assert.GreaterOrEqual(somme, compteurs, "un compteur de technique a reculé : on ne désapprend pas");
                eclosions = etat.Permanent.NombreEclosions;
                contenance = etat.Permanent.ContenanceMana.ToNumber();
                foi = etat.Permanent.Foi.ToNumber();
                compteurs = somme;
            });
            Assert.AreEqual(Constantes.NombreDEclosionsVise, eclosions);
        }

        [Test]
        public void LEclosionEmporteLePeuplementEtLaGeometrieEtRienDAutre()
        {
            var resultat = Simulation.Simuler(2);
            Assert.AreEqual(1, resultat.Etat.Cycle.PaliersOuverts);
            Assert.AreEqual(0, resultat.Etat.Cycle.Bancs.Count);
            Assert.Greater(resultat.Etat.Permanent.ProfondeurMaxAtteinte, 1);
            Assert.IsTrue(resultat.Etat.Permanent.ManaAmbiant.Gt(0));
        }

        [Test]
        public void LaProgressionDescendReellementDUnCycleALAutre()
        {
            var cycles = Quinze.Releve.Cycles;
            Assert.Greater(cycles[cycles.Count - 1].PaliersOuverts, cycles[0].PaliersOuverts);
        }

        [Test]
        public void LaFractionPasseeARedescendreEstReleveeAChaqueCycle()
        {
            var resultat = Simulation.Simuler(5);
            foreach (var cycle in resultat.Releve.Cycles)
            {
                Assert.GreaterOrEqual(cycle.FractionEnRedescente, 0);
                Assert.LessOrEqual(cycle.FractionEnRedescente, 1);
            }
            Assert.IsTrue(resultat.Releve.Cycles.Skip(1).Any(c => c.FractionEnRedescente > 0));
        }

        [Test]
        public void TauNeSEffondrePlusIlVautSaGraineDuPremierCycleAuDernier()
        {
            var tauNominal = 1 / Constantes.KTauxDeRepeuplement;
            foreach (var cycles in new[] { 1, 6 })
            {
                Assert.AreEqual(tauNominal, Telemetrie.Relever(Simulation.Simuler(cycles).Etat).TauDeRepeuplementSecondes, 5e-7, "τ après " + cycles + " cycles");
            }
            Assert.AreEqual(tauNominal, Telemetrie.Relever(Quinze.Etat).TauDeRepeuplementSecondes, 5e-7);
        }

        [Test]
        public void LaDensiteNAPlusQuUnDeboucheLAcquisDeSejour()
        {
            var depart = Reducteur.EtatInitial(1);
            var dense = depart with { Permanent = depart.Permanent with { Densites = depart.Permanent.Densites.Select(_ => 1e6).ToArray() } };
            Assert.Greater(Reducteur.Tick(dense, 3600).Cycle.AcquisDeSejour, Reducteur.Tick(depart, 3600).Cycle.AcquisDeSejour);
            Assert.AreEqual(Constantes.KTauxDeRepeuplement, Densite.VitesseDeRepeuplement());
        }

        [Test]
        public void LeCalibreurRefuseLesBranchesBorneesEtResoutLesAutres()
        {
            Assert.IsNull(Calibreur.ResoudreCoupleAB(new CibleDOuverture(BrancheTechnique.Recrutement, new[] { 1 }), new double[] { 1, 2 }));
            var trajectoire = Enumerable.Range(1, 15).Select(i => 1000.0 * i * i).ToArray();
            var couple = Calibreur.ResoudreCoupleAB(new CibleDOuverture(BrancheTechnique.Creusement, new[] { 1, 3, 6 }), trajectoire);
            Assert.IsNotNull(couple);
            Assert.Less(couple.Erreur, 3);
        }
    }
}
