/*
 * IdlePond — calibreur. Port de `src/simulateur/calibreur.ts`.
 *
 * §7.2, et c'est le point le plus contre-intuitif du projet : le calibrage se
 * fait à l'envers. « Le calendrier des verbes est le seul réglage de durée
 * réelle du jeu. » La donnée d'ENTRÉE est une table `verbe → cycle d'ouverture
 * visé` ; le calibreur résout (A, B) à l'envers. On ne règle JAMAIS « combien de
 * points par cycle » à la main.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Simulateur
{
    /// <summary>Ce que l'auteur fournit : à quel cycle chaque rang de la branche s'ouvre.</summary>
    public sealed record CibleDOuverture(BrancheTechnique Branche, IReadOnlyList<int> CyclesVises);

    public sealed record CoupleResolu(double A, double B, double Erreur);

    public static class Calibreur
    {
        private static double CoutCumuleJusquAuRang(int rang) => Constantes.CoutsDeNoeud.Take(rang + 1).Sum();

        /// <summary>Cycle auquel (A, B) ouvre chaque rang, d'après la trajectoire mesurée. null : jamais.</summary>
        public static IReadOnlyList<int?> CyclesDOuverture(double a, double b, IReadOnlyList<double> trajectoire, int nombreDeRangs)
        {
            var points = trajectoire.Select(compteur => Math.Floor(a * Math.Log(1 + compteur / b))).ToArray();
            var cycles = new int?[nombreDeRangs];
            for (var rang = 0; rang < nombreDeRangs; rang += 1)
            {
                var requis = CoutCumuleJusquAuRang(rang);
                var cycle = Array.FindIndex(points, p => p >= requis);
                cycles[rang] = cycle == -1 ? (int?)null : cycle;
            }
            return cycles;
        }

        /// <summary>
        /// Résout (A, B) à l'envers depuis les cycles d'ouverture visés. Balayage
        /// logarithmique sur B, puis sur A. Ne s'applique qu'aux branches NON BORNÉES.
        /// </summary>
        public static CoupleResolu ResoudreCoupleAB(CibleDOuverture cible, IReadOnlyList<double> trajectoire)
        {
            if (Technique.RegimeDe(cible.Branche) == RegimeCompteur.Borne) return null;
            if (trajectoire.Count == 0 || cible.CyclesVises.Count == 0) return null;

            CoupleResolu meilleur = null;
            // Le pas flottant de 0,05 est celui du web, accumulation d'arrondi comprise :
            // c'est ce qui fait tomber les deux balayages sur les mêmes valeurs.
            for (var exposant = -6.0; exposant <= 12; exposant += 0.05)
            {
                var b = Math.Exp(exposant);
                for (var a = 0.5; a <= 400; a *= 1.02)
                {
                    var obtenus = CyclesDOuverture(a, b, trajectoire, cible.CyclesVises.Count);
                    double erreur = 0;
                    for (var rang = 0; rang < cible.CyclesVises.Count; rang += 1)
                    {
                        // Un rang qui ne s'ouvre jamais est pénalisé au-delà de l'horizon
                        // simulé : un arbre jamais fini est aussi faux qu'un arbre fini au
                        // cycle 8 (§7.6).
                        var effectif = obtenus[rang] ?? trajectoire.Count + cible.CyclesVises.Count;
                        erreur += Math.Pow(effectif - cible.CyclesVises[rang], 2);
                    }
                    if (meilleur == null || erreur < meilleur.Erreur) meilleur = new CoupleResolu(a, b, erreur);
                    if (erreur == 0) return new CoupleResolu(a, b, erreur);
                }
            }
            return meilleur;
        }
    }
}
