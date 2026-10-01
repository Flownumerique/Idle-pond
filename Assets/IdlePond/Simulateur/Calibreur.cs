using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Simulateur
{
    /// Ce que l'auteur fournit : à quel cycle chaque rang de la branche s'ouvre.
    /// `CyclesVises` : cycle visé pour chaque rang, index 0 = premier nœud.
    public sealed record CibleDOuverture(BrancheTechnique Branche, IReadOnlyList<int> CyclesVises);

    /// `Erreur` : somme des carrés des écarts, en cycles, entre ouverture visée et obtenue.
    public sealed record CoupleAB(double A, double B, double Erreur);

    /// <summary>
    /// IdlePond — calibreur.
    ///
    /// §7.2, et c'est le point le plus contre-intuitif du projet : le calibrage se
    /// fait à l'envers.
    ///
    ///   « Le calendrier des verbes est le seul réglage de durée réelle du jeu. »
    ///
    /// Raison : le simulateur établit ~38 h de jeu ACTIF pour ~600 h CALENDAIRES.
    /// L'écart entier est du temps d'attente entre check-ins. Or un nœud CHIFFRE
    /// réduit un coût dans une économie géométrique : une réduction de facteur `c`
    /// vaut `log_g(1/c)` paliers d'avance, soit — pour toute la branche Creusement —
    /// moins d'un palier. C'est un décalage additif constant, qui ne compose pas. Un
    /// nœud VERBE, lui, supprime des intervalles de check-in, c'est-à-dire les 560
    /// heures qui ne sont pas du jeu actif.
    ///
    /// Conséquence, et elle est structurante pour ce fichier : la donnée d'ENTRÉE
    /// est une table `branche → cycle d'ouverture visé`. Le calibreur résout (A, B) à
    /// l'envers. On ne règle JAMAIS « combien de points par cycle » à la main.
    /// </summary>
    public static class Calibreur
    {
        static int CoutCumuleJusquAuRang(int rang) => Constantes.COUTS_DE_NOEUD.Take(rang + 1).Sum();

        /// <summary>
        /// Cycle auquel (A, B) ouvre chaque rang, d'après la trajectoire mesurée — le
        /// compteur d'usage à la fin de chaque cycle.
        /// </summary>
        public static IReadOnlyList<int?> CyclesDOuverture(double a, double b, IReadOnlyList<double> trajectoire, int nombreDeRangs)
        {
            var points = trajectoire.Select(compteur => Math.Floor(a * Math.Log(1 + compteur / b))).ToArray();
            var resultat = new List<int?>();
            for (var rang = 0; rang < nombreDeRangs; rang += 1)
            {
                var requis = CoutCumuleJusquAuRang(rang);
                var cycle = Array.FindIndex(points, p => p >= requis);
                resultat.Add(cycle == -1 ? (int?)null : cycle);
            }
            return resultat;
        }

        /// <summary>
        /// Résout (A, B) à l'envers depuis les cycles d'ouverture visés.
        ///
        /// Balayage logarithmique sur B puis résolution directe de A : pour un B donné,
        /// A est fixé par la contrainte du premier rang, et le reste de la branche
        /// s'ensuit. C'est une recherche, pas une formule fermée — mais l'entrée reste
        /// le calendrier des verbes, jamais un débit de points choisi à la main.
        ///
        /// Ne s'applique qu'aux branches à compteur NON BORNÉ : sur une branche bornée,
        /// le §7.1 impose une table de seuils directe, et « encore 2 renaissances » est
        /// lisible et exact là où le logarithme n'ajoute qu'une barre opaque.
        /// </summary>
        public static CoupleAB ResoudreCoupleAB(CibleDOuverture cible, IReadOnlyList<double> trajectoire)
        {
            if (Technique.REGIME_PAR_BRANCHE[cible.Branche] == RegimeCompteur.Borne) return null;
            if (trajectoire.Count == 0 || cible.CyclesVises.Count == 0) return null;

            CoupleAB meilleur = null;
            for (var exposant = -6.0; exposant <= 12; exposant += 0.05)
            {
                var b = Math.Exp(exposant);
                for (var a = 0.5; a <= 400; a *= 1.02)
                {
                    var obtenus = CyclesDOuverture(a, b, trajectoire, cible.CyclesVises.Count);
                    var erreur = 0.0;
                    for (var rang = 0; rang < cible.CyclesVises.Count; rang += 1)
                    {
                        var obtenu = obtenus[rang];
                        // Un rang qui ne s'ouvre jamais est pénalisé au-delà de l'horizon
                        // simulé : un arbre jamais fini est aussi faux qu'un arbre fini au
                        // cycle 8 (§7.6).
                        var cycleObtenu = obtenu ?? trajectoire.Count + cible.CyclesVises.Count;
                        erreur += Math.Pow(cycleObtenu - cible.CyclesVises[rang], 2);
                    }
                    if (meilleur == null || erreur < meilleur.Erreur) meilleur = new CoupleAB(a, b, erreur);
                    if (erreur == 0) return new CoupleAB(a, b, erreur);
                }
            }
            return meilleur;
        }
    }
}
