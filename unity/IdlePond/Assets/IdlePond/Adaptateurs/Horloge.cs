/*
 * IdlePond — horloge. LE SEUL module autorisé à lire l'heure système (§10).
 * Port de `src/adaptateurs/horloge.ts`.
 *
 * Deux règles, et la seconde est la seule protection anti-triche dont le jeu
 * ait besoin :
 *   - le recul d'horloge est ignoré ;
 *   - l'avance ne crédite que du temps hors ligne plafonné, et le compteur
 *     Entretien ne lit que les heures CRÉDITÉES, jamais le temps écoulé.
 *
 * On ne punit jamais l'absence : le plafond borne ce que l'absence rapporte, il
 * ne retire rien.
 */
using System;
using IdlePond.Noyau;

namespace IdlePond.Adaptateurs
{
    public interface IHorloge
    {
        /// <summary>Millisecondes depuis l'époque Unix, comme `Date.now()`.</summary>
        double MaintenantMs();
    }

    public sealed class HorlogeSysteme : IHorloge
    {
        public static readonly HorlogeSysteme Instance = new HorlogeSysteme();

        private HorlogeSysteme()
        {
        }

        public double MaintenantMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>Horloge déterministe pour les tests et le simulateur.</summary>
    public sealed class HorlogeFigee : IHorloge
    {
        private double _instant;

        public HorlogeFigee(double depart) => _instant = depart;

        public double MaintenantMs() => _instant;

        public void AvancerMs(double delta) => _instant += delta;
    }

    public static class Horloge
    {
        public static double CapHorsLigneSecondes(double heures)
        {
            var borne = Math.Min(Math.Max(heures, Constantes.CapHorsLigneHeuresInitial), Constantes.CapHorsLigneHeuresMaximum);
            return borne * 3600;
        }

        /// <summary>
        /// Secondes hors ligne effectivement créditées entre deux instants. Rendues
        /// telles quelles au noyau, qui les absorbe en UN SEUL appel à Tick.
        /// </summary>
        public static double SecondesHorsLigneCreditees(double dernierInstantMs, double maintenantMs, double capHeures)
        {
            var ecoule = maintenantMs - dernierInstantMs;
            if (!(ecoule > 0)) return 0;
            return Math.Min(ecoule / 1000, CapHorsLigneSecondes(capHeures));
        }

        /// <summary>
        /// Graine d'une nouvelle partie : `Date.now() >>> 0`. Le noyau interdit le
        /// hasard système ; la graine vient donc d'ici, le seul module qui ait le
        /// droit d'être impur.
        /// </summary>
        public static uint GrainePourNouvellePartie(IHorloge horloge = null)
        {
            var ms = (horloge ?? HorlogeSysteme.Instance).MaintenantMs();
            return unchecked((uint)(ulong)Math.Floor(ms));
        }
    }
}
