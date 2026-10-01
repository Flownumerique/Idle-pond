using System;
using IdlePond.Noyau;

namespace IdlePond.Jeu
{
    /// <summary>
    /// L'instant, en millisecondes Unix. Une interface, pour que le jeu lise l'heure
    /// système et que les tests lisent une heure qu'ils tiennent en main.
    /// </summary>
    public interface IHorloge
    {
        long MaintenantMs();
    }

    /// <summary>
    /// IdlePond — horloge. `HorlogeSysteme` est LE SEUL endroit autorisé à lire l'heure
    /// système (§10) : le noyau, lui, ne la voit jamais.
    /// </summary>
    public sealed class HorlogeSysteme : IHorloge
    {
        public long MaintenantMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// Horloge déterministe pour les tests et le simulateur.
    public sealed class HorlogeFigee : IHorloge
    {
        long instant;

        public HorlogeFigee(long depart) { instant = depart; }

        public long MaintenantMs() => instant;

        public void AvancerMs(long delta) { instant += delta; }
    }

    /// <summary>
    /// Les deux règles de l'horloge, et la seconde est la seule protection anti-triche
    /// dont le jeu ait besoin :
    ///   - le recul d'horloge est ignoré ;
    ///   - l'avance ne crédite que du temps hors ligne plafonné, et le compteur
    ///     Entretien ne lit que les heures CRÉDITÉES, jamais le temps écoulé. Sans ça,
    ///     avancer son horloge farme l'arbre permanent.
    ///
    /// On ne punit jamais l'absence : le plafond borne ce que l'absence rapporte, il ne
    /// retire rien.
    /// </summary>
    public static class Horloge
    {
        public static double CapHorsLigneSecondes(double heures)
        {
            var borne = Math.Min(Math.Max(heures, Constantes.CAP_HORS_LIGNE_HEURES_INITIAL), Constantes.CAP_HORS_LIGNE_HEURES_MAXIMUM);
            return borne * 3600;
        }

        /// <summary>
        /// Secondes hors ligne effectivement créditées entre deux instants. Rendues
        /// telles quelles au noyau, qui les absorbe en UN SEUL appel à Tick — c'est
        /// précisément ce que garantit le filtre du §5.2.
        /// </summary>
        public static double SecondesHorsLigneCreditees(long dernierInstantMs, long maintenantMs, double capHeures)
        {
            var ecoule = maintenantMs - dernierInstantMs;
            // Écrit tel quel, et non `ecoule <= 0` : un recul d'horloge ne crédite rien.
            if (!(ecoule > 0)) return 0;
            return Math.Min(ecoule / 1000.0, CapHorsLigneSecondes(capHeures));
        }

        /// <summary>
        /// Graine d'une nouvelle partie.
        ///
        /// Le noyau exige un PRNG à graine et interdit le hasard libre ; il faut bien que
        /// la graine vienne de quelque part, et ce quelque part est ce module — le seul
        /// qui ait le droit d'être impur. Les 32 bits bas, non signés, comme `>>> 0`.
        /// </summary>
        public static long GrainePourNouvellePartie(IHorloge horloge) => horloge.MaintenantMs() & 0xFFFFFFFF;
    }
}
