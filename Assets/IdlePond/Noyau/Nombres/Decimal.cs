using System;
using System.Globalization;

namespace IdlePond.Noyau
{
    /// <summary>
    /// Un grand nombre : mantisse × 10^exposant. Portage fidèle de
    /// break_infinity.js 2.2.0, limité aux opérations que le jeu emploie.
    ///
    /// Fidèle veut dire jusque dans ses bizarreries : `Add` arrondit à 1e14 près
    /// avec le Math.round de JavaScript, `ToNumber` recolle les quasi-entiers,
    /// `Mul(double)` ne suit pas le même chemin que `Mul(Decimal)`. Le TypeScript a
    /// été calibré sur ces nombres-là ; la parité (PariteTests) ne tient que s'ils
    /// sont reproduits, pas améliorés.
    ///
    /// Ce n'est pas System.Decimal. Dans l'espace de noms IdlePond.Noyau, `Decimal`
    /// désigne ce type-ci ; ailleurs, écrire `using Decimal = IdlePond.Noyau.Decimal;`.
    /// </summary>
    public readonly struct Decimal : IEquatable<Decimal>
    {
        const int MAX_SIGNIFICANT_DIGITS = 17;
        const double EXP_LIMIT = 9e15;
        const int NUMBER_EXP_MAX = 308;
        const int NUMBER_EXP_MIN = -324;
        const double ROUND_TOLERANCE = 1e-10;
        const double LN10 = 2.302585092994046;

        /// La table de break_infinity : Math.Pow(10, n) est inexact pour les grands |n|.
        static readonly double[] PuissancesDe10 = ConstruirePuissancesDe10();

        static double[] ConstruirePuissancesDe10()
        {
            var table = new double[NUMBER_EXP_MAX - NUMBER_EXP_MIN];
            for (var i = NUMBER_EXP_MIN + 1; i <= NUMBER_EXP_MAX; i++)
                // `i` est un int : sa concaténation directe passerait par la culture courante
                // (signe négatif, chiffres natifs sur certaines cultures) ; forcer l'invariante.
                table[i - (NUMBER_EXP_MIN + 1)] = double.Parse("1e" + i.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return table;
        }

        static double PuissanceDe10(double puissance) => PuissancesDe10[(int)puissance + 323];

        public static readonly Decimal Zero = default;
        public static readonly Decimal Un = new Decimal(1.0);

        public readonly double Mantisse;
        public readonly double Exposant;

        Decimal(double mantisse, double exposant, bool brut)
        {
            Mantisse = mantisse;
            Exposant = exposant;
        }

        /* ─── Construction ─────────────────────────────────────────────────── */

        /// `new Decimal(number)` — fromNumber.
        public Decimal(double valeur)
        {
            if (double.IsNaN(valeur)) { Mantisse = double.NaN; Exposant = double.NaN; return; }
            if (double.IsPositiveInfinity(valeur)) { Mantisse = 1; Exposant = EXP_LIMIT; return; }
            if (double.IsNegativeInfinity(valeur)) { Mantisse = -1; Exposant = EXP_LIMIT; return; }
            if (valeur == 0) { Mantisse = 0; Exposant = 0; return; }
            var e = Math.Floor(Math.Log10(Math.Abs(valeur)));
            // 5e-324 et -5e-324 à part, comme la source.
            var m = e == NUMBER_EXP_MIN ? valeur * 10 / 1e-323 : valeur / PuissanceDe10(e);
            var n = Normaliser(m, e);
            Mantisse = n.Mantisse;
            Exposant = n.Exposant;
        }

        /// normalize(). Garde en plus contre une mantisse non finie, que la source
        /// transforme en NaN par un index hors table ; ici l'index lèverait.
        static Decimal Normaliser(double m, double e)
        {
            if (m >= 1 && m < 10) return new Decimal(m, e, true);
            if (m == 0) return new Decimal(0, 0, true);
            if (double.IsNaN(m) || double.IsInfinity(m)) return new Decimal(double.NaN, double.NaN, true);
            var temp = Math.Floor(Math.Log10(Math.Abs(m)));
            m = temp == NUMBER_EXP_MIN ? m * 10 / 1e-323 : m / PuissanceDe10(temp);
            return new Decimal(m, e + temp, true);
        }

        /// fromMantissaExponent — ME(). La source, sur une entrée non finie, rend
        /// `this` inchangé : un `new Decimal()` qui vaut zéro. Reproduit tel quel.
        static Decimal ME(double mantisse, double exposant)
        {
            if (double.IsNaN(mantisse) || double.IsInfinity(mantisse) || double.IsNaN(exposant) || double.IsInfinity(exposant))
                return Zero;
            return Normaliser(mantisse, exposant);
        }

        /// fromMantissaExponent_noNormalize — ME_NN().
        static Decimal MENN(double mantisse, double exposant) => new Decimal(mantisse, exposant, true);

        /// fromString. Accepte en plus « E » majuscule, qu'écrit le .NET.
        public static Decimal Parse(string texte)
        {
            if (texte == null) throw new FormatException("Decimal nul");
            var s = texte.Trim();
            var indexE = s.IndexOfAny(new[] { 'e', 'E' });
            if (indexE >= 0 && !s.EndsWith("Infinity", StringComparison.Ordinal))
            {
                var m = double.Parse(s.Substring(0, indexE), NumberStyles.Float, CultureInfo.InvariantCulture);
                var e = double.Parse(s.Substring(indexE + 1), NumberStyles.Float, CultureInfo.InvariantCulture);
                return Normaliser(m, e);
            }
            if (s == "NaN") return new Decimal(double.NaN, double.NaN, true);
            if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur))
                throw new FormatException("[DecimalError] Invalid argument: " + texte);
            return new Decimal(valeur);
        }

