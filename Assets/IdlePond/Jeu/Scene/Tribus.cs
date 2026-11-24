using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;

namespace IdlePond.Jeu.Scene
{
    /// Ce qu'une tribu montre sur un événement : une lumière en éclair, jamais en continu.
    public enum GenreDeReaction
    {
        /// Le Chœur des vairons : une vague de rouge parcourt la troupe. `Espece` : vairon.
        VagueRouge,
        /// Les Lamproies pèlerines : un fil de mana va d'une tribu qui a monté jusqu'à elles.
        /// `Source` : la tribu qui a monté.
        FilDeMana,
        /// Les Ombres-hérauts : une ombre laisse un mirage du nouveau venu. `Source` : la
        /// tribu tout juste convaincue.
        Mirage,
        /// Toute tribu : à la conviction, son représentant entre dans la bande et rallie sa
        /// troupe. `Source` : la tribu convaincue.
        Entree,
        /// La faune d'ambiance (Codex v1.3) : quand une tribu est ralliée, les écrevisses
        /// porte-bannière défilent en procession au fond de sa bande. `Source` : la tribu.
        Procession,
    }

    /// `Debut` : l'instant de la scène (en secondes) où la réaction commence.
    public sealed record Reaction(GenreDeReaction Genre, string Source, double Debut)
    {
        public double Duree => Tribus.DureeDe(Genre);
        public bool EstFinie(double instant) => instant >= Debut + Duree;
        /// De 0 au début à 1 à la fin.
        public double Avancement(double instant) => Math.Max(0, Math.Min(1, (instant - Debut) / Duree));
    }

    /// <summary>
    /// Ce qui se lit dans l'état, sans événement : les chabots pétrifiés tant que l'eau est
    /// trouble, les loches qui s'agitent quand l'eau va se troubler ou quand la descente va se
    /// rouvrir, et les nids que l'épinoche a bâtis dans cette vie.
    /// </summary>
    public sealed record EtatDesTribus(bool ChabotsPetrifies, bool LochesAgitees, int NidsDEpinoche)
    {
        public static readonly EtatDesTribus CALME = new EtatDesTribus(false, false, 0);
    }

    /// <summary>
    /// IdlePond — les six tribus à l'écran (Codex v1.4, §2, « Les six tribus à l'écran »).
    ///
    /// Pur, comme `VueDeScene` : aucune référence à UnityEngine. Ce fichier dit QUAND une
    /// tribu réagit et COMMENT se calcule ce qu'elle montre ; `ComportementsDesTribus` le
    /// dessine. Rien de ce qui est ici ne touche l'économie : ce sont des événements et des
    /// lectures d'état, pour l'écran seul.
    ///
    /// Deux familles :
    /// - les **états** (`Lire`) : chabots pétrifiés, loches agitées, nids de l'épinoche ;
    /// - les **réactions** (`Reactions`) : la vague des vairons, le fil des lamproies, le
    ///   mirage des ombres — une lumière en éclair, sur un événement, puis plus rien.
    /// </summary>
    public static class Tribus
    {
        public const string VAIRON = "vairon";
        public const string LOCHE = "loche";
        public const string EPINOCHE = "epinoche";
        public const string CHABOT = "chabot";
        public const string LAMPROIE = "lamproie";
        public const string OMBRE = "ombre";

        /// Les loches sentent l'eau se troubler quand la mare en est à cette distance du seuil
        /// d'alerte (85 %) : de 70 % à 85 %.
        public const double AVANCE_DES_LOCHES_SUR_LE_TROUBLE = 0.15;

        /// Et la descente se rouvrir quand la contenance atteint cette part du prix du creux
        /// suivant, sans l'avoir encore rattrapé.
        public const double AVANCE_DES_LOCHES_SUR_LA_DESCENTE = 0.9;

        /// Un nid par seuil de jalon franchi (10, 25, 50, 100) : quatre au plus.
        public static int NidsDEpinoche(int niveau) => Constantes.SEUILS_DE_JALON.Count(s => niveau >= s.Seuil);

