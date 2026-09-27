/*
 * IdlePond — télémétrie. Port de `src/adaptateurs/telemetrie.ts`.
 *
 * §11 : un OBJET DE MESURE, pas de la finition — c'est elle qui réfute `α`,
 * l'exposant de densité, `k`, les couples (A, B) et le rapport g/D.
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Adaptateurs
{
    public sealed record ReleveDeCycle
    {
        public int Index { get; init; }
        public double DureeEcouleeSecondes { get; init; }
        /// <summary>Risque n° 1 : « la redescente devient le jeu ». Métrique n° 1.</summary>
        public double FractionEnRedescente { get; init; }
        public int PaliersOuverts { get; init; }
        public double ProductionPicParSeconde { get; init; }
        public double FoiGagnee { get; init; }
    }

    public sealed record Releve
    {
        public int NombreEclosions { get; init; }
        public IReadOnlyList<ReleveDeCycle> Cycles { get; init; }
        /// <summary>Détecte l'assèchement de mi-partie. null sans intervalle mesuré.</summary>
        public double? IntervalleMoyenEntreSuccesSecondes { get; init; }
        public IReadOnlyDictionary<BrancheTechnique, double> PointsDeTechniqueRendus { get; init; }
        /// <summary>Temps de jeu ÉCOULÉ — le calendaire du §11, pas le temps actif.</summary>
        public double TempsEcouleSecondes { get; init; }
        /// <summary>Temps caractéristique du repeuplement, en secondes. Décision V11.</summary>
        public double TauDeRepeuplementSecondes { get; init; }
    }

    public static class Telemetrie
    {
        public static Releve Relever(EtatJeu etat)
        {
            var points = new Dictionary<BrancheTechnique, double>();
            foreach (var branche in Branches.Toutes) points[branche] = Technique.PointsDeBranche(branche, Technique.Compteur(etat, branche));

            var intervalles = etat.Telemetrie.IntervallesEntreSucces;
            return new Releve
            {
                NombreEclosions = etat.Permanent.NombreEclosions,
                Cycles = etat.Telemetrie.Cycles.Select(c => new ReleveDeCycle
                {
                    Index = c.Index,
                    DureeEcouleeSecondes = c.DureeEcouleeSecondes,
                    FractionEnRedescente = c.DureeEcouleeSecondes > 0 ? c.SecondesEnRedescente / c.DureeEcouleeSecondes : 0,
                    PaliersOuverts = c.PaliersOuverts,
                    ProductionPicParSeconde = c.ProductionPicParSeconde.ToNumber(),
                    FoiGagnee = c.FoiGagnee.ToNumber(),
                }).ToArray(),
                IntervalleMoyenEntreSuccesSecondes = intervalles.Count > 0 ? intervalles.Sum() / intervalles.Count : (double?)null,
                PointsDeTechniqueRendus = points,
                TempsEcouleSecondes = etat.TempsJeuSecondes,
                TauDeRepeuplementSecondes = 1 / Densite.VitesseDeRepeuplement(),
            };
        }
    }

    /// <summary>Un puits de télémétrie. L'implémentation réseau viendra ; le contrat non.</summary>
    public interface ICollecteur
    {
        void Publier(Releve releve);
    }

    public sealed class CollecteurSilencieux : ICollecteur
    {
        public static readonly CollecteurSilencieux Instance = new CollecteurSilencieux();

        public void Publier(Releve releve)
        {
        }
    }
}
