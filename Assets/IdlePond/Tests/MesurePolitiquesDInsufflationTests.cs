using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// La mesure du chantier 2 de la roadmap : que deviennent la courbe et les rangs
    /// d'insufflation selon ce que le joueur fait de son Souffle ? Une MESURE, pas une
    /// garde : `Explicit`, elle ne tourne qu'à la demande et écrit son tableau dans
    /// `Logs/mesures/politiques-d-insufflation.md`. La politique retenue reste à trancher.
    /// </summary>
    [Explicit("mesure, à la demande")]
    public class MesurePolitiquesDInsufflationTests
    {
        const int CYCLES = 15;
        const long GRAINE = 7;

        static readonly (string Nom, Func<EtatJeu, EtatJeu> Insuffler)[] POLITIQUES =
        {
            ("la moins chère d'abord (actuelle)", null),
            ("la moitié en réserve", InsufflerALaMoitie),
            ("la globale seule", InsufflerLaGlobaleSeule),
            ("la plus profonde + la globale", InsufflerLaPlusProfonde),
        };

        [Test]
        public void Comparer_les_politiques_d_insufflation()
        {
            var texte = new StringBuilder();
            texte.AppendLine($"# Politiques d'insufflation — {CYCLES} cycles, graine {GRAINE}");
            foreach (var (titre, fraction) in new[] { ("Renaître quand l'acquis atteint 95 % (défaut)", POLITIQUE_PAR_DEFAUT.FractionDeSaturationPourRenaitre), ("Renaître dès qu'on est bloqué", 0.0) })
            {
            texte.AppendLine();
            texte.AppendLine("## " + titre);
            texte.AppendLine();
            texte.AppendLine("| Politique | Cycles | Rangs achetés | Ciblée max | Globale | Jeu actif (h) | Cycle 1 (h) | Cycle 5 (h) | Cycle 10 (h) | Cycle 15 (h) | Fond atteint au cycle | Pic du cycle 15 (/s) | Souffle gagné au cycle 15 |");
            texte.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");

            foreach (var (nom, insuffler) in POLITIQUES)
            {
                var politique = POLITIQUE_PAR_DEFAUT with { Insuffler = insuffler, FractionDeSaturationPourRenaitre = fraction };
                var resultat = Simuler(CYCLES, politique, GRAINE);
                var rangs = resultat.Etat.Permanent.Insufflations;
                var total = rangs.Values.Sum();
                var globale = rangs.TryGetValue(Insufflations.GLOBALE_ID, out var g) ? g : 0;
                var cibleeMax = rangs.Where(r => r.Key != Insufflations.GLOBALE_ID).Select(r => r.Value).DefaultIfEmpty(0).Max();
                var cycles = resultat.Cycles;
                string Heures(int n) => cycles.Count >= n ? (cycles[n - 1].DureeEcouleeSecondes / 3600).ToString("F1", CultureInfo.InvariantCulture) : "—";
                var fond = cycles.Select((c, i) => (c, i)).FirstOrDefault(x => x.c.PaliersOuverts >= Constantes.NOMBRE_DE_PALIERS);
                var auFond = fond.c != null ? (fond.i + 1).ToString(CultureInfo.InvariantCulture) : "jamais";

                texte.AppendLine($"| {nom} | {resultat.CyclesAcheves} | {total} | {cibleeMax} | {globale} | " +
                                 $"{(resultat.SecondesActives / 3600).ToString("F1", CultureInfo.InvariantCulture)} | " +
                                 $"{Heures(1)} | {Heures(5)} | {Heures(10)} | {Heures(15)} | {auFond} | " +
                                 $"{(cycles.Count > 0 ? cycles[cycles.Count - 1].ProductionPicParSeconde.ToString() : "—")} | " +
                                 $"{(cycles.Count > 0 ? cycles[cycles.Count - 1].SouffleGagne.ToString() : "—")} |");
            }
            }

            var dossier = Path.GetFullPath(Path.Combine("Logs", "mesures"));
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Path.Combine(dossier, "politiques-d-insufflation.md"), texte.ToString());
            TestContext.Out.WriteLine(texte.ToString());
            Assert.Pass(texte.ToString());
        }
    }
}
