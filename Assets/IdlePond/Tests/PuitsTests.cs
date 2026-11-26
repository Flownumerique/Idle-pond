using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le puits (décidé par l'auteur le 2026-11-25) : on creuse un puits, pas les étages d'une
    /// tour — des parois qui ondulent, une corniche par palier, un sol au fond seulement, la
    /// lumière qui glisse d'un palier à l'autre, et le sol qui s'ouvre quand on creuse.
    /// </summary>
    public class PuitsTests
    {
        const int H = Gabarits.HAUTEUR_DE_BANDE;
        static readonly int PROFONDEUR = Assises.PALIERS_LIVRES * H;

        [Test, Description("les parois ondulent sans jamais fermer le puits ni manger la place des nageurs")]
        public void Les_parois_ondulent_sans_fermer_le_puits()
        {
            var gauches = Enumerable.Range(0, PROFONDEUR).Select(y => Puits.BordGauche(-y)).ToList();
            var droits = Enumerable.Range(0, PROFONDEUR).Select(y => Puits.BordDroit(-y)).ToList();
            Assert.That(gauches.All(x => x >= 0 && x <= Puits.MARGE_DES_PAROIS), Is.True);
            Assert.That(droits.All(x => x < Gabarits.LARGEUR_VISEE && x >= Gabarits.LARGEUR_VISEE - 1 - Puits.MARGE_DES_PAROIS), Is.True);
            Assert.That(gauches.Distinct().Count(), Is.GreaterThan(12), "un bord qui ondule, pas une ligne");
            // D'une rangée à l'autre, le bord ne saute jamais : il se raccorde.
            for (var y = 1; y < PROFONDEUR; y++)
            {
                Assert.That(System.Math.Abs(gauches[y] - gauches[y - 1]), Is.LessThanOrEqualTo(2), $"y {-y}");
                Assert.That(System.Math.Abs(droits[y] - droits[y - 1]), Is.LessThanOrEqualTo(2), $"y {-y}");
            }
        }

        [Test, Description("chaque palier a sa corniche, qui sort d'une paroi et change de côté en descendant")]
        public void Chaque_palier_a_sa_corniche()
        {
            var corniches = Enumerable.Range(0, Assises.PALIERS_LIVRES).Select(Puits.CornicheDe).ToList();
            for (var p = 0; p < corniches.Count; p++)
            {
                var c = corniches[p];
                Assert.That(c.Longueur, Is.InRange(Puits.CORNICHE_LA_PLUS_COURTE, Puits.CORNICHE_LA_PLUS_LONGUE));
                Assert.That(c.Sol, Is.EqualTo(-(p + 1) * H + Puits.EPAISSEUR_DE_CORNICHE));
                if (c.AGauche) Assert.That(c.Debut, Is.EqualTo(Puits.BordGauche(c.Sol)), "elle sort de la paroi");
                else Assert.That(c.Fin, Is.EqualTo(Puits.BordDroit(c.Sol) + 1), "elle sort de la paroi");
                Assert.That(c.Debut >= 0 && c.Fin <= Gabarits.LARGEUR_VISEE, Is.True);
            }
            Assert.That(corniches.Count(c => c.AGauche), Is.InRange(corniches.Count / 4, corniches.Count * 3 / 4), "des deux côtés");
            Assert.That(corniches.Zip(corniches.Skip(1), (a, b) => a.AGauche != b.AGauche).Count(change => change),
                Is.GreaterThan(corniches.Count / 2), "le plus souvent, elle change de côté");
            Assert.That(corniches.Select(c => c.Longueur).Distinct().Count(), Is.GreaterThan(corniches.Count / 2), "pas deux fois la même");
        }

        [Test, Description("ce qui se pose sur une corniche y tient, chacun dans sa case")]
        public void Ce_qui_se_pose_tient_sur_la_corniche()
        {
            for (var p = 0; p < Assises.PALIERS_LIVRES; p++)
            {
                var c = Puits.CornicheDe(p);
                foreach (var (cases, largeur) in new[] { (6, 7), (4, 18), (3, 7) })
                    for (var n = 0; n < cases; n++)
                    {
                        var x = Puits.XSurLaCorniche(c, n, cases, largeur, 11);
                        Assert.That(x, Is.GreaterThanOrEqualTo(c.Debut));
                        Assert.That(x + largeur, Is.LessThanOrEqualTo(c.Fin));
                        if (n > 0) Assert.That(x, Is.GreaterThan(Puits.XSurLaCorniche(c, n - 1, cases, largeur, 11) + largeur));
                    }
            }
        }

        [Test, Description("le sol s'ouvre : la roche du palier neuf recule vers le bas, vite puis doucement")]
        public void Le_sol_s_ouvre()
        {
            Assert.That(Puits.RocheRestante(0), Is.EqualTo(H));
            Assert.That(Puits.RocheRestante(Puits.DUREE_DE_L_OUVERTURE), Is.EqualTo(0));
            Assert.That(Puits.RocheRestante(99), Is.EqualTo(0));
            var restes = Enumerable.Range(0, 15).Select(i => Puits.RocheRestante(i * 0.1)).ToList();
            for (var i = 1; i < restes.Count; i++) Assert.That(restes[i], Is.LessThanOrEqualTo(restes[i - 1]));
            Assert.That(H - Puits.RocheRestante(0.2), Is.GreaterThan(Puits.RocheRestante(1.0) - Puits.RocheRestante(1.2)), "vite d'abord");
        }

        [Test, Description("la lumière glisse d'un palier à l'autre, sauf au-dessus d'une cassure")]
        public void La_lumiere_glisse_sauf_a_une_cassure()
        {
            var noue = Assises.Toutes[0];
            var gour = Assises.Toutes[1];
            for (var p = 0; p < noue.NombreDePaliers - 1; p++)
                Assert.That(RegistreDArt.LumiereAuBasDu(p), Is.EqualTo((RegistreDArt.LumiereDuPalier(p + 1), RegistreDArt.TeinteDuPalier(p + 1))), $"palier {p}");
            // Le Gour s'ouvre par une cassure : la dernière lumière de la Noue ne prend pas le bleu.
            var dernier = noue.NombreDePaliers - 1;
            var (lumiere, teinte) = RegistreDArt.LumiereAuBasDu(dernier);
            Assert.That(RegistreDArt.DecorDe("gour").Jonction, Is.EqualTo(JonctionDeLieu.Cassure));
            Assert.That(teinte, Is.EqualTo(RegistreDArt.TeinteDuPalier(dernier)));
            Assert.That(lumiere, Is.LessThan(RegistreDArt.LumiereDuPalier(dernier)), "la pente de la Noue continue");
            Assert.That(Lieux.ACassure(gour.IndexPremierPalier), Is.True);
            Assert.That(Lieux.ACassure(gour.IndexPremierPalier + 1), Is.False);
            // Au bout du contenu, rien ne casse.
            Assert.That(RegistreDArt.LumiereAuBasDu(Assises.PALIERS_LIVRES - 1).Lumiere, Is.GreaterThan(0));
            Assert.That(RegistreDArt.DecorDe("lieu-sans-dessin").Jonction, Is.EqualTo(JonctionDeLieu.Fondu), "le fondu est le défaut");
        }

        [Test, Description("chaque lieu dessiné a ses parois à lui : la terre de la Noue, le calcaire du Gour")]
        public void Chaque_lieu_a_la_matiere_de_ses_parois()
        {
            var matieres = RegistreDArt.ASSISES_DESSINEES.Select(a => RegistreDArt.DecorDe(a).Paroi).ToList();
            Assert.That(matieres, Is.Unique, "deux lieux aux mêmes parois ne se distinguent que par la couleur");
            Assert.That(RegistreDArt.DecorDe("noue").Paroi, Is.EqualTo(MatiereDeParoi.Terre));
            Assert.That(RegistreDArt.DecorDe("gour").Paroi, Is.EqualTo(MatiereDeParoi.Calcaire));
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                var decor = RegistreDArt.DecorDe(assise);
                Assert.That(decor.Roche.Count, Is.EqualTo(4), assise);
                if (decor.Paroi != MatiereDeParoi.Pierres)
                    Assert.That(decor.Inclusions.Count, Is.GreaterThanOrEqualTo(2), $"{assise} : la matière dessine ses galets ou sa calcite");
            }
            // Les racines qui sortent de la terre sont celles de la berge.
            Assert.That(RegistreDArt.DecorDe("noue").Racines, Is.Not.Empty);
        }
    }
}
