/*
 * Outils de test — comparaison d'états à la tolérance flottante près. Port de
 * `tests/outils.ts`.
 *
 * Le §12 formule le critère de l'équivalence de pas ainsi : « 480 appels à
 * dt = 60 s et 1 appel à dt = 8 h donnent le même état, à la tolérance
 * flottante près ». C'est cette tolérance-là, et rien de plus permissif.
 *
 * La comparaison passe par la SÉRIALISATION : deux états sont égaux quand leurs
 * saves le sont, nombre par nombre. C'est plus strict qu'une égalité de records
 * (l'ordre des clefs compte, comme sur le web) et cela sert tel quel au test de
 * parité, qui compare un état C# à une save produite par le TypeScript.
 */
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Adaptateurs;
using IdlePond.Nombres;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public static class OutilsDeTest
    {
        public const double ToleranceRelative = 1e-9;

        public const double H = 3600;

        private static bool Proche(double a, double b, double tolerance)
        {
            if (a.Equals(b)) return true;
            if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b)) return false;
            var echelle = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1e-300);
            return Math.Abs(a - b) / echelle <= tolerance;
        }

        private static bool ProcheGrand(GrandNombre a, GrandNombre b, double tolerance)
        {
            if (a.Eq(b)) return true;
            var ecart = a.Sub(b).Abs().Div(GrandNombre.Max(a.Abs(), b.Abs()).Add(1e-300));
            return ecart.ToNumber() <= tolerance;
        }

        public static void ComparerAToleranceFlottante(EtatJeu obtenu, EtatJeu attendu, double tolerance = ToleranceRelative)
        {
            ComparerJson(Persistance.Serialiser(obtenu), Persistance.Serialiser(attendu), tolerance, "");
        }

        /// <summary>
        /// Compare deux arbres JSON. Deux chaînes qui se lisent comme des grands
        /// nombres se comparent à la tolérance ; toute autre chaîne, à l'identique.
        /// </summary>
        public static void ComparerJson(JsonValeur obtenu, JsonValeur attendu, double tolerance, string chemin, bool textesNumeriques = true)
        {
            switch (attendu)
            {
                case JsonNombre na:
                    Assert.IsTrue(obtenu is JsonNombre, "nombre attendu en " + chemin + ", obtenu " + obtenu?.EnTexte());
                    var no = ((JsonNombre)obtenu).Valeur;
                    Assert.IsTrue(Proche(no, na.Valeur, tolerance),
                        "nombre divergent en " + chemin + " : " + NombreJs.VersTexte(no) + " vs " + NombreJs.VersTexte(na.Valeur));
                    return;
                case JsonTexte ta:
                    Assert.IsTrue(obtenu is JsonTexte, "chaîne attendue en " + chemin);
                    var to = ((JsonTexte)obtenu).Valeur;
                    if (to == ta.Valeur) return;
                    if (textesNumeriques && tolerance > 0 && GrandNombre.EssayerDeLire(ta.Valeur, out var ga) && GrandNombre.EssayerDeLire(to, out var go))
                    {
                        Assert.IsTrue(ProcheGrand(go, ga, tolerance), "grand nombre divergent en " + chemin + " : " + to + " vs " + ta.Valeur);
                        return;
                    }
                    Assert.AreEqual(ta.Valeur, to, "chaîne divergente en " + chemin);
                    return;
                case JsonTableau tab:
                    Assert.IsTrue(obtenu is JsonTableau, "tableau attendu en " + chemin);
                    var tob = (JsonTableau)obtenu;
                    Assert.AreEqual(tab.Count, tob.Count, "longueur divergente en " + chemin);
                    for (var i = 0; i < tab.Count; i += 1) ComparerJson(tob[i], tab[i], tolerance, chemin + "[" + i + "]", textesNumeriques);
                    return;
                case JsonObjet oa:
                    Assert.IsTrue(obtenu is JsonObjet, "objet attendu en " + chemin);
                    var oo = (JsonObjet)obtenu;
                    CollectionAssert.AreEqual(oa.Clefs, oo.Clefs, "clefs divergentes en " + chemin);
                    foreach (var clef in oa.Clefs)
                    {
                        ComparerJson(oo.Lire(clef), oa.Lire(clef), tolerance, chemin.Length == 0 ? clef : chemin + "." + clef, textesNumeriques);
                    }
                    return;
                default:
                    Assert.AreEqual(attendu?.EnTexte(), obtenu?.EnTexte(), "valeur divergente en " + chemin);
                    return;
            }
        }

        /// <summary>Retire commentaires de bloc et de ligne : les tests portent sur le code, pas sur la prose.</summary>
        public static string SansCommentaires(string source)
        {
            var sansBlocs = Regex.Replace(source, @"/\*[\s\S]*?\*/", " ");
            return Regex.Replace(sansBlocs, @"(^|[^:])//[^\n]*", "$1 ");
        }

        /// <summary>
        /// La racine du projet Unity : le dossier qui contient `Assets/IdlePond`. Dans
        /// l'éditeur, c'est le répertoire courant ; en vérification, un parent du
        /// binaire. On remonte donc depuis les deux.
        /// </summary>
        public static string RacineDuProjet()
        {
            foreach (var depart in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var dossier = new DirectoryInfo(depart);
                while (dossier != null)
                {
                    if (Directory.Exists(Path.Combine(dossier.FullName, "Assets", "IdlePond"))) return dossier.FullName;
                    dossier = dossier.Parent;
                }
            }
            throw new DirectoryNotFoundException("Impossible de trouver Assets/IdlePond depuis " + Directory.GetCurrentDirectory());
        }

        public static string DossierDuJeu() => Path.Combine(RacineDuProjet(), "Assets", "IdlePond");

        public static string[] FichiersCs(string dossier) =>
            Directory.GetFiles(dossier, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "Tests" + Path.DirectorySeparatorChar))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();

        public static string Relatif(string chemin) =>
            chemin.Substring(RacineDuProjet().Length).TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/');
    }
}
