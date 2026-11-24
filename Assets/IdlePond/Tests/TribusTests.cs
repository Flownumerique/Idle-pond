using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les six tribus à l'écran (Codex v1.4, §2) : quand chacune réagit, et ce qu'elle
    /// montre. Rien de tout cela ne touche l'économie.
    /// </summary>
    public class TribusTests
    {
        static readonly string[] Toutes = { Tribus.VAIRON, Tribus.LOCHE, Tribus.EPINOCHE, Tribus.CHABOT, Tribus.LAMPROIE, Tribus.OMBRE };

        static EtatJeu APartDeContenance(EtatJeu etat, double part) =>
            etat with { Cycle = etat.Cycle with { ManaCourant = Economie.Contenance(etat).Mul(part) } };

        [Test, Description("les identifiants des tribus sont ceux des six espèces livrées")]
        public void Les_tribus_sont_les_six_especes_livrees()
        {
            var livrees = Especes.Toutes.Take(6).Select(e => e.Id).ToArray();
            Assert.That(Toutes, Is.EquivalentTo(livrees));
        }

        [Test, Description("les chabots se pétrifient tant que l'eau est trouble, et seulement alors")]
        public void Les_chabots_se_petrifient_dans_l_eau_trouble()
        {
            var etat = EtatDeTravail.Creer();
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.5)).ChabotsPetrifies, Is.False);
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.9)).ChabotsPetrifies, Is.True);
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.9)).ChabotsPetrifies, Is.EqualTo(Economie.EauTroublee(APartDeContenance(etat, 0.9))));
        }

        [Test, Description("les loches s'agitent AVANT que l'eau se trouble, pas après")]
        public void Les_loches_sentent_venir_le_trouble()
        {
            var etat = EtatDeTravail.Creer(contenance: "1e300");
            Assume.That(Economie.EstBloque(etat), Is.False, "la fixture doit pouvoir descendre");
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.5)).LochesAgitees, Is.False);
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.75)).LochesAgitees, Is.True);
            Assert.That(Tribus.Lire(APartDeContenance(etat, 0.9)).LochesAgitees, Is.False, "l'eau est trouble : ce n'est plus un pressentiment");
        }

        [Test, Description("les loches s'agitent quand la descente va se rouvrir")]
        public void Les_loches_sentent_venir_la_descente()
        {
            var etat = APartDeContenance(EtatDeTravail.Creer(), 0.1);
            var cout = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
            EtatJeu AvecContenance(double partDuCout) =>
                APartDeContenance(etat with { Permanent = etat.Permanent with { ContenanceMana = cout.Mul(partDuCout) } }, 0.1);

            var loin = AvecContenance(0.5);
            Assume.That(Economie.EstBloque(loin), Is.True);
            Assert.That(Tribus.Lire(loin).LochesAgitees, Is.False);

            var proche = AvecContenance(0.95);
            Assume.That(Economie.EstBloque(proche), Is.True);
            Assert.That(Tribus.Lire(proche).LochesAgitees, Is.True);
        }

        [Test, Description("un nid d'épinoche par seuil franchi : 0, 1, 2, 3, 4")]
        public void Un_nid_par_seuil_franchi()
        {
            Assert.That(Tribus.NidsDEpinoche(0), Is.EqualTo(0));
            Assert.That(Tribus.NidsDEpinoche(9), Is.EqualTo(0));
            Assert.That(Tribus.NidsDEpinoche(10), Is.EqualTo(1));
            Assert.That(Tribus.NidsDEpinoche(49), Is.EqualTo(2));
            Assert.That(Tribus.NidsDEpinoche(50), Is.EqualTo(3));
            Assert.That(Tribus.NidsDEpinoche(1000), Is.EqualTo(4));
        }

        [Test, Description("les nids tiennent dans la bande, sans se toucher")]
        public void Les_nids_tiennent_dans_la_bande()
        {
            for (var n = 0; n < 4; n++)
            {
                Assert.That(Tribus.XDuNid(n), Is.GreaterThanOrEqualTo(0));
                Assert.That(Tribus.XDuNid(n) + Tribus.LARGEUR_DE_NID, Is.LessThanOrEqualTo(Gabarits.LARGEUR_VISEE));
                if (n > 0) Assert.That(Tribus.XDuNid(n), Is.GreaterThan(Tribus.XDuNid(n - 1) + Tribus.LARGEUR_DE_NID));
                var points = Tribus.PointsDUnNid(n);
                Assert.That(points.All(p => p.X >= 0 && p.X < Tribus.LARGEUR_DE_NID && p.Y >= 0), Is.True);
                Assert.That(points.All(p => p.Couleur == -1 || (p.Couleur >= 0 && p.Couleur < Tribus.HERBES_DU_NID.Count)), Is.True);
                Assert.That(points.Count(p => p.Couleur == -1), Is.EqualTo(1), "une épine de verre par nid");
            }
        }

        [Test, Description("la vague du Chœur passe d'un bout à l'autre de la troupe")]
        public void La_vague_passe_d_un_bout_a_l_autre()
        {
            const int effectif = 8;
            Assert.That(Tribus.EstRouge(0, effectif, 0.05), Is.True);
            Assert.That(Tribus.EstRouge(effectif - 1, effectif, 0.05), Is.False);
            Assert.That(Tribus.EstRouge(0, effectif, 0.95), Is.False);
            Assert.That(Tribus.EstRouge(effectif - 1, effectif, 0.95), Is.True);
            Assert.That(Tribus.EstRouge(0, effectif, 0), Is.False, "pas de vague, pas de rouge");
            Assert.That(Tribus.EstRouge(0, 1, 0.5), Is.True);
            for (var a = 0.01; a < 1; a += 0.05)
                Assert.That(Enumerable.Range(0, effectif).Any(i => Tribus.EstRouge(i, effectif, a)), Is.True, $"au moins un rouge à {a:0.00}");
        }

        [Test, Description("le frisson d'une loche : un pixel au plus, de part et d'autre")]
        public void Le_frisson_reste_d_un_pixel()
        {
            var vus = new HashSet<int>();
            for (var s = 0.0; s < 3; s += 0.01) vus.Add(Tribus.Frisson(s, 0.37));
            Assert.That(vus, Is.EquivalentTo(new[] { -1, 0, 1 }));
        }

        [Test, Description("le fil des lamproies part de la source, s'allonge, puis rejoint la lamproie")]
        public void Le_fil_va_de_la_source_a_la_lamproie()
        {
            var source = new Pixel(10, -20);
            var cible = new Pixel(100, -80);
            var debut = Tribus.PointsDuFil(source, cible, 0.1);
            Assert.That(debut.First(), Is.EqualTo(source));
            Assert.That(debut, Has.No.Member(cible));
            var milieu = Tribus.PointsDuFil(source, cible, 0.5);
            Assert.That(milieu.First(), Is.EqualTo(source));
            Assert.That(milieu.Last(), Is.EqualTo(cible));
            var fin = Tribus.PointsDuFil(source, cible, 0.9);
            Assert.That(fin, Has.No.Member(source));
            Assert.That(fin.Last(), Is.EqualTo(cible));
            Assert.That(milieu.Count, Is.GreaterThan(fin.Count));
        }

        [Test, Description("le mirage clignote, et s'éteint à la fin")]
        public void Le_mirage_clignote_puis_s_eteint()
        {
            Assert.That(Tribus.MirageVisible(0.2), Is.True);
            Assert.That(Tribus.MirageVisible(1.0), Is.False);
            var etats = Enumerable.Range(1, 99).Select(i => Tribus.MirageVisible(i / 100.0)).ToList();
            Assert.That(etats.Skip(50), Has.Member(false), "il clignote avant de disparaître");
        }

        [Test, Description("un seuil du vairon lance la vague ; une renaissance efface tout")]
        public void Un_seuil_du_vairon_lance_la_vague()
        {
            var reactions = new ReactionsDesTribus();
            var presentes = new HashSet<string>(Toutes);
            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.SeuilFranchi, Tribus.VAIRON, 10) }, 5.0, presentes);
            Assert.That(reactions.VagueEnCours(5.5), Is.Not.Null);
            Assert.That(reactions.VagueEnCours(5.0 + Tribus.DUREE_DE_LA_VAGUE), Is.Null);

            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.Renaissance, null, 1) }, 5.1, presentes);
            Assert.That(reactions.EnCours, Is.Empty);
        }

        [Test, Description("une tribu qui monte envoie un fil aux lamproies — un par seconde au plus")]
        public void Un_fil_par_tribu_et_par_seconde()
        {
            var reactions = new ReactionsDesTribus();
            var presentes = new HashSet<string>(Toutes);
            var crans = Enumerable.Range(0, 20).Select(i => new EvenementDeJeu(GenreDEvenement.CransMontes, Tribus.CHABOT, i + 1)).ToArray();
            reactions.Recevoir(crans, 1.0, presentes);
            Assert.That(reactions.EnCours.Count(r => r.Genre == GenreDeReaction.FilDeMana), Is.EqualTo(1));
            Assert.That(reactions.EnCours[0].Source, Is.EqualTo(Tribus.CHABOT));

            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.CransMontes, Tribus.LAMPROIE, 5) }, 3.0, presentes);
            Assert.That(reactions.EnCours.Count(r => r.Source == Tribus.LAMPROIE), Is.EqualTo(0), "elle ne se tire pas de fil à elle-même");

            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.CransMontes, Tribus.VAIRON, 5) }, 3.0, new HashSet<string> { Tribus.VAIRON });
            Assert.That(reactions.EnCours.Count(r => r.Source == Tribus.VAIRON), Is.EqualTo(0), "sans lamproie, pas de fil");
        }

        [Test, Description("une tribu convaincue laisse un mirage, si les ombres sont là")]
        public void Une_tribu_convaincue_laisse_un_mirage()
        {
            var reactions = new ReactionsDesTribus();
            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.EspeceConvaincue, Tribus.LAMPROIE, 1) }, 0, new HashSet<string> { Tribus.VAIRON });
            Assert.That(reactions.EnCours, Is.Empty, "sans ombre, pas de mirage");

            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.EspeceConvaincue, Tribus.LAMPROIE, 1) }, 0, new HashSet<string>(Toutes));
            var mirage = reactions.EnCours.Single(r => r.Genre == GenreDeReaction.Mirage);
            Assert.That(mirage.Source, Is.EqualTo(Tribus.LAMPROIE));
            Assert.That(reactions.Avancer(Tribus.DUREE_DU_MIRAGE + 0.01), Is.False);
            Assert.That(reactions.EnCours, Is.Empty);
        }

        [Test, Description("une lamproie accrochée reste contre sa paroi, dans sa bande")]
        public void Une_lamproie_reste_contre_sa_paroi()
        {
            for (var palier = 0; palier < 18; palier++)
            for (var numero = 0; numero < VueDeScene.POISSONS_MAX_PAR_GROUPE; numero++)
            {
                var cadre = Gabarits.CADRE_D_ESPECE;
                var t = Nage.TrajetAccroche(palier, numero, cadre);
                Assert.That(t.Amplitude, Is.EqualTo(0));
                if (Tribus.AccrocheeAGauche(numero)) Assert.That(t.X0, Is.LessThanOrEqualTo(3));
                else Assert.That(t.X0 + cadre.Largeur, Is.GreaterThanOrEqualTo(Gabarits.LARGEUR_VISEE - 3));
                Assert.That(t.X0, Is.GreaterThanOrEqualTo(0));
                Assert.That(t.X0 + cadre.Largeur, Is.LessThanOrEqualTo(Gabarits.LARGEUR_VISEE));
                var haut = -palier * Gabarits.HAUTEUR_DE_BANDE;
                Assert.That(t.Y - 1, Is.GreaterThanOrEqualTo(haut - Gabarits.HAUTEUR_DE_BANDE));
                Assert.That(t.Y + cadre.Hauteur, Is.LessThanOrEqualTo(haut));
                Assert.That(Nage.TrajetAccroche(palier, numero, cadre), Is.EqualTo(t), "déterministe");
            }
        }
    }
}
