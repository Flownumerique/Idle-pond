using System;

namespace IdlePond.Jeu
{
    /// <summary>
    /// Porte la partie d'une scène à l'autre : l'Amorce de `Demarrage.unity` l'installe,
    /// `Mare.unity` la reprend. Un singleton C# simple, sans Unity — l'objet
    /// `DontDestroyOnLoad` qui l'entoure, s'il en faut un, est l'affaire du code de scène.
    ///
    /// Si l'on lance `Mare.unity` directement dans l'éditeur, personne n'a installé de
    /// partie : `ObtenirOuCreer` en crée une, pour que la scène reste jouable seule.
    /// </summary>
    public static class ServicesDePartie
    {
        static Partie courante;

        public static bool EstInstallee => courante != null;

        public static Partie Partie => courante ?? throw new InvalidOperationException("Aucune partie installée.");

        public static void Installer(Partie partie) => courante = partie ?? throw new ArgumentNullException(nameof(partie));

        public static Partie ObtenirOuCreer(Func<Partie> creer)
        {
            if (courante == null) courante = creer();
            return courante;
        }

        static MagasinDeReglages reglages;

        /// Les réglages du joueur, ouverts une fois et gardés avec la partie : ils vivent
        /// dans le même dossier, et le même `Oublier` les remet au disque.
        public static MagasinDeReglages ObtenirOuCreerLesReglages(Func<MagasinDeReglages> creer)
        {
            if (reglages == null) reglages = creer();
            return reglages;
        }

        /// <summary>
        /// Le dossier où la `Boucle` sauvegarde à la place de `persistentDataPath`, s'il est
        /// posé. Seuls les ateliers le posent : un bac à sable qui donne du mana ne doit
        /// jamais écrire dans la partie du joueur. Oublié avec la partie.
        /// </summary>
        public static string DossierDeSauvegardeRedirige => redirige;

        static string redirige;

        public static void RedirigerLaSauvegarde(string dossier) =>
            redirige = string.IsNullOrEmpty(dossier) ? throw new ArgumentException("Dossier vide.", nameof(dossier)) : dossier;

        public static void Oublier()
        {
            courante = null;
            reglages = null;
            redirige = null;
        }
    }
}
