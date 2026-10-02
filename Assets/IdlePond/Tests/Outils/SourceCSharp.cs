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

        /// Le code reste, la prose part : commentaires blanchis, littéraux réduits à
        /// `""` et `' '`. Une chaîne interpolée garde le CODE de ses trous —
        /// `$"a{x.ToString("R")}b"` donne `"{x.ToString("")}"` — parce que ce code
        /// peut contenir lui-même des chaînes, dont les guillemets ne ferment pas la
        /// chaîne extérieure.
        public static string SansCommentairesNiChaines(string source)
        {
            var sortie = new StringBuilder(source.Length);
            var i = 0;
            Code(source, ref i, sortie, false);
            return sortie.ToString();
        }

        static char A(string s, int i) => i < s.Length ? s[i] : '\0';

        /// Lit du code jusqu'à la fin du source, ou, dans un trou d'interpolation
        /// (`dansUnTrou`), jusqu'à son `}` fermant ou à son format (`:F2`), sans le
        /// consommer.
        static void Code(string source, ref int i, StringBuilder sortie, bool dansUnTrou)
        {
            var profondeur = 0;
            while (i < source.Length)
            {
                var c = source[i];
                var suivant = A(source, i + 1);
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
                var interpolee = (c == '$' && suivant == '"') || (c == '$' && suivant == '@') || (c == '@' && suivant == '$');
                var verbatim = (c == '@' && suivant == '"') || (c == '$' && suivant == '@') || (c == '@' && suivant == '$');
                if (verbatim || interpolee || c == '"')
                {
                    while (source[i] != '"') i++;
                    i++;
                    Chaine(source, ref i, sortie, verbatim, interpolee);
                    continue;
                }
                if (c == '\'')
                {
                    var fin = source.IndexOf('\'', i + (suivant == '\\' ? 3 : 2));
                    if (fin > i && fin - i <= 8) { sortie.Append("' '"); i = fin + 1; continue; }
                }
                if (dansUnTrou)
                {
                    if (c == '(' || c == '[' || c == '{') profondeur++;
                    else if (c == ')' || c == ']' || (c == '}' && profondeur > 0)) profondeur--;
                    else if (c == '}') return;
                    else if (c == ':' && profondeur == 0 && suivant != ':') return;
                }
                sortie.Append(c);
                i++;
            }
        }

        /// Lit une chaîne dont le guillemet ouvrant vient d'être consommé ; n'écrit
        /// que `"`, puis le code de chaque trou entre accolades, puis `"`.
        static void Chaine(string source, ref int i, StringBuilder sortie, bool verbatim, bool interpolee)
        {
            sortie.Append('"');
            while (i < source.Length)
            {
                var c = source[i];
                var suivant = A(source, i + 1);
                if (!verbatim && c == '\\') { i += 2; continue; }
                if (c == '"')
                {
                    if (verbatim && suivant == '"') { i += 2; continue; }
                    i++;
                    break;
                }
                if (interpolee && c == '{')
                {
                    if (suivant == '{') { i += 2; continue; }
                    i++;
                    sortie.Append('{');
                    Code(source, ref i, sortie, true);
                    // Le format (`:F2`, `,5`…) est du texte : on le saute jusqu'au `}`.
                    while (i < source.Length && source[i] != '}') i++;
                    i++;
                    sortie.Append('}');
                    continue;
                }
                if (interpolee && c == '}' && suivant == '}') { i += 2; continue; }
                i++;
            }
            sortie.Append('"');
        }

        /// <summary>
        /// Le contenu des littéraux de chaîne d'un source, commentaires écartés : ce que
        /// `SansCommentairesNiChaines` jette, et qu'il faut précisément relire pour savoir ce
        /// que le joueur lira. Une chaîne interpolée est lue comme une chaîne ordinaire ;
        /// les guillemets d'un trou en feraient des fragments en trop, sans conséquence
        /// pour une recherche de mots.
        /// </summary>
        public static IEnumerable<string> Litterales(string source)
        {
            var i = 0;
            while (i < source.Length)
            {
                var c = source[i];
                var suivant = A(source, i + 1);
                if (c == '/' && suivant == '/') { while (i < source.Length && source[i] != '\n') i++; continue; }
                if (c == '/' && suivant == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }
                if (c == '\'')
                {
                    var fin = source.IndexOf('\'', i + (suivant == '\\' ? 3 : 2));
                    if (fin > i && fin - i <= 8) { i = fin + 1; continue; }
                }
                var verbatim = c == '@' || (c == '$' && suivant == '@');
                if (c == '"' || ((c == '$' || c == '@') && (suivant == '"' || suivant == '@' || suivant == '$')))
                {
                    while (i < source.Length && source[i] != '"') i++;
                    i++;
                    var texte = new StringBuilder();
                    while (i < source.Length)
                    {
                        var d = source[i];
                        if (!verbatim && d == '\\') { if (A(source, i + 1) != '\0') texte.Append(source[i + 1]); i += 2; continue; }
                        if (d == '"')
                        {
                            if (verbatim && A(source, i + 1) == '"') { texte.Append('"'); i += 2; continue; }
                            i++;
                            break;
                        }
                        texte.Append(d);
                        i++;
                    }
                    yield return texte.ToString();
                    continue;
                }
                i++;
            }
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
