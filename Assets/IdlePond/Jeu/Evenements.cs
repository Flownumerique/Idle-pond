using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu
{
    /// Ce qui vient de se passer, du point de vue de quelqu'un qui regarde.
    public enum GenreDEvenement
    {
        /// Un creux de plus est ouvert. `Valeur` : l'index du creux ouvert.
        CreuxOuvert,
        /// Une espèce vient d'être convaincue. `Sujet` : son identifiant.
        EspeceConvaincue,
        /// Une espèce a monté. `Sujet` : son identifiant ; `Valeur` : son cran d'arrivée.
        CransMontes,
        /// Un seuil de jalon vient d'être franchi. `Sujet` : l'espèce ; `Valeur` : le seuil.
        SeuilFranchi,
        /// Le héros a grandi. `Valeur` : son niveau d'arrivée.
        HerosGrandi,
        /// Un rang de bonus de lieu ou de technique. `Sujet` : l'identifiant ; `Valeur` : le rang.
        BonusAchete,
        /// Un rang d'amélioration de renaissance. `Sujet` : l'identifiant ; `Valeur` : le rang.
        AmeliorationAchetee,
        /// Un succès est tombé. `Sujet` : son identifiant.
        SuccesObtenu,
        /// L'eau vient de se troubler (85 % de la contenance).
        EauTroublee,
        /// Le stock vient de toucher la contenance.
        ContenancePleine,
        /// Le creux suivant vient de passer hors de portée : il ne reste qu'à rester ou rentrer.
        DescenteBloquee,
        /// La descente vient de se rouvrir : la contenance a rattrapé le prix du creux suivant.
        DescenteRouverte,
        /// Il est rentré dans l'œuf. `Valeur` : le nombre de renaissances.
        Renaissance,
        /// Un nœud de l'arbre de technique vient d'être appris. `Sujet` : son identifiant.
        TechniqueApprise,
    }

    /// <summary>
    /// Un événement de jeu. `Automatique` : c'est un verbe qui a agi (la main sûre, la
    /// sonde), pas le joueur — l'écran peut vouloir le montrer plus discrètement.
    /// </summary>
    public sealed record EvenementDeJeu(GenreDEvenement Genre, string Sujet = null, double Valeur = 0, bool Automatique = false);

    /// <summary>
    /// IdlePond — les événements de jeu, déduits de deux états (2026-11-13).
    ///
    /// Le noyau ne raconte rien : il rend un état. Mais l'habillage — un nombre qui jaillit,
    /// la roche qui cède, un son — a besoin de savoir CE QUI VIENT D'ARRIVER, pas seulement
    /// où l'on en est. Cette fonction compare l'état d'avant et celui d'après et nomme les
    /// différences. Elle est pure, elle n'ajoute aucune règle, et elle ne vit pas dans le
    /// noyau : ce qu'elle rend ne sert qu'à l'écran et au son.
    ///
    /// Elle tourne à chaque pas (dix fois par seconde). Les tables d'un état ne sont
    /// remplacées que par un achat : tant que la référence est la même, rien n'est parcouru.
    /// </summary>
    public static class Evenements
    {
        static readonly IReadOnlyList<EvenementDeJeu> AUCUN = new EvenementDeJeu[0];

        public static IReadOnlyList<EvenementDeJeu> Entre(EtatJeu avant, EtatJeu apres, bool automatique = false)
        {
            if (avant == null || apres == null || ReferenceEquals(avant, apres)) return AUCUN;
            List<EvenementDeJeu> liste = null;
            void Noter(GenreDEvenement genre, string sujet = null, double valeur = 0) =>
                (liste ??= new List<EvenementDeJeu>()).Add(new EvenementDeJeu(genre, sujet, valeur, automatique));

            // Une renaissance remet la vie à zéro : rien d'autre ne se compare d'une vie à l'autre.
            if (apres.Permanent.NombreDeRenaissances > avant.Permanent.NombreDeRenaissances)
            {
                Noter(GenreDEvenement.Renaissance, null, apres.Permanent.NombreDeRenaissances);
                NoterLesAmeliorations(avant, apres, Noter);
                NoterLesSucces(avant, apres, Noter);
                return liste;
            }
            // Une partie remplacée ou réinitialisée (moins de renaissances) : pas d'événement.
            if (apres.Permanent.NombreDeRenaissances < avant.Permanent.NombreDeRenaissances) return AUCUN;

            for (var palier = avant.Cycle.PaliersOuverts; palier < apres.Cycle.PaliersOuverts; palier += 1)
                Noter(GenreDEvenement.CreuxOuvert, null, palier);

            if (!ReferenceEquals(avant.Cycle.Especes, apres.Cycle.Especes))
            {
                foreach (var espece in Especes.Toutes)
                {
                    var niveauAvant = Niveau(avant, espece.Id);
                    var niveauApres = Niveau(apres, espece.Id);
                    if (niveauApres <= niveauAvant) continue;
                    if (niveauAvant == 0) Noter(GenreDEvenement.EspeceConvaincue, espece.Id, niveauApres);
                    else Noter(GenreDEvenement.CransMontes, espece.Id, niveauApres);
                    foreach (var seuil in Constantes.SEUILS_DE_JALON)
                        if (niveauAvant < seuil.Seuil && niveauApres >= seuil.Seuil)
                            Noter(GenreDEvenement.SeuilFranchi, espece.Id, seuil.Seuil);
                }
            }

            if (apres.Cycle.NiveauDuHeros > avant.Cycle.NiveauDuHeros)
                Noter(GenreDEvenement.HerosGrandi, null, apres.Cycle.NiveauDuHeros);

            if (!ReferenceEquals(avant.Cycle.Bonus, apres.Cycle.Bonus))
            {
                foreach (var bonus in RegistreDesBonus.Tous)
                {
                    var rang = Economie.RangDeBonus(apres, bonus.Id);
                    if (rang > Economie.RangDeBonus(avant, bonus.Id)) Noter(GenreDEvenement.BonusAchete, bonus.Id, rang);
                }
            }

            NoterLesAmeliorations(avant, apres, Noter);
            NoterLesSucces(avant, apres, Noter);
            if (!ReferenceEquals(avant.Permanent.NoeudsTechnique, apres.Permanent.NoeudsTechnique))
                foreach (var id in apres.Permanent.NoeudsTechnique)
                    if (!avant.Permanent.NoeudsTechnique.Contains(id)) Noter(GenreDEvenement.TechniqueApprise, id);

            if (!Economie.EauTroublee(avant) && Economie.EauTroublee(apres)) Noter(GenreDEvenement.EauTroublee);
            if (!Economie.EstSature(avant) && Economie.EstSature(apres)) Noter(GenreDEvenement.ContenancePleine);
            var bloqueAvant = Economie.EstBloque(avant);
            var bloqueApres = Economie.EstBloque(apres);
            if (!bloqueAvant && bloqueApres) Noter(GenreDEvenement.DescenteBloquee);
            if (bloqueAvant && !bloqueApres) Noter(GenreDEvenement.DescenteRouverte);

            return liste ?? AUCUN;
        }

        delegate void Note(GenreDEvenement genre, string sujet = null, double valeur = 0);

        static void NoterLesAmeliorations(EtatJeu avant, EtatJeu apres, Note noter)
        {
            if (ReferenceEquals(avant.Permanent.AmeliorationsDeRenaissance, apres.Permanent.AmeliorationsDeRenaissance)) return;
            foreach (var amelioration in AmeliorationsDeRenaissance.Toutes)
            {
                var rang = Economie.RangDAmelioration(apres, amelioration.Id);
                if (rang > Economie.RangDAmelioration(avant, amelioration.Id))
                    noter(GenreDEvenement.AmeliorationAchetee, amelioration.Id, rang);
            }
        }

        static void NoterLesSucces(EtatJeu avant, EtatJeu apres, Note noter)
        {
            if (ReferenceEquals(avant.Permanent.Succes, apres.Permanent.Succes)) return;
            if (apres.Permanent.Succes.Count == avant.Permanent.Succes.Count) return;
            foreach (var id in apres.Permanent.Succes.Keys)
                if (!avant.Permanent.Succes.ContainsKey(id)) noter(GenreDEvenement.SuccesObtenu, id);
        }

        static int Niveau(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee ? vivante.Niveau : 0;
    }
}
