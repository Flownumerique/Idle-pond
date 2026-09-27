/*
 * IdlePond — population d'un banc : effectif, repeuplement. Port de
 * `src/noyau/population.ts`.
 *
 * Le filtre du §5.2 décide de tout ce module : « toute mécanique du cœur doit
 * se calculer en un seul pas pour dt = 8 heures ». L'effectif converge vers sa
 * cible par une exponentielle dont la primitive est fermée : avancer de 8 h en
 * un pas ou en 480 pas de 60 s donne le même effectif ET la même intégrale.
 */
using System;
using System.Collections.Generic;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public readonly struct AvanceeDeBanc
    {
        /// <summary>Effectif à la fin de l'intervalle.</summary>
        public readonly double Effectif;
        /// <summary>∫ effectif dt sur l'intervalle.</summary>
        public readonly double IntegraleEffectif;
        /// <summary>∫ multiplicateur(effectif) × effectif dt : c'est lui qui donne le mana produit.</summary>
        public readonly double IntegralePonderee;
        /// <summary>Multiplicateur de seuil à la fin de l'intervalle.</summary>
        public readonly double MultiplicateurFinal;

        public AvanceeDeBanc(double effectif, double integraleEffectif, double integralePonderee, double multiplicateurFinal)
        {
            Effectif = effectif;
            IntegraleEffectif = integraleEffectif;
            IntegralePonderee = integralePonderee;
            MultiplicateurFinal = multiplicateurFinal;
        }
    }

    public static class Population
    {
        private static readonly IReadOnlyList<SeuilPondere> AucunSeuil = Array.Empty<SeuilPondere>();

        private static double MultiplicateurA(double effectif, IReadOnlyList<SeuilPondere> seuils)
        {
            double multiplicateur = 1;
            foreach (var seuil in seuils)
            {
                if (effectif >= seuil.Seuil) multiplicateur = seuil.MultiplicateurCumule;
            }
            return multiplicateur;
        }

        /// <summary>L'effectif cible d'un banc est la PLACE qu'on lui a faite.</summary>
        public static double EffectifCible(int place) => place;

        /// <summary>
        /// Avance exacte de l'effectif sur `dt` secondes, seuils compris.
        ///
        ///   e(t)  = C + (e0 − C)·e^(−k·t)
        ///   ∫e dt = C·Δ + (e0 − C)·(1 − e^(−k·Δ))/k
        ///
        /// Le multiplicateur de seuil change EN COURS D'INTERVALLE ; `e` étant
        /// monotone, chaque seuil est franchi au plus une fois, et l'intervalle se
        /// découpe en au plus cinq morceaux à multiplicateur constant — une partition
        /// analytique bornée, jamais une file d'événements.
        /// </summary>
        public static AvanceeDeBanc AvancerBanc(double effectif, double cible, double k, double dt, IReadOnlyList<SeuilPondere> seuils = null)
        {
            seuils = seuils ?? AucunSeuil;
            if (dt <= 0)
            {
                return new AvanceeDeBanc(effectif, 0, 0, MultiplicateurA(effectif, seuils));
            }

            var ecart = effectif - cible;
            if (k <= 0 || ecart == 0)
            {
                var multiplicateur = MultiplicateurA(effectif, seuils);
                return new AvanceeDeBanc(effectif, effectif * dt, multiplicateur * effectif * dt, multiplicateur);
            }

            // expm1 plutôt que 1 - exp : sur les pas de 100 ms, k·dt vaut ~3e-4.
            var perte = -MathJs.Expm1(-k * dt);
            var effectifFinal = cible + ecart * (1 - perte);
            var integraleEffectif = cible * dt + (ecart * perte) / k;

            var instants = InstantsDeFranchissement(effectif, cible, k, dt, seuils);
            if (instants.Count == 0)
            {
                var multiplicateur = MultiplicateurA(effectif, seuils);
                return new AvanceeDeBanc(effectifFinal, integraleEffectif, multiplicateur * integraleEffectif, multiplicateur);
            }

            double integralePonderee = 0;
            double debut = 0;
            instants.Add(dt);
            foreach (var fin in instants)
            {
                if (fin <= debut) continue;
                // Le multiplicateur est lu au MILIEU du morceau : au bord exact,
                // l'erreur d'arrondi peut retomber du mauvais côté du seuil.
                var multiplicateur = MultiplicateurA(EffectifA((debut + fin) / 2, effectif, cible, k), seuils);
                integralePonderee += multiplicateur * IntegraleSur(debut, fin, effectif, cible, k);
                debut = fin;
            }

            return new AvanceeDeBanc(effectifFinal, integraleEffectif, integralePonderee, MultiplicateurA(effectifFinal, seuils));
        }

        private static double EffectifA(double t, double effectif, double cible, double k) =>
            cible + (effectif - cible) * Math.Exp(-k * t);

        /// <summary>∫ e dt sur [a, b], fermée.</summary>
        private static double IntegraleSur(double a, double b, double effectif, double cible, double k)
        {
            var ecart = effectif - cible;
            return cible * (b - a) + (ecart * (Math.Exp(-k * a) - Math.Exp(-k * b))) / k;
        }

        /// <summary>Instants, croissants, où l'effectif franchit un seuil. Au plus un par seuil.</summary>
        private static List<double> InstantsDeFranchissement(
            double effectif, double cible, double k, double dt, IReadOnlyList<SeuilPondere> seuils)
        {
            var instants = new List<double>();
            foreach (var s in seuils)
            {
                var rapport = (s.Seuil - cible) / (effectif - cible);
                if (!(rapport > 0) || rapport >= 1) continue;
                var instant = -Math.Log(rapport) / k;
                if (instant > 0 && instant < dt) instants.Add(instant);
            }
            instants.Sort();
            return instants;
        }
    }
}
