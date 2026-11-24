using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// La faune d'ambiance (Codex v1.3, §2) : elle vit dans la mare sans rien produire,
    /// reste dans son champ, et change avec les renaissances et les ralliements.
    /// </summary>
    public class FauneTests
    {
        static readonly double[] INSTANTS = Enumerable.Range(0, 400).Select(i => i * 0.137).ToArray();

        static void DansLeChamp(PoseDeFaune p, int bande)
        {
            var cadre = Faune.DESSINS[p.Nom].Cadre;
            Assert.That(p.X, Is.GreaterThanOrEqualTo(0), p.Nom);
            Assert.That(p.X + cadre.Largeur, Is.LessThanOrEqualTo(Gabarits.LARGEUR_VISEE), p.Nom);
            var haut = -bande * Gabarits.HAUTEUR_DE_BANDE;
            Assert.That(p.Y, Is.GreaterThanOrEqualTo(haut - Gabarits.HAUTEUR_DE_BANDE), p.Nom);
            Assert.That(p.Y + cadre.Hauteur, Is.LessThanOrEqualTo(haut), p.Nom + " dépasse la surface");
            Assert.That(p.Image, Is.InRange(0, Faune.DESSINS[p.Nom].Images - 1), p.Nom);
        }

        [Test, Description("les bêtes de surface restent dans la première bande, sous la surface")]
        public void Les_betes_de_surface_restent_dans_la_premiere_bande()
        {
            foreach (var t in INSTANTS)
            {
                for (var n = 0; n < Faune.LIBELLULES; n++) DansLeChamp(Faune.Libellule(n, t), 0);
                for (var n = 0; n < Faune.GERRIS_A_LA_SURFACE; n++) DansLeChamp(Faune.Gerris(n, t), 0);
                for (var n = 0; n < 3; n++) DansLeChamp(Faune.Grenouille(n, t, 4), 0);
                DansLeChamp(Faune.Hydre(t), 0);
            }
        }

        [Test, Description("la lanterne d'une libellule s'allume en éclair, jamais en continu")]
        public void La_lanterne_s_allume_en_eclair()
        {
            var allumee = INSTANTS.Count(t => Faune.Libellule(0, t).Image == Faune.IMAGE_ALLUMEE);
            Assert.That(allumee, Is.GreaterThan(0));
            Assert.That(allumee, Is.LessThan(INSTANTS.Length / 8), "une fois toutes les sept secondes, pas plus");
        }

        [Test, Description("un gerris file puis se pose, et laisse une rune là où il s'est posé")]
        public void Un_gerris_laisse_des_runes()
        {
            var positions = INSTANTS.Select(t => Faune.Gerris(1, t).X).Distinct().Count();
            Assert.That(positions, Is.GreaterThan(5), "il se déplace");
            var avecRune = INSTANTS.Count(t => Faune.RuneDuGerris(1, t) != null);
            Assert.That(avecRune, Is.GreaterThan(0).And.LessThan(INSTANTS.Length), "la rune apparaît, puis s'efface");
            foreach (var t in INSTANTS)
            {
                var rune = Faune.RuneDuGerris(1, t);
                if (rune != null) Assert.That(rune.X >= 0 && rune.X < Gabarits.LARGEUR_VISEE && rune.Y < 0, Is.True);
            }
        }

        [Test, Description("les grenouilles-chantres : une de plus et plus bavardes à chaque renaissance")]
        public void Les_grenouilles_suivent_les_renaissances()
        {
            Assert.That(Enumerable.Range(0, 6).Select(Faune.Grenouilles), Is.EqualTo(new[] { 1, 2, 3, 3, 3, 3 }));
            Assert.That(Faune.PeriodeDuChant(0), Is.GreaterThan(Faune.PeriodeDuChant(3)));
            Assert.That(Faune.PeriodeDuChant(100), Is.EqualTo(2.5));
            int Chants(int r) => INSTANTS.Count(t => Faune.Grenouille(0, t, r).Image == 1);
            Assert.That(Chants(5), Is.GreaterThan(Chants(0)));
        }

        [Test, Description("les jardins de la Sève fleurissent et grandissent à chaque renaissance")]
        public void Les_jardins_fleurissent()
        {
            Assert.That(Faune.Jardin(0, 0), Is.Empty, "avant la première renaissance, rien");
            Assert.That(Faune.Jardin(2, 1), Has.Count.EqualTo(1));
            Assert.That(Faune.Jardin(2, 20), Has.Count.EqualTo(6));
            Assert.That(new[] { 1, 3, 6 }.Select(Faune.StadeDesFleurs), Is.EqualTo(new[] { 0, 1, 2 }));
            foreach (var fleur in Faune.Jardin(4, 20)) DansLeChamp(fleur, 4);
            var xs = Faune.Jardin(4, 20).Select(f => f.X).ToList();
            for (var i = 1; i < xs.Count; i++) Assert.That(xs[i], Is.GreaterThan(xs[i - 1] + Faune.DESSINS[Faune.FLEUR].Cadre.Largeur));
        }

        [Test, Description("la procession traverse le fond de la bande, bannière en tête, puis s'en va")]
        public void La_procession_traverse_la_bande()
        {
            Assert.That(Faune.Procession(2, 0), Is.Empty);
            Assert.That(Faune.Procession(2, 1), Is.Empty);
            var vues = new HashSet<string>();
            int? tetePrecedente = null;
            for (var a = 0.01; a < 1; a += 0.01)
            {
                var poses = Faune.Procession(2, a);
                foreach (var p in poses)
                {
                    // Elles entrent et sortent par les bords : chacune touche le champ, sans y tenir toute.
                    var largeur = Faune.DESSINS[p.Nom].Cadre.Largeur;
                    Assert.That(p.X + largeur > 0 && p.X < Gabarits.LARGEUR_VISEE, Is.True, p.Nom);
                    Assert.That(p.Y, Is.EqualTo(-3 * Gabarits.HAUTEUR_DE_BANDE + 4), "au fond de sa bande");
                    vues.Add(p.Nom);
                }
                var tete = poses.FirstOrDefault(p => p.Nom == Faune.ECREVISSE_BANNIERE);
                if (tete == null) continue;
                if (tetePrecedente.HasValue) Assert.That(tete.X, Is.GreaterThanOrEqualTo(tetePrecedente.Value), "de gauche à droite");
                tetePrecedente = tete.X;
            }
            Assert.That(vues, Is.EquivalentTo(new[] { Faune.ECREVISSE, Faune.ECREVISSE_BANNIERE }));
        }

        [Test, Description("une tribu ralliée lance la procession des écrevisses")]
        public void Une_tribu_ralliee_lance_la_procession()
        {
            var reactions = new ReactionsDesTribus();
            reactions.Recevoir(new[] { new EvenementDeJeu(GenreDEvenement.EspeceConvaincue, Tribus.CHABOT, 1) }, 3.0, new HashSet<string>());
            var procession = reactions.EnCours.Single(r => r.Genre == GenreDeReaction.Procession);
            Assert.That(procession.Source, Is.EqualTo(Tribus.CHABOT));
            Assert.That(procession.Duree, Is.EqualTo(Faune.DUREE_DE_LA_PROCESSION));
        }
    }
}
