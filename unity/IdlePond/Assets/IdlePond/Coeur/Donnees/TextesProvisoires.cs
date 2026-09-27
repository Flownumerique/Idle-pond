/*
 * IdlePond — TOUS les textes affichés, et ils sont PROVISOIRES. Port de
 * `src/donnees/textes-provisoires.ts`, chaîne pour chaîne.
 *
 * ╔════════════════════════════════════════════════════════════════════════╗
 * ║  Les NOMS de la Noue sont du canon (amendement v1.1 §2.E).             ║
 * ║  Les PHRASES restent provisoires : elles vivent ici, à un seul endroit, ║
 * ║  et aucune n'entre dans un identifiant, une clef de save ou une donnée. ║
 * ║  [P] P3 reste ouvert pour les assises II à VI.                         ║
 * ╚════════════════════════════════════════════════════════════════════════╝
 *
 * Règle d'écriture, elle non provisoire (§8.3) : le narrateur rapporte ce qui
 * est arrivé et ce que ça a changé. Il n'explique JAMAIS pourquoi le monde
 * fonctionne ainsi.
 *
 * Le test de parité vérifie que ces textes sont ceux du web, à l'octet près :
 * les réécrire se fait des deux côtés à la fois, tant que le web reste la
 * référence.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace IdlePond.Donnees
{
    public sealed record TexteDeSucces(string Nom, string Condition, string Rapport);

    public static class TextesProvisoires
    {
        /// <summary>Le nom propre du lieu. L'UI n'affiche jamais « Assise I » (§3).</summary>
        public static readonly IReadOnlyDictionary<string, string> NomDesAssises = new Dictionary<string, string>
        {
            ["noue"] = "la Noue",
        };

        public static readonly IReadOnlyDictionary<string, string> NomDesEspeces = new Dictionary<string, string>
        {
            ["vairon"] = "le vairon",
            ["loche"] = "la loche",
            ["epinoche"] = "l’épinoche",
        };

        public static readonly IReadOnlyDictionary<string, TexteDeSucces> TextesDeSucces = new Dictionary<string, TexteDeSucces>
        {
            /* — Actes ——————————————————————————————————————————————————————— */
            ["acte-premiere-conviction"] = new TexteDeSucces(
                "Un banc te suit", "Convaincre un premier banc", "Ils sont venus sans qu’on les appelle deux fois."),
            ["acte-deuxieme-niveau"] = new TexteDeSucces(
                "Ils reviennent", "Faire une deuxième place dans un banc", "Un de plus s’est joint sans qu’on insiste."),
            ["acte-cinquieme-niveau"] = new TexteDeSucces(
                "Le geste prend", "Faire cinq places dans un banc", "Ça va plus vite qu’au début."),
            ["acte-premier-banc-de-cinq"] = new TexteDeSucces(
                "Cinq à demeure", "Cinq individus installés dans un même banc", "Ils ne repartent plus entre deux passages."),
            ["acte-premier-creusement"] = new TexteDeSucces(
                "La roche cède", "Creuser une fois plus bas", "Le fond s’est ouvert. Il y avait de la place dessous."),
            ["acte-deux-bancs"] = new TexteDeSucces(
                "Ils sont deux", "Convaincre deux bancs à la fois", "Le second n’a pas fui en voyant le premier."),
            ["acte-trois-bancs"] = new TexteDeSucces(
                "La Noue répond", "Convaincre trois bancs à la fois", "On ne peut plus les compter d’un seul regard."),
            ["acte-premier-palier-sature"] = new TexteDeSucces(
                "Plein à ras", "Remplir un creux jusqu’à sa cible", "Ce creux ne prend plus personne. Il faudra descendre."),
            ["acte-dixieme-niveau"] = new TexteDeSucces(
                "La main est faite", "Faire dix places dans un banc", "Le geste se répète tout seul, maintenant."),

            /* — Seuils, engendrés par gabarit ——————————————————————————————— */
            ["seuil-vairon-10"] = new TexteDeSucces("Dix vairons", "Dix individus", "Ils tiennent le banc ensemble."),
            ["seuil-vairon-25"] = new TexteDeSucces("Vingt-cinq vairons", "Vingt-cinq individus", "Le banc vire d’un seul tenant."),
            ["seuil-vairon-50"] = new TexteDeSucces("Cinquante vairons", "Cinquante individus", "On entend le courant qu’ils font."),
            ["seuil-vairon-100"] = new TexteDeSucces("Cent vairons", "Cent individus", "Ils ne repartiront plus. Jamais."),
            ["seuil-loche-10"] = new TexteDeSucces("Dix loches", "Dix individus", "Le fond est remué par en dessous."),
            ["seuil-loche-25"] = new TexteDeSucces("Vingt-cinq loches", "Vingt-cinq individus", "Elles ouvrent des passages qu’on n’a pas creusés."),
            ["seuil-loche-50"] = new TexteDeSucces("Cinquante loches", "Cinquante individus", "La vase ne tient plus en place."),
            ["seuil-loche-100"] = new TexteDeSucces("Cent loches", "Cent individus", "Elles ne repartiront plus. Jamais."),
            ["seuil-epinoche-10"] = new TexteDeSucces("Dix épinoches", "Dix individus", "Elles tiennent là où l’eau se charge."),
            ["seuil-epinoche-25"] = new TexteDeSucces("Vingt-cinq épinoches", "Vingt-cinq individus", "Rien ne les déloge du bord."),
            ["seuil-epinoche-50"] = new TexteDeSucces("Cinquante épinoches", "Cinquante individus", "L’eau lourde ne leur fait plus rien."),
            ["seuil-epinoche-100"] = new TexteDeSucces("Cent épinoches", "Cent individus", "Elles ne repartiront plus. Jamais."),

            /* — Franchissements ————————————————————————————————————————————— */
            ["franchissement-premiere-eclosion"] = new TexteDeSucces(
                "Rentrer", "Éclore une première fois", "Tout est resté en bas. Presque tout."),
            ["franchissement-deuxieme-eclosion"] = new TexteDeSucces(
                "Rentrer encore", "Éclore deux fois", "La descente a été plus courte que la première."),
            ["franchissement-troisieme-eclosion"] = new TexteDeSucces(
                "Le chemin se sait", "Éclore trois fois", "Les mêmes fonds, et plus vite."),
            ["franchissement-densite"] = new TexteDeSucces(
                "La Noue est chargée", "Charger le premier creux", "Ce qui a été porté là y est resté."),
            ["franchissement-fond-de-la-mare"] = new TexteDeSucces(
                "Le fond de la Noue", "Ouvrir la Noue jusqu’au fond", "Il n’y a plus de roche à ouvrir ici."),
        };

        /* Les familles engendrées par gabarit ont leur texte engendré aussi (§8.1). */

        /// <summary>Le rapport change avec le rang : deux entrées voisines ne se lisent jamais pareil.</summary>
        private static string Tour(IReadOnlyList<string> liste, int n) => liste[n % liste.Count];

        private static readonly string[] RapportsDeMare =
        {
            "On ne les compte plus d’un seul regard.",
            "Il y a du monde jusque dans les recoins.",
            "L’eau bouge toute seule, maintenant.",
            "Le fond ne se voit plus à travers eux.",
            "Ça tient sans qu’on s’en occupe.",
        };

        private static readonly string[] RapportsDeProfondeur =
        {
            "Le fond est plus loin qu’à la dernière descente.",
            "La lumière ne descend plus jusqu’ici.",
            "L’eau est froide, et plus lourde.",
        };

        private sealed record Gabarit(Regex Motif, Func<int, TexteDeSucces> Texte);

        private static readonly Gabarit[] Gabarits =
        {
            new Gabarit(new Regex(@"^seuil-mare-(\d+)$"), n => new TexteDeSucces(
                n + " dans la Noue",
                n + " individus, tous bancs confondus",
                Tour(RapportsDeMare, n / 20))),
            new Gabarit(new Regex(@"^seuil-profondeur-(\d+)$"), n => new TexteDeSucces(
                n + " creusements",
                "Ouvrir " + n + " fois plus bas dans la même vie",
                Tour(RapportsDeProfondeur, n))),
            new Gabarit(new Regex(@"^seuil-palier-sature-(\d+)$"), _ => new TexteDeSucces(
                "Un creux de plus est plein",
                "Remplir un creux jusqu’à sa cible",
                "Celui-là ne prendra plus personne.")),
        };

        /// <summary>Repli sobre : un succès sans texte reste listable, il ne casse pas l'écran.</summary>
        public static readonly TexteDeSucces TexteDeSuccesInconnu = new TexteDeSucces("—", "—", "—");

        public static TexteDeSucces TexteDuSucces(string id)
        {
            if (TextesDeSucces.TryGetValue(id, out var ecrit)) return ecrit;
            foreach (var gabarit in Gabarits)
            {
                var trouve = gabarit.Motif.Match(id);
                if (trouve.Success) return gabarit.Texte(int.Parse(trouve.Groups[1].Value, CultureInfo.InvariantCulture));
            }
            return TexteDeSuccesInconnu;
        }
    }
}
