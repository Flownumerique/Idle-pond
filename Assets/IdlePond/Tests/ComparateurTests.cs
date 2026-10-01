using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le comparateur lui-même : « à la tolérance flottante près », et rien de plus
    /// permissif. Égalité exacte pour les entiers (l'état du PRNG en est un), et un
    /// NaN n'est jamais égal à rien, pas même à lui-même — comme le `===` du TypeScript.
    /// </summary>
    public class ComparateurTests
    {
        static void Echoue(JToken obtenu, JToken attendu) =>
            Assert.Throws<AssertionException>(() => Comparateur.ComparerATolerance(obtenu, attendu));

        [Test, Description("NaN contre NaN échoue toujours ; une infinité ne vaut que la même")]
        public void NaN_contre_NaN_echoue_toujours_une_infinite_ne_vaut_que_la_meme()
        {
            Echoue(new JValue(double.NaN), new JValue(double.NaN));
            var nan = new Decimal(double.NaN).EnMantisseExposant();
            Echoue(new JValue(nan), new JValue(nan));
            Echoue(new JValue("NaN"), new JValue("NaN"));
            Echoue(new JObject { ["x"] = double.NaN }, new JObject { ["x"] = double.NaN });
            // Une infinité vaut la même infinité, et elle seule — `Infinity === Infinity`.
            Comparateur.ComparerATolerance(new JValue(double.PositiveInfinity), new JValue(double.PositiveInfinity));
            Echoue(new JValue(double.NegativeInfinity), new JValue(double.PositiveInfinity));
            Echoue(new JValue("-Infinity"), new JValue("Infinity"));
        }

        [Test, Description("deux entiers se comparent exactement, sans tolérance")]
        public void Deux_entiers_se_comparent_exactement_sans_tolerance()
        {
            Echoue(new JValue(1800000001L), new JValue(1800000000L));
            Echoue(new JObject { ["prng"] = new JObject { ["graine"] = 1800000001u } },
                new JObject { ["prng"] = new JObject { ["graine"] = 1800000000u } });
            Comparateur.ComparerATolerance(new JValue(1800000000u), new JValue(1800000000L));
        }

        [Test, Description("un flottant à moins de 1e-9 relatif passe, au-delà il échoue")]
        public void Un_flottant_a_moins_de_1e_9_relatif_passe_au_dela_il_echoue()
        {
            Comparateur.ComparerATolerance(new JValue(1.0 + 5e-10), new JValue(1.0));
            Echoue(new JValue(1.0 + 5e-9), new JValue(1.0));
        }

        [Test, Description("un flottant face à une chaîne numérique de référence se compare en nombres")]
        public void Un_flottant_face_a_une_chaine_numerique_de_reference_se_compare_en_nombres()
        {
            // `References.Lire` livre tout littéral non entier en chaîne.
            Comparateur.ComparerATolerance(new JValue(0.1 + 0.2), new JValue("0.3"));
            Comparateur.ComparerATolerance(new JValue(28800.0), new JValue(28800L));
            Echoue(new JValue(0.31), new JValue("0.3"));
        }
    }
}
