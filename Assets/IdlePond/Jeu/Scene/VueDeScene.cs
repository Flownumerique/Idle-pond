using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu.Scene
{
    public sealed record VueDEspece(string Id, int Rang, int Niveau);

    public sealed record VueDePalier(
        int Index,
        string Assise,
        // 1 à 6.
        int RangDAssise,
        // L'espèce débloquée que ce palier porte, ou rien.
        VueDEspece Espece);

    /// `Stade` : 0 à 3, la taille dessinée du corps (spec DA §3). Il remplace l'échelle
    /// continue de la spec 2026-09-17 [D12] : le pixel art ne s'agrandit pas.
    public sealed record VueDuHeros(int Niveau, int Stade, IReadOnlyList<string> Couches);

    /// <summary>
    /// IdlePond — la vue de la scène : ce que la scène dessine, sans Unity.
    ///
    /// Projection PURE de l'état (spec 2026-09-17 [D9]). La scène ne lit jamais
    /// `EtatJeu` : elle reçoit cette structure et la dessine. C'est ce qui rend le
    /// décor testable par ses données, et remplaçable par de vrais sprites sans toucher
    /// au noyau. Même contrainte de pureté que le noyau : aucune référence à
    /// UnityEngine (`VueDeSceneTests` le vérifie).
    /// </summary>
    public sealed class VueDeScene
    {
        /// Au-delà, le banc ne grossit plus : il resterait illisible dans une bande de 56.
        public const int POISSONS_MAX_PAR_GROUPE = 14;

        string clef;

        public VueDeScene(IReadOnlyList<VueDePalier> paliers, VueDuHeros heros, bool eauTroublee, bool sature)
        {
            Paliers = paliers;
            Heros = heros;
            EauTroublee = eauTroublee;
            Sature = sature;
        }

        public IReadOnlyList<VueDePalier> Paliers { get; }
        public VueDuHeros Heros { get; }
        public bool EauTroublee { get; }
        public bool Sature { get; }

        /// <summary>
        /// La mue (spec DA §3) : le corps passe à un stade plus grand. Jamais au premier
        /// dessin — une partie chargée n'a pas mué —, jamais en rapetissant — la renaissance
        /// n'est pas une mue. La vue ne connaît qu'un état : c'est la scène qui retient le
        /// stade qu'elle affiche.
        /// </summary>
        public static bool EstUneMue(int? stadeAffiche, int nouveauStade) =>
            stadeAffiche.HasValue && nouveauStade > stadeAffiche.Value;

        /// <summary>
        /// L'effectif dessiné d'un banc : `1 + ⌊log₂ niveau⌋`, plafonné. Un logarithme,
        /// parce que le niveau d'une espèce monte à des centaines : le joueur doit voir
        /// le banc grossir, pas le compter.
        /// </summary>
        public static int EffectifDesPoissons(int niveau) =>
            Math.Min(POISSONS_MAX_PAR_GROUPE, 1 + (int)Math.Floor(Math.Log(Math.Max(1, niveau), 2)));

        public static VueDeScene Depuis(EtatJeu etat)
        {
            var paliers = new List<VueDePalier>();
            for (var index = 0; index < etat.Cycle.PaliersOuverts; index += 1)
            {
                var palier = DuPalier(index);
                var assise = Assises.DuPalier(index);
                VueDEspece espece = null;
                if (palier.Espece != null)
                {
                    var definition = Especes.ParId(palier.Espece);
                    if (etat.Cycle.Especes.TryGetValue(palier.Espece, out var vivante) && vivante.Debloquee && definition != null)
                        espece = new VueDEspece(definition.Id, definition.Rang, vivante.Niveau);
                }
                paliers.Add(new VueDePalier(index, assise.Id, assise.Rang, espece));
            }
            return new VueDeScene(
                paliers,
                new VueDuHeros(etat.Cycle.NiveauDuHeros, Gabarits.StadeDuNiveau(etat.Cycle.NiveauDuHeros), etat.Permanent.Couches),
                Economie.EauTroublee(etat),
                Economie.EstSature(etat));
        }

        static Palier DuPalier(int index) => IdlePond.Noyau.Donnees.Paliers.Tous[index];

        /// <summary>
        /// Une clé de comparaison : deux vues qui dessinent la même chose ont la même clé.
        /// En C#, les `record` ne comparent pas leurs listes ; le TypeScript comparait le
        /// JSON de la vue, et une chaîne coûte moins qu'un redessin. Il n'y a que des entiers,
        /// des identifiants et des booléens dans la clé : aucune écriture de flottant.
        /// </summary>
        public string Clef
        {
            get
            {
                if (clef != null) return clef;
                var sb = new StringBuilder();
                foreach (var p in Paliers)
                {
                    sb.Append(p.Index).Append(':').Append(p.Assise).Append(':').Append(p.RangDAssise);
                    if (p.Espece != null) sb.Append(':').Append(p.Espece.Id).Append(':').Append(p.Espece.Rang).Append(':').Append(p.Espece.Niveau);
                    sb.Append(';');
                }
                sb.Append('|').Append(Heros.Niveau).Append(':').Append(Heros.Stade)
                  .Append(':').Append(string.Join(",", Heros.Couches ?? Array.Empty<string>()))
                  .Append('|').Append(EauTroublee ? 1 : 0).Append(Sature ? 1 : 0);
                return clef = sb.ToString();
            }
        }
    }
}