        /* ─── Arrondis de JavaScript ───────────────────────────────────────── */

        /// Math.round : les demis vont vers +∞. Math.Round de .NET les enverrait au pair.
        public static double JsRound(double x)
        {
            var plancher = Math.Floor(x);
            return x - plancher >= 0.5 ? plancher + 1 : plancher;
        }

        static bool EstEntierSur(double x) =>
            !double.IsNaN(x) && !double.IsInfinity(x) && Math.Floor(x) == x && Math.Abs(x) <= 9007199254740991;

        static bool EstEntier(double x) => !double.IsNaN(x) && !double.IsInfinity(x) && Math.Floor(x) == x;

        /* ─── Conversions ──────────────────────────────────────────────────── */

        public double ToNumber()
        {
            if (double.IsNaN(Exposant) || double.IsInfinity(Exposant)) return double.NaN;
            if (Exposant > NUMBER_EXP_MAX) return Mantisse > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            if (Exposant < NUMBER_EXP_MIN) return 0;
            if (Exposant == NUMBER_EXP_MIN) return Mantisse > 0 ? 5e-324 : -5e-324;
            var resultat = Mantisse * PuissanceDe10(Exposant);
            if (double.IsInfinity(resultat) || double.IsNaN(resultat) || Exposant < 0) return resultat;
            var arrondi = JsRound(resultat);
            return Math.Abs(arrondi - resultat) < ROUND_TOLERANCE ? arrondi : resultat;
        }

        /// La même structure que le ToString() de break_infinity (mantisse seule, ou
        /// « m e[+|-]exp »), mais mise en texte par le formatage .NET, pas V8 : les deux
        /// n'écrivent pas forcément le même nombre de chiffres. Pour l'affichage et le
        /// débogage seulement ; pour un aller-retour exact, `EnMantisseExposant`.
        public override string ToString()
        {
            if (double.IsNaN(Mantisse) || double.IsNaN(Exposant)) return "NaN";
            if (Exposant >= EXP_LIMIT) return Mantisse > 0 ? "Infinity" : "-Infinity";
            if (Exposant <= -EXP_LIMIT || Mantisse == 0) return "0";
            if (Exposant < 21 && Exposant > -7) return FormaterDouble(ToNumber());
            return FormaterDouble(Mantisse) + "e" + (Exposant >= 0 ? "+" : "") + FormaterDouble(Exposant);
        }

        /// Exacte : « mantisse e exposant », relue telle quelle par `Parse`.
        public string EnMantisseExposant() => FormaterDouble(Mantisse) + "e" + FormaterDouble(Exposant);

        /// Le « R » du Mono d'Unity ne garantit pas l'aller-retour ; G17 si besoin.
        static string FormaterDouble(double x)
        {
            var court = x.ToString("R", CultureInfo.InvariantCulture);
            return double.Parse(court, NumberStyles.Float, CultureInfo.InvariantCulture).Equals(x)
                ? court
                : x.ToString("G17", CultureInfo.InvariantCulture);
        }

