using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class ArchitectureTests
    {
        static readonly string[] Dossiers = { "Assets/IdlePond/Noyau", "Assets/IdlePond/Simulateur" };

        static readonly (string Motif, string Quoi)[] Interdits =
        {
            (@"\bDateTime\s*\.\s*(Now|UtcNow|Today)\b", "horloge système"),
            (@"\bDateTimeOffset\s*\.\s*(Now|UtcNow)\b", "horloge système"),
            (@"\bEnvironment\s*\.\s*TickCount", "horloge système"),
            (@"\bStopwatch\b", "horloge système"),
            (@"\bSystem\s*\.\s*Random\b|\bnew\s+Random\s*\(", "hasard hors de l'état"),
            (@"\bGuid\s*\.\s*NewGuid\b", "hasard hors de l'état"),
            (@"\bUnityEngine\b|\bUnityEditor\b", "moteur dans le noyau"),
            (@"\bFile\s*\.|\bDirectory\s*\.|\bConsole\s*\.", "entrée/sortie dans le noyau"),
        };

        [Test, Description("le noyau contient des fichiers à vérifier")]
        public void Le_noyau_contient_des_fichiers_a_verifier()
        {
            Assert.That(SourceCSharp.Fichiers("Assets/IdlePond/Noyau").Count(), Is.GreaterThan(10));
        }

        [Test, Description("le noyau et le simulateur ne touchent ni horloge, ni hasard, ni moteur, ni fichier")]
        public void Le_noyau_ne_touche_ni_horloge_ni_hasard_ni_moteur_ni_fichier()
        {
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from interdit in Interdits
                          where Regex.IsMatch(code, interdit.Motif)
                          select $"{Path.GetFileName(fichier)} : {interdit.Quoi}").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("les réglages et le calcul du son sont purs : ni moteur, ni fichier")]
        public void Les_reglages_sont_purs()
        {
            var fautes = (from nom in new[] { "Reglages.cs", "Son.cs" }
                          let fichier = Path.Combine("Assets/IdlePond/Jeu/Reglages", nom)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from motif in new[] { @"\bUnityEngine\b|\bUnityEditor\b", @"\bFile\s*\.|\bDirectory\s*\." }
                          where Regex.IsMatch(code, motif)
                          select $"{nom} : {motif}").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("le jeu ne lit l'heure que par HorlogeSysteme, et ne tire aucun hasard hors de l'état")]
        public void Le_jeu_ne_lit_l_heure_que_par_HorlogeSysteme()
        {
            // La pureté du noyau s'arrête à sa frontière : le jeu a des fichiers, un moteur,
            // une horloge. Mais il n'en a QU'UNE — `HorlogeSysteme` — et aucun hasard qui
            // ne vienne du PRNG de l'état. Une interface qui tirerait au sort ou lirait
            // l'heure de son côté ferait diverger l'écran de ce que la partie a calculé.
            var interdits = new[]
            {
                (@"\bDateTime\s*\.\s*(Now|UtcNow|Today)\b", "horloge système"),
                (@"\bDateTimeOffset\s*\.\s*(Now|UtcNow)\b", "horloge système"),
                (@"\bEnvironment\s*\.\s*TickCount", "horloge système"),
                (@"\bStopwatch\b", "horloge système"),
                (@"\bSystem\s*\.\s*Random\b|\bnew\s+Random\s*\(|\bUnityEngine\s*\.\s*Random\b|\bRandom\s*\.\s*(Range|value|insideUnitCircle)\b", "hasard hors de l'état"),
                (@"\bGuid\s*\.\s*NewGuid\b", "hasard hors de l'état"),
            };
            var fautes = (from fichier in SourceCSharp.Fichiers("Assets/IdlePond/Jeu")
                          where Path.GetFileName(fichier) != "Horloge.cs"
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from interdit in interdits
                          where Regex.IsMatch(code, interdit.Item1)
                          select $"{Path.GetFileName(fichier)} : {interdit.Item2}").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("l'interface ne lit aucune horloge : elle montre l'état, elle ne le date pas")]
        public void L_interface_ne_lit_aucune_horloge()
        {
            // La scène dessinée peut animer sur le temps du moteur (un voile qui monte en
            // 0,9 s) : c'est du rendu. Les panneaux, eux, ne datent rien — leurs minuteries
            // passent par le planificateur d'UI Toolkit, jamais par `Time`.
            var fautes = (from fichier in SourceCSharp.Fichiers("Assets/IdlePond/Jeu/UI")
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          where Regex.IsMatch(code, @"\bTime\s*\.\s*\w+")
                          select Path.GetFileName(fichier)).ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("aucun état hors du réducteur : pas de champ statique modifiable")]
        public void Aucun_etat_hors_du_reducteur()
        {
            var champModifiable = new Regex(@"^\s*(public|private|internal|protected)?\s*static\s+(?!readonly\b|class\b|partial\b|extern\b)[\w<>,\[\]\s?.()]+?\s+\w+\s*(=[^>]|;)", RegexOptions.Multiline);
            // Une propriété auto-implémentée statique avec `set` (public ou privé) est un
            // champ modifiable déguisé — le premier motif ne la voit pas, faute de `=` ou
            // de `;` juste après le nom : elle se referme sur une accolade. `init` n'est
            // pas valide sur un membre statique, donc tout `set` suffit à la qualifier.
            var proprieteModifiable = new Regex(@"^\s*(public|private|internal|protected)?\s*static\s+(?!class\b|partial\b|extern\b)[\w<>,\[\]\s?.()]+?\s+\w+\s*\{[^{}]*\bset\b[^{}]*\}", RegexOptions.Multiline);
            var motifs = new[] { champModifiable, proprieteModifiable };
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from motif in motifs
                          from Match m in motif.Matches(code)
                          where !m.Value.Contains("(")
                          select $"{Path.GetFileName(fichier)} : {m.Value.Trim()}").ToList();
            Assert.That(fautes, Is.Empty);
        }
    }
}
