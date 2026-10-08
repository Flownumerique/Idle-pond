using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>Ce que l'écran dit d'une amélioration — un verbe, ce que ça fait.</summary>
    public sealed record TexteDAmelioration(string Nom, string Effet);

    public sealed record TexteDeSucces(string Nom, string Condition, string Rapport);

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

        /// <summary>
        /// Ce que l'écran dit d'une amélioration — un verbe, ce que ça fait. La globale
        /// a un nom à elle ; une ciblée prend le nom de son espèce.
        /// </summary>
        public static readonly TexteDAmelioration AMELIORATION_GLOBALE = new("Améliorer l’eau", "tout ce qui vit ici capte un peu plus, et tout ce qui viendra");

        public static readonly TexteDAmelioration AMELIORATION_CIBLEE = new(null, "ils te donnent moitié plus, à chaque fois");

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

            /* — La captation ———————————————————————————————————————————————————— */
            public const string FERMER = "fermer";
            public const string CE_QU_ILS_TE_DONNENT = "ce qu’ils te donnent";
            public const string PAR_SECONDE = "par seconde";

            /* — La mare ————————————————————————————————————————————————————————— */
            public const string ESPECE_QUI_S_ATTARDE = "un banc s’attarde";
            public const string VERBE_DEBLOQUER = "Convaincre";
            public const string MONTER = "Monter";
            public const string CREUSER = "Creuser plus bas";
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

            /* — Les améliorations ——————————————————————————————————————————————————— */
            public const string AMELIORATIONS_TITRE = "Améliorations de renaissance";
            public const string SOUFFLE_EN_RESERVE = "{0} de Souffle";
            public const string AMELIORER = "Améliorer";
            public const string AMELIORER_UNE_ESPECE = "Améliorer {0}";
            public const string JAMAIS = "jamais";
            public const string FOIS = "{0} fois";

            /* — Le dock : un mot sous chaque icône, qui tient sous le pouce ————————— */
            public const string DOCK_TOI = "Toi";
            public const string DOCK_ESPECES = "Espèces";
            public const string DOCK_JOURNAL = "Journal";
            public const string DOCK_OEUF = "L’œuf";

            /* — Ce qu'un tiroir dit sous son titre ————————————————————————————————— */
            public const string SOUS_TITRE_TOI = "Ce que tu captes, et ce que rester t’a déjà gagné";
            public const string SOUS_TITRE_ESPECES = "Ceux qui vivent ici, lieu par lieu";
            public const string SOUS_TITRE_OEUF = "Ce que tu emportes, ce que tu laisses";
            public const string SOUS_TITRE_JOURNAL = "Ce qui est arrivé, et ce qui vient";

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
            public const string FICHE_DEBIT_PROPRE = "Toi seul";
            public const string FICHE_RETOURS = "Retours dans l’œuf";

            /* — Le tiroir « Espèces » ———————————————————————————————————————————————— */
            public const string CRANS_ET_DEBIT = "{0} crans · ";
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

            // Les chaînes de l'interface : les constantes de `Ecran`, lues une à une.
            foreach (var champ in typeof(Ecran).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                if (champ.IsLiteral && champ.GetRawConstantValue() is string chaine)
                    yield return ($"ecran {champ.Name}", chaine);

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
