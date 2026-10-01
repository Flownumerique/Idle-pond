using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Jeu
{
    public sealed record SaveSerialisee(int VersionSave, JObject Contenu);

    /// <summary>
    /// Ce que `Charger` rend : l'état, l'instant de la dernière sauvegarde, et ce qu'il
    /// est advenu d'un fichier illisible (son nouveau chemin), sinon null.
    /// </summary>
    public sealed record Chargement(EtatJeu Etat, long DernierInstantMs, bool NouvellePartie, string FichierMisDeCote);

    /// <summary>
    /// IdlePond — persistance : sérialisation Decimal et versionnage de save.
    ///
    /// §10 : versionnage dès la v0.1, avec une chaîne de migrations même vide.
    /// Rétrofiter un versionnage sur des saves existantes coûte un wipe — c'est la
    /// raison d'être de ce fichier au tout premier jalon, avant qu'il y ait quoi que ce
    /// soit à migrer. La sauvegarde Unity repart en version 1 (spec 2026-09-27).
    ///
    /// Tout est écrit champ par champ. Une promenade générique sur l'objet paraîtrait
    /// plus courte, mais Newtonsoft ne sait ni les records ni les `Decimal` du noyau, et
    /// le mappage explicite est ce qui rend l'aller-retour testable — et ce qui permet
    /// de remettre un champ absent à sa valeur par défaut, un champ à la fois.
    ///
    /// Les `Decimal` persistent en chaîne « mantisse e exposant », que `Decimal.Parse`
    /// relit à l'exact. Les `double` persistent eux aussi en CHAÎNES, jamais en nombres
    /// JSON : l'analyseur de Newtonsoft passe par `double.Parse`, qui n'est pas
    /// correctement arrondi sur le runtime Mono d'Unity (voir `AnalyseDouble`). Les entiers
    /// restent des nombres JSON : ils sont exacts.
    ///
    /// Le dossier est INJECTÉ : `Application.persistentDataPath` n'est lu que par le code
    /// Unity, pour que tout ce qui est ici se teste sans moteur.
    /// </summary>
    public static class Persistance
    {
        public const string NOM_DU_FICHIER = "idlepond.json";
        const string SUFFIXE_TEMPORAIRE = ".tmp";

        /// <summary>
        /// Chaîne de migrations. Une entrée par version franchie : `MIGRATIONS[n]`
        /// transforme un contenu de version `n` en contenu de version `n + 1`.
        /// Vide aujourd'hui, et c'est le but : le mécanisme existe avant le besoin.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, Func<JObject, JObject>> MIGRATIONS =
            new Dictionary<int, Func<JObject, JObject>>();

        /* ─── Les nombres ───────────────────────────────────────────────────────────*/

        /// Un Decimal persiste en chaîne : `EnMantisseExposant` fait l'aller-retour à l'exact.
        public static string SerialiserDecimal(Decimal valeur) => valeur.EnMantisseExposant();

        public static Decimal DeserialiserDecimal(JToken valeur, Decimal repli)
        {
            if (valeur == null) return repli;
            try
            {
                Decimal lu;
                if (valeur.Type == JTokenType.String) lu = Decimal.Parse((string)valeur);
                else if (TenterNombre(valeur, out var x)) lu = new Decimal(x);
                else return repli;
                return double.IsNaN(lu.Mantisse) ? repli : lu;
            }
            catch (Exception)
            {
                // Une chaîne que `Parse` refuse, ou dont l'exposant sort de sa table : peu
                // importe la cause, le repli vaut mieux qu'une partie qui ne se charge pas.
                return repli;
            }
        }

        /// Le « R » du Mono d'Unity ne garantit pas l'aller-retour : on le vérifie avec un
        /// lecteur correctement arrondi, et G17 prend le relais s'il échoue.
        static string EcrireDouble(double x)
        {
            var court = x.ToString("R", CultureInfo.InvariantCulture);
            return AnalyseDouble.EssayerDeLire(court, out var relu) && relu.Equals(x)
                ? court
                : x.ToString("G17", CultureInfo.InvariantCulture);
        }

        static JValue N(double x) => new JValue(EcrireDouble(x));

        static JArray TableauDeNombres(IEnumerable<double> valeurs) => new JArray(valeurs.Select(N));

        static JArray Textes(IEnumerable<string> valeurs) => new JArray(valeurs.Select(v => new JValue(v)));

        /* ─── La lecture, champ par champ ───────────────────────────────────────────*/

        /// Un nombre fini, écrit en chaîne (notre format) ou en nombre JSON (un fichier
        /// édité à la main). NaN et les infinis sont refusés : aucun champ d'état n'en veut.
        static bool TenterNombre(JToken t, out double x)
        {
            x = 0;
            if (t == null) return false;
            if (t.Type == JTokenType.String) return AnalyseDouble.EssayerDeLire((string)t, out x) && EstFini(x);
            if (t.Type == JTokenType.Integer)
            {
                switch (((JValue)t).Value)
                {
                    case long l: x = l; return true;
                    case int i: x = i; return true;
                    case BigInteger b: x = (double)b; return EstFini(x);
                    default: return false;
                }
            }
            if (t.Type == JTokenType.Float) { x = (double)t; return EstFini(x); }
            return false;
        }

        static bool EstFini(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        static double Nombre(JToken t, double repli) => TenterNombre(t, out var x) ? x : repli;

        static bool TenterEntier(JToken t, out long n)
        {
            n = 0;
            if (t == null || t.Type != JTokenType.Integer) return false;
            switch (((JValue)t).Value)
            {
                case long l: n = l; return true;
                case int i: n = i; return true;
                default: return false;
            }
        }

        static int Entier(JToken t, int repli) =>
            TenterEntier(t, out var n) && n >= int.MinValue && n <= int.MaxValue ? (int)n : repli;

        static bool Booleen(JToken t, bool repli) => t != null && t.Type == JTokenType.Boolean ? (bool)t : repli;

        static JObject Objet(JToken t) => t as JObject ?? new JObject();

        static IReadOnlyList<string> ListeDeTextes(JToken t, IReadOnlyList<string> repli) =>
            t is JArray tableau
                ? tableau.Where(e => e.Type == JTokenType.String).Select(e => (string)e).ToArray()
                : repli;

        /* ─── Sérialisation ─────────────────────────────────────────────────────────*/

        public static SaveSerialisee Serialiser(EtatJeu etat)
        {
            var c = etat.Cycle;
            var p = etat.Permanent;
            var t = etat.Telemetrie;

            var especes = new JObject();
            foreach (var e in Especes.Toutes)
                if (c.Especes.TryGetValue(e.Id, out var v))
                    especes[e.Id] = new JObject { ["debloquee"] = v.Debloquee, ["niveau"] = v.Niveau };

            var succes = new JObject();
            foreach (var s in RegistreDesSucces.Tous)
                if (p.Succes.TryGetValue(s.Id, out var entree))
                    succes[s.Id] = new JObject { ["obtenuAuCycle"] = entree.ObtenuAuCycle, ["registre"] = NomDeRegistre(entree.Registre) };

            var insufflations = new JObject();
            foreach (var i in Insufflations.Toutes)
                if (p.Insufflations.TryGetValue(i.Id, out var rang))
                    insufflations[i.Id] = rang;

            var compteurs = new JObject();
            foreach (BrancheTechnique branche in Enum.GetValues(typeof(BrancheTechnique)))
                if (p.CompteursTechnique.TryGetValue(branche, out var compteur))
                    compteurs[NomDeBranche(branche)] = N(compteur);

            // Le réglage n'est PAS écrit (R41) : il appartient à la version du jeu, pas à
            // la partie. Un réglage persisté figerait l'ancienne courbe dans les saves
            // existantes au moment même où l'on recalibre.
            var contenu = new JObject
            {
                ["prng"] = new JObject { ["graine"] = etat.Prng.Graine },
                ["tempsJeuSecondes"] = N(etat.TempsJeuSecondes),
                ["limiteDeContenu"] = etat.LimiteDeContenu,
                ["cycle"] = new JObject
                {
                    ["manaCourant"] = SerialiserDecimal(c.ManaCourant),
                    ["paliersOuverts"] = c.PaliersOuverts,
                    ["especes"] = especes,
                    ["productionPicParSeconde"] = SerialiserDecimal(c.ProductionPicParSeconde),
                    ["dureeSecondes"] = N(c.DureeSecondes),
                    ["acquisDeSejour"] = N(c.AcquisDeSejour),
                    ["niveauDuHeros"] = c.NiveauDuHeros,
                },
                ["permanent"] = new JObject
                {
                    ["densites"] = TableauDeNombres(p.Densites),
                    ["souffle"] = SerialiserDecimal(p.Souffle),
                    ["contenanceMana"] = SerialiserDecimal(p.ContenanceMana),
                    ["couches"] = Textes(p.Couches),
                    ["profondeurMaxAtteinte"] = p.ProfondeurMaxAtteinte,
                    ["compteursTechnique"] = compteurs,
                    ["noeudsTechnique"] = Textes(p.NoeudsTechnique),
                    ["succes"] = succes,
                    ["nombreDeRenaissances"] = p.NombreDeRenaissances,
                    ["especesAyantAtteintCent"] = Textes(p.EspecesAyantAtteintCent),
                    ["manaAmbiant"] = SerialiserDecimal(p.ManaAmbiant),
                    ["heuresHorsLigneCreditees"] = N(p.HeuresHorsLigneCreditees),
                    ["insufflations"] = insufflations,
                },
                ["telemetrie"] = new JObject
                {
                    ["cycles"] = new JArray(t.Cycles.Select(m => new JObject
                    {
                        ["index"] = m.Index,
                        ["dureeEcouleeSecondes"] = N(m.DureeEcouleeSecondes),
                        ["paliersOuverts"] = m.PaliersOuverts,
                        ["productionPicParSeconde"] = SerialiserDecimal(m.ProductionPicParSeconde),
                        ["souffleGagne"] = SerialiserDecimal(m.SouffleGagne),
                    })),
                    ["secondesDepuisDernierSucces"] = N(t.SecondesDepuisDernierSucces),
                    ["intervallesEntreSucces"] = TableauDeNombres(t.IntervallesEntreSucces),
                },
            };
            return new SaveSerialisee(etat.VersionSave, contenu);
        }

        static string NomDeRegistre(PalierDeVoix palier) => palier.ToString().ToLowerInvariant();

        static string NomDeBranche(BrancheTechnique branche) => branche.ToString().ToLowerInvariant();

        /// <summary>
        /// Le fichier tout entier : la save, et `dernierInstantMs` à côté de l'état, comme
        /// avant — c'est de lui que le crédit hors ligne part au retour.
        /// </summary>
        public static JObject ComposerLeFichier(SaveSerialisee save, long dernierInstantMs) =>
            new JObject
            {
                ["versionSave"] = save.VersionSave,
                ["contenu"] = save.Contenu,
                ["dernierInstantMs"] = dernierInstantMs,
            };

        /* ─── Migration ─────────────────────────────────────────────────────────────*/

        public static JObject Migrer(SaveSerialisee save)
        {
            // Une save plus récente que le jeu ne se lit pas « au mieux » : ses champs ont
            // peut-être changé de sens, et la charger à moitié en coûterait autant qu'un wipe.
            if (save.VersionSave > Constantes.VERSION_SAVE)
                throw new InvalidOperationException(
                    $"Save d'une version plus récente que le jeu : {save.VersionSave} > {Constantes.VERSION_SAVE}");
            var contenu = save.Contenu;
            for (var version = save.VersionSave; version < Constantes.VERSION_SAVE; version++)
            {
                if (!MIGRATIONS.TryGetValue(version, out var migration))
                    throw new InvalidOperationException($"Migration de save manquante : version {version} → {version + 1}");
                contenu = migration(contenu);
            }
            return contenu;
        }

        /* ─── Désérialisation ───────────────────────────────────────────────────────*/

        public static EtatJeu Deserialiser(SaveSerialisee save, EtatJeu repli)
        {
            var brut = Migrer(save);
            var cycle = Objet(brut["cycle"]);
            var permanent = Objet(brut["permanent"]);
            var telemetrie = Objet(brut["telemetrie"]);

            var cycleRepli = repli.Cycle;
            var permanentRepli = repli.Permanent;
            var telemetrieRepli = repli.Telemetrie;

            // Dans l'ordre du registre, jamais dans celui du fichier : deux parties de même
            // contenu doivent se sérialiser à l'identique, quel que soit l'ordre des clefs.
            IReadOnlyDictionary<string, EtatEspece> especes = cycleRepli.Especes;
            if (cycle["especes"] is JObject tableEspeces)
            {
                var lues = new Dictionary<string, EtatEspece>();
                foreach (var e in Especes.Toutes)
                    if (tableEspeces[e.Id] is JObject v)
                        lues[e.Id] = new EtatEspece(Booleen(v["debloquee"], false), Entier(v["niveau"], 0));
                especes = lues;
            }

            var renaissances = Entier(permanent["nombreDeRenaissances"], permanentRepli.NombreDeRenaissances);

            IReadOnlyDictionary<string, EntreeDeSucces> succes = permanentRepli.Succes;
            if (permanent["succes"] is JObject tableSucces)
            {
                var lus = new Dictionary<string, EntreeDeSucces>();
                foreach (var s in RegistreDesSucces.Tous)
                    if (tableSucces[s.Id] is JObject v)
                        lus[s.Id] = new EntreeDeSucces(
                            Entier(v["obtenuAuCycle"], renaissances),
                            // Un registre illisible reçoit le palier que les franchissements de
                            // la save impliquent : aucune autre valeur ne serait plus vraie.
                            Enum.TryParse<PalierDeVoix>(v["registre"]?.Type == JTokenType.String ? (string)v["registre"] : "", true, out var registre)
                                ? registre
                                : Voix.PalierApres(renaissances));
                succes = lus;
            }

            IReadOnlyDictionary<string, int> insufflations = permanentRepli.Insufflations;
            if (permanent["insufflations"] is JObject tableInsufflations)
            {
                var lues = new Dictionary<string, int>();
                foreach (var i in Insufflations.Toutes)
                {
                    var rang = Entier(tableInsufflations[i.Id], 0);
                    if (rang > 0) lues[i.Id] = rang;
                }
                insufflations = lues;
            }

            IReadOnlyDictionary<BrancheTechnique, double> compteurs = permanentRepli.CompteursTechnique;
            if (permanent["compteursTechnique"] is JObject tableCompteurs)
            {
                var lus = new Dictionary<BrancheTechnique, double>();
                foreach (BrancheTechnique branche in Enum.GetValues(typeof(BrancheTechnique)))
                    lus[branche] = Nombre(tableCompteurs[NomDeBranche(branche)],
                        permanentRepli.CompteursTechnique.TryGetValue(branche, out var defaut) ? defaut : 0);
                compteurs = lus;
            }

            // Un tableau de densités de la mauvaise longueur ne ferait que décaler les
            // paliers : on part du repli et l'on y écrit ce que la save porte.
            var densites = permanentRepli.Densites.ToArray();
            if (permanent["densites"] is JArray tableauDeDensites)
                for (var i = 0; i < densites.Length && i < tableauDeDensites.Count; i++)
                    densites[i] = Nombre(tableauDeDensites[i], densites[i]);

            IReadOnlyList<MesureDeCycle> cycles = telemetrieRepli.Cycles;
            if (telemetrie["cycles"] is JArray tableauDeCycles)
                cycles = tableauDeCycles.OfType<JObject>().Select(m => new MesureDeCycle(
                    Index: Entier(m["index"], 0),
                    DureeEcouleeSecondes: Nombre(m["dureeEcouleeSecondes"], 0),
                    PaliersOuverts: Entier(m["paliersOuverts"], 0),
                    ProductionPicParSeconde: DeserialiserDecimal(m["productionPicParSeconde"], Decimal.Zero),
                    SouffleGagne: DeserialiserDecimal(m["souffleGagne"], Decimal.Zero))).ToArray();

            IReadOnlyList<double> intervalles = telemetrieRepli.IntervallesEntreSucces;
            if (telemetrie["intervallesEntreSucces"] is JArray tableauDIntervalles)
                intervalles = tableauDIntervalles.Where(e => TenterNombre(e, out _)).Select(e => Nombre(e, 0)).ToArray();

            var graine = TenterEntier(Objet(brut["prng"])["graine"], out var g) && g >= 0 && g <= uint.MaxValue
                ? new EtatPrng((uint)g)
                : repli.Prng;

            return new EtatJeu(
                VersionSave: Constantes.VERSION_SAVE,
                Prng: graine,
                TempsJeuSecondes: Nombre(brut["tempsJeuSecondes"], repli.TempsJeuSecondes),
                // Une save d'un jalon antérieur reprend la limite du jalon courant : une
                // assise livrée depuis ne doit pas rester fermée à qui jouait déjà.
                LimiteDeContenu: Entier(brut["limiteDeContenu"], repli.LimiteDeContenu),
                // JAMAIS lu de la save (R41) : un réglage appartient à la version du jeu,
                // pas à la partie. `Serialiser` ne l'écrit pas ; s'il traînait dans un
                // vieux fichier, on l'ignorerait quand même.
                Reglage: repli.Reglage,
                Cycle: new EtatCycle(
                    ManaCourant: DeserialiserDecimal(cycle["manaCourant"], cycleRepli.ManaCourant),
                    PaliersOuverts: Entier(cycle["paliersOuverts"], cycleRepli.PaliersOuverts),
                    Especes: especes,
                    ProductionPicParSeconde: DeserialiserDecimal(cycle["productionPicParSeconde"], cycleRepli.ProductionPicParSeconde),
                    DureeSecondes: Nombre(cycle["dureeSecondes"], cycleRepli.DureeSecondes),
                    AcquisDeSejour: Nombre(cycle["acquisDeSejour"], cycleRepli.AcquisDeSejour),
                    NiveauDuHeros: Entier(cycle["niveauDuHeros"], cycleRepli.NiveauDuHeros)),
                Permanent: new EtatPermanent(
                    Densites: densites,
                    Souffle: DeserialiserDecimal(permanent["souffle"], permanentRepli.Souffle),
                    ContenanceMana: DeserialiserDecimal(permanent["contenanceMana"], permanentRepli.ContenanceMana),
                    Couches: ListeDeTextes(permanent["couches"], permanentRepli.Couches),
                    ProfondeurMaxAtteinte: Entier(permanent["profondeurMaxAtteinte"], permanentRepli.ProfondeurMaxAtteinte),
                    CompteursTechnique: compteurs,
                    NoeudsTechnique: ListeDeTextes(permanent["noeudsTechnique"], permanentRepli.NoeudsTechnique),
                    Succes: succes,
                    NombreDeRenaissances: renaissances,
                    EspecesAyantAtteintCent: ListeDeTextes(permanent["especesAyantAtteintCent"], permanentRepli.EspecesAyantAtteintCent),
                    ManaAmbiant: DeserialiserDecimal(permanent["manaAmbiant"], permanentRepli.ManaAmbiant),
                    HeuresHorsLigneCreditees: Nombre(permanent["heuresHorsLigneCreditees"], permanentRepli.HeuresHorsLigneCreditees),
                    Insufflations: insufflations),
                Telemetrie: new EtatTelemetrie(
                    Cycles: cycles,
                    SecondesDepuisDernierSucces: Nombre(telemetrie["secondesDepuisDernierSucces"], telemetrieRepli.SecondesDepuisDernierSucces),
                    IntervallesEntreSucces: intervalles));
        }

        /* ─── Le fichier ────────────────────────────────────────────────────────────*/

        public static string CheminDeLaSauvegarde(string dossier) => Path.Combine(dossier, NOM_DU_FICHIER);

        /// <summary>
        /// Écrit la sauvegarde par un fichier temporaire puis un remplacement : une coupure
        /// au milieu de l'écriture laisse un `.tmp` bancal, jamais une sauvegarde
        /// tronquée — la précédente reste en place, intacte, jusqu'au remplacement.
        /// </summary>
        public static void Sauvegarder(string dossier, EtatJeu etat, long dernierInstantMs)
        {
            Directory.CreateDirectory(dossier);
            var cible = CheminDeLaSauvegarde(dossier);
            var temporaire = cible + SUFFIXE_TEMPORAIRE;
            var texte = ComposerLeFichier(Serialiser(etat), dernierInstantMs).ToString(Formatting.Indented);
            // Sans BOM : un fichier JSON qui en porte un déroute certains lecteurs.
            File.WriteAllText(temporaire, texte, new UTF8Encoding(false));
            if (File.Exists(cible)) File.Replace(temporaire, cible, null);
            else File.Move(temporaire, cible);
        }

        /// <summary>
        /// Charge la sauvegarde du dossier. Absente : une nouvelle partie. Illisible — JSON
        /// tronqué, version inconnue, trou dans la chaîne de migrations : une nouvelle
        /// partie aussi, et le fichier fautif est renommé en
        /// `idlepond.corrompue-&lt;horodatage&gt;.json` au lieu d'être écrasé. Une save
        /// illisible ne doit pas bloquer le jeu sur un écran blanc, ni détruire ce que le
        /// joueur pourrait encore faire relire.
        /// </summary>
        public static Chargement Charger(string dossier, IHorloge horloge)
        {
            var maintenant = horloge.MaintenantMs();
            var repli = Partie.NouvelEtat(horloge);
            var chemin = CheminDeLaSauvegarde(dossier);
            if (!File.Exists(chemin)) return new Chargement(repli, maintenant, true, null);

            try
            {
                var racine = Lire(File.ReadAllText(chemin, Encoding.UTF8));
                if (!TenterEntier(racine["versionSave"], out var version) || version < int.MinValue || version > int.MaxValue)
                    throw new InvalidDataException("versionSave absente ou illisible");
                if (!(racine["contenu"] is JObject contenu))
                    throw new InvalidDataException("contenu absent ou illisible");
                var etat = Deserialiser(new SaveSerialisee((int)version, contenu), repli);
                // Absent, `dernierInstantMs` vaut maintenant : on ne crédite pas une absence
                // dont on ne connaît pas le début.
                var dernier = TenterEntier(racine["dernierInstantMs"], out var instant) ? instant : maintenant;
                return new Chargement(etat, dernier, false, null);
            }
            catch (Exception)
            {
                // Toute cause : JSON tronqué, version inconnue, champ du mauvais type, fichier
                // illisible. Une save qu'on ne sait pas lire est une save qu'on met de côté.
                return new Chargement(repli, maintenant, true, MettreDeCote(chemin, maintenant));
            }
        }

        /// Dates en chaînes : un texte qui ressemble à une date reste un texte.
        static JObject Lire(string texte)
        {
            using var lecteur = new JsonTextReader(new StringReader(texte)) { DateParseHandling = DateParseHandling.None };
            var jeton = JToken.ReadFrom(lecteur);
            return jeton as JObject ?? throw new InvalidDataException("la racine n'est pas un objet JSON");
        }

        /// Renomme le fichier fautif. Si le renommage échoue (fichier verrouillé), on rend
        /// null : la partie neuve démarre quand même, et le fichier sera remplacé à la
        /// prochaine sauvegarde.
        static string MettreDeCote(string chemin, long instantMs)
        {
            var dossier = Path.GetDirectoryName(chemin);
            var horodatage = DateTimeOffset.FromUnixTimeMilliseconds(instantMs).UtcDateTime
                .ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            var destination = Path.Combine(dossier, $"idlepond.corrompue-{horodatage}.json");
            // Deux sauvegardes illisibles dans la même milliseconde : la seconde ne doit pas
            // écraser la première.
            for (var n = 2; File.Exists(destination); n++)
                destination = Path.Combine(dossier, $"idlepond.corrompue-{horodatage}-{n}.json");
            try
            {
                File.Move(chemin, destination);
                return destination;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
