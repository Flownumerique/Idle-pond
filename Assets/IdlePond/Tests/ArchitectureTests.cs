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

        [Test, Description("aucun état hors du réducteur : pas de champ statique modifiable")]
        public void Aucun_etat_hors_du_reducteur()
        {
            var champModifiable = new Regex(@"^\s*(public|private|internal|protected)?\s*static\s+(?!readonly\b|class\b|partial\b|extern\b)[\w<>,\[\]\s?.()]+?\s+\w+\s*(=[^>]|;)", RegexOptions.Multiline);
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from Match m in champModifiable.Matches(code)
                          where !m.Value.Contains("(")
                          select $"{Path.GetFileName(fichier)} : {m.Value.Trim()}").ToList();
            Assert.That(fautes, Is.Empty);
        }
    }
}
