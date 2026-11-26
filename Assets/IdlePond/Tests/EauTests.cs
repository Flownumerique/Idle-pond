using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// L'eau vivante (2026-11-25) : les caustiques, les bulles, le pollen de la Noue, les filets
    /// du courant du Gour, et la teinte de l'eau qui change en descendant dans chaque lieu.
    /// </summary>
    public class EauTests
    {
        static readonly double[] INSTANTS = Enumerable.Range(0, 400).Select(i => i * 0.137).ToArray();
        const int H = Gabarits.HAUTEUR_DE_BANDE;

        [Test, Description("les caustiques glissent par pixels entiers, en sens contraires, plus vite dans le courant")]
        public void Les_caustiques_glissent()
        {
            foreach (var t in INSTANTS)
                for (var c = 0; c < Eau.COUCHES_DE_CAUSTIQUES; c++)
                    foreach (var courant in new[] { false, true })
                        Assert.That(Eau.DecalageDesCaustiques(c, t, courant), Is.InRange(0, Eau.LARGEUR_DES_CAUSTIQUES - 1));
            Assert.That(Eau.DecalageDesCaustiques(0, 1, false), Is.EqualTo(4));
            Assert.That(Eau.DecalageDesCaustiques(1, 1, false), Is.EqualTo(Eau.LARGEUR_DES_CAUSTIQUES - 3), "la seconde couche part à gauche");
            Assert.That(Eau.DecalageDesCaustiques(0, 1, true), Is.EqualTo(10), "le courant les emporte");
            Assert.That(Gabarits.LARGEUR_DU_DECOR % Eau.LARGEUR_DES_CAUSTIQUES, Is.EqualTo(0), "le motif se raccorde au décor");
        }

        [Test, Description("les reflets pâlissent en descendant, comme la lumière")]
        public void Les_reflets_palissent_en_descendant()
        {
            var forces = Enumerable.Range(0, Assises.PALIERS_LIVRES).Select(p => Eau.ForceDesCaustiques(0, p)).ToList();
            for (var p = 1; p < forces.Count; p++) Assert.That(forces[p], Is.LessThan(forces[p - 1]), $"palier {p}");
            Assert.That(forces[0], Is.LessThan(0.5), "jamais un aplat de lumière");
            Assert.That(Eau.ForceDesCaustiques(1, 0), Is.LessThan(Eau.ForceDesCaustiques(0, 0)), "la seconde couche est la plus pâle");
        }

        [Test, Description("une bulle monte dans sa bande, et disparaît un temps entre deux montées")]
        public void Une_bulle_monte_dans_sa_bande()
        {
            for (var bande = 0; bande < 8; bande++)
                for (var n = 0; n < Eau.BULLES; n++)
                {
                    var vues = INSTANTS.Select(t => Eau.Bulle(bande, n, t)).ToList();
                    Assert.That(vues.Any(b => b == null), Is.True, "une pause entre deux montées");
                    foreach (var b in vues.Where(b => b != null))
                    {
                        Assert.That(b.Y, Is.InRange(-(bande + 1) * H + 4, -bande * H - 2), $"bande {bande}");
                        Assert.That(b.X, Is.InRange(Puits.MARGE_DES_PAROIS, Gabarits.LARGEUR_VISEE - Puits.MARGE_DES_PAROIS - 2), "entre les parois");
                    }
                }
            // Elle monte : sur une courte durée sans pause, son Y croît.
            var avant = Eau.Bulle(2, 0, 10.0);
            var apres = Eau.Bulle(2, 0, 10.3);
            if (avant != null && apres != null && apres.Y > avant.Y - 10) Assert.That(apres.Y, Is.GreaterThanOrEqualTo(avant.Y));
        }

        [Test, Description("le pollen descend dans sa bande ; les filets filent à travers le cadre")]
        public void Pollen_et_filets_restent_dans_leur_bande()
        {
            foreach (var t in INSTANTS)
                for (var n = 0; n < Eau.GRAINS; n++)
                {
                    var g = Eau.Grain(3, n, t);
                    if (g == null) continue;
                    Assert.That(g.Y, Is.InRange(-4 * H + 4, -3 * H - 2));
                }
            var xs = new System.Collections.Generic.HashSet<int>();
            foreach (var t in INSTANTS)
                for (var n = 0; n < Eau.FILETS; n++)
                {
                    var f = Eau.Filet(7, n, t);
                    if (f == null) continue;
                    Assert.That(f.Y, Is.InRange(-8 * H + 10, -7 * H - 10), "entre la surface et les galets");
                    Assert.That(f.Longueur, Is.InRange(4, 10));
                    Assert.That(f.X, Is.GreaterThanOrEqualTo(Puits.BordGauche(f.Y)), "pas dans la paroi");
                    Assert.That(f.X + f.Longueur - 1, Is.LessThanOrEqualTo(Puits.BordDroit(f.Y)), "pas dans la paroi");
                    xs.Add(f.X);
                }
            Assert.That(xs.Min(), Is.LessThan(Puits.MARGE_DES_PAROIS));
            Assert.That(xs.Max(), Is.GreaterThan(Gabarits.LARGEUR_VISEE - Puits.MARGE_DES_PAROIS - 12), "il traverse tout le puits");
        }

        [Test, Description("chaque lieu met dans son eau ce que dit son décor : pollen dans la Noue, courant dans le Gour")]
        public void Chaque_lieu_a_son_eau()
        {
            Assert.That(Eau.APollen("noue") && !Eau.ACourant("noue"), Is.True);
            Assert.That(Eau.ACourant("gour") && !Eau.APollen("gour"), Is.True);
            Assert.That(Eau.CouleurDesBulles("noue"), Is.Not.EqualTo(Eau.CouleurDesBulles("gour")));
            Assert.That(Eau.CouleurDesCaustiques("noue"), Is.EqualTo(RegistreDArt.DecorDe("noue").Rayon));
        }

        [Test, Description("l'eau change de teinte en descendant : la Noue vers le vert d'eau, le Gour vers l'indigo")]
        public void L_eau_change_de_teinte_en_descendant()
        {
            var noue = Assises.Toutes[0];
            var gour = Assises.Toutes[1];
            Assert.That(RegistreDArt.TeinteDuPalier(0), Is.EqualTo(RegistreDArt.DecorDe("noue").TeinteDeLumiere));
            Assert.That(RegistreDArt.TeinteDuPalier(noue.NombreDePaliers - 1), Is.EqualTo(RegistreDArt.DecorDe("noue").TeinteDuFond));
            Assert.That(RegistreDArt.TeinteDuPalier(gour.IndexPremierPalier), Is.EqualTo(RegistreDArt.DecorDe("gour").TeinteDeLumiere));
            Assert.That(RegistreDArt.TeinteDuPalier(gour.IndexPremierPalier + gour.NombreDePaliers - 1), Is.EqualTo(RegistreDArt.DecorDe("gour").TeinteDuFond));
            // Le Gour bleuit en descendant : le rouge baisse moins vite que le vert.
            int R(int c) => (c >> 16) & 0xFF;
            int G(int c) => (c >> 8) & 0xFF;
            var haut = RegistreDArt.TeinteDuPalier(gour.IndexPremierPalier);
            var bas = RegistreDArt.TeinteDuPalier(gour.IndexPremierPalier + gour.NombreDePaliers - 1);
            Assert.That(G(bas) - R(bas), Is.LessThan(G(haut) - R(haut)), "vers l'indigo");
            Assert.That(RegistreDArt.Melanger(0x000000, 0xFF8040, 0.5), Is.EqualTo(0x804020));
        }
    

        [Test, Description("dans la vasque du Gour, les bulles s'arrêtent sous la membrane")]
        public void Les_bulles_s_arretent_sous_la_membrane()
        {
            const int VASQUE = 6;
            var vues = 0;
            for (var n = 0; n < Eau.BULLES; n++)
                foreach (var b in INSTANTS.Select(t => Eau.Bulle(VASQUE, n, t)).Where(b => b != null))
                {
                    vues++;
                    // Le point fait deux pixels de haut ; la membrane et sa lueur descendent de 5 sous son arc.
                    Assert.That(b.Y + 2, Is.LessThanOrEqualTo(-VASQUE * H - Lieux.CreuxDeLaMembrane(b.X) - 5 + 1e-9), $"bulle à x = {b.X}");
                }
            Assert.That(vues, Is.GreaterThan(0));
        }
    }
}
