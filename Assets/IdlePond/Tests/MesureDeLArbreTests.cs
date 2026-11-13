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
    /// À quelle vie chaque nœud de l'arbre de technique est-il appris ? Les calendriers et
    /// les couples (A, B) de `Technique.cs` ont été posés par le calcul ; ceci les confronte
    /// au simulateur. Une MESURE, pas une garde : `Explicit`, elle écrit son tableau dans
    /// `Logs/mesures/arbre.md`.
    ///
    /// La branche Entretien n'y tombe jamais : le simulateur ne s'absente pas. La branche
    /// Construction non plus : rien ne crédite son compteur.
    /// </summary>
    [Explicit("mesure, à la demande")]
    public class MesureDeLArbreTests
    {
        const long GRAINE = 7;

        [Test]
        public void A_quelle_vie_chaque_noeud_est_appris()
        {
            var texte = new StringBuilder();
            texte.AppendLine($"# Arbre de technique — à quelle vie chaque nœud est appris (graine {GRAINE})");
            texte.AppendLine();
            texte.AppendLine("| Branche | Rang | Nœud | Monde entier (62 paliers, 15 vies) | Contenu livré (" + Assises.PALIERS_LIVRES + " paliers, 8 vies) |");
            texte.AppendLine("|---|---|---|---|---|");

            var entier = ViesDApprentissage(15, Constantes.NOMBRE_DE_PALIERS, out var dureesEntier);
            var livre = ViesDApprentissage(8, Assises.PALIERS_LIVRES, out var dureesLivre);

            foreach (var noeud in NoeudsTechnique.Tous)
                texte.AppendLine($"| {noeud.Branche} | {noeud.Rang} | {Textes.DuNoeud(noeud.Id).Nom} | {Vie(entier, noeud.Id)} | {Vie(livre, noeud.Id)} |");

            texte.AppendLine();
            texte.AppendLine("Durée des vies, en heures :");
            texte.AppendLine();
            texte.AppendLine("- monde entier : " + string.Join(" · ", dureesEntier.Select(h => h.ToString("F1", CultureInfo.InvariantCulture))));
            texte.AppendLine("- contenu livré : " + string.Join(" · ", dureesLivre.Select(h => h.ToString("F1", CultureInfo.InvariantCulture))));

            var dossier = Path.GetFullPath(Path.Combine("Logs", "mesures"));
            Directory.CreateDirectory(dossier);
            File.WriteAllText(Path.Combine(dossier, "arbre.md"), texte.ToString());
            TestContext.Out.WriteLine(texte.ToString());
            Assert.Pass(texte.ToString());
        }

        static string Vie(IReadOnlyDictionary<string, int> vies, string noeud) =>
            vies.TryGetValue(noeud, out var vie) ? "vie " + vie.ToString(CultureInfo.InvariantCulture) : "—";

        /// Pour chaque nœud, la vie (comptée à partir de 1) pendant laquelle il est apparu.
        static IReadOnlyDictionary<string, int> ViesDApprentissage(int cycles, int limite, out IReadOnlyList<double> durees)
        {
            var vies = new Dictionary<string, int>();
            void Observer(EtatJeu etat)
            {
                foreach (var id in etat.Permanent.NoeudsTechnique)
                    if (!vies.ContainsKey(id))
                    {
                        // Un nœud de la branche Renaissance tombe en rentrant : il est compté
                        // dans la vie qui vient de finir.
                        var noeud = NoeudsTechnique.ParId(id);
                        var enRentrant = noeud != null && noeud.Branche == BrancheTechnique.Renaissance;
                        vies[id] = etat.Permanent.NombreDeRenaissances + (enRentrant ? 0 : 1);
                    }
            }
            var resultat = Simuler(cycles, null, GRAINE, Observer, limite);
            durees = resultat.Cycles.Select(c => c.DureeEcouleeSecondes / 3600).ToList();
            return vies;
        }
    }
}
