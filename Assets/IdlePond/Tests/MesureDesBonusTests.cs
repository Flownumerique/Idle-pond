using System.Collections.Generic;
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
    /// Que changent les bonus de lieu et les techniques (onglet « Débloquer ») à la courbe ?
    /// Leurs parts, leurs rangs et leurs prix sont des graines que rien n'a mesurées. Une
    /// MESURE, pas une garde : `Explicit`, elle ne tourne qu'à la demande et écrit son
    /// tableau dans `Logs/mesures/bonus.md`.
    ///
    /// Le contenu est borné aux paliers livrés : seuls la Noue et le Gour portent des bonus,
    /// et c'est la partie que le joueur vit.
    /// </summary>
    [Explicit("mesure, à la demande")]
    public class MesureDesBonusTests
    {
        const int CYCLES = 6;
        const long GRAINE = 7;

        static readonly (string Nom, double Part)[] POLITIQUES =
        {
            ("aucun bonus (actuelle)", 0),
            ("un bonus s'il coûte au plus 10 % du mana", 0.1),
            ("un bonus s'il coûte au plus 50 % du mana", 0.5),
            ("un bonus dès qu'il est payable", 1),
        };

        [Test]
        public void Comparer_les_parties_avec_et_sans_bonus()
        {
            var texte = new StringBuilder();
            texte.AppendLine($"# Bonus de lieu et techniques — {CYCLES} cycles, graine {GRAINE}, {Assises.PALIERS_LIVRES} paliers livrés");
            texte.AppendLine();
            texte.AppendLine("| Politique | Cycle | Durée (h) | Paliers ouverts | Part de la vie bloquée | Rangs de bonus à la fin de la vie | Pic (/s) |");
            texte.AppendLine("|---|---|---|---|---|---|---|");

            foreach (var (nom, part) in POLITIQUES)
            {
                // Par vie (indexée par le nombre de renaissances) : secondes passées bloqué, et
                // rangs de bonus tenus au dernier instant observé avant la renaissance.
                var bloque = new Dictionary<int, double>();
                var rangs = new Dictionary<int, int>();
                var vieObservee = -1;
                double dureeObservee = 0;

                void Observer(EtatJeu etat)
                {
                    var vie = etat.Permanent.NombreDeRenaissances;
                    if (vie != vieObservee)
                    {
                        vieObservee = vie;
                        dureeObservee = 0;
                    }
                    var ecoule = etat.Cycle.DureeSecondes - dureeObservee;
                    dureeObservee = etat.Cycle.DureeSecondes;
                    if (ecoule > 0 && Economie.EstBloque(etat))
                        bloque[vie] = (bloque.TryGetValue(vie, out var deja) ? deja : 0) + ecoule;
                    var tenus = etat.Cycle.Bonus?.Values.Sum() ?? 0;
                    if (tenus > 0 || !rangs.ContainsKey(vie)) rangs[vie] = tenus;
                }

                var politique = POLITIQUE_PAR_DEFAUT with { PartDuManaPourUnBonus = part };
                var resultat = Simuler(CYCLES, politique, GRAINE, Observer, Assises.PALIERS_LIVRES);

                for (var i = 0; i < resultat.Cycles.Count; i += 1)
                {
                    var cycle = resultat.Cycles[i];
                    var duree = cycle.DureeEcouleeSecondes;
                    var partBloquee = duree > 0 && bloque.TryGetValue(i, out var b) ? b / duree : 0;
                    texte.AppendLine(
                        $"| {nom} | {i + 1} | {(duree / 3600).ToString("F2", CultureInfo.InvariantCulture)} | {cycle.PaliersOuverts} | " +
                        $"{(partBloquee * 100).ToString("F0", CultureInfo.InvariantCulture)} % | " +
                        $"{(rangs.TryGetValue(i, out var r) ? r : 0)} | {cycle.ProductionPicParSeconde} |");
                }
                texte.AppendLine(
                    $"| {nom} | **total** | {(resultat.SecondesActives / 3600).ToString("F2", CultureInfo.InvariantCulture)} | | | | |");
            }

            var dossier = Path.GetFullPath(Path.Combine("Logs", "mesures"));
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Path.Combine(dossier, "bonus.md"), texte.ToString());
            TestContext.Out.WriteLine(texte.ToString());
            Assert.Pass(texte.ToString());
        }
    }
}
