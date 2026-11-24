using System;
using System.Collections.Generic;

namespace IdlePond.Jeu.Scene
{
    /// Ce qu'un événement de jeu fait jaillir dans la scène (ROADMAP, chantier 6 : « les
    /// retours visuels »).
    public enum GenreDeGerbe
    {
        /// Un creux de plus : des éclats de roche tombent du haut de la bande ouverte.
        RocheQuiCede,
        /// Un seuil franchi : une couronne d'or s'écarte de la tribu.
        Seuil,
        /// Une tribu ralliée : un éclair d'eau claire et d'or au milieu de sa bande.
        Ralliement,
        /// L'eau se trouble : de la vase monte du fond.
        Trouble,
    }

    /// `Origine` : d'où elle jaillit, en pixels de la scène ; `Debut` : l'instant de la scène.
    public sealed record Gerbe(GenreDeGerbe Genre, Pixel Origine, double Debut);

    /// Un point d'une gerbe : où, et le rang de sa couleur dans `Gerbes.CouleursDe`.
    public sealed record PointDeGerbe(int X, int Y, int Couleur);

    /// <summary>
    /// IdlePond — les retours visuels des événements de jeu : des gerbes de points (l'éclat de
    /// deux pixels), qui jaillissent, s'écartent et s'éteignent. Pur et déterministe : une gerbe
    /// est entièrement dite par son genre, son origine et le temps écoulé. Une lumière en
    /// éclair, jamais en continu (Codex v1.4) ; rien ne touche l'économie.
    /// </summary>
    public static class Gerbes
    {
        public static double DureeDe(GenreDeGerbe genre) => genre switch
        {
            GenreDeGerbe.RocheQuiCede => 0.9,
            GenreDeGerbe.Seuil => 0.8,
            GenreDeGerbe.Ralliement => 1.0,
            _ => 1.6,
        };

        public static int NombreDe(GenreDeGerbe genre) => genre switch
        {
            GenreDeGerbe.RocheQuiCede => 16,
            GenreDeGerbe.Seuil => 12,
            GenreDeGerbe.Ralliement => 14,
            _ => 18,
        };

        /// Les couleurs de chaque gerbe : la pierre de la roche (`RegistreDArt`), l'or des
        /// étoiles de l'éveil, l'eau claire de la Noue, la vase.
        public static IReadOnlyList<int> CouleursDe(GenreDeGerbe genre) => genre switch
        {
            GenreDeGerbe.RocheQuiCede => RegistreDArt.DecorDe("noue").Roche,
            GenreDeGerbe.Seuil => new[] { 0xFFE070, 0xFFFBEA },
            GenreDeGerbe.Ralliement => new[] { 0xD8FFF0, 0xFFE070, 0x7FE6E8 },
            _ => new[] { 0x4A3424, 0x6A4C30 },
        };

        /// Un millionième de seconde de tolérance : `début + durée − début` n'est pas toujours la
        /// durée en virgule flottante.
        const double TOLERANCE = 1e-6;

        public static bool EstFinie(Gerbe gerbe, double instant) => instant - gerbe.Debut >= DureeDe(gerbe.Genre) - TOLERANCE;

        static uint Melanger(uint x)
        {
            x ^= x >> 16; x *= 0x7feb352dU; x ^= x >> 15; x *= 0x846ca68bU; x ^= x >> 16;
            return x;
        }

        /// Les points de la gerbe à cet instant. Vide avant son début et après sa fin. Chaque
        /// point clignote en fin de course (jamais de demi-transparence) avant de s'éteindre.
        public static IReadOnlyList<PointDeGerbe> Points(Gerbe gerbe, double instant)
        {
            var points = new List<PointDeGerbe>();
            var age = instant - gerbe.Debut;
            var duree = DureeDe(gerbe.Genre);
            if (age < 0 || age >= duree - TOLERANCE) return points;
            var a = age / duree;
            var couleurs = CouleursDe(gerbe.Genre).Count;
            var graine = (uint)(gerbe.Origine.X * 73856093) ^ (uint)(gerbe.Origine.Y * 19349663) ^ (uint)gerbe.Genre;
            for (var i = 0; i < NombreDe(gerbe.Genre); i++)
            {
                var h = Melanger(graine + (uint)i * 2654435761u);
                // En fin de course, un point sur deux s'éteint une image sur deux.
                if (a > 0.7 && (i + (int)(age * 20)) % 2 == 0) continue;
                var u = (h % 1000u) / 1000.0;
                var v = ((h >> 10) % 1000u) / 1000.0;
                double dx, dy;
                switch (gerbe.Genre)
                {
                    case GenreDeGerbe.RocheQuiCede:
                        // Des éclats qui partent de côté puis tombent.
                        dx = (u - 0.5) * 80 * a;
                        dy = -(v * 10 * a + 50 * a * a);
                        break;
                    case GenreDeGerbe.Seuil:
                    case GenreDeGerbe.Ralliement:
                        // Une couronne qui s'écarte.
                        var angle = 2 * Math.PI * (i + u * 0.3) / NombreDe(gerbe.Genre);
                        var rayon = (gerbe.Genre == GenreDeGerbe.Seuil ? 26 : 40) * Math.Sqrt(a) + 4;
                        dx = Math.Cos(angle) * rayon;
                        dy = Math.Sin(angle) * rayon * 0.6;
                        break;
                    default:
                        // La vase qui monte en volutes.
                        dx = (u - 0.5) * 120 + Math.Sin(age * 3 + i) * 4;
                        dy = v * 6 + 30 * a;
                        break;
                }
                points.Add(new PointDeGerbe(gerbe.Origine.X + (int)Math.Round(dx), gerbe.Origine.Y + (int)Math.Round(dy), (int)(h >> 20) % couleurs));
            }
            return points;
        }

        /// D'où jaillit la gerbe d'un événement, quand il en a une ; null sinon. `bandesOuvertes`
        /// situe la vase au fond ; `centreDe` donne le milieu d'une tribu qui nage (son
        /// représentant), ou null.
        public static Gerbe De(EvenementDeJeu evenement, double instant, int bandesOuvertes, Func<string, Pixel> centreDe)
        {
            var milieu = Gabarits.LARGEUR_VISEE / 2;
            var h = Gabarits.HAUTEUR_DE_BANDE;
            switch (evenement.Genre)
            {
                case GenreDEvenement.CreuxOuvert:
                    return new Gerbe(GenreDeGerbe.RocheQuiCede, new Pixel(milieu, -(int)evenement.Valeur * h - 4), instant);
                case GenreDEvenement.SeuilFranchi:
                    var centre = centreDe?.Invoke(evenement.Sujet);
                    return centre != null ? new Gerbe(GenreDeGerbe.Seuil, centre, instant) : null;
                case GenreDEvenement.EspeceConvaincue:
                    var espece = IdlePond.Noyau.Donnees.Especes.ParId(evenement.Sujet);
                    return espece != null ? new Gerbe(GenreDeGerbe.Ralliement, new Pixel(milieu, -espece.Palier * h - h / 2), instant) : null;
                case GenreDEvenement.EauTroublee:
                    return new Gerbe(GenreDeGerbe.Trouble, new Pixel(milieu, -Math.Max(1, bandesOuvertes) * h + 4), instant);
                default:
                    return null;
            }
        }
    }
}
