/*
 * IdlePond — densité. Port de `src/noyau/densite.ts`.
 *
 * Tier 0, invariant : la densité ne redescend pas. Elle est monotone croissante
 * par palier et survit à l'éclosion. Elle a UN SEUL débouché depuis V11 —
 * l'acquis de séjour, via `MultiplicateurDensite` : « séjour en mana dense ».
 */
using System;
using System.Collections.Generic;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public static class Densite
    {
        public static double DensiteDuPalier(EtatJeu etat, int palier) =>
            palier >= 0 && palier < etat.Permanent.Densites.Count ? etat.Permanent.Densites[palier] : 0;

        /// <summary>
        /// Multiplicateur de densité : `densité ^ (θ/α)` (amendement v1.1 §2.A). Il
        /// raccourcit le temps caractéristique du séjour ; planché à 1.
        /// </summary>
        public static double MultiplicateurDensite(double densite) =>
            Math.Pow(Math.Max(1, densite), Constantes.DensiteExposant());

        /// <summary>
        /// Vitesse de repeuplement, par seconde. **V11 : la densité est DÉCOUPLÉE du
        /// repeuplement.** À exposant nu, `k` s'effondrait de 300 s à 10⁻⁴ s en quinze
        /// cycles, et avec lui le délai entre l'achat d'une place et son effet.
        /// </summary>
        public static double VitesseDeRepeuplement() => Constantes.KTauxDeRepeuplement;

        /// <summary>Densité qu'un cycle laisse derrière lui : `pointe ^ α` (§2.A).</summary>
        public static double DensiteLaisseeParLeCycle(GrandNombre productionDePic)
        {
            var rapport = productionDePic.Div(Constantes.ProductionDeReference);
            if (rapport.Lte(0)) return 0;
            return Math.Pow(rapport.ToNumber(), Constantes.AlphaGainDeDensite);
        }

        /// <summary>
        /// Porte la densité des paliers occupés au niveau que le cycle a laissé.
        /// `max`, jamais une affectation : la densité ne redescend JAMAIS (Tier 0).
        /// </summary>
        public static IReadOnlyList<double> AppliquerGainDeDensite(EtatJeu etat, int paliersOuverts, GrandNombre productionDePic)
        {
            var conservation = Technique.FacteurDeTechnique(etat, TermeDeFormule.DensiteConservee);
            var laissee = DensiteLaisseeParLeCycle(productionDePic) * conservation;
            if (!(laissee > 0)) return etat.Permanent.Densites;
            var densites = new double[etat.Permanent.Densites.Count];
            for (var index = 0; index < densites.Length; index += 1)
            {
                var d = etat.Permanent.Densites[index];
                densites[index] = index < paliersOuverts ? Math.Max(d, laissee) : d;
            }
            return densites;
        }
    }
}
