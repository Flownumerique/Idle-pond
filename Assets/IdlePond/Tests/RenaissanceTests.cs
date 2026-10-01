using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// La renaissance — noyau v1.0 §3 et §6.5.
    ///
    /// Reset complet (f = 1) : ce qui appartient au cycle repart de l'œuf, et
    /// quatre choses traversent — contenance, densité, Souffle, technique —, plus le
    /// drapeau des cent, l'unique exception au « tout se reperd ».
    ///
    /// Chaque assertion porte sur un état de départ où la grandeur visée n'est PAS
    /// déjà à sa valeur d'arrivée : un reset affirmé sur un champ déjà vide, ou une
    /// conservation affirmée sur un champ vide, ne prouve rien.
    /// </summary>
    public class RenaissanceTests
    {
        /* ─── §3.1 — la renaissance remet le cycle à l'œuf ─────────────────────── */

        [Test, Description("mana, paliers, espèces, pointe, durée et acquis repartent de l’œuf")]
        public void Mana_paliers_especes_pointe_duree_et_acquis_repartent_de_l_oeuf()
        {
            var depart = EtatDeTravail.Creer();
            var avant = depart with
            {
                Cycle = depart.Cycle with
                {
                    ProductionPicParSeconde = new Decimal(500),
                    DureeSecondes = 7200,
                    AcquisDeSejour = 30,
                    NiveauDuHeros = 4,
                },
            };
            // Rien de tout cela n'est déjà à sa valeur de départ.
            Assert.That(avant.Cycle.ManaCourant.Eq(Constantes.MANA_A_LA_SORTIE_DE_L_OEUF), Is.False);
            Assert.That(avant.Cycle.PaliersOuverts, Is.GreaterThan(Constantes.PALIERS_OUVERTS_AU_DEPART));
            Assert.That(avant.Cycle.Especes.Count, Is.GreaterThan(0));

            var apres = Renaissance.Renaitre(avant).Cycle;
            // Le mana repart CHARGÉ, pas de zéro : c'est la charge de l'œuf, qui tient
            // le plancher de cadence du §8.4 et n'est pas interchangeable avec le
            // débit du héros (voir `MANA_A_LA_SORTIE_DE_L_OEUF`).
            Assert.That(apres.ManaCourant.Eq(Constantes.MANA_A_LA_SORTIE_DE_L_OEUF), Is.True);
            Assert.That(apres.PaliersOuverts, Is.EqualTo(Constantes.PALIERS_OUVERTS_AU_DEPART));
            Assert.That(apres.Especes, Is.Empty);
            Assert.That(apres.ProductionPicParSeconde.Eq(0), Is.True);
            Assert.That(apres.DureeSecondes, Is.EqualTo(0));
            Assert.That(apres.AcquisDeSejour, Is.EqualTo(0));
            Assert.That(apres.NiveauDuHeros, Is.EqualTo(1));
        }

        /* ─── §3.1 — ce qui traverse la renaissance ───────────────────────────── */

        [Test, Description("sans aucun séjour, la contenance traverse telle quelle — et ne monte pas")]
        public void Sans_aucun_sejour_la_contenance_traverse_telle_quelle_et_ne_monte_pas()
        {
            // Tier 0 §8 : le plafond ne monte QUE par séjour. Une renaissance sans séjour
            // ne doit donc rien lui ajouter — un forfait par renaissance échouerait ici —,
            // et ne doit pas non plus la remettre à sa valeur initiale.
            var depart = EtatDeTravail.Creer();
            var avant = depart with { Cycle = depart.Cycle with { AcquisDeSejour = 0 } };
            Assert.That(avant.Permanent.ContenanceMana.Eq(Decimal.Parse("1e14")), Is.True);
            Assert.That(Renaissance.Renaitre(avant).Permanent.ContenanceMana.Eq(avant.Permanent.ContenanceMana), Is.True);
        }

        [Test, Description("le Souffle traverse, augmenté du gain que la pointe du cycle a mérité")]
        public void Le_Souffle_traverse_augmente_du_gain_que_la_pointe_du_cycle_a_merite()
        {
            var depart = EtatDeTravail.Creer();
            var avant = depart with
            {
                Cycle = depart.Cycle with { ProductionPicParSeconde = new Decimal(1e6) },
                Permanent = depart.Permanent with { Souffle = new Decimal(7) },
            };
            var gain = Renaissance.GainDeSoufflePrevu(avant);
            Assert.That(gain.Gt(0), Is.True);
            Assert.That(Renaissance.Renaitre(avant).Permanent.Souffle.Eq(avant.Permanent.Souffle.Add(gain)), Is.True);
        }

        [Test, Description("les nœuds de technique traversent")]
        public void Les_noeuds_de_technique_traversent()
        {
            var depart = EtatDeTravail.Creer();
            var avant = depart with { Permanent = depart.Permanent with { NoeudsTechnique = new[] { "noeud-temoin-a", "noeud-temoin-b" } } };
            Assert.That(Renaissance.Renaitre(avant).Permanent.NoeudsTechnique, Is.EqualTo(new[] { "noeud-temoin-a", "noeud-temoin-b" }));
        }

        [Test, Description("les compteurs de technique traversent, et celui de la renaissance monte d’un")]
        public void Les_compteurs_de_technique_traversent_et_celui_de_la_renaissance_monte_d_un()
        {
            var avant = EtatDeTravail.Creer();
            var compteursAvant = avant.Permanent.CompteursTechnique;
            // La fixture a creusé, débloqué et amélioré : les compteurs ne sont pas nuls.
            Assert.That(compteursAvant[BrancheTechnique.Creusement], Is.GreaterThan(0));
            Assert.That(compteursAvant[BrancheTechnique.Recrutement], Is.GreaterThan(0));
            Assert.That(compteursAvant[BrancheTechnique.Amelioration], Is.GreaterThan(0));

            var compteursApres = Renaissance.Renaitre(avant).Permanent.CompteursTechnique;
            var attendus = new Dictionary<BrancheTechnique, double>();
            foreach (var paire in compteursAvant) attendus[paire.Key] = paire.Value;
            attendus[BrancheTechnique.Renaissance] = compteursAvant[BrancheTechnique.Renaissance] + 1;
            Assert.That(compteursApres, Is.EqualTo(attendus));
        }

        [Test, Description("le drapeau des cent survit — c’est l’unique exception (§2.1)")]
        public void Le_drapeau_des_cent_survit_c_est_l_unique_exception_2_1()
        {
            var depart = EtatDeTravail.Creer();
            var avant = depart with { Permanent = depart.Permanent with { EspecesAyantAtteintCent = new[] { "vairon" } } };
            Assert.That(Renaissance.Renaitre(avant).Permanent.EspecesAyantAtteintCent, Is.EqualTo(new[] { "vairon" }));
        }

        [Test, Description("les insufflations traversent")]
        public void Les_insufflations_traversent()
        {
            var depart = EtatDeTravail.Creer();
            var avant = depart with
            {
                Permanent = depart.Permanent with
                {
                    Insufflations = new Dictionary<string, int> { ["insufflation-globale"] = 2, ["insufflation-vairon"] = 1 },
                },
            };
            Assert.That(Renaissance.Renaitre(avant).Permanent.Insufflations, Is.EqualTo(avant.Permanent.Insufflations));
        }

        [Test, Description("chaque assise traversée dans cette vie laisse une couche, dans l’ordre des assises, une seule fois")]
        public void Chaque_assise_traversee_dans_cette_vie_laisse_une_couche_dans_l_ordre_des_assises_une_seule_fois()
        {
            // GDD §15.1 : « une marque par assise fixée ». `couches` était déclaré et
            // jamais écrit (audit du 2026-09-08). Spec 2026-09-17 [D11].
            var travail = EtatDeTravail.Creer();
            var dansLaNoue = travail with { Cycle = travail.Cycle with { PaliersOuverts = 4 } };
            Assert.That(dansLaNoue.Permanent.Couches, Is.Empty);
            var uneFois = Renaissance.Renaitre(dansLaNoue);
            Assert.That(uneFois.Permanent.Couches, Is.EqualTo(new[] { "noue" }));

            // Deux assises ouvertes, la première déjà marquée : une seule couche neuve.
            var plusBas = uneFois with
            {
                Permanent = uneFois.Permanent with { Couches = new[] { "noue" } },
                Cycle = uneFois.Cycle with { PaliersOuverts = 8 },
            };
            Assert.That(Renaissance.Renaitre(plusBas).Permanent.Couches, Is.EqualTo(new[] { "noue", "assise-2" }));

            // L'ordre est celui des assises, pas celui de l'obtention.
            var autre = EtatDeTravail.Creer();
            var desordre = autre with
            {
                Permanent = autre.Permanent with { Couches = new[] { "assise-2" } },
                Cycle = autre.Cycle with { PaliersOuverts = 2 },
            };
            Assert.That(Renaissance.Renaitre(desordre).Permanent.Couches, Is.EqualTo(new[] { "noue", "assise-2" }));
        }

        /* ─── §6.5 — la densité se pose par max, et son gain vaut pointe^α ────── */

        [Test, Description("chaque palier ouvert porte max(ancienne, pointe^α) ; les paliers fermés ne bougent pas")]
        public void Chaque_palier_ouvert_porte_max_ancienne_pointe_alpha_les_paliers_fermes_ne_bougent_pas()
        {
            var @base = EtatDeTravail.Creer();
            var ouverts = @base.Cycle.PaliersOuverts;
            Assert.That(ouverts, Is.GreaterThan(2));
            var pointe = new Decimal(1e6);
            var laissee = Math.Pow(pointe.ToNumber() / Constantes.PRODUCTION_DE_REFERENCE, Constantes.ALPHA_GAIN_DE_DENSITE);
            // Un palier ouvert DÉJÀ plus dense que ce que le cycle laisse, et un palier
            // fermé chargé : le premier doit garder sa valeur (max, pas affectation),
            // le second ne doit pas être touché (paliers ouverts seulement).
            var densites = @base.Permanent.Densites.Select((d, palier) =>
            {
                if (palier == 1) return laissee * 10;
                if (palier == ouverts + 2) return laissee * 3;
                return d;
            }).ToArray();
            var avant = @base with
            {
                Cycle = @base.Cycle with { ProductionPicParSeconde = pointe },
                Permanent = @base.Permanent with { Densites = densites },
            };

            var apres = Renaissance.Renaitre(avant).Permanent.Densites;
            Assert.That(apres.Count, Is.EqualTo(densites.Length));
            for (var palier = 0; palier < apres.Count; palier += 1)
            {
                var densite = apres[palier];
                if (palier < ouverts)
                    Assert.That(densite, Is.EqualTo(Math.Max(densites[palier], laissee)).Within(0.5e-6), $"palier ouvert {palier}");
                else
                    Assert.That(densite, Is.EqualTo(densites[palier]), $"palier fermé {palier}");
            }
            // Sur un palier vierge (densité 0), la valeur posée est `pointe^α`
            // exactement : la loi du gain, et une conservation neutre. Ce point ne
            // distingue PAS `max` d'une addition (0 + x = max(0, x)) — c'est le
            // palier 1, déjà plus dense que la pointe, qui le fait, dans la boucle
            // ci-dessus.
            Assert.That(apres[0], Is.EqualTo(laissee).Within(0.5e-6));
        }

        [Test, Description("une pointe plus faible que la densité acquise ne fait rien baisser")]
        public void Une_pointe_plus_faible_que_la_densite_acquise_ne_fait_rien_baisser()
        {
            var @base = EtatDeTravail.Creer();
            var densites = @base.Permanent.Densites.Select(_ => 100.0).ToArray();
            // pointe 2 : laisse 2^0,6 ≈ 1,5, bien sous les 100 acquis.
            var avant = @base with
            {
                Cycle = @base.Cycle with { ProductionPicParSeconde = new Decimal(2) },
                Permanent = @base.Permanent with { Densites = densites },
            };
            Assert.That(Renaissance.Renaitre(avant).Permanent.Densites, Is.EqualTo(densites));
        }

        /* ─── §2.B — la contenance monte par le séjour ─────────────────────────────
         * La cible `g^PALIERS_PAR_CYCLE_VISE` d'un cycle NOMINAL est affirmée par
         * `ContenanceTests` (« un cycle nominal la multiplie par ≈47,1 »). Ici, ce
         * qui compte est que la renaissance LISE l'acquis : une loi qui l'ignorerait —
         * un forfait par renaissance — ferait tomber ce test. */

        [Test, Description("un cycle écourté fixe moins de contenance qu’un cycle plein")]
        public void Un_cycle_ecourte_fixe_moins_de_contenance_qu_un_cycle_plein()
        {
            var avant = EtatDeTravail.Creer();
            var court = avant with { Cycle = avant.Cycle with { AcquisDeSejour = Constantes.ACQUIS_MAX / 4 } };
            var plein = avant with { Cycle = avant.Cycle with { AcquisDeSejour = Constantes.ACQUIS_MAX } };
            Assert.That(Renaissance.Renaitre(court).Permanent.ContenanceMana.Lt(Renaissance.Renaitre(plein).Permanent.ContenanceMana), Is.True);
        }
    }
}
