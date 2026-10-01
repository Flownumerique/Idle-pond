using System.Linq;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// La parité avec le TypeScript — spec 2026-09-27 §3. Tant qu'elle est verte, le
    /// portage n'a rien changé à la mécanique. Une parité rouge ne se corrige ni en
    /// relâchant la tolérance, ni en régénérant les références : on cherche l'écart.
    /// </summary>
    public class PariteTests
    {
        static readonly JObject Parties = References.Lire("parties");

        static JArray Instants(string nom) =>
            (JArray)Parties["scenarios"].First(s => (string)s["nom"] == nom)["instants"];

        static void Comparer(string nom, JToken obtenu, JToken attendu) =>
            Comparateur.ComparerATolerance(obtenu, attendu, Comparateur.TOLERANCE_RELATIVE, nom);

        [Test, Description("la séquence de dt de déterminisme rend les états du TypeScript")]
        public void La_sequence_de_dt_rend_les_etats_du_TypeScript()
        {
            var attendus = Instants("sequence");
            var etat = EtatDeTravail.Creer(4242);
            Comparer("sequence[0]", Instantane.De(etat), attendus[0]);
            var sequence = new[] { 0.1, 60, 0.1, 3600, 7, 28800, 0.1, 900 };
            for (var i = 0; i < sequence.Length; i++)
            {
                etat = Reducteur.Tick(etat, sequence[i]);
                Comparer($"sequence[{i + 1}]", Instantane.De(etat), attendus[i + 1]);
            }
        }

        [Test, Description("le joueur scripté rend les états du TypeScript")]
        public void Le_joueur_scripte_rend_les_etats_du_TypeScript()
        {
            var attendus = Instants("joueur");
            var secondes = new[] { 60.0, 600, 1800, 3600 };
            for (var i = 0; i < secondes.Length; i++)
                Comparer($"joueur[{secondes[i]}]", Instantane.De(Joueur.Rejoue(secondes[i])), attendus[i]);
        }

        [Test, Description("trois renaissances rendent les états du TypeScript")]
        public void Trois_renaissances_rendent_les_etats_du_TypeScript()
        {
            var attendus = Instants("renaissances");
            var etat = EtatDeTravail.Creer(777);
            var k = 0;
            Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
            for (var cycle = 0; cycle < 3; cycle++)
            {
                etat = Reducteur.Tick(etat, 3600);
                for (var i = 0; i < 3; i++) etat = Reducteur.Grandir(etat);
                etat = Renaissance.Renaitre(etat);
                Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
                etat = Reducteur.Insuffler(etat, IdlePond.Noyau.Donnees.Insufflations.GLOBALE_ID);
                etat = Reducteur.Insuffler(etat, "insufflation-vairon");
                etat = Reducteur.Creuser(etat);
                etat = Reducteur.Debloquer(etat, "vairon");
                for (var n = 0; n < 20; n++) etat = Reducteur.Ameliorer(etat, "vairon");
                etat = Reducteur.Tick(etat, 600);
                Comparer($"renaissances[{k}]", Instantane.De(etat), attendus[k++]);
            }
        }

        [Test, Description("quinze cycles simulés rendent l’état du TypeScript")]
        public void Quinze_cycles_simules_rendent_l_etat_du_TypeScript()
        {
            var attendu = Parties["simulation"];
            var resultat = Simuler(15, null, 7);
            Assert.That(resultat.CyclesAcheves, Is.EqualTo((int)attendu["cyclesAcheves"]));
            Comparer("simulation.secondesActives", resultat.SecondesActives, attendu["secondesActives"]);
            Comparer("simulation.secondesEcoulees", resultat.SecondesEcoulees, attendu["secondesEcoulees"]);
            Comparer("simulation", Instantane.De(resultat.Etat), attendu["instant"]);
        }
    }
}
