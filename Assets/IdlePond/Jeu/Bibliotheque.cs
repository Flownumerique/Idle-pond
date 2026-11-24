using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu
{
    /// Ce que la bibliothèque sait d'une tribu.
    public enum EtatDeTribu
    {
        /// Son lieu est atteint, elle n'est pas encore convaincue : une silhouette.
        Apercue,
        /// Convaincue dans cette vie, ou déjà connue d'une vie d'avant : son représentant, son
        /// nom, ses mœurs, son texte.
        Connue,
    }

    public sealed record EntreeDeTribu(Espece Espece, EtatDeTribu Etat);

    /// Une bête de la faune d'ambiance dans la bibliothèque : son identifiant (celui de
    /// `Textes.TEXTES_DE_LA_FAUNE`), son lieu, si on l'a déjà vue, et l'image de son portrait.
    public sealed record EntreeDeFaune(string Id, string Lieu, bool Rencontree, string Dessin, int Image);

    /// Le détail d'un succès, à la demande (GDD §14.8) : ce qui s'est passé, la condition,
    /// l'effet exact, chiffré, et la vie où il a été noté.
    public sealed record DetailDeSucces(string Nom, string Rapport, string Condition, string Effet, int? NoteDansLaVie);

    /// <summary>
    /// IdlePond — la bibliothèque (GDD §14.8 ; Codex v1.4, les Carapaces en sont les
    /// gardiennes) : elle inventorie, le journal racontera. Deux parties : ceux qu'il a
    /// rencontrés — les tribus — et ce qu'il sait — les succès.
    ///
    /// Pure, comme `Evenements` : elle lit l'état, n'ajoute aucune règle, et ne sert qu'à
    /// l'écran.
    ///
    /// Verrouillage par lieu, comme les succès (§14.6) : une tribu n'est listée que lorsque
    /// son lieu est atteint — la bibliothèque n'annonce pas ce qui vit plus bas.
    /// </summary>
    public static class Bibliotheque
    {
        /// Le seuil qui fait une tribu connue pour toujours : son premier succès de seuil. Une
        /// tribu convaincue une fois et montée à dix ne s'oublie plus, même après la renaissance.
        public const int NIVEAU_QUI_FAIT_CONNAITRE = 10;

        public static IReadOnlyList<EntreeDeTribu> Tribus(EtatJeu etat)
        {
            var lieux = RegleDesSucces.AssisesAtteintes(etat);
            return Especes.Toutes
                .Where(e => lieux.Contains(e.Assise) && Textes.DeLaTribu(e.Id) != null)
                .Select(e => new EntreeDeTribu(e, EstConnue(etat, e.Id) ? EtatDeTribu.Connue : EtatDeTribu.Apercue))
                .ToList();
        }

        public static bool EstConnue(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee && vivante.Niveau > 0
            || RegleDesSucces.EstAcquis(etat, $"seuil-{espece}-{NIVEAU_QUI_FAIT_CONNAITRE}");

        /* ─── Ce qui vit autour : la faune d'ambiance ──────────────────────────────── */

        /// Où vit chaque bête, quel dessin fait son portrait (la lanterne allumée, la gorge
        /// gonflée, la bannière, la grande fleur), et quand on l'a vue.
        static readonly IReadOnlyDictionary<string, (string Lieu, string Dessin, int Image, Func<EtatJeu, bool> Vue)> FAUNE =
            new Dictionary<string, (string, string, int, Func<EtatJeu, bool>)>
            {
                // À la surface de la Noue : là dès qu'on y est.
                ["libellules"] = ("noue", Scene.Faune.LIBELLULE, Scene.Faune.IMAGE_ALLUMEE, _ => true),
                ["gerris"] = ("noue", Scene.Faune.GERRIS, 0, _ => true),
                ["grenouilles"] = ("noue", Scene.Faune.GRENOUILLE, 1, _ => true),
                ["hydre"] = ("noue", Scene.Faune.HYDRE, 0, _ => true),
                // Les jardins fleurissent à la première renaissance.
                ["jardins"] = ("noue", Scene.Faune.FLEUR, 2, e => e.Permanent.NombreDeRenaissances > 0),
                // La procession défile à la première tribu ralliée.
                ["ecrevisses"] = ("noue", Scene.Faune.ECREVISSE_BANNIERE, 0, e => e.Permanent.NombreDeRenaissances > 0 || UneTribuRalliee(e)),
                // Les méduses sont dans le décor du Gour.
                ["meduses"] = ("gour", Scene.Faune.MEDUSE, 0, _ => true),
            };

        static bool UneTribuRalliee(EtatJeu etat) => etat.Cycle.Especes.Values.Any(v => v.Debloquee && v.Niveau > 0);

        /// La faune des lieux atteints, dans l'ordre du Codex ; rien d'un lieu qu'on n'a pas atteint.
        public static IReadOnlyList<EntreeDeFaune> Faune(EtatJeu etat)
        {
            var lieux = RegleDesSucces.AssisesAtteintes(etat);
            return Textes.TEXTES_DE_LA_FAUNE
                .Where(f => FAUNE.ContainsKey(f.Id) && lieux.Contains(FAUNE[f.Id].Lieu))
                .Select(f =>
                {
                    var (lieu, dessin, image, vue) = FAUNE[f.Id];
                    return new EntreeDeFaune(f.Id, lieu, vue(etat), dessin, image);
                })
                .ToList();
        }

        /* ─── Ce qu'il sait : le détail d'un succès ─────────────────────────────────── */

        public static DetailDeSucces Detail(EtatJeu etat, Succes succes)
        {
            var texte = Textes.DuSucces(succes.Id);
            var entree = etat.Permanent.Succes.TryGetValue(succes.Id, out var e) ? e : null;
            return new DetailDeSucces(texte.Nom, entree != null ? texte.Rapport : null, texte.Condition, EffetDe(succes.Effet),
                entree != null ? entree.ObtenuAuCycle + 1 : (int?)null);
        }

        /// L'effet exact, chiffré (§14.3 : le lieu de l'audit). Jamais de production : un succès
        /// baisse un prix, monte une limite, ou apprend un geste (amendement v1.1 §2.D).
        public static string EffetDe(EffetDeSucces effet)
        {
            if (effet == null) return E.EFFET_AUCUN;
            var part = Pourcent(effet.Part);
            switch (effet.Genre)
            {
                case GenreDEffetDeSucces.ReductionCout:
                    return string.Format(CultureInfo.InvariantCulture, effet.Terme switch
                    {
                        TermeDeFormule.CoutCreuser => E.EFFET_CREUSER,
                        TermeDeFormule.CoutNiveau => E.EFFET_MONTER,
                        TermeDeFormule.CoutDeblocage => E.EFFET_DEBLOQUER,
                        _ => E.EFFET_COUT,
                    }, part);
                case GenreDEffetDeSucces.Plafond:
                    return string.Format(CultureInfo.InvariantCulture, effet.Terme switch
                    {
                        TermeDeFormule.ContenanceDeDepart => E.EFFET_PORTER,
                        TermeDeFormule.CapHorsLigne => E.EFFET_ABSENCE,
                        _ => E.EFFET_PLAFOND,
                    }, part);
                default:
                    return E.EFFET_VERBE;
            }
        }

        /// 0,02 → « 2 % », avec l'espace insécable de la typographie française.
        public static string Pourcent(double part)
        {
            var valeur = Math.Round(part * 100, 1);
            var texte = valeur == Math.Floor(valeur)
                ? valeur.ToString("0", CultureInfo.InvariantCulture)
                : valeur.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return texte + " %";
        }
    }
}
