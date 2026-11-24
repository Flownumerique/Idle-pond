using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>Ce que l'écran dit d'une amélioration — un verbe, ce que ça fait.</summary>
    public sealed record TexteDAmelioration(string Nom, string Effet);

    public sealed record TexteDeSucces(string Nom, string Condition, string Rapport);

    /// Une tribu dans la bibliothèque (Codex v1.4, « Les six tribus à l'écran ») : son nom, son
    /// représentant, ses mœurs telles qu'on les voit dans la mare, et son texte.
    public sealed record TexteDeTribu(string Tribu, string Representant, string Moeurs, string Citation);

    /// <summary>
    /// Ce que l'écran dit d'un bonus de lieu ou d'une technique : un nom, et ce que fait un
    /// rang. `{0}` reçoit la part d'un rang, en pour cent, que `Format` écrit.
    /// </summary>
    public sealed record TexteDeBonus(string Nom, string Effet);

    /// <summary>
    /// IdlePond — TOUS les textes affichés, et ils sont PROVISOIRES.
    ///
    /// ╔════════════════════════════════════════════════════════════════════════╗
    /// ║  Les NOMS nommés au canon le restent (amendement v1.1 §2.E) : le lieu, ║
    /// ║  le vairon, la loche, l'épinoche et l'espèce réservée. L'épinoche a    ║
    /// ║  suivi la répartition 2/4/4/4/4/3 en tête de l'assise II ; son nom ne  ║
    /// ║  bouge pas pour autant, il est justifié au canon.                      ║
    /// ║                                                                        ║
    /// ║  Les PHRASES, elles, restent provisoires : le jalon v0.2 demande       ║
    /// ║  « aucun texte définitif ». Elles vivent ici, à un seul endroit, et    ║
    /// ║  aucune n'est reprise dans un identifiant, une clef de save ou une     ║
    /// ║  donnée — les réécrire ne touche aucune autre ligne du dépôt et        ║
    /// ║  n'invalide aucune sauvegarde.                                         ║
    /// ║                                                                        ║
    /// ║  [P] P3 reste ouvert pour les assises II à VI.                         ║
    /// ╚════════════════════════════════════════════════════════════════════════╝
    ///
    /// La charte phonétique du §2.E est une jauge de profondeur : le joueur entend
    /// qu'il descend avant de le lire. L'assise I n'invente rien — voyelles claires,
    /// lexique réel du français d'eau douce. Le monde n'est pas exotique, il est
    /// VIEUX.
    ///
    /// Règle d'écriture, elle non provisoire (§8.3) : le narrateur rapporte ce qui
    /// est arrivé et ce que ça a changé. Il n'explique JAMAIS pourquoi le monde
    /// fonctionne ainsi. Une seule phrase qui livre la vraie physique annule le
    /// principe « nommé sans être compris » et grille le retournement d'échelle de
    /// fin de partie.
    /// </summary>
    public static class Textes
    {
        /// Le nom propre du lieu. L'UI n'affiche jamais « Assise I » (§3).
        public static readonly IReadOnlyDictionary<string, string> NOM_DES_ASSISES = new Dictionary<string, string>
        {
            ["noue"] = "la Noue",
            ["gour"] = "le Gour",
        };

        public static readonly IReadOnlyDictionary<string, string> NOM_DES_ESPECES = new Dictionary<string, string>
        {
            ["vairon"] = "le vairon",
            ["loche"] = "la loche",
            ["epinoche"] = "l’épinoche",
            ["chabot"] = "le chabot",
            ["lamproie"] = "la lamproie",
            ["ombre"] = "l’ombre",
        };

        /// Les six tribus livrées, d'après le Codex v1.4 (§2) — mot pour mot pour leur texte.
        public static readonly IReadOnlyDictionary<string, TexteDeTribu> TEXTES_DES_TRIBUS = new Dictionary<string, TexteDeTribu>
        {
            ["vairon"] = new("le Chœur des vairons", "le Coryphée",
                "Ils nagent à l’unisson ; à chaque seuil, une vague de rouge les parcourt.",
                "Cent corps, une seule voix. Quand ils chantent, ils changent de couleur."),
            ["loche"] = new("les Loches devineresses", "la Doyenne",
                "Elles s’agitent avant que l’eau se trouble, et avant que la descente se rouvre.",
                "Elles savent avant l’eau. On vient les voir quand on a peur."),
            ["epinoche"] = new("la Garde des épinoches", "le Capitaine de la Garde",
                "À chaque seuil, un nid-forteresse d’herbes pousse au fond de leur eau.",
                "Elles bâtissent avant de se battre, et se battent pour ce qu’elles ont bâti."),
            ["chabot"] = new("les Chabots-gardiens", "le Vieux Gardien",
                "Tant que l’eau est trouble, ils deviennent pierre.",
                "Chaque pierre a le sien. Menacé, il devient la pierre."),
            ["lamproie"] = new("les Lamproies pèlerines", "la Mère pèlerine",
                "Accrochées aux parois, elles reçoivent un fil de mana de chaque tribu qui monte.",
                "Elles remontent tout ce qui descend, et boivent un peu de chacun. On les craint, on les écoute."),
            ["ombre"] = new("les Ombres-hérauts", "le Grand Héraut",
                "Quand une tribu est convaincue, une ombre laisse son mirage.",
                "On les voit passer, jamais arriver. Leur voile dit ce qui vient d’avoir lieu."),
        };

        public static TexteDeTribu DeLaTribu(string espece) =>
            TEXTES_DES_TRIBUS.TryGetValue(espece, out var texte) ? texte : null;

        /// <summary>
        /// Ce que l'écran dit d'une amélioration — un verbe, ce que ça fait. La globale
        /// a un nom à elle ; une ciblée prend le nom de son espèce.
        /// </summary>
        public static readonly TexteDAmelioration AMELIORATION_GLOBALE = new("Améliorer l’eau", "tout ce qui vit ici capte un peu plus, et tout ce qui viendra");

        public static readonly TexteDAmelioration AMELIORATION_CIBLEE = new(null, "ils te donnent moitié plus, à chaque fois");

        /// Les nœuds de l'arbre de technique : ce que le héros sait, pour toujours.
        static readonly IReadOnlyDictionary<string, TexteDAmelioration> NOEUDS = new Dictionary<string, TexteDAmelioration>
        {
            ["creusement-etaiement"] = new("L’étaiement", "creuser te coûte moins"),
            ["creusement-drainage"] = new("Le drainage", "creuser te coûte encore moins"),
            ["creusement-file"] = new("La file", "tu peux demander plusieurs creux d’avance"),
            ["creusement-percement"] = new("Le percement guidé", "creuser te coûte presque un tiers de moins"),
            ["creusement-galeries"] = new("Galeries connues", "la roche que tu as déjà ouverte dans une autre vie se rouvre seule"),
            ["amelioration-repetition"] = new("La répétition", "monter d’un cran te coûte moins"),
            ["amelioration-main-sure"] = new("La main sûre", "le cran le moins cher monte seul, sans vider ta réserve"),
            ["amelioration-geste"] = new("Le geste appris", "monter d’un cran te coûte encore moins"),
            ["amelioration-elan"] = new("L’élan", "ceux que tu convaincs arrivent déjà nombreux"),
            ["amelioration-suite"] = new("Tout le monde suit", "les crans montent seuls jusqu’au prochain seuil"),
            ["recrutement-approche"] = new("L’approche", "convaincre te coûte moins"),
            ["recrutement-habitudes"] = new("Les habitudes tiennent", "ceux que tu as déjà convaincus reviennent seuls, dès que tu peux payer"),
            ["recrutement-memoire"] = new("La mémoire des gestes", "convaincre te coûte encore moins"),
            ["recrutement-reputation"] = new("La réputation", "monter d’un cran te coûte un peu moins"),
            ["recrutement-refuge"] = new("Le refuge est connu", "convaincre te coûte moitié prix"),
            ["entretien-releve"] = new("La relève", "ton absence compte deux fois plus longtemps"),
            ["entretien-veille"] = new("La veille", "ton absence compte encore deux fois plus longtemps"),
            ["entretien-compte"] = new("Compter les siens", "tu vois ce que chacun te donne"),
            ["entretien-peuple"] = new("Le peuple continue", "ce qui se fait seul se fait aussi quand tu n’es pas là"),
            ["entretien-rapport"] = new("Ce qui s’est passé pendant", "à ton retour, tu sais tout ce qui a bougé"),
            ["construction-fondations"] = new("Les fondations", "bâtir te coûte moins"),
            ["construction-tunnels"] = new("Les tunnels", "relier deux eaux te coûte moins"),
            ["construction-signaletique"] = new("La signalétique", "tu vas droit où tu veux"),
            ["construction-reouverture"] = new("La réouverture", "rouvrir un passage te coûte moitié prix"),
            ["construction-collecteurs"] = new("Les collecteurs", "ce qu’on te répond charge mieux l’eau"),
            ["renaissance-heure"] = new("Rentrer à l’heure", "l’eau garde davantage de ce que tu as porté"),
            ["renaissance-lire-l-eau"] = new("Lire l’eau", "tu vois toujours ce que rentrer te vaudrait"),
            ["renaissance-ce-qui-reste"] = new("Ce qui reste", "l’eau se souvient un peu mieux de toi"),
            ["renaissance-coquille"] = new("Coquille épaisse", "tu peux porter deux fois plus"),
            ["renaissance-chemin-connu"] = new("Le chemin connu", "tout ce que tu avais refait se refait seul"),
        };

        /// Lance si le nœud n'a pas de texte : un nœud sans nom ne doit pas arriver à l'écran.
        public static TexteDAmelioration DuNoeud(string id) =>
            NOEUDS.TryGetValue(id, out var texte) ? texte : throw new System.InvalidOperationException($"Aucun texte pour le nœud {id}");

        /// L'amélioration de séjour (2026-11-13) : la seule qui raccourcit une vie.
        public static readonly TexteDAmelioration AMELIORATION_DE_SEJOUR = new("Apprendre à porter", "tu te fais à l’eau plus vite : ce que tu peux porter monte plus tôt");

        /// L'amélioration du toucher (spec du 2026-10-11) : actif, le héros est un courant
        /// dans l'eau — il prend (Codex).
        public static readonly TexteDAmelioration AMELIORATION_DU_TOUCHER = new("Prendre plus fort", "quand tu touches l’eau, tu emportes davantage, et tu t’y fais plus vite");

        /// Les bonus de l'onglet « Débloquer » (spec du 2026-10-10), par identifiant de registre.
        static readonly IReadOnlyDictionary<string, TexteDeBonus> TEXTES_DE_BONUS = new Dictionary<string, TexteDeBonus>
        {
            ["noue-vase"] = new("Vase meuble", "creuser la Noue coûte {0} % de moins, par rang"),
            ["noue-herbier"] = new("Herbier", "ce qui vit dans la Noue te donne {0} % de plus, par rang"),
            ["noue-racines"] = new("Racines", "convaincre dans la Noue coûte {0} % de moins, par rang"),
            ["noue-eau-calme"] = new("Eau calme", "monter d’un cran dans la Noue coûte {0} % de moins, par rang"),
            ["gour-roche-tendre"] = new("Roche tendre", "creuser le Gour coûte {0} % de moins, par rang"),
            ["gour-courant"] = new("Courant porteur", "ce qui vit dans le Gour te donne {0} % de plus, par rang"),
            ["gour-abris"] = new("Abris", "convaincre dans le Gour coûte {0} % de moins, par rang"),
            ["gour-vasques"] = new("Vasques", "monter d’un cran dans le Gour coûte {0} % de moins, par rang"),
            ["technique-pelle"] = new("Pelle de nacre", "creuser coûte {0} % de moins, par rang"),
            ["technique-geste"] = new("Geste sûr", "monter d’un cran coûte {0} % de moins, par rang"),
            ["technique-approche"] = new("Approche", "convaincre coûte {0} % de moins, par rang"),
            ["technique-croissance"] = new("Mue facile", "grandir coûte {0} % de moins, par rang"),
            ["technique-patience"] = new("Patience", "ton absence compte {0} % plus longtemps, par rang"),
        };

        public static readonly TexteDeBonus BONUS_INCONNU = new("—", "—");

        public static TexteDeBonus DuBonus(string id) =>
            id != null && TEXTES_DE_BONUS.TryGetValue(id, out var texte) ? texte : BONUS_INCONNU;

        static readonly IReadOnlyDictionary<string, TexteDeSucces> TEXTES_DE_SUCCES = new Dictionary<string, TexteDeSucces>
        {
            /* — Actes ——————————————————————————————————————————————————————————————— */
            ["acte-premiere-conviction"] = new(
                "Un banc te suit",
                "Convaincre un premier banc",
                "Ils sont venus sans qu’on les appelle deux fois."),
            ["acte-deuxieme-niveau"] = new(
                "Ils reviennent",
                "Monter un banc d’un cran",
                "Un de plus s’est joint sans qu’on insiste."),
            ["acte-cinquieme-niveau"] = new(
                "Le geste prend",
                "Monter un banc de cinq crans",
                "Ça va plus vite qu’au début."),
            ["acte-premier-banc-de-cinq"] = new(
                "Trois à demeure",
                "Trois crans tenus dans la Noue",
                "Ils ne repartent plus entre deux passages."),
            ["acte-premier-creusement"] = new(
                "La roche cède",
                "Creuser une fois plus bas",
                "Le fond s’est ouvert. Il y avait de la place dessous."),
            ["acte-deux-bancs"] = new(
                "Ils sont deux",
                "Convaincre deux bancs dans la même vie",
                "Le second n’a pas fui en voyant le premier."),
            ["acte-trois-bancs"] = new(
                "La Noue répond",
                "Monter la loche de dix crans",
                "On ne peut plus les compter d’un seul regard."),
            ["acte-premier-palier-sature"] = new(
                "Plein à ras",
                "Mener un banc jusqu’à cent",
                "Ce creux ne prend plus personne. Il faudra descendre."),
            ["acte-dixieme-niveau"] = new(
                "La main est faite",
                "Monter un banc de dix crans",
                "Le geste se répète tout seul, maintenant."),

            /* — Seuils, engendrés par gabarit ——————————————————————————————————————— */
            ["seuil-vairon-10"] = new("Dix vairons", "Dixième cran", "Ils tiennent le banc ensemble."),
            ["seuil-vairon-25"] = new("Vingt-cinq vairons", "Vingt-cinquième cran", "Le banc vire d’un seul tenant."),
            ["seuil-vairon-50"] = new("Cinquante vairons", "Cinquantième cran", "On entend le courant qu’ils font."),
            ["seuil-vairon-100"] = new("Cent vairons", "Centième cran", "Ils ne repartiront plus. Jamais."),
            ["seuil-loche-10"] = new("Dix loches", "Dixième cran", "Le fond est remué par en dessous."),
            ["seuil-loche-25"] = new("Vingt-cinq loches", "Vingt-cinquième cran", "Elles ouvrent des passages qu’on n’a pas creusés."),
            ["seuil-loche-50"] = new("Cinquante loches", "Cinquantième cran", "La vase ne tient plus en place."),
            ["seuil-loche-100"] = new("Cent loches", "Centième cran", "Elles ne repartiront plus. Jamais."),
            ["seuil-epinoche-10"] = new("Dix épinoches", "Dixième cran", "Elles tiennent là où l’eau se charge."),
            ["seuil-epinoche-25"] = new("Vingt-cinq épinoches", "Vingt-cinquième cran", "Rien ne les déloge du bord."),
            ["seuil-epinoche-50"] = new("Cinquante épinoches", "Cinquantième cran", "L’eau lourde ne leur fait plus rien."),
            ["seuil-epinoche-100"] = new("Cent épinoches", "Centième cran", "Elles ne repartiront plus. Jamais."),
            ["seuil-chabot-10"] = new("Dix chabots", "Dixième cran", "Chaque pierre a le sien."),
            ["seuil-chabot-25"] = new("Vingt-cinq chabots", "Vingt-cinquième cran", "Le courant passe au-dessus d’eux sans les prendre."),
            ["seuil-chabot-50"] = new("Cinquante chabots", "Cinquantième cran", "Le fond des galeries est tenu, pierre à pierre."),
            ["seuil-chabot-100"] = new("Cent chabots", "Centième cran", "Ils ne repartiront plus. Jamais."),
            ["seuil-lamproie-10"] = new("Dix lamproies", "Dixième cran", "Elles s’accrochent à la roche, et elles attendent."),
            ["seuil-lamproie-25"] = new("Vingt-cinq lamproies", "Vingt-cinquième cran", "Le courant tire. Elles ne lâchent rien."),
            ["seuil-lamproie-50"] = new("Cinquante lamproies", "Cinquantième cran", "Les parois portent leurs marques."),
            ["seuil-lamproie-100"] = new("Cent lamproies", "Centième cran", "Elles ne repartiront plus. Jamais."),
            ["seuil-ombre-10"] = new("Dix ombres", "Dixième cran", "On les voit passer, jamais arriver."),
            ["seuil-ombre-25"] = new("Vingt-cinq ombres", "Vingt-cinquième cran", "Elles remontent le courant sans effort."),
            ["seuil-ombre-50"] = new("Cinquante ombres", "Cinquantième cran", "Le noir des galeries a pris leur couleur."),
            ["seuil-ombre-100"] = new("Cent ombres", "Centième cran", "Elles ne repartiront plus. Jamais."),

            /* — Franchissements ————————————————————————————————————————————————————— */
            ["franchissement-premiere-eclosion"] = new(
                "Rentrer",
                "Renaître une première fois",
                "Tout est resté en bas. Presque tout."),
            ["franchissement-deuxieme-eclosion"] = new(
                "Rentrer encore",
                "Renaître deux fois",
                "La descente a été plus courte que la première."),
            ["franchissement-troisieme-eclosion"] = new(
                "Le chemin se sait",
                "Renaître trois fois",
                "Les mêmes fonds, et plus vite."),
            ["franchissement-densite"] = new(
                "La Noue est chargée",
                "Charger le premier creux",
                "Ce qui a été porté là y est resté."),
            ["franchissement-fond-de-la-mare"] = new(
                "Le fond de la Noue",
                "Ouvrir la Noue jusqu’au fond",
                // Réécrit le 2026-10-07 : sous la Noue, il y a le Gour.
                "La Noue s’arrête là. En dessous, l’eau court."),
            ["franchissement-fond-du-gour"] = new(
                "Le fond du Gour",
                "Ouvrir le Gour jusqu’au fond",
                "Le courant ne mène plus nulle part."),
        };

        /// <summary>
        /// Les familles engendrées par gabarit ont leur texte engendré aussi.
        ///
        /// Écrire à la main quarante entrées qui ne diffèrent que par un nombre serait
        /// une invitation à la faute de copie, et le §8.1 range explicitement ces
        /// familles dans le « généré par gabarit ». Les actes et les franchissements,
        /// eux, restent écrits un par un : ils sont « à la main » dans le même §8.1.
        /// </summary>
        /// <summary>Le rapport change avec le rang : deux entrées voisines ne se lisent jamais pareil.</summary>
        static T Tour<T>(IReadOnlyList<T> liste, int n) => liste[n % liste.Count];

        static readonly IReadOnlyList<string> RAPPORTS_DE_MARE = new[]
        {
            "On ne les compte plus d’un seul regard.",
            "Il y a du monde jusque dans les recoins.",
            "L’eau bouge toute seule, maintenant.",
            "Le fond ne se voit plus à travers eux.",
            "Ça tient sans qu’on s’en occupe.",
        };

        static readonly IReadOnlyList<string> RAPPORTS_DE_PROFONDEUR = new[]
        {
            "Le fond est plus loin qu’à la dernière descente.",
            "La lumière ne descend plus jusqu’ici.",
            "L’eau est froide, et plus lourde.",
        };

        static readonly IReadOnlyList<(Regex Motif, System.Func<int, TexteDeSucces> Texte)> GABARITS = new (Regex, System.Func<int, TexteDeSucces>)[]
        {
            (new Regex(@"^seuil-mare-(\d+)$", RegexOptions.CultureInvariant), n => new TexteDeSucces(
                $"{n} dans la Noue",
                $"{n} crans tenus, tous bancs confondus",
                Tour(RAPPORTS_DE_MARE, n / 20))),

            (new Regex(@"^seuil-profondeur-(\d+)$", RegexOptions.CultureInvariant), n => new TexteDeSucces(
                $"{n} creusements",
                $"Ouvrir {n} fois plus bas dans la même vie",
                Tour(RAPPORTS_DE_PROFONDEUR, n))),

            (new Regex(@"^seuil-palier-sature-(\d+)$", RegexOptions.CultureInvariant), _ => new TexteDeSucces(
                "Un creux de plus est plein",
                "Un creux qui ne prendra plus personne",
                "Celui-là ne prendra plus personne.")),
        };

        /// <summary>
        /// Les chaînes de l'interface : tout ce que les panneaux écrivent hors des noms, des
        /// améliorations et des succès ci-dessus. Elles vivent ici, au même endroit que les
        /// autres, parce que la règle est la même — provisoires, jamais dans un identifiant
        /// ni dans une sauvegarde, et balayées par `LexiqueTests` contre les mots morts et
        /// les mots interdits à l'écran (Codex §5).
        ///
        /// `{0}`, `{1}` : les trous que `Format` remplit. Une phrase à trous reste une
        /// phrase entière, qu'on relit d'un bloc ; la couper en morceaux collés par le code
        /// l'écrirait en dur dans la grammaire.
        /// </summary>
        public static class Ecran
        {
            /* — En-tête et retour ————————————————————————————————————————————————— */
            public const string TITRE = "IdlePond";
            public const string SOUFFLE = "Souffle";
            public const string RETOURS_DANS_L_OEUF = "retours dans l’œuf";
            public const string RETOUR_ABSENCE = "{0} a tourné sans toi pendant {1}.";
            /// Le détail du retour (2026-11-13) : une ligne par chose qui a bougé.
            public const string RETOUR_PLAFONNE = "Tu es resté loin {0} ; au-delà de {1}, l’eau ne compte plus.";
            public const string RETOUR_MANA = "+{0} de mana";
            public const string RETOUR_CONTENANCE = "Tu peux porter {0} de plus.";
            public const string RETOUR_STOCK_PLEIN = "Tu es plein : le reste s’est perdu dans l’eau. Reste auprès d’elle pour porter plus.";
            public const string RETOUR_DESCENTE_ROUVERTE = "La roche d’en dessous est de nouveau à ta portée.";
            public const string RETOUR_PEUT_CREUSER = "Tu as de quoi creuser plus bas.";
            public const string RETOUR_SUCCES = "{0} choses consignées au journal.";
            public const string RETOUR_UN_SUCCES = "Une chose consignée au journal.";
            public const string RETOUR_FERMER = "toucher pour reprendre";

            /* — Contenance ———————————————————————————————————————————————————————— */
            public const string DEBIT = "+{0} / s";
            public const string DONT_DEBIT = "dont +{0} / s";
            public const string SUR_LA_CONTENANCE = "sur {0}";
            public const string CAPTATION_ARRETEE = "tu ne captes plus rien";
            public const string PLUS_DE_QUOI_PORTER = "Il n’y a plus de quoi porter le prochain creusement. Rien n’empêche de continuer à faire venir du monde.";

            /* — Le héros ———————————————————————————————————————————————————————— */
            public const string HEROS = "toi";
            public const string ALEVIN = "alevin";
            public const string GRANDI = "grandi {0} fois";
            public const string BONUS_DU_HEROS = "et tout ce que tu convaincs donne +{0} %";
            public const string GRANDIR = "Grandir";
            public const string PAR_TOUCHER_AU_PROCHAIN = "+{0} par toucher en grandissant";
            /// Le chiffre qui s'envole quand on touche la mare.
            public const string GAIN_DU_TOUCHER = "+{0}";
            /// Mare pleine : ce que le toucher a ajouté à ce qu'on peut porter (2026-11-14).
            public const string GAIN_A_PORTER = "+{0} à porter";
            /// Sous « Par toucher », dans la fiche : les secondes d'eau qu'un toucher fait gagner.
            public const string SEJOUR_PAR_TOUCHER = "+{0} s à te faire à l’eau";
            /// « Lire l'eau » : à côté du Souffle, ce que rentrer rapporterait.
            public const string SOUFFLE_EN_RENTRANT = "+{0} de Souffle en rentrant";

            /* — La captation ———————————————————————————————————————————————————— */
            public const string FERMER = "fermer";
            public const string CE_QU_ILS_TE_DONNENT = "ce qu’ils te donnent";
            public const string PAR_SECONDE = "par seconde";

            /* — La mare ————————————————————————————————————————————————————————— */
            public const string ESPECE_QUI_S_ATTARDE = "un banc s’attarde";
            public const string VERBE_DEBLOQUER = "Convaincre";
            public const string MONTER = "Monter";
            /// Le bouton d'un lot de crans : « Monter ×7 ».
            public const string MONTER_PLUSIEURS = "Monter ×{0}";
            /// Les deux façons de monter, choisies en tête du tiroir « Espèces ».
            public const string MONTER_UN_CRAN = "un cran";
            public const string MONTER_JUSQU_AU_SEUIL = "jusqu’au seuil";
            public const string FACON_DE_MONTER = "Monter";
            public const string CREUSER = "Creuser plus bas";
            /// « La file » : des creux demandés d'avance attendent leur mana.
            public const string CREUSER_EN_ATTENTE = "Creuser plus bas · {0} en attente";
            public const string PLUS_DE_ROCHE = "Il n’y a plus de roche à ouvrir ici";

            /* — La renaissance ———————————————————————————————————————————————————— */
            public const string RENTRER_DANS_L_OEUF = "Rentrer dans l’œuf";
            public const string TOUT_RESTERA_ICI = "Tout ce qui vit ici restera ici. Ce que tu as appris te suivra.";
            public const string SOUFFLE_LAISSE = "Souffle que le vivant a laissé";
            public const string CHARGE_GARDEE = "Ce que la mare gardera de ta charge";
            public const string PLUS_DENSE = "plus dense";
            public const string RESTER_OU_PARTIR = "Rester plus longtemps fait monter le Souffle. Partir maintenant fait descendre plus bas.";
            public const string RENTRER = "Rentrer";
            public const string RESTER = "Rester";

            /* — Les succès ———————————————————————————————————————————————————————— */
            public const string SUCCES_TITRE = "Ce qui est arrivé — {0}";
            public const string SUCCES_RIEN_ENCORE = "Rien encore. Ça vient vite.";
            public const string SUCCES_EN_CHEMIN = "En chemin";
            public const string SUCCES_PLUS_LOIN = "Plus loin";
            public const string SUCCES_EMPLACEMENTS_VIDES = "Emplacements vides";
            public const string SUCCES_MARQUE_DES_VIDES = "·";

            /* — La bibliothèque (GDD §14.8) : qui il a rencontré, ce qu'il sait ———————— */
            public const string BIBLIOTHEQUE_TRIBUS = "Ceux qu’il a rencontrés";
            public const string BIBLIOTHEQUE_SAVOIR = "Ce qu’il sait";
            /// Une tribu d'un lieu atteint, pas encore convaincue : sa silhouette, rien d'autre.
            public const string TRIBU_APERCUE = "Une silhouette, au loin. Convaincs-la pour la connaître.";
            /// Le détail d'un succès, à la demande : la condition, l'effet chiffré, la vie.
            public const string DETAIL_QUAND = "Quand : {0}";
            public const string DETAIL_EFFET = "Ce que ça change : {0}";
            public const string DETAIL_NOTE = "Noté dans ta vie n° {0}";
            public const string DETAIL_TOUCHER = "Touche pour lire le détail";
            public const string EFFET_AUCUN = "rien d’autre qu’un souvenir";
            public const string EFFET_CREUSER = "creuser coûte {0} de moins";
            public const string EFFET_MONTER = "monter d’un cran coûte {0} de moins";
            public const string EFFET_DEBLOQUER = "convaincre coûte {0} de moins";
            public const string EFFET_COUT = "un prix baisse de {0}";
            public const string EFFET_PORTER = "tu peux porter {0} de plus";
            public const string EFFET_ABSENCE = "ton absence compte {0} de plus";
            public const string EFFET_PLAFOND = "une limite monte de {0}";
            public const string EFFET_VERBE = "ton peuple fait désormais un geste seul";

            /* — Les améliorations ——————————————————————————————————————————————————— */
            public const string AMELIORATIONS_TITRE = "Améliorations de renaissance";
            public const string SOUFFLE_EN_RESERVE = "{0} de Souffle";
            public const string AMELIORER = "Améliorer";
            public const string TOUT_AMELIORER = "Tout améliorer";
            /// Le tiroir « Toi » : ce que le héros a appris, et ce qui vient ensuite.
            public const string SAVOIR_TITRE = "Ce que tu sais";
            public const string SAVOIR_RIEN_ENCORE = "Rien encore. Ça vient en faisant.";
            public const string SAVOIR_PROCHAIN = "Encore {0} à convaincre, et tu en sauras plus.";
            public const string SAVOIR_PROCHAIN_UN = "Encore un à convaincre, et tu en sauras plus.";
            public const string AMELIORER_UNE_ESPECE = "Améliorer {0}";
            public const string JAMAIS = "jamais";
            public const string FOIS = "{0} fois";

            /* — L’onglet Débloquer : lieux, bonus de lieu, techniques (spec du 2026-10-10) */
            public const string DEBLOQUER = "Débloquer";
            public const string SOUS_TITRE_DEBLOQUER = "Ce que tu peux encore ouvrir, ici et partout";
            public const string ONGLET_LIEUX = "Lieux";
            public const string ONGLET_BONUS_DE_LIEU = "Bonus de lieu";
            public const string ONGLET_TECHNIQUES = "Techniques";
            public const string LIEUX_ATTEINTS = "{0} sur {1} atteints";
            public const string LIEU_OUVERT = "ouvert";
            public const string MAITRISE = "Maîtrise";
            public const string MAITRISE_SUR = "{0} / {1}";
            public const string BONUS_S_OUVRE_A = "s’ouvre à la maîtrise {0}";
            public const string TECHNIQUE_S_OUVRE_A = "s’ouvre à {0} creux ouverts";
            public const string LIEU_FERME_CONDITION = "s’ouvre en creusant {0} jusqu’au fond";
            public const string BONUS_DU_LIEU_EXPLIQUE = "Ils ne valent que pour leur lieu, et restent ici quand tu rentres dans l’œuf.";
            public const string TECHNIQUES_EXPLIQUE = "Elles valent partout, et restent ici quand tu rentres dans l’œuf.";
            public const string RANG_SUR = "rang {0} / {1}";
            public const string ACHETER = "Acheter";
            public const string AU_MAXIMUM = "max";
            public const string VERROUILLE = "fermé";
            public const string GENRE_COUT = "coût";
            public const string GENRE_PRODUCTION = "captation";
            public const string GENRE_CONFORT = "absence";
            public const string GENRE_VERBE = "seul";
            public const string DOCK_DEBLOQUER = "Débloquer";
            public const string AUCUN_BONUS_DE_LIEU = "aucun bonus de lieu";
            public const string BONUS_DE_LIEU_RANGS = "{0} rangs de bonus de lieu";

            /* — Le dock : un mot sous chaque icône, qui tient sous le pouce ————————— */
            public const string DOCK_TOI = "Toi";
            public const string DOCK_ESPECES = "Espèces";
            public const string DOCK_JOURNAL = "Bibliothèque";
            public const string DOCK_OEUF = "L’œuf";

            /* — Ce qu'un tiroir dit sous son titre ————————————————————————————————— */
            public const string SOUS_TITRE_TOI = "Ce que tu captes, et ce que rester t’a déjà gagné";
            public const string SOUS_TITRE_ESPECES = "Ceux qui vivent ici, lieu par lieu";
            public const string SOUS_TITRE_OEUF = "Ce que tu emportes, ce que tu laisses";
            public const string SOUS_TITRE_JOURNAL = "Ceux qu’il a rencontrés, ce qu’il sait";

            /* — L’écran d’accueil (spec du 2026-10-08) ————————————————————————————— */
            public const string ACCUEIL_SOUS_TITRE = "Une mare, un poisson, et tout le temps du monde";
            public const string COMMENCER = "Commencer";
            public const string CONTINUER = "Continuer";
            public const string NOUVELLE_PARTIE = "Nouvelle partie";
            public const string QUITTER = "Quitter";
            public const string ACCUEIL_MANA = "{0} de mana";
            public const string ACCUEIL_ABSENT = "absent {0}";
            public const string VERSION = "version {0}";

            /* — Le tiroir des réglages (spec du 2026-10-08) ———————————————————————— */
            public const string REGLAGES = "Réglages";
            public const string SOUS_TITRE_REGLAGES = "Le son, l’écran, et ce qui t’aide à jouer";
            public const string ONGLET_SON = "Son";
            public const string ONGLET_AFFICHAGE = "Affichage";
            public const string ONGLET_JEU = "Jeu";
            public const string ONGLET_ACCESSIBILITE = "Accessibilité";
            public const string OUI = "Oui";
            public const string NON = "Non";
            public const string POURCENT_DU_REGLAGE = "{0} %";
            public const string VOLUME_GENERAL = "Volume général";
            public const string VOLUME_MUSIQUE = "Musique";
            public const string VOLUME_EFFETS = "Effets";
            public const string COUPER_LE_SON = "Couper le son";
            public const string SE_TAIRE_DERRIERE = "Se taire quand le jeu passe derrière";
            public const string SONS_A_VENIR = "La mare n’a pas encore de sons : ces réglages les attendent.";
            public const string FORMAT_D_AFFICHAGE = "Format de l’écran";
            public const string FORMAT_DETECTE = "Reconnu : {0}";
            public const string FORMAT_AUTO = "Auto";
            public const string FORMAT_TELEPHONE = "Téléphone";
            public const string FORMAT_TABLETTE = "Tablette";
            public const string FORMAT_PC = "PC";
            public const string TAILLE_DE_L_INTERFACE = "Taille de l’interface";
            public const string PLEIN_ECRAN = "Plein écran";
            public const string IMAGES_PAR_SECONDE = "Images par seconde";
            public const string IMAGES_SANS_LIMITE = "Sans limite";
            public const string IMAGES_DETAIL = "Moins d’images, c’est moins de batterie.";
            public const string NOTATION = "Les grands nombres";
            public const string ANNONCES_DE_SUCCES = "Annoncer les succès";
            public const string ANNONCES_DETAIL = "Ils arrivent toujours dans le Journal.";
            public const string EFFACER_LA_PARTIE = "Effacer la partie";
            public const string EFFACER_DETAIL = "Tout recommence. Une copie de ta partie est gardée à côté de la sauvegarde.";
            public const string RETABLIR_LES_REGLAGES = "Rétablir les réglages";
            public const string TOUCHER_POUR_CONFIRMER = "Toucher encore pour confirmer";
            public const string MOUVEMENT_REDUIT = "Réduire les animations";
            public const string CONTRASTE_RENFORCE = "Contraste renforcé";

            /* — Le tiroir « Toi » ——————————————————————————————————————————————————— */
            public const string GAIN_DU_SEJOUR = "Rester t’a gagné";
            public const string POURCENT_DE_CONTENANCE = "+{0} % de ce que tu peux porter";
            public const string FICHE_CAPTATION = "Ce que tu captes";
            public const string FICHE_CONTENANCE = "Ce que tu peux porter";
            public const string FICHE_PAR_TOUCHER = "Par toucher";
            public const string FICHE_RETOURS = "Retours dans l’œuf";

            /* — Le tiroir « Espèces » ———————————————————————————————————————————————— */
            public const string CRAN_ET_DEBIT = "{0} cran · ";
            public const string CRANS_ET_DEBIT = "{0} crans · ";
            /// Sans « Compter les siens » : ce qu'une espèce donne, en pente (part du tout).
            public const string PENTE_L_ESSENTIEL = "l’essentiel";
            public const string PENTE_BONNE_PART = "une bonne part";
            public const string PENTE_UN_PEU = "un peu";
            public const string PENTE_PRESQUE_RIEN = "presque rien";
            public const string PENTE_RIEN_ENCORE = "rien encore";
            public const string PROCHAIN_SEUIL = "×{0} au cran {1}";
            public const string TOUS_LES_SEUILS = "tous les seuils franchis";
            public const string CONVAINCUS_SUR = "{0} / {1}";
            public const string JUSQU_A = "jusqu’à {0}";
            public const string A_PROFONDEUR = "à {0}";
            public const string INCONNU = "???";
            public const string PLUS_BAS_QUE = "plus bas que {0}";

            /* — Le tiroir « L'œuf » ——————————————————————————————————————————————————— */
            public const string CONTENANCE_GARDEE = "Ce que tu pourras porter";
            public const string POURCENT = "+{0} %";
            public const string TU_EMPORTES = "Tu emportes";
            public const string TU_LAISSES = "Tu laisses";
            public const string EMPORTE_SOUFFLE = "le Souffle, et tes améliorations";
            public const string EMPORTE_JOURNAL = "ce qui est arrivé";
            public const string EMPORTE_MARQUES = "les marques sur ton corps";
            public const string LAISSE_MANA = "le mana";
            public const string LAISSE_ESPECES = "les espèces convaincues";
            public const string LAISSE_PROFONDEUR = "la profondeur creusée";
            public const string LAISSE_TAILLE = "ta taille";

            /* — Le tiroir « Journal » ————————————————————————————————————————————————— */
            public const string JOURNAL_COMPTE = "{0} sur {1}";
            public const string JOURNAL_ARRIVES = "arrivés jusqu’ici";
            public const string FAMILLE_TOUS = "Tous";
            public const string FAMILLE_FRANCHISSEMENT = "Profondeur";
            public const string FAMILLE_SEUIL = "Seuils";
            public const string FAMILLE_ACTE = "Gestes";
            public const string ARRIVE = "arrivé";

            /* — Ce que `Format` met en mots ——————————————————————————————————————————— */
            public const string PLUS_BAS = "plus bas";
            public const string ESPECE_SANS_NOM = "un banc sans nom";
            public const string A_FLEUR_D_EAU = "à fleur d’eau";
            public const string BRASSE = "{0} brasse";
            public const string BRASSES = "{0} brasses";
            public const string PERSONNE_ENCORE = "personne encore";
            public const string CRAN_TENU = "{0} cran tenu";
            public const string CRANS_TENUS = "{0} crans tenus";
            public const string AUCUNE_ESPECE_AU_COMPLET = "aucune espèce au complet";
            public const string ESPECE_AU_COMPLET = "{0} espèce déjà au complet";
            public const string ESPECES_AU_COMPLET = "{0} espèces déjà au complet";
            public const string EAU_NEUTRE = "eau neutre";
            public const string EAU_A_DENSITE = "eau à {0} de densité";
            public const string TOI_QUI_CAPTES_SEUL = "toi, qui captes seul";
            public const string TOI_GRANDI = "toi, grandi {0} fois";
            public const string AUCUNE_AMELIORATION = "aucune amélioration";
            public const string AMELIORE_FOIS = "amélioré {0} fois";
            public const string DUREE_INCONNUE = "—";
        }

        /// Repli sobre : un succès sans texte reste listable, il ne casse pas l'écran.
        public static readonly TexteDeSucces SUCCES_INCONNU = new("—", "—", "—");

        public static TexteDeSucces DuSucces(string id)
        {
            if (TEXTES_DE_SUCCES.TryGetValue(id, out var ecrit)) return ecrit;
            foreach (var (motif, texte) in GABARITS)
            {
                var trouve = motif.Match(id);
                if (trouve.Success) return texte(int.Parse(trouve.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            return SUCCES_INCONNU;
        }

        public static IEnumerable<(string Ou, string Texte)> TousLesTextesAffiches()
        {
            foreach (var assise in Assises.Toutes)
                if (NOM_DES_ASSISES.TryGetValue(assise.Id, out var nom))
                    yield return ($"assise {assise.Id}.nom", nom);

            foreach (var espece in Especes.Toutes)
                if (NOM_DES_ESPECES.TryGetValue(espece.Id, out var nom))
                    yield return ($"espece {espece.Id}.nom", nom);

            if (AMELIORATION_GLOBALE.Nom != null) yield return ("amélioration amelioration-globale.nom", AMELIORATION_GLOBALE.Nom);
            yield return ("amélioration amelioration-globale.effet", AMELIORATION_GLOBALE.Effet);
            if (AMELIORATION_CIBLEE.Nom != null) yield return ("amélioration amelioration-ciblee.nom", AMELIORATION_CIBLEE.Nom);
            yield return ("amélioration amelioration-ciblee.effet", AMELIORATION_CIBLEE.Effet);
            yield return ("amélioration amelioration-sejour.nom", AMELIORATION_DE_SEJOUR.Nom);
            yield return ("amélioration amelioration-sejour.effet", AMELIORATION_DE_SEJOUR.Effet);
            yield return ("amélioration amelioration-toucher.nom", AMELIORATION_DU_TOUCHER.Nom);
            yield return ("amélioration amelioration-toucher.effet", AMELIORATION_DU_TOUCHER.Effet);

            foreach (var noeud in NoeudsTechnique.Tous)
            {
                var texte = DuNoeud(noeud.Id);
                yield return ($"nœud {noeud.Id}.nom", texte.Nom);
                yield return ($"nœud {noeud.Id}.effet", texte.Effet);
            }

            // Les chaînes de l'interface : les constantes de `Ecran`, lues une à une.
            foreach (var champ in typeof(Ecran).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (champ.IsLiteral && champ.GetRawConstantValue() is string chaine)
                    yield return ($"ecran {champ.Name}", chaine);

            foreach (var bonus in RegistreDesBonus.Tous)
            {
                var texte = DuBonus(bonus.Id);
                yield return ($"bonus {bonus.Id}.nom", texte.Nom);
                yield return ($"bonus {bonus.Id}.effet", texte.Effet);
            }

            foreach (var tribu in TEXTES_DES_TRIBUS)
            {
                yield return ($"tribu {tribu.Key}.tribu", tribu.Value.Tribu);
                yield return ($"tribu {tribu.Key}.representant", tribu.Value.Representant);
                yield return ($"tribu {tribu.Key}.moeurs", tribu.Value.Moeurs);
                yield return ($"tribu {tribu.Key}.citation", tribu.Value.Citation);
            }

            foreach (var succes in RegistreDesSucces.Tous)
            {
                var texte = DuSucces(succes.Id);
                yield return ($"succes {succes.Id}.nom", texte.Nom);
                yield return ($"succes {succes.Id}.condition", texte.Condition);
                yield return ($"succes {succes.Id}.rapport", texte.Rapport);
            }
        }
    }
}