        /* ─── Signe ─────────────────────────────────────────────────────────── */

        public Decimal Abs() => MENN(Math.Abs(Mantisse), Exposant);
        public Decimal Neg() => MENN(-Mantisse, Exposant);
        int Signe() => Math.Sign(Mantisse);

        /* ─── Arrondis ──────────────────────────────────────────────────────── */

        public Decimal Round()
        {
            if (Exposant < -1) return Zero;
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(JsRound(ToNumber()));
            return this;
        }

        public Decimal Floor()
        {
            if (Exposant < -1) return Signe() >= 0 ? Zero : new Decimal(-1.0);
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(Math.Floor(ToNumber()));
            return this;
        }

        public Decimal Ceil()
        {
            if (Exposant < -1) return Signe() > 0 ? Un : Zero;
            if (Exposant < MAX_SIGNIFICANT_DIGITS) return new Decimal(Math.Ceiling(ToNumber()));
            return this;
        }

        /* ─── Arithmétique ─────────────────────────────────────────────────── */

        public Decimal Add(Decimal valeur)
        {
            if (Mantisse == 0) return valeur;
            if (valeur.Mantisse == 0) return this;
            Decimal grand, petit;
            if (Exposant >= valeur.Exposant) { grand = this; petit = valeur; }
            else { grand = valeur; petit = this; }
            // Un opérande NaN (mantisse et exposant NaN) rend cette différence NaN ; la source
            // retombe sur zéro par ME (mantisse non finie), ici l'indexation de la table
            // lèverait avant d'y arriver. Vérifié : Decimal(NaN).add(Decimal(1)) vaut 0 en JS.
            if (double.IsNaN(grand.Exposant - petit.Exposant)) return Zero;
            if (grand.Exposant - petit.Exposant > MAX_SIGNIFICANT_DIGITS) return grand;
            // « 299 + 18 » : additionner des mantisses mises à l'échelle perd des entiers.
            var mantisse = JsRound(1e14 * grand.Mantisse + 1e14 * petit.Mantisse * PuissanceDe10(petit.Exposant - grand.Exposant));
            return ME(mantisse, grand.Exposant - 14);
        }

        public Decimal Add(double valeur) => Add(new Decimal(valeur));
        public Decimal Sub(Decimal valeur) => Add(valeur.Neg());
        public Decimal Sub(double valeur) => Add(new Decimal(valeur).Neg());

        /// Le chemin « number » de la source : la mantisse absorbe le facteur.
        public Decimal Mul(double valeur)
        {
            if (valeur < 1e307 && valeur > -1e307) return ME(Mantisse * valeur, Exposant);
            return ME(Mantisse * 1e-307 * valeur, Exposant + 307);
        }

        public Decimal Mul(Decimal valeur) => ME(Mantisse * valeur.Mantisse, Exposant + valeur.Exposant);
        public Decimal Recip() => ME(1 / Mantisse, -Exposant);
        public Decimal Div(Decimal valeur) => Mul(valeur.Recip());
        public Decimal Div(double valeur) => Mul(new Decimal(valeur).Recip());

        /* ─── Comparaisons ─────────────────────────────────────────────────── */

        public int Cmp(Decimal v)
        {
            if (Mantisse == 0)
            {
                if (v.Mantisse == 0) return 0;
                if (v.Mantisse < 0) return 1;
                if (v.Mantisse > 0) return -1;
            }
            if (v.Mantisse == 0)
            {
                if (Mantisse < 0) return -1;
                if (Mantisse > 0) return 1;
            }
            if (Mantisse > 0)
            {
                if (v.Mantisse < 0) return 1;
                if (Exposant > v.Exposant) return 1;
                if (Exposant < v.Exposant) return -1;
                if (Mantisse > v.Mantisse) return 1;
                if (Mantisse < v.Mantisse) return -1;
                return 0;
            }
            if (Mantisse < 0)
            {
                if (v.Mantisse > 0) return -1;
                if (Exposant > v.Exposant) return -1;
                if (Exposant < v.Exposant) return 1;
                if (Mantisse > v.Mantisse) return 1;
                if (Mantisse < v.Mantisse) return -1;
                return 0;
            }
            throw new InvalidOperationException("Unreachable code");
        }

