/*
 * IdlePond — les grands nombres.
 *
 * Port FIDÈLE de `break_infinity.js` 2.x, réduit aux opérations que le jeu
 * utilise, et fidèle jusque dans ses bizarreries. Ce n'est pas du zèle : la
 * version web reste la référence tant que la parité n'est pas établie, et le
 * test de parité compare les deux implémentations à 1e-9 près. Une addition qui
 * arrondirait autrement, un `toNumber` qui ne recollerait pas les quasi-entiers,
 * et la comparaison mesurerait la bibliothèque au lieu du jeu.
 *
 * Représentation : mantisse dans [1, 10[ (ou ]-10, -1]), exposant entier porté
 * par un double. Zéro vaut (0, 0) — c'est aussi la valeur par défaut du type.
 *
 * Une bizarrerie à connaître, parce qu'elle est reproduite : une mantisse non
 * finie rend ZÉRO (`fromMantissaExponent` de la bibliothèque renvoie l'objet
 * inchangé, qui vaut zéro). Diviser par zéro rend donc zéro, pas l'infini.
 */
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace IdlePond.Nombres
{
    public readonly struct GrandNombre : IEquatable<GrandNombre>
    {
        /// <summary>Au-delà, l'exposant dit « infini » (EXP_LIMIT de la bibliothèque).</summary>
        public const double LimiteDExposant = 9e15;

        public static readonly GrandNombre Zero = new GrandNombre(0, 0);
        public static readonly GrandNombre Un = new GrandNombre(1, 0);
        public static readonly GrandNombre NaN = new GrandNombre(double.NaN, double.NaN);

        public readonly double Mantisse;
        public readonly double Exposant;

        private GrandNombre(double mantisse, double exposant)
        {
            Mantisse = mantisse;
            Exposant = exposant;
        }

        /* ─── Puissances de dix exactes ──────────────────────────────────────
         * La bibliothèque les lit dans une table construite par `Number("1e" + n)`,
         * pas par `Math.pow` : l'analyse d'une chaîne est correctement arrondie,
         * la puissance ne l'est pas toujours.
         */
        private static readonly double[] PuissancesDeDix = ConstruirePuissancesDeDix();

        private static double[] ConstruirePuissancesDeDix()
        {
            var table = new double[308 + 323 + 1];
            for (var n = -323; n <= 308; n += 1)
            {
                table[n + 323] = double.Parse("1e" + n.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            }
            return table;
        }

        private static double PuissanceDeDix(double n)
        {
            var index = (int)n + 323;
            if (index < 0 || index >= PuissancesDeDix.Length) return double.NaN;
            return PuissancesDeDix[index];
        }

        /// <summary>`Math.round` de JavaScript : au plus proche, égalité vers +∞.</summary>
        private static double ArrondiJs(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return x;
            var bas = Math.Floor(x);
            return x - bas >= 0.5 ? bas + 1 : bas;
        }

        /* ─── Construction ───────────────────────────────────────────────────*/

        public static GrandNombre SansNormaliser(double mantisse, double exposant)
        {
            return new GrandNombre(mantisse, exposant);
        }

        public static GrandNombre DepuisMantisseEtExposant(double mantisse, double exposant)
        {
            if (double.IsNaN(mantisse) || double.IsInfinity(mantisse) || double.IsNaN(exposant) || double.IsInfinity(exposant))
            {
                return Zero;
            }
            return Normaliser(mantisse, exposant);
        }

        private static GrandNombre Normaliser(double mantisse, double exposant)
        {
            if (mantisse >= 1 && mantisse < 10) return new GrandNombre(mantisse, exposant);
            if (mantisse == 0) return Zero;
            if (double.IsNaN(mantisse)) return NaN;
            var t = Math.Floor(Math.Log10(Math.Abs(mantisse)));
            var m = t == -324 ? 10 * mantisse / 1e-323 : mantisse / PuissanceDeDix(t);
            return new GrandNombre(m, exposant + t);
        }

        public static GrandNombre DepuisNombre(double valeur)
        {
            if (double.IsNaN(valeur)) return NaN;
            if (double.IsPositiveInfinity(valeur)) return new GrandNombre(1, LimiteDExposant);
            if (double.IsNegativeInfinity(valeur)) return new GrandNombre(-1, LimiteDExposant);
            if (valeur == 0) return Zero;
            var e = Math.Floor(Math.Log10(Math.Abs(valeur)));
            var m = e == -324 ? 10 * valeur / 1e-323 : valeur / PuissanceDeDix(e);
            return Normaliser(m, e);
        }

        public static implicit operator GrandNombre(double valeur) => DepuisNombre(valeur);

        private static readonly Regex MotifDeParseFloat = new Regex(
            @"^[+-]?(Infinity|(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?)",
            RegexOptions.CultureInvariant);

        /// <summary>`parseFloat` : lit le plus long préfixe numérique, NaN sinon.</summary>
        private static double ParseFloatJs(string texte)
        {
            var trouve = MotifDeParseFloat.Match(texte.TrimStart());
            if (!trouve.Success) return double.NaN;
            var lu = trouve.Value;
            if (lu.EndsWith("Infinity", StringComparison.Ordinal))
            {
                return lu.StartsWith("-", StringComparison.Ordinal) ? double.NegativeInfinity : double.PositiveInfinity;
            }
            return double.Parse(lu, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>`new Decimal(chaine)`. Lève une FormatException comme la bibliothèque lève une Error.</summary>
        public static GrandNombre Lire(string texte)
        {
            if (texte == null) throw new FormatException("[DecimalError] Invalid argument: null");
            if (texte.IndexOf('e') != -1)
            {
                var morceaux = texte.Split('e');
                var m = ParseFloatJs(morceaux[0]);
                var e = ParseFloatJs(morceaux[1]);
                if (double.IsNaN(m) || double.IsNaN(e)) return NaN;
                return Normaliser(m, e);
            }
            if (texte == "NaN") return NaN;
            var nombre = DepuisNombre(ParseFloatJs(texte));
            if (double.IsNaN(nombre.Mantisse)) throw new FormatException("[DecimalError] Invalid argument: " + texte);
            return nombre;
        }

        public static bool EssayerDeLire(string texte, out GrandNombre valeur)
        {
            try
            {
                valeur = Lire(texte);
                return !double.IsNaN(valeur.Mantisse);
            }
            catch (FormatException)
            {
                valeur = Zero;
                return false;
            }
        }

        /* ─── Lecture ────────────────────────────────────────────────────────*/

        public bool EstNaN => double.IsNaN(Mantisse) || double.IsNaN(Exposant);

        public double ToNumber()
        {
            if (double.IsNaN(Exposant) || double.IsInfinity(Exposant)) return double.NaN;
            if (Exposant > 308) return Mantisse > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            if (Exposant < -324) return 0;
            if (Exposant == -324) return Mantisse > 0 ? 5e-324 : -5e-324;
            var t = Mantisse * PuissanceDeDix(Exposant);
            if (double.IsInfinity(t) || double.IsNaN(t) || Exposant < 0) return t;
            var n = ArrondiJs(t);
            return Math.Abs(n - t) < 1e-10 ? n : t;
        }

        /// <summary>`toString()` de la bibliothèque : c'est le format de la save.</summary>
        public override string ToString()
        {
            if (EstNaN) return "NaN";
            if (Exposant >= LimiteDExposant) return Mantisse > 0 ? "Infinity" : "-Infinity";
            if (Exposant <= -LimiteDExposant || Mantisse == 0) return "0";
            if (Exposant < 21 && Exposant > -7) return NombreJs.VersTexte(ToNumber());
            return NombreJs.VersTexte(Mantisse) + "e" + (Exposant >= 0 ? "+" : "") + NombreJs.VersTexte(Exposant);
        }

        /// <summary>`toExponential(chiffres)`, pour l'affichage des très grands montants.</summary>
        public string ToExponential(int chiffres)
        {
            if (EstNaN) return "NaN";
            if (Exposant >= LimiteDExposant) return Mantisse > 0 ? "Infinity" : "-Infinity";
            var decimales = chiffres > 0 ? "." + new string('0', chiffres) : "";
            if (Exposant <= -LimiteDExposant || Mantisse == 0) return "0" + decimales + "e+0";
            if (Exposant > -324 && Exposant < 308) return NombreJs.ToExponential(ToNumber(), chiffres);
            var r = chiffres + 1;
            var i = Math.Max(1, Math.Ceiling(Math.Log10(Math.Abs(Mantisse))));
            var arrondie = ArrondiJs(Mantisse * Math.Pow(10, r - i)) * Math.Pow(10, i - r);
            return NombreJs.ToFixed(arrondie, (int)Math.Max(r - i, 0))
                   + "e" + (Exposant >= 0 ? "+" : "") + NombreJs.VersTexte(Exposant);
        }

        public double Log10() => Exposant + Math.Log10(Mantisse);

        public double AbsLog10() => Exposant + Math.Log10(Math.Abs(Mantisse));

        public int Signe => Math.Sign(Mantisse);

        /* ─── Arithmétique ───────────────────────────────────────────────────*/

        public GrandNombre Abs() => new GrandNombre(Math.Abs(Mantisse), Exposant);

        public GrandNombre Neg() => new GrandNombre(-Mantisse, Exposant);

        public GrandNombre Floor()
        {
            if (Exposant < -1) return Math.Sign(Mantisse) >= 0 ? Zero : DepuisNombre(-1);
            if (Exposant < 17) return DepuisNombre(Math.Floor(ToNumber()));
            return this;
        }

        public GrandNombre Add(GrandNombre autre)
        {
            if (Mantisse == 0) return autre;
            if (autre.Mantisse == 0) return this;
            GrandNombre grand, petit;
            if (Exposant >= autre.Exposant)
            {
                grand = this;
                petit = autre;
            }
            else
            {
                grand = autre;
                petit = this;
            }
            if (grand.Exposant - petit.Exposant > 17) return grand;
            var somme = ArrondiJs(1e14 * grand.Mantisse + 1e14 * petit.Mantisse * PuissanceDeDix(petit.Exposant - grand.Exposant));
            return DepuisMantisseEtExposant(somme, grand.Exposant - 14);
        }

        public GrandNombre Sub(GrandNombre autre) => Add(autre.Neg());

        /// <summary>`mul(nombre)` : la bibliothèque ne convertit PAS le nombre en Decimal.</summary>
        public GrandNombre Mul(double facteur)
        {
            if (facteur < 1e307 && facteur > -1e307) return DepuisMantisseEtExposant(Mantisse * facteur, Exposant);
            return DepuisMantisseEtExposant(1e-307 * Mantisse * facteur, Exposant + 307);
        }

        public GrandNombre Mul(GrandNombre autre) => DepuisMantisseEtExposant(Mantisse * autre.Mantisse, Exposant + autre.Exposant);

        public GrandNombre Recip() => DepuisMantisseEtExposant(1 / Mantisse, -Exposant);

        /// <summary>`div(x)` = `mul(Decimal(x).recip())`, même quand x est un nombre.</summary>
        public GrandNombre Div(GrandNombre diviseur) => Mul(diviseur.Recip());

        public GrandNombre Div(double diviseur) => Mul(DepuisNombre(diviseur).Recip());

        public GrandNombre Pow(double puissance)
        {
            var r = Exposant * puissance;
            double n;
            if (EstEntierSur(r))
            {
                n = Math.Pow(Mantisse, puissance);
                if (!double.IsInfinity(n) && !double.IsNaN(n) && n != 0) return DepuisMantisseEtExposant(n, r);
            }

            var o = Math.Truncate(r);
            var u = r - o;
            n = Math.Pow(10, puissance * Math.Log10(Mantisse) + u);
            if (!double.IsInfinity(n) && !double.IsNaN(n) && n != 0) return DepuisMantisseEtExposant(n, o);

            var s = PuissanceDeDixGrande(puissance * AbsLog10());
            if (Signe == -1)
            {
                var reste = Math.Abs(puissance % 2);
                if (reste == 1) return s.Neg();
                if (reste == 0) return s;
                return NaN;
            }
            return s;
        }

        public static GrandNombre Pow(GrandNombre baseDeLaPuissance, double puissance) => baseDeLaPuissance.Pow(puissance);

        public static GrandNombre Pow(double baseDeLaPuissance, double puissance)
        {
            if (baseDeLaPuissance == 10 && puissance == Math.Floor(puissance) && !double.IsInfinity(puissance))
            {
                return new GrandNombre(1, puissance);
            }
            return DepuisNombre(baseDeLaPuissance).Pow(puissance);
        }

        /// <summary>`Decimal.pow10(t)`.</summary>
        private static GrandNombre PuissanceDeDixGrande(double t)
        {
            if (t == Math.Floor(t) && !double.IsInfinity(t)) return new GrandNombre(1, t);
            return DepuisMantisseEtExposant(Math.Pow(10, t % 1), Math.Truncate(t));
        }

        private static bool EstEntierSur(double r)
        {
            return !double.IsNaN(r) && !double.IsInfinity(r) && Math.Floor(r) == r && Math.Abs(r) <= 9007199254740991d;
        }

        /* ─── Comparaisons ───────────────────────────────────────────────────*/

        public bool Lt(GrandNombre n)
        {
            if (Mantisse == 0) return n.Mantisse > 0;
            if (n.Mantisse == 0) return Mantisse <= 0;
            if (Exposant == n.Exposant) return Mantisse < n.Mantisse;
            if (Mantisse > 0) return n.Mantisse > 0 && Exposant < n.Exposant;
            return n.Mantisse > 0 || Exposant > n.Exposant;
        }

        public bool Gt(GrandNombre n)
        {
            if (Mantisse == 0) return n.Mantisse < 0;
            if (n.Mantisse == 0) return Mantisse > 0;
            if (Exposant == n.Exposant) return Mantisse > n.Mantisse;
            if (Mantisse > 0) return n.Mantisse < 0 || Exposant > n.Exposant;
            return n.Mantisse < 0 && Exposant < n.Exposant;
        }

        public bool Lte(GrandNombre n) => !Gt(n);

        public bool Gte(GrandNombre n) => !Lt(n);

        /// <summary>`eq` : égalité des deux champs, donc NaN n'est égal à rien.</summary>
        public bool Eq(GrandNombre n) => Exposant == n.Exposant && Mantisse == n.Mantisse;

        public GrandNombre Max(GrandNombre n) => Lt(n) ? n : this;

        public GrandNombre Min(GrandNombre n) => Gt(n) ? n : this;

        public static GrandNombre Max(GrandNombre a, GrandNombre b) => a.Max(b);

        public static GrandNombre Min(GrandNombre a, GrandNombre b) => a.Min(b);

        /* ─── Égalité structurelle, pour les records qui en contiennent ───────*/

        public bool Equals(GrandNombre autre) => Mantisse.Equals(autre.Mantisse) && Exposant.Equals(autre.Exposant);

        public override bool Equals(object obj) => obj is GrandNombre autre && Equals(autre);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Mantisse.GetHashCode() * 397) ^ Exposant.GetHashCode();
            }
        }
    }
}