        public static EtatDesTribus Lire(EtatJeu etat)
        {
            if (etat == null) return EtatDesTribus.CALME;
            var part = Economie.PartDeContenance(etat);
            var trouble = part >= Constantes.SEUIL_D_ALERTE_DE_CONTENANCE;
            var niveauEpinoche = etat.Cycle.Especes.TryGetValue(EPINOCHE, out var e) && e.Debloquee ? e.Niveau : 0;
            return new EtatDesTribus(trouble, LochesAgitees(etat, part, trouble), NidsDEpinoche(niveauEpinoche));
        }

        static bool LochesAgitees(EtatJeu etat, double part, bool trouble)
        {
            if (!trouble && part >= Constantes.SEUIL_D_ALERTE_DE_CONTENANCE - AVANCE_DES_LOCHES_SUR_LE_TROUBLE) return true;
            if (Economie.ToutEstCreuse(etat) || !Economie.EstBloque(etat)) return false;
            var cout = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
            if (cout.Lte(0)) return false;
            return Economie.Contenance(etat).Div(cout).ToNumber() >= AVANCE_DES_LOCHES_SUR_LA_DESCENTE;
        }

        /* ─── Les réactions ─────────────────────────────────────────────────────────── */

        public const double DUREE_DE_LA_VAGUE = 1.2;
        public const double DUREE_DU_FIL = 0.8;
        public const double DUREE_DU_MIRAGE = 2.4;
        public const double DUREE_DE_L_ENTREE = 2.0;

        /// Un fil au plus par tribu source et par seconde : « monter jusqu'au seuil » fait des
        /// dizaines de crans d'un coup, l'écran n'en montre qu'un.
        public const double ECART_ENTRE_DEUX_FILS = 1.0;

        public static double DureeDe(GenreDeReaction genre) => genre switch
        {
            GenreDeReaction.VagueRouge => DUREE_DE_LA_VAGUE,
            GenreDeReaction.FilDeMana => DUREE_DU_FIL,
            GenreDeReaction.Entree => DUREE_DE_L_ENTREE,
            GenreDeReaction.Procession => Faune.DUREE_DE_LA_PROCESSION,
            _ => DUREE_DU_MIRAGE,
        };

        /* ─── La vague du Chœur ─────────────────────────────────────────────────────── */

        /// La part de la vague que chaque vairon porte en rouge : elle passe d'un bout à
        /// l'autre de la troupe, chacun rougit à son tour.
        public const double PART_DE_ROUGE = 0.3;

        /// Le `numero`-ième vairon d'une troupe de `effectif` est-il rouge, à cet avancement
        /// de la vague ? Le premier rougit au début, le dernier à la fin ; seul, il rougit tout du long.
        public static bool EstRouge(int numero, int effectif, double avancement)
        {
            if (effectif <= 0 || avancement <= 0 || avancement >= 1) return false;
            if (effectif == 1) return true;
            var depart = (1 - PART_DE_ROUGE) * numero / (effectif - 1);
            return avancement >= depart && avancement < depart + PART_DE_ROUGE;
        }

        /* ─── Les loches agitées ────────────────────────────────────────────────────── */

        /// Un frisson d'un pixel, à gauche ou à droite, huit fois par seconde. Chaque loche a
        /// sa phase : la troupe ne tremble pas d'un bloc.
        public static int Frisson(double secondes, double phase)
        {
            var pas = (long)Math.Floor((secondes + phase) * 8);
            return (int)(((pas % 3) + 3) % 3) - 1;
        }

        /* ─── Le fil des lamproies ──────────────────────────────────────────────────── */

        /// L'écart entre deux points du fil, en pixels.
        public const int PAS_DU_FIL = 4;

