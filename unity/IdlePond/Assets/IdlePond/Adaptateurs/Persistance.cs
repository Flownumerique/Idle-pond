/*
 * IdlePond — persistance : sérialisation des grands nombres et versionnage de
 * save. Port de `src/adaptateurs/persistance.ts`.
 *
 * §10 : versionnage dès la v0.1, avec une chaîne de migrations même vide.
 *
 * LE FORMAT EST CELUI DU WEB, clef pour clef et dans le même ordre. Une save
 * écrite par la version web (la valeur `idlepond` de son localStorage) se relit
 * ici, migrations comprises ; une save écrite ici se relit sur le web. C'est ce
 * qui permet de migrer les joueurs sans leur faire perdre leur partie, et c'est
 * ce que le test de parité vérifie sur des saves produites par le code
 * TypeScript lui-même.
 *
 * Les grands nombres sont sérialisés explicitement, champ par champ, en chaîne
 * (`toString()` de break_infinity). Le mappage explicite est ce qui fait que
 * l'aller-retour est testable.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Adaptateurs
{
    /// <summary>`{ versionSave, contenu }`. Le contenu reste un arbre JSON tant qu'il n'est pas migré.</summary>
    public sealed record SaveSerialisee(double VersionSave, JsonValeur Contenu);

    public static class Persistance
    {
        /* ─── Grands nombres ────────────────────────────────────────────────── */

        /// <summary>Un grand nombre persiste en chaîne : `toString()` fait l'aller-retour à l'exact.</summary>
        public static JsonTexte SerialiserGrandNombre(GrandNombre valeur) => new JsonTexte(valeur.ToString());

        public static GrandNombre DeserialiserGrandNombre(JsonValeur valeur, GrandNombre repli)
        {
            GrandNombre lu;
            switch (valeur)
            {
                case JsonTexte texte:
                    if (!GrandNombre.EssayerDeLire(texte.Valeur, out lu)) return repli;
                    return lu;
                case JsonNombre nombre:
                    lu = GrandNombre.DepuisNombre(nombre.Valeur);
                    return lu.EstNaN ? repli : lu;
                default:
                    return repli;
            }
        }

        /* ─── Sérialisation ─────────────────────────────────────────────────── */

        private static JsonNombre N(double valeur) => new JsonNombre(valeur);

        private static JsonTableau Nombres(IEnumerable<double> valeurs) => new JsonTableau(valeurs.Select(v => (JsonValeur)N(v)));

        private static JsonTableau Textes(IEnumerable<string> valeurs) => new JsonTableau(valeurs.Select(v => (JsonValeur)new JsonTexte(v)));

        public static JsonObjet Serialiser(EtatJeu etat)
        {
            var cycle = etat.Cycle;
            var bancs = new JsonObjet();
            foreach (var paire in cycle.Bancs)
            {
                bancs.Poser(paire.Key, new JsonObjet().Poser("place", N(paire.Value.Place)).Poser("effectif", N(paire.Value.Effectif)));
            }

            var permanent = etat.Permanent;
            var acclimatations = new JsonObjet();
            foreach (var paire in permanent.Acclimatations) acclimatations.Poser(paire.Key, N(paire.Value));
            var compteurs = new JsonObjet();
            foreach (var paire in permanent.CompteursTechnique) compteurs.Poser(paire.Key, N(paire.Value));
            var succes = new JsonObjet();
            foreach (var paire in permanent.Succes)
            {
                succes.Poser(paire.Key, new JsonObjet()
                    .Poser("obtenuAuCycle", N(paire.Value.ObtenuAuCycle))
                    .Poser("registre", new JsonTexte(RegistresDeVoix.Identifiant(paire.Value.Registre))));
            }

            var telemetrie = etat.Telemetrie;
            var cycles = new JsonTableau();
            foreach (var c in telemetrie.Cycles)
            {
                cycles.Ajouter(new JsonObjet()
                    .Poser("index", N(c.Index))
                    .Poser("dureeEcouleeSecondes", N(c.DureeEcouleeSecondes))
                    .Poser("secondesEnRedescente", N(c.SecondesEnRedescente))
                    .Poser("paliersOuverts", N(c.PaliersOuverts))
                    .Poser("productionPicParSeconde", SerialiserGrandNombre(c.ProductionPicParSeconde))
                    .Poser("foiGagnee", SerialiserGrandNombre(c.FoiGagnee)));
            }

            var contenu = new JsonObjet()
                .Poser("prng", new JsonObjet().Poser("graine", N(etat.Prng.Graine)))
                .Poser("tempsJeuSecondes", N(etat.TempsJeuSecondes))
                .Poser("limiteDeContenu", N(etat.LimiteDeContenu))
                .Poser("cycle", new JsonObjet()
                    .Poser("manaCourant", SerialiserGrandNombre(cycle.ManaCourant))
                    .Poser("paliersOuverts", N(cycle.PaliersOuverts))
                    .Poser("bancs", bancs)
                    .Poser("productionPicParSeconde", SerialiserGrandNombre(cycle.ProductionPicParSeconde))
                    .Poser("dureeSecondes", N(cycle.DureeSecondes))
                    .Poser("acquisDeSejour", N(cycle.AcquisDeSejour))
                    .Poser("secondesEnSaturation", N(cycle.SecondesEnSaturation)))
                .Poser("permanent", new JsonObjet()
                    .Poser("densites", Nombres(permanent.Densites))
                    .Poser("partsMures", Nombres(permanent.PartsMures))
                    .Poser("acclimatations", acclimatations)
                    .Poser("foi", SerialiserGrandNombre(permanent.Foi))
                    .Poser("contenanceMana", SerialiserGrandNombre(permanent.ContenanceMana))
                    .Poser("couches", Textes(permanent.Couches))
                    .Poser("profondeurMaxAtteinte", N(permanent.ProfondeurMaxAtteinte))
                    .Poser("compteursTechnique", compteurs)
                    .Poser("noeudsTechnique", Textes(permanent.NoeudsTechnique))
                    .Poser("succes", succes)
                    .Poser("nombreEclosions", N(permanent.NombreEclosions))
                    .Poser("especesAyantAtteintCent", Textes(permanent.EspecesAyantAtteintCent))
                    .Poser("manaAmbiant", SerialiserGrandNombre(permanent.ManaAmbiant))
                    .Poser("heuresHorsLigneCreditees", N(permanent.HeuresHorsLigneCreditees)))
                .Poser("telemetrie", new JsonObjet()
                    .Poser("cycles", cycles)
                    .Poser("secondesEnRedescente", N(telemetrie.SecondesEnRedescente))
                    .Poser("secondesDepuisDernierSucces", N(telemetrie.SecondesDepuisDernierSucces))
                    .Poser("intervallesEntreSucces", Nombres(telemetrie.IntervallesEntreSucces)));

            return new JsonObjet().Poser("versionSave", N(etat.VersionSave)).Poser("contenu", contenu);
        }

        /* ─── Migrations ────────────────────────────────────────────────────────
         * `Migrations[n]` transforme un contenu de version `n` en contenu de
         * version `n + 1`. Elles opèrent sur l'arbre JSON, comme celles du web
         * opèrent sur un `unknown` : une save ancienne n'a pas la forme d'un état.
         */

        private static JsonObjet ObjetOuVide(JsonValeur valeur) => (valeur as JsonObjet)?.Copier() ?? new JsonObjet();

        private static IEnumerable<string> TextesDe(JsonValeur valeur) =>
            (valeur as JsonTableau)?.Elements.OfType<JsonTexte>().Select(t => t.Valeur) ?? Enumerable.Empty<string>();

        /// <summary>`String.prototype.replace(motif, remplacement)` : la PREMIÈRE occurrence seulement.</summary>
        private static string RemplacerPremiere(string texte, string motif, string remplacement)
        {
            var index = texte.IndexOf(motif, StringComparison.Ordinal);
            return index < 0 ? texte : texte.Substring(0, index) + remplacement + texte.Substring(index + motif.Length);
        }

        public static readonly IReadOnlyDictionary<int, Func<JsonValeur, JsonValeur>> Migrations =
            new Dictionary<int, Func<JsonValeur, JsonValeur>>
            {
                /*
                 * 1 → 2 — amendement v1.1. « Niveau » devient « place », les espèces
                 * de la Noue prennent leur nom, l'assise I passe de dix paliers à six.
                 * Le cycle est rendu, le permanent conservé : une save v1 se réveille
                 * au sortir de l'œuf, avec tous ses acquis.
                 */
                [1] = contenu =>
                {
                    var brut = ObjetOuVide(contenu);
                    var permanent = ObjetOuVide(brut.Lire("permanent"));
                    permanent.Poser("couches", Textes(TextesDe(permanent.Lire("couches")).Select(c => c == "assise-1" ? "noue" : c)));
                    permanent.Poser("especesAyantAtteintCent", new JsonTableau());
                    permanent.Poser("succesDebloques", Textes(TextesDe(permanent.Lire("succesDebloques")).Select(id =>
                        RemplacerPremiere(
                            RemplacerPremiere(
                                RemplacerPremiere(id, "seuil-espece-1-1-", "seuil-vairon-"),
                                "seuil-espece-1-2-", "seuil-loche-"),
                            "seuil-espece-1-3-", "seuil-epinoche-"))));
                    brut.Retirer("cycle");
                    brut.Poser("permanent", permanent);
                    return brut;
                },

                /*
                 * 2 → 3 — le GDD devient directif. `benedictions` disparaît ;
                 * `succesDebloques: string[]` devient `succes: { id: { obtenuAuCycle,
                 * registre } }`. Le registre d'alors n'est pas reconstituable :
                 * toutes les entrées reçoivent celui que les franchissements de la
                 * save impliquent aujourd'hui — faux pour les plus anciennes, et
                 * sciemment.
                 */
                [2] = contenu =>
                {
                    var brut = ObjetOuVide(contenu);
                    var permanent = ObjetOuVide(brut.Lire("permanent"));
                    var franchissements = Json.EssayerNombre(permanent.Lire("nombreEclosions"), out var n) ? (int)n : 0;
                    var registre = RegistresDeVoix.Identifiant(Voix.PalierDeVoixApres(franchissements));
                    var acquis = new HashSet<string>(TextesDe(permanent.Lire("succesDebloques")), StringComparer.Ordinal);

                    // Dans l'ordre du registre, comme le noyau.
                    var succes = new JsonObjet();
                    foreach (var s in RegistreDesSucces.Liste)
                    {
                        if (!acquis.Contains(s.Id)) continue;
                        succes.Poser(s.Id, new JsonObjet()
                            .Poser("obtenuAuCycle", N(franchissements))
                            .Poser("registre", new JsonTexte(registre)));
                    }

                    // Les deux clefs disparues sont RETIRÉES : une clef morte qui
                    // survit à une migration se retrouve dans la save suivante.
                    permanent.Poser("succes", succes);
                    permanent.Retirer("benedictions");
                    permanent.Retirer("succesDebloques");
                    brut.Poser("permanent", permanent);
                    return brut;
                },

                /*
                 * 3 → 4 — les deux canaux de captation (GDD §3 et §3.0). `partsMures`
                 * entre dans l'état permanent, à 1 : une eau que rien n'a habitée est
                 * mûre (§6.5).
                 */
                [3] = contenu =>
                {
                    var brut = ObjetOuVide(contenu);
                    var permanent = ObjetOuVide(brut.Lire("permanent"));
                    permanent.Poser("partsMures",
                        Nombres(Enumerable.Repeat(Maturation.PartMureDUneEauIntouchee, Constantes.NombreDePaliers)));
                    brut.Poser("permanent", permanent);
                    return brut;
                },
            };

        public static JsonValeur Migrer(SaveSerialisee save)
        {
            var contenu = save.Contenu;
            // `for (let v = save.versionSave; v < VERSION_SAVE; v += 1)` : une version
            // absente ou non numérique ne migre pas, comme sur le web.
            if (double.IsNaN(save.VersionSave)) return contenu;
            for (var version = save.VersionSave; version < Constantes.VersionSave; version += 1)
            {
                var entiere = (int)version;
                if (entiere != version || !Migrations.TryGetValue(entiere, out var migration))
                {
                    throw new InvalidOperationException(
                        "Migration de save manquante : version " + NombreJs.VersTexte(version) + " → " + NombreJs.VersTexte(version + 1));
                }
                contenu = migration(contenu);
            }
            return contenu;
        }

        /// <summary>Lit l'enveloppe `{ versionSave, contenu }` d'une save.</summary>
        public static SaveSerialisee Enveloppe(JsonValeur racine)
        {
            var objet = racine as JsonObjet ?? new JsonObjet();
            var version = Json.EssayerNombre(objet.Lire("versionSave"), out var v) ? v : double.NaN;
            return new SaveSerialisee(version, objet.Lire("contenu"));
        }

        /* ─── Désérialisation ───────────────────────────────────────────────────
         * `{ ...repli.cycle, ...cycle }` : chaque champ présent et bien typé est
         * pris dans la save, chaque autre vient du repli. Une save illisible sur un
         * champ ne perd que ce champ.
         */

        public static EtatJeu Deserialiser(SaveSerialisee save, EtatJeu repli)
        {
            var brut = Migrer(save) as JsonObjet ?? new JsonObjet();
            var cycle = brut.Lire("cycle") as JsonObjet ?? new JsonObjet();
            var permanent = brut.Lire("permanent") as JsonObjet ?? new JsonObjet();
            var telemetrie = brut.Lire("telemetrie") as JsonObjet ?? new JsonObjet();

            var prng = repli.Prng;
            if (brut.Lire("prng") is JsonObjet prngBrut && Json.EssayerNombre(prngBrut.Lire("graine"), out var graine))
            {
                prng = new EtatPrng(unchecked((uint)(long)graine));
            }

            return new EtatJeu
            {
                VersionSave = Constantes.VersionSave,
                Prng = prng,
                TempsJeuSecondes = Json.NombreOu(brut.Lire("tempsJeuSecondes"), repli.TempsJeuSecondes),
                // Une save d'un jalon antérieur reprend la limite du jalon courant.
                LimiteDeContenu = Json.EntierOu(brut.Lire("limiteDeContenu"), repli.LimiteDeContenu),
                Cycle = new EtatCycle
                {
                    ManaCourant = DeserialiserGrandNombre(cycle.Lire("manaCourant"), repli.Cycle.ManaCourant),
                    PaliersOuverts = Borne(Json.EntierOu(cycle.Lire("paliersOuverts"), repli.Cycle.PaliersOuverts), Constantes.PaliersOuvertsAuDepart),
                    Bancs = LireBancs(cycle.Lire("bancs"), repli.Cycle.Bancs),
                    ProductionPicParSeconde = DeserialiserGrandNombre(cycle.Lire("productionPicParSeconde"), repli.Cycle.ProductionPicParSeconde),
                    DureeSecondes = Json.NombreOu(cycle.Lire("dureeSecondes"), repli.Cycle.DureeSecondes),
                    AcquisDeSejour = Json.NombreOu(cycle.Lire("acquisDeSejour"), repli.Cycle.AcquisDeSejour),
                    SecondesEnSaturation = Json.NombreOu(cycle.Lire("secondesEnSaturation"), repli.Cycle.SecondesEnSaturation),
                },
                Permanent = new EtatPermanent
                {
                    Densites = LireNombres(permanent.Lire("densites"), repli.Permanent.Densites),
                    PartsMures = LireNombres(permanent.Lire("partsMures"), repli.Permanent.PartsMures),
                    Acclimatations = LireTableDeNombres(permanent.Lire("acclimatations"), repli.Permanent.Acclimatations),
                    Foi = DeserialiserGrandNombre(permanent.Lire("foi"), repli.Permanent.Foi),
                    ContenanceMana = DeserialiserGrandNombre(permanent.Lire("contenanceMana"), repli.Permanent.ContenanceMana),
                    Couches = LireTextes(permanent.Lire("couches"), repli.Permanent.Couches),
                    ProfondeurMaxAtteinte = Borne(Json.EntierOu(permanent.Lire("profondeurMaxAtteinte"), repli.Permanent.ProfondeurMaxAtteinte), 0),
                    CompteursTechnique = LireTableDeNombres(permanent.Lire("compteursTechnique"), repli.Permanent.CompteursTechnique),
                    NoeudsTechnique = LireTextes(permanent.Lire("noeudsTechnique"), repli.Permanent.NoeudsTechnique),
                    Succes = LireSucces(permanent.Lire("succes"), repli.Permanent.Succes),
                    NombreEclosions = Json.EntierOu(permanent.Lire("nombreEclosions"), repli.Permanent.NombreEclosions),
                    EspecesAyantAtteintCent = LireTextes(permanent.Lire("especesAyantAtteintCent"), repli.Permanent.EspecesAyantAtteintCent),
                    ManaAmbiant = DeserialiserGrandNombre(permanent.Lire("manaAmbiant"), repli.Permanent.ManaAmbiant),
                    HeuresHorsLigneCreditees = Json.NombreOu(permanent.Lire("heuresHorsLigneCreditees"), repli.Permanent.HeuresHorsLigneCreditees),
                },
                Telemetrie = new EtatTelemetrie
                {
                    Cycles = LireCycles(telemetrie.Lire("cycles"), repli.Telemetrie.Cycles),
                    SecondesEnRedescente = Json.NombreOu(telemetrie.Lire("secondesEnRedescente"), repli.Telemetrie.SecondesEnRedescente),
                    SecondesDepuisDernierSucces = Json.NombreOu(telemetrie.Lire("secondesDepuisDernierSucces"), repli.Telemetrie.SecondesDepuisDernierSucces),
                    IntervallesEntreSucces = LireNombres(telemetrie.Lire("intervallesEntreSucces"), repli.Telemetrie.IntervallesEntreSucces),
                },
            };
        }

        /// <summary>
        /// Une profondeur hors de la roche ferait sortir le noyau de sa table de
        /// paliers au premier pas. Le web ne la borne pas ; une save corrompue y
        /// planterait la boucle, ici elle reprend au plus profond qui existe.
        /// </summary>
        private static int Borne(int profondeur, int minimum) => Math.Max(minimum, Math.Min(profondeur, Constantes.NombreDePaliers));

        /// <summary>Le chemin complet d'une chaîne JSON à un état : enveloppe, migrations, repli.</summary>
        public static EtatJeu DeserialiserTexte(string texte, EtatJeu repli) => Deserialiser(Enveloppe(Json.Lire(texte)), repli);

        private static IReadOnlyList<double> LireNombres(JsonValeur valeur, IReadOnlyList<double> repli)
        {
            if (!(valeur is JsonTableau tableau)) return repli;
            var nombres = new double[tableau.Count];
            for (var i = 0; i < tableau.Count; i += 1)
            {
                // `JSON.stringify` écrit `null` pour un nombre non fini ; on le relit en NaN.
                nombres[i] = Json.NombreOu(tableau[i], double.NaN);
            }
            return nombres;
        }

        private static IReadOnlyList<string> LireTextes(JsonValeur valeur, IReadOnlyList<string> repli)
        {
            if (!(valeur is JsonTableau tableau)) return repli;
            return tableau.Elements.OfType<JsonTexte>().Select(t => t.Valeur).ToArray();
        }

        private static TableOrdonnee<double> LireTableDeNombres(JsonValeur valeur, TableOrdonnee<double> repli)
        {
            if (!(valeur is JsonObjet objet)) return repli;
            var paires = new List<KeyValuePair<string, double>>();
            foreach (var clef in objet.Clefs)
            {
                if (Json.EssayerNombre(objet.Lire(clef), out var n)) paires.Add(new KeyValuePair<string, double>(clef, n));
            }
            return TableOrdonnee<double>.Depuis(paires);
        }

        private static TableOrdonnee<EtatBanc> LireBancs(JsonValeur valeur, TableOrdonnee<EtatBanc> repli)
        {
            if (!(valeur is JsonObjet objet)) return repli;
            var paires = new List<KeyValuePair<string, EtatBanc>>();
            foreach (var clef in objet.Clefs)
            {
                if (!(objet.Lire(clef) is JsonObjet banc)) continue;
                var place = Json.EntierOu(banc.Lire("place"), 0);
                var effectif = Json.NombreOu(banc.Lire("effectif"), 0);
                paires.Add(new KeyValuePair<string, EtatBanc>(clef, new EtatBanc(place, effectif)));
            }
            return TableOrdonnee<EtatBanc>.Depuis(paires);
        }

        private static TableOrdonnee<EntreeDeSucces> LireSucces(JsonValeur valeur, TableOrdonnee<EntreeDeSucces> repli)
        {
            if (!(valeur is JsonObjet objet)) return repli;
            var paires = new List<KeyValuePair<string, EntreeDeSucces>>();
            foreach (var clef in objet.Clefs)
            {
                if (!(objet.Lire(clef) is JsonObjet entree)) continue;
                var cycle = Json.EntierOu(entree.Lire("obtenuAuCycle"), 0);
                RegistresDeVoix.EssayerDeLire(Json.TexteOu(entree.Lire("registre"), "pente"), out var registre);
                paires.Add(new KeyValuePair<string, EntreeDeSucces>(clef, new EntreeDeSucces(cycle, registre)));
            }
            return TableOrdonnee<EntreeDeSucces>.Depuis(paires);
        }

        private static IReadOnlyList<MesureDeCycle> LireCycles(JsonValeur valeur, IReadOnlyList<MesureDeCycle> repli)
        {
            if (!(valeur is JsonTableau tableau)) return repli;
            var cycles = new List<MesureDeCycle>();
            foreach (var element in tableau.Elements)
            {
                if (!(element is JsonObjet c)) continue;
                cycles.Add(new MesureDeCycle
                {
                    Index = Json.EntierOu(c.Lire("index"), 0),
                    DureeEcouleeSecondes = Json.NombreOu(c.Lire("dureeEcouleeSecondes"), 0),
                    SecondesEnRedescente = Json.NombreOu(c.Lire("secondesEnRedescente"), 0),
                    PaliersOuverts = Json.EntierOu(c.Lire("paliersOuverts"), 0),
                    ProductionPicParSeconde = DeserialiserGrandNombre(c.Lire("productionPicParSeconde"), GrandNombre.Zero),
                    FoiGagnee = DeserialiserGrandNombre(c.Lire("foiGagnee"), GrandNombre.Zero),
                });
            }
            return cycles.ToArray();
        }
    }
}
