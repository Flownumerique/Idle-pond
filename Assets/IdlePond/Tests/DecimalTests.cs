using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    public class DecimalTests
    {
        static Decimal[] Valeurs(JObject reference) =>
            reference["valeurs"].Select(v => Decimal.Parse((string)v)).ToArray();

        /// Exact pour tout ce qui ne passe que par des multiplications, des additions
        /// et la table de puissances de 10 ; à 1e-12 relatif pour ce qui appelle
        /// Math.Log10, Math.Pow ou Math.Exp, dont V8 et .NET peuvent différer au dernier bit.
        static readonly string[] OperationsTranscendantes = { "pow", "powStatique", "log10", "exp", "div", "recip" };

        static void Verifier(string contexte, JToken attendu, object obtenu, bool exact)
        {
            switch (obtenu)
            {
                case bool b:
                    Assert.That(b, Is.EqualTo((bool)attendu), contexte);
                    break;
                case int i:
                    Assert.That(i, Is.EqualTo((int)attendu), contexte);
                    break;
                case double x:
                    // « Infinity », « -Infinity », « NaN » arrivent en chaîne (voir le générateur) ;
                    // les entiers restent des nombres JSON. References.Double lit les deux, jamais
                    // via double.Parse (pas correctement arrondi sur ce runtime).
                    ComparerNombres(contexte, References.Double(attendu), x, exact);
                    break;
                case Decimal dec:
                    // Le générateur écrit `r` comme `${x.mantissa}e${x.exponent}`, la mantisse et
                    // l'exposant BRUTS, jamais renormalisés (voir generer-references.ts, fonction
                    // `d`). `Decimal.Parse` renormaliserait une mantisse hors [1, 10) — ce qui
                    // arrive légitimement pour certains résultats de `Mul(double)` — et cacherait
                    // un vrai écart derrière une comparaison qui ne teste plus la même chose.
                    var (mCible, eCible) = LireMantisseExposantBrut((string)attendu);
                    var detail = $"{dec.EnMantisseExposant()} vs {mCible}e{eCible}";
                    // NaN (mantisse et exposant NaN des deux côtés) vaut égal, comme partout ailleurs :
                    // NaN != NaN empêcherait une comparaison directe de le voir.
                    if (double.IsNaN(dec.Mantisse) && double.IsNaN(mCible)) return;
                    if (exact)
                    {
                        // Comparer Mantisse/Exposant directement, jamais ToNumber() : au-delà de
                        // l'intervalle d'un double, ToNumber() écrase deux Decimal différents sur
                        // le même ±Infini/0 et laisserait passer un vrai écart (ex. 1e500, -3.5e-400).
                        Assert.That(dec.Mantisse == mCible && dec.Exposant == eCible, Is.True, $"{contexte} : {detail}");
                        return;
                    }
                    // Ops tolérants qui rendent un Decimal (pow, powStatique, exp, div, recip) :
                    // comparer l'exposant à l'exact près (un écart de 1 n'est permis qu'à la
                    // frontière 1/10 de la mantisse, où le dernier bit peut faire basculer
                    // l'exposant après normalisation) et la mantisse, ramenée au même exposant,
                    // à 1e-12 relatif — toujours sans passer par ToNumber().
                    var ecartExposant = dec.Exposant - eCible;
                    var frontiere = EstProcheDeLaFrontiereMantisse(dec.Mantisse) || EstProcheDeLaFrontiereMantisse(mCible);
                    Assert.That(ecartExposant == 0 || (Math.Abs(ecartExposant) == 1 && frontiere), Is.True,
                        $"{contexte} exposant : {detail}");
                    var mantisseDecAEchelleDeCible = dec.Mantisse * Math.Pow(10, ecartExposant);
                    var echelleMantisse = Math.Max(Math.Max(Math.Abs(mantisseDecAEchelleDeCible), Math.Abs(mCible)), 1e-300);
                    Assert.That(Math.Abs(mantisseDecAEchelleDeCible - mCible) / echelleMantisse, Is.LessThanOrEqualTo(1e-12),
                        $"{contexte} mantisse : {detail}");
                    break;
            }
        }

        /// Lit « m e » sans jamais normaliser, contrairement à `Decimal.Parse` — c'est la valeur
        /// brute que le générateur TypeScript a écrite depuis `x.mantissa`/`x.exponent`. Lu par
        /// `AnalyseDouble`, correctement arrondi (round 1 a montré que ni `double.Parse`, ni
        /// `Convert.ToDouble`, ni `decimal.Parse`+cast ne le sont de façon fiable sur ce runtime).
        static (double m, double e) LireMantisseExposantBrut(string texte)
        {
            var indexE = texte.IndexOf('e');
            var m = AnalyseDouble.Lire(texte.Substring(0, indexE));
            var e = AnalyseDouble.Lire(texte.Substring(indexE + 1));
            return (m, e);
        }

        /// Vrai si la mantisse, en valeur absolue, est tout près de 1 ou de 10 — la seule zone
        /// où un dernier bit différent peut faire basculer l'exposant de ±1 après normalisation.
        static bool EstProcheDeLaFrontiereMantisse(double mantisse)
        {
            var m = Math.Abs(mantisse);
            return Math.Abs(m - 1) < 1e-9 || Math.Abs(m - 10) < 1e-9;
        }

        static void ComparerNombres(string contexte, double attendu, double obtenu, bool exact, string detail = "")
        {
            if (attendu.Equals(obtenu)) return;
            Assert.That(exact, Is.False, $"{contexte} : {obtenu} au lieu de {attendu} {detail}");
            var echelle = Math.Max(Math.Max(Math.Abs(attendu), Math.Abs(obtenu)), 1e-300);
            Assert.That(Math.Abs(attendu - obtenu) / echelle, Is.LessThanOrEqualTo(1e-12), $"{contexte} {detail}");
        }

        [Test, Description("chaque opération rend ce que break_infinity.js rendait")]
        public void Chaque_operation_rend_ce_que_break_infinity_js_rendait()
        {
            var reference = References.Lire("decimal");
            var v = Valeurs(reference);
            foreach (JObject cas in reference["cas"])
            {
                var op = (string)cas["op"];
                var exact = !OperationsTranscendantes.Contains(op);
                var a = cas["a"] != null ? v[(int)cas["a"]] : Decimal.Zero;
                var b = cas["b"] != null ? v[(int)cas["b"]] : Decimal.Zero;
                var x = cas["x"] != null ? References.Double(cas["x"]) : 0.0;
                object r = op switch
                {
                    "neg" => a.Neg(), "abs" => a.Abs(), "recip" => a.Recip(),
                    "floor" => a.Floor(), "round" => a.Round(), "ceil" => a.Ceil(),
                    "toNumber" => a.ToNumber(), "log10" => a.Log10(), "exp" => a.Exp(),
                    "add" => a.Add(b), "sub" => a.Sub(b), "mul" => a.Mul(b), "div" => a.Div(b),
                    "eq" => a.Eq(b), "lt" => a.Lt(b), "gt" => a.Gt(b), "lte" => a.Lte(b), "gte" => a.Gte(b),
                    "max" => a.Max(b), "min" => a.Min(b), "cmp" => a.Cmp(b),
                    "mulNombre" => a.Mul(x), "addNombre" => a.Add(x), "pow" => a.Pow(x),
                    "powStatique" => Decimal.Pow(References.Double(cas["base"]), x),
                    _ => throw new InvalidOperationException(op),
                };
                Verifier($"{op}({cas})", cas["r"], r, exact);
            }
        }

        [Test, Description("JsRound arrondit les demis vers plus l’infini, comme Math.round")]
        public void JsRound_arrondit_les_demis_vers_plus_l_infini()
        {
            Assert.That(Decimal.JsRound(2.5), Is.EqualTo(3.0));
            Assert.That(Decimal.JsRound(-2.5), Is.EqualTo(-2.0));
            Assert.That(Decimal.JsRound(0.49999999999999994), Is.EqualTo(0.0));
            Assert.That(Decimal.JsRound(1.5), Is.EqualTo(2.0));
            Assert.That(double.IsNaN(Decimal.JsRound(double.NaN)), Is.True);
        }

        [Test, Description("relit les chaînes écrites par JavaScript")]
        public void Relit_les_chaines_ecrites_par_JavaScript()
        {
            Assert.That(Decimal.Parse("1.5e25").Mantisse, Is.EqualTo(1.5));
            Assert.That(Decimal.Parse("1.5e25").Exposant, Is.EqualTo(25.0));
            Assert.That(Decimal.Parse("1.5e+25").Exposant, Is.EqualTo(25.0));
            Assert.That(Decimal.Parse("0e0").Eq(Decimal.Zero), Is.True);
            Assert.That(Decimal.Parse("9.999999999999998e2").Mantisse, Is.EqualTo(9.999999999999998));
            // break_infinity.js perd la dernière décimale ici : c'est vérifié contre la
            // bibliothèque non modifiée (node_modules/break_infinity.js), pas une approximation
            // du port. Decimal.fromString("123.45").toNumber() === 123.45 vaut `false` en JS ;
            // le ROUND_TOLERANCE ne recolle que les quasi-entiers, pas ce cas-ci.
            Assert.That(Decimal.Parse("123.45").ToNumber(), Is.EqualTo(123.44999999999999));
            Assert.That(Decimal.Parse("1E+21").Exposant, Is.EqualTo(21.0));
            Assert.That(Decimal.Parse("Infinity").Exposant, Is.EqualTo(9e15));
            Assert.That(double.IsNaN(Decimal.Parse("NaN").Mantisse), Is.True);
            Assert.Throws<FormatException>(() => Decimal.Parse("abc"));
            // Un préfixe de « Infinity » (le reste après le signe de « 1e- »/« 1e+ »/« - ») n'est
            // pas « Infinity » : voir AnalyseDoubleTests pour la même exigence côté lecteur.
            Assert.Throws<FormatException>(() => Decimal.Parse("1e-"));
            Assert.Throws<FormatException>(() => Decimal.Parse("1e+"));
            Assert.Throws<FormatException>(() => Decimal.Parse("-"));
            // Vérifié contre la bibliothèque non modifiée : Decimal.fromNumber(NaN).add(Decimal.fromNumber(1))
            // rend m=0, e=0 en JS (la garde NaN de Add empêche l'indexation de la table qui lèverait sinon).
            Assert.That(new Decimal(double.NaN).Add(Decimal.Un).Eq(Decimal.Zero), Is.True);
        }

        [Test, Description("aller-retour sous la culture fr-FR")]
        public void Aller_retour_sous_la_culture_fr_FR()
        {
            var avant = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                foreach (var texte in new[] { "2.4", "1e12", "123456789.123", "-3.5e-400", "1e500", "0.1" })
                {
                    var x = Decimal.Parse(texte);
                    Assert.That(Decimal.Parse(x.EnMantisseExposant()).Eq(x), Is.True, texte);
                    Assert.That(x.ToString(), Does.Not.Contain(","), texte);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = avant;
            }
        }

        [Test, Description("un Decimal par défaut vaut zéro, comme new Decimal()")]
        public void Un_Decimal_par_defaut_vaut_zero()
        {
            Assert.That(default(Decimal).Eq(Decimal.Zero), Is.True);
            Assert.That(default(Decimal).ToString(), Is.EqualTo("0"));
        }
    }
}
