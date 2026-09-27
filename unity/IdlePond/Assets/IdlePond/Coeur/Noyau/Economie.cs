/*
 * IdlePond — production, coûts, seuils. Port de `src/noyau/economie.ts`.
 *
 * DEUX CANAUX ADDITIFS — GDD §3, « Fixé (canon) » au §16.1 :
 *
 *   captation/s =   débit_natif(population vivante présente)
 *                 + débit_acclimaté(part_mûre(palier) × rendement_acclimatation)
 *
 * Additifs, jamais multiplicatifs. Les deux se rejoignent dans
 * `ProductionTotaleParSeconde`, et nulle part ailleurs.
 */
using System;
using System.Collections.Generic;
using IdlePond.Donnees;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public static class Economie
    {
        /* ─── Seuils de jalon ───────────────────────────────────────────────── */

        /// <summary>
        /// Multiplicateur de seuil d'un banc, d'après son EFFECTIF (§2.C). Cumulé :
        /// on retient celui du seuil le plus haut franchi. Cent individus valent ×16.
        /// </summary>
        public static double MultiplicateurDeSeuil(double effectif)
        {
            double multiplicateur = 1;
            foreach (var seuil in Constantes.SeuilsDeJalon)
            {
                if (effectif >= seuil.Seuil) multiplicateur = seuil.MultiplicateurCumule;
            }
            return multiplicateur;
        }

        /// <summary>Bonus global des espèces ayant DÉJÀ atteint cent individus. Additif.</summary>
        public static double MultiplicateurDesDrapeaux(EtatJeu etat) =>
            1 + Constantes.BonusGlobalACentIndividus * etat.Permanent.EspecesAyantAtteintCent.Count;

        /* ─── Taux de base ──────────────────────────────────────────────────── */

        /// <summary>Production d'un palier à pleine puissance : `D^palier`. Mesurée sur le PALIER.</summary>
        public static GrandNombre TauxBaseDuPalier(int palier) => Echelles.PuissanceDeD(palier).Mul(Constantes.TauxBaseAuPalier0);

        public static GrandNombre TauxBaseDuBanc(Banc banc) =>
            TauxBaseDuPalier(banc.Palier).Div(Paliers.BancsDuPalier(banc.Palier).Count);

        /// <summary>Rendement du héros sur le type de mana du palier. Jamais repayé (Tier 0).</summary>
        public static double RendementAcclimatation(EtatJeu etat, int palier)
        {
            var typeMana = Assises.DuPalier(palier).TypeMana;
            return etat.Permanent.Acclimatations.Lire(typeMana, Constantes.RendementAcclimatationPleinJusquEnV05);
        }

        /// <summary>Taux par individu SANS le multiplicateur de seuil — le seul terme qui varie en cours de pas.</summary>
        public static GrandNombre TauxParIndividuHorsSeuil(EtatJeu etat, Banc banc) =>
            TauxBaseDuBanc(banc)
                .Mul(RendementAcclimatation(etat, banc.Palier))
                .Mul(MultiplicateurDesDrapeaux(etat));

        public static GrandNombre TauxParIndividu(EtatJeu etat, Banc banc, double effectif) =>
            TauxParIndividuHorsSeuil(etat, banc).Mul(MultiplicateurDeSeuil(effectif));

        public static GrandNombre ProductionDuBanc(EtatJeu etat, Banc banc)
        {
            if (!etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var bancEtat) || bancEtat.Place <= 0) return GrandNombre.Zero;
            return TauxParIndividu(etat, banc, bancEtat.Effectif).Mul(bancEtat.Effectif);
        }

        /* ─── Le canal acclimaté — GDD §3 et §3.0 ───────────────────────────── */

        public static double PartMureDuPalier(EtatJeu etat, int palier) =>
            palier >= 0 && palier < etat.Permanent.PartsMures.Count
                ? etat.Permanent.PartsMures[palier]
                : Maturation.PartMureDUneEauIntouchee;

        /// <summary>Place totale installée sur un palier — ce qui dilue son type (§3.0).</summary>
        public static double PlaceDuPalier(EtatJeu etat, int palier)
        {
            double place = 0;
            foreach (var banc in Paliers.Liste[palier].Bancs)
            {
                if (etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var bancEtat)) place += bancEtat.Place;
            }
            return place;
        }

        /// <summary>Débit acclimaté d'un palier, par seconde. Ne dépend d'AUCUN vivant.</summary>
        public static GrandNombre ProductionAcclimateeDuPalier(EtatJeu etat, int palier) =>
            TauxBaseDuPalier(palier)
                .Mul(Constantes.IndividusEquivalentsDuCanalAcclimate)
                .Mul(PartMureDuPalier(etat, palier))
                .Mul(RendementAcclimatation(etat, palier));

        /// <summary>La somme des deux canaux, sur tous les paliers ouverts.</summary>
        public static GrandNombre ProductionTotaleParSeconde(EtatJeu etat)
        {
            var total = GrandNombre.Zero;
            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
            {
                foreach (var banc in Paliers.Liste[palier].Bancs) total = total.Add(ProductionDuBanc(etat, banc));
                total = total.Add(ProductionAcclimateeDuPalier(etat, palier));
            }
            return total;
        }

        /// <summary>Détail de la captation (§8.2) : chaque terme actif attribuable à sa source.</summary>
        public static IReadOnlyList<LigneDeCaptation> DetailDeCaptation(EtatJeu etat, Banc banc)
        {
            etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var bancEtat);
            var effectif = bancEtat?.Effectif ?? 0;
            return new[]
            {
                new LigneDeCaptation(TermeDeFormule.Effectif, effectif, new SourceDeTerme { Quoi = QuoiSource.Population }),
                new LigneDeCaptation(TermeDeFormule.TauxBase, TauxBaseDuBanc(banc).ToNumber(),
                    new SourceDeTerme { Quoi = QuoiSource.Palier, Palier = banc.Palier }),
                new LigneDeCaptation(TermeDeFormule.RendementAcclimatation, RendementAcclimatation(etat, banc.Palier),
                    new SourceDeTerme { Quoi = QuoiSource.Acclimatation, TypeMana = Assises.DuPalier(banc.Palier).TypeMana }),
                new LigneDeCaptation(TermeDeFormule.MultiplicateurJalon, MultiplicateurDeSeuil(effectif),
                    new SourceDeTerme { Quoi = QuoiSource.Place, Place = bancEtat?.Place ?? 0 }),
                new LigneDeCaptation(TermeDeFormule.MultiplicateurDrapeau, MultiplicateurDesDrapeaux(etat),
                    new SourceDeTerme { Quoi = QuoiSource.DrapeauxPermanents, Especes = etat.Permanent.EspecesAyantAtteintCent.Count }),
            };
        }

        /// <summary>
        /// Détail du canal acclimaté d'un palier. Séparé du précédent : le natif se lit
        /// par banc, l'acclimaté par palier, et il tombe même quand il n'y a personne.
        /// </summary>
        public static IReadOnlyList<LigneDeCaptation> DetailDuCanalAcclimate(EtatJeu etat, int palier)
        {
            var part = PartMureDuPalier(etat, palier);
            return new[]
            {
                new LigneDeCaptation(TermeDeFormule.PartMure, part, new SourceDeTerme { Quoi = QuoiSource.EauMurie, Part = part }),
                new LigneDeCaptation(TermeDeFormule.RendementAcclimatation, RendementAcclimatation(etat, palier),
                    new SourceDeTerme { Quoi = QuoiSource.Acclimatation, TypeMana = Assises.DuPalier(palier).TypeMana }),
                new LigneDeCaptation(TermeDeFormule.DebitAcclimate, ProductionAcclimateeDuPalier(etat, palier).ToNumber(),
                    new SourceDeTerme { Quoi = QuoiSource.CanalAcclimate }),
            };
        }

        /* ─── Coûts ─────────────────────────────────────────────────────────── */

        /// <summary>
        /// Facteur appliqué à un terme de coût par les succès acquis. Lu directement
        /// dans l'état, jamais via `SystemeDeSucces` : les dépendances restent à sens
        /// unique.
        /// </summary>
        public static double FacteurDeSucces(EtatJeu etat, TermeDeFormule terme)
        {
            double facteur = 1;
            foreach (var succes in Donnees.RegistreDesSucces.Liste)
            {
                var effet = succes.Effet;
                if (effet == null || effet.Genre == GenreDEffet.Verbe) continue;
                if (effet.Terme != terme) continue;
                if (!etat.Permanent.Succes.Contient(succes.Id)) continue;
                facteur *= effet.Genre == GenreDEffet.ReductionCout ? 1 - effet.Part : 1 + effet.Part;
            }
            return facteur;
        }

        /// <summary>Technique et succès se composent sur un même terme, chacun nommé et attribuable.</summary>
        private static double FacteurDeCout(EtatJeu etat, TermeDeFormule terme) =>
            Technique.FacteurDeTechnique(etat, terme) * FacteurDeSucces(etat, terme);

        /// <summary>Coût d'origine d'un palier, avant tout levier. Le palier 0 est ouvert au départ.</summary>
        public static GrandNombre CoutBaseDuPalier(int cible) =>
            Echelles.PuissanceDeG(Math.Max(0, cible - 1)).Mul(Constantes.CoutCreuserAuPalier1);

        /// <summary>Vrai si ce palier a déjà été atteint dans une vie précédente — GDD §6.4.</summary>
        public static bool EstUnAmenagement(EtatJeu etat, int cible) => cible < etat.Permanent.ProfondeurMaxAtteinte;

        /// <summary>
        /// Ce que coûte de descendre d'un palier — les deux puits du GDD §4.1.
        ///   creuser   : coût_base(palier)
        ///   aménager  : coût_base(palier) × f × réduction_technique      (§6.4)
        /// « Un puits, un levier. »
        /// </summary>
        public static GrandNombre CoutDeDescente(EtatJeu etat, int cible)
        {
            var baseDuCout = CoutBaseDuPalier(cible);
            if (!EstUnAmenagement(etat, cible)) return baseDuCout.Mul(FacteurDeCout(etat, TermeDeFormule.CoutCreuser));
            return baseDuCout.Mul(Constantes.FFractionDAmenagement).Mul(FacteurDeCout(etat, TermeDeFormule.ReductionTechnique));
        }

        /// <summary>
        /// Coût de conviction d'un banc — GDD §7.1 :
        ///   coût_base(espèce) ÷ affinité(type, espèce) ÷ densité_locale_du_type
        /// Aucun facteur de technique ni de succès n'entre ici : la conviction est
        /// payée par la densité, et par elle seule.
        /// </summary>
        public static GrandNombre CoutDeConviction(EtatJeu etat, Banc banc)
        {
            var memoireDuMonde = Math.Pow(1 + Densite.DensiteDuPalier(etat, banc.Palier), Constantes.ExposantReconvictionDensite);
            return Echelles.PuissanceDeG(banc.Palier)
                .Mul(Constantes.CoutDeblocageAuPalier0)
                .Div(Constantes.AffinitePleineJusquEnV05)
                .Div(memoireDuMonde);
        }

        /// <summary>Coût d'une place de plus. Achat répétable, ×1.15.</summary>
        public static GrandNombre CoutDePlace(EtatJeu etat, Banc banc, int place) =>
            Echelles.PuissanceDeG(banc.Palier)
                .Mul(Constantes.CoutDePlaceAuPalier0)
                .Mul(Echelles.PuissanceDuCoutDeNiveau(Math.Max(0, place - 1)))
                .Mul(FacteurDeCout(etat, TermeDeFormule.CoutPlace));

        /* ─── Contenance et blocage doux (§6.4) ─────────────────────────────── */

        /// <summary>La contenance limite le stock, pas la production.</summary>
        public static GrandNombre Contenance(EtatJeu etat) => etat.Permanent.ContenanceMana;

        /* ─── La jauge et sa saturation — GDD §2.4 ──────────────────────────── */

        /// <summary>Part du plafond effectivement portée, de 0 à 1.</summary>
        public static double PartDeContenance(EtatJeu etat)
        {
            var plafond = Contenance(etat);
            if (plafond.Lte(0)) return 0;
            return Math.Min(1, etat.Cycle.ManaCourant.Div(plafond).ToNumber());
        }

        /// <summary>L'alerte : « l'eau se trouble ». Le noyau rend l'état, jamais l'effet.</summary>
        public static bool EauTroublee(EtatJeu etat) => PartDeContenance(etat) >= Constantes.SeuilDAlerteDeContenance;

        /// <summary>Saturation : « la captation s'arrête. Il dépense encore, il ne gagne plus. »</summary>
        public static bool EstSature(EtatJeu etat) => etat.Cycle.ManaCourant.Gte(Contenance(etat));

        /// <summary>La divergence non choisie est due : la jauge est restée pleine trop longtemps.</summary>
        public static bool DivergenceNonChoisieEstDue(EtatJeu etat) =>
            etat.Cycle.SecondesEnSaturation >= Constantes.DelaiDeDivergenceNonChoisieHeures * 3600;

        /// <summary>Plus rien à creuser : soit la roche est finie, soit le contenu l'est.</summary>
        public static bool ToutEstCreuse(EtatJeu etat) =>
            etat.Cycle.PaliersOuverts >= Math.Min(etat.LimiteDeContenu, Constantes.NombreDePaliers);

        /// <summary>
        /// Le blocage doux : le palier suivant coûte plus que ce que la contenance peut
        /// porter. C'est la raison diégétique de l'éclosion, et sa seule vraie décision.
        /// </summary>
        public static bool EstBloque(EtatJeu etat)
        {
            if (ToutEstCreuse(etat)) return true;
            return CoutDeDescente(etat, etat.Cycle.PaliersOuverts).Gt(Contenance(etat));
        }
    }
}
