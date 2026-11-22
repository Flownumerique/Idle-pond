using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IdlePond.Noyau;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// La mesure du toucher (roadmap, chantier 2) : que change une cadence de toucher au rythme
    /// mesuré dans `RESULTATS.md` ? Une MESURE, pas une garde : `Explicit`, elle ne tourne qu'à
    /// la demande et écrit son tableau dans `Logs/mesures/toucher.md`.
    ///
    /// La valeur d'un toucher est linéaire en `BASE_DU_TOUCHER` tant que la contenance ne la
    /// borne pas : doubler la cadence, c'est doubler la base. Le tableau se lit donc aussi comme
    /// un balayage de `BASE_DU_TOUCHER` à cadence fixe.
    /// </summary>
    [Explicit("mesure, à la demande")]
    public class MesureDuToucherTests
    {
        const int CYCLES = 15;
        const long GRAINE = 7;
        static readonly double[] CADENCES = { 0, 0.5, 1, 2, 5, 10 };
        static readonly int[] MINUTES = { 5, 15, 30, 60 };

        static string F(double x, string format = "F1") => x.ToString(format, CultureInfo.InvariantCulture);

        [Test]
        public void Mesurer_le_toucher()
        {
            var texte = new StringBuilder();
            texte.AppendLine($"# Le toucher — graine {GRAINE}, BASE_DU_TOUCHER = {F(Constantes.BASE_DU_TOUCHER, "R")}");
            texte.AppendLine();
            texte.AppendLine("Cadence : touchers par seconde devant l'écran, en achat continu (tout le temps est actif).");
            texte.AppendLine("Les cadences non nulles achètent aussi l'amélioration du toucher (`AcheterAuMieuxEnTouchant`).");

            // ── La première vie, de près ─────────────────────────────────────────────────
            texte.AppendLine();
            texte.AppendLine("## Cycle 1");
            texte.AppendLine();
            texte.AppendLine("| Cadence (/s) | Paliers à " + string.Join(" | Paliers à ", MINUTES.Select(m => m + " min")) +
                             " | Bloqué à (min) | Durée (h) | Part du toucher, 15 premières min | Pic (/s) | Souffle gagné |");
            texte.AppendLine("|---|" + string.Join("", MINUTES.Select(_ => "---|")) + "---|---|---|---|---|");
            foreach (var cadence in CADENCES)
            {
                var paliers = new Dictionary<int, int>();
                double? bloque = null;
                var resultat = Simuler(1, POLITIQUE_PAR_DEFAUT with { ToucherParSeconde = cadence }, GRAINE, etat =>
                {
                    if (etat.Permanent.NombreDeRenaissances > 0) return;
                    var t = etat.Cycle.DureeSecondes;
                    foreach (var m in MINUTES)
                        if (!paliers.ContainsKey(m) && t >= m * 60) paliers[m] = etat.Cycle.PaliersOuverts;
                    if (bloque == null && Economie.EstBloque(etat)) bloque = t;
                });
                var cycle = resultat.Cycles[0];

                // La part du toucher, sur les quinze premières minutes : le garde-fou de durée
                // arrête la simulation là. La production du pas écoulé est lue sur l'état
                // d'arrivée : une estimation au pour-cent près, assez pour une part.
                var passif = Decimal.Zero;
                double dernier = 0;
                var debut = Simuler(1, POLITIQUE_PAR_DEFAUT with { ToucherParSeconde = cadence, DureeMaxParCycleSecondes = 15 * 60 }, GRAINE, etat =>
                {
                    var t = etat.Cycle.DureeSecondes;
                    if (t > dernier) { passif = passif.Add(Economie.ProductionTotaleParSeconde(etat).Mul(t - dernier)); dernier = t; }
                });
                var part = debut.ManaTouche.Div(debut.ManaTouche.Add(passif)).ToNumber();
                texte.AppendLine($"| {F(cadence)} | " + string.Join(" | ", MINUTES.Select(m => paliers.TryGetValue(m, out var p) ? p.ToString() : "—")) +
                                 $" | {(bloque.HasValue ? F(bloque.Value / 60, "F0") : "—")} | {F(cycle.DureeEcouleeSecondes / 3600, "F2")} | {F(part * 100)} % | " +
                                 $"{cycle.ProductionPicParSeconde} | {cycle.SouffleGagne} |");
            }

            // ── Quinze vies ──────────────────────────────────────────────────────────────
            texte.AppendLine();
            texte.AppendLine($"## {CYCLES} cycles");
            texte.AppendLine();
            texte.AppendLine("Bloqué à : minutes de la vie avant le premier blocage, le moment où la contenance, et non plus le mana, décide.");
            texte.AppendLine();
            texte.AppendLine("| Cadence (/s) | Cycles | Jeu actif (h) | Cycle 2 (h) | Cycle 5 (h) | Cycle 15 (h) | Bloqué à, cycle 2 | cycle 5 | cycle 15 | Fond atteint au cycle | Rang du toucher | Souffle gagné au cycle 15 |");
            texte.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var cadence in CADENCES)
            {
                var politique = POLITIQUE_PAR_DEFAUT with
                {
                    ToucherParSeconde = cadence,
                    AcheterLesAmeliorations = cadence > 0 ? AcheterAuMieuxEnTouchant : null,
                };
                var bloques = new Dictionary<int, double>();
                var resultat = Simuler(CYCLES, politique, GRAINE, etat =>
                {
                    var vie = etat.Permanent.NombreDeRenaissances + 1;
                    if (!bloques.ContainsKey(vie) && Economie.EstBloque(etat)) bloques[vie] = etat.Cycle.DureeSecondes;
                });
                string Bloque(int n) => bloques.TryGetValue(n, out var t) ? F(t / 60, "F0") : "—";
                var cycles = resultat.Cycles;
                string Heures(int n) => cycles.Count >= n ? F(cycles[n - 1].DureeEcouleeSecondes / 3600, "F2") : "—";
                var fond = cycles.Select((c, i) => (c, i)).FirstOrDefault(x => x.c.PaliersOuverts >= Constantes.NOMBRE_DE_PALIERS);
                var rang = Economie.RangDAmelioration(resultat.Etat, Noyau.Donnees.AmeliorationsDeRenaissance.TOUCHER_ID);
                texte.AppendLine($"| {F(cadence)} | {resultat.CyclesAcheves} | {F(resultat.SecondesActives / 3600)} | {Heures(2)} | {Heures(5)} | {Heures(15)} | {Bloque(2)} | {Bloque(5)} | {Bloque(15)} | " +
                                 $"{(fond.c != null ? (fond.i + 1).ToString() : "jamais")} | {rang} | " +
                                 $"{(cycles.Count > 0 ? cycles[cycles.Count - 1].SouffleGagne.ToString() : "—")} |");
            }

            var dossier = Path.GetFullPath(Path.Combine("Logs", "mesures"));
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Path.Combine(dossier, "toucher.md"), texte.ToString());
            TestContext.Out.WriteLine(texte.ToString());
            Assert.Pass(texte.ToString());
        }
    }
}
