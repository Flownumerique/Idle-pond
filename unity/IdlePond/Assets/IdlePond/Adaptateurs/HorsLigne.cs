/*
 * IdlePond — crédit du temps hors ligne. Port de `src/adaptateurs/hors-ligne.ts`.
 *
 * « Le calcul est un appel unique à `tick` avec un grand `dt` » (§10). Il n'y a
 * rien à rattraper, aucune boucle de rattrapage, aucune approximation : le même
 * réducteur, un seul pas.
 */
using IdlePond.Noyau;

namespace IdlePond.Adaptateurs
{
    public sealed record RetourDeHorsLigne(EtatJeu Etat, double SecondesCreditees);

    public static class HorsLigne
    {
        /// <summary>Plafond courant, en heures. La branche Entretien le pousse de 6 h à 24 h.</summary>
        public static double CapHorsLigneCourantHeures(EtatJeu etat) =>
            Constantes.CapHorsLigneHeuresInitial * Technique.FacteurDeTechnique(etat, TermeDeFormule.CapHorsLigne);

        public static RetourDeHorsLigne CrediterHorsLigne(EtatJeu etat, double dernierInstantMs, double maintenantMs)
        {
            var secondes = Horloge.SecondesHorsLigneCreditees(dernierInstantMs, maintenantMs, CapHorsLigneCourantHeures(etat));
            if (secondes <= 0) return new RetourDeHorsLigne(etat, 0);

            var avance = Reducteur.TickDetaille(etat, secondes).Etat;
            return new RetourDeHorsLigne(
                avance with
                {
                    Permanent = avance.Permanent with
                    {
                        // Le compteur Entretien lit les heures CRÉDITÉES, jamais le
                        // temps écoulé : sans ça, avancer son horloge farme l'arbre.
                        HeuresHorsLigneCreditees = avance.Permanent.HeuresHorsLigneCreditees + secondes / 3600,
                    },
                },
                secondes);
        }
    }
}
