using System.IO;
using System.Text;
using IdlePond.Noyau;
using Newtonsoft.Json.Linq;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Les fichiers produits par `tests/parite/generer-references.ts` pendant que
    /// le TypeScript était la vérité. Le répertoire courant d'un test EditMode est
    /// la racine du projet Unity.
    ///
    /// `Lire` fait ressortir tout littéral non entier comme une chaîne JSON (voir
    /// `ProtegerLesFlottants`) : ne jamais les lire par `(double)token`, toujours par
    /// `References.Double(token)`.
    /// </summary>
    public static class References
    {
        public static JObject Lire(string nom)
        {
            var chemin = Path.GetFullPath(Path.Combine("Assets", "IdlePond", "Tests", "Reference", nom + ".json"));
            var texte = File.ReadAllText(chemin, Encoding.UTF8);
            return (JObject)JToken.Parse(ProtegerLesFlottants(texte));
        }

        /// Met entre guillemets chaque littéral JSON numérique non entier (avec un `.`, un `e` ou
        /// un `E`) situé hors chaîne, pour que Newtonsoft ne le fasse jamais passer par son propre
        /// analyseur de double — lui aussi adossé, en bout de chaîne, au `double.Parse` du runtime,
        /// pas correctement arrondi ici (voir task-3-report.md, fix round 1 et 2). Les entiers
        /// restent des nombres JSON tels quels : ils sont exacts, pas de raison de les protéger.
        static string ProtegerLesFlottants(string json)
        {
            var resultat = new StringBuilder(json.Length + 64);
            var dansChaine = false;
            var echappe = false;
            var i = 0;
            while (i < json.Length)
            {
                var c = json[i];
                if (dansChaine)
                {
                    resultat.Append(c);
                    if (echappe) echappe = false;
                    else if (c == '\\') echappe = true;
                    else if (c == '"') dansChaine = false;
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    dansChaine = true;
                    resultat.Append(c);
                    i++;
                    continue;
                }
                if (c == '-' || (c >= '0' && c <= '9'))
                {
                    var debut = i;
                    if (json[i] == '-') i++;
                    while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
                    var nonEntier = false;
                    if (i < json.Length && json[i] == '.')
                    {
                        nonEntier = true;
                        i++;
                        while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
                    }
                    if (i < json.Length && (json[i] == 'e' || json[i] == 'E'))
                    {
                        nonEntier = true;
                        i++;
                        if (i < json.Length && (json[i] == '+' || json[i] == '-')) i++;
                        while (i < json.Length && json[i] >= '0' && json[i] <= '9') i++;
                    }
                    var jeton = json.Substring(debut, i - debut);
                    if (nonEntier) resultat.Append('"').Append(jeton).Append('"');
                    else resultat.Append(jeton);
                    continue;
                }
                resultat.Append(c);
                i++;
            }
            return resultat.ToString();
        }

        /// Lit un nombre depuis un JToken de référence : chaîne (protégée par
        /// `ProtegerLesFlottants`) → `AnalyseDouble.Lire`, correctement arrondi ; entier JSON →
        /// conversion directe, exacte (aucun flottant ne l'a jamais approchée).
        public static double Double(JToken t) =>
            t.Type == JTokenType.String ? AnalyseDouble.Lire((string)t) : (double)(long)t;
    }
}
