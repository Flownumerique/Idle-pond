using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// La branche Entretien, que `MesureDeLArbreTests` ne voit pas : son compteur est l'absence
    /// créditée, et le simulateur ne s'absente pas. Ici, un joueur part et revient : une
    /// présence (`JouerUnePresence`), puis le VRAI crédit du jeu (`HorsLigne.Crediter`), en
    /// boucle, jusqu'à quinze renaissances ou un an de calendrier.
    ///
    /// Une MESURE, pas une garde : `Explicit`, elle écrit son tableau dans
    /// `Logs/mesures/entretien.md`.
    /// </summary>
    [Explicit("mesure, à la demande")]
    public class MesureDeLEntretienTests
    {
        const long GRAINE = 7;
        const int VIES = 15;
        const double JOURS_AU_PLUS = 365;

        static readonly (string Nom, double MinutesPresent, double HeuresAbsent)[] PROFILS =
        {
            ("Toutes les heures (5 min, puis 1 h)", 5, 1),
            ("Quatre fois par jour (10 min, puis 6 h)", 10, 6),
            ("Deux fois par jour (10 min, puis 12 h)", 10, 12),
            ("Une fois par jour (15 min, puis 24 h)", 15, 24),
        };

        static string F(double x, string format = "F1") => x.ToString(format, CultureInfo.InvariantCulture);

        [Test]
        public void A_quel_jour_chaque_noeud_d_entretien_est_appris()
        {
            var noeuds = NoeudsTechnique.Tous.Where(n => n.Branche == BrancheTechnique.Entretien).OrderBy(n => n.Rang).ToList();
            var texte = new StringBuilder();
            texte.AppendLine($"# Entretien — à quel jour chaque nœud est appris (graine {GRAINE}, {VIES} vies, {F(JOURS_AU_PLUS, "F0")} jours au plus)");
            texte.AppendLine();
            texte.AppendLine("Chaque case : le jour de calendrier, la vie en cours, l'assise la plus profonde jamais atteinte, le plafond d'absence après le nœud.");
            texte.AppendLine();
            texte.AppendLine("| Profil | " + string.Join(" | ", noeuds.Select(n => Textes.DuNoeud(n.Id).Nom)) +
                             " | Vies finies | Jours | Heures créditées | Heures perdues au plafond | Présence (h) |");
            texte.AppendLine("|---|" + string.Join("", noeuds.Select(_ => "---|")) + "---|---|---|---|---|");

            // Ce qui règle le couple (A, B) : l'absence créditée à la fin de chaque vie.
            var parVie = new StringBuilder();
            parVie.AppendLine("| Profil | " + string.Join(" | ", Enumerable.Range(1, VIES).Select(v => "vie " + v)) + " |");
            parVie.AppendLine("|---|" + string.Join("", Enumerable.Range(1, VIES).Select(_ => "---|")));

            foreach (var profil in PROFILS)
            {
                var etat = Reducteur.EtatInitial(GRAINE);
                var appris = new Dictionary<string, string>();
                double calendrier = 0, perdues = 0, presence = 0;
                var finsDeVie = new List<double>();
                while (etat.Permanent.NombreDeRenaissances < VIES && calendrier < JOURS_AU_PLUS * 24)
                {
                    etat = JouerUnePresence(etat, POLITIQUE_PAR_DEFAUT, profil.MinutesPresent * 60);
                    presence += profil.MinutesPresent / 60;
                    calendrier += profil.MinutesPresent / 60;

                    var retour = HorsLigne.Crediter(etat, 0, (long)(profil.HeuresAbsent * 3600 * 1000));
                    etat = retour.Etat;
                    perdues += profil.HeuresAbsent - retour.SecondesCreditees / 3600;
                    calendrier += profil.HeuresAbsent;

                    while (finsDeVie.Count < etat.Permanent.NombreDeRenaissances)
                        finsDeVie.Add(etat.Permanent.HeuresHorsLigneCreditees);
                    foreach (var noeud in noeuds)
                        if (!appris.ContainsKey(noeud.Id) && etat.Permanent.NoeudsTechnique.Contains(noeud.Id))
                            appris[noeud.Id] = $"jour {F(calendrier / 24)}, vie {etat.Permanent.NombreDeRenaissances + 1}, assise {Assise(etat)}, plafond {F(Horloge.CapHorsLigneSecondes(HorsLigne.CapHorsLigneCourantHeures(etat)) / 3600, "F0")} h";
                }
                texte.AppendLine($"| {profil.Nom} | " +
                                 string.Join(" | ", noeuds.Select(n => appris.TryGetValue(n.Id, out var quand) ? quand : "—")) +
                                 $" | {etat.Permanent.NombreDeRenaissances} | {F(calendrier / 24)} | {F(etat.Permanent.HeuresHorsLigneCreditees)} | {F(perdues)} | {F(presence)} |");
                parVie.AppendLine($"| {profil.Nom} | " + string.Join(" | ", finsDeVie.Select(h => F(h, "F0"))) + " |");
            }

            texte.AppendLine();
            texte.AppendLine("Heures créditées à la fin de chaque vie :");
            texte.AppendLine();
            texte.Append(parVie);
            texte.AppendLine();
            texte.AppendLine("Seuils du compteur (heures créditées) : " + string.Join(" · ", noeuds.Select(n =>
                $"{Textes.DuNoeud(n.Id).Nom} {F(Seuil(n.Rang), "F0")} h")));

            var dossier = Path.GetFullPath(Path.Combine("Logs", "mesures"));
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Path.Combine(dossier, "entretien.md"), texte.ToString());
            TestContext.Out.WriteLine(texte.ToString());
            Assert.Pass(texte.ToString());
        }

        static string Assise(EtatJeu etat)
        {
            var paliers = System.Math.Max(etat.Permanent.ProfondeurMaxAtteinte, etat.Cycle.PaliersOuverts);
            return Assises.Toutes.Last(a => a.IndexPremierPalier < System.Math.Max(1, paliers)).Id;
        }

        /// Le compteur auquel le nœud de ce rang tombe : l'inverse de `points = A·ln(1 + c/B)`.
        static double Seuil(int rang)
        {
            var (a, b) = Technique.COUPLES_A_B[BrancheTechnique.Entretien];
            var points = Constantes.COUTS_DE_NOEUD.Take(rang).Sum();
            return b * (System.Math.Exp(points / a) - 1);
        }
    }
}
