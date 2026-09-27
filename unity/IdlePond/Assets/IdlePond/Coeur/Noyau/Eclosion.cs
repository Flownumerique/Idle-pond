/*
 * IdlePond — éclosion. Port de `src/noyau/eclosion.ts`.
 *
 * Le héros ENTRE dans l'œuf. Jamais « ponte », jamais « prestige », jamais
 * « rebirth » — ni ici, ni dans un identifiant, ni à l'écran (§3).
 *
 * §6.5 : f = 1 — reset complet du peuplement et de la géométrie.
 *   Conservé : acclimatation, densité, arbre de technique, succès, couches,
 *              contenance.
 *   Perdu    : population, paliers ouverts, mana courant.
 *   Le mana expire vers l'ambiant — il n'est pas détruit (Tier 0 §5).
 */
using System.Linq;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public static class Eclosion
    {
        /// <summary>Fraction du peuplement conservée. f = 1 ⇒ zéro : il n'y a pas de demi-vie.</summary>
        public const double FractionConservee = 1 - Constantes.FTarifRedescente;

        /// <summary>
        /// Gain de Foi prévu, indexé sur la production de pic du cycle. La Foi ne se
        /// gagne pas en attendant, elle se gagne en faisant monter le pic. [P] graine.
        /// </summary>
        public static GrandNombre GainDeFoiPrevu(EtatJeu etat)
        {
            var rapport = etat.Cycle.ProductionPicParSeconde.Div(Constantes.ProductionDeReference);
            if (rapport.Lte(1)) return GrandNombre.Zero;
            return GrandNombre.DepuisNombre(Constantes.FoiBase).Mul(GrandNombre.Pow(rapport, Constantes.FoiExposant)).Floor();
        }

        /// <summary>L'état de cycle d'un départ d'œuf. Aucun acquis permanent n'y figure.</summary>
        public static EtatCycle CycleInitial() => new EtatCycle
        {
            ManaCourant = GrandNombre.DepuisNombre(Constantes.ManaALaSortieDeLOeuf),
            PaliersOuverts = Constantes.PaliersOuvertsAuDepart,
            Bancs = TableOrdonnee<EtatBanc>.Vide,
            ProductionPicParSeconde = GrandNombre.Zero,
            DureeSecondes = 0,
            AcquisDeSejour = 0,
            SecondesEnSaturation = 0,
        };

        /// <summary>
        /// L'éclosion. `choisie` distingue les deux entrées dans l'œuf du GDD §2.4 :
        /// choisie, tout l'acquis est fixé ; non choisie, la divergence « fixe moins
        /// d'acquis ». Moins, jamais rien.
        /// </summary>
        public static EtatJeu Eclore(EtatJeu etat, bool choisie = true)
        {
            var pic = etat.Cycle.ProductionPicParSeconde;
            var foiGagnee = GainDeFoiPrevu(etat);
            var densites = Densite.AppliquerGainDeDensite(etat, etat.Cycle.PaliersOuverts, pic);

            // Le plafond ne monte QUE par séjour prolongé en mana dense (Tier 0 §8) :
            // aucun facteur n'est écrit en dur — le ×47,1 visé est un RÉSULTAT.
            var acquisFixe = choisie
                ? etat.Cycle.AcquisDeSejour
                : etat.Cycle.AcquisDeSejour * Constantes.PartDAcquisFixeeParDivergenceNonChoisie;
            var contenanceMana = etat.Permanent.ContenanceMana.Mul(1 + acquisFixe);

            var mesure = new MesureDeCycle
            {
                Index = etat.Permanent.NombreEclosions,
                DureeEcouleeSecondes = etat.Cycle.DureeSecondes,
                SecondesEnRedescente = etat.Telemetrie.SecondesEnRedescente,
                PaliersOuverts = etat.Cycle.PaliersOuverts,
                ProductionPicParSeconde = pic,
                FoiGagnee = foiGagnee,
            };

            return etat with
            {
                Cycle = CycleInitial(),
                Permanent = etat.Permanent with
                {
                    Densites = densites,
                    Foi = etat.Permanent.Foi.Add(foiGagnee),
                    ContenanceMana = contenanceMana,
                    ProfondeurMaxAtteinte = System.Math.Max(etat.Permanent.ProfondeurMaxAtteinte, etat.Cycle.PaliersOuverts),
                    NombreEclosions = etat.Permanent.NombreEclosions + 1,
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Eclosion, 1),
                    // Le mana courant expire vers l'ambiant. Il n'y a pas de puits ici.
                    ManaAmbiant = etat.Permanent.ManaAmbiant.Add(etat.Cycle.ManaCourant),
                },
                Telemetrie = etat.Telemetrie with
                {
                    Cycles = etat.Telemetrie.Cycles.Append(mesure).ToArray(),
                    SecondesEnRedescente = 0,
                },
            };
        }
    }
}
