/*
 * IdlePond — arbre de technique. Port de `src/noyau/technique.ts`.
 *
 * §4.3 : la technique baisse les COÛTS et automatise. Elle ne monte aucune
 * production. Les compteurs survivent à l'éclosion et ne se redistribuent
 * jamais : on ne désapprend pas. Les points sont par branche, donc non
 * fongibles.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;

namespace IdlePond.Noyau
{
    public sealed record CoupleAB(double A, double B);

    public static class Technique
    {
        /// <summary>§7.1 — régime de compteur par branche. Trois bornées, trois non bornées.</summary>
        public static RegimeCompteur RegimeDe(BrancheTechnique branche)
        {
            switch (branche)
            {
                case BrancheTechnique.Recrutement:
                case BrancheTechnique.Construction:
                case BrancheTechnique.Eclosion:
                    return RegimeCompteur.Borne;
                default:
                    return RegimeCompteur.NonBorne;
            }
        }

        /// <summary>
        /// [P] — les couples (A, B) des trois branches non bornées sont « à mesurer »
        /// et sans graine : la table est vide, résolue à l'envers en v0.4. Tant qu'un
        /// couple manque, la branche rend zéro point — un arbre muet, jamais deviné.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, CoupleAB> CouplesAB = new Dictionary<BrancheTechnique, CoupleAB>();

        /// <summary>[P] — tables de seuils des trois branches bornées : figées en v0.4.</summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>> SeuilsParBrancheBornee =
            new Dictionary<BrancheTechnique, IReadOnlyList<double>>();

        public static double PointsDeBranche(BrancheTechnique branche, double compteur)
        {
            if (RegimeDe(branche) == RegimeCompteur.Borne)
            {
                if (!SeuilsParBrancheBornee.TryGetValue(branche, out var seuils)) return 0;
                return seuils.Count(s => compteur >= s);
            }
            if (!CouplesAB.TryGetValue(branche, out var couple)) return 0;
            return Math.Floor(couple.A * Math.Log(1 + compteur / couple.B));
        }

        public static double Compteur(EtatJeu etat, BrancheTechnique branche) =>
            etat.Permanent.CompteursTechnique.Lire(Branches.Identifiant(branche), 0);

        public static double PointsDisponibles(EtatJeu etat, BrancheTechnique branche)
        {
            var rendus = PointsDeBranche(branche, Compteur(etat, branche));
            var depenses = NoeudsTechnique.Liste
                .Where(n => n.Branche == branche && etat.Permanent.NoeudsTechnique.Contains(n.Id))
                .Sum(n => n.Cout);
            return rendus - depenses;
        }

        /// <summary>Facteur appliqué à un terme de coût ou de confort par les nœuds acquis. Neutre = 1.</summary>
        public static double FacteurDeTechnique(EtatJeu etat, TermeDeFormule terme)
        {
            double facteur = 1;
            foreach (var noeud in NoeudsTechnique.Liste)
            {
                if (!(noeud.Effet is EffetChiffre chiffre)) continue;
                if (chiffre.Terme != terme) continue;
                if (!etat.Permanent.NoeudsTechnique.Contains(noeud.Id)) continue;
                facteur *= chiffre.Facteur;
            }
            return facteur;
        }

        /// <summary>Capacités ouvertes par l'arbre. Les succès en ouvrent d'autres, jamais les mêmes.</summary>
        public static IReadOnlyCollection<CapaciteId> CapacitesDeLArbre(EtatJeu etat)
        {
            var ouvertes = new HashSet<CapaciteId>();
            foreach (var noeud in NoeudsTechnique.Liste)
            {
                if (!(noeud.Effet is EffetVerbe verbe)) continue;
                if (!etat.Permanent.NoeudsTechnique.Contains(noeud.Id)) continue;
                ouvertes.Add(verbe.Capacite);
            }
            return ouvertes;
        }

        /// <summary>Incrémente un compteur d'usage. Le compteur ne se dépense pas : il produit des points.</summary>
        public static TableOrdonnee<double> CreditCompteur(TableOrdonnee<double> compteurs, BrancheTechnique branche, double montant)
        {
            var clef = Branches.Identifiant(branche);
            return compteurs.Avec(clef, compteurs.Lire(clef, 0) + montant);
        }
    }
}
