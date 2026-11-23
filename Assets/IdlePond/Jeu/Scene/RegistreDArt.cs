using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    public sealed record LumiereEmise(int Couleur, double Intensite, int Rayon);

    /// Ce qu'une assise laisse sur le corps (GDD §15.1) : un calque posé à un ancrage, et en
    /// option une teinte de tout le corps (le rouge de la lave) et une lumière (la
    /// luminescence, une décharge).
    public sealed record MarqueDArt(AncrageId Ancrage, IReadOnlyList<int> Couleurs, int? Teinte, LumiereEmise Lumiere);

    public sealed record DecorDArt(
        string Assise,
        IReadOnlyList<double> Lumieres,
        int TeinteDeLumiere,
        // L'eau d'une bande, du haut vers le bas (bible v0.3).
        IReadOnlyList<int> Eau,
        int Vase,
        IReadOnlyList<int> Racines,
        int Rayon,
        bool Berge,
        int Voile,
        // La roche à creuser : le fond, la pierre, son arête claire, les fissures.
        IReadOnlyList<int> Roche,
        // L'eau file : des stries horizontales dans le fond (le Gour, sa contrainte).
        bool Courant = false)
    {
        static readonly int[] AUCUNE = new int[0];
        /// La ligne de lumière de la surface, en haut de la berge.
        public int? Surface { get; init; }
        /// Le bout des racines, qui bourgeonne (la Sève).
        public int? Bourgeon { get; init; }
        /// Les nénuphars posés à la surface : feuille, feuille claire, fleur, cœur.
        public IReadOnlyList<int> Nenuphar { get; init; } = AUCUNE;
        /// Les herbes qui montent de la vase.
        public IReadOnlyList<int> Herbes { get; init; } = AUCUNE;
        /// Ce qui flotte dans l'eau de chaque bande (le pollen de la Sève).
        public IReadOnlyList<int> Grains { get; init; } = AUCUNE;
        /// Les traits du courant, en pointillés.
        public IReadOnlyList<int> Ecume { get; init; } = AUCUNE;
        /// Le lit de galets : trois galets, puis le fond entre eux.
        public IReadOnlyList<int> Galets { get; init; } = AUCUNE;
        /// La mousse sur les galets et les pierres.
        public int? Mousse { get; init; }
        /// Les grosses pierres du fond : la pierre, son dessus éclairé.
        public IReadOnlyList<int> Pierres { get; init; } = AUCUNE;
        /// La faune d'ambiance : les méduses d'eau douce du Gour (Codex §2).
        public IReadOnlyList<int> Meduses { get; init; } = AUCUNE;

        /// Ce qui se dessine par-dessus l'eau de chaque bande : des grains, des méduses, des pierres.
        public bool AUneAmbiance => Grains.Count > 0 || Meduses.Count > 0 || Pierres.Count > 0;

        public IEnumerable<int> Couleurs()
        {
            var toutes = Eau.Concat(Racines).Concat(Roche).Append(Vase).Append(Rayon)
                .Concat(Nenuphar).Concat(Herbes).Concat(Grains).Concat(Ecume).Concat(Galets).Concat(Pierres).Concat(Meduses);
            if (Surface.HasValue) toutes = toutes.Append(Surface.Value);
            if (Bourgeon.HasValue) toutes = toutes.Append(Bourgeon.Value);
            if (Mousse.HasValue) toutes = toutes.Append(Mousse.Value);
            return toutes;
        }
    }

    /// Où une espèce porte son accent : quelques pixels, jamais une lumière (Codex v1.4, §2).
    public enum PoseDAccent { Aucune, Queue, Flanc, Epines, Taches, Bouche, Voile }

    /// Les couleurs d'un poisson. L'accent est une quatrième couleur posée sur quelques pixels ;
    /// l'ombre en a deux, une par image. Le héros seul a des nageoires, un reflet, une joue.
    public sealed record CouleursDePoisson(int Corps, int Dos, int Ventre)
    {
        public int? Accent { get; init; }
        public int? AccentDeLAutreImage { get; init; }
        public PoseDAccent Pose { get; init; }
        public int? Oeil { get; init; }
        public int? Nageoire { get; init; }
        public int? Reflet { get; init; }
        public int? Joue { get; init; }

        public IEnumerable<int> Couleurs() =>
            new int?[] { Corps, Dos, Ventre, Accent, AccentDeLAutreImage, Oeil, Nageoire, Reflet, Joue }
                .Where(c => c.HasValue).Select(c => c.Value);
    }

    /// <summary>
    /// IdlePond — le registre d'art : une assise → son décor, sa courbe de lumière, sa marque
    /// sur le héros (spec DA §1). Ajouter la lave plus tard, c'est ajouter une entrée ici et
    /// des PNG — pas du code. Couleurs en 0xRRGGBB : ce fichier reste pur.
    /// </summary>
    public static class RegistreDArt
    {
        public const int CONTOUR = 0x121C1A;
        /// Le voile et les éclats sont dessinés en blanc et teintés au rendu.
        public const int BLANC = 0xFFFFFF;
        /// Ce qui éclaire encore là où aucune lumière de palier n'arrive.
        public const double LUMIERE_AMBIANTE = 0.15;
        /// La roche à creuser : ce qui descend de l'eau creusée s'y éteint, du haut vers le bas.
        /// Assez pour lire la pierre, jamais assez pour qu'elle ressemble à de l'eau.
        public const double LUMIERE_DE_ROCHE_EN_HAUT = 0.55;
        public const double LUMIERE_DE_ROCHE_EN_BAS = 0.12;
        public const int TEINTE_DE_ROCHE = 0xB8C8C4;

        /// Le héros (bible v0.3) : la seule chose chaude de la mare, en or ; il se détache sur
        /// le vert de la Noue comme sur le bleu du Gour.
        public static readonly CouleursDePoisson HEROS = new CouleursDePoisson(0xF2B84A, 0xC07A2A, 0xFFE6A8)
        {
            Nageoire = 0xE89A3A,
            Reflet = 0xFFF0C0,
            Joue = 0xFF9A80,
        };

        /// Le héros dessiné (bible v0.4) : la palette commune à ses quatre stades, relevée par
        /// `outils/da/heros.py` sur les inspirations — du brun du contour à la crème du ventre,
        /// puis l'œil et son éclat.
        public static readonly IReadOnlyList<int> HEROS_DESSINE = new[]
        {
            0x4A1E0C, 0x88391A, 0xC03709, 0xD74006, 0xDF5307, 0xB46D38, 0xE16B11, 0xEF6908, 0xF27C0C, 0xF78F13,
            0xDA8F42, 0xFAA429, 0xE7A455, 0xFBBA40, 0xF3BF75, 0xFBD27D, 0xFCE2A4, 0x2A1408, 0xFFFBEA,
        };

        /// L'éveil (bible v0.4, d'après da-3) : le turquoise des runes et du voile, la crème du
        /// voile, l'or des étoiles, de l'anneau et de l'aura, l'éclat du cœur des étoiles.
        public static readonly IReadOnlyList<int> EVEIL = new[] { 0x1EA8B8, 0x7FE6E8, 0xFFF4D0, 0xFFE070, 0xF8D070, 0xFFFBEA };

        /// Une couleur par espèce, dans l'ordre des rangs ; au-delà, on reprend du début.
        /// La dérivation des six espèces : Codex v1.4, §2, et `docs/da/gabarits.md`.
        static readonly CouleursDePoisson[] ESPECES_PAR_RANG =
        {
            // le vairon, sa queue-feuille
            new CouleursDePoisson(0x7FC8A0, 0x2E7D5B, 0xE8483A) { Accent = 0xB8E86A, Pose = PoseDAccent.Queue },
            // la loche, ses runes
            new CouleursDePoisson(0xC08A3E, 0x6E4A22, 0xF0D9A0) { Accent = 0x4FE0D0, Pose = PoseDAccent.Flanc },
            // l'épinoche, gorge rouge, épines roses, œil bleu
            new CouleursDePoisson(0x6FA05A, 0x2F5A35, 0xD8283A) { Accent = 0xFF8FA0, Pose = PoseDAccent.Epines, Oeil = 0x4AA8FF },
            // le chabot, son lichen
            new CouleursDePoisson(0x7C8790, 0x4B545C, 0xC9CFC9) { Accent = 0xC8A040, Pose = PoseDAccent.Taches },
            // la lamproie, l'anneau de sa bouche
            new CouleursDePoisson(0x9B7FC0, 0x5A3F7A, 0xD9C8F0) { Accent = 0x9FF0FF, Pose = PoseDAccent.Bouche },
            // l'ombre, son voile : une couleur par image
            new CouleursDePoisson(0xA8B8D8, 0x4A5A8A, 0xE8EEF8) { Accent = 0xC060F0, AccentDeLAutreImage = 0x40D0C0, Pose = PoseDAccent.Voile },
        };

        public static CouleursDePoisson CouleursDEspece(int rang) => ESPECES_PAR_RANG[Math.Max(0, rang) % ESPECES_PAR_RANG.Length];

        /// Les assises qui ont leurs dessins, dans l'ordre. Les autres empruntent la Noue.
        public static readonly IReadOnlyList<string> ASSISES_DESSINEES = new[] { "noue", "gour" };

        static readonly Dictionary<string, DecorDArt> DECORS = new Dictionary<string, DecorDArt>
        {
            // La Noue (bible v0.3) : la Sève, une eau claire et ensoleillée qui s'assombrit vers
            // le fond ; la lumière du jour baisse palier par palier (GDD §15.2 : « la lumière
            // est la variable de progression »).
            ["noue"] = new DecorDArt(
                "noue",
                new[] { 0.85, 0.77, 0.69, 0.61, 0.53, 0.45 },
                0xD8F0DC,
                new[] { 0xA8F0D0, 0x5CC8A0, 0x2F9A80, 0x1E6A62, 0x163E44 },
                0x4A3424,
                new[] { 0x6A4C30 },
                0xFFF0A0,
                true,
                0x8A7A3A,
                new[] { 0x1E2422, 0x2A322F, 0x3A4440, 0x121614 })
            {
                Surface = 0xD8FFF0,
                Bourgeon = 0x8FE070,
                Nenuphar = new[] { 0x4FB85A, 0x6FD070, 0xF4A8C8, 0xFFE070 },
                Herbes = new[] { 0x3FA060, 0x5CC070, 0x7CD070 },
                Grains = new[] { 0xFFE070, 0xF6C840, 0xDFFFA0 },
            },

            // Le Gour (bible v0.3) : le Courant, une eau vive qui plonge vers le bleu nuit. Des
            // galeries noyées, plus de jour : la lumière part sous la dernière de la Noue et
            // descend encore ; un calcaire plus froid et plus sombre ; ni berge, ni racines.
            ["gour"] = new DecorDArt(
                "gour",
                new[] { 0.42, 0.40, 0.38, 0.36, 0.34, 0.32, 0.30, 0.28, 0.26, 0.24, 0.22, 0.20 },
                0xB8D8E0,
                new[] { 0x9ADCF8, 0x5AB0E6, 0x3A80C8, 0x24589A, 0x16345E },
                0x2A3E58,
                new int[0],
                0xE6F8FF,
                false,
                0x5A6A5A,
                new[] { 0x1A1E22, 0x262C31, 0x353D43, 0x101316 },
                Courant: true)
            {
                Ecume = new[] { 0xE6F8FF, 0xBFEAFF },
                Galets = new[] { 0x8FA8C0, 0x6E88A4, 0xA8BCD0, 0x2A3E58 },
                Mousse = 0x4FA070,
                Pierres = new[] { 0x7A90A8, 0x93A8BE },
                Meduses = new[] { 0xE8F4FF, 0xC8E8FF },
            },
        };

        static readonly Dictionary<string, MarqueDArt> MARQUES = new Dictionary<string, MarqueDArt>
        {
            // Les branchies : la première adaptation, sans teinte ni lumière, en vert de Sève.
            ["noue"] = new MarqueDArt(AncrageId.Branchies, new[] { 0x8FE070 }, null, null),
            // Les membranes (GDD §15.1), en bleu de Courant : tendues entre les rayons des
            // nageoires du dos et du ventre, pour tenir dans le courant. L'orange reste.
            ["gour"] = new MarqueDArt(AncrageId.Dos, new[] { 0x8FD8FF }, null, null),
        };

        public static DecorDArt DecorDe(string assise)
        {
            if (assise != null && DECORS.TryGetValue(assise, out var decor)) return decor;
            // Une assise sans dessin : le décor de la Noue, sous la lumière de sa palette —
            // plus bas, plus sombre. La berge n'existe qu'en surface.
            var noue = DECORS[ASSISES_DESSINEES[0]];
            return noue with
            {
                Assise = assise ?? "",
                Lumieres = new[] { 0.5 * Palette.PaletteDe(assise).Lumiere },
                Berge = false,
            };
        }

        /// La lumière propre du héros : chaude, à peine dorée. Elle complète celle de sa bande
        /// jusqu'à 1, pour qu'il reste d'or au fond comme en surface (bible v0.3 : « la seule
        /// chose chaude de la mare »), sans jamais le surexposer près du jour.
        public const int LUMIERE_DU_HEROS = 0xFFF0D8;

        public static double IntensiteDuHeros(double lumiereDeSaBande) =>
            Math.Max(0.0, 1.0 - lumiereDeSaBande - LUMIERE_AMBIANTE);

        public static MarqueDArt MarqueDe(string assise) =>
            assise != null && MARQUES.TryGetValue(assise, out var marque) ? marque : null;

        public static double LumiereDuPalier(int index)
        {
            var assise = Assises.DuPalier(index);
            var lumieres = DecorDe(assise.Id).Lumieres;
            return lumieres[Math.Min(index - assise.IndexPremierPalier, lumieres.Count - 1)];
        }

        public static IReadOnlyList<int> PaletteDuHeros() =>
            HEROS_DESSINE.Append(CONTOUR)
                .Concat(MARQUES.Values.SelectMany(m => m.Couleurs)).Concat(EVEIL).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDesEspeces() =>
            ESPECES_PAR_RANG.SelectMany(c => c.Couleurs()).Append(CONTOUR).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDuDecor(string assise) => DecorDe(assise).Couleurs().Distinct().ToList();
    }
}
