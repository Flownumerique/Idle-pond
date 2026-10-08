namespace IdlePond.Jeu
{
    /// <summary>
    /// Le volume qu'une source doit jouer, selon son canal. Pur : `SourceSonore` le pose sur
    /// son `AudioSource`, ce fichier ne sait rien du moteur.
    ///
    /// Pas d'`AudioMixer` : Unity n'en crée pas par script, et tout le projet est généré.
    /// </summary>
    public static class Son
    {
        public static float VolumeEffectif(Canal canal, Reglages r)
        {
            if (r.Muet) return 0f;
            var propre = canal == Canal.Musique ? r.VolumeMusique : r.VolumeEffets;
            return (float)(r.VolumeGeneral * propre);
        }
    }
}
