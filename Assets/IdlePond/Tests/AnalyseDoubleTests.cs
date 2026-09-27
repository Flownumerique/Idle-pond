using System;
using System.Globalization;
using System.Threading;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class AnalyseDoubleTests
    {
        /// Motifs binaires produits par V8 (`DataView.getBigUint64`), à comparer bit à bit avec
        /// `BitConverter.DoubleToInt64Bits`. Aucun de ces textes n'a le bit de signe posé : les
        /// convertir en `long` par `NumberStyles.HexNumber` reste donc sans ambiguïté.
        static readonly (string texte, string bitsHex)[] VecteursV8 =
        {
            ("3.0000000000025", "40080000000015fd"),
            ("0.03", "3f9eb851eb851eb8"),
            ("0.1", "3fb999999999999a"),
            ("2.4", "4003333333333333"),
            ("1.15", "3ff2666666666666"),
            ("123.45", "405edccccccccccd"),
            ("1e23", "44b52d02c7e14af6"),
            ("1e-323", "0000000000000002"),
            ("5e-324", "0000000000000001"),
            ("1.7976931348623157e308", "7fefffffffffffff"),
            ("2.2250738585072014e-308", "0010000000000000"),
            ("2.225073858507201e-308", "000fffffffffffff"),
            ("9007199254740993", "4340000000000000"),
            ("0.30000000000000004", "3fd3333333333334"),
            ("4.9406564584124654e-324", "0000000000000001"),
            ("1e308", "7fe1ccf385ebc8a0"),
            ("1e-5", "3ee4f8b588e368f1"),
            ("123456789.123", "419d6f34547df3b6"),
            ("9.99189671906449e11", "426d148b0dfc4e5e"),
            ("3.3333333333333335", "400aaaaaaaaaaaab"),
            ("1e22", "4480f0cf064dd592"),
            ("8.98846567431158e307", "7fe0000000000000"),
            ("0.000001", "3eb0c6f7a0b5ed8d"),
            ("1e21", "444b1ae4d6e2ef50"),
            ("7.450580596923828e-9", "3e40000000000000"),
        };

        static long BitsAttendus(string hex) => long.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        [Test, Description("chaque vecteur V8 retombe sur le même motif binaire")]
        public void Chaque_vecteur_V8_retombe_sur_le_meme_motif_binaire()
        {
            foreach (var (texte, bitsHex) in VecteursV8)
            {
                var attendu = BitsAttendus(bitsHex);
                var bits = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire(texte));
                Assert.That(bits, Is.EqualTo(attendu), $"{texte} : obtenu 0x{bits:x16} attendu 0x{attendu:x16}");
            }
        }

        [Test, Description("le signe se lit indépendamment de la magnitude")]
        public void Le_signe_se_lit_independamment_de_la_magnitude()
        {
            var positif = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("0.1"));
            var negatif = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("-0.1"));
            Assert.That(negatif, Is.EqualTo(positif ^ long.MinValue));
        }

        [Test, Description("un exposant qui déborde donne l'infini, un qui sous-déborde donne zéro")]
        public void Un_exposant_qui_deborde_donne_l_infini_un_qui_sous_deborde_donne_zero()
        {
            Assert.That(double.IsPositiveInfinity(AnalyseDouble.Lire("1e400")), Is.True);
            Assert.That(AnalyseDouble.Lire("1e-400"), Is.EqualTo(0.0));
            Assert.That(double.IsNegative(AnalyseDouble.Lire("1e-400")), Is.False, "0.0, pas -0.0");
        }

        [Test, Description("Infinity, -Infinity et NaN se lisent tels quels")]
        public void Infinity_moins_Infinity_et_NaN_se_lisent_tels_quels()
        {
            Assert.That(double.IsPositiveInfinity(AnalyseDouble.Lire("Infinity")), Is.True);
            Assert.That(double.IsNegativeInfinity(AnalyseDouble.Lire("-Infinity")), Is.True);
            Assert.That(double.IsNaN(AnalyseDouble.Lire("NaN")), Is.True);
        }

        [Test, Description("les formes 1E+21 et 1.5e+25 se lisent comme leurs équivalentes")]
        public void Les_formes_1E_21_et_1_5e_25_se_lisent_comme_leurs_equivalentes()
        {
            var e21 = BitsAttendus("444b1ae4d6e2ef50");
            Assert.That(BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("1E+21")), Is.EqualTo(e21));

            // Pas de vecteur V8 pour 1.5e+25 : cohérence interne entre trois écritures du même
            // nombre plutôt qu'un oracle externe.
            var a = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("1.5e+25"));
            var b = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("15e24"));
            var c = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire("0.15e26"));
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.EqualTo(c));
        }

        [Test, Description("une chaîne invalide lève FormatException")]
        public void Une_chaine_invalide_leve_FormatException()
        {
            Assert.Throws<FormatException>(() => AnalyseDouble.Lire("abc"));
            Assert.Throws<FormatException>(() => AnalyseDouble.Lire(""));
            Assert.Throws<FormatException>(() => AnalyseDouble.Lire("1e"));
        }

        [Test, Description("la culture fr-FR ne change rien : jamais de virgule attendue en entrée")]
        public void La_culture_fr_FR_ne_change_rien()
        {
            var avant = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                foreach (var (texte, bitsHex) in VecteursV8)
                {
                    var attendu = BitsAttendus(bitsHex);
                    var bits = BitConverter.DoubleToInt64Bits(AnalyseDouble.Lire(texte));
                    Assert.That(bits, Is.EqualTo(attendu), texte);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = avant;
            }
        }

        [Test, Description("aller-retour sur 10000 doubles pseudo-aléatoires (sans NaN ni infini)")]
        public void Aller_retour_sur_10000_doubles_pseudo_aleatoires()
        {
            // xorshift64, graine fixe : reproductible, jamais 0 (sinon la suite reste à 0).
            var graine = 88172645463325252UL;
            var n = 0;
            while (n < 10000)
            {
                graine ^= graine << 13;
                graine ^= graine >> 7;
                graine ^= graine << 17;
                var bits = unchecked((long)graine);
                if (((bits >> 52) & 0x7FF) == 0x7FF) continue; // exclut NaN et ±Infini
                n++;
                var x = BitConverter.Int64BitsToDouble(bits);
                var texte = x.ToString("G17", CultureInfo.InvariantCulture);
                var relu = AnalyseDouble.Lire(texte);
                Assert.That(BitConverter.DoubleToInt64Bits(relu), Is.EqualTo(bits), texte);
            }
        }
    }
}
