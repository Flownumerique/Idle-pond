namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Un geste qui efface se confirme : le premier toucher arme, un second dans
    /// `DELAI_MS` confirme. Passé le délai, on repart de zéro. Pur — l'instant est donné
    /// par l'appelant — pour se tester sans moteur.
    /// </summary>
    public sealed class Confirmation
    {
        public const long DELAI_MS = 3000;

        long? armeeA;

        /// Vrai si ce toucher confirme ; faux s'il ne fait qu'armer.
        public bool Toucher(long maintenantMs)
        {
            if (EnAttente(maintenantMs))
            {
                armeeA = null;
                return true;
            }
            armeeA = maintenantMs;
            return false;
        }

        public bool EnAttente(long maintenantMs) => armeeA.HasValue && maintenantMs - armeeA.Value <= DELAI_MS;

        public void Annuler() => armeeA = null;
    }
}
