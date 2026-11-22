using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Jeu
{
    /// `Declenches` : les succès tombés pendant l'absence, pour qu'ils soient annoncés au retour.
    public sealed record RetourDeHorsLigne(EtatJeu Etat, double SecondesCreditees, IReadOnlyList<string> Declenches = null);

    /// <summary>
    /// IdlePond — crédit du temps hors ligne.
    ///
    /// « Le calcul est un appel unique à Tick avec un grand `dt` — c'est précisément ce
    /// que garantit le filtre du §5.2 » (§10). Il n'y a donc rien à rattraper, aucune
    /// boucle de rattrapage, aucune approximation : le même réducteur, un seul pas.
    ///
    /// Absent, le mana monte mais le séjour ne bouge pas (2026-11-15) : la mare se remplit
    /// jusqu'à ce qu'on portait en partant. Pour porter plus, il faut être là.
    ///
    /// On ne punit jamais l'absence. Le plafond borne ce que l'absence RAPPORTE ; il ne
    /// retire rien, et rien ne se dégrade pendant qu'on n'est pas là.
    /// </summary>
    public static class HorsLigne
    {
        /// Plafond courant, en heures. La technique « Patience » le pousse, jusqu'au maximum
        /// que `Horloge` garde de toute façon.
        public static double CapHorsLigneCourantHeures(EtatJeu etat) =>
            Constantes.CAP_HORS_LIGNE_HEURES_INITIAL * Technique.FacteurDeTechnique(etat, TermeDeFormule.CapHorsLigne)
            * Economie.FacteurDeBonus(etat, TermeDeFormule.CapHorsLigne, null);

        /// Garde-fou : un retour ne fait pas plus de gestes que ça. Les prix montent, la boucle
        /// s'arrête d'elle-même bien avant.
        const int GESTES_MAXIMUM_AU_RETOUR = 20_000;

        /// <summary>
        /// « Le peuple continue » (arbre, branche Entretien) : les verbes ont tourné pendant
        /// l'absence. Ils jouent ICI, après le pas unique, sur le stock que l'absence a laissé
        /// — jamais au milieu du pas, qui doit rester homogène (§5.2). Ce n'est donc pas ce
        /// qu'aurait rendu une présence continue (rien ne s'est composé en route) : c'est ce
        /// que le peuple a fait de ce qui s'est amassé. Sans le nœud, l'état revient inchangé.
        /// </summary>
        public static EtatJeu LePeupleContinue(EtatJeu etat)
        {
            if (!Technique.CapacitesDeLArbre(etat).Contains(CapaciteId.AutomatismesHorsLigne)) return etat;
            var courant = etat;
            for (var geste = 0; geste < GESTES_MAXIMUM_AU_RETOUR; geste += 1)
            {
                var suivant = Automatismes.Appliquer(courant);
                if (ReferenceEquals(suivant, courant)) break;
                courant = suivant;
            }
            return courant;
        }

        /// <summary>
        /// « La file » : les creux demandés avant de partir se sont creusés pendant l'absence,
        /// sur ce qu'elle a amassé — sans « Le peuple continue » : c'était une demande du
        /// joueur, pas un verbe qui tourne seul.
        /// </summary>
        public static EtatJeu LaFileSeVide(EtatJeu etat)
        {
            var courant = etat;
            while (courant.Cycle.CreuxDemandes > 0)
            {
                var suivant = Automatismes.CreuserUnCreuxDemande(courant);
                if (ReferenceEquals(suivant, courant)) break;
                courant = suivant;
            }
            return courant;
        }

        public static RetourDeHorsLigne Crediter(EtatJeu etat, long dernierInstantMs, long maintenantMs)
        {
            var secondes = Horloge.SecondesHorsLigneCreditees(dernierInstantMs, maintenantMs, CapHorsLigneCourantHeures(etat));
            if (secondes <= 0) return new RetourDeHorsLigne(etat, 0);

            var resultat = Reducteur.TickDetaille(etat, secondes, absent: true);
            var avance = resultat.Etat;
            var credite = avance with
            {
                Permanent = avance.Permanent with
                {
                    // Le compteur Entretien lit les heures CRÉDITÉES, jamais le temps
                    // écoulé. Sans ça, avancer son horloge farme l'arbre permanent — et
                    // c'est la seule protection anti-triche nécessaire dans tout le jeu.
                    HeuresHorsLigneCreditees = avance.Permanent.HeuresHorsLigneCreditees + secondes / 3600,
                },
            };
            // S'absenter apprend à s'absenter : la branche Entretien de l'arbre.
            credite = Technique.Apprendre(credite, BrancheTechnique.Entretien);
            return new RetourDeHorsLigne(LaFileSeVide(LePeupleContinue(credite)), secondes, resultat.Declenches);
        }
    }
}
