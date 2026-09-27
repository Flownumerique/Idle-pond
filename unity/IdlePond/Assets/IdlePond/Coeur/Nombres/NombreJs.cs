/*
 * IdlePond — les nombres écrits comme JavaScript les écrit.
 *
 * Une seule raison d'être : la sauvegarde. La version web écrit ses nombres avec
 * `Number.prototype.toString` (et `JSON.stringify`, qui l'appelle), et le port
 * Unity doit pouvoir relire une save web et en écrire une que le web relit — à
 * la chaîne près, pour que le test de déterminisme compare la même chose des
 * deux côtés.
 *
 * .NET sait produire les chiffres (« R » rend une écriture qui fait
 * l'aller-retour) ; il ne sait pas les disposer comme ECMAScript. Ce module ne
 * fait que la disposition — ECMA-262, Number::toString, étapes 5 à 10.
 */
using System;
using System.Globalization;
using System.Numerics;

namespace IdlePond.Nombres
{
    public static class NombreJs
    {
        /// <summary>Écriture d'un double, identique à `String(nombre)` en JavaScript.</summary>
        public static string VersTexte(double valeur)
        {
            if (double.IsNaN(valeur)) return "NaN";
            if (double.IsPositiveInfinity(valeur)) return "Infinity";
            if (double.IsNegativeInfinity(valeur)) return "-Infinity";
            if (valeur == 0) return "0";

            var signe = valeur < 0 ? "-" : "";
            ChiffresEtExposant(Math.Abs(valeur), out var chiffres, out var n);
            var k = chiffres.Length;

            string corps;
            if (k <= n && n <= 21)
            {
                corps = chiffres + new string('0', n - k);
            }
            else if (0 < n && n <= 21)
            {
                corps = chiffres.Substring(0, n) + "." + chiffres.Substring(n);
            }
            else if (-6 < n && n <= 0)
            {
                corps = "0." + new string('0', -n) + chiffres;
            }
            else
            {
                var e = n - 1;
                var signeExposant = e >= 0 ? "+" : "-";
                corps = k == 1
                    ? chiffres + "e" + signeExposant + Math.Abs(e).ToString(CultureInfo.InvariantCulture)
                    : chiffres.Substring(0, 1) + "." + chiffres.Substring(1) + "e" + signeExposant
                      + Math.Abs(e).ToString(CultureInfo.InvariantCulture);
            }
            return signe + corps;
        }

        /// <summary>
        /// `nombre.toFixed(decimales)` — arrondi sur la valeur binaire EXACTE, égalité
        /// vers le haut, comme ECMA-262. Les formats « F » de .NET arrondissent selon
        /// le runtime (Mono passe par quinze chiffres), et un coût de 1,005 ne doit
        /// pas s'afficher « 1.01 » ici et « 1.00 » sur le web.
        /// </summary>
        public static string ToFixed(double valeur, int decimales)
        {
            if (double.IsNaN(valeur)) return "NaN";
            if (Math.Abs(valeur) >= 1e21 || double.IsInfinity(valeur)) return VersTexte(valeur);

            var signe = valeur < 0 ? "-" : "";
            ValeurExacte(Math.Abs(valeur), out var numerateur, out var denominateur);
            var n = ArrondiAuPlusProche(numerateur * BigInteger.Pow(10, decimales), denominateur);
            if (n.IsZero) signe = "";

            var chiffres = n.ToString(CultureInfo.InvariantCulture);
            if (decimales == 0) return signe + chiffres;
            if (chiffres.Length <= decimales) chiffres = new string('0', decimales - chiffres.Length + 1) + chiffres;
            var coupure = chiffres.Length - decimales;
            return signe + chiffres.Substring(0, coupure) + "." + chiffres.Substring(coupure);
        }

