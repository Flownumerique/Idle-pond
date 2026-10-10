using System.Collections.Generic;

namespace IdlePond.Noyau
{
    /* ─── Termes de formule (§7.5 règle 3) ─────────────────────────────────
     * Un seul enum ; la partition production / coût / confort vit dans Termes.cs
     * et un test la vérifie, là où TypeScript la portait par trois types. */
    public enum TermeDeFormule
    {
        // production
        TauxBase, Niveau, MultiplicateurJalon, MultiplicateurDrapeau, MultiplicateurProfondeur,
        MultiplicateurDensite, DebitHeros, MultiplicateurHeros, MultiplicateurAmelioration, AmeliorationGlobale,
        // coût
        CoutCreuser, CoutNiveau, CoutDeblocage, CoutCroissance, CoutAmelioration, CoutTemple, CoutPortail, CoutReouverture,
        // confort
        CapHorsLigne, DensiteConservee, ContenanceDeDepart, NiveauDeDepart, ChargeAllieeParReponse,
        // production, ajouté le 2026-10-10 : les bonus de lieu. En queue d'énumération pour ne
        // décaler aucune valeur existante ; la partition vit dans Termes.cs, pas dans l'ordre.
        MultiplicateurDeLieu,
    }

    public enum CapaciteId
    {
        FileDeDescente, CreusementAuto, AchatAuto, AchatAutoMax, DeblocageAuto, LectureDebits,
        AutomatismesHorsLigne, RapportDeRetour, NavigationDirecte, LectureEau, RetourRapide,
    }

    public enum SourceDeCapacite { Technique, Succes }

    public enum BrancheTechnique { Creusement, Amelioration, Recrutement, Entretien, Construction, Renaissance }

    public enum RegimeCompteur { NonBorne, Borne }

    public enum NatureDEffet { Chiffre, Verbe }

    /// Un nœud chiffre cible un terme de coût ou de confort ; un nœud verbe ouvre une capacité.
    public sealed record EffetDeNoeud(NatureDEffet Nature, TermeDeFormule? Terme, double Facteur, CapaciteId? Capacite)
    {
        public static EffetDeNoeud Chiffre(TermeDeFormule terme, double facteur) => new(NatureDEffet.Chiffre, terme, facteur, null);
        public static EffetDeNoeud Verbe(CapaciteId capacite) => new(NatureDEffet.Verbe, null, 0, capacite);
    }

    public sealed record NoeudTechnique(string Id, BrancheTechnique Branche, int Rang, int Cout, EffetDeNoeud Effet);

    public enum PalierDeVoix { Pente, Signes, Directives, Dialogue }

    /* ─── Succès (§8) ─────────────────────────────────────────────────────── */

    public enum FamilleDeSucces { Franchissement, Seuil, Acte }
    public enum VisibiliteDeSucces { Ouvert, Ferme, Secret }

    public enum QuoiDeclencheur
    {
        Renaissances, PaliersOuverts, ProfondeurMax, EspecesDebloquees, NiveauDEspece,
        NiveauxCumules, ProductionParSeconde, Souffle, DensiteDePalier, PalierAuComplet,
    }

    /// Toujours un SEUIL relu sur l'état de fin de tick, jamais un événement
    /// consommé au vol (voir types.ts). `Espece` et `Palier` ne valent que pour
    /// les déclencheurs qui les nomment.
    public sealed record DeclencheurDeSucces(QuoiDeclencheur Quoi, double Seuil = 0, string Espece = null, int Palier = -1);

    public enum GenreDEffetDeSucces { ReductionCout, Plafond, Verbe }

    /// Amendement v1.1 §2.D : JAMAIS de production. Aucun genre ne la porte.
    public sealed record EffetDeSucces(GenreDEffetDeSucces Genre, TermeDeFormule? Terme, double Part, CapaciteId? Capacite)
    {
        public static EffetDeSucces ReductionCout(TermeDeFormule terme, double part) => new(GenreDEffetDeSucces.ReductionCout, terme, part, null);
        public static EffetDeSucces Plafond(TermeDeFormule terme, double part) => new(GenreDEffetDeSucces.Plafond, terme, part, null);
        public static EffetDeSucces Verbe(CapaciteId capacite) => new(GenreDEffetDeSucces.Verbe, null, 0, capacite);
    }

    /// §14.5 : `Registre` FIGE la langue de l'entrée, jamais réécrite.
    public sealed record EntreeDeSucces(int ObtenuAuCycle, PalierDeVoix Registre);

    public sealed record Succes(string Id, FamilleDeSucces Famille, VisibiliteDeSucces Visibilite, string Assise,
        DeclencheurDeSucces Declencheur, EffetDeSucces Effet);

    /* ─── Contenu structurel ──────────────────────────────────────────────── */

    public sealed record Assise(string Id, int Rang, string TypeMana, int IndexPremierPalier, int NombreDePaliers);
    public sealed record Espece(string Id, string Assise, int Rang, int Palier);
    public sealed record Palier(int Index, string Assise, string Espece);

