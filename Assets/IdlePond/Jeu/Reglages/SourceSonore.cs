using UnityEngine;

namespace IdlePond.Jeu
{
    /// <summary>
    /// À poser à côté d'un `AudioSource` : la source suit les réglages de son canal. Une
    /// musique, c'est un `AudioSource` + une `SourceSonore` réglée sur `Musique` ; les
    /// curseurs du menu la règlent sans autre câblage.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SourceSonore : MonoBehaviour
    {
        public Canal canal = Canal.Effets;

        /// Le volume propre au son, avant les réglages du joueur.
        [Range(0f, 1f)] public float volumeDeBase = 1f;

        AudioSource source;
        MagasinDeReglages magasin;

        void OnEnable()
        {
            source = GetComponent<AudioSource>();
            magasin = Boucle.ObtenirOuCreerLesReglages();
            magasin.Change += Regler;
            Regler(magasin.Courants);
        }

        void OnDisable()
        {
            if (magasin != null) magasin.Change -= Regler;
            magasin = null;
        }

        void Regler(Reglages r) => source.volume = volumeDeBase * Son.VolumeEffectif(canal, r);
    }
}
