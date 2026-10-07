using IdlePond.Jeu;

namespace IdlePond.Atelier
{
    /// <summary>
    /// L'horloge des ateliers : une autre horloge, plus un décalage. « Partir 8 h », c'est
    /// décaler de 8 h puis `Partie.Reprendre` — le chemin réel du retour. L'heure système,
    /// elle, reste lue par `HorlogeSysteme` seule.
    /// </summary>
    public sealed class HorlogeDecalee : IHorloge
    {
        readonly IHorloge source;
        long decalageMs;

        public HorlogeDecalee(IHorloge source) { this.source = source; }

        public long MaintenantMs() => source.MaintenantMs() + decalageMs;

        public void Decaler(long deltaMs) { decalageMs += deltaMs; }
    }
}
