using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — renaissance.
    ///
    /// Le héros ENTRE dans l'œuf. Jamais « ponte », jamais « prestige », jamais
    /// « rebirth » — ni ici, ni dans un identifiant, ni à l'écran (§3).
    ///
    /// §6.5, et rien de plus :
    ///   f = 1 — reset complet du peuplement et de la géométrie, aucune fraction
    ///           conservée.
    ///   Conservé : densité, arbre de technique, succès, couches, contenance, et le
    ///              drapeau des cent — l'unique exception.
    ///   Perdu    : espèces débloquées et leurs niveaux, paliers ouverts, mana
    ///              courant, bonus de lieu et techniques (payés en mana).
    ///   Le mana expire vers l'ambiant — il n'est pas détruit (Tier 0 §5).
    /// </summary>
    public static class Renaissance
    {
        /// <summary>
        /// Gain de Souffle prévu, indexé sur la production de pic du cycle.
        ///
        /// C'est ce que le nœud « Lire l'eau » affichera en permanence, et c'est le
        /// versant « rester pour le Souffle » de la seule vraie décision du joueur : le
        /// Souffle ne se gagne pas en attendant, il se gagne en faisant monter le pic.
        ///
        /// [P] graine — le barème n'est fixé par aucun document. À réfuter en v0.3.
        /// </summary>
        public static Decimal GainDeSoufflePrevu(EtatJeu etat)
        {
            var rapport = etat.Cycle.ProductionPicParSeconde.Div(Constantes.PRODUCTION_DE_REFERENCE);
            if (rapport.Lte(1)) return new Decimal(0);
            return new Decimal(Constantes.SOUFFLE_BASE).Mul(Decimal.Pow(rapport, Constantes.SOUFFLE_EXPOSANT)).Floor();
        }

        /// <summary>
        /// L'état de cycle d'un départ d'œuf. Aucun acquis permanent n'y figure.
        ///
        /// Le mana courant part chargé — `MANA_A_LA_SORTIE_DE_L_OEUF` — et pas de
        /// zéro : la tâche 9 a mesuré que `DEBIT_HEROS` seul ne peut pas tenir cette
        /// place (voir son commentaire dans `Constantes.cs`). Les deux mécanismes
        /// coexistent délibérément.
        /// </summary>
        public static EtatCycle CycleInitial() => new EtatCycle(
            ManaCourant: new Decimal(Constantes.MANA_A_LA_SORTIE_DE_L_OEUF),
            PaliersOuverts: Constantes.PALIERS_OUVERTS_AU_DEPART,
            Especes: new Dictionary<string, EtatEspece>(),
            ProductionPicParSeconde: new Decimal(0),
            DureeSecondes: 0,
            AcquisDeSejour: 0,
            NiveauDuHeros: Constantes.NIVEAU_DU_HEROS_AU_DEPART,
            // Les bonus de lieu et les techniques se paient en mana : ils se perdent avec lui.
            Bonus: new Dictionary<string, int>());

        /// <summary>
        /// Les couches du corps — GDD §15.1, « une marque par assise fixée ».
        ///
        /// Toute assise dont le premier palier a été ouvert dans cette vie laisse sa
        /// marque. Dans l'ordre des assises, jamais dans l'ordre de l'obtention : la
        /// divergence « se lit comme une somme d'histoire », et une somme n'a pas
        /// d'ordre — mais la save, elle, compare des chaînes.
        /// </summary>
        public static IReadOnlyList<string> CouchesApres(EtatJeu etat, int paliersOuverts)
        {
            var acquises = new HashSet<string>(etat.Permanent.Couches);
            foreach (var assise in Assises.Toutes)
                if (assise.IndexPremierPalier < paliersOuverts) acquises.Add(assise.Id);
            return Assises.Toutes.Where(a => acquises.Contains(a.Id)).Select(a => a.Id).ToArray();
        }

        /// <summary>
        /// La renaissance.
        ///
        /// Le seul geste volontaire du jeu (§10.1) : toute renaissance est choisie, et
        /// tout l'acquis du cycle est fixé. Le noyau v1.0 §2.2 le pose sans détour :
        /// « le blocage est doux, il peut continuer à jouer indéfiniment ». Rester
        /// jauge pleine ne fait plus renaître à sa place.
        /// </summary>
        public static EtatJeu Renaitre(EtatJeu etat)
        {
            var pic = etat.Cycle.ProductionPicParSeconde;
            var souffleGagne = GainDeSoufflePrevu(etat);
            var densites = Densite.AppliquerGain(etat, etat.Cycle.PaliersOuverts, pic);

            // Le plafond ne monte QUE par séjour prolongé en mana dense (Tier 0 §8) :
            // l'acquis accumulé pendant le cycle se dépense ici, et nulle part ailleurs.
            // « Dense » n'agit plus sur l'acquis, dont le temps vaut `τ₀` constant : il
            // agit sur la production, par le multiplicateur de densité.
            // Aucun facteur n'est écrit en dur — le ×47,1 visé est un RÉSULTAT de
            // `A∞` et `τ₀`, pas une ligne de code (§2.B).
            var contenanceMana = Economie.Contenance(etat);

            var cycles = new List<MesureDeCycle>(etat.Telemetrie.Cycles)
            {
                new MesureDeCycle(
                    Index: etat.Permanent.NombreDeRenaissances,
                    DureeEcouleeSecondes: etat.Cycle.DureeSecondes,
                    PaliersOuverts: etat.Cycle.PaliersOuverts,
                    ProductionPicParSeconde: pic,
                    SouffleGagne: souffleGagne),
            };

            return etat with
            {
                Cycle = CycleInitial(),
                Permanent = etat.Permanent with
                {
                    Densites = densites,
                    Couches = CouchesApres(etat, etat.Cycle.PaliersOuverts),
                    Souffle = etat.Permanent.Souffle.Add(souffleGagne),
                    ContenanceMana = contenanceMana,
                    ProfondeurMaxAtteinte = Math.Max(etat.Permanent.ProfondeurMaxAtteinte, etat.Cycle.PaliersOuverts),
                    NombreDeRenaissances = etat.Permanent.NombreDeRenaissances + 1,
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Renaissance, 1),
                    // Le mana courant expire vers l'ambiant. Aucun système d'IdlePond ne se
                    // comporte comme un puits : il n'y a pas de machine non vivante ici.
                    ManaAmbiant = etat.Permanent.ManaAmbiant.Add(etat.Cycle.ManaCourant),
                },
                Telemetrie = etat.Telemetrie with { Cycles = cycles },
            };
        }
    }
}
