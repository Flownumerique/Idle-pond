/*
 * L'exécuteur des tests EditMode hors Unity.
 *
 * Il cherche, par réflexion, les classes marquées [TestFixture] et leurs méthodes
 * [Test], les exécute une à une, et rend un code de sortie non nul au premier
 * échec — c'est ce que la CI lit.
 *
 *   dotnet run -c Release                  tous les tests
 *   dotnet run -c Release -- Parite        ceux dont le nom complet contient « Parite »
 */
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace IdlePond.Verification
{
    public static class Programme
    {
        public static int Main(string[] arguments)
        {
            var filtre = arguments.Length > 0 ? arguments[0] : null;
            var fixtures = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToArray();

            int reussis = 0, echoues = 0;
            var chrono = Stopwatch.StartNew();
            foreach (var fixture in fixtures)
            {
                var tests = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                    .OrderBy(m => m.MetadataToken)
                    .ToArray();
                foreach (var test in tests)
                {
                    var nom = fixture.Name + "." + test.Name;
                    if (filtre != null && nom.IndexOf(filtre, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var debut = chrono.Elapsed;
                    try
                    {
                        var instance = Activator.CreateInstance(fixture);
                        test.Invoke(instance, null);
                        reussis += 1;
                        Console.WriteLine("  ✓ " + nom + "  (" + (chrono.Elapsed - debut).TotalMilliseconds.ToString("0") + " ms)");
                    }
                    catch (TargetInvocationException enveloppe)
                    {
                        echoues += 1;
                        var cause = enveloppe.InnerException ?? enveloppe;
                        Console.WriteLine("  ✗ " + nom);
                        Console.WriteLine("      " + cause.GetType().Name + " : " + cause.Message.Replace("\n", "\n      "));
                        if (!(cause is AssertionException)) Console.WriteLine(cause.StackTrace);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine(reussis + " réussis, " + echoues + " échoués, en " + chrono.Elapsed.TotalSeconds.ToString("0.0") + " s.");
            return echoues == 0 && reussis > 0 ? 0 : 1;
        }
    }
}
