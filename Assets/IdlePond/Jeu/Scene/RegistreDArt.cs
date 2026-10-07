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
        IReadOnlyList<int> Eau,
        int Vase,
        IReadOnlyList<int> Racines,
        int Rayon,
        bool Berge,
        int Voile,
        // La roche à creuser : le fond, la pierre, son arête claire, les fissures.
        IReadOnlyList<int> Roche,
        // L'eau file : des stries horizontales dans le fond (le Gour, sa contrainte).
        bool Courant = false);

    public sealed record CouleursDePoisson(int Corps, int Dos, int Ventre);

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

        public static readonly CouleursDePoisson HEROS = new CouleursDePoisson(0xC49C5C, 0x8C683A, 0xE2CE96);

        /// Une couleur par espèce, dans l'ordre des rangs ; au-delà, on reprend du début.
        static readonly CouleursDePoisson[] ESPECES_PAR_RANG =
        {
            new CouleursDePoisson(0x96AAA0, 0x566C64, 0xC8D4CC), // le vairon
            new CouleursDePoisson(0xA08A5E, 0x6A5A3A, 0xCCB88A), // la loche
            new CouleursDePoisson(0x7E9A6A, 0x4A6040, 0xC8D0A0), // l'épinoche, vert argent
            new CouleursDePoisson(0x7A6A4E, 0x4E4232, 0xA8987A), // le chabot, brun marbré
            new CouleursDePoisson(0x5E5E50, 0x3A3A30, 0x8A8A78), // la lamproie, olive sombre
            new CouleursDePoisson(0x8A8AA0, 0x585870, 0xC0C0D0), // l'ombre, gris violacé
        };

        public static CouleursDePoisson CouleursDEspece(int rang) => ESPECES_PAR_RANG[Math.Max(0, rang) % ESPECES_PAR_RANG.Length];

        /// Les assises qui ont leurs dessins, dans l'ordre. Les autres empruntent la Noue.
        public static readonly IReadOnlyList<string> ASSISES_DESSINEES = new[] { "noue", "gour" };

        static readonly Dictionary<string, DecorDArt> DECORS = new Dictionary<string, DecorDArt>
        {
            // La Noue : eau douce, racines de la berge, la lumière du jour qui baisse palier
            // par palier (GDD §15.2 : « la lumière est la variable de progression »).
            ["noue"] = new DecorDArt(
                "noue",
                new[] { 0.85, 0.77, 0.69, 0.61, 0.53, 0.45 },
                0xD8F0DC,
                new[] { 0x4A8070, 0x34645C, 0x244A48 },
                0x28221A,
                new[] { 0x3A2C20, 0x5C4630 },
                0x96BE96,
                true,
                0x8A7A3A,
                new[] { 0x1E2422, 0x2A322F, 0x3A4440, 0x121614 }),

            // Le Gour (spec du 2026-10-07) : des galeries noyées, plus de jour. Une eau froide,
            // bleu-gris, qui file ; la lumière part sous la dernière de la Noue et descend
            // encore ; un calcaire plus froid et plus sombre ; ni berge, ni racines.
            ["gour"] = new DecorDArt(
                "gour",
                new[] { 0.42, 0.40, 0.38, 0.36, 0.34, 0.32, 0.30, 0.28, 0.26, 0.24, 0.22, 0.20 },
                0xB8D8E0,
                new[] { 0x2E5866, 0x22444F, 0x17323A },
                0x3A3A34,
                new[] { 0x2A3236, 0x3C464A },
                0x7FA8B0,
                false,
                0x5A6A5A,
                new[] { 0x1A1E22, 0x262C31, 0x353D43, 0x101316 },
                Courant: true),
        };

        static readonly Dictionary<string, MarqueDArt> MARQUES = new Dictionary<string, MarqueDArt>
        {
            // Les branchies : la première adaptation, sans teinte ni lumière.
            ["noue"] = new MarqueDArt(AncrageId.Branchies, new[] { 0x5E4426 }, null, null),
            // Les membranes (GDD §15.1) : une crête sur le dos, pour tenir dans le courant.
            ["gour"] = new MarqueDArt(AncrageId.Dos, new[] { 0x7A9AA2, 0x4E6870 }, null, null),
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

        public static MarqueDArt MarqueDe(string assise) =>
            assise != null && MARQUES.TryGetValue(assise, out var marque) ? marque : null;

        public static double LumiereDuPalier(int index)
        {
            var assise = Assises.DuPalier(index);
            var lumieres = DecorDe(assise.Id).Lumieres;
            return lumieres[Math.Min(index - assise.IndexPremierPalier, lumieres.Count - 1)];
        }

        public static IReadOnlyList<int> PaletteDuHeros() =>
            new[] { HEROS.Corps, HEROS.Dos, HEROS.Ventre, CONTOUR }
                .Concat(MARQUES.Values.SelectMany(m => m.Couleurs)).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDesEspeces() =>
            ESPECES_PAR_RANG.SelectMany(c => new[] { c.Corps, c.Dos, c.Ventre }).Append(CONTOUR).Distinct().ToList();

        public static IReadOnlyList<int> PaletteDuDecor(string assise)
        {
            var d = DecorDe(assise);
            return d.Eau.Concat(d.Racines).Concat(d.Roche).Append(d.Vase).Append(d.Rayon).Distinct().ToList();
        }
    }
}
