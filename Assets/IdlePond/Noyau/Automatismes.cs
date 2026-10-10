using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — les verbes des techniques : ce qui se fait seul (spec du 2026-10-10).
    ///
    /// Un réducteur pur, appelé par la partie APRÈS chaque pas de jeu, jamais par le tick :
    /// le pas du noyau doit rester homogène (§5.2), et un achat au milieu d'un pas de 8 h
    /// ferait diverger le hors ligne d'un jeu en ligne. Les automatismes ne tournent donc
    /// qu'en jeu, pas pendant l'absence — c'est ce que le canon réservait au nœud « Le peuple
    /// continue ».
    ///
    /// Chaque appel fait AU PLUS un geste par verbe : creuser une fois, monter un cran. À
    /// dix pas par seconde, c'est assez pour suivre, et l'écran voit passer chaque achat.
    /// </summary>
    public static class Automatismes
    {
        public static EtatJeu Appliquer(EtatJeu etat)
        {
            var capacites = Economie.CapacitesDesBonus(etat);
            if (capacites.Count == 0) return etat;
            var apres = etat;
            if (capacites.Contains(CapaciteId.CreusementAuto) && !Economie.EstBloque(apres))
                apres = Reducteur.Creuser(apres);
            if (capacites.Contains(CapaciteId.AchatAuto))
                apres = MonterLeMoinsCher(apres);
            return apres;
        }

        /// <summary>
        /// « La main sûre » : monte le cran le moins cher parmi les espèces convaincues, s'il
        /// coûte moins que `PART_DU_MANA_POUR_UN_ACHAT_AUTO` du mana courant. Sans ce seuil,
        /// la main dépenserait chaque goutte à mesure qu'elle tombe, et plus rien ne
        /// s'épargnerait pour creuser, grandir ou débloquer.
        /// </summary>
        static EtatJeu MonterLeMoinsCher(EtatJeu etat)
        {
            Espece choisie = null;
            var meilleur = Decimal.Zero;
            foreach (var espece in Especes.Toutes)
            {
                if (espece.Palier >= etat.Cycle.PaliersOuverts) continue;
                if (!etat.Cycle.Especes.TryGetValue(espece.Id, out var vivante) || !vivante.Debloquee) continue;
                var cout = Economie.CoutDeNiveau(etat, espece, vivante.Niveau);
                if (choisie == null || cout.Lt(meilleur))
                {
                    choisie = espece;
                    meilleur = cout;
                }
            }
            if (choisie == null) return etat;
            if (meilleur.Gt(etat.Cycle.ManaCourant.Mul(Constantes.PART_DU_MANA_POUR_UN_ACHAT_AUTO))) return etat;
            return Reducteur.Ameliorer(etat, choisie.Id);
        }
    }
}
