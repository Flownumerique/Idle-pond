using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Noyau;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// « À la tolérance flottante près » (§12), et rien de plus permissif. Un
    /// Decimal s'écrit « m e » dans un instantané : deux chaînes de cette forme se
    /// comparent en nombres ; toute autre chaîne se compare à l'identique.
    ///
    /// Les références JSON portent leurs nombres non entiers en CHAÎNES (voir
    /// `References.Lire`) : un nombre JSON d'un côté face à une chaîne numérique de
    /// l'autre se compare donc en doubles, la chaîne relue par `AnalyseDouble`,
    /// correctement arrondi — jamais par le `double.Parse` du runtime.
    /// </summary>
    public static class Comparateur
    {
        public const double TOLERANCE_RELATIVE = 1e-9;

        static readonly Regex FormeDecimal = new Regex(@"^-?[0-9.]+(e[+-]?[0-9.]+)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        static bool Proche(double a, double b, double tolerance)
        {
            if (a.Equals(b)) return true;
            if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b)) return false;
            var echelle = Math.Max(Math.Max(Math.Abs(a), Math.Abs(b)), 1e-300);
            return Math.Abs(a - b) / echelle <= tolerance;
        }

        static bool EstNombre(JToken t) => t.Type == JTokenType.Integer || t.Type == JTokenType.Float;

        static bool EstChaineNumerique(JToken t) => t.Type == JTokenType.String && FormeDecimal.IsMatch((string)t);

        /// Un nombre JSON lu sans aucune conversion dépendante de la culture.
        static double Nombre(JToken t)
        {
            if (t.Type == JTokenType.String) return AnalyseDouble.Lire((string)t);
            switch (((JValue)t).Value)
            {
                case double d: return d;
                case float f: return f;
                case long l: return l;
                case int i: return i;
                case ulong u: return u;
                case uint u: return u;
                case decimal m: return (double)m;
                case System.Numerics.BigInteger b: return (double)b;
                default: throw new InvalidOperationException($"nombre JSON de type inattendu : {((JValue)t).Value?.GetType()}");
            }
        }

        static string Ecrire(double x) => x.ToString("R", CultureInfo.InvariantCulture);

        /// Deux Decimal, à la tolérance près — `comparerAToleranceFlottante` sur des Decimal.
        public static void ComparerATolerance(Decimal obtenu, Decimal attendu, double tolerance = TOLERANCE_RELATIVE) =>
            ComparerATolerance(new JValue(obtenu.EnMantisseExposant()), new JValue(attendu.EnMantisseExposant()), tolerance);

        public static void ComparerATolerance(JToken obtenu, JToken attendu, double tolerance = TOLERANCE_RELATIVE, string chemin = "")
        {
            if (EstChaineNumerique(attendu) && EstChaineNumerique(obtenu))
            {
                var a = Decimal.Parse((string)obtenu);
                var b = Decimal.Parse((string)attendu);
                if (a.Eq(b)) return;
                var ecart = a.Sub(b).Abs().Div(Decimal.Max(a.Abs(), b.Abs()).Add(1e-300)).ToNumber();
                Assert.That(ecart, Is.LessThanOrEqualTo(tolerance), $"Decimal divergent en {chemin} : {obtenu} vs {attendu}");
                return;
            }
            if ((EstNombre(attendu) || EstChaineNumerique(attendu)) && (EstNombre(obtenu) || EstChaineNumerique(obtenu)))
            {
                var a = Nombre(obtenu);
                var b = Nombre(attendu);
                Assert.That(Proche(a, b, tolerance), Is.True, $"nombre divergent en {chemin} : {Ecrire(a)} vs {Ecrire(b)}");
                return;
            }
            if (attendu is JArray ta)
            {
                Assert.That(obtenu, Is.InstanceOf<JArray>(), $"tableau attendu en {chemin}");
                var to = (JArray)obtenu;
                Assert.That(to.Count, Is.EqualTo(ta.Count), $"longueur divergente en {chemin}");
                for (var i = 0; i < ta.Count; i++) ComparerATolerance(to[i], ta[i], tolerance, $"{chemin}[{i}]");
                return;
            }
            if (attendu is JObject oa)
            {
                Assert.That(obtenu, Is.InstanceOf<JObject>(), $"objet attendu en {chemin}");
                var oo = (JObject)obtenu;
                var clefs = oa.Properties().Select(p => p.Name).Union(oo.Properties().Select(p => p.Name));
                foreach (var clef in clefs)
                {
                    var suite = chemin == "" ? clef : $"{chemin}.{clef}";
                    Assert.That(oo.ContainsKey(clef), Is.True, $"clef absente de l'obtenu : {suite}");
                    Assert.That(oa.ContainsKey(clef), Is.True, $"clef en trop dans l'obtenu : {suite}");
                    ComparerATolerance(oo[clef], oa[clef], tolerance, suite);
                }
                return;
            }
            Assert.That(JToken.DeepEquals(obtenu, attendu), Is.True, $"valeur divergente en {chemin} : {obtenu} vs {attendu}");
        }
    }
}
