/*
 * IdlePond — les identifiants de chaîne des enums, et la partition des termes.
 *
 * Ces chaînes sont celles de la version web, à la lettre : elles entrent dans
 * les sauvegardes (`registre: "pente"`, `compteursTechnique.creusement`) et à
 * l'écran du détail de captation (`taux_base`). En changer une invaliderait
 * toutes les saves existantes — elles relèvent donc du registre figé, comme les
 * identifiants de succès.
 */
using System;
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    public static class Termes
    {
        public static readonly IReadOnlyList<TermeDeFormule> DeProduction = new[]
        {
            TermeDeFormule.TauxBase,
            TermeDeFormule.Effectif,
            TermeDeFormule.RendementAcclimatation,
            TermeDeFormule.MultiplicateurJalon,
            TermeDeFormule.MultiplicateurDrapeau,
            TermeDeFormule.PartMure,
            TermeDeFormule.DebitAcclimate,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DeCout = new[]
        {
            TermeDeFormule.CoutCreuser,
            TermeDeFormule.ReductionTechnique,
            TermeDeFormule.CoutPlace,
            TermeDeFormule.CoutReconviction,
            TermeDeFormule.CoutTemple,
            TermeDeFormule.CoutPortail,
            TermeDeFormule.CoutReouverture,
        };

        public static readonly IReadOnlyList<TermeDeFormule> DeConfort = new[]
        {
            TermeDeFormule.CapHorsLigne,
            TermeDeFormule.DensiteConservee,
            TermeDeFormule.ContenanceDeDepart,
            TermeDeFormule.PlaceDeDepart,
            TermeDeFormule.ChargeAllieeParReponse,
        };

        public static FamilleDeTerme Famille(TermeDeFormule terme)
        {
            foreach (var t in DeProduction) if (t == terme) return FamilleDeTerme.Production;
            foreach (var t in DeCout) if (t == terme) return FamilleDeTerme.Cout;
            return FamilleDeTerme.Confort;
        }

        public static string Identifiant(TermeDeFormule terme)
        {
            switch (terme)
            {
                case TermeDeFormule.TauxBase: return "taux_base";
                case TermeDeFormule.Effectif: return "effectif";
                case TermeDeFormule.RendementAcclimatation: return "rendement_acclimatation";
                case TermeDeFormule.MultiplicateurJalon: return "multiplicateur_jalon";
                case TermeDeFormule.MultiplicateurDrapeau: return "multiplicateur_drapeau";
                case TermeDeFormule.PartMure: return "part_mure";
                case TermeDeFormule.DebitAcclimate: return "debit_acclimate";
                case TermeDeFormule.CoutCreuser: return "cout_creuser";
                case TermeDeFormule.ReductionTechnique: return "reduction_technique";
                case TermeDeFormule.CoutPlace: return "cout_place";
                case TermeDeFormule.CoutReconviction: return "cout_reconviction";
                case TermeDeFormule.CoutTemple: return "cout_temple";
                case TermeDeFormule.CoutPortail: return "cout_portail";
                case TermeDeFormule.CoutReouverture: return "cout_reouverture";
                case TermeDeFormule.CapHorsLigne: return "cap_hors_ligne";
                case TermeDeFormule.DensiteConservee: return "densite_conservee";
                case TermeDeFormule.ContenanceDeDepart: return "contenance_de_depart";
                case TermeDeFormule.PlaceDeDepart: return "place_de_depart";
                case TermeDeFormule.ChargeAllieeParReponse: return "charge_alliee_par_reponse";
                default: throw new ArgumentOutOfRangeException(nameof(terme), terme, null);
            }
        }
    }

    public static class Branches
    {
        public static readonly IReadOnlyList<BrancheTechnique> Toutes = new[]
        {
            BrancheTechnique.Creusement,
            BrancheTechnique.Amelioration,
            BrancheTechnique.Recrutement,
            BrancheTechnique.Entretien,
            BrancheTechnique.Construction,
            BrancheTechnique.Eclosion,
        };

        public static string Identifiant(BrancheTechnique branche)
        {
            switch (branche)
            {
                case BrancheTechnique.Creusement: return "creusement";
                case BrancheTechnique.Amelioration: return "amelioration";
                case BrancheTechnique.Recrutement: return "recrutement";
                case BrancheTechnique.Entretien: return "entretien";
                case BrancheTechnique.Construction: return "construction";
                case BrancheTechnique.Eclosion: return "eclosion";
                default: throw new ArgumentOutOfRangeException(nameof(branche), branche, null);
            }
        }
    }

    public static class RegistresDeVoix
    {
        public static string Identifiant(PalierDeVoix palier)
        {
            switch (palier)
            {
                case PalierDeVoix.Pente: return "pente";
                case PalierDeVoix.Signes: return "signes";
                case PalierDeVoix.Directives: return "directives";
                case PalierDeVoix.Dialogue: return "dialogue";
                default: throw new ArgumentOutOfRangeException(nameof(palier), palier, null);
            }
        }

        public static bool EssayerDeLire(string identifiant, out PalierDeVoix palier)
        {
            switch (identifiant)
            {
                case "pente": palier = PalierDeVoix.Pente; return true;
                case "signes": palier = PalierDeVoix.Signes; return true;
                case "directives": palier = PalierDeVoix.Directives; return true;
                case "dialogue": palier = PalierDeVoix.Dialogue; return true;
                default: palier = PalierDeVoix.Pente; return false;
            }
        }
    }
}
