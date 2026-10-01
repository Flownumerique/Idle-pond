using IdlePond.Noyau;

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
}
