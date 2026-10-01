using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>Ce que l'écran dit d'une insufflation — un verbe, ce que ça fait.</summary>
    public sealed record TexteDInsufflation(string Nom, string Effet);

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
        };

        public static readonly IReadOnlyDictionary<string, string> NOM_DES_ESPECES = new Dictionary<string, string>
        {
            ["vairon"] = "le vairon",
            ["loche"] = "la loche",
            ["epinoche"] = "l’épinoche",
        };

        /// <summary>
        /// Ce que l'écran dit d'une insufflation — un verbe, ce que ça fait. La globale
        /// a un nom à elle ; une ciblée prend le nom de son espèce.
        /// </summary>
        public static readonly TexteDInsufflation INSUFFLATION_GLOBALE = new("Insuffler l’eau", "tout ce qui vit ici capte un peu plus, et tout ce qui viendra");

        public static readonly TexteDInsufflation INSUFFLATION_CIBLEE = new(null, "ils te donnent moitié plus, à chaque fois");

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
                "Il n’y a plus de roche à ouvrir ici."),
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

            if (INSUFFLATION_GLOBALE.Nom != null) yield return ("insufflation insufflation-globale.nom", INSUFFLATION_GLOBALE.Nom);
            yield return ("insufflation insufflation-globale.effet", INSUFFLATION_GLOBALE.Effet);
            if (INSUFFLATION_CIBLEE.Nom != null) yield return ("insufflation insufflation-ciblee.nom", INSUFFLATION_CIBLEE.Nom);
            yield return ("insufflation insufflation-ciblee.effet", INSUFFLATION_CIBLEE.Effet);

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
