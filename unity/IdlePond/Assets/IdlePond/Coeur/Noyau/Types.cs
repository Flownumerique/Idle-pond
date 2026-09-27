/*
 * IdlePond — vocabulaire de l'état de jeu. Port de `src/noyau/types.ts`.
 *
 * Tier 2. Lexique : assise, palier, banc, densité, Foi, technique,
 * acclimatation, conviction. Aucun anglicisme, aucun « prestige ».
 *
 * Les unions de chaînes du TypeScript deviennent des enums, et chaque enum
 * garde son IDENTIFIANT de chaîne (`Identifiants.cs`) : c'est lui qui entre dans
 * les sauvegardes, et une save web doit se relire ici à l'identique.
 *
 * Les records sont immuables. `with` remplace le `{ ...etat, cycle: ... }` de la
 * version web, et c'est la même discipline : un réducteur pur ne mute rien.
 */
using System.Collections.Generic;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    /* ─── Termes de formule (§7.5 règle 3) ─────────────────────────────────────
     * « Aucun effet chiffré flottant. Un nœud cible toujours un TermeDeFormule
     * nommé, donc auditable dans le détail de captation. »
     *
     * La partition production / coût / confort est ce qui rend vérifiable par un
     * test qu'aucun système gratuit ne monte la production. Le C# ne sait pas
     * découper un enum en trois types comme le TypeScript découpe une union : la
     * partition vit donc dans `Termes`, et le test de canon la vérifie sur la
     * donnée.
     */
    public enum TermeDeFormule
    {
        // Production
        TauxBase,
        Effectif,
        RendementAcclimatation,
        MultiplicateurJalon,
        MultiplicateurDrapeau,
        /// <summary>Seule entrée du canal acclimaté (GDD §3.0). Peupler la dilue.</summary>
        PartMure,
        DebitAcclimate,

        // Coût
        /// <summary>Ouvrir un palier JAMAIS atteint. L'autre moitié est `ReductionTechnique`.</summary>
        CoutCreuser,
        /// <summary>Le levier de l'aménagement — GDD §6.4, seul terme qui allège un palier retraversé.</summary>
        ReductionTechnique,
        CoutPlace,
        /// <summary>
        /// Convaincre un banc. Payé par la DENSITÉ, et par elle seule (§7.1) : ce terme
        /// existe pour être NOMMÉ, jamais pour être ciblé.
        /// </summary>
        CoutReconviction,
        CoutTemple,
        CoutPortail,
        CoutReouverture,

        // Confort
        CapHorsLigne,
        DensiteConservee,
        ContenanceDeDepart,
        PlaceDeDepart,
        ChargeAllieeParReponse,
    }

    public enum FamilleDeTerme
    {
        Production,
        Cout,
        Confort,
    }

    /* ─── Capacités (§7.5 règle 1) : « une capacité a exactement une source » ───*/
    public enum CapaciteId
    {
        FileDeDescente,
        CreusementAuto,
        AchatAuto,
        AchatAutoMax,
        DeblocageAuto,
        LectureDebits,
        AutomatismesHorsLigne,
        RapportDeRetour,
        NavigationDirecte,
        LectureEau,
        RetourRapide,
    }

    public enum SourceDeCapacite
    {
        Technique,
        Succes,
    }

    /* ─── Technique (§7) ───────────────────────────────────────────────────────*/
    public enum BrancheTechnique
    {
        Creusement,
        Amelioration,
        Recrutement,
        Entretien,
        Construction,
        Eclosion,
    }

    /// <summary>
    /// §7.1 — deux régimes de compteur. `NonBorne` : points = floor(A · ln(1 + compteur / B)).
    /// `Borne` : table de seuils directe.
    /// </summary>
    public enum RegimeCompteur
    {
        NonBorne,
        Borne,
    }

    /// <summary>Un nœud chiffre cible un terme ; un nœud verbe ouvre une capacité.</summary>
    public abstract record EffetDeNoeud;

    public sealed record EffetChiffre(TermeDeFormule Terme, double Facteur) : EffetDeNoeud;

    public sealed record EffetVerbe(CapaciteId Capacite) : EffetDeNoeud;

    public sealed record NoeudTechnique(string Id, BrancheTechnique Branche, int Rang, double Cout, EffetDeNoeud Effet);

    /* ─── La voix — GDD §13.1 ──────────────────────────────────────────────────
     * Elle n'est pas portée dans l'état : elle se DÉRIVE du nombre de
     * franchissements survécus (`Voix.cs`).
     */
    public enum PalierDeVoix
    {
        Pente,
        Signes,
        Directives,
        Dialogue,
    }

    /* ─── Succès (§8) ──────────────────────────────────────────────────────────*/
    public enum FamilleDeSucces
    {
        Franchissement,
        Seuil,
        Acte,
    }

    public enum VisibiliteDeSucces
    {
        Ouvert,
        Ferme,
        Secret,
    }

    public enum QuoiDeclencheur
    {
        Eclosions,
        PaliersOuverts,
        ProfondeurMax,
        BancsConvaincus,
        EffectifDeBanc,
        EffectifDEspece,
        PlaceDeBanc,
        EffectifTotal,
        ProductionParSeconde,
        Foi,
        DensiteDePalier,
        PalierSature,
    }

    /// <summary>
    /// Le déclencheur d'un succès est toujours un SEUIL relu sur l'état de fin de
    /// tick, jamais un événement consommé au vol — sinon 480 pas de 60 s et un pas
    /// de 8 h divergeraient (§5.2).
    /// </summary>
    public sealed record DeclencheurDeSucces
    {
        public QuoiDeclencheur Quoi { get; init; }
        public double Seuil { get; init; }
        public string Banc { get; init; }
        public string Espece { get; init; }
        public int Palier { get; init; }

        public static DeclencheurDeSucces De(QuoiDeclencheur quoi, double seuil) => new DeclencheurDeSucces { Quoi = quoi, Seuil = seuil };

        public static DeclencheurDeSucces DeBanc(QuoiDeclencheur quoi, string banc, double seuil) =>
            new DeclencheurDeSucces { Quoi = quoi, Banc = banc, Seuil = seuil };

        public static DeclencheurDeSucces DEspece(string espece, double seuil) =>
            new DeclencheurDeSucces { Quoi = QuoiDeclencheur.EffectifDEspece, Espece = espece, Seuil = seuil };

        public static DeclencheurDeSucces DensiteDe(int palier, double seuil) =>
            new DeclencheurDeSucces { Quoi = QuoiDeclencheur.DensiteDePalier, Palier = palier, Seuil = seuil };

        public static DeclencheurDeSucces Sature(int palier) =>
            new DeclencheurDeSucces { Quoi = QuoiDeclencheur.PalierSature, Palier = palier };
    }

    public enum GenreDEffet
    {
        ReductionCout,
        Plafond,
        Verbe,
    }

    /// <summary>
    /// Effet d'un succès — amendement v1.1 §2.D. « JAMAIS de production. » Il n'y a
    /// donc aucun genre `Production`, et il ne faut pas en ajouter.
    /// </summary>
    public sealed record EffetDeSucces
    {
        public GenreDEffet Genre { get; init; }
        public TermeDeFormule Terme { get; init; }
        public double Part { get; init; }
        public CapaciteId Capacite { get; init; }

        public static EffetDeSucces ReductionDe(TermeDeFormule terme, double part) =>
            new EffetDeSucces { Genre = GenreDEffet.ReductionCout, Terme = terme, Part = part };

        public static EffetDeSucces PlafondDe(TermeDeFormule terme, double part) =>
            new EffetDeSucces { Genre = GenreDEffet.Plafond, Terme = terme, Part = part };

        public static EffetDeSucces VerbeDe(CapaciteId capacite) =>
            new EffetDeSucces { Genre = GenreDEffet.Verbe, Capacite = capacite };
    }

    /// <summary>
    /// Ce qu'on retient d'un succès obtenu — GDD §14.5. `Registre` FIGE la langue de
    /// l'entrée : elle n'est jamais réécrite.
    /// </summary>
    public sealed record EntreeDeSucces(int ObtenuAuCycle, PalierDeVoix Registre);

    public sealed record Succes(
        string Id,
        FamilleDeSucces Famille,
        VisibiliteDeSucces Visibilite,
        string Assise,
        DeclencheurDeSucces Declencheur,
        EffetDeSucces Effet);

    /* ─── Contenu structurel ───────────────────────────────────────────────────*/

    public sealed record Assise(string Id, int Rang, string TypeMana, int IndexPremierPalier, int NombreDePaliers);

    public sealed record Espece(string Id, string Assise);

    /// <summary>Une espèce installée sur un palier : l'unité d'achat du joueur.</summary>
    public sealed record Banc(string Id, string Espece, int Palier);

    public sealed record Palier(int Index, string Assise, IReadOnlyList<Banc> Bancs);

    /* ─── État ─────────────────────────────────────────────────────────────────*/

    /// <summary>PRNG à graine, dans l'état. Aucun hasard système dans le noyau.</summary>
    public sealed record EtatPrng(uint Graine);

    /// <summary>
    /// La PLACE achetée est le plafond de population du banc ; 0 = pas encore
    /// convaincu. L'effectif réel croît seul vers la place (§2.C).
    /// </summary>
    public sealed record EtatBanc(int Place, double Effectif);

    /// <summary>Ce que l'éclosion emporte. f = 1 : reset complet.</summary>
    public sealed record EtatCycle
    {
        public GrandNombre ManaCourant { get; init; }
        public int PaliersOuverts { get; init; }
        public TableOrdonnee<EtatBanc> Bancs { get; init; } = TableOrdonnee<EtatBanc>.Vide;
        /// <summary>Indexe le gain de densité et le gain de Foi (§6.5, §6.6).</summary>
        public GrandNombre ProductionPicParSeconde { get; init; }
        public double DureeSecondes { get; init; }
        /// <summary>Acquis de séjour, accumulation saturante vers `A∞` (§2.B). Seul moteur de la contenance.</summary>
        public double AcquisDeSejour { get; init; }
        /// <summary>Temps passé jauge pleine, sans interruption — GDD §2.4.</summary>
        public double SecondesEnSaturation { get; init; }
    }

    /// <summary>Ce que l'éclosion ne touche pas.</summary>
    public sealed record EtatPermanent
    {
        /// <summary>Charge de mana par palier. Persistante, monotone croissante.</summary>
        public IReadOnlyList<double> Densites { get; init; }
        /// <summary>Part mûre de chaque palier — GDD §3.0. Persistante, non monotone.</summary>
        public IReadOnlyList<double> PartsMures { get; init; }
        /// <summary>Rendement du héros par type de mana. Jamais repayé.</summary>
        public TableOrdonnee<double> Acclimatations { get; init; } = TableOrdonnee<double>.Vide;
        public GrandNombre Foi { get; init; }
        /// <summary>Limite le stock de mana, pas la production.</summary>
        public GrandNombre ContenanceMana { get; init; }
        public IReadOnlyList<string> Couches { get; init; }
        public int ProfondeurMaxAtteinte { get; init; }
        /// <summary>Clefs : identifiants de branche (`creusement`, …), comme la save web.</summary>
        public TableOrdonnee<double> CompteursTechnique { get; init; } = TableOrdonnee<double>.Vide;
        public IReadOnlyList<string> NoeudsTechnique { get; init; }
        /// <summary>Toujours reconstruite dans l'ordre du registre, jamais dans l'ordre d'arrivée.</summary>
        public TableOrdonnee<EntreeDeSucces> Succes { get; init; } = TableOrdonnee<EntreeDeSucces>.Vide;
        public int NombreEclosions { get; init; }
        /// <summary>Espèces ayant DÉJÀ atteint cent individus. Conservé à l'éclosion.</summary>
        public IReadOnlyList<string> EspecesAyantAtteintCent { get; init; }
        /// <summary>Le mana expire vers l'ambiant. Il n'est pas détruit (Tier 0 §5).</summary>
        public GrandNombre ManaAmbiant { get; init; }
        /// <summary>Compteur Entretien : heures effectivement créditées, jamais écoulées.</summary>
        public double HeuresHorsLigneCreditees { get; init; }
    }

    public sealed record MesureDeCycle
    {
        public int Index { get; init; }
        /// <summary>Durée ÉCOULÉE du cycle. Le temps actif est une quantité de politique.</summary>
        public double DureeEcouleeSecondes { get; init; }
        public double SecondesEnRedescente { get; init; }
        public int PaliersOuverts { get; init; }
        public GrandNombre ProductionPicParSeconde { get; init; }
        public GrandNombre FoiGagnee { get; init; }
    }

    public sealed record EtatTelemetrie
    {
        public IReadOnlyList<MesureDeCycle> Cycles { get; init; }
        public double SecondesEnRedescente { get; init; }
        public double SecondesDepuisDernierSucces { get; init; }
        public IReadOnlyList<double> IntervallesEntreSucces { get; init; }
    }

    public sealed record EtatJeu
    {
        public int VersionSave { get; init; }
        public EtatPrng Prng { get; init; }
        public double TempsJeuSecondes { get; init; }
        /// <summary>
        /// Nombre de paliers effectivement livrés — la porte de jalon, pas une valeur
        /// de canon. Un seul code, deux mondes : le jeu et le simulateur.
        /// </summary>
        public int LimiteDeContenu { get; init; }
        public EtatCycle Cycle { get; init; }
        public EtatPermanent Permanent { get; init; }
        public EtatTelemetrie Telemetrie { get; init; }
    }

    /* ─── Détail de captation (§8.2) ───────────────────────────────────────────
     * Une structure, jamais une phrase : le noyau ne fabrique aucun texte d'écran.
     */
    public enum QuoiSource
    {
        Population,
        Palier,
        Acclimatation,
        Place,
        DrapeauxPermanents,
        EauMurie,
        CanalAcclimate,
    }

    public sealed record SourceDeTerme
    {
        public QuoiSource Quoi { get; init; }
        public int Palier { get; init; }
        public string TypeMana { get; init; }
        public int Place { get; init; }
        public int Especes { get; init; }
        public double Part { get; init; }
    }

    public sealed record LigneDeCaptation(TermeDeFormule Terme, double Valeur, SourceDeTerme Source);
}
