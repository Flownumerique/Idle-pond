using System.IO;
using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using Newtonsoft.Json;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// La scène — spec 2026-09-17 §3.3. Ce qui se teste : la projection pure de l'état vers
    /// ce que la scène dessine, et les données de palette. Le dessin lui-même n'est pas
    /// testé ici (spec du portage §3).
    /// </summary>
    public class VueDeSceneTests
    {
        [Test, Description("C2 — la vue : liste exactement les paliers ouverts, avec leur assise et leur espèce")]
        public void La_vue_liste_exactement_les_paliers_ouverts_avec_leur_assise_et_leur_espece()
        {
            var etat = EtatDeTravail.Creer();
            var vue = VueDeScene.Depuis(etat);
            Assert.That(vue.Paliers.Count, Is.EqualTo(etat.Cycle.PaliersOuverts));
            for (var i = 0; i < vue.Paliers.Count; i++)
            {
                Assert.That(vue.Paliers[i].Index, Is.EqualTo(i));
                Assert.That(Assises.Toutes.Any(a => a.Id == vue.Paliers[i].Assise), Is.True);
            }
            var avecEspece = vue.Paliers.Where(p => p.Espece != null).Select(p => p.Espece.Id).ToList();
            var attendues = Especes.Toutes
                .Where(e => e.Palier < etat.Cycle.PaliersOuverts && etat.Cycle.Especes.TryGetValue(e.Id, out var v) && v.Debloquee)
                .Select(e => e.Id).ToList();
            Assert.That(avecEspece, Is.EqualTo(attendues));
        }

        [Test, Description("C2 — la vue : une espèce non débloquée n’apparaît pas, même si son palier est ouvert")]
        public void Une_espece_non_debloquee_n_apparait_pas_meme_si_son_palier_est_ouvert()
        {
            var etat = Reducteur.EtatInitial(1);
            etat = etat with { Cycle = etat.Cycle with { ManaCourant = etat.Cycle.ManaCourant.Mul(1e6) } };
            etat = Reducteur.Creuser(Reducteur.Creuser(Reducteur.Creuser(etat)));
            Assert.That(VueDeScene.Depuis(etat).Paliers.All(p => p.Espece == null), Is.True);
            etat = Reducteur.Debloquer(etat, Especes.Toutes[0].Id);
            etat = Reducteur.Ameliorer(etat, Especes.Toutes[0].Id);
            var premier = VueDeScene.Depuis(etat).Paliers[0];
            Assert.That(premier.Espece, Is.EqualTo(new VueDEspece(Especes.Toutes[0].Id, 0, 2)));
        }

        [Test, Description("DA §3 — la vue : le héros porte son niveau, son stade et ses couches")]
        public void Le_heros_porte_son_niveau_son_stade_et_ses_couches()
        {
            var base_ = EtatDeTravail.Creer();
            var etat = base_ with
            {
                Cycle = base_.Cycle with { NiveauDuHeros = 16 },
                Permanent = base_.Permanent with { Couches = new[] { "noue" } },
            };
            var heros = VueDeScene.Depuis(etat).Heros;
            Assert.That(heros.Niveau, Is.EqualTo(16));
            Assert.That(heros.Stade, Is.EqualTo(2));
            Assert.That(heros.Couches, Is.EqualTo(new[] { "noue" }));
        }

        [Test, Description("DA §3 — le stade change aux niveaux 4, 16 et 256")]
        public void Le_stade_change_aux_niveaux_4_16_et_256()
        {
            var attendus = new[] { (1, 0), (3, 0), (4, 1), (15, 1), (16, 2), (255, 2), (256, 3), (100000, 3) };
            foreach (var (niveau, stade) in attendus)
                Assert.That(Gabarits.StadeDuNiveau(niveau), Is.EqualTo(stade), $"niveau {niveau}");
        }

        [Test, Description("DA §3 — le stade est borné à 0..3, même pour un niveau absurde")]
        public void Le_stade_est_borne()
        {
            Assert.That(Gabarits.StadeDuNiveau(0), Is.EqualTo(0));
            Assert.That(Gabarits.StadeDuNiveau(-5), Is.EqualTo(0));
            Assert.That(Gabarits.StadeDuNiveau(int.MaxValue), Is.EqualTo(3));
        }

        [Test, Description("DA §3 — après une renaissance : stade 0, et la marque reste")]
        public void Apres_une_renaissance_le_heros_redevient_petit_et_garde_ses_marques()
        {
            var etat = Reducteur.Tick(EtatDeTravail.Creer(777), 3600);
            etat = etat with
            {
                Cycle = etat.Cycle with { NiveauDuHeros = 20 },
                Permanent = etat.Permanent with { Couches = new[] { "noue" } },
            };
            Assert.That(VueDeScene.Depuis(etat).Heros.Stade, Is.EqualTo(2));
            var apres = VueDeScene.Depuis(Renaissance.Renaitre(etat)).Heros;
            Assert.That(apres.Stade, Is.EqualTo(0));
            Assert.That(apres.Couches, Has.Member("noue"));
        }

        [Test, Description("DA §3 — une mue : le stade monte ; jamais au premier dessin ni en rapetissant")]
        public void Une_mue_est_un_stade_qui_monte()
        {
            Assert.That(VueDeScene.EstUneMue(null, 2), Is.False, "une partie chargée n’a pas mué");
            Assert.That(VueDeScene.EstUneMue(0, 1), Is.True);
            Assert.That(VueDeScene.EstUneMue(1, 3), Is.True);
            Assert.That(VueDeScene.EstUneMue(2, 2), Is.False);
            Assert.That(VueDeScene.EstUneMue(3, 0), Is.False, "la renaissance n’est pas une mue");
        }

        [Test, Description("C2 — la vue : l’eau trouble et la saturation passent dans la vue")]
        public void L_eau_trouble_et_la_saturation_passent_dans_la_vue()
        {
            var vue = VueDeScene.Depuis(Reducteur.EtatInitial(1));
            Assert.That(vue.EauTroublee, Is.False);
            Assert.That(vue.Sature, Is.False);
        }

        [Test, Description("C2 — la vue : sérialisable telle quelle, et deux vues identiques ont la même clef")]
        public void La_vue_est_serialisable_telle_quelle()
        {
            Assert.That(() => JsonConvert.SerializeObject(VueDeScene.Depuis(EtatDeTravail.Creer())), Throws.Nothing);
            Assert.That(VueDeScene.Depuis(EtatDeTravail.Creer()).Clef, Is.EqualTo(VueDeScene.Depuis(EtatDeTravail.Creer()).Clef));
            var etat = EtatDeTravail.Creer();
            var autre = etat with { Cycle = etat.Cycle with { NiveauDuHeros = etat.Cycle.NiveauDuHeros + 1 } };
            Assert.That(VueDeScene.Depuis(autre).Clef, Is.Not.EqualTo(VueDeScene.Depuis(etat).Clef));
        }

        [Test, Description("C2 — la vue : un banc croît avec le logarithme du niveau, jusqu’à 14 poissons")]
        public void Le_banc_croit_avec_le_logarithme_du_niveau_jusqu_a_quatorze_poissons()
        {
            Assert.That(VueDeScene.EffectifDesPoissons(1), Is.EqualTo(1));
            Assert.That(VueDeScene.EffectifDesPoissons(2), Is.EqualTo(2));
            Assert.That(VueDeScene.EffectifDesPoissons(8), Is.EqualTo(4));
            Assert.That(VueDeScene.EffectifDesPoissons(1 << 20), Is.EqualTo(14));
        }

        [Test, Description("C2 — la palette : six palettes, une par assise, et la lumière baisse en descendant")]
        public void Six_palettes_une_par_assise_et_la_lumiere_baisse_en_descendant()
        {
            Assert.That(Palette.Palettes.Count, Is.EqualTo(Assises.Toutes.Count));
            Assert.That(Assises.Toutes.All(a => Palette.Palettes.ContainsKey(a.Id)), Is.True);
            var lumieres = Assises.Toutes.Select(a => Palette.PaletteDe(a.Id).Lumiere).ToList();
            for (var i = 1; i < lumieres.Count; i++) Assert.That(lumieres[i], Is.LessThan(lumieres[i - 1]));
        }

        [Test, Description("C2 — la palette : une marque par assise, toutes différentes")]
        public void Une_marque_par_assise_toutes_differentes()
        {
            var marques = Assises.Toutes.Select(a => Palette.MarqueParAssise[a.Id]).ToList();
            Assert.That(marques.Distinct().Count(), Is.EqualTo(Assises.Toutes.Count));
        }

        [Test, Description("C2 — la palette : les poissons d’un rang ont une couleur valide, et elle change avec le rang")]
        public void Les_poissons_d_un_rang_ont_une_couleur_valide_et_elle_change_avec_le_rang()
        {
            var couleurs = Enumerable.Range(0, 21).Select(Palette.CouleurDesPoissons).ToList();
            Assert.That(couleurs.All(c => c >= 0 && c <= 0xFFFFFF), Is.True);
            Assert.That(couleurs.Distinct().Count(), Is.GreaterThan(15));
        }

        [Test, Description("C2 — la vue : VueDeScene.cs et Palette.cs ne référencent pas UnityEngine")]
        public void VueDeScene_et_Palette_ne_referencent_pas_UnityEngine()
        {
            foreach (var f in new[]
                     {
                         "Assets/IdlePond/Jeu/Scene/VueDeScene.cs", "Assets/IdlePond/Jeu/Scene/Palette.cs",
                         "Assets/IdlePond/Jeu/Scene/Gabarits.cs", "Assets/IdlePond/Jeu/Scene/RegistreDArt.cs",
                     })
                Assert.That(File.ReadAllText(f), Does.Not.Contain("using UnityEngine"));
        }
    }
}
