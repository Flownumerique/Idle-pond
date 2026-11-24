using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// La bibliothèque (GDD §14.8) : ceux qu'il a rencontrés — les tribus —, et le détail de
    /// ce qu'il sait — l'effet exact, chiffré, de chaque succès.
    /// </summary>
    public class BibliothequeTests
    {
        static EtatJeu SansConviction(EtatJeu etat)
        {
            var especes = etat.Cycle.Especes.ToDictionary(p => p.Key, p => p.Value with { Debloquee = false, Niveau = 0 });
            return etat with { Cycle = etat.Cycle with { Especes = especes } };
        }

        [Test, Description("chaque tribu livrée a son nom, son représentant, ses mœurs et son texte")]
        public void Chaque_tribu_livree_a_ses_textes()
        {
            foreach (var espece in Especes.Toutes.Take(6))
            {
                var texte = Textes.DeLaTribu(espece.Id);
                Assert.That(texte, Is.Not.Null, espece.Id);
                Assert.That(new[] { texte.Tribu, texte.Representant, texte.Moeurs, texte.Citation }.All(t => !string.IsNullOrWhiteSpace(t)), Is.True, espece.Id);
            }
            Assert.That(Textes.DeLaTribu("espece-3-1"), Is.Null, "une tribu à venir n'a pas encore de texte");
        }

        [Test, Description("au départ, seule la Noue est atteinte : ses deux tribus en silhouette, rien du Gour")]
        public void Au_depart_seules_les_tribus_de_la_Noue_en_silhouette()
        {
            var tribus = Bibliotheque.Tribus(Reducteur.EtatInitial(1));
            Assert.That(tribus.Select(t => t.Espece.Id), Is.EqualTo(new[] { "vairon", "loche" }));
            Assert.That(tribus.All(t => t.Etat == EtatDeTribu.Apercue), Is.True);
        }

        [Test, Description("une tribu convaincue est connue ; une tribu du lieu, pas encore, reste en silhouette")]
        public void Une_tribu_convaincue_est_connue()
        {
            var etat = EtatDeTravail.Creer();
            var tribus = Bibliotheque.Tribus(etat).ToDictionary(t => t.Espece.Id, t => t.Etat);
            foreach (var espece in Especes.Toutes.Where(e => tribus.ContainsKey(e.Id)))
            {
                var convaincue = etat.Cycle.Especes.TryGetValue(espece.Id, out var v) && v.Debloquee && v.Niveau > 0;
                Assert.That(tribus[espece.Id], Is.EqualTo(convaincue ? EtatDeTribu.Connue : EtatDeTribu.Apercue), espece.Id);
            }
        }

        [Test, Description("connue une fois montée à dix, une tribu ne s'oublie plus à la renaissance")]
        public void Une_tribu_connue_ne_s_oublie_pas()
        {
            var etat = SansConviction(EtatDeTravail.Creer());
            Assert.That(Bibliotheque.EstConnue(etat, "vairon"), Is.False);
            var succes = new Dictionary<string, EntreeDeSucces>(etat.Permanent.Succes)
            {
                ["seuil-vairon-10"] = new EntreeDeSucces(0, PalierDeVoix.Pente),
            };
            etat = etat with { Permanent = etat.Permanent with { Succes = succes } };
            Assert.That(Bibliotheque.EstConnue(etat, "vairon"), Is.True);
            Assert.That(Bibliotheque.Tribus(etat).Single(t => t.Espece.Id == "vairon").Etat, Is.EqualTo(EtatDeTribu.Connue));
        }

        [Test, Description("l'effet d'un succès est dit en clair, et chiffré")]
        public void L_effet_est_dit_et_chiffre()
        {
            Assert.That(Bibliotheque.EffetDe(EffetDeSucces.ReductionCout(TermeDeFormule.CoutNiveau, 0.02)), Is.EqualTo("monter d’un cran coûte 2 % de moins"));
            Assert.That(Bibliotheque.EffetDe(EffetDeSucces.ReductionCout(TermeDeFormule.CoutCreuser, 0.025)), Is.EqualTo("creuser coûte 2,5 % de moins"));
            Assert.That(Bibliotheque.EffetDe(EffetDeSucces.Plafond(TermeDeFormule.ContenanceDeDepart, 0.1)), Is.EqualTo("tu peux porter 10 % de plus"));
            Assert.That(Bibliotheque.EffetDe(EffetDeSucces.Verbe(CapaciteId.CreusementAuto)), Is.EqualTo(Textes.Ecran.EFFET_VERBE));
            Assert.That(Bibliotheque.EffetDe(null), Is.EqualTo(Textes.Ecran.EFFET_AUCUN));
            foreach (var succes in RegistreDesSucces.Tous)
                Assert.That(Bibliotheque.EffetDe(succes.Effet), Is.Not.Empty, succes.Id);
        }

        [Test, Description("le détail : le rapport et la vie seulement une fois arrivé ; la condition toujours")]
        public void Le_detail_d_un_succes()
        {
            var etat = Reducteur.EtatInitial(1);
            var succes = RegistreDesSucces.Tous.First(s => s.Id == "seuil-vairon-10");
            var avant = Bibliotheque.Detail(etat, succes);
            Assert.That(avant.Rapport, Is.Null);
            Assert.That(avant.NoteDansLaVie, Is.Null);
            Assert.That(avant.Condition, Is.Not.Empty);

            var acquis = new Dictionary<string, EntreeDeSucces>(etat.Permanent.Succes) { [succes.Id] = new EntreeDeSucces(2, PalierDeVoix.Pente) };
            var apres = Bibliotheque.Detail(etat with { Permanent = etat.Permanent with { Succes = acquis } }, succes);
            Assert.That(apres.Rapport, Is.EqualTo(Textes.DuSucces(succes.Id).Rapport));
            Assert.That(apres.NoteDansLaVie, Is.EqualTo(3), "noté pendant la troisième vie : deux renaissances avant");
        }
    }
}
