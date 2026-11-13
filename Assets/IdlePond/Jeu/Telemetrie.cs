using System.IO;
using System.Text;
using IdlePond.Noyau;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IdlePond.Jeu
{
    /// <summary>
    /// Un puits de télémétrie. L'implémentation réseau viendra ; le contrat non.
    ///
    /// Le relevé, lui, est pur et vit dans le noyau (`Telemetrie.Relever`) : seul ce qui
    /// publie — donc ce qui touche le monde — appartient à cette assembly.
    /// </summary>
    public interface ICollecteur
    {
        void Publier(Releve releve);
    }

    public sealed class CollecteurSilencieux : ICollecteur
    {
        public static readonly CollecteurSilencieux Instance = new CollecteurSilencieux();

        public void Publier(Releve releve) { }
    }

    /// <summary>
    /// Écrit une ligne JSON par renaissance dans `telemetrie.jsonl`, à côté de la partie :
    /// c'est ce qui permet de mesurer une VRAIE partie, et plus seulement le simulateur
    /// (règle d'engagement : « aucune assise n'est produite avant que la précédente ait été
    /// mesurée »). Le fichier ne fait que grandir ; rien ne le relit en jeu.
    ///
    /// Le dossier est injecté, comme pour `Persistance` : les ateliers et les tests qui
    /// redirigent la sauvegarde redirigent aussi ce fichier.
    /// </summary>
    public sealed class CollecteurSurDisque : ICollecteur
    {
        public const string NOM_DU_FICHIER = "telemetrie.jsonl";

        readonly string dossier;

        public CollecteurSurDisque(string dossier) => this.dossier = dossier;

        public string Chemin => Path.Combine(dossier, NOM_DU_FICHIER);

        /// Le dernier cycle clos du relevé, en une ligne. Un relevé sans cycle n'écrit rien.
        public void Publier(Releve releve)
        {
            if (releve == null || releve.Cycles.Count == 0) return;
            var cycle = releve.Cycles[releve.Cycles.Count - 1];
            var ligne = new JObject
            {
                ["renaissances"] = releve.NombreDeRenaissances,
                ["cycle"] = cycle.Index,
                ["dureeSecondes"] = cycle.DureeEcouleeSecondes,
                ["paliersOuverts"] = cycle.PaliersOuverts,
                ["productionPicParSeconde"] = cycle.ProductionPicParSeconde,
                ["souffleGagne"] = cycle.SouffleGagne,
                ["intervalleMoyenEntreSuccesSecondes"] = releve.IntervalleMoyenEntreSuccesSecondes,
                ["tempsEcouleSecondes"] = releve.TempsEcouleSecondes,
            };
            Directory.CreateDirectory(dossier);
            File.AppendAllText(Chemin, ligne.ToString(Formatting.None) + "\n", new UTF8Encoding(false));
        }
    }
}
