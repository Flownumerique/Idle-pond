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

    /// De quoi sont faites les parois d'un lieu (2026-11-26) : des pierres jointes (le défaut,
    /// pour les assises sans dessin), une berge de terre en lits où des galets sont pris et
    /// d'où sortent des racines (la Noue), un calcaire en lits épais, que l'eau croûte de calcite et
    /// qui retient des flaques sur ses corniches — des gours (le Gour). Préparées en avance
    /// (2026-11-26) : le calcaire à fossiles et veines d'éclat de l'Aven, la glace à facettes
    /// de la fosse, le basalte en colonnes veiné de lave de la faille chaude, le sel en lits et en
    /// cubes de la mer relique.
    public enum MatiereDeParoi { Pierres, Terre, Calcaire, Fossiles, Glace, Basalte, Sel }

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
        // La roche à creuser et les parois : le fond, la pierre, son arête claire, les fissures
        // (dans la terre : deux lits, le limon clair, l'humus sombre).
        IReadOnlyList<int> Roche,
        // L'eau file : des traits d'écume dans chaque bande (le Gour, sa contrainte), dessinés
        // dans l'ambiance, et les filets qui la traversent (`Eau.Filet`).
        bool Courant = false)
    {
        static readonly int[] AUCUNE = new int[0];
        /// La ligne de lumière de la surface, en haut de la berge.
        public int? Surface { get; init; }
        /// La teinte de la lumière au DERNIER palier du lieu (2026-11-25) : l'eau change de
        /// couleur en descendant, de `TeinteDeLumiere` en haut jusqu'à celle-ci au fond. Rien :
        /// la même du haut en bas.
        public int? TeinteDuFond { get; init; }
        /// Comment le lieu se raccorde à celui d'au-dessus : en fondu (le défaut), ou par une
        /// cassure — un seuil de roche, l'eau et la lumière qui changent net (le Gour, sa vasque).
        public JonctionDeLieu Jonction { get; init; } = JonctionDeLieu.Fondu;
        /// La couleur de la magie du lieu, quand le héros n'en porte pas encore la marque : celle de
        /// sa membrane et de ses lueurs (Codex §2, « Les six magies »).
        public int? Magie { get; init; }
        /// La matière des parois, des corniches, de la roche à creuser et du seuil.
        public MatiereDeParoi Paroi { get; init; } = MatiereDeParoi.Pierres;
        /// Ce que la matière porte en plus de ses quatre couleurs, le sombre puis le clair : les
        /// galets pris dans la terre ; la calcite qui croûte le calcaire. Deux au moins, sauf
        /// pour les pierres.
        public IReadOnlyList<int> Inclusions { get; init; } = AUCUNE;
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

        /// Ce qui flotte dans l'eau de chaque bande : des grains, des méduses, les traits du
        /// courant. (Les grosses pierres sont sur le sol du fond du puits, `lit.png`, depuis le
        /// 2026-11-25.)
        public bool AUneAmbiance => Grains.Count > 0 || Meduses.Count > 0 || (Courant && Ecume.Count > 0);

        /// L'eau d'un lieu, du haut vers le bas, avec entre deux couleurs voisines
        /// `RegistreDArt.NUANCES_D_EAU - 1` couleurs intermédiaires (2026-11-26) : le dégradé du
        /// puits se trame d'une nuance à la suivante, sans aplat ni marche visible.
        public IReadOnlyList<int> EauFine
        {
            get
            {
                var fine = new List<int>();
                for (var i = 0; i + 1 < Eau.Count; i++)
                    for (var k = 0; k < RegistreDArt.NUANCES_D_EAU; k++)
                        fine.Add(RegistreDArt.Melanger(Eau[i], Eau[i + 1], k / (double)RegistreDArt.NUANCES_D_EAU));
                if (Eau.Count > 0) fine.Add(Eau[Eau.Count - 1]);
                return fine;
            }
        }

        public IEnumerable<int> Couleurs()
        {
            var toutes = EauFine.Concat(Racines).Concat(Roche).Append(Vase).Append(Rayon)
                .Concat(Nenuphar).Concat(Herbes).Concat(Grains).Concat(Ecume).Concat(Galets).Concat(Pierres).Concat(Meduses).Concat(Inclusions);
            if (Surface.HasValue) toutes = toutes.Append(Surface.Value);
            if (Bourgeon.HasValue) toutes = toutes.Append(Bourgeon.Value);
            if (Mousse.HasValue) toutes = toutes.Append(Mousse.Value);
            if (Magie.HasValue) toutes = toutes.Append(Magie.Value);
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
        /// Le pas du dégradé de l'eau entre deux couleurs du registre (`DecorDArt.EauFine`).
        public const int NUANCES_D_EAU = 4;

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
        /// puis l'œil et son éclat (relevée à l'échelle de 360 px, 2026-11-24).
        public static readonly IReadOnlyList<int> HEROS_DESSINE = new[]
        {
            0x4A1E0C, 0x722813, 0xB2340C, 0xD23906, 0xDD4D06, 0xE4660B, 0xBE6A2F, 0xF06808, 0xF37C0C, 0xE78D2D,
            0xFA951A, 0xFBAB2D, 0xE9A557, 0xFCC045, 0xF5C57A, 0xFCD981, 0xFCE4AE, 0x2A1408, 0xFFFBEA,
            // Le contour doux de l'œuf et de la larve, translucides (le renouveau).
            0xB46D38,
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

        /// Les assises qui ont leurs dessins, dans l'ordre — jouables, avec la marque du héros. Les
        /// autres empruntent la Noue.
        public static readonly IReadOnlyList<string> ASSISES_DESSINEES = new[] { "noue", "gour" };

        /// Les lieux dont le décor est dessiné, dans l'ordre (2026-11-26) : les assises dessinées,
        /// puis les quatre lieux à venir, préparés en avance — eau, parois, corniches, fond, roche,
        /// membrane. Leurs identifiants restent ceux du noyau tant qu'ils ne sont pas livrés :
        /// `assise-3` est l'Aven.
        public static readonly IReadOnlyList<string> DECORS_DESSINES = new[] { "noue", "gour", "assise-3", "assise-4", "assise-5", "assise-6" };

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
                new[] { 0x3E2C20, 0x503A2A, 0x6E5238, 0x24180F })
            {
                Surface = 0xD8FFF0,
                // Une berge de terre brune (2026-11-26) : la Noue est un fossé creusé dans la
                // prairie, pas une carrière. Des galets beiges y sont pris.
                Paroi = MatiereDeParoi.Terre,
                Inclusions = new[] { 0x7C705E, 0x9C8E74 },
                // Du jour vert-doré de la surface au vert d'eau froid du fond, qui annonce le Gour.
                TeinteDuFond = 0xB4E0DC,
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
                new[] { 0x34404C, 0x43505E, 0x5E6E80, 0x1A2028 },
                Courant: true)
            {
                // Un calcaire gris-bleu en lits épais (2026-11-26), croûté de calcite : le gour est le
                // bassin que l'eau calcaire se bâtit à elle-même.
                Paroi = MatiereDeParoi.Calcaire,
                Inclusions = new[] { 0x8CA2B6, 0xB8CCDA },
                // Du bleu vif de la vasque à l'indigo des galeries : la luminescence vient dessous.
                TeinteDuFond = 0xA0A8EC,
                Jonction = JonctionDeLieu.Cassure,
                Ecume = new[] { 0xE6F8FF, 0xBFEAFF },
                Galets = new[] { 0x8FA8C0, 0x6E88A4, 0xA8BCD0, 0x2A3E58 },
                Mousse = 0x4FA070,
                Pierres = new[] { 0x7A90A8, 0x93A8BE },
                Meduses = new[] { 0xE8F4FF, 0xC8E8FF },
            },

            /* ─── Les lieux à venir, préparés en avance (2026-11-26) ─────────────────
             * Le décor seulement : pas encore jouables. Leurs lumières continuent de baisser,
             * mais le merveilleux croît vers le bas (Codex §2) : ce qui luit tient la vue. Les
             * valeurs de lumière sont de l'art, à valider à l'écran — pas des graines du noyau. */

            // III · l'Aven (le récif fossile), l'Éclat : violet électrique, blanc. Un calcaire
            // sombre où dorment des ammonites et des coquilles, fendu de veines d'éclat.
            ["assise-3"] = new DecorDArt(
                "assise-3",
                new[] { 0.34, 0.33, 0.32, 0.31, 0.30, 0.29, 0.28, 0.27, 0.26, 0.25, 0.24, 0.23 },
                0xC8B8F0,
                new[] { 0x8C7CE0, 0x6A58C8, 0x4C3CA0, 0x30267A, 0x1C1650 },
                0x2E2A3C,
                new int[0],
                0xE8D8FF,
                false,
                0x4A3A6A,
                new[] { 0x2E2A3C, 0x3B3650, 0x58527A, 0x1A1724 })
            {
                TeinteDuFond = 0xA890E8,
                Jonction = JonctionDeLieu.Cassure,
                Magie = 0xC080FF,
                Paroi = MatiereDeParoi.Fossiles,
                // Les fossiles, leur cœur clair, la veine d'éclat.
                Inclusions = new[] { 0x9A90B8, 0xD0C8F0, 0xC080FF },
                Grains = new[] { 0xE0C0FF, 0xFFFFFF, 0xB080FF },
            },

            // IV · la fosse, le Gel : bleu glacier, argent. De la glace à grandes facettes,
            // givrée au bord, et des glaçons sous les corniches.
            ["assise-4"] = new DecorDArt(
                "assise-4",
                new[] { 0.30, 0.29, 0.28, 0.27, 0.26, 0.25, 0.24, 0.23, 0.22, 0.21, 0.20, 0.19 },
                0xD8F4FF,
                new[] { 0xA8E8F8, 0x78C8E8, 0x4C98C8, 0x2C6898, 0x183C60 },
                0x2A3A4E,
                new int[0],
                0xF0FAFF,
                false,
                0x5A7A8A,
                new[] { 0x2A3A4E, 0x3A5068, 0x8FB8D8, 0x18222E })
            {
                TeinteDuFond = 0xB0D0F0,
                Jonction = JonctionDeLieu.Cassure,
                Magie = 0xBFF0FF,
                Paroi = MatiereDeParoi.Glace,
                Inclusions = new[] { 0xBFE6FF, 0xF0FAFF },
                Grains = new[] { 0xF0FAFF, 0xC8E8FF },
            },

            // V · la faille chaude, la Braise : rouge sang, orange, soufre. Du basalte en colonnes,
            // veiné de lave, et des bassins de lave au fond.
            ["assise-5"] = new DecorDArt(
                "assise-5",
                new[] { 0.38, 0.37, 0.36, 0.35, 0.34, 0.33, 0.32, 0.31, 0.30, 0.29, 0.28, 0.27 },
                0xFFD0A0,
                new[] { 0xE88850, 0xC85A38, 0x983A2C, 0x642420, 0x381414 },
                0x241C1C,
                new int[0],
                0xFFD8A0,
                false,
                0x6A3A2A,
                new[] { 0x241C1C, 0x34282A, 0x4E3C3A, 0x120C0C })
            {
                TeinteDuFond = 0xFF9870,
                Jonction = JonctionDeLieu.Cassure,
                Magie = 0xFF8A30,
                Paroi = MatiereDeParoi.Basalte,
                Inclusions = new[] { 0xFF6A20, 0xFFC040 },
                Grains = new[] { 0xFFB040, 0xFF6A20, 0xFFE070 },
            },

            // VI · la mer relique, le Sel : nacre, vert-de-gris, noir. Des lits de sel et de
            // schiste noir, des cubes de sel, une croûte blanche sur les corniches.
            ["assise-6"] = new DecorDArt(
                "assise-6",
                new[] { 0.26, 0.25, 0.24, 0.23, 0.22, 0.21, 0.20, 0.19 },
                0xD8E8DC,
                new[] { 0x8CB8A8, 0x5E9888, 0x3E7468, 0x26504A, 0x14302E },
                0x1C2020,
                new int[0],
                0xF0F0E0,
                false,
                0x3A4A44,
                new[] { 0x1C2020, 0x2E3634, 0x7F9C90, 0x0E1010 })
            {
                TeinteDuFond = 0xA8C0B4,
                Jonction = JonctionDeLieu.Cassure,
                Magie = 0xE8E0D0,
                Paroi = MatiereDeParoi.Sel,
                Inclusions = new[] { 0xE8E0D0, 0xFFF8EE },
                Grains = new[] { 0xE8E0D0, 0xB8D0C0 },
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

        /// <summary>
        /// La teinte de la lumière d'un palier : de `TeinteDeLumiere` au premier palier de son
        /// lieu à `TeinteDuFond` au dernier, canal par canal. L'eau d'un lieu n'a pas une
        /// couleur mais une pente ; la lumière (`LumiereDuPalier`), elle, baisse à part.
        /// </summary>
        public static int TeinteDuPalier(int index)
        {
            var assise = Assises.DuPalier(index);
            var decor = DecorDe(assise.Id);
            if (!decor.TeinteDuFond.HasValue || assise.NombreDePaliers <= 1) return decor.TeinteDeLumiere;
            var t = Math.Min(1.0, Math.Max(0.0, (index - assise.IndexPremierPalier) / (double)(assise.NombreDePaliers - 1)));
            return Melanger(decor.TeinteDeLumiere, decor.TeinteDuFond.Value, t);
        }

        /// <summary>
        /// La lumière et sa teinte au BAS d'un palier : celles du haut du palier suivant, pour
        /// que la lumière glisse d'un palier à l'autre sans marche — sauf au-dessus d'une
        /// cassure, ou au bout du contenu : la pente du lieu continue, et la marche reste.
        /// </summary>
        public static (double Lumiere, int Teinte) LumiereAuBasDu(int palier)
        {
            var assise = Assises.DuPalier(palier);
            var suivant = palier + 1;
            var derniere = Assises.Toutes[Assises.Toutes.Count - 1];
            var existe = suivant < derniere.IndexPremierPalier + derniere.NombreDePaliers;
            var dansLeLieu = suivant < assise.IndexPremierPalier + assise.NombreDePaliers;
            if (existe && (dansLeLieu || DecorDe(Assises.DuPalier(suivant).Id).Jonction == JonctionDeLieu.Fondu))
                return (LumiereDuPalier(suivant), TeinteDuPalier(suivant));
            var pente = palier > assise.IndexPremierPalier ? LumiereDuPalier(palier - 1) - LumiereDuPalier(palier) : 0.04;
            return (Math.Max(0.02, LumiereDuPalier(palier) - pente), TeinteDuPalier(palier));
        }

        /// <summary>
        /// Les nappes de lumière (2026-11-26, le budget de lumières) : les paliers d'eau, en suites
        /// où la lumière glisse d'un palier à l'autre sans marche — une coupure à chaque cassure,
        /// et là où les paliers ne se suivent pas. Une seule lumière éclaire toute une nappe : la
        /// Noue entière en est une, le Gour une autre ; à l'écran, deux lumières d'eau au plus.
        /// </summary>
        public static IReadOnlyList<(int Premier, int Dernier)> NappesDeLumiere(IEnumerable<int> paliers)
        {
            var nappes = new List<(int Premier, int Dernier)>();
            var debut = -1;
            var precedent = -1;
            foreach (var p in paliers.Distinct().OrderBy(x => x))
            {
                if (debut < 0) debut = p;
                else if (p != precedent + 1 || Lieux.ACassure(p))
                {
                    nappes.Add((debut, precedent));
                    debut = p;
                }
                precedent = p;
            }
            if (debut >= 0) nappes.Add((debut, precedent));
            return nappes;
        }

        /// Deux couleurs 0xRRGGBB mêlées : 0 la première, 1 la seconde.
        public static int Melanger(int a, int b, double t)
        {
            int Canal(int decalage)
            {
                var ca = (a >> decalage) & 0xFF;
                var cb = (b >> decalage) & 0xFF;
                return (int)Math.Round(ca + (cb - ca) * t) << decalage;
            }
            return Canal(16) | Canal(8) | Canal(0);
        }

        public static IReadOnlyList<int> PaletteDuHeros() =>
            HEROS_DESSINE.Append(CONTOUR)
                .Concat(MARQUES.Values.SelectMany(m => m.Couleurs)).Concat(EVEIL).Distinct().ToList();

        /// Les six tribus dessinées (`outils/da/especes.py`, d'après la maquette validée) : toutes
        /// les couleurs de leurs poissons de troupe et de leurs représentants — corps, dos, ventre, accent, mais aussi
        /// nageoires, reflets, taches, joues, l'œil et son éclat.
        public static readonly IReadOnlyList<int> ESPECES_DESSINEES = new[]
        {
            0x121C1A, 0x2A1A40, 0x2E7D5B, 0x2F5A35, 0x2F7FE0, 0x3A2458, 0x3A4148, 0x3A4870, 0x40D0C0, 0x4A5A8A,
            0x4B545C, 0x4FE0D0, 0x5A3A18, 0x5A3F7A, 0x5A8A48, 0x5DB07E, 0x69747C, 0x6E4A22, 0x6FA05A, 0x6FA83A,
            0x7888B0, 0x7A5020, 0x7C8790, 0x7E60A8, 0x7FC8A0, 0x8A50E0, 0x9B7FC0, 0x9FF0FF, 0xA4AFB8, 0xA8783A,
            0xA8B8D8, 0xA8D88A, 0xB8E86A, 0xB9F0CF, 0xC060F0, 0xC08A3E, 0xC4A8E8, 0xC8A040, 0xC9CFC9, 0xD4E0F4,
            0xD8283A, 0xD9C8F0, 0xE2B470, 0xE8483A, 0xE8EEF8, 0xF070C0, 0xF0A0D0, 0xF0D070, 0xF0D9A0, 0xF4F1E4,
            0xFF8AA0, 0xFF8FA0, 0xFFE070, 0xFFE4EA, 0xFFFFFF,
        };

        /// La faune d'ambiance dessinée (`outils/da/faune.py`, Codex v1.3) : libellules,
        /// gerris, grenouilles, écrevisses et leur bannière, hydre, fleurs des jardins, et la méduse
        /// du Gour pour son portrait ; et les saisons : la glace, les œufs du frai, la feuille de la crue.
        public static readonly IReadOnlyList<int> FAUNE_DESSINEE = new[]
        {
            0x121C1A, 0x2FB8B0, 0x3FA060, 0x4A3424, 0x5CC070, 0x6A4C30, 0x7CD070, 0x7FE6E8, 0x8A4A20, 0x8FD8FF,
            0x8FE070, 0xA8F0D0, 0xB8452A, 0xB8E8C8, 0xBFEAFF, 0xC8E8FF, 0xD8FFF0, 0xE07040, 0xE6F8EE, 0xE6F8FF,
            0xE8F4FF, 0xF4A8C8, 0xF6C840, 0xFFD0E4, 0xFFE070, 0xFFF0A0, 0xFFF0C0,
        };

        public static IReadOnlyList<int> PaletteDeLaFaune() => FAUNE_DESSINEE.Append(CONTOUR).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDesEspeces() =>
            ESPECES_DESSINEES.Concat(ESPECES_PAR_RANG.SelectMany(c => c.Couleurs())).Append(CONTOUR).Distinct().ToList();

        /// Les couleurs d'un décor ; un lieu sous un autre y ajoute l'eau la plus sombre du lieu
        /// d'au-dessus, qui descend dans son premier palier (le fondu) ou appuie sur sa membrane
        /// (la cassure).
        public static IReadOnlyList<int> PaletteDuDecor(string assise)
        {
            var decor = DecorDe(assise);
            var couleurs = decor.Couleurs();
            var rang = DECORS_DESSINES.ToList().IndexOf(assise);
            if (rang > 0)
            {
                var dessus = DecorDe(DECORS_DESSINES[rang - 1]).Eau;
                couleurs = couleurs.Append(dessus[dessus.Count - 1]);
            }
            return couleurs.Distinct().ToList();
        }
    }
}
