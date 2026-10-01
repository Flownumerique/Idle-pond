using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau
{
    public sealed record ReleveDeCycle(
        int Index,
        double DureeEcouleeSecondes,
        int PaliersOuverts,
        double ProductionPicParSeconde,
        double SouffleGagne);

    /// <summary>
    /// `IntervalleMoyenEntreSuccesSecondes` détecte l'assèchement de mi-partie ; null
    /// tant qu'aucun intervalle n'a été observé.
    ///
    /// `TempsEcouleSecondes` : temps de jeu ÉCOULÉ — le temps calendaire du §11, pas
    /// le temps actif. Le temps actif se mesure du côté des politiques, pas ici : le
    /// noyau ne sait pas quand quelqu'un regarde.
    /// </summary>
    public sealed record Releve(
        int NombreDeRenaissances,
        IReadOnlyList<ReleveDeCycle> Cycles,
        double? IntervalleMoyenEntreSuccesSecondes,
        IReadOnlyDictionary<BrancheTechnique, int> PointsDeTechniqueRendus,
        double TempsEcouleSecondes);

    /// <summary>
    /// IdlePond — télémétrie, sa partie pure.
    ///
    /// §11 : instrumentée dès le jalon v0.1. Elle est un OBJET DE MESURE, pas de la
    /// finition — c'est elle qui réfute `α`, l'exposant de densité, les couples
    /// (A, B) et le rapport g/D. Ce qui n'est pas mesuré ne peut pas être calibré,
    /// et un paramètre non calibré est un paramètre inventé.
    ///
    /// `tauDeRepeuplementSecondes` est parti avec le modèle à population : il n'y a
    /// plus de population à repeupler, donc plus de `k` à mesurer.
    ///
    /// Les métriques dérivables de l'état sont relevées ici, purement. Celles qui
    /// demandent l'heure (temps calendaire, intervalle réel entre deux sessions)
    /// passent par l'horloge, côté adaptateur — tout comme le collecteur qui publie
    /// le relevé, qui n'appartient pas au noyau.
    /// </summary>
    public static class Telemetrie
    {
        public static Releve Relever(EtatJeu etat)
        {
            var pointsDeTechniqueRendus = new Dictionary<BrancheTechnique, int>();
            foreach (var branche in Technique.REGIME_PAR_BRANCHE.Keys)
                pointsDeTechniqueRendus[branche] = Technique.PointsDeBranche(
                    branche, etat.Permanent.CompteursTechnique.TryGetValue(branche, out var compteur) ? compteur : 0);

            var intervalles = etat.Telemetrie.IntervallesEntreSucces;
            return new Releve(
                NombreDeRenaissances: etat.Permanent.NombreDeRenaissances,
                Cycles: etat.Telemetrie.Cycles.Select(c => new ReleveDeCycle(
                    c.Index,
                    c.DureeEcouleeSecondes,
                    c.PaliersOuverts,
                    c.ProductionPicParSeconde.ToNumber(),
                    c.SouffleGagne.ToNumber())).ToList(),
                IntervalleMoyenEntreSuccesSecondes:
                    intervalles.Count > 0 ? intervalles.Aggregate(0.0, (a, b) => a + b) / intervalles.Count : (double?)null,
                PointsDeTechniqueRendus: pointsDeTechniqueRendus,
                TempsEcouleSecondes: etat.TempsJeuSecondes);
        }
    }
}
