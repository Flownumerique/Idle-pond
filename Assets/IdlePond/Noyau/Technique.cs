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
        /// <summary>
        /// POSÉS le 2026-11-13, par le calcul et non par le calibreur — des graines, donc.
        /// `points = floor(A · ln(1 + compteur / B))`, et un arbre entier coûte 167 points
        /// (5 + 12 + 25 + 45 + 80).
        ///
        ///   CREUSEMENT (3,8 ; 60). Le mana dépensé à creuser est dominé par le palier le
        ///   plus profond jamais ouvert : `≈ 60 × 2,4^p`, donc `ln ≈ 0,875 p` avec `B = 60`,
        ///   le prix du premier palier. `A = 3,8` met le dernier nœud vers le palier 50
        ///   (assise V, comme le veut le §6.1), les autres vers les paliers 2, 5, 13 et 26.
        ///   Ce compteur ne se farme pas : recreuser les mêmes paliers n'ajoute presque rien.
        ///
        ///   AMÉLIORATION (4,3 ; 20 000), réglé le 2026-11-14 sur la mesure. Le mana dépensé
        ///   en crans suit la production, donc la densité : il grossit d'environ ×50 par vie,
        ///   même à profondeur égale (4e6 à la fin de la vie 1, 6e20 à la fin de la vie 9).
        ///   Deux ancres : la main sûre au milieu de la première vie (~1e6 de mana dépensé),
        ///   et le dernier nœud vers 1,5e21, donc à la vie 10, en assise V — le §6.1 veut que
        ///   le joueur finisse par tout avoir, « mais pas avant l'assise V ». Le premier
        ///   couple (1,9 ; 140) visait 1e40 pour le dernier nœud : quinze vies n'en rendaient
        ///   que 125 points sur 167, et « Tout le monde suit » ne tombait jamais.
        ///
        ///   ENTRETIEN (84 ; 100). Compteur en heures créditées : nœuds vers 6 h, 23 h, 65 h,
        ///   184 h et 640 h d'absence comptée.
        ///
        /// `MesureDeLArbreTests` dit à quelle vie chaque nœud tombe réellement au simulateur.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, (double A, double B)> COUPLES_A_B =
            new Dictionary<BrancheTechnique, (double A, double B)>
            {
                [BrancheTechnique.Creusement] = (3.8, 60),
                [BrancheTechnique.Amelioration] = (4.3, 20000),
                [BrancheTechnique.Entretien] = (84, 100),
            };

        /// <summary>
        /// [P] graine — le calendrier d'une branche bornée : la valeur du compteur à laquelle
        /// le nœud de chaque rang devient acquis. C'est la table `verbe → moment d'ouverture`
        /// du §7.2, écrite directement dans l'unité du compteur.
        ///
        /// RECRUTEMENT (2026-11-13) — compteur : espèces convaincues, toutes vies confondues.
        /// Sur les 18 paliers livrés, une vie en convainc 4, puis 5, puis 6 :
        ///
        ///   3  — pendant la première vie : le premier nœud tombe tôt, pour qu'on voie
        ///        l'arbre exister ;
        ///   6  — au début de la deuxième vie, après avoir reconvaincu deux espèces à la
        ///        main : c'est le verbe, il arrive quand la corvée vient d'être sentie ;
        ///   12, 20, 30 — vers les troisième, quatrième et sixième vies.
        ///
        /// Ni le simulateur ni une vraie partie n'ont réglé ces cinq nombres.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>> CALENDRIER_PAR_BRANCHE_BORNEE =
            new Dictionary<BrancheTechnique, IReadOnlyList<double>>
            {
                [BrancheTechnique.Recrutement] = new double[] { 3, 6, 12, 20, 30 },
                // RENAISSANCE — compteur : renaissances. Lire l'eau à la deuxième, quand les
                // signes sont déjà là ; le chemin connu à la neuvième, pour que les dernières
                // vies de la descente soient indolores (§6.2, « le capstone du jeu »).
                [BrancheTechnique.Renaissance] = new double[] { 1, 2, 4, 6, 9 },
            };

        /// <summary>
        /// [P] — tables de seuils des trois branches bornées : figées en v0.4, quand le
        /// calendrier des verbes est arrêté. Sur un compteur qui plafonne, « encore 2
        /// renaissances » est lisible et exact là où le logarithme n'ajoute qu'une barre
        /// opaque.
        /// </summary>
        public static readonly IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>> SEUILS_PAR_BRANCHE_BORNEE =
            ConstruireLesSeuils();

        /// <summary>
        /// Les seuils d'une branche bornée se déduisent de son calendrier : le nœud de rang
        /// `i` coûte `COUTS_DE_NOEUD[i]` points, donc la valeur de compteur qui l'ouvre
        /// figure autant de fois dans la table. Les points restent ceux du §6.1 — par
        /// branche, non fongibles —, et « encore 2 espèces » reste lisible et exact.
        /// </summary>
        static IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<double>> ConstruireLesSeuils()
        {
            var seuils = new Dictionary<BrancheTechnique, IReadOnlyList<double>>();
            foreach (var paire in CALENDRIER_PAR_BRANCHE_BORNEE)
            {
                var table = new List<double>();
                for (var rang = 0; rang < paire.Value.Count && rang < Constantes.COUTS_DE_NOEUD.Count; rang += 1)
                    for (var point = 0; point < Constantes.COUTS_DE_NOEUD[rang]; point += 1)
                        table.Add(paire.Value[rang]);
                seuils[paire.Key] = table;
            }
            return seuils;
        }

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
            var rendus = PointsDeBranche(branche, CompteurDe(etat, branche));
            return rendus - PointsDepenses(etat, branche);
        }

        /// <summary>
        /// Facteur appliqué à un terme de coût ou de confort par les nœuds acquis.
        /// Neutre = 1. Un nœud « −20 % » porte un facteur 0.8, jamais un « +12 % »
        /// flottant sans terme nommé (§7.5 règle 3).
        /// </summary>
        public static double FacteurDeTechnique(EtatJeu etat, TermeDeFormule terme)
        {
            // Appelée à chaque calcul de prix : on parcourt ce qui est ACQUIS (souvent rien),
            // pas le registre. La liste acquise est dans l'ordre du registre (`Apprendre`),
            // donc le produit se fait toujours dans le même ordre.
            var acquis = etat.Permanent.NoeudsTechnique;
            var facteur = 1.0;
            for (var i = 0; i < acquis.Count; i += 1)
            {
                var noeud = NoeudsTechnique.ParId(acquis[i]);
                if (noeud == null || noeud.Effet.Nature != NatureDEffet.Chiffre) continue;
                if (noeud.Effet.Terme != terme) continue;
                facteur *= noeud.Effet.Facteur;
            }
            return facteur;
        }

        /// Capacités ouvertes par l'arbre. Les succès en ouvrent d'autres, jamais les mêmes.
        static readonly IReadOnlyCollection<CapaciteId> AUCUNE_CAPACITE = new HashSet<CapaciteId>();

        public static IReadOnlyCollection<CapaciteId> CapacitesDeLArbre(EtatJeu etat)
        {
            var acquis = etat.Permanent.NoeudsTechnique;
            if (acquis.Count == 0) return AUCUNE_CAPACITE;
            HashSet<CapaciteId> ouvertes = null;
            for (var i = 0; i < acquis.Count; i += 1)
            {
                var noeud = NoeudsTechnique.ParId(acquis[i]);
                if (noeud == null || noeud.Effet.Nature != NatureDEffet.Verbe) continue;
                (ouvertes ??= new HashSet<CapaciteId>()).Add(noeud.Effet.Capacite.Value);
            }
            return ouvertes ?? AUCUNE_CAPACITE;
        }

        /// Le nœud suivant d'une branche : le premier, dans l'ordre des rangs, qui n'est pas acquis.
        public static NoeudTechnique ProchainNoeud(EtatJeu etat, BrancheTechnique branche)
        {
            // Appelée à chaque achat : une boucle nue, sans allocation.
            var acquis = etat.Permanent.NoeudsTechnique;
            foreach (var noeud in NoeudsTechnique.DeLaBranche(branche))
            {
                var su = false;
                for (var i = 0; i < acquis.Count; i += 1)
                    if (acquis[i] == noeud.Id) { su = true; break; }
                if (!su) return noeud;
            }
            return null;
        }

        /// <summary>
        /// Le compteur d'usage d'une branche. Celui d'Entretien est `HeuresHorsLigneCreditees` :
        /// les heures CRÉDITÉES, jamais le temps écoulé (§6.2) — il vit déjà dans l'état
        /// permanent, et le recopier dans la table des compteurs en ferait deux vérités.
        /// </summary>
        public static double CompteurDe(EtatJeu etat, BrancheTechnique branche)
        {
            if (branche == BrancheTechnique.Entretien) return etat.Permanent.HeuresHorsLigneCreditees;
            return etat.Permanent.CompteursTechnique.TryGetValue(branche, out var c) ? c : 0;
        }

        /// Les points déjà dépensés dans une branche : le coût de ses nœuds acquis.
        static int PointsDepenses(EtatJeu etat, BrancheTechnique branche)
        {
            var depenses = 0;
            var acquis = etat.Permanent.NoeudsTechnique;
            foreach (var noeud in NoeudsTechnique.DeLaBranche(branche))
                for (var i = 0; i < acquis.Count; i += 1)
                    if (acquis[i] == noeud.Id) { depenses += noeud.Cout; break; }
            return depenses;
        }

        /// <summary>
        /// Ce qu'il manque au compteur d'une branche bornée pour que son prochain nœud soit
        /// acquis — « encore 2 espèces ». Null s'il n'y a plus de nœud, ou pas de calendrier.
        /// </summary>
        public static double? ResteAvantLeProchainNoeud(EtatJeu etat, BrancheTechnique branche)
        {
            var prochain = ProchainNoeud(etat, branche);
            if (prochain == null) return null;
            if (!CALENDRIER_PAR_BRANCHE_BORNEE.TryGetValue(branche, out var calendrier)) return null;
            if (prochain.Rang < 1 || prochain.Rang > calendrier.Count) return null;
            return Math.Max(0, calendrier[prochain.Rang - 1] - CompteurDe(etat, branche));
        }

        /// <summary>
        /// La technique s'apprend par l'usage, elle ne s'achète pas (GDD §11.2) : dès que les
        /// points d'une branche paient son prochain nœud, il est acquis, sans geste du joueur.
        /// Dans une branche, l'ordre est celui des rangs et les points ne servent qu'à elle :
        /// il n'y a rien à choisir, donc rien à demander.
        ///
        /// Appelée par le réducteur là où un compteur vient de monter. Rien à apprendre :
        /// l'état revient inchangé, la même référence. La liste est reconstruite dans l'ordre
        /// du registre — deux parties qui savent les mêmes choses se sérialisent pareil.
        /// </summary>
        public static EtatJeu Apprendre(EtatJeu etat, BrancheTechnique branche)
        {
            var courant = etat;
            while (true)
            {
                var prochain = ProchainNoeud(courant, branche);
                if (prochain == null || PointsDisponibles(courant, branche) < prochain.Cout) return courant;
                var acquis = courant.Permanent.NoeudsTechnique;
                var connus = NoeudsTechnique.Tous.Where(n => n.Id == prochain.Id || acquis.Contains(n.Id)).Select(n => n.Id);
                // Un identifiant que le registre ne connaît pas (une save plus récente) est gardé, en tête.
                var inconnus = acquis.Where(id => NoeudsTechnique.Tous.All(n => n.Id != id));
                courant = courant with
                {
                    Permanent = courant.Permanent with { NoeudsTechnique = inconnus.Concat(connus).ToArray() },
                };
            }
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
