/*
 * Test d'architecture — à écrire en premier (§5.3). Port de
 * `tests/architecture.test.ts`.
 *
 * « noyau/ ne doit importer AUCUN module hors de noyau/ et donnees/. C'est la
 * garantie la moins chère du projet et celle qui sauve le simulateur. »
 *
 * En C#, la frontière a deux gardiens, et ce test vérifie les deux :
 *   - les définitions d'assemblage (.asmdef) : le Cœur ne référence RIEN, et
 *     aucun assemblage pur ne voit UnityEngine (`noEngineReferences`) — Unity
 *     refusera de compiler une faute ;
 *   - le source lui-même, pour ce que le compilateur laisse passer : l'horloge,
 *     le hasard, les fichiers, les fils d'exécution.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using IdlePond.Adaptateurs;
using IdlePond.Noyau;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class ArchitectureTests
    {
        private static string Dossier(string nom) => Path.Combine(OutilsDeTest.DossierDuJeu(), nom);

        /// <summary>Ce que le §5.1 interdit nommément, plus ce qui casserait la pureté par la bande.</summary>
        private static readonly (Regex Motif, string Quoi)[] MotifsImpurs =
        {
            (new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b"), "l'heure système"),
            (new Regex(@"\bStopwatch\b"), "Stopwatch"),
            (new Regex(@"\bEnvironment\s*\.\s*TickCount"), "Environment.TickCount"),
            (new Regex(@"\bnew\s+(System\s*\.\s*)?Random\b|\bRandom\s*\.\s*(Shared|Range|value)\b"), "le hasard système"),
            (new Regex(@"\bUnityEngine\b"), "UnityEngine"),
            (new Regex(@"\bUnityEditor\b"), "UnityEditor"),
            (new Regex(@"\bSystem\s*\.\s*IO\b|\bFile\s*\.\s*\w+\("), "le système de fichiers"),
            (new Regex(@"\bSystem\s*\.\s*Threading\b|\bTask\s*\.\s*(Run|Delay)\b"), "les fils d'exécution"),
            (new Regex(@"\bPlayerPrefs\b"), "PlayerPrefs"),
        };

        private static JsonObjet LireAsmdef(string dossier)
        {
            var fichier = Directory.GetFiles(dossier, "*.asmdef").Single();
            return (JsonObjet)Json.Lire(File.ReadAllText(fichier));
        }

        private static IReadOnlyList<string> References(JsonObjet asmdef) =>
            (asmdef.Lire("references") as JsonTableau)?.Elements.OfType<JsonTexte>().Select(t => t.Valeur).ToList()
            ?? new List<string>();

        [Test]
        public void LeCoeurContientDesFichiersAVerifier()
        {
            Assert.Greater(OutilsDeTest.FichiersCs(Dossier("Coeur")).Length, 5);
        }

        [Test]
        public void LesAssemblagesPursNeVoientPasUnityEtNeReferencentQueCeQuiLeurEstPermis()
        {
            var permis = new Dictionary<string, string[]>
            {
                ["Coeur"] = new string[0],
                ["Adaptateurs"] = new[] { "IdlePond.Coeur" },
                ["Etat"] = new[] { "IdlePond.Coeur", "IdlePond.Adaptateurs" },
                ["Presentation"] = new[] { "IdlePond.Coeur" },
                ["Simulateur"] = new[] { "IdlePond.Coeur", "IdlePond.Adaptateurs" },
            };
            foreach (var paire in permis)
            {
                var asmdef = LireAsmdef(Dossier(paire.Key));
                Assert.IsTrue(asmdef.Lire("noEngineReferences") is JsonBooleen b && b.Valeur, paire.Key + " doit être compilé sans UnityEngine");
                foreach (var reference in References(asmdef))
                {
                    CollectionAssert.Contains(paire.Value, reference, paire.Key + " référence « " + reference + " », qui ne lui est pas permis");
                }
            }
        }

        [Test]
        public void LesAssemblagesPursNeTouchentNiHorlogeNiHasardNiFichiersNiUnity()
        {
            var fautes = new List<string>();
            foreach (var dossier in new[] { "Coeur", "Etat", "Presentation", "Simulateur" })
            {
                foreach (var fichier in OutilsDeTest.FichiersCs(Dossier(dossier)))
                {
                    var code = OutilsDeTest.SansCommentaires(File.ReadAllText(fichier));
                    foreach (var (motif, quoi) in MotifsImpurs)
                    {
                        if (motif.IsMatch(code)) fautes.Add(OutilsDeTest.Relatif(fichier) + " utilise " + quoi);
                    }
                }
            }
            CollectionAssert.IsEmpty(fautes);
        }

        [Test]
        public void LHorlogeSystemeNEstLueQueDansAdaptateursHorloge()
        {
            var horloge = Path.Combine("Adaptateurs", "Horloge.cs");
            var heure = new Regex(@"\bDateTime(Offset)?\s*\.\s*(Now|UtcNow|Today)\b");
            var fautifs = OutilsDeTest.FichiersCs(OutilsDeTest.DossierDuJeu())
                .Where(f => !f.EndsWith(horloge, StringComparison.Ordinal))
                .Where(f => heure.IsMatch(OutilsDeTest.SansCommentaires(File.ReadAllText(f))))
                .Select(OutilsDeTest.Relatif)
                .ToList();
            CollectionAssert.IsEmpty(fautifs);
        }

        [Test]
        public void LeCoeurNePorteAucunEtatStatiqueMutable()
        {
            // « Aucun état hors du reducer : pas de variable de module, pas de cache. »
            // Un champ statique non readonly serait exactement cela. Les champs que le
            // compilateur engendre (caches de lambdas) ne sont pas du code écrit.
            var fautes = typeof(Reducteur).Assembly.GetTypes()
                .Where(t => t.Namespace != null && (t.Namespace.StartsWith("IdlePond.Noyau", StringComparison.Ordinal)
                                                    || t.Namespace.StartsWith("IdlePond.Donnees", StringComparison.Ordinal)
                                                    || t.Namespace.StartsWith("IdlePond.Nombres", StringComparison.Ordinal)))
                .Where(t => t.GetCustomAttribute<CompilerGeneratedAttribute>() == null && !t.Name.Contains("<"))
                .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(f => !f.IsInitOnly && !f.IsLiteral && !f.Name.Contains("<"))
                    .Select(f => t.FullName + "." + f.Name))
                .ToList();
            CollectionAssert.IsEmpty(fautes);
        }
    }
}
