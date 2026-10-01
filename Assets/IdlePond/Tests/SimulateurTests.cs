using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Simulateur;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// Critère d'acceptation du jalon v0.1 :
    /// « le simulateur tourne 15 cycles sans jouer, et le test d'équivalence de pas
    /// passe. »
    ///
    /// Le simulateur n'a pas de moteur à lui : il appelle le même `Tick` que le jeu
    /// avec un `dt` plus grand. Ce test vérifie donc deux choses à la fois — que
    /// quinze cycles s'enchaînent, et que les invariants du Tier 0 tiennent sur
    /// toute la durée, pas seulement à l'arrivée.
    /// </summary>
    public class SimulateurTests
    {
        /// Recopiée de `InsufflationsTests` : pas encore de module d'aides de test partagé.
        static EtatJeu Insufflee(EtatJeu etat, IReadOnlyDictionary<string, int> rangs)
        {
            var insufflations = new Dictionary<string, int>(etat.Permanent.Insufflations);
            foreach (var kv in rangs) insufflations[kv.Key] = kv.Value;
            return etat with { Permanent = etat.Permanent with { Insufflations = insufflations } };
        }

        /// <summary>
        /// Un état où les TROIS achats sont ouverts en même temps, choisi pour que
        /// chaque approximation connue échoue : densités inégales par palier, donc
        /// ouvrir un palier change la densité du séjour (0,78 → 0,91) et un gain de
        /// creusement en `prod × (m_p − 1)` est faux ; une espèce déjà drapée, donc le
        /// drapeau suivant vaut `0,03 / 1,03` et non `0,03` ; une espèce à 99, non
        /// drapée, pour que ce drapeau soit en jeu ; et une espèce ouverte non
        /// débloquée, sans quoi la branche « débloquer » n'est jamais prise.
        ///
        /// `baseEtat` par défaut `EtatDeTravail.Creer()`, mais accepte tout état déjà
        /// préparé (par exemple insufflé) : seule la forme (paliers ouverts, espèces)
        /// compte ici.
        /// </summary>
        static (EtatJeu Etat, Espece ACent, Espece ADebloquer) EtatAuxTroisAchats(EtatJeu baseEtat = null)
        {
            baseEtat ??= EtatDeTravail.Creer();
            var ouvertes = Especes.Toutes.Where(e => e.Palier < baseEtat.Cycle.PaliersOuverts).ToList();
            Assert.That(ouvertes.Count, Is.GreaterThanOrEqualTo(3));
            var aCent = ouvertes[1];
            var aDebloquer = ouvertes[2];
            Assert.That(baseEtat.Permanent.EspecesAyantAtteintCent, Has.No.Member(aCent.Id));
            // Le drapeau déjà posé vient d'`EtatDeTravail`. S'il disparaissait de cette
            // fixture partagée, `MultiplicateurDesDrapeaux` retomberait à 1 et le terme
            // `0,03 / m` cesserait d'être discriminé, sans qu'aucun test ne le dise.
            Assert.That(baseEtat.Permanent.EspecesAyantAtteintCent.Count, Is.GreaterThan(0));
            var especes = new Dictionary<string, EtatEspece>();
            foreach (var kv in baseEtat.Cycle.Especes)
                if (kv.Key != aDebloquer.Id) especes[kv.Key] = kv.Value;
            especes[aCent.Id] = new EtatEspece(true, 99);
            var etat = baseEtat with { Cycle = baseEtat.Cycle with { Especes = especes } };
            Assert.That(
                Densite.DuSejour(etat with { Cycle = etat.Cycle with { PaliersOuverts = etat.Cycle.PaliersOuverts + 1 } }),
                Is.GreaterThan(Densite.DuSejour(etat)));
            return (etat, aCent, aDebloquer);
        }

        /* ─── describe('simulateur') ────────────────────────────────────────────── */

        [Test, Description("une partie sans UI atteint la renaissance 2 en headless")]
        public void Une_partie_sans_UI_atteint_la_renaissance_2_en_headless()
        {
            // Critère d'acceptation du §6 de l'amendement v1.1. Il porte sur le monde
            // LIVRÉ — la Noue et ses six paliers —, pas sur les 62 que le simulateur
            // mesure : c'est le jeu qu'un joueur touche qui doit boucler.
            var partie = Simuler(2, null, 1, null, Assises.PALIERS_LIVRES);
            Assert.That(partie.CycleNonConvergent, Is.Null);
            Assert.That(partie.Etat.Permanent.NombreDeRenaissances, Is.EqualTo(2));
            Assert.That(partie.Etat.Permanent.ContenanceMana.Gt(Constantes.CONTENANCE_INITIALE), Is.True);
            Assert.That(partie.Etat.Permanent.Densites[0], Is.GreaterThan(0));
        }

        [Test, Description("enchaîne 14 cycles sans jouer")]
        public void Enchaine_14_cycles_sans_jouer()
        {
            var resultat = Simuler(Constantes.NOMBRE_DE_RENAISSANCES_VISE);
            Assert.That(resultat.CycleNonConvergent, Is.Null, "un cycle n’a pas convergé");
            Assert.That(resultat.CyclesAcheves, Is.EqualTo(Constantes.NOMBRE_DE_RENAISSANCES_VISE));
            Assert.That(resultat.Releve.Cycles.Count, Is.EqualTo(Constantes.NOMBRE_DE_RENAISSANCES_VISE));
            foreach (var cycle in resultat.Releve.Cycles)
            {
                Assert.That(cycle.DureeEcouleeSecondes, Is.GreaterThan(0));
                Assert.That(cycle.PaliersOuverts, Is.GreaterThan(0));
            }
        }

        [Test, Description("sépare le temps ACTIF du temps ÉCOULÉ")]
        public void Separe_le_temps_ACTIF_du_temps_ECOULE()
        {
            // §11 : « durée de cycle, active et calendaire », et « intervalle réel
            // entre deux sessions | distingue temps actif et temps calendaire ».
            // Les confondre fait lire les ~600 h calendaires du §5.4 comme si c'étaient
            // les ~38 h actives — l'erreur exacte que les jalons v0.1 et v0.2 ont
            // rapportée deux fois.
            //
            // Un relevé tient le joueur devant l'écran le temps d'un pas. En achat
            // continu les relevés se touchent : il est là tout du long, et l'actif
            // ÉGALE l'écoulé — c'est le joueur optimal du finding 2. Relâché, il n'est
            // là qu'un pas par relevé : l'actif vaut exactement `pas / intervalle` de
            // l'écoulé. Un compteur qui recopierait l'autre tomberait sur la seconde
            // assertion ; un compteur resté à zéro, sur la première.
            var continu = Simuler(3);
            Assert.That(continu.SecondesActives, Is.GreaterThan(0));
            Assert.That(continu.SecondesActives / continu.SecondesEcoulees, Is.EqualTo(1).Within(0.5e-9));

            var releves = POLITIQUE_PAR_DEFAUT with { SecondesEntreReleves = 4 * 3600 };
            var relache = Simuler(3, releves);
            Assert.That(relache.SecondesActives / relache.SecondesEcoulees,
                Is.EqualTo(releves.Pas / releves.SecondesEntreReleves).Within(0.5e-9));
        }

        [Test, Description("l’intervalle entre deux relevés est le seul réglage de temps calendaire")]
        public void L_intervalle_entre_deux_releves_est_le_seul_reglage_de_temps_calendaire()
        {
            // §5.4 : « aucun réglage de paramètre ne produira de croissance de cycle en
            // temps actif — seules les politiques ». Le vérifier plutôt que d'y croire :
            // doubler l'absence doit à peu près doubler le calendaire, et laisser
            // l'actif tranquille.
            var court = Simuler(6, POLITIQUE_PAR_DEFAUT with { SecondesEntreReleves = 2 * 3600 });
            var longue = Simuler(6, POLITIQUE_PAR_DEFAUT with { SecondesEntreReleves = 8 * 3600 });
            Assert.That(longue.SecondesEcoulees, Is.GreaterThan(court.SecondesEcoulees * 2));
            var ecartActif = Math.Abs(longue.SecondesActives / court.SecondesActives - 1);
            Assert.That(ecartActif, Is.LessThan(0.5));
        }

        [Test, Description("le joueur rentre dans l’œuf sur la saturation, pas sur un minuteur")]
        public void Le_joueur_rentre_dans_l_oeuf_sur_la_saturation_pas_sur_un_minuteur()
        {
            // La seule vraie décision du §6.4, rendue réelle par l'acquis saturant du
            // §2.B : à chaque renaissance, l'acquis doit avoir fait son travail.
            var renaissances = 0;
            Simuler(4, null, 1, etat =>
            {
                if (etat.Permanent.NombreDeRenaissances == renaissances) return;
                renaissances = etat.Permanent.NombreDeRenaissances;
                // Juste après la renaissance l'acquis est remis à zéro ; ce qui compte
                // est que la contenance ait bien été multipliée par un acquis saturé.
                Assert.That(etat.Cycle.AcquisDeSejour, Is.EqualTo(0));
            });
            var partie = Simuler(4);
            var attendu = Math.Pow(1 + Constantes.ACQUIS_MAX * POLITIQUE_PAR_DEFAUT.FractionDeSaturationPourRenaitre, 4);
            Assert.That(partie.Etat.Permanent.ContenanceMana.Div(Constantes.CONTENANCE_INITIALE).ToNumber(), Is.GreaterThan(attendu));
        }

        [Test, Description("la densité ne redescend jamais (Tier 0)")]
        public void La_densite_ne_redescend_jamais_Tier_0()
        {
            // La comparaison est faite à la main et `Assert` n'est appelé qu'à
            // l'arrivée : l'observateur passe des dizaines de milliers de fois sur 62
            // paliers, et une assertion par palier coûtait du temps de harnais, pas de
            // mesure. La discrimination est la même, la première violation est retenue
            // avec son palier et ses deux valeurs.
            IReadOnlyList<double> precedentes = null;
            string faute = null;
            void Verifier(EtatJeu etat)
            {
                var densites = etat.Permanent.Densites;
                if (precedentes != null && faute == null)
                {
                    for (var palier = 0; palier < densites.Count; palier += 1)
                    {
                        if (densites[palier] < precedentes[palier])
                        {
                            faute = $"densité du palier {palier} : {precedentes[palier]} → {densites[palier]}";
                            break;
                        }
                    }
                }
                precedentes = densites;
            }
            Simuler(Constantes.NOMBRE_DE_RENAISSANCES_VISE, null, 1, Verifier);
            Assert.That(faute, Is.Null, "la densité a reculé");
            Assert.That(precedentes, Is.Not.Null);
            Assert.That(precedentes.Any(d => d > 0), Is.True);
        }

        [Test, Description("les acquis permanents ne se reperdent jamais")]
        public void Les_acquis_permanents_ne_se_reperdent_jamais()
        {
            var renaissances = 0;
            double contenance = 0;
            double souffle = 0;
            double compteurs = 0;
            Simuler(Constantes.NOMBRE_DE_RENAISSANCES_VISE, null, 1, etat =>
            {
                // Un être surévolué conserve ses acquis à vie.
                Assert.That(etat.Permanent.NombreDeRenaissances, Is.GreaterThanOrEqualTo(renaissances));
                Assert.That(etat.Permanent.ContenanceMana.ToNumber(), Is.GreaterThanOrEqualTo(contenance));
                Assert.That(etat.Permanent.Souffle.ToNumber(), Is.GreaterThanOrEqualTo(souffle));
                var somme = etat.Permanent.CompteursTechnique.Values.Sum();
                Assert.That(somme, Is.GreaterThanOrEqualTo(compteurs), "un compteur de technique a reculé : on ne désapprend pas");
                renaissances = etat.Permanent.NombreDeRenaissances;
                contenance = etat.Permanent.ContenanceMana.ToNumber();
                souffle = etat.Permanent.Souffle.ToNumber();
                compteurs = somme;
            });
            Assert.That(renaissances, Is.EqualTo(Constantes.NOMBRE_DE_RENAISSANCES_VISE));
        }

        [Test, Description("la renaissance emporte le peuplement et la géométrie, et rien d’autre")]
        public void La_renaissance_emporte_le_peuplement_et_la_geometrie_et_rien_d_autre()
        {
            var resultat = Simuler(2);
            Assert.That(resultat.Etat.Cycle.PaliersOuverts, Is.EqualTo(1));
            Assert.That(resultat.Etat.Cycle.Especes.Keys, Is.Empty);
            Assert.That(resultat.Etat.Permanent.ProfondeurMaxAtteinte, Is.GreaterThan(1));
            // Le mana courant expire vers l'ambiant. Il n'est pas détruit : aucun
            // système d'IdlePond ne se comporte comme un puits.
            Assert.That(resultat.Etat.Permanent.ManaAmbiant.Gt(0), Is.True);
        }

        [Test, Description("la progression descend réellement d’un cycle à l’autre")]
        public void La_progression_descend_reellement_d_un_cycle_a_l_autre()
        {
            var resultat = Simuler(Constantes.NOMBRE_DE_RENAISSANCES_VISE);
            var premier = resultat.Releve.Cycles[0];
            var dernier = resultat.Releve.Cycles[resultat.Releve.Cycles.Count - 1];
            Assert.That(dernier.PaliersOuverts, Is.GreaterThan(premier.PaliersOuverts));
        }

        /* ─── describe('le simulateur tourne sur le noyau v1.0') ────────────────── */

        sealed class Suivi
        {
            public double Duree;
            public double DernierPalier;
            public int Paliers;
            public double ProdAuBlocage;
            public double ProdFin;
        }

        [Test, Description("quinze renaissances, et le temps actif est distinct du temps écoulé")]
        public void Quinze_renaissances_et_le_temps_actif_est_distinct_du_temps_ecoule()
        {
            var r = Simuler(15, POLITIQUE_PAR_DEFAUT, 1);
            Assert.That(r.Cycles.Count, Is.EqualTo(15));
            Assert.That(r.SecondesActives, Is.GreaterThan(0));
            Assert.That(r.SecondesEcoulees, Is.GreaterThanOrEqualTo(r.SecondesActives));
        }

        [Test, Description("le premier cycle dure environ trois heures")]
        public void Le_premier_cycle_dure_environ_trois_heures()
        {
            var r = Simuler(15, POLITIQUE_PAR_DEFAUT, 1);
            var h = r.Cycles[0].DureeEcouleeSecondes / 3600;
            Assert.That(h, Is.GreaterThan(1));
            Assert.That(h, Is.LessThan(8));
        }

        [Test, Description("la politique optimale et la politique relâchée diffèrent (finding 2)")]
        public void La_politique_optimale_et_la_politique_relachee_different_finding_2()
        {
            // Le seuil tenait un ×2 sur une absence de 4 h tant qu'un cycle durait
            // 2,62 h : l'absence dépassait le cycle, et chaque cycle payait un
            // intervalle entier. Sous la courbe v1.2 le cycle passe à 8,9 h dès le
            // quinzième, donc une absence de 4 h y tient DEDANS et coûte relativement
            // moins. Mesuré sur 13 cycles, graine 1 — optimale 67,9 h ; relâchée à
            // 4 h 104,0 h (×1,532), à 8 h 184,0 h (×2,711), à 24 h 504,0 h (×7,425).
            //
            // Le contenu du finding 2 n'est pas le ×2, c'est que l'intervalle de relevé
            // est le SEUL réglage qui gonfle le temps calendaire, et qu'il le gonfle
            // d'autant plus qu'il est long. Le test dit maintenant cela, et le dit sur
            // deux points au lieu d'un : un compteur qui ignorerait l'intervalle rend
            // les trois valeurs égales et tombe sur les deux assertions.
            var optimale = Simuler(13, POLITIQUE_PAR_DEFAUT, 1);
            double Ecoule(double heures) =>
                Simuler(13, POLITIQUE_PAR_DEFAUT with { SecondesEntreReleves = heures * 3600 }, 1).SecondesEcoulees;
            var a4 = Ecoule(4);
            var a24 = Ecoule(24);
            Assert.That(a4, Is.GreaterThan(optimale.SecondesEcoulees * 1.3), "une absence de 4 h coûte du temps calendaire");
            Assert.That(a24, Is.GreaterThan(a4 * 2), "et six fois plus d’absence en coûte davantage");
        }

        [Test, Description("le gain de chaque achat est la production qu’il ajoute réellement")]
        public void Le_gain_de_chaque_achat_est_la_production_qu_il_ajoute_reellement()
        {
            // L'outil qui mesure doit le moins pouvoir mentir : à la tâche 9, une liste
            // de multiplicateurs recopiée à la main dans le simulateur a oublié la
            // densité et biaisé toute mesure, en silence. Ici le gain analytique est
            // confronté au noyau lui-même — la production APRÈS l'achat, moins la
            // production avant —, pour les quatre achats.
            //
            // Un second passage, insufflé, referme la même mesure côté Tâche B2 : le
            // simulateur doit lire l'assiette INSUFFLÉE (`DebitInsuffle` × multiplicateur
            // d'insufflation), pas la seule assiette de base — sans quoi ses décisions
            // d'achat sous-estimeraient toute espèce insufflée.
            var passages = new[]
            {
                (Etiquette: "sans insufflation", Base: EtatDeTravail.Creer()),
                (Etiquette: "avec insufflation",
                    Base: Insufflee(EtatDeTravail.Creer(), new Dictionary<string, int> { ["insufflation-globale"] = 2, ["insufflation-vairon"] = 1 })),
            };
            foreach (var (etiquette, baseEtat) in passages)
            {
                var (etat, aCent, _) = EtatAuxTroisAchats(baseEtat);

                EtatJeu AppliquerAchat(Achat achat)
                {
                    if (achat.Type == TypeDAchat.Creuser) return Reducteur.Creuser(etat);
                    if (achat.Type == TypeDAchat.Grandir) return Reducteur.Grandir(etat);
                    if (achat.Type == TypeDAchat.Debloquer) return Reducteur.Debloquer(etat, achat.Espece.Id);
                    return Reducteur.Ameliorer(etat, achat.Espece.Id);
                }

                var avant = Economie.ProductionTotaleParSeconde(etat);
                var achats = AchatsDisponibles(etat);
                Assert.That(achats.Select(a => a.Type).Distinct(),
                    Is.EquivalentTo(new[] { TypeDAchat.Creuser, TypeDAchat.Grandir, TypeDAchat.Debloquer, TypeDAchat.Niveau }), etiquette);
                Assert.That(achats.Any(a => a.Type == TypeDAchat.Niveau && a.Espece.Id == aCent.Id), Is.True, etiquette);
                foreach (var achat in achats)
                {
                    var apres = AppliquerAchat(achat);
                    Assert.That(apres, Is.Not.SameAs(etat), $"{etiquette} : {achat.Type} n’a pas été payé");
                    var reel = Economie.ProductionTotaleParSeconde(apres).Sub(avant);
                    var libelle = achat.Type == TypeDAchat.Creuser || achat.Type == TypeDAchat.Grandir
                        ? achat.Type.ToString()
                        : $"{achat.Type} {achat.Espece.Id}";
                    Assert.That(achat.Gain.Div(reel).ToNumber(), Is.EqualTo(1).Within(0.5e-9), $"{etiquette} : {libelle}");
                }
            }
        }

        [Test, Description("le joueur optimal fait grandir le héros, à peu près une fois par palier")]
        public void Le_joueur_optimal_fait_grandir_le_heros_a_peu_pres_une_fois_par_palier()
        {
            // Spec [D3] : le coût suit g comme le palier, donc le rapport coût/gain des
            // deux achats reste comparable tout le long. On ne demande pas l'égalité —
            // le gain d'un palier vaut (m_p − 1), celui d'un niveau vaut b — mais un
            // héros laissé au niveau 1 signifierait que l'achat n'est jamais rentable,
            // et le rebudget de D serait faux.
            //
            // Le niveau du héros se reperd à chaque renaissance, mais `PaliersOuverts`
            // croît cycle après cycle (10 → 15 → 19 sur trois cycles, mesuré) : le pic
            // de `niveauMax` sur plusieurs cycles est donc atteint dans le DERNIER
            // cycle simulé, jamais dans le premier.
            var niveauMax = 0;
            var resultat = Simuler(3, null, 1, etat => { niveauMax = Math.Max(niveauMax, etat.Cycle.NiveauDuHeros); });
            var paliersDuDernierCycle = resultat.Cycles[resultat.Cycles.Count - 1].PaliersOuverts;
            Assert.That(niveauMax, Is.GreaterThanOrEqualTo(paliersDuDernierCycle / 2));
            Assert.That(niveauMax, Is.LessThanOrEqualTo(paliersDuDernierCycle + 2));
        }

        [Test, Description("la saturation ne gèle pas la partie (amendement v1.3)")]
        public void La_saturation_ne_gele_pas_la_partie_amendement_v1_3()
        {
            // LE test de genre : IdlePond est un idle incremental, pas un minuteur.
            //
            // Avec un plafond de contenance gelé jusqu'à la renaissance, une partie
            // mesurée donnait ceci — le dernier palier d'un cycle s'ouvrait à la
            // PREMIÈRE MINUTE, puis 99 % du cycle ne voyait plus ni palier, ni espèce,
            // ni même de production (×1,1 sur 8,9 h au cycle 15). Le joueur regardait
            // un minuteur : le palier suivant coûtait plus que ce qu'il pouvait
            // PORTER, et rien dans la vie courante ne pouvait plus changer cela.
            //
            // Deux quantités le disent, et ce sont les deux que le plafond continu
            // rétablit. Mesuré sur les six premiers cycles, graine 1 :
            //
            //   part du cycle au dernier palier ouvert : 0,166 · 0,411 · 0,225 · 0,629 ·
            //     0,304 · 0,184   (plafond gelé : 0,12 puis 0,005 à 0,01)
            //   production gagnée après le premier blocage : ×36 · ×203 · ×449 · ×170 ·
            //     ×254 · ×131      (plafond gelé : ×8,4 · ×20 · ×7 · ×9 · ×1,1)
            //
            // Les bornes sont posées sous le pire cycle mesuré, pas sur la moyenne :
            // un seul cycle gelé est un cycle où le joueur n'a rien à faire.
            var cycles = new Dictionary<int, Suivi>();
            Simuler(6, null, 1, etat =>
            {
                var index = etat.Permanent.NombreDeRenaissances;
                if (!cycles.TryGetValue(index, out var suivi))
                {
                    suivi = new Suivi { Paliers = etat.Cycle.PaliersOuverts };
                    cycles[index] = suivi;
                }
                if (etat.Cycle.PaliersOuverts > suivi.Paliers)
                {
                    suivi.Paliers = etat.Cycle.PaliersOuverts;
                    suivi.DernierPalier = etat.Cycle.DureeSecondes;
                }
                suivi.Duree = Math.Max(suivi.Duree, etat.Cycle.DureeSecondes);
                var production = Economie.ProductionTotaleParSeconde(etat).ToNumber();
                if (suivi.ProdAuBlocage == 0 && Economie.EstBloque(etat)) suivi.ProdAuBlocage = production;
                suivi.ProdFin = production;
            });

            Assert.That(cycles.Count, Is.GreaterThanOrEqualTo(6));
            foreach (var kv in cycles)
            {
                var index = kv.Key;
                var suivi = kv.Value;
                if (index >= 6) continue;
                var part = suivi.DernierPalier / suivi.Duree;
                Assert.That(part, Is.GreaterThan(0.1), $"cycle {index + 1} : le dernier palier s’ouvre à {(part * 100):F1} % du cycle");
                var gagnee = suivi.ProdFin / suivi.ProdAuBlocage;
                Assert.That(gagnee, Is.GreaterThan(25), $"cycle {index + 1} : la production ne gagne que ×{gagnee:F1} après le blocage");
            }
        }

        [Test, Description("la croissance du séjour est le bouton de la FORME de la courbe")]
        public void La_croissance_du_sejour_est_le_bouton_de_la_FORME_de_la_courbe()
        {
            // La contrepartie mesurable de l'amendement v1.2, et ce qui donne à la
            // tâche 13 une bissection qui a prise : à `τ` constant, tous les cycles
            // durent `τ₀ ln 20` et le rapport `dernier / premier` vaut 1 quoi qu'on
            // règle ailleurs — c'est ce que la tâche 12 a mesuré sur 45 cycles.
            double Rapport(double croissance)
            {
                var r = Simuler(6, null, 1, null, null, new Reglage(croissance));
                var durees = r.Cycles.Select(c => c.DureeEcouleeSecondes).ToList();
                Assert.That(durees.Count, Is.EqualTo(6));
                return durees[durees.Count - 1] / durees[0];
            }
            Assert.That(Rapport(1), Is.EqualTo(1).Within(0.5e-3), "à croissance 1, la courbe est plate — la loi d’avant");
            Assert.That(Rapport(1.05), Is.GreaterThan(2), "à croissance 1,05, les cycles s’allongent");
        }

        [Test, Description("le budget retire les achats hors de portée, et rien d’autre")]
        public void Le_budget_retire_les_achats_hors_de_portee_et_rien_d_autre()
        {
            // Le chemin chaud passe un budget pour ne pas calculer le gain de ce qu'il
            // ne peut pas payer — une décision d'achat évalue toutes les espèces
            // ouvertes et n'en paie qu'une. C'est une optimisation, donc une occasion
            // de diverger en silence de la liste complète : la famille de défaut qui a
            // mordu la tâche 9. Ce test attache l'une à l'autre.
            var (etat, _, _) = EtatAuxTroisAchats();
            string Libelle(Achat achat) => achat.Type == TypeDAchat.Creuser || achat.Type == TypeDAchat.Grandir
                ? achat.Type.ToString()
                : $"{achat.Type} {achat.Espece.Id}";
            var complets = AchatsDisponibles(etat);
            Assert.That(complets.Select(a => a.Type).Distinct(),
                Is.EquivalentTo(new[] { TypeDAchat.Creuser, TypeDAchat.Grandir, TypeDAchat.Debloquer, TypeDAchat.Niveau }));

            // Chaque coût de la liste sert à son tour de budget : toutes les coupes
            // sont éprouvées, et chacune sur sa propre valeur — un budget qui vaut
            // exactement un coût doit PAYER cet achat, jamais le retirer.
            foreach (var coupe in complets)
            {
                var attendus = complets.Where(achat => !achat.Cout.Gt(coupe.Cout)).ToList();
                Assert.That(attendus.Select(Libelle), Has.Member(Libelle(coupe)));
                var restreints = AchatsDisponibles(etat, coupe.Cout);
                Assert.That(restreints.Select(Libelle), Is.EqualTo(attendus.Select(Libelle)), $"budget du {Libelle(coupe)}");
                for (var rang = 0; rang < restreints.Count; rang += 1)
                {
                    Assert.That(restreints[rang].Cout.Eq(attendus[rang].Cout), Is.True, $"coût de {Libelle(restreints[rang])}");
                    Assert.That(restreints[rang].Gain.Eq(attendus[rang].Gain), Is.True, $"gain de {Libelle(restreints[rang])}");
                }
            }

            // Un budget que rien ne paie rend une liste vide — et n'a pas eu besoin de
            // la production totale pour le dire.
            Assert.That(AchatsDisponibles(etat, new Decimal(0)), Is.Empty);
        }

        [Test, Description("un cycle qui dépasse le garde-fou est déclaré non convergent, et la simulation s’arrête")]
        public void Un_cycle_qui_depasse_le_garde_fou_est_declare_non_convergent_et_la_simulation_s_arrete()
        {
            // La bissection de θ (tâche 13) passera par des réglages où un cycle ne
            // converge pas : sans garde-fou, le calibrage boucle.
            var r = Simuler(3, POLITIQUE_PAR_DEFAUT with { DureeMaxParCycleSecondes = 3600 });
            Assert.That(r.CycleNonConvergent, Is.EqualTo(0));
            Assert.That(r.CyclesAcheves, Is.EqualTo(0));
            Assert.That(r.Etat.Permanent.NombreDeRenaissances, Is.EqualTo(0));
        }

        [Test, Description("le Souffle est dépensé en insufflations après la renaissance, et la partie converge toujours")]
        public void Le_Souffle_est_depense_en_insufflations_apres_la_renaissance_et_la_partie_converge_toujours()
        {
            // Spec [D6] : l'échelle de Souffle (~5 au cycle 1, ~1 600 au cycle 2) doit
            // rendre la première insufflation payable dès la première renaissance,
            // sans que tout le registre soit acheté avant le cycle 5.
            var resultat = Simuler(5, null, 1);
            Assert.That(resultat.CycleNonConvergent, Is.Null);
            var rangs = resultat.Etat.Permanent.Insufflations.Values.ToList();
            Assert.That(rangs.Count, Is.GreaterThan(0));
            var total = rangs.Sum();
            Assert.That(total, Is.GreaterThanOrEqualTo(2));
            // Le plan visait <40 comme approximation de « pas tout le registre acheté
            // avant le cycle 5 », mais n'avait pas mesuré la composition sur 5 cycles
            // complets : le Souffle croît de façon exponentielle d'un cycle à l'autre
            // (~5 au cycle 1, ~1600 au cycle 2, bien plus ensuite), et aucune valeur
            // raisonnable de RATIO_COUT_D_INSUFFLATION ne peut contenir ça sans casser
            // l'accessibilité de la première insufflation au cycle 1 (mesuré : 4→12 ne
            // fait passer le total que de 684 à 357 — ruling du contrôleur, tâche B4).
            // 1000 garde une marge large sur le total mesuré à la graine (684) tout en
            // attrapant une vraie régression (boucle infinie, double achat...).
            Assert.That(total, Is.LessThan(1000));
            // Il reste moins de Souffle qu'il n'en faut pour l'insufflation la moins
            // chère : la politique dépense, elle ne thésaurise pas.
            var moinsChere = Insufflations.Toutes.Select(b => Economie.CoutDInsufflation(resultat.Etat, b)).Aggregate((a, b) => a.Lt(b) ? a : b);
            Assert.That(resultat.Etat.Permanent.Souffle.Lt(moinsChere), Is.True);
        }
    }
}
