using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau.Donnees
{
    /// <summary>
    /// IdlePond — registre des nœuds de technique : les trente nœuds du noyau v1.0 §6.2,
    /// six branches de cinq, transcrits le 2026-11-13.
    ///
    /// Contenu pur. Les règles vivent dans `Technique.cs` (points, calendriers, apprentissage),
    /// les verbes dans `Automatismes.cs`, les textes dans `Textes.cs`. Les identifiants entrent
    /// dans les saves : figés.
    ///
    /// Trois lectures du noyau, à connaître :
    ///
    ///   - Les remises d'une même branche sont des PALIERS, pas des remises qui se composent :
    ///     « −10 %, −20 %, −30 % » veut dire 0,90, puis 0,80, puis 0,70 en tout. Chaque nœud
    ///     porte donc le facteur qui amène le produit au palier annoncé.
    ///   - « Réputation » donnait +5 % de production, contre la règle dure du §6.3.
    ///     `PRESEANCE.md` la veut en réduction de coût : c'est le coût d'un cran, −5 %.
    ///   - « Pondre à l'heure » donnait `α + 0,05`. `α` n'est pas un terme qu'un facteur
    ///     multiplie proprement ; le nœud porte le facteur `(α + 0,05) / α` sur le terme
    ///     `GainDeDensite`, et `Densite.cs` l'applique à l'exposant.
    ///
    /// [P] graine — les valeurs que le noyau ne chiffre pas : la remise de « Réputation »,
    /// le facteur de « Coquille épaisse » (×2), et le niveau de départ d'« Élan » (5, lui,
    /// est au noyau).
    ///
    /// La branche CONSTRUCTION est transcrite mais MUETTE : son compteur — temples et
    /// portails bâtis — n'est crédité par rien tant que ces systèmes n'existent pas.
    /// </summary>
    public static class NoeudsTechnique
    {
        static NoeudTechnique Noeud(string id, BrancheTechnique branche, int rang, EffetDeNoeud effet) =>
            new NoeudTechnique(id, branche, rang, Constantes.COUTS_DE_NOEUD[rang - 1], effet);

        static EffetDeNoeud Chiffre(TermeDeFormule terme, double facteur) => EffetDeNoeud.Chiffre(terme, facteur);
        static EffetDeNoeud Verbe(CapaciteId capacite) => EffetDeNoeud.Verbe(capacite);

        public static readonly IReadOnlyList<NoeudTechnique> Tous = new[]
        {
            /* — Creusement : mana dépensé à creuser ——————————————————————————————— */
            Noeud("creusement-etaiement", BrancheTechnique.Creusement, 1, Chiffre(TermeDeFormule.CoutCreuser, 0.90)),
            Noeud("creusement-drainage", BrancheTechnique.Creusement, 2, Chiffre(TermeDeFormule.CoutCreuser, 0.80 / 0.90)),
            Noeud("creusement-file", BrancheTechnique.Creusement, 3, Verbe(CapaciteId.FileDeDescente)),
            Noeud("creusement-percement", BrancheTechnique.Creusement, 4, Chiffre(TermeDeFormule.CoutCreuser, 0.70 / 0.80)),
            Noeud("creusement-galeries", BrancheTechnique.Creusement, 5, Verbe(CapaciteId.CreusementAuto)),

            /* — Amélioration : mana dépensé en crans et en croissance ————————————— */
            Noeud("amelioration-repetition", BrancheTechnique.Amelioration, 1, Chiffre(TermeDeFormule.CoutNiveau, 0.92)),
            Noeud("amelioration-main-sure", BrancheTechnique.Amelioration, 2, Verbe(CapaciteId.AchatAuto)),
            Noeud("amelioration-geste", BrancheTechnique.Amelioration, 3, Chiffre(TermeDeFormule.CoutNiveau, 0.85 / 0.92)),
            Noeud("amelioration-elan", BrancheTechnique.Amelioration, 4, Chiffre(TermeDeFormule.NiveauDeDepart, 5)),
            Noeud("amelioration-suite", BrancheTechnique.Amelioration, 5, Verbe(CapaciteId.AchatAutoMax)),

            /* — Recrutement : espèces convaincues, toutes vies confondues ————————— */
            Noeud("recrutement-approche", BrancheTechnique.Recrutement, 1, Chiffre(TermeDeFormule.CoutDeblocage, 0.85)),
            Noeud("recrutement-habitudes", BrancheTechnique.Recrutement, 2, Verbe(CapaciteId.DeblocageAuto)),
            Noeud("recrutement-memoire", BrancheTechnique.Recrutement, 3, Chiffre(TermeDeFormule.CoutDeblocage, 0.70 / 0.85)),
            Noeud("recrutement-reputation", BrancheTechnique.Recrutement, 4, Chiffre(TermeDeFormule.CoutNiveau, 0.95)),
            Noeud("recrutement-refuge", BrancheTechnique.Recrutement, 5, Chiffre(TermeDeFormule.CoutDeblocage, 0.50 / 0.70)),

            /* — Entretien : heures hors ligne effectivement créditées ————————————— */
            // Compter les siens en tête (2026-11-14) : sans lui, les débits par espèce ne se
            // lisent pas, et à son ancien rang (~65 h d'absence) qui s'absente peu ne les
            // aurait jamais vus. La relève et la veille glissent d'un rang.
            Noeud("entretien-compte", BrancheTechnique.Entretien, 1, Verbe(CapaciteId.LectureDebits)),
            Noeud("entretien-releve", BrancheTechnique.Entretien, 2, Chiffre(TermeDeFormule.CapHorsLigne, 2)),
            Noeud("entretien-veille", BrancheTechnique.Entretien, 3, Chiffre(TermeDeFormule.CapHorsLigne, 2)),
            Noeud("entretien-peuple", BrancheTechnique.Entretien, 4, Verbe(CapaciteId.AutomatismesHorsLigne)),
            Noeud("entretien-rapport", BrancheTechnique.Entretien, 5, Verbe(CapaciteId.RapportDeRetour)),

            /* — Construction : temples et portails bâtis — MUETTE, voir l'en-tête ——— */
            Noeud("construction-fondations", BrancheTechnique.Construction, 1, Chiffre(TermeDeFormule.CoutTemple, 0.80)),
            Noeud("construction-tunnels", BrancheTechnique.Construction, 2, Chiffre(TermeDeFormule.CoutPortail, 0.75)),
            Noeud("construction-signaletique", BrancheTechnique.Construction, 3, Verbe(CapaciteId.NavigationDirecte)),
            Noeud("construction-reouverture", BrancheTechnique.Construction, 4, Chiffre(TermeDeFormule.CoutReouverture, 0.50)),
            Noeud("construction-collecteurs", BrancheTechnique.Construction, 5, Chiffre(TermeDeFormule.ChargeAllieeParReponse, 1.30)),

            /* — Renaissance : renaissances ———————————————————————————————————————— */
            Noeud("renaissance-heure", BrancheTechnique.Renaissance, 1,
                Chiffre(TermeDeFormule.GainDeDensite, (Constantes.ALPHA_GAIN_DE_DENSITE + 0.05) / Constantes.ALPHA_GAIN_DE_DENSITE)),
            Noeud("renaissance-lire-l-eau", BrancheTechnique.Renaissance, 2, Verbe(CapaciteId.LectureEau)),
            Noeud("renaissance-ce-qui-reste", BrancheTechnique.Renaissance, 3, Chiffre(TermeDeFormule.DensiteConservee, 1.10)),
            Noeud("renaissance-coquille", BrancheTechnique.Renaissance, 4, Chiffre(TermeDeFormule.ContenanceDeDepart, 2)),
            Noeud("renaissance-chemin-connu", BrancheTechnique.Renaissance, 5, Verbe(CapaciteId.RetourRapide)),
        };

        static readonly IReadOnlyDictionary<string, NoeudTechnique> PAR_ID = Tous.ToDictionary(n => n.Id);

        static readonly IReadOnlyDictionary<BrancheTechnique, IReadOnlyList<NoeudTechnique>> PAR_BRANCHE =
            Tous.GroupBy(n => n.Branche).ToDictionary(g => g.Key, g => (IReadOnlyList<NoeudTechnique>)g.OrderBy(n => n.Rang).ToList());

        static readonly IReadOnlyList<NoeudTechnique> AUCUN = new NoeudTechnique[0];

        public static NoeudTechnique ParId(string id) => id != null && PAR_ID.TryGetValue(id, out var noeud) ? noeud : null;

        /// Les nœuds d'une branche, dans l'ordre des rangs.
        public static IReadOnlyList<NoeudTechnique> DeLaBranche(BrancheTechnique branche) =>
            PAR_BRANCHE.TryGetValue(branche, out var noeuds) ? noeuds : AUCUN;
    }
}