    public enum PorteeDAmelioration { Ciblee, Globale }

    /// Noyau v1.0 §4.2 : la ciblée MULTIPLIE une espèce, la globale ADDITIONNE
    /// au débit de base de toutes. `Espece` est null pour la globale.
    public sealed record AmeliorationDeRenaissance(string Id, PorteeDAmelioration Portee, string Espece);

    /* ─── Bonus de lieu et techniques (spec du 2026-10-10) ────────────────── */

    /// <summary>
    /// Ce qu'un bonus fait, par rang acheté.
    ///   ReductionDeCout — le coût du terme est multiplié par `(1 − part) ^ rang` ;
    ///   Production      — le débit des espèces du lieu est multiplié par `(1 + part) ^ rang` ;
    ///   Confort         — le plafond du terme est multiplié par `1 + part × rang` ;
    ///   Verbe           — une capacité s'ouvre au premier rang.
    /// </summary>
    public enum GenreDeBonus { ReductionDeCout, Production, Confort, Verbe }

    /// <summary>
    /// Un achat de l'onglet « Débloquer », payé en MANA et perdu à la renaissance comme les
    /// quatre autres achats au mana.
    ///
    /// `Assise` nomme le lieu dont il ne touche que les espèces et les paliers ; null, c'est
    /// une TECHNIQUE, qui vaut partout. La ligne du canon (CanonTests) : seul un bonus de lieu
    /// monte une production ; une technique baisse un coût, relève un plafond ou ouvre un verbe.
    ///
    /// `MaitriseRequise` : pour un bonus de lieu, les paliers ouverts DANS le lieu ; pour une
    /// technique, les paliers ouverts en tout. `PalierDePrix` ancre le prix sur le coût de ce
    /// palier, pour qu'un bonus profond coûte ce que coûte sa profondeur.
    /// </summary>
    public sealed record Bonus(
        string Id, string Assise, GenreDeBonus Genre, TermeDeFormule? Terme, double Part,
        int RangMax, int MaitriseRequise, int PalierDePrix, CapaciteId? Capacite = null);

    /* ─── État ────────────────────────────────────────────────────────────── */

    public sealed record EtatPrng(uint Graine);

    public sealed record EtatEspece(bool Debloquee, int Niveau);

    public sealed record EtatCycle(
        Decimal ManaCourant,
        int PaliersOuverts,
        IReadOnlyDictionary<string, EtatEspece> Especes,
        Decimal ProductionPicParSeconde,
        double DureeSecondes,
        double AcquisDeSejour,
        int NiveauDuHeros,
        IReadOnlyDictionary<string, int> Bonus);

    public sealed record EtatPermanent(
        IReadOnlyList<double> Densites,
        Decimal Souffle,
        Decimal ContenanceMana,
        IReadOnlyList<string> Couches,
        int ProfondeurMaxAtteinte,
        IReadOnlyDictionary<BrancheTechnique, double> CompteursTechnique,
        IReadOnlyList<string> NoeudsTechnique,
        IReadOnlyDictionary<string, EntreeDeSucces> Succes,
        int NombreDeRenaissances,
        IReadOnlyList<string> EspecesAyantAtteintCent,
        Decimal ManaAmbiant,
        double HeuresHorsLigneCreditees,
        IReadOnlyDictionary<string, int> AmeliorationsDeRenaissance);

    public sealed record MesureDeCycle(int Index, double DureeEcouleeSecondes, int PaliersOuverts,
        Decimal ProductionPicParSeconde, Decimal SouffleGagne);

    public sealed record EtatTelemetrie(IReadOnlyList<MesureDeCycle> Cycles, double SecondesDepuisDernierSucces,
        IReadOnlyList<double> IntervallesEntreSucces);

    /// Les boutons de la courbe. Jamais persistés (R41) : une propriété de la
    /// VERSION du jeu, pas de la partie.
    public sealed record Reglage(double CroissanceDuSejourParPalier);

    public sealed record EtatJeu(
        int VersionSave,
        EtatPrng Prng,
        double TempsJeuSecondes,
        int LimiteDeContenu,
        Reglage Reglage,
        EtatCycle Cycle,
        EtatPermanent Permanent,
        EtatTelemetrie Telemetrie);

    /* ─── Détail de captation (§8.2) ──────────────────────────────────────── */

    public enum QuoiSource { Niveau, Palier, DrapeauxPermanents, Profondeur, Densite, Heros, AmeliorationDeRenaissance, BonusDeLieu }

    /// Une structure, jamais une phrase : le noyau ne fabrique aucun texte d'écran.
    /// `Valeur` est le niveau, le palier, le nombre d'espèces, les paliers ouverts,
    /// la densité ou le rang (la somme des rangs pour les bonus de lieu), selon `Quoi`.
    public sealed record SourceDeTerme(QuoiSource Quoi, double Valeur);

    public sealed record LigneDeCaptation(TermeDeFormule Terme, double Valeur, SourceDeTerme Source);

    public sealed record SeuilDeJalon(int Seuil, double MultiplicateurCumule);
}
