/*
 * IdlePond — tous les paramètres, un seul endroit (§5.3). Port de
 * `src/noyau/constantes.ts`, valeur pour valeur.
 *
 * Trois sections, et elles ne se mélangent pas :
 *   §13.1 FIXÉS    — décision de canon. Ne pas toucher sans en ouvrir une.
 *   §13.2 DÉRIVÉS  — recalculés ici, jamais saisis à la main.
 *   §13.3 GRAINES  — marqués « à mesurer ». Une graine est là pour être réfutée
 *                    par le simulateur ou la télémétrie.
 *
 * Tant que la version web reste la référence, une graine se change DES DEUX
 * CÔTÉS à la fois, et le test de parité (`ParitéAvecLeWebTests`) le rappelle à
 * qui l'oublierait.
 */
using System;
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    public sealed record SeuilPondere(double Seuil, double MultiplicateurCumule);

    public static class Constantes
    {
        /* ═══ §13.1 — FIXÉS ═══════════════════════════════════════════════════ */

        /// <summary>`g` — coût de palier. Chaque palier coûte ×2.4 le précédent.</summary>
        public const double GCoutPalier = 2.4;

        /// <summary>
        /// Ce que l'éclosion emporte du peuplement et de la géométrie. 1 = tout. À NE
        /// PAS CONFONDRE avec le `f` du GDD §6.4 : c'est `FFractionDAmenagement`.
        /// </summary>
        public const double FTarifRedescente = 1;

        /// <summary>Coût de place, achat répétable.</summary>
        public const double RatioCoutNiveau = 1.15;

        /// <summary>
        /// Seuils et MULTIPLICATEUR CUMULÉ — amendement v1.1 §2.C. Cent individus
        /// valent ×16, jamais ×1024 : `D = 2.31` a été calibré contre cette lecture.
        /// </summary>
        public static readonly IReadOnlyList<SeuilPondere> SeuilsDeJalon = new[]
        {
            new SeuilPondere(10, 2),
            new SeuilPondere(25, 4),
            new SeuilPondere(50, 8),
            new SeuilPondere(100, 16),
        };

        /// <summary>Au-delà, une espèce pose un drapeau PERMANENT (§2.C).</summary>
        public const double SeuilDuDrapeauPermanent = 100;

        public const int NombreDePaliers = 62;
        public const int NombreDAssises = 6;
        public const double PaliersParCycleVise = 4.4;
        public const int NombreDEclosionsVise = 15;
        public const int NombreDEspecesDeBase = 21;
        public const int NombreDeDivergences = 6;

        /// <summary>Plafond hors ligne : 6 h au départ, 24 h par la branche Entretien.</summary>
        public const double CapHorsLigneHeuresInitial = 6;
        public const double CapHorsLigneHeuresMaximum = 24;

        public static readonly IReadOnlyList<double> CoutsDeNoeud = new double[] { 5, 12, 25, 45, 80 };

        /// <summary>§13.4 — cibles de courbe fixées.</summary>
        public const double DureeDuCycle1Heures = 3;
        public const double CroissanceParCycleVisee = 1.18;

        /* ═══ §13.2 — DÉRIVÉS ═════════════════════════════════════════════════ */

        /// <summary>`g/D` = croissance_par_cycle ^ (1 / paliers_par_cycle).</summary>
        public static readonly double RapportGSurD = Math.Pow(CroissanceParCycleVisee, 1 / PaliersParCycleVise);

        /// <summary>`D` — production totale d'un palier, rapportée au précédent.</summary>
        public static readonly double DProductionParPalier = GCoutPalier / RapportGSurD;

        /// <summary>Contenance par éclosion : `g ^ paliers_par_cycle` = ×47,1. Atteinte VIA l'acquis de séjour.</summary>
        public static readonly double ContenanceParEclosion = Math.Pow(GCoutPalier, PaliersParCycleVise);

        /* ═══ §13.3 — GRAINES, À MESURER ══════════════════════════════════════ */

        /// <summary>[P] graine — `α`, gain de densité.</summary>
        public const double AlphaGainDeDensite = 0.6;

        /// <summary>`θ` — part du besoin que la densité compense. Borne dure [0, 1]. [P] graine.</summary>
        public const double ThetaPartCompensee = 0.8;

        /// <summary>Exposant du multiplicateur de densité — DÉRIVÉ, jamais saisi : θ/α.</summary>
        public static double DensiteExposant() => ThetaPartCompensee / AlphaGainDeDensite;

        /// <summary>[P5] graine — `f` du GDD §6.4 : fraction du coût d'origine d'un palier retraversé.</summary>
        public const double FFractionDAmenagement = 0.25;

        /// <summary>[P] graine — exposant de la densité dans le coût de conviction : (1 + densité)^e.</summary>
        public const double ExposantReconvictionDensite = 0.5;

        /// <summary>[P29] graine — temps caractéristique de maturation d'un palier, en heures.</summary>
        public const double TauMaturationHeures = 6;

        /// <summary>[P29] graine — place à laquelle l'eau d'un palier est moitié vive, moitié mûre.</summary>
        public const double PlaceQuiDilueAMoitie = 10;

        /// <summary>[P] graine — force du canal acclimaté, en individus équivalents. Volontairement petite.</summary>
        public const double IndividusEquivalentsDuCanalAcclimate = 1;

        /// <summary>[P] — l'affinité du §7.1 est du contenu v0.5. D'ici là elle vaut 1 partout.</summary>
        public const double AffinitePleineJusquEnV05 = 1;

        /// <summary>[P] graine — `k`, taux de repeuplement, par seconde.</summary>
        public const double KTauxDeRepeuplement = 1.0 / 300;

        /// <summary>[P] graine — bonus global par espèce ayant déjà atteint cent individus. Additif.</summary>
        public const double BonusGlobalACentIndividus = 0.03;

        /* ─── Graines d'échelle économique ──────────────────────────────────── */

        /// <summary>[P] graine — taux de base au palier 0, mana/s par individu.</summary>
        public const double TauxBaseAuPalier0 = 0.2;

        /// <summary>[P] graine — coût de creusement du palier 1. Croît ensuite en g^index.</summary>
        public const double CoutCreuserAuPalier1 = 60;

        /// <summary>[P] graine — coût de conviction d'un banc du palier 0. Croît en g^index.</summary>
        public const double CoutDeblocageAuPalier0 = 10;

        /// <summary>[P] graine — coût de la première PLACE d'un banc du palier 0. Croît en g^index.</summary>
        public const double CoutDePlaceAuPalier0 = 8;

        /// <summary>Nombre de paliers ouverts au début d'un cycle.</summary>
        public const int PaliersOuvertsAuDepart = 1;

        /// <summary>[P] graine — mana porté à la sortie de l'œuf. Calé sur le coût de la première conviction.</summary>
        public const double ManaALaSortieDeLOeuf = CoutDeblocageAuPalier0;

        /* ─── Contenance et acquis de séjour — amendement v1.1 §2.B ──────────── */

        public const double ContenanceInitiale = 1200;

        /// <summary>`A∞` — plafond de l'acquis de séjour. [P] graine, résolue à l'envers avec `τ₀`.</summary>
        public const double AcquisMax = 47.6;

        /// <summary>`τ₀` — temps caractéristique du séjour, en heures, à densité neutre. [P] graine.</summary>
        public const double TauSejourHeures = 0.87;

        /* ─── Graines d'éclosion ─────────────────────────────────────────────── */

        public const double ProductionDeReference = 1;
        public const double FoiBase = 1;
        public const double FoiExposant = 0.5;

        /* ─── Acclimatation ──────────────────────────────────────────────────── */

        public const double RendementAcclimatationPleinJusquEnV05 = 1;

        /* ─── Succès ─────────────────────────────────────────────────────────── */

        /// <summary>Fraction de sa cible qu'un effectif doit atteindre pour qu'un palier compte comme saturé.</summary>
        public const double SaturationDUnPalier = 0.99;

        /* ─── Saturation de la jauge — GDD §2.4 ──────────────────────────────── */

        /// <summary>Alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. »</summary>
        public const double SeuilDAlerteDeContenance = 0.85;

        /// <summary>
        /// [P] graine — délai de saturation CONTINUE au bout duquel la divergence se
        /// déclenche seule. Hors d'atteinte d'un retour au plafond MAXIMAL : deux
        /// absences pleines sans le moindre geste.
        /// </summary>
        public const double DelaiDeDivergenceNonChoisieHeures = 2 * CapHorsLigneHeuresMaximum;

        /// <summary>[P] graine — part de l'acquis de séjour que fixe une divergence NON CHOISIE.</summary>
        public const double PartDAcquisFixeeParDivergenceNonChoisie = 0.5;

        /* ─── Paliers de voix — GDD §13.1 ────────────────────────────────────── */

        public const int FranchissementsPourLesSignes = 1;
        public const int FranchissementsPourLesDirectives = 3;

        /// <summary>§8.4 — plancher garanti sur l'assise I.</summary>
        public const double PremierSuccesAvantSecondes = 120;
        public const double CadenceMaxEntreSuccesSecondes = 5 * 60;
        public const double FenetreDuPlancherDeCadenceSecondes = 30 * 60;

        /// <summary>[P] graine — PART retirée d'un coût par un succès de la Noue.</summary>
        public const double PartRemiseDUnSucces = 0.02;

        /* ─── Budget de verbes (§7.5 règle 2) ────────────────────────────────── */

        public const int BudgetDeVerbesTotal = 15;
        public const int BudgetDeVerbesArbre = 10;

        /// <summary>En deçà, l'absence est créditée mais pas annoncée.</summary>
        public const double SecondesMinimalesPourAnnoncerLeRetour = 60;

        /* ─── Boucle ─────────────────────────────────────────────────────────── */

        /// <summary>Le jeu appelle tick à 100 ms. Le simulateur l'appelle avec 60 s ou 8 h.</summary>
        public const double PeriodeDeTickMs = 100;

        /// <summary>Version de save courante. Toute évolution passe par une migration.</summary>
        public const int VersionSave = 4;
    }
}
