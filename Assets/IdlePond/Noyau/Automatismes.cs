using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — les verbes de l'arbre de technique : ce qui se fait seul.
    ///
    /// Un réducteur pur, appelé par la partie APRÈS chaque pas de jeu, jamais par le tick :
    /// le pas du noyau doit rester homogène (§5.2), et un achat au milieu d'un pas de 8 h
    /// ferait diverger le hors ligne d'un jeu en ligne. Pendant l'absence ils ne tournent
    /// donc pas — sauf si le nœud « Le peuple continue » est appris, et alors ils jouent
    /// AU RETOUR, sur le stock que l'absence a laissé (`Jeu/HorsLigne.cs`).
    ///
    /// Chaque appel fait AU PLUS un geste par verbe (un lot, pour « Tout le monde suit »).
    /// À dix pas par seconde, c'est assez pour suivre, et l'écran voit passer chaque achat.
    ///
    /// Depuis le 2026-11-13, tous les verbes viennent de l'arbre : la main sûre et la sonde,
    /// d'abord vendues au mana dans l'onglet « Débloquer », y sont passées (décision D2).
    /// Une capacité a exactement une source.
    ///
    /// Aucun verbe ne fait de cadeau : chaque geste passe par le même réducteur que celui
    /// du joueur et paie le même prix.
    /// </summary>
    public static class Automatismes
    {
        public static EtatJeu Appliquer(EtatJeu etat)
        {
            var capacites = Capacites(etat);
            if (capacites.Count == 0) return etat;
            // « Le chemin connu » refait tout ce qu'on savait déjà faire : il vaut les
            // quatre autres verbes de redescente, et rachète en plus les bonus.
            var retour = capacites.Contains(CapaciteId.RetourRapide);
            var apres = etat;

            // Ce que le joueur a demandé passe avant ce qui se fait seul.
            if (capacites.Contains(CapaciteId.FileDeDescente))
                apres = CreuserUnCreuxDemande(apres);
            // D'abord reconvaincre : c'est ce qui remet la production en route.
            if (retour || capacites.Contains(CapaciteId.DeblocageAuto))
                apres = ReconvaincreUneEspeceConnue(apres);
            if (retour || capacites.Contains(CapaciteId.CreusementAuto))
                apres = RouvrirUneGalerieConnue(apres);
            if (capacites.Contains(CapaciteId.AchatAutoMax))
                apres = MonterVersLeSeuil(apres);
            else if (retour || capacites.Contains(CapaciteId.AchatAuto))
                apres = MonterLeMoinsCher(apres);
            if (retour)
                apres = RacheterUnBonus(apres);
            return apres;
        }

        /// <summary>
        /// « La file » (Creusement) : un creux demandé d'avance se creuse dès que le mana le
        /// paie. Un par appel. Toute la roche ouverte, la file se vide d'elle-même.
        /// </summary>
        public static EtatJeu CreuserUnCreuxDemande(EtatJeu etat)
        {
            var demandes = etat.Cycle.CreuxDemandes;
            if (demandes <= 0) return etat;
            if (Economie.ToutEstCreuse(etat)) return etat with { Cycle = etat.Cycle with { CreuxDemandes = 0 } };
            var creuse = Reducteur.Creuser(etat);
            if (ReferenceEquals(creuse, etat)) return etat;
            return creuse with { Cycle = creuse.Cycle with { CreuxDemandes = demandes - 1 } };
        }

        /// Les verbes ouverts : ceux de l'arbre, et ceux qu'un bonus porterait (aucun aujourd'hui).
        public static IReadOnlyCollection<CapaciteId> Capacites(EtatJeu etat)
        {
            var deLArbre = Technique.CapacitesDeLArbre(etat);
            var desBonus = Economie.CapacitesDesBonus(etat);
            if (desBonus.Count == 0) return deLArbre;
            var toutes = new HashSet<CapaciteId>(deLArbre);
            toutes.UnionWith(desBonus);
            return toutes;
        }

        /// <summary>
        /// Connu : le palier porte la densité d'une vie passée. « Le monde se souvient de la
        /// densité » — une renaissance n'en laisse que sur les paliers qu'elle avait ouverts,
        /// donc un palier chargé est un palier où l'on est déjà allé. Ce qu'on atteint pour
        /// la première fois se fait toujours à la main : la découverte reste un geste.
        /// </summary>
        public static bool PalierConnu(EtatJeu etat, int palier) =>
            palier >= 0 && palier < etat.Permanent.Densites.Count && etat.Permanent.Densites[palier] > 0;

        public static bool EstConnue(EtatJeu etat, Espece espece) => PalierConnu(etat, espece.Palier);

        /// « Les habitudes tiennent » (Recrutement) : une espèce déjà connue revient seule
        /// dès qu'elle est à portée et payable. Une par appel.
        static EtatJeu ReconvaincreUneEspeceConnue(EtatJeu etat)
        {
            foreach (var espece in Especes.Toutes)
            {
                if (espece.Palier >= etat.Cycle.PaliersOuverts) continue;
                if (etat.Cycle.Especes.TryGetValue(espece.Id, out var vivante) && vivante.Debloquee) continue;
                if (!EstConnue(etat, espece)) continue;
                // La moins profonde d'abord ; si elle n'est pas payable, les suivantes ne le sont pas non plus.
                return Reducteur.Debloquer(etat, espece.Id);
            }
            return etat;
        }

        /// « Galeries connues » (Creusement) : le palier suivant se rouvre seul s'il a déjà
        /// été ouvert dans une vie passée. Le premier palier neuf arrête la descente.
        static EtatJeu RouvrirUneGalerieConnue(EtatJeu etat)
        {
            if (Economie.EstBloque(etat)) return etat;
            if (!PalierConnu(etat, etat.Cycle.PaliersOuverts)) return etat;
            return Reducteur.Creuser(etat);
        }

        /// L'espèce convaincue dont le prochain cran est le moins cher, ou null.
        static Espece LaMoinsChereAMonter(EtatJeu etat, out Decimal cout)
        {
            Espece choisie = null;
            cout = Decimal.Zero;
            foreach (var espece in Especes.Toutes)
            {
                if (espece.Palier >= etat.Cycle.PaliersOuverts) continue;
                if (!etat.Cycle.Especes.TryGetValue(espece.Id, out var vivante) || !vivante.Debloquee) continue;
                var prix = Economie.CoutDeNiveau(etat, espece, vivante.Niveau);
                if (choisie == null || prix.Lt(cout))
                {
                    choisie = espece;
                    cout = prix;
                }
            }
            return choisie;
        }

        /// <summary>
        /// « La main sûre » (Amélioration) : monte le cran le moins cher parmi les espèces
        /// convaincues, s'il coûte moins que `PART_DU_MANA_POUR_UN_ACHAT_AUTO` du mana
        /// courant. Sans ce seuil, la main dépenserait chaque goutte à mesure qu'elle tombe,
        /// et plus rien ne s'épargnerait pour creuser, grandir ou convaincre.
        /// </summary>
        static EtatJeu MonterLeMoinsCher(EtatJeu etat)
        {
            var choisie = LaMoinsChereAMonter(etat, out var cout);
            if (choisie == null) return etat;
            if (cout.Gt(etat.Cycle.ManaCourant.Mul(Constantes.PART_DU_MANA_POUR_UN_ACHAT_AUTO))) return etat;
            return Reducteur.Ameliorer(etat, choisie.Id);
        }

        /// <summary>
        /// « Tout le monde suit » (Amélioration) : la même main, mais elle vise le prochain
        /// seuil — autant de crans de l'espèce la moins chère que la même part du mana en
        /// paie, jusqu'au seuil. Elle remplace la main sûre, elle ne s'y ajoute pas.
        /// </summary>
        static EtatJeu MonterVersLeSeuil(EtatJeu etat)
        {
            var choisie = LaMoinsChereAMonter(etat, out _);
            if (choisie == null) return etat;
            var budget = etat.Cycle.ManaCourant.Mul(Constantes.PART_DU_MANA_POUR_UN_ACHAT_AUTO);
            var cible = Economie.ProchainSeuilDeNiveau(etat.Cycle.Especes[choisie.Id].Niveau);
            var courant = etat;
            for (var niveau = etat.Cycle.Especes[choisie.Id].Niveau; niveau < cible; niveau += 1)
            {
                var prix = Economie.CoutDeNiveau(courant, choisie, niveau);
                if (prix.Gt(budget)) break;
                var suivant = Reducteur.Ameliorer(courant, choisie.Id);
                if (ReferenceEquals(suivant, courant)) break;
                budget = budget.Sub(prix);
                courant = suivant;
            }
            return courant;
        }

        /// <summary>
        /// « Le chemin connu » (Renaissance) rachète aussi les bonus de lieu et les techniques
        /// au mana : le rang ouvert le moins cher, s'il coûte moins que la même part du mana.
        /// </summary>
        static EtatJeu RacheterUnBonus(EtatJeu etat)
        {
            var plafond = etat.Cycle.ManaCourant.Mul(Constantes.PART_DU_MANA_POUR_UN_ACHAT_AUTO);
            Bonus choisi = null;
            var meilleur = Decimal.Zero;
            foreach (var bonus in RegistreDesBonus.Tous)
            {
                if (!Economie.BonusOuvert(etat, bonus) || Economie.BonusAuMaximum(etat, bonus)) continue;
                var cout = Economie.CoutDeBonus(etat, bonus);
                if (cout.Gt(plafond)) continue;
                if (choisi == null || cout.Lt(meilleur))
                {
                    choisi = bonus;
                    meilleur = cout;
                }
            }
            return choisi == null ? etat : Reducteur.AcheterUnBonus(etat, choisi.Id);
        }
    }
}
