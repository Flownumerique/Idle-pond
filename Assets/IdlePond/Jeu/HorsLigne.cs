using IdlePond.Noyau;

namespace IdlePond.Jeu
{
    public sealed record RetourDeHorsLigne(EtatJeu Etat, double SecondesCreditees);

    /// <summary>
    /// IdlePond — crédit du temps hors ligne.
    ///
    /// « Le calcul est un appel unique à Tick avec un grand `dt` — c'est précisément ce
    /// que garantit le filtre du §5.2 » (§10). Il n'y a donc rien à rattraper, aucune
    /// boucle de rattrapage, aucune approximation : le même réducteur, un seul pas.
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

        public static RetourDeHorsLigne Crediter(EtatJeu etat, long dernierInstantMs, long maintenantMs)
        {
            var secondes = Horloge.SecondesHorsLigneCreditees(dernierInstantMs, maintenantMs, CapHorsLigneCourantHeures(etat));
            if (secondes <= 0) return new RetourDeHorsLigne(etat, 0);

            var avance = Reducteur.TickDetaille(etat, secondes).Etat;
            return new RetourDeHorsLigne(
                avance with
                {
                    Permanent = avance.Permanent with
                    {
                        // Le compteur Entretien lit les heures CRÉDITÉES, jamais le temps
                        // écoulé. Sans ça, avancer son horloge farme l'arbre permanent — et
                        // c'est la seule protection anti-triche nécessaire dans tout le jeu.
                        HeuresHorsLigneCreditees = avance.Permanent.HeuresHorsLigneCreditees + secondes / 3600,
                    },
                },
                secondes);
        }
    }
}
