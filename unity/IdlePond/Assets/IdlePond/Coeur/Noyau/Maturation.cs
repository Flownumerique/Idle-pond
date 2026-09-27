/*
 * IdlePond — la part mûre d'un palier, et le plafond de maturation. Port de
 * `src/noyau/maturation.ts`.
 *
 * GDD §3.0 : part_mûre(palier) = charge du type mûr ÷ charge totale du palier.
 * C'est ce qui BORNE le canal acclimaté. **Peupler un palier le noie de signature
 * vive, donc dilue son type mûr, donc fait baisser son rendement acclimaté.**
 *
 * La part est portée DIRECTEMENT comme état : elle suit la même loi que
 * l'effectif d'un banc, dont la primitive est fermée. Sa cible est fonction de
 * la PLACE installée, constante sur un pas.
 */
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public readonly struct AvanceeDeMaturation
    {
        /// <summary>Part mûre à la fin de l'intervalle.</summary>
        public readonly double Part;
        /// <summary>∫ part dt sur l'intervalle : le facteur temps du canal acclimaté.</summary>
        public readonly double Integrale;

        public AvanceeDeMaturation(double part, double integrale)
        {
            Part = part;
            Integrale = integrale;
        }
    }

    public static class Maturation
    {
        /// <summary>Une eau que rien n'habite est mûre (§6.5) : le héros descend dans du mûr.</summary>
        public const double PartMureDUneEauIntouchee = 1;

        /// <summary>Part mûre d'équilibre pour une place donnée. Vaut ½ à `PlaceQuiDilueAMoitie`.</summary>
        public static double CibleDeMaturation(double place) =>
            1 / (1 + System.Math.Max(0, place) / Constantes.PlaceQuiDilueAMoitie);

        /// <summary>
        /// Avance exacte de la part mûre sur `dt` secondes.
        ///   p(t)  = C + (p₀ − C)·e^(−t/τ)
        ///   ∫p dt = C·Δ + (p₀ − C)·τ·(1 − e^(−Δ/τ))
        /// </summary>
        public static AvanceeDeMaturation AvancerMaturation(double part, double cible, double dt)
        {
            var tau = Constantes.TauMaturationHeures * 3600;
            if (!(dt > 0)) return new AvanceeDeMaturation(part, 0);
            if (!(tau > 0)) return new AvanceeDeMaturation(cible, cible * dt);

            var ecart = part - cible;
            var perte = -MathJs.Expm1(-dt / tau);
            return new AvanceeDeMaturation(cible + ecart * (1 - perte), cible * dt + ecart * tau * perte);
        }
    }
}
