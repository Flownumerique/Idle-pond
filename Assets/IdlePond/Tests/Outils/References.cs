using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Les fichiers produits par `tests/parite/generer-references.ts` pendant que
    /// le TypeScript était la vérité. Le répertoire courant d'un test EditMode est
    /// la racine du projet Unity.
    /// </summary>
    public static class References
    {
        public static JObject Lire(string nom)
        {
            var chemin = Path.GetFullPath(Path.Combine("Assets", "IdlePond", "Tests", "Reference", nom + ".json"));
            using var lecteur = new JsonTextReader(new StreamReader(chemin, Encoding.UTF8))
            {
                // Un double reste un double : jamais un System.Decimal arrondi.
                FloatParseHandling = FloatParseHandling.Double,
            };
            return JObject.Load(lecteur);
        }
    }
}
