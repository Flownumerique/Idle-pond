using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Les tests d'architecture et de lexique portent sur le CODE, pas sur la prose
    /// ni sur les données : commentaires et littéraux de chaîne sont blanchis avant
    /// d'être lus. Les chaînes affichées ont leur propre test (LexiqueTests).
    /// </summary>
    public static class SourceCSharp
    {
        public static IEnumerable<string> Fichiers(string dossier) =>
            Directory.GetFiles(Path.GetFullPath(dossier), "*.cs", SearchOption.AllDirectories).OrderBy(f => f);

        public static string SansCommentairesNiChaines(string source)
        {
            var sortie = new StringBuilder(source.Length);
            var i = 0;
            while (i < source.Length)
            {
                var c = source[i];
                var suivant = i + 1 < source.Length ? source[i + 1] : '\0';
                if (c == '/' && suivant == '/')
                {
                    while (i < source.Length && source[i] != '\n') i++;
                    continue;
                }
                if (c == '/' && suivant == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }
                var verbatim = (c == '@' && suivant == '"') || (c == '$' && suivant == '@') || (c == '@' && suivant == '$');
                if (verbatim || c == '"' || (c == '$' && suivant == '"'))
                {
                    while (source[i] != '"') i++;
                    i++;
                    while (i < source.Length)
                    {
                        if (!verbatim && source[i] == '\\') { i += 2; continue; }
                        if (source[i] == '"')
                        {
                            if (verbatim && i + 1 < source.Length && source[i + 1] == '"') { i += 2; continue; }
                            break;
                        }
                        i++;
                    }
                    i++;
                    sortie.Append("\"\"");
                    continue;
                }
                if (c == '\'')
                {
                    var fin = source.IndexOf('\'', i + (suivant == '\\' ? 3 : 2));
                    if (fin > i && fin - i <= 8) { sortie.Append("' '"); i = fin + 1; continue; }
                }
                sortie.Append(c);
                i++;
            }
            return sortie.ToString();
        }

        static readonly Regex Identifiant = new Regex(@"[\p{L}_][\p{L}\p{Nd}_]*", RegexOptions.CultureInvariant);

        /// Chaque identifiant découpé en mots : `CoutDInsufflation` → cout, d, insufflation ;
        /// `SOUFFLE_BASE` → souffle, base. Minuscules, sans accents.
        public static IEnumerable<string> Mots(string code)
        {
            foreach (Match m in Identifiant.Matches(code))
            {
                foreach (var morceau in Regex.Split(m.Value, @"_|(?<=\p{Ll})(?=\p{Lu})|(?<=\p{Lu})(?=\p{Lu}\p{Ll})"))
                    if (morceau.Length > 0) yield return SansAccents(morceau.ToLowerInvariant());
            }
        }

        public static string SansAccents(string texte)
        {
            var decompose = texte.Normalize(NormalizationForm.FormD);
            var sortie = new StringBuilder(decompose.Length);
            foreach (var c in decompose)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sortie.Append(c);
            return sortie.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
