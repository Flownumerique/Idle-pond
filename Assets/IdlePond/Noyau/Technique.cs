using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — arbre de technique.
    ///
    /// §4.3 : la technique baisse les COÛTS et automatise. Elle ne monte aucune
    /// production. Le typage des effets (`TermeDeCout | TermeDeConfort` pour les
    /// nœuds chiffre, `CapaciteId` pour les nœuds verbe) rend la faute
    /// inexprimable ; le test de canon la vérifie tout de même sur le registre, car
    /// c'est le test le plus fréquemment échoué du projet.
    ///
    /// Les compteurs survivent à la renaissance et ne se redistribuent jamais : on ne
    /// désapprend pas. Les points sont par branche, donc non fongibles — aucune
    /// monnaie commune n'est introduite, un pool fongible rouvrirait la porte du
    /// Corail de Prestige par le côté (§7.4).
    /// </summary>
    public static class Technique
    {
        /// §7.1 — régime de compteur par branche. Trois bornées, trois non bornées.
        public static readonly IReadOnlyDictionary<BrancheTechnique, RegimeCompteur> REGIME_PAR_BRANCHE = new Dictionary<BrancheTechnique, RegimeCompteur>
        {
            [BrancheTechnique.Creusement] = RegimeCompteur.NonBorne,
            [BrancheTechnique.Amelioration] = RegimeCompteur.NonBorne,
            [BrancheTechnique.Entretien] = RegimeCompteur.NonBorne,
            [BrancheTechnique.Recrutement] = RegimeCompteur.Borne,
            [BrancheTechnique.Construction] = RegimeCompteur.Borne,
            [BrancheTechnique.Renaissance] = RegimeCompteur.Borne,
        };

        /// <summary>
        /// [P] — les couples (A, B) des trois branches non bornées sont « à mesurer »
        /// (§13.3) et le §13.3 ne leur donne AUCUNE graine. Ils ne sont donc pas
        /// inventés ici : la table est vide, et elle sera résolue à l'envers en v0.4
        /// depuis la table `verbe → cycle d'ouverture visé` (§7.2). Tant qu'un couple
        /// manque, la branche rend zéro point — un arbre muet, jamais un arbre deviné.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, (double A, double B)> COUPLES_A_B =
            new Dictionary<BrancheTechnique, (double A, double B)>();

        /// <summary>
        /// [P] — tables de seuils des trois branches bornées : figées en v0.4, quand le
        /// calendrier des verbes est arrêté. Sur un compteur qui plafonne, « encore 2
        /// renaissances » est lisible et exact là où le logarithme n'ajoute qu'une barre
        /// opaque.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>> SEUILS_PAR_BRANCHE_BORNEE =
            new Dictionary<BrancheTechnique, IReadOnlyList<double>>();

        /// Points rendus par une branche, d'après son compteur d'usage.
        public static int PointsDeBranche(BrancheTechnique branche, double compteur)
        {
            if (REGIME_PAR_BRANCHE[branche] == RegimeCompteur.Borne)
            {
                if (!SEUILS_PAR_BRANCHE_BORNEE.TryGetValue(branche, out var seuils)) return 0;
                return seuils.Count(s => compteur >= s);
            }
            if (!COUPLES_A_B.TryGetValue(branche, out var couple)) return 0;
            return (int)Math.Floor(couple.A * Math.Log(1 + compteur / couple.B));
        }

        public static int PointsDisponibles(EtatJeu etat, BrancheTechnique branche)
        {
            var compteur = etat.Permanent.CompteursTechnique.TryGetValue(branche, out var c) ? c : 0;
            var rendus = PointsDeBranche(branche, compteur);
            var depenses = NoeudsTechnique.Tous
                .Where(n => n.Branche == branche && etat.Permanent.NoeudsTechnique.Contains(n.Id))
                .Sum(n => n.Cout);
            return rendus - depenses;
        }

        /// <summary>
        /// Facteur appliqué à un terme de coût ou de confort par les nœuds acquis.
        /// Neutre = 1. Un nœud « −20 % » porte un facteur 0.8, jamais un « +12 % »
        /// flottant sans terme nommé (§7.5 règle 3).
        /// </summary>
        public static double FacteurDeTechnique(EtatJeu etat, TermeDeFormule terme)
        {
            var facteur = 1.0;
            foreach (var noeud in NoeudsTechnique.Tous)
            {
                if (noeud.Effet.Nature != NatureDEffet.Chiffre) continue;
                if (noeud.Effet.Terme != terme) continue;
                if (!etat.Permanent.NoeudsTechnique.Contains(noeud.Id)) continue;
                facteur *= noeud.Effet.Facteur;
            }
            return facteur;
        }

        /// Capacités ouvertes par l'arbre. Les succès en ouvrent d'autres, jamais les mêmes.
        public static IReadOnlyCollection<CapaciteId> CapacitesDeLArbre(EtatJeu etat)
        {
            var ouvertes = new HashSet<CapaciteId>();
            foreach (var noeud in NoeudsTechnique.Tous)
            {
                if (noeud.Effet.Nature != NatureDEffet.Verbe) continue;
                if (!etat.Permanent.NoeudsTechnique.Contains(noeud.Id)) continue;
                ouvertes.Add(noeud.Effet.Capacite.Value);
            }
            return ouvertes;
        }

        /// Incrémente un compteur d'usage. Le compteur ne se dépense pas : il produit des points.
        public static IReadOnlyDictionary<BrancheTechnique, double> CreditCompteur(
            IReadOnlyDictionary<BrancheTechnique, double> compteurs, BrancheTechnique branche, double montant)
        {
            var copie = new Dictionary<BrancheTechnique, double>(compteurs);
            copie[branche] = (compteurs.TryGetValue(branche, out var actuel) ? actuel : 0) + montant;
            return copie;
        }
    }
}
