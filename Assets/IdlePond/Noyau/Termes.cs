using System.Collections.Generic;
using System.Linq;

namespace IdlePond.Noyau
{
    /// <summary>
    /// La partition des termes (§7.5 règle 3) : elle rend vérifiable par un test,
    /// plutôt que par une relecture, qu'aucun système gratuit ne monte la production.
    /// </summary>
    public static class Termes
    {
        public static readonly IReadOnlyList<TermeDeFormule> DE_PRODUCTION = new[]
        {
            TermeDeFormule.TauxBase, TermeDeFormule.Niveau, TermeDeFormule.MultiplicateurJalon,
            TermeDeFormule.MultiplicateurDrapeau, TermeDeFormule.MultiplicateurProfondeur,
            TermeDeFormule.MultiplicateurDensite, TermeDeFormule.DebitHeros, TermeDeFormule.MultiplicateurHeros,
            TermeDeFormule.MultiplicateurInsufflation, TermeDeFormule.InsufflationGlobale,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DE_COUT = new[]
        {
            TermeDeFormule.CoutCreuser, TermeDeFormule.CoutNiveau, TermeDeFormule.CoutDeblocage,
            TermeDeFormule.CoutCroissance, TermeDeFormule.CoutInsufflation, TermeDeFormule.CoutTemple,
            TermeDeFormule.CoutPortail, TermeDeFormule.CoutReouverture,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DE_CONFORT = new[]
        {
            TermeDeFormule.CapHorsLigne, TermeDeFormule.DensiteConservee, TermeDeFormule.ContenanceDeDepart,
            TermeDeFormule.NiveauDeDepart, TermeDeFormule.ChargeAllieeParReponse,
        };

        public static bool EstDeProduction(TermeDeFormule t) => DE_PRODUCTION.Contains(t);
        public static bool EstDeCout(TermeDeFormule t) => DE_COUT.Contains(t);
        public static bool EstDeConfort(TermeDeFormule t) => DE_CONFORT.Contains(t);

        /// Le nom du lexique, en snake_case : celui des références et du détail de captation.
        public static string Identifiant(TermeDeFormule t)
        {
            var nom = t.ToString();
            var sortie = new System.Text.StringBuilder();
            for (var i = 0; i < nom.Length; i++)
            {
                if (i > 0 && char.IsUpper(nom[i])) sortie.Append('_');
                sortie.Append(char.ToLowerInvariant(nom[i]));
            }
            return sortie.ToString();
        }
    }
}