        public bool Eq(Decimal v) => Exposant == v.Exposant && Mantisse == v.Mantisse;
        public bool Eq(double v) => Eq(new Decimal(v));

        public bool Lt(Decimal v)
        {
            if (Mantisse == 0) return v.Mantisse > 0;
            if (v.Mantisse == 0) return Mantisse <= 0;
            if (Exposant == v.Exposant) return Mantisse < v.Mantisse;
            if (Mantisse > 0) return v.Mantisse > 0 && Exposant < v.Exposant;
            return v.Mantisse > 0 || Exposant > v.Exposant;
        }

        public bool Gt(Decimal v)
        {
            if (Mantisse == 0) return v.Mantisse < 0;
            if (v.Mantisse == 0) return Mantisse > 0;
            if (Exposant == v.Exposant) return Mantisse > v.Mantisse;
            if (Mantisse > 0) return v.Mantisse < 0 || Exposant > v.Exposant;
            return v.Mantisse < 0 && Exposant < v.Exposant;
        }

        public bool Lte(Decimal v) => !Gt(v);
        public bool Gte(Decimal v) => !Lt(v);
        public bool Lt(double v) => Lt(new Decimal(v));
        public bool Gt(double v) => Gt(new Decimal(v));
        public bool Lte(double v) => Lte(new Decimal(v));
        public bool Gte(double v) => Gte(new Decimal(v));

        public Decimal Max(Decimal v) => Lt(v) ? v : this;
        public Decimal Min(Decimal v) => Gt(v) ? v : this;
        public static Decimal Max(Decimal a, Decimal b) => a.Max(b);
        public static Decimal Min(Decimal a, Decimal b) => a.Min(b);

        /* ─── Logarithmes, puissances ──────────────────────────────────────── */

        public double Log10() => Exposant + Math.Log10(Mantisse);
        public double AbsLog10() => Exposant + Math.Log10(Math.Abs(Mantisse));
        public double Log(double @base) => LN10 / Math.Log(@base) * Log10();

        public static Decimal Pow10(double valeur)
        {
            if (EstEntier(valeur)) return MENN(1, valeur);
            return ME(Math.Pow(10, valeur % 1), Math.Truncate(valeur));
        }

        public Decimal Pow(Decimal valeur) => Pow(valeur.ToNumber());

        public Decimal Pow(double valeur)
        {
            var temp = Exposant * valeur;
            double nouvelleMantisse;
            if (EstEntierSur(temp))
            {
                nouvelleMantisse = Math.Pow(Mantisse, valeur);
                if (!double.IsInfinity(nouvelleMantisse) && !double.IsNaN(nouvelleMantisse) && nouvelleMantisse != 0)
                    return ME(nouvelleMantisse, temp);
            }
            var nouvelExposant = Math.Truncate(temp);
            var residu = temp - nouvelExposant;
            nouvelleMantisse = Math.Pow(10, valeur * Math.Log10(Mantisse) + residu);
            if (!double.IsInfinity(nouvelleMantisse) && !double.IsNaN(nouvelleMantisse) && nouvelleMantisse != 0)
                return ME(nouvelleMantisse, nouvelExposant);
            var resultat = Pow10(valeur * AbsLog10());
            if (Signe() == -1)
            {
                if (Math.Abs(valeur % 2) == 1) return resultat.Neg();
                if (Math.Abs(valeur % 2) == 0) return resultat;
                return new Decimal(double.NaN);
            }
            return resultat;
        }

        /// Decimal.pow(value, other) : 10^entier par la voie rapide, sinon D(value).pow(other).
        public static Decimal Pow(double @base, double x)
        {
            if (@base == 10 && EstEntier(x)) return MENN(1, x);
            return new Decimal(@base).Pow(x);
        }

        public static Decimal Pow(Decimal @base, double x) => @base.Pow(x);

        public Decimal Exp()
        {
            var x = ToNumber();
            if (-706 < x && x < 709) return new Decimal(Math.Exp(x));
            return Pow(Math.E, x);
        }

        /* ─── Égalité ──────────────────────────────────────────────────────── */

        public bool Equals(Decimal autre) => Eq(autre);
        public override bool Equals(object obj) => obj is Decimal autre && Eq(autre);
        public override int GetHashCode() => Mantisse.GetHashCode() ^ (Exposant.GetHashCode() * 397);
    }
}
