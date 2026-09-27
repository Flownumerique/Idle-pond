/*
 * Un sous-ensemble de NUnit 3, juste ce que les tests EditMode d'IdlePond
 * utilisent : les attributs de fixture et le modèle d'assertion « classique ».
 *
 * Il n'existe que parce que la vérification hors Unity doit se construire sans
 * NuGet. Dans Unity, les mêmes tests compilent contre le vrai NUnit du Test
 * Framework ; ici, contre ceci. Une assertion absente d'ici ne compilera pas en
 * vérification — c'est voulu : les tests s'en tiennent à ce qui existe des deux
 * côtés.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestFixtureAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute
    {
        public string Description { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string nom) => Nom = nom;

        public string Nom { get; }
    }

    public delegate void TestDelegate();

    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    public static class Assert
    {
        private static string Avec(string message, string detail) =>
            string.IsNullOrEmpty(message) ? detail : message + Environment.NewLine + "  " + detail;

        private static string Decrire(object valeur)
        {
            switch (valeur)
            {
                case null: return "null";
                case string s: return "\"" + s + "\"";
                case double d: return d.ToString("R", CultureInfo.InvariantCulture);
                case float f: return f.ToString("R", CultureInfo.InvariantCulture);
                default: return Convert.ToString(valeur, CultureInfo.InvariantCulture);
            }
        }

        public static void Fail(string message = null) => throw new AssertionException(message ?? "Échec.");

        public static void IsTrue(bool condition, string message = null)
        {
            if (!condition) throw new AssertionException(Avec(message, "Attendu : vrai. Obtenu : faux."));
        }

        public static void IsFalse(bool condition, string message = null)
        {
            if (condition) throw new AssertionException(Avec(message, "Attendu : faux. Obtenu : vrai."));
        }

        public static void That(bool condition, string message = null) => IsTrue(condition, message);

        public static void IsNull(object valeur, string message = null)
        {
            if (valeur != null) throw new AssertionException(Avec(message, "Attendu : null. Obtenu : " + Decrire(valeur)));
        }

        public static void IsNotNull(object valeur, string message = null)
        {
            if (valeur == null) throw new AssertionException(Avec(message, "Attendu : une valeur. Obtenu : null."));
        }

        public static void AreSame(object attendu, object obtenu, string message = null)
        {
            if (!ReferenceEquals(attendu, obtenu)) throw new AssertionException(Avec(message, "Attendu : la même instance."));
        }

        public static void AreNotSame(object attendu, object obtenu, string message = null)
        {
            if (ReferenceEquals(attendu, obtenu)) throw new AssertionException(Avec(message, "Attendu : deux instances distinctes."));
        }

        private static bool EstNumerique(object o) =>
            o is sbyte || o is byte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong
            || o is float || o is double || o is decimal;

        internal static bool SontEgaux(object attendu, object obtenu)
        {
            if (attendu == null || obtenu == null) return attendu == null && obtenu == null;
            if (EstNumerique(attendu) && EstNumerique(obtenu))
            {
                var a = Convert.ToDouble(attendu, CultureInfo.InvariantCulture);
                var b = Convert.ToDouble(obtenu, CultureInfo.InvariantCulture);
                return a.Equals(b);
            }
            if (!(attendu is string) && attendu is IEnumerable ea && obtenu is IEnumerable eb)
            {
                var la = ea.Cast<object>().ToList();
                var lb = eb.Cast<object>().ToList();
                if (la.Count != lb.Count) return false;
                for (var i = 0; i < la.Count; i += 1) if (!SontEgaux(la[i], lb[i])) return false;
                return true;
            }
            return attendu.Equals(obtenu);
        }

        public static void AreEqual(object attendu, object obtenu, string message = null)
        {
            if (!SontEgaux(attendu, obtenu))
            {
                throw new AssertionException(Avec(message, "Attendu : " + Decrire(attendu) + ". Obtenu : " + Decrire(obtenu) + "."));
            }
        }

        public static void AreNotEqual(object attendu, object obtenu, string message = null)
        {
            if (SontEgaux(attendu, obtenu)) throw new AssertionException(Avec(message, "Attendu : autre chose que " + Decrire(attendu) + "."));
        }

        public static void AreEqual(double attendu, double obtenu, double delta, string message = null)
        {
            if (double.IsNaN(attendu) && double.IsNaN(obtenu)) return;
            if (attendu.Equals(obtenu)) return;
            if (!(Math.Abs(attendu - obtenu) <= delta))
            {
                throw new AssertionException(Avec(message,
                    "Attendu : " + Decrire(attendu) + " ± " + Decrire(delta) + ". Obtenu : " + Decrire(obtenu) + "."));
            }
        }

        public static void AreEqual(double attendu, double? obtenu, double delta, string message = null)
        {
            if (obtenu == null) throw new AssertionException(Avec(message, "Attendu : " + Decrire(attendu) + ". Obtenu : null."));
            AreEqual(attendu, obtenu.Value, delta, message);
        }

        private static void Comparer(double a, double b, Func<double, double, bool> critere, string symbole, string message)
        {
            if (!critere(a, b)) throw new AssertionException(Avec(message, "Attendu : " + Decrire(a) + " " + symbole + " " + Decrire(b) + "."));
        }

        public static void Greater(double a, double b, string message = null) => Comparer(a, b, (x, y) => x > y, ">", message);

        public static void GreaterOrEqual(double a, double b, string message = null) => Comparer(a, b, (x, y) => x >= y, "≥", message);

        public static void Less(double a, double b, string message = null) => Comparer(a, b, (x, y) => x < y, "<", message);

        public static void LessOrEqual(double a, double b, string message = null) => Comparer(a, b, (x, y) => x <= y, "≤", message);

        public static T Throws<T>(TestDelegate code, string message = null) where T : Exception
        {
            try
            {
                code();
            }
            catch (T attendue)
            {
                return attendue;
            }
            catch (Exception autre)
            {
                throw new AssertionException(Avec(message, "Attendu : " + typeof(T).Name + ". Obtenu : " + autre.GetType().Name + " — " + autre.Message));
            }
            throw new AssertionException(Avec(message, "Attendu : " + typeof(T).Name + ". Rien n'a été levé."));
        }

        public static void DoesNotThrow(TestDelegate code, string message = null)
        {
            try
            {
                code();
            }
            catch (Exception e)
            {
                throw new AssertionException(Avec(message, "Rien ne devait être levé. Obtenu : " + e.GetType().Name + " — " + e.Message));
            }
        }
    }

    public static class CollectionAssert
    {
        public static void AreEqual(IEnumerable attendu, IEnumerable obtenu, string message = null)
        {
            var a = attendu.Cast<object>().ToList();
            var b = obtenu.Cast<object>().ToList();
            if (a.Count != b.Count)
            {
                throw new AssertionException((message ?? "") + " Longueurs différentes : " + a.Count + " attendus, " + b.Count + " obtenus. Obtenu : ["
                                             + string.Join(", ", b) + "]");
            }
            for (var i = 0; i < a.Count; i += 1)
            {
                if (!Assert.SontEgaux(a[i], b[i]))
                {
                    throw new AssertionException((message ?? "") + " Différence à l'index " + i + " : « " + a[i] + " » attendu, « " + b[i] + " » obtenu.");
                }
            }
        }

        public static void Contains(IEnumerable collection, object element, string message = null)
        {
            if (!collection.Cast<object>().Any(e => Assert.SontEgaux(element, e)))
            {
                throw new AssertionException((message ?? "") + " « " + element + " » absent de la collection.");
            }
        }

        public static void DoesNotContain(IEnumerable collection, object element, string message = null)
        {
            if (collection.Cast<object>().Any(e => Assert.SontEgaux(element, e)))
            {
                throw new AssertionException((message ?? "") + " « " + element + " » présent dans la collection.");
            }
        }

        public static void IsEmpty(IEnumerable collection, string message = null)
        {
            var elements = collection.Cast<object>().ToList();
            if (elements.Count > 0) throw new AssertionException((message ?? "") + " Collection non vide : [" + string.Join(", ", elements) + "]");
        }

        public static void IsNotEmpty(IEnumerable collection, string message = null)
        {
            if (!collection.Cast<object>().Any()) throw new AssertionException((message ?? "") + " Collection vide.");
        }
    }

    public static class StringAssert
    {
        public static void Contains(string attendu, string obtenu, string message = null)
        {
            if (obtenu == null || !obtenu.Contains(attendu))
            {
                throw new AssertionException((message ?? "") + " « " + attendu + " » absent de « " + obtenu + " ».");
            }
        }
    }
}
