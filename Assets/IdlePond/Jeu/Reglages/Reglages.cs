using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IdlePond.Jeu
{
    /// Le format de l'interface. `Auto` laisse `Adaptation.Detecter` choisir.
    public enum FormatDAffichage { Auto, Telephone, Tablette, Pc }

    /// L'écriture des grands nombres : `1.25 M`, `1.25e+6`, ou l'exposant multiple de 3.
    public enum Notation { Suffixes, Scientifique, Ingenieur }

    public enum LimiteDImages { Trente, Soixante, Illimitee }

    public enum Canal { Musique, Effets }

    /// <summary>
    /// IdlePond — les réglages du joueur (spec du 2026-10-08). Un record immuable, comme
    /// l'état du jeu : on ne le modifie pas, on en fait un autre (`with`), et le magasin le
    /// remplace. PUR : ni moteur ni fichier ici, pour qu'il se teste sans Unity.
    ///
    /// Il ne contient rien de la partie : réinitialiser la partie ne touche pas aux
    /// réglages, et un réglage n'est jamais une règle du jeu.
    ///
    /// Le fichier se relit sans jamais lever. Un champ absent ou du mauvais type prend son
    /// défaut, les autres sont gardés ; un fichier qu'on ne sait pas lire du tout (tronqué,
    /// version future) rend tous les défauts. Perdre un volume vaut mieux qu'un jeu qui ne
    /// démarre pas.
    /// </summary>
    public sealed record Reglages
    {
        public const int VERSION = 1;
        public const double TAILLE_MIN = 0.9;
        public const double TAILLE_MAX = 1.3;
        /// Le pas du curseur de taille, en inverse : 20 crans par unité, soit 5 %.
        const double CRANS_DE_TAILLE = 20;

        public double VolumeGeneral { get; init; } = 0.8;
        public double VolumeMusique { get; init; } = 0.7;
        public double VolumeEffets { get; init; } = 0.8;
        public bool Muet { get; init; }
        public bool MuetEnArrierePlan { get; init; } = true;

        public FormatDAffichage Affichage { get; init; } = FormatDAffichage.Auto;
        public double TailleDInterface { get; init; } = 1.0;
        /// Null tant que le joueur n'a pas choisi : on garde la fenêtre telle qu'elle s'ouvre.
        public bool? PleinEcran { get; init; }
        /// Null tant que le joueur n'a pas choisi : 60 sur PC, 30 sur mobile.
        public LimiteDImages? Images { get; init; }

        public Notation Notation { get; init; } = Notation.Suffixes;
        public bool AnnoncesAffichees { get; init; } = true;

        public bool MouvementReduit { get; init; }
        public bool ContrasteRenforce { get; init; }

        public static readonly Reglages ParDefaut = new Reglages();

        /// Chaque valeur ramenée dans ses bornes ; ce qui n'a pas de sens prend son défaut.
        public Reglages Borner()
        {
            var d = ParDefaut;
            return this with
            {
                VolumeGeneral = Volume(VolumeGeneral, d.VolumeGeneral),
                VolumeMusique = Volume(VolumeMusique, d.VolumeMusique),
                VolumeEffets = Volume(VolumeEffets, d.VolumeEffets),
                TailleDInterface = Taille(TailleDInterface),
                Affichage = Enum.IsDefined(typeof(FormatDAffichage), Affichage) ? Affichage : d.Affichage,
                Notation = Enum.IsDefined(typeof(Notation), Notation) ? Notation : d.Notation,
                Images = Images.HasValue && Enum.IsDefined(typeof(LimiteDImages), Images.Value) ? Images : null,
            };
        }

        static double Volume(double v, double repli) => double.IsNaN(v) ? repli : Math.Max(0, Math.Min(1, v));

        static double Taille(double t)
        {
            if (double.IsNaN(t)) return ParDefaut.TailleDInterface;
            var bornee = Math.Max(TAILLE_MIN, Math.Min(TAILLE_MAX, t));
            return Math.Round(bornee * CRANS_DE_TAILLE, MidpointRounding.AwayFromZero) / CRANS_DE_TAILLE;
        }

        /* ─── Le fichier ──────────────────────────────────────────────────────────── */

        public static string Serialiser(Reglages r) => new JObject
        {
            ["version"] = VERSION,
            ["volumeGeneral"] = r.VolumeGeneral,
            ["volumeMusique"] = r.VolumeMusique,
            ["volumeEffets"] = r.VolumeEffets,
            ["muet"] = r.Muet,
            ["muetEnArrierePlan"] = r.MuetEnArrierePlan,
            ["affichage"] = r.Affichage.ToString(),
            ["tailleDInterface"] = r.TailleDInterface,
            ["pleinEcran"] = r.PleinEcran.HasValue ? new JValue(r.PleinEcran.Value) : JValue.CreateNull(),
            ["images"] = r.Images.HasValue ? new JValue(r.Images.Value.ToString()) : JValue.CreateNull(),
            ["notation"] = r.Notation.ToString(),
            ["annoncesAffichees"] = r.AnnoncesAffichees,
            ["mouvementReduit"] = r.MouvementReduit,
            ["contrasteRenforce"] = r.ContrasteRenforce,
        }.ToString(Formatting.Indented);

        /// <summary>
        /// `lisible` est faux quand le fichier entier a été écarté (et les défauts rendus) :
        /// l'appelant le dit dans la console. Un champ isolé qu'on ne comprend pas, lui,
        /// prend son défaut sans bruit.
        /// </summary>
        public static Reglages Deserialiser(string texte, out bool lisible)
        {
            lisible = false;
            JObject o;
            try
            {
                using var lecteur = new JsonTextReader(new StringReader(texte ?? "")) { DateParseHandling = DateParseHandling.None };
                o = JToken.ReadFrom(lecteur) as JObject;
            }
            catch (JsonException)
            {
                return ParDefaut;
            }
            if (o == null) return ParDefaut;
            var version = o["version"];
            // Un entier trop grand pour un `long` arrive en BigInteger : on compare sans convertir.
            if (version == null || version.Type != JTokenType.Integer || !(((JValue)version).Value is long numero)
                || numero < 1 || numero > VERSION)
                return ParDefaut;

            lisible = true;
            var d = ParDefaut;
            return new Reglages
            {
                VolumeGeneral = Nombre(o["volumeGeneral"], d.VolumeGeneral),
                VolumeMusique = Nombre(o["volumeMusique"], d.VolumeMusique),
                VolumeEffets = Nombre(o["volumeEffets"], d.VolumeEffets),
                Muet = Booleen(o["muet"], d.Muet),
                MuetEnArrierePlan = Booleen(o["muetEnArrierePlan"], d.MuetEnArrierePlan),
                Affichage = Enumere(o["affichage"], d.Affichage),
                TailleDInterface = Nombre(o["tailleDInterface"], d.TailleDInterface),
                PleinEcran = o["pleinEcran"]?.Type == JTokenType.Boolean ? (bool)o["pleinEcran"] : (bool?)null,
                Images = o["images"]?.Type == JTokenType.String ? Enumere(o["images"], LimiteDImages.Soixante) : (LimiteDImages?)null,
                Notation = Enumere(o["notation"], d.Notation),
                AnnoncesAffichees = Booleen(o["annoncesAffichees"], d.AnnoncesAffichees),
                MouvementReduit = Booleen(o["mouvementReduit"], d.MouvementReduit),
                ContrasteRenforce = Booleen(o["contrasteRenforce"], d.ContrasteRenforce),
            }.Borner();
        }

        static double Nombre(JToken t, double repli) =>
            t != null && (t.Type == JTokenType.Float || t.Type == JTokenType.Integer) ? (double)t : repli;

        static bool Booleen(JToken t, bool repli) => t != null && t.Type == JTokenType.Boolean ? (bool)t : repli;

        /// Par son nom seulement : « 2 » n'est pas un format, même si l'énuméré en a un troisième.
        static T Enumere<T>(JToken t, T repli) where T : struct, Enum
        {
            if (t == null || t.Type != JTokenType.String) return repli;
            var nom = (string)t;
            foreach (var valeur in (T[])Enum.GetValues(typeof(T)))
                if (valeur.ToString() == nom) return valeur;
            return repli;
        }
    }
}