        /// Les points du fil, de la source vers la lamproie, à cet avancement : le fil
        /// s'allonge pendant la première moitié, puis se retire de la source vers la lamproie
        /// pendant la seconde — le mana arrive chez elle. Au pixel entier.
        public static IReadOnlyList<Pixel> PointsDuFil(Pixel source, Pixel cible, double avancement)
        {
            var dx = cible.X - source.X;
            var dy = cible.Y - source.Y;
            var longueur = Math.Sqrt(dx * (double)dx + dy * (double)dy);
            var nombre = Math.Max(1, (int)Math.Ceiling(longueur / PAS_DU_FIL));
            double debut, fin;
            if (avancement < 0.5) { debut = 0; fin = avancement * 2; }
            else { debut = (avancement - 0.5) * 2; fin = 1; }
            var points = new List<Pixel>();
            for (var i = 0; i <= nombre; i++)
            {
                var t = (double)i / nombre;
                if (t < debut || t > fin) continue;
                points.Add(new Pixel(source.X + (int)Math.Round(dx * t), source.Y + (int)Math.Round(dy * t)));
            }
            return points;
        }

        /* ─── Le mirage des ombres ──────────────────────────────────────────────────── */

        /// Le mirage clignote — jamais de demi-transparence : six fois par seconde au début,
        /// et il s'éteint plus souvent à mesure qu'il se défait.
        public static bool MirageVisible(double avancement)
        {
            if (avancement <= 0 || avancement >= 1) return false;
            var pas = (int)Math.Floor(avancement * DUREE_DU_MIRAGE * 6);
            if (avancement < 0.5) return true;
            if (avancement < 0.8) return pas % 2 == 0;
            return pas % 3 == 0;
        }

        /* ─── L'entrée du représentant ───────────────────────────────────────────── */

        /// Le numéro de trajet du représentant dans sa bande : hors de ceux de la troupe (au plus
        /// `VueDeScene.POISSONS_MAX_PAR_GROUPE`), il nage son propre chemin.
        public const int NUMERO_DU_REPRESENTANT = 100;

        /// De combien le représentant est encore à gauche de sa place, à cet avancement de son
        /// entrée : de toute la `distance` au début à rien à la fin, en ralentissant à
        /// l'arrivée. Au pixel entier.
        public static int DecalageDEntree(double avancement, int distance)
        {
            if (avancement >= 1 || distance <= 0) return 0;
            var reste = 1 - Math.Max(0, avancement);
            return -(int)Math.Round(distance * reste * reste);
        }

        /* ─── Les nids de l'épinoche ────────────────────────────────────────────────── */

        /// Un nid-forteresse d'herbes tressées : un monticule de 2 px de grain, posé sur le
        /// fond de la bande. `numero` le fait un peu différent des autres. Les points ont leur
        /// coin bas-gauche relatif au pied du nid ; la couleur est un rang dans `HERBES_DU_NID`.
        public sealed record PointDeNid(int X, int Y, int Couleur);

        public static readonly IReadOnlyList<int> HERBES_DU_NID = new[] { 0x2F5A35, 0x3FA060, 0x6FA05A, 0x8FE070 };

        public const int LARGEUR_DE_NID = 18;

        public static IReadOnlyList<PointDeNid> PointsDUnNid(int numero)
        {
            // Un dôme : 9 colonnes de 2 px, plus hautes au milieu ; une porte sombre au pied.
            var bases = new[] { 1, 2, 3, 4, 5, 4, 3, 2, 1 };
            var points = new List<PointDeNid>();
            var hauteurs = new int[bases.Length];
            for (var c = 0; c < bases.Length; c++)
            {
                hauteurs[c] = bases[c] + ((numero + c) % 4 == 0 ? 1 : 0);
                for (var r = 0; r < hauteurs[c]; r++)
                {
                    var porte = r <= 1 && c >= 3 && c <= 5 && !(r == 1 && c != 4);
                    var couleur = porte ? 0 : 1 + ((c + r + numero) % 3);
                    points.Add(new PointDeNid(2 * c, 2 * r, couleur));
                }
            }
            // Une épine de verre rouge plantée au sommet : la Garde. Couleur −1 : l'accent de
            // l'épinoche, pas une herbe.
            var sommet = 3 + numero % 3;
            points.Add(new PointDeNid(2 * sommet, 2 * hauteurs[sommet], -1));
            return points;
        }

