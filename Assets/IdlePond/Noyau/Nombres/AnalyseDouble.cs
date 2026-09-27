using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace IdlePond.Noyau
{
    /// <summary>
    /// Chaîne décimale → double, correctement arrondi (IEEE 754, pair au plus proche en cas
    /// d'égalité), par arithmétique exacte sur <see cref="BigInteger"/>.
    ///
    /// Nécessaire parce que le <c>double.Parse</c> du Mono d'Unity Editor n'est pas
    /// correctement arrondi (constaté : « 3.0000000000025 » perd le dernier bit — voir
    /// task-3-report.md, fix round 1). <c>Convert.ToDouble</c> et <c>Newtonsoft.Json</c> en
    /// souffrent de la même façon puisqu'ils s'appuient dessus au bout du compte. Cette classe
    /// n'appelle jamais <c>double.Parse</c>, <c>decimal.Parse</c> ni <c>Convert.ToDouble</c> en
    /// interne : uniquement des chiffres ASCII lus à la main et de l'arithmétique BigInteger.
    /// </summary>
    public static class AnalyseDouble
    {
        const int EXPOSANT_DEBORDEMENT = 310;
        const int EXPOSANT_SOUS_DEBORDEMENT = -330;
        const int EXPOSANT_NORMAL_MIN = -1022;
        const int EXPOSANT_NORMAL_MAX = 1023;

        public static double Lire(string texte)
        {
            if (!EssayerDeLire(texte, out var valeur))
                throw new FormatException("[AnalyseDouble] Chaîne invalide : " + texte);
            return valeur;
        }

        public static bool EssayerDeLire(string texte, out double valeur)
        {
            valeur = 0.0;
            if (string.IsNullOrEmpty(texte)) return false;
            var s = texte.Trim();
            if (s.Length == 0) return false;
            if (s == "NaN") { valeur = double.NaN; return true; }

            var negatif = false;
            var i = 0;
            if (s[0] == '+' || s[0] == '-') { negatif = s[0] == '-'; i = 1; }

            if (string.CompareOrdinal(s, i, "Infinity", 0, s.Length - i) == 0)
            {
                valeur = negatif ? double.NegativeInfinity : double.PositiveInfinity;
                return true;
            }

            // Chiffres entiers puis fractionnaires, concaténés (le point ne compte pas comme
            // chiffre) ; nbApresPoint retient combien viennent après le point pour l'exposant.
            var chiffres = new StringBuilder();
            var pointVu = false;
            var nbApresPoint = 0;
            var vuChiffre = false;
            var j = i;
            for (; j < s.Length; j++)
            {
                var c = s[j];
                if (c >= '0' && c <= '9')
                {
                    chiffres.Append(c);
                    vuChiffre = true;
                    if (pointVu) nbApresPoint++;
                }
                else if (c == '.' && !pointVu)
                {
                    pointVu = true;
                }
                else break;
            }
            if (!vuChiffre) return false;

            var exposantLu = 0;
            if (j < s.Length && (s[j] == 'e' || s[j] == 'E'))
            {
                j++;
                var expNeg = false;
                if (j < s.Length && (s[j] == '+' || s[j] == '-')) { expNeg = s[j] == '-'; j++; }
                var expDebut = j;
                while (j < s.Length && s[j] >= '0' && s[j] <= '9') j++;
                if (j == expDebut) return false; // « 1e » sans chiffre
                var expTexte = s.Substring(expDebut, j - expDebut);
                // Une chaîne d'exposant absurdement longue ("1e999999999999") mènerait de toute
                // façon à ±Infini ou ±0 ; plafonner évite un BigInteger.Pow ou un int.Parse inutiles.
                if (expTexte.Length > 9) exposantLu = expNeg ? -1_000_000_000 : 1_000_000_000;
                else
                {
                    exposantLu = int.Parse(expTexte, NumberStyles.None, CultureInfo.InvariantCulture);
                    if (expNeg) exposantLu = -exposantLu;
                }
            }
            if (j != s.Length) return false; // caractères de trop (ex. « 12.5xyz »)

            // valeur = D × 10^e, D chiffres purs, jamais passés par un parseur de double.
            var d = BigInteger.Parse(chiffres.ToString(), NumberStyles.None, CultureInfo.InvariantCulture);
            if (d.IsZero) { valeur = negatif ? -0.0 : 0.0; return true; }

            var e = exposantLu - nbApresPoint;
            var nbChiffresD = d.ToString(CultureInfo.InvariantCulture).Length;

            if (e + nbChiffresD > EXPOSANT_DEBORDEMENT)
            {
                valeur = negatif ? double.NegativeInfinity : double.PositiveInfinity;
                return true;
            }
            if (e + nbChiffresD < EXPOSANT_SOUS_DEBORDEMENT)
            {
                valeur = negatif ? -0.0 : 0.0;
                return true;
            }

            BigInteger n, q;
            if (e >= 0) { n = d * BigInteger.Pow(10, e); q = BigInteger.One; }
            else { n = d; q = BigInteger.Pow(10, -e); }

            var k = BitLength(n) - BitLength(q) - 54;
            BigInteger qq, rem;
            while (true)
            {
                (qq, rem) = DiviserAvecDecalage(n, q, k);
                if (qq < (BigInteger.One << 53)) { k--; continue; }
                if (qq >= (BigInteger.One << 54)) { k++; continue; }
                break;
            }

            var e2 = k + 53;

            if (e2 < EXPOSANT_NORMAL_MIN)
            {
                // Sous-normal : un seul arrondi, à la position -1074 (un bit de garde de plus :
                // -1075), jamais un arrondi « normal » suivi d'un second arrondi vers le sous-normal.
                var deficit = EXPOSANT_NORMAL_MIN - e2;
                k += deficit;
                (qq, rem) = DiviserAvecDecalage(n, q, k);
                AssemblerSousNormal(qq, rem, negatif, out valeur);
                return true;
            }

            var mantisse = ArrondirAuPair(qq, rem);
            if (mantisse == (BigInteger.One << 53)) { mantisse = BigInteger.One << 52; e2++; }

            if (e2 > EXPOSANT_NORMAL_MAX)
            {
                valeur = negatif ? double.NegativeInfinity : double.PositiveInfinity;
                return true;
            }

            var fraction = mantisse - (BigInteger.One << 52);
            Assembler(negatif, (long)(e2 + 1023), fraction, out valeur);
            return true;
        }

        /// floor(n × 2^-k / q) et son reste, pour k de n'importe quel signe.
        static (BigInteger q, BigInteger rem) DiviserAvecDecalage(BigInteger n, BigInteger q, int k)
        {
            BigInteger numerateur, denominateur;
            if (k >= 0) { numerateur = n; denominateur = q << k; }
            else { numerateur = n << -k; denominateur = q; }
            var quotient = BigInteger.DivRem(numerateur, denominateur, out var reste);
            return (quotient, reste);
        }

        /// qq tient sur 54 bits (2^53 ≤ qq < 2^54) : son bit 0 est le bit de garde, `rem` le bit
        /// collant. Rend une mantisse de 53 bits (implicite compris), éventuellement 2^53 (retenue,
        /// à gérer par l'appelant).
        static BigInteger ArrondirAuPair(BigInteger qq, BigInteger rem)
        {
            var bitDeGarde = qq & BigInteger.One;
            var mantisse = qq >> 1;
            if (bitDeGarde.IsZero) return mantisse;
            if (!rem.IsZero) return mantisse + 1;
            return mantisse.IsEven ? mantisse : mantisse + 1;
        }

        static void AssemblerSousNormal(BigInteger qq, BigInteger rem, bool negatif, out double valeur)
        {
            // qq a ici de 0 à 53 bits : même règle d'arrondi (bit de garde + collant), mais sans
            // bit implicite puisqu'un sous-normal n'en a pas.
            var bitDeGarde = qq.IsZero ? BigInteger.Zero : qq & BigInteger.One;
            var mantisse = qq >> 1;
            BigInteger finale;
            if (bitDeGarde.IsZero) finale = mantisse;
            else if (!rem.IsZero) finale = mantisse + 1;
            else finale = mantisse.IsEven ? mantisse : mantisse + 1;

            if (finale.IsZero) { valeur = negatif ? -0.0 : 0.0; return; }

            if (finale == (BigInteger.One << 52))
                Assembler(negatif, 1, BigInteger.Zero, out valeur); // arrondi jusqu'au plus petit normal
            else
                Assembler(negatif, 0, finale, out valeur);
        }

        static void Assembler(bool negatif, long exposantBiaise, BigInteger fraction, out double valeur)
        {
            var bits = (negatif ? long.MinValue : 0L) | (exposantBiaise << 52) | (long)fraction;
            valeur = BitConverter.Int64BitsToDouble(bits);
        }

        /// Nombre de bits significatifs d'un BigInteger positif ou nul.
        static int BitLength(BigInteger v)
        {
            if (v.IsZero) return 0;
            var octets = v.ToByteArray();
            var index = octets.Length - 1;
            while (index > 0 && octets[index] == 0) index--;
            var bits = index * 8;
            var haut = octets[index];
            while (haut != 0) { bits++; haut >>= 1; }
            return bits;
        }
    }
}
