using System;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// La contenance et l'acquis de séjour — amendement v1.1 §2.B.
    ///
    /// « Un cycle nominal multiplie la contenance par ≈47,1 (tolérance 2 %). »
    ///
    /// Ce facteur n'est écrit nulle part dans le code de la renaissance, et c'est
    /// tout l'intérêt du test : il doit ÉMERGER de `A∞` et `τ₀`. Tier 0 §8 — le
    /// plafond ne monte que par séjour prolongé en mana dense ; une contenance
    /// indexée sur le compteur de renaissances violerait l'invariant, et passerait
    /// ce test tout en étant fausse. C'est pourquoi le test regarde aussi la forme
    /// de la montée, pas seulement son résultat.
    ///
    /// Et, second bloc : le blocage est doux (noyau v1.0 §2.2).
    /// </summary>
    public class ContenanceTests
    {
        const double H = 3600;

        static EtatJeu Dense(EtatJeu depart, double densite) =>
            depart with { Permanent = depart.Permanent with { Densites = depart.Permanent.Densites.Select(_ => densite).ToArray() } };

        [Test, Description("un cycle nominal la multiplie par ≈47,1")]
        public void Un_cycle_nominal_la_multiplie_par_47_1()
        {
            var depart = Reducteur.EtatInitial(1);
            var apresSejour = Reducteur.Tick(depart, Constantes.DUREE_DU_CYCLE_1_HEURES * H);
            var apres = Renaissance.Renaitre(apresSejour);
            var rapport = apres.Permanent.ContenanceMana.Div(depart.Permanent.ContenanceMana).ToNumber();
            Assert.That(rapport, Is.EqualTo(Constantes.CONTENANCE_PAR_RENAISSANCE).Within(0.5));
            Assert.That(Math.Abs(rapport / Constantes.CONTENANCE_PAR_RENAISSANCE - 1), Is.LessThan(0.02));
        }

        [Test, Description("la cible dérivée vaut bien g^4.4")]
        public void La_cible_derivee_vaut_bien_g_4_4()
        {
            Assert.That(Constantes.CONTENANCE_PAR_RENAISSANCE, Is.EqualTo(47.1).Within(0.05));
        }

        [Test, Description("l’acquis sature vers A∞, et son t₉₀ tombe à ≈2 h")]
        public void L_acquis_sature_vers_A_et_son_t_tombe_a_2_h()
        {
            var depart = Reducteur.EtatInitial(1);
            var a90 = Reducteur.Tick(depart, 2 * H).Cycle.AcquisDeSejour;
            Assert.That(a90 / Constantes.ACQUIS_MAX, Is.EqualTo(0.9).Within(0.005));
            var aLInfini = Reducteur.Tick(depart, 200 * H).Cycle.AcquisDeSejour;
            Assert.That(aLInfini, Is.EqualTo(Constantes.ACQUIS_MAX).Within(0.0005));
        }

        [Test, Description("rester au-delà de la saturation ne rapporte plus de profondeur")]
        public void Rester_au_dela_de_la_saturation_ne_rapporte_plus_de_profondeur()
        {
            // L'effet secondaire recherché du §2.B, et il ne doit pas se casser : passé
            // la saturation, rester ne rapporte plus que du Souffle. C'est ce qui rend
            // réelle la seule vraie décision du joueur.
            //
            // Le blocage est doux (noyau v1.0 §2.2) : rien ne borne plus la comparaison,
            // le joueur peut rester indéfiniment sans qu'aucune renaissance ne se
            // déclenche à sa place. `longSejour` n'a donc qu'à être largement plus long
            // qu'un cycle nominal — sa valeur exacte n'a plus de portée canonique.
            const double longSejour = 200 * H;
            Assert.That(longSejour / H, Is.GreaterThan(10 * Constantes.DUREE_DU_CYCLE_1_HEURES));

            var depart = Reducteur.EtatInitial(1);
            var nominal = Renaissance.Renaitre(Reducteur.Tick(depart, Constantes.DUREE_DU_CYCLE_1_HEURES * H)).Permanent.ContenanceMana;
            var bienPlusLong = Renaissance.Renaitre(Reducteur.Tick(depart, longSejour)).Permanent.ContenanceMana;

            Assert.That(Reducteur.Tick(depart, longSejour).Permanent.NombreDeRenaissances, Is.EqualTo(0), "aucune renaissance ne se déclenche seule");
            Assert.That(bienPlusLong.Div(nominal).ToNumber(), Is.LessThan(1.04));
        }

        [Test, Description("l’acquis se dépense entièrement à la renaissance")]
        public void L_acquis_se_depense_entierement_a_la_renaissance()
        {
            var apres = Renaissance.Renaitre(Reducteur.Tick(Reducteur.EtatInitial(1), 3 * H));
            Assert.That(apres.Cycle.AcquisDeSejour, Is.EqualTo(0));
        }

        [Test, Description("le multiplicateur de densité vaut 1 en eau neutre, jamais moins, et croît")]
        public void Le_multiplicateur_de_densite_vaut_1_en_eau_neutre_jamais_moins_et_croit()
        {
            // Le point `Multiplicateur(1) == 1` verrouillait le PLANCHER de l'ancienne
            // forme (`Math.max(1, densité)^e`), remplacée par celle des contraintes
            // globales, `(1 + densité/d₀)^e` — les deux coïncident à densité 0, pas à
            // densité 1. Ce que ce test protège n'est pas ce point-là : c'est `≥ 1`
            // partout, et croissant. Le multiplicateur ne raccourcit plus le séjour
            // (voir les deux tests suivants) ; il multiplie la production, et une eau
            // plus dense ne doit jamais la faire baisser.
            var precedent = Densite.Multiplicateur(0);
            Assert.That(precedent, Is.EqualTo(1));
            foreach (var densite in new[] { 0.5, 1, 2, 5, 10, 50 })
            {
                var valeur = Densite.Multiplicateur(densite);
                Assert.That(valeur, Is.GreaterThanOrEqualTo(1));
                Assert.That(valeur, Is.GreaterThanOrEqualTo(precedent));
                precedent = valeur;
            }
            Assert.That(Densite.Multiplicateur(10), Is.GreaterThan(1));
        }

        // Ces deux tests remplacent l'affirmation « une eau dense sature plus vite ».
        // Elle verrouillait une dégénérescence : la densité vaut `pointe^α` et croît
        // sans borne, donc un temps caractéristique divisé par elle s'effondre — t₉₀
        // de 2 h à densité nulle, 0,082 h à densité 10, quasi nul à 10⁶. L'acquis
        // saturait alors en quelques minutes au deuxième cycle, en une fraction de
        // seconde à partir du troisième, et
        // la contenance ne lisait plus que `A∞` : un forfait plat, sous le nom de
        // séjour. Le temps du séjour est désormais `τ₀`, constant.
        [Test, Description("le temps du séjour ne dépend plus de la densité")]
        public void Le_temps_du_sejour_ne_depend_plus_de_la_densite()
        {
            var depart = Reducteur.EtatInitial(1);
            var dense = Dense(depart, 1e6);
            var neutre = Reducteur.Tick(depart, H).Cycle.AcquisDeSejour;
            Assert.That(Reducteur.Tick(dense, H).Cycle.AcquisDeSejour, Is.EqualTo(neutre).Within(0.5e-9));
        }

        [Test, Description("en eau dense, une heure de séjour ne sature toujours pas l’acquis")]
        public void En_eau_dense_une_heure_de_sejour_ne_sature_toujours_pas_l_acquis()
        {
            // Densité 10 : du même ordre que celle que laisse le premier cycle (7,3,
            // mesuré) ; le deuxième en laisse déjà 7 100.
            // Après une heure, l'acquis vaut `1 − e^(−1 h / τ₀)` ≈ 0,683 de `A∞`, et non
            // ≈ 1 : la loi de contenance lit encore la durée du séjour.
            var depart = Reducteur.EtatInitial(1);
            var dense = Dense(depart, 10);
            var rapport = Reducteur.Tick(dense, H).Cycle.AcquisDeSejour / Constantes.ACQUIS_MAX;
            Assert.That(rapport, Is.EqualTo(1 - Math.Exp(-1 / Constantes.TAU_SEJOUR_HEURES)).Within(0.5e-6));
            Assert.That(rapport, Is.LessThan(0.7));
        }

        [Test, Description("la contenance monte PENDANT le cycle, et la renaissance ne fait que la fixer")]
        public void La_contenance_monte_PENDANT_le_cycle_et_la_renaissance_ne_fait_que_la_fixer()
        {
            // Amendement v1.3. Le plafond ne montait qu'à la renaissance, donc il était
            // GELÉ pendant toute la vie : le palier suivant coûtait plus que ce que le
            // héros pouvait porter, et rien ne pouvait plus changer cela avant la vie
            // d'après. Mesuré sur le cycle 15 : le dernier palier s'ouvrait à la
            // première minute, et les 99,8 % restants ne voyaient ni palier, ni espèce,
            // ni même de production (×1,1 en 8,9 h). Ce n'est pas un jeu incrémental,
            // c'est un minuteur.
            //
            // Le plafond monte maintenant avec l'acquis, donc en continu. Tier 0 §8
            // tient toujours — il ne monte QUE par séjour prolongé —, il monte
            // simplement au fil du séjour au lieu d'être versé en bloc à la sortie.
            var depart = Reducteur.EtatInitial(1);
            Assert.That(Economie.Contenance(depart).Eq(depart.Permanent.ContenanceMana), Is.True);

            var apres = Reducteur.Tick(depart, H);
            Assert.That(Economie.Contenance(apres).Gt(Economie.Contenance(depart)), Is.True);
            Assert.That(Economie.Contenance(apres).Eq(apres.Permanent.ContenanceMana.Mul(1 + apres.Cycle.AcquisDeSejour)), Is.True);
            // Le plafond BANQUÉ, lui, n'a pas bougé : l'acquis n'est pas encore dépensé.
            Assert.That(apres.Permanent.ContenanceMana.Eq(depart.Permanent.ContenanceMana), Is.True);

            // La renaissance ne crée rien : elle fixe ce que le cycle portait déjà.
            var renee = Renaissance.Renaitre(apres);
            Assert.That(renee.Permanent.ContenanceMana.Eq(Economie.Contenance(apres)), Is.True);
            Assert.That(Economie.Contenance(renee).Eq(renee.Permanent.ContenanceMana), Is.True);
        }

        [Test, Description("le temps du séjour croît avec la profondeur ATTEINTE (amendement v1.2)")]
        public void Le_temps_du_sejour_croit_avec_la_profondeur_ATTEINTE_amendement_v1_2()
        {
            // La tâche 12 a mesuré ce que `τ` constant produit : les 45 cycles durent
            // exactement 2,617 h — `τ₀ ln 20` — et `dernier / premier` vaut 1,000 quel
            // que soit le réglage d'économie. La durée d'un cycle est plafonnée par le
            // séjour, pas par l'économie ; aucun bouton d'économie n'a donc prise sur
            // la forme de la courbe. C'est `τ` qui devient ce bouton.
            //
            // Ce n'est PAS le retour de la loi que R39 a révoquée : celle-là DIVISAIT
            // `τ` par la densité, une grandeur sans borne, et `t₉₀` s'effondrait de 2 h
            // à 0,05 s en trois cycles. Ici `τ` croît, et il croît par PALIER — une
            // quantité entière et bornée par les 62 paliers du monde.
            var depart = Reducteur.EtatInitial(1);
            Assert.That(depart.Reglage, Is.EqualTo(Constantes.REGLAGE_CANONIQUE));
            Assert.That(Reducteur.TauDuSejourSecondes(depart), Is.EqualTo(Constantes.TAU_SEJOUR_HEURES * H).Within(0.5e-9));

            EtatJeu Profond(int p, double croissance) => depart with
            {
                Reglage = new Reglage(croissance),
                Permanent = depart.Permanent with { ProfondeurMaxAtteinte = p },
            };
            // La profondeur zéro vaut toujours `τ₀` : la loi ne déplace pas son origine.
            Assert.That(Reducteur.TauDuSejourSecondes(Profond(0, 1.05)), Is.EqualTo(Constantes.TAU_SEJOUR_HEURES * H).Within(0.5e-9));
            Assert.That(Reducteur.TauDuSejourSecondes(Profond(10, 1.05)),
                Is.EqualTo(Constantes.TAU_SEJOUR_HEURES * H * Math.Pow(1.05, 10)).Within(0.5e-9));
            Assert.That(Reducteur.TauDuSejourSecondes(Profond(10, 1.05)), Is.GreaterThan(Reducteur.TauDuSejourSecondes(Profond(9, 1.05))));
            // À croissance 1, la loi d'avant, à l'identique.
            Assert.That(Reducteur.TauDuSejourSecondes(Profond(20, 1)), Is.EqualTo(Constantes.TAU_SEJOUR_HEURES * H).Within(0.5e-9));
        }

        [Test, Description("en profondeur, la même heure de séjour rapporte moins d’acquis")]
        public void En_profondeur_la_meme_heure_de_sejour_rapporte_moins_d_acquis()
        {
            // L'effet, et non la formule : c'est par là que les cycles s'allongent.
            var depart = Reducteur.EtatInitial(1);
            EtatJeu Profond(int p) => depart with
            {
                Reglage = new Reglage(1.05),
                Permanent = depart.Permanent with { ProfondeurMaxAtteinte = p },
            };
            var surface = Reducteur.Tick(Profond(0), H).Cycle.AcquisDeSejour;
            var fond = Reducteur.Tick(Profond(20), H).Cycle.AcquisDeSejour;
            Assert.That(fond, Is.LessThan(surface));
            // Et l'acquis sature toujours vers le MÊME plafond : `τ` change le temps,
            // jamais la valeur. Cent heures au fond y arrivent encore.
            Assert.That(Reducteur.Tick(Profond(20), 100 * H).Cycle.AcquisDeSejour / Constantes.ACQUIS_MAX, Is.GreaterThan(0.95));
        }

        [Test, Description("τ₀ est bien le temps caractéristique à densité neutre")]
        public void Tau_est_bien_le_temps_caracteristique_a_densite_neutre()
        {
            var apres = Reducteur.Tick(Reducteur.EtatInitial(1), Constantes.TAU_SEJOUR_HEURES * H);
            Assert.That(apres.Cycle.AcquisDeSejour / Constantes.ACQUIS_MAX, Is.EqualTo(1 - Math.Exp(-1)).Within(0.5e-6));
        }

        /* ─── Le blocage est doux (noyau v1.0 §2.2) ─────────────────────────────── */

        [Test, Description("une jauge pleine pendant une semaine ne déclenche aucune renaissance")]
        public void Une_jauge_pleine_pendant_une_semaine_ne_declenche_aucune_renaissance()
        {
            var etat = EtatDeTravail.Creer();
            var renaissancesAvant = etat.Permanent.NombreDeRenaissances;
            // 7 jours en un seul pas, jauge saturée du début à la fin
            var pleine = Economie.Contenance(etat);
            etat = Reducteur.Tick(etat with { Cycle = etat.Cycle with { ManaCourant = pleine } }, 7 * 24 * 3600);
            Assert.That(etat.Permanent.NombreDeRenaissances, Is.EqualTo(renaissancesAvant));
            // « Pleine » est devenue une cible MOBILE : le plafond monte avec l'acquis
            // pendant que le joueur est absent, et peut s'éloigner plus vite que la
            // production ne remplit. Ce qui doit tenir n'a pas changé : la jauge ne
            // déborde jamais, elle ne redescend jamais, et rien ne renaît à sa place.
            Assert.That(etat.Cycle.ManaCourant.Lte(Economie.Contenance(etat)), Is.True);
            Assert.That(etat.Cycle.ManaCourant.Gte(pleine), Is.True);
        }
    }
}
