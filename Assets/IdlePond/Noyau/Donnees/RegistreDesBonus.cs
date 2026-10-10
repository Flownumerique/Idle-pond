using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — les bonus de lieu et les techniques (spec du 2026-10-10, l'onglet
    /// « Débloquer »).
    ///
    /// Contenu pur. Les formules vivent dans `Economie.cs`, les graines de prix dans
    /// `Constantes.cs`, les textes dans `Textes.cs`. Les identifiants entrent dans les
    /// saves : figés.
    ///
    /// Un BONUS DE LIEU ne touche que son lieu : les espèces qui y vivent, les paliers qui
    /// le creusent. C'est le seul achat au mana qui monte une production, et seulement la
    /// sienne. Une TECHNIQUE (`Assise` null) vaut partout, et reste du côté des coûts, des
    /// plafonds et des verbes.
    ///
    /// [P] graine — toutes les parts, tous les rangs maximaux et tous les paliers de prix de
    /// ce fichier. Ils n'ont été mesurés ni au simulateur ni en vraie partie : le simulateur
    /// n'achète aucun bonus, et les références de parité ne les voient donc pas.
    ///
    /// Seuls les deux lieux livrés en portent. Un lieu suivant en recevra quand il sera
    /// nommé (Codex §6).
    /// </summary>
    public static class RegistreDesBonus
    {
        public static readonly IReadOnlyList<Bonus> Tous = new[]
        {
            /* — La Noue : six paliers, le vairon et la loche ————————————————————— */
            new Bonus("noue-vase", "noue", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutCreuser, 0.08, 5, 1, 2),
            new Bonus("noue-herbier", "noue", GenreDeBonus.Production, TermeDeFormule.MultiplicateurDeLieu, 0.25, 5, 2, 3),
            new Bonus("noue-racines", "noue", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutDeblocage, 0.2, 3, 3, 3),
            new Bonus("noue-eau-calme", "noue", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutNiveau, 0.1, 5, 4, 4),

            /* — Le Gour : douze paliers, l'épinoche, le chabot, la lamproie, l'ombre —— */
            new Bonus("gour-roche-tendre", "gour", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutCreuser, 0.08, 5, 1, 8),
            new Bonus("gour-courant", "gour", GenreDeBonus.Production, TermeDeFormule.MultiplicateurDeLieu, 0.25, 5, 3, 10),
            new Bonus("gour-abris", "gour", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutDeblocage, 0.2, 3, 4, 11),
            new Bonus("gour-vasques", "gour", GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutNiveau, 0.1, 5, 6, 13),

            /* — Les techniques : partout ——————————————————————————————————————————— */
            new Bonus("technique-pelle", null, GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutCreuser, 0.05, 10, 2, 2),
            new Bonus("technique-geste", null, GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutNiveau, 0.04, 10, 3, 3),
            new Bonus("technique-approche", null, GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutDeblocage, 0.1, 5, 4, 4),
            new Bonus("technique-croissance", null, GenreDeBonus.ReductionDeCout, TermeDeFormule.CoutCroissance, 0.05, 5, 4, 4),
            new Bonus("technique-patience", null, GenreDeBonus.Confort, TermeDeFormule.CapHorsLigne, 0.5, 4, 5, 5),
            new Bonus("technique-main-sure", null, GenreDeBonus.Verbe, null, 0, 1, 6, 7, CapaciteId.AchatAuto),
            new Bonus("technique-sonde", null, GenreDeBonus.Verbe, null, 0, 1, 9, 10, CapaciteId.CreusementAuto),
        };

        static readonly IReadOnlyDictionary<string, Bonus> PAR_ID = Tous.ToDictionary(b => b.Id);

        public static Bonus ParId(string id) => id != null && PAR_ID.TryGetValue(id, out var bonus) ? bonus : null;

        /// Les bonus d'un lieu, dans l'ordre du registre.
        public static IReadOnlyList<Bonus> DuLieu(string assiseId) => Tous.Where(b => b.Assise == assiseId).ToList();

        /// Les techniques, dans l'ordre du registre.
        public static IReadOnlyList<Bonus> Techniques() => Tous.Where(b => b.Assise == null).ToList();
    }
}
