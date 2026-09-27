/*
 * §8.4 — plancher garanti sur l'assise I. Port de
 * `tests/plancher-de-cadence.test.ts`.
 *
 * | Garantie              | Valeur                                    |
 * |-----------------------|-------------------------------------------|
 * | Premier succès        | Dans les DEUX PREMIÈRES MINUTES           |
 * | Première demi-heure   | Un déclenchement toutes les 3 à 5 minutes |
 * | Première éclosion     | Un franchissement, OBLIGATOIREMENT        |
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class PlancherDeCadenceTests
    {
        private static IReadOnlyList<Declenchement> _releve;

        private static IReadOnlyList<Declenchement> Releve => _releve ??= Joueur.JoueUneDemiHeure();

        [Test]
        public void LePremierSuccesTombeEnMoinsDeDeuxMinutes()
        {
            Assert.Greater(Releve.Count, 0, "aucun succès déclenché");
            Assert.Less(Releve[0].InstantSecondes, Constantes.PremierSuccesAvantSecondes);
        }

        [Test]
        public void UnDeclenchementAuMoinsToutesLesCinqMinutesSurLaPremiereDemiHeure()
        {
            var trous = new List<string>();
            double precedent = 0;
            foreach (var d in Releve)
            {
                var ecart = d.InstantSecondes - precedent;
                if (ecart > Constantes.CadenceMaxEntreSuccesSecondes) trous.Add((ecart / 60).ToString("0.0") + " min avant " + d.Id);
                precedent = d.InstantSecondes;
            }
            var fin = Constantes.FenetreDuPlancherDeCadenceSecondes - precedent;
            if (fin > Constantes.CadenceMaxEntreSuccesSecondes) trous.Add((fin / 60).ToString("0.0") + " min de silence jusqu’à la trentième minute");
            CollectionAssert.IsEmpty(trous, "la cadence du §8.4 est trouée");
        }

        [Test]
        public void LaPremiereEclosionDeclencheUnFranchissementObligatoirement()
        {
            var etat = Reducteur.Eclore(Reducteur.EtatInitial(1));
            var declenches = Reducteur.TickDetaille(etat, 0.1).Declenches;
            var franchissements = declenches.Where(id =>
                RegistreDesSucces.Liste.FirstOrDefault(s => s.Id == id)?.Famille == FamilleDeSucces.Franchissement);
            Assert.Greater(franchissements.Count(), 0);
        }

        [Test]
        public void LaCadenceNeTientPasAUnSeulSuccesQuiSeRepeterait()
        {
            Assert.AreEqual(Releve.Count, Releve.Select(d => d.Id).Distinct().Count());
        }
    }
}
