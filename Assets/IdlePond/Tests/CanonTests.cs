using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Tests de canon — les phrases qui, si elles sont violées, invalident le build.
    ///
    /// Ils portent sur des registres aujourd'hui vides. C'est délibéré : le §8 note
    /// que le typage, la visibilité et le registre figé « ne se rétrofitent pas », et
    /// la même chose vaut pour les gardes. Un test de frontière ajouté après le
    /// contenu ne fait que constater les dégâts.
    ///
    /// Seuls les tests de donnée et de chiffre sont ici ; les tests lexicaux
    /// (balayage des sources et des textes) viennent à la tâche 9.
    /// </summary>
    public class CanonTests
    {
        /* ─── noyau v1.0 §4 — le Souffle achète des insufflations, et rien d'autre ne
         * monte la production ──────────────────────────────────────────────────────
         *
         * RETOURNÉ une seconde fois, le 2026-09-17. Le 2026-09-08 ce bloc avait
         * supprimé les insufflations au nom du GDD §4.2 ; le soir même la préséance
         * est passée au noyau v1.0 pour la mécanique (`docs/PRESEANCE.md`), et le
         * noyau §4 fait des insufflations « l'écran d'améliorations du jeu ». Le code
         * avait gardé la suppression. Spec 2026-09-17 [D8].
         *
         * Ce qui reste vrai, et vérifié : la technique et les succès ne montent
         * jamais une production ; une insufflation ne fait QUE cela. */

        [Test, Description("aucun nœud de technique ne monte une production")]
        public void Aucun_noeud_de_technique_ne_monte_une_production()
        {
            foreach (var noeud in NoeudsTechnique.Tous)
            {
                if (noeud.Effet.Nature != NatureDEffet.Chiffre) continue;
                var admis = Termes.DE_COUT.Concat(Termes.DE_CONFORT).ToList();
                Assert.That(admis, Has.Member(noeud.Effet.Terme), $"le nœud {noeud.Id} cible {noeud.Effet.Terme}");
            }
        }

        [Test, Description("une insufflation ne cible qu'un terme de production, jamais un coût ni un plafond")]
        public void Une_insufflation_ne_cible_qu_un_terme_de_production_jamais_un_cout_ni_un_plafond()
        {
            foreach (var insufflation in Insufflations.Toutes)
            {
                var terme = insufflation.Portee == PorteeDInsufflation.Ciblee
                    ? TermeDeFormule.MultiplicateurInsufflation
                    : TermeDeFormule.InsufflationGlobale;
                Assert.That(Termes.DE_PRODUCTION, Has.Member(terme));
                Assert.That(Termes.DE_COUT, Has.No.Member(terme));
                Assert.That(Termes.DE_CONFORT, Has.No.Member(terme));
            }
        }

        [Test, Description("une insufflation ciblée par espèce, une globale, et pas une de plus")]
        public void Une_insufflation_ciblee_par_espece_une_globale_et_pas_une_de_plus()
        {
            var ciblees = Insufflations.Toutes.Where(i => i.Portee == PorteeDInsufflation.Ciblee).ToList();
            var globales = Insufflations.Toutes.Where(i => i.Portee == PorteeDInsufflation.Globale).ToList();
            Assert.That(ciblees.Select(i => i.Espece), Is.EqualTo(Especes.Toutes.Select(e => e.Id)));
            Assert.That(globales, Has.Count.EqualTo(1));
            Assert.That(globales[0].Espece, Is.Null);
        }

        [Test, Description("aucun succès ne monte une production (amendement v1.1 §2.D)")]
        public void Aucun_succes_ne_monte_une_production_amendement_v1_1_2_D()
        {
            // « Un succès ne peut porter qu'un effet qui existe déjà comme terme de
            // technique : réduction de coût, relèvement de plafond, ou verbe. »
            // Le typage l'interdit déjà ; ce test le vérifie sur la donnée, parce
            // qu'un registre peut un jour venir d'ailleurs que du compilateur.
            var genresAdmis = new[] { GenreDEffetDeSucces.ReductionCout, GenreDEffetDeSucces.Plafond, GenreDEffetDeSucces.Verbe };
            foreach (var succes in RegistreDesSucces.Tous)
            {
                if (succes.Effet == null) continue;
                Assert.That(genresAdmis, Has.Member(succes.Effet.Genre), $"le succès {succes.Id}");
                if (succes.Effet.Genre == GenreDEffetDeSucces.Verbe) continue;
                Assert.That(Termes.DE_PRODUCTION, Has.No.Member(succes.Effet.Terme),
                    $"le succès {succes.Id} cible le terme de production {succes.Effet.Terme}");
            }
        }

        /*
         * RETIRÉ le 2026-09-09 : « un puits, un levier — rien ne double la densité
         * sur la conviction ».
         *
         * Il vérifiait qu'aucun nœud ni succès ne visait `cout_reconviction`, parce
         * que la conviction était payée par la DENSITÉ et par elle seule (GDD §7.1) :
         * lui donner un second levier rendait l'ensemble inéquilibrable. Le terme est
         * devenu `cout_deblocage`, et sa formule ne lit plus la densité du tout — le
         * noyau v1.0 §1.3 en fait une fraction du coût de son palier. Il n'y a donc
         * plus de premier levier à protéger, et interdire le second reviendrait à
         * défendre une règle dont l'objet a disparu.
         *
         * `reduction_technique` et le puits d'aménagement qu'il portait sont partis
         * à leur tour le 2026-09-09 (tâche 6, noyau v1.0 §3.1) : `f` = 1, il n'y a
         * plus qu'un seul puits de descente, `cout_creuser`. Ce qui reste vrai et
         * reste vérifié : aucun effet ne monte une production.
         */

        [Test, Description("aucun effet chiffré ne flotte sans terme nommé")]
        public void Aucun_effet_chiffre_ne_flotte_sans_terme_nomme()
        {
            foreach (var noeud in NoeudsTechnique.Tous)
            {
                if (noeud.Effet.Nature != NatureDEffet.Chiffre) continue;
                Assert.That(noeud.Effet.Terme.HasValue, Is.True, $"le nœud {noeud.Id}");
            }
        }

        /* ─── §7.5 — les trois règles dures de l'arbre ────────────────────────── */

        [Test, Description("une capacité a exactement une source")]
        public void Une_capacite_a_exactement_une_source()
        {
            var parLArbre = new HashSet<CapaciteId>();
            foreach (var noeud in NoeudsTechnique.Tous)
                if (noeud.Effet.Nature == NatureDEffet.Verbe) parLArbre.Add(noeud.Effet.Capacite.Value);
            foreach (var succes in RegistreDesSucces.Tous)
            {
                if (succes.Effet?.Genre != GenreDEffetDeSucces.Verbe) continue;
                Assert.That(parLArbre.Contains(succes.Effet.Capacite.Value), Is.False,
                    $"{succes.Effet.Capacite} est atteignable par l’arbre ET par le succès {succes.Id}");
            }
        }

        [Test, Description("le budget de verbes est commun et tenu")]
        public void Le_budget_de_verbes_est_commun_et_tenu()
        {
            var verbesDeLArbre = NoeudsTechnique.Tous.Count(n => n.Effet.Nature == NatureDEffet.Verbe);
            var verbesDesSucces = RegistreDesSucces.Tous.Count(s => s.Effet?.Genre == GenreDEffetDeSucces.Verbe);
            Assert.That(verbesDeLArbre, Is.LessThanOrEqualTo(Constantes.BUDGET_DE_VERBES_ARBRE));
            Assert.That(verbesDeLArbre + verbesDesSucces, Is.LessThanOrEqualTo(Constantes.BUDGET_DE_VERBES_TOTAL));
        }

        /* ─── §13 — les valeurs fixées et leurs dérivations ───────────────────── */

        [Test, Description("g/D se dérive de la croissance et des paliers par cycle, jamais saisi")]
        public void G_D_se_derive_de_la_croissance_et_des_paliers_par_cycle_jamais_saisi()
        {
            Assert.That(Constantes.RAPPORT_G_SUR_D,
                Is.EqualTo(Math.Pow(Constantes.CROISSANCE_PAR_CYCLE_VISEE, 1 / Constantes.PALIERS_PAR_CYCLE_VISE)).Within(0.5e-12));
            // Le §6.3 cite « 1.039 » ; la dérivation exacte donne 1.03833. L'écart est
            // un arrondi du document, pas un désaccord : c'est bien la dérivation qui
            // fait foi, le §13.2 classant g/D en « recalculer, ne pas saisir ».
            Assert.That(Math.Abs(Constantes.RAPPORT_G_SUR_D - 1.039), Is.LessThan(1e-3));
        }

        [Test, Description("D vaut g / 1.039, soit 2.31")]
        public void D_vaut_g_1_039_soit_2_31()
        {
            Assert.That(Constantes.D_PRODUCTION_PAR_PALIER, Is.EqualTo(Constantes.G_COUT_PALIER / Constantes.RAPPORT_G_SUR_D).Within(0.5e-12));
            Assert.That(Constantes.D_PRODUCTION_PAR_PALIER, Is.EqualTo(2.31).Within(0.005));
        }

        [Test, Description("D ≠ g, sinon la durée d’un cycle est plate")]
        public void D_g_sinon_la_duree_d_un_cycle_est_plate()
        {
            Assert.That(Constantes.D_PRODUCTION_PAR_PALIER, Is.Not.EqualTo(Constantes.G_COUT_PALIER).Within(0.005));
        }

        [Test, Description("f = 1 : reset complet, et la constante elle-même n’existe plus")]
        public void F_1_reset_complet_et_la_constante_elle_meme_n_existe_plus()
        {
            // En TypeScript, un balayage des sources du noyau. Ici, la réflexion sur
            // l'assemblage du noyau : ni la constante du tarif de redescente, ni le
            // prédicat d'aménagement qui la lisait, ne doivent y exister sous aucun nom.
            Assert.That(typeof(Constantes).GetField("F_TARIF_REDESCENTE"), Is.Null);
            const BindingFlags tout = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var membres = typeof(Constantes).Assembly.GetTypes()
                .SelectMany(t => t.GetMembers(tout))
                .Select(m => m.Name)
                .Where(nom => nom.IndexOf("F_TARIF_REDESCENTE", StringComparison.OrdinalIgnoreCase) >= 0
                    || nom.IndexOf("EstUnAmenagement", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            Assert.That(membres, Is.Empty);
        }

        [Test, Description("les seuils sont CUMULÉS : le centième niveau vaut ×16, pas ×1024")]
        public void Les_seuils_sont_CUMULES_le_centieme_niveau_vaut_16_pas_1024()
        {
            // Le §2.C ordonne cette vérification avant toute ligne de code : `D = 2.31`
            // a été ajusté contre cette lecture, et une table multiplicative rendrait
            // tout le calibrage faux.
            Assert.That(Constantes.SEUILS_DE_JALON.Select(s => s.MultiplicateurCumule), Is.EqualTo(new[] { 2, 4, 8, 16 }));
            Assert.That(Constantes.SEUILS_DE_JALON.Select(s => s.Seuil), Is.EqualTo(new[] { 10, 25, 50, 100 }));
        }

        [Test, Description("θ borné à [0, 1], et l’exposant de densité dérivé de θ/α")]
        public void Theta_borne_a_0_1_et_l_exposant_de_densite_derive_de_theta_alpha()
        {
            Assert.That(Constantes.THETA_PART_COMPENSEE, Is.GreaterThanOrEqualTo(0));
            Assert.That(Constantes.THETA_PART_COMPENSEE, Is.LessThanOrEqualTo(1));
            Assert.That(Constantes.DensiteExposant(),
                Is.EqualTo(Constantes.THETA_PART_COMPENSEE / Constantes.ALPHA_GAIN_DE_DENSITE).Within(0.5e-12));
        }

        [Test, Description("62 paliers, 6 assises, 21 espèces de base")]
        public void _62_paliers_6_assises_21_especes_de_base()
        {
            Assert.That(Paliers.Tous.Count, Is.EqualTo(Constantes.NOMBRE_DE_PALIERS));
            Assert.That(Assises.Toutes.Count, Is.EqualTo(6));
            Assert.That(Especes.Toutes.Count, Is.EqualTo(Constantes.NOMBRE_D_ESPECES_DE_BASE));
        }

        [Test, Description("chaque palier appartient à une assise, et porte au plus une espèce")]
        public void Chaque_palier_appartient_a_une_assise_et_porte_au_plus_une_espece()
        {
            foreach (var palier in Paliers.Tous)
            {
                Assert.That(Assises.Toutes.Any(a => a.Id == palier.Assise), Is.True);
                if (palier.Espece == null) continue;
                Assert.That(Especes.Toutes.Select(e => e.Id), Has.Member(palier.Espece));
            }
            Assert.That(Paliers.Tous.Where(p => p.Espece != null).ToList(), Has.Count.EqualTo(Constantes.NOMBRE_D_ESPECES_DE_BASE));
        }

        [Test, Description("une espèce est ancrée à trois fois son rang, sinon son débit décroche de D")]
        public void Une_espece_est_ancree_a_trois_fois_son_rang_sinon_son_debit_decroche_de_D()
        {
            // Le débit de base d'une espèce croît de `DEBIT_RATIO_ESPECE` par RANG, et
            // le multiplicateur de profondeur porte le reste de `D` par PALIER. Les
            // deux ne se composent en `D^palier` que si l'ancre vaut exactement
            // `3 × rang` — ce que la répartition 6/12/12/12/12/8 garantit, et qu'une
            // autre casserait sans qu'aucun autre test ne le dise.
            foreach (var espece in Especes.Toutes)
                Assert.That(espece.Palier, Is.EqualTo(espece.Rang * Constantes.ESPECE_TOUS_LES_N_PALIERS), $"l’espèce {espece.Id}");
        }

        [Test, Description("le multiplicateur de palier et le héros portent ensemble la part de D que le bestiaire ne porte pas")]
        public void Le_multiplicateur_de_palier_et_le_heros_portent_ensemble_la_part_de_D_que_le_bestiaire_ne_porte_pas()
        {
            // Spec 2026-09-17 [D3] : un niveau de héros par palier, et sa part sort de
            // `m_p`. Ce qui doit tenir est le produit des deux, pas `m_p` seul.
            var parTroisPaliers =
                Math.Pow(Constantes.MultiplicateurDePalier() * (1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS), Constantes.ESPECE_TOUS_LES_N_PALIERS) *
                Constantes.DEBIT_RATIO_ESPECE;
            Assert.That(parTroisPaliers,
                Is.EqualTo(Math.Pow(Constantes.D_PRODUCTION_PAR_PALIER, Constantes.ESPECE_TOUS_LES_N_PALIERS)).Within(0.5e-6));
        }

        /* ─── §3 — la donnée réservée et les textes ───────────────────────────── */

        [Test, Description("`tanche` n’est assignée à aucun générateur")]
        public void Tanche_n_est_assignee_a_aucun_generateur()
        {
            // Longévité, faible débit, très forte contenance : c'est le portrait du
            // héros, pas d'une espèce ordinaire (§2.E).
            Assert.That(Especes.Toutes.Select(e => e.Id), Has.No.Member(Especes.ESPECE_RESERVEE));
        }

        [Test, Description("chaque succès livré porte un texte, jamais le repli")]
        public void Chaque_succes_livre_porte_un_texte_jamais_le_repli()
        {
            var sansTexte = RegistreDesSucces.Tous.Where(s => ReferenceEquals(Textes.DuSucces(s.Id), Textes.SUCCES_INCONNU));
            Assert.That(sansTexte.Select(s => s.Id), Is.Empty);
        }
    }
}