        /// <summary>`nombre.toExponential(decimales)`, sur la valeur binaire exacte.</summary>
        public static string ToExponential(double valeur, int decimales)
        {
            if (double.IsNaN(valeur)) return "NaN";
            if (double.IsInfinity(valeur)) return valeur > 0 ? "Infinity" : "-Infinity";

            var signe = valeur < 0 ? "-" : "";
            var x = Math.Abs(valeur);
            string mantisse;
            int e;
            if (x == 0)
            {
                mantisse = new string('0', decimales + 1);
                e = 0;
            }
            else
            {
                ValeurExacte(x, out var numerateur, out var denominateur);
                e = (int)Math.Floor(Math.Log10(x));
                BigInteger n;
                // log10 peut se tromper d'un cran près d'une puissance de dix : on
                // corrige tant que n n'a pas exactement decimales + 1 chiffres.
                for (;;)
                {
                    var decalage = decimales - e;
                    n = decalage >= 0
                        ? ArrondiAuPlusProche(numerateur * BigInteger.Pow(10, decalage), denominateur)
                        : ArrondiAuPlusProche(numerateur, denominateur * BigInteger.Pow(10, -decalage));
                    var borneHaute = BigInteger.Pow(10, decimales + 1);
                    var borneBasse = BigInteger.Pow(10, decimales);
                    if (n >= borneHaute) e += 1;
                    else if (n < borneBasse) e -= 1;
                    else break;
                }
                mantisse = n.ToString(CultureInfo.InvariantCulture);
            }

            var corps = decimales == 0 ? mantisse : mantisse.Substring(0, 1) + "." + mantisse.Substring(1);
            return signe + corps + "e" + (e >= 0 ? "+" : "-") + Math.Abs(e).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>La valeur exacte d'un double positif fini, en fraction numérateur / dénominateur.</summary>
        private static void ValeurExacte(double positif, out BigInteger numerateur, out BigInteger denominateur)
        {
            var bits = BitConverter.DoubleToInt64Bits(positif);
            var exposantBrut = (int)((bits >> 52) & 0x7FF);
            var fraction = bits & 0xFFFFFFFFFFFFFL;
            long mantisse;
            int exposant;
            if (exposantBrut == 0)
            {
                mantisse = fraction;
                exposant = -1074;
            }
            else
            {
                mantisse = fraction | (1L << 52);
                exposant = exposantBrut - 1075;
            }
            if (exposant >= 0)
            {
                numerateur = new BigInteger(mantisse) * BigInteger.Pow(2, exposant);
                denominateur = BigInteger.One;
            }
            else
            {
                numerateur = new BigInteger(mantisse);
                denominateur = BigInteger.Pow(2, -exposant);
            }
        }

        /// <summary>Entier le plus proche de num / den (positifs), égalité vers le haut.</summary>
        private static BigInteger ArrondiAuPlusProche(BigInteger numerateur, BigInteger denominateur) =>
            BigInteger.Divide(numerateur * 2 + denominateur, denominateur * 2);

        /// <summary>`parseFloat` réduit à ce que la save peut contenir.</summary>
        public static bool EssayerDeLire(string texte, out double valeur)
        {
            return double.TryParse(
                texte.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out valeur);
        }

        /// <summary>
        /// Les chiffres significatifs, sans zéro de tête ni de queue, et `n` tel que
        /// valeur = 0,chiffres × 10^n — la notation du §7.1.12.2 d'ECMA-262.
        /// </summary>
        private static void ChiffresEtExposant(double positif, out string chiffres, out int n)
        {
            var texte = positif.ToString("R", CultureInfo.InvariantCulture);
            // Certains runtimes Mono ont connu des « R » qui ne revenaient pas au
            // même double. G17 est toujours exact, au prix de chiffres en trop.
            if (!double.TryParse(texte, NumberStyles.Float, CultureInfo.InvariantCulture, out var relu)
                || relu != positif)
            {
                texte = positif.ToString("G17", CultureInfo.InvariantCulture);
            }

            var exposant = 0;
            var indexE = texte.IndexOfAny(new[] { 'E', 'e' });
            if (indexE >= 0)
            {
                exposant = int.Parse(texte.Substring(indexE + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                texte = texte.Substring(0, indexE);
            }

            var point = texte.IndexOf('.');
            string entiere = point >= 0 ? texte.Substring(0, point) : texte;
            string fraction = point >= 0 ? texte.Substring(point + 1) : "";
            var brut = entiere + fraction;
            var position = entiere.Length + exposant;

            var debut = 0;
            while (debut < brut.Length - 1 && brut[debut] == '0')
            {
                debut += 1;
                position -= 1;
            }
            var fin = brut.Length;
            while (fin > debut + 1 && brut[fin - 1] == '0') fin -= 1;

            chiffres = brut.Substring(debut, fin - debut);
            n = position;
        }
    }
}