        /// Le pied du `numero`-ième nid dans la bande : répartis sur le fond, sans se toucher.
        public static int XDuNid(int numero) => 27 + numero * 78 + (numero % 2) * 14;

        /* ─── Les lamproies aux parois ─────────────────────────────────────────────── */

        /// Une lamproie accrochée à la paroi : contre le bord gauche ou droit de la bande, la
        /// bouche vers la paroi, qui ne fait que monter et descendre d'un pixel.
        public static bool AccrocheeAGauche(int numero) => numero % 2 == 0;

        /// L'oscillation verticale d'une lamproie accrochée : un pixel, lentement.
        public static int Respiration(double secondes, double phase) =>
            Math.Sin((secondes + phase) * Math.PI * 0.8) >= 0 ? 0 : -1;
    }

    /// <summary>
    /// Les réactions en cours : reçoit les événements de jeu, rend celles qui sont encore
    /// actives à un instant donné. L'instant est celui de la scène (pas l'horloge de la
    /// partie) : une réaction n'existe que pour l'œil.
    /// </summary>
    public sealed class ReactionsDesTribus
    {
        readonly List<Reaction> enCours = new List<Reaction>();
        readonly Dictionary<string, double> dernierFil = new Dictionary<string, double>();

        public IReadOnlyList<Reaction> EnCours => enCours;

        /// `presentes` : les tribus convaincues dans cette vie (celles qu'on voit nager).
        public void Recevoir(IEnumerable<EvenementDeJeu> evenements, double instant, ICollection<string> presentes)
        {
            foreach (var e in evenements)
            {
                switch (e.Genre)
                {
                    case GenreDEvenement.Renaissance:
                        enCours.Clear();
                        dernierFil.Clear();
                        break;
                    case GenreDEvenement.SeuilFranchi when e.Sujet == Tribus.VAIRON:
                        enCours.RemoveAll(r => r.Genre == GenreDeReaction.VagueRouge);
                        enCours.Add(new Reaction(GenreDeReaction.VagueRouge, Tribus.VAIRON, instant));
                        break;
                    case GenreDEvenement.EspeceConvaincue:
                        enCours.Add(new Reaction(GenreDeReaction.Entree, e.Sujet, instant));
                        enCours.Add(new Reaction(GenreDeReaction.Procession, e.Sujet, instant));
                        if (e.Sujet != Tribus.OMBRE && presentes.Contains(Tribus.OMBRE))
                            enCours.Add(new Reaction(GenreDeReaction.Mirage, e.Sujet, instant));
                        break;
                }
                if ((e.Genre == GenreDEvenement.CransMontes || e.Genre == GenreDEvenement.SeuilFranchi)
                    && e.Sujet != null && e.Sujet != Tribus.LAMPROIE && presentes.Contains(Tribus.LAMPROIE)
                    && (!dernierFil.TryGetValue(e.Sujet, out var avant) || instant - avant >= Tribus.ECART_ENTRE_DEUX_FILS))
                {
                    dernierFil[e.Sujet] = instant;
                    enCours.Add(new Reaction(GenreDeReaction.FilDeMana, e.Sujet, instant));
                }
            }
        }

        /// Oublie les réactions finies ; rend vrai s'il en reste.
        public bool Avancer(double instant)
        {
            enCours.RemoveAll(r => r.EstFinie(instant));
            return enCours.Count > 0;
        }

        /// L'entrée en cours du représentant de cette tribu, ou null.
        public Reaction EntreeDe(string espece, double instant) =>
            enCours.FirstOrDefault(r => r.Genre == GenreDeReaction.Entree && r.Source == espece && !r.EstFinie(instant));

        public Reaction VagueEnCours(double instant) =>
            enCours.FirstOrDefault(r => r.Genre == GenreDeReaction.VagueRouge && !r.EstFinie(instant));
    }
}
