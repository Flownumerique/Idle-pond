using System.Collections.Generic;
using System.IO;
using IdlePond.Editeur;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les gabarits du dessin (spec DA §5) : ce qu'un sprite, provisoire ou vrai, doit
    /// respecter pour prendre sa place sans code.
    /// </summary>
    public class GabaritsTests
    {
        [Test, Description("le cadre d'un corps : sa longueur plus le contour, et une hauteur impaire")]
        public void Le_cadre_d_un_corps()
        {
            Assert.That(Gabarits.CadreDuCorps(0), Is.EqualTo(new Cadre(23, 15)));
            Assert.That(Gabarits.CadreDuCorps(1), Is.EqualTo(new Cadre(30, 17)));
            Assert.That(Gabarits.CadreDuCorps(2), Is.EqualTo(new Cadre(44, 23)));
            Assert.That(Gabarits.CadreDuCorps(3), Is.EqualTo(new Cadre(65, 31)));
        }

        [Test, Description("une espèce mesure de 7 à 12 px selon son rang")]
        public void Une_espece_mesure_de_7_a_12_px()
        {
            Assert.That(Gabarits.LongueurDEspece(0), Is.EqualTo(7));
            Assert.That(Gabarits.LongueurDEspece(20), Is.EqualTo(12));
            Assert.That(Enumerable.Range(0, 21).Select(Gabarits.LongueurDEspece).All(l => l >= 7 && l <= 12), Is.True);
        }

        [Test, Description("chaque ancrage tombe dans le tronc, à chaque stade")]
        public void Chaque_ancrage_tombe_dans_le_tronc()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                var tronc = Gabarits.TroncDUnPoisson(Gabarits.LONGUEURS_DU_CORPS[stade]);
                foreach (AncrageId ancrage in System.Enum.GetValues(typeof(AncrageId)))
                {
                    var p = Gabarits.Ancrage(stade, ancrage);
                    Assert.That(tronc.Contient(p.X, p.Y), Is.True, $"stade {stade}, {ancrage} en ({p.X}, {p.Y})");
                }
            }
        }

        [Test, Description("la Noue : six paliers éclairés, la lumière baisse, et ne dépasse jamais 1 avec l'ambiante")]
        public void La_lumiere_de_la_Noue_baisse_et_reste_sous_1()
        {
            var noue = Assises.Toutes[0];
            var lumieres = Enumerable.Range(noue.IndexPremierPalier, noue.NombreDePaliers).Select(RegistreDArt.LumiereDuPalier).ToList();
            for (var i = 1; i < lumieres.Count; i++) Assert.That(lumieres[i], Is.LessThan(lumieres[i - 1]));
            Assert.That(lumieres.All(l => l > 0 && l + RegistreDArt.LUMIERE_AMBIANTE <= 1.0), Is.True);
        }

        [Test, Description("une assise sans dessin emprunte le décor de la Noue, plus sombre, sans erreur")]
        public void Une_assise_sans_dessin_emprunte_la_Noue_assombrie()
        {
            var profonde = Assises.Toutes[2].Id;
            var decor = RegistreDArt.DecorDe(profonde);
            Assert.That(decor.Assise, Is.EqualTo(profonde));
            Assert.That(decor.Berge, Is.False);
            Assert.That(RegistreDArt.LumiereDuPalier(Assises.Toutes[2].IndexPremierPalier),
                Is.LessThan(RegistreDArt.LumiereDuPalier(Assises.Toutes[0].IndexPremierPalier + 5)));
            Assert.That(() => RegistreDArt.LumiereDuPalier(61), Throws.Nothing);
            Assert.That(RegistreDArt.MarqueDe(profonde), Is.Null);
            Assert.That(RegistreDArt.MarqueDe(null), Is.Null);
        }

        [Test, Description("chaque assise dessinée a sa marque et sa courbe de lumière")]
        public void Chaque_assise_dessinee_a_sa_marque_et_sa_lumiere()
        {
            Assert.That(RegistreDArt.ASSISES_DESSINEES, Is.EqualTo(new[] { "noue", "gour" }));
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                Assert.That(RegistreDArt.MarqueDe(assise), Is.Not.Null, assise);
                Assert.That(RegistreDArt.DecorDe(assise).Lumieres.Count,
                    Is.EqualTo(Assises.Toutes.First(a => a.Id == assise).NombreDePaliers), assise);
            }
        }

        /* ─── Les fichiers d'Art/ : ce qui est livré respecte son gabarit ──────────── */

        static Color32[] LirePng(string chemin, out int largeur, out int hauteur)
        {
            Assert.That(File.Exists(chemin), Is.True, chemin + " manque : lancer « IdlePond ▸ Générer les sprites provisoires »");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(chemin)), Is.True, chemin);
                largeur = texture.width;
                hauteur = texture.height;
                return texture.GetPixels32();
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static int Rgb(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        static string Art(string relatif) => Chemins.ART + "/" + relatif;

        [Test, Description("le corps : quatre images par stade, aux dimensions du gabarit")]
        public void Le_corps_a_ses_images_aux_dimensions_du_gabarit()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++)
                {
                    LirePng(Art(Chemins.CorpsDuHeros(stade, image)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDuCorps(stade)), $"stade {stade}, image {image}");
                }
        }

        [Test, Description("les ancrages sont peints, et du même pixel dans les quatre images : seul la queue bat")]
        public void Les_ancrages_ne_bougent_pas_d_une_image_a_l_autre()
        {
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                var images = Enumerable.Range(0, Gabarits.IMAGES_DE_NAGE)
                    .Select(i => LirePng(Art(Chemins.CorpsDuHeros(stade, i)), out var l, out _)).ToList();
                var largeur = Gabarits.CadreDuCorps(stade).Largeur;
                foreach (AncrageId ancrage in System.Enum.GetValues(typeof(AncrageId)))
                {
                    var p = Gabarits.Ancrage(stade, ancrage);
                    var pixels = images.Select(px => px[p.Y * largeur + p.X]).ToList();
                    Assert.That(pixels.All(c => c.a == 255), Is.True, $"stade {stade}, {ancrage} transparent");
                    Assert.That(pixels.Select(Rgb).Distinct().Count(), Is.EqualTo(1), $"stade {stade}, {ancrage} bouge");
                }
            }
        }

        [Test, Description("une marque a le cadre du corps et ne déborde jamais du corps")]
        public void Une_marque_reste_sur_le_corps()
        {
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
                for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
                {
                    var marque = LirePng(Art(Chemins.MarqueDAssise(assise, stade)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDuCorps(stade)), $"{assise}, stade {stade}");
                    var corps = LirePng(Art(Chemins.CorpsDuHeros(stade, 0)), out _, out _);
                    for (var i = 0; i < marque.Length; i++)
                        if (marque[i].a > 0) Assert.That(corps[i].a, Is.EqualTo(255), $"{assise}, stade {stade} : la marque déborde au pixel {i}");
                }
        }

        [Test, Description("chaque espèce livrée a ses deux images, aux dimensions de son rang")]
        public void Chaque_espece_livree_a_ses_images()
        {
            var livrees = Chemins.EspecesLivrees().ToList();
            Assert.That(livrees.Select(e => e.Id), Is.EqualTo(new[] { "vairon", "loche", "epinoche", "chabot", "lamproie", "ombre" }));
            foreach (var espece in livrees)
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++)
                {
                    LirePng(Art(Chemins.ImageDEspece(espece.Id, image)), out var l, out var h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(Gabarits.CadreDEspece(espece.Rang)), espece.Id);
                }
        }

        [Test, Description("chaque assise dessinée a son fond, ses rayons, et sa berge si elle en déclare une")]
        public void Chaque_assise_dessinee_a_son_decor()
        {
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                LirePng(Art(Chemins.Fond(assise)), out var l, out var h);
                Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE)));
                LirePng(Art(Chemins.Roche(assise)), out l, out h);
                Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE)), "la roche pave comme le fond");
                LirePng(Art(Chemins.Rayons(assise)), out l, out h);
                Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DES_RAYONS)));
                if (RegistreDArt.DecorDe(assise).Berge)
                {
                    LirePng(Art(Chemins.Berge(assise)), out l, out h);
                    Assert.That(new Cadre(l, h), Is.EqualTo(new Cadre(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE)));
                }
            }
        }

        [Test, Description("chaque pixel opaque d'Art/ appartient à la palette de sa famille, sans demi-transparence")]
        public void Chaque_pixel_appartient_a_sa_palette()
        {
            IReadOnlyList<int> PaletteDe(string relatif)
            {
                if (relatif.StartsWith("Heros/")) return RegistreDArt.PaletteDuHeros();
                if (relatif.StartsWith("Especes/")) return RegistreDArt.PaletteDesEspeces();
                if (relatif.StartsWith("Fonds/")) return RegistreDArt.PaletteDuDecor(relatif.Split('/')[1]);
                if (relatif.StartsWith("Eau/")) return new[] { RegistreDArt.BLANC };
                Assert.Fail("famille inconnue : " + relatif);
                return null;
            }
            var fichiers = Directory.GetFiles(Chemins.ART, "*.png", SearchOption.AllDirectories);
            Assert.That(fichiers, Is.Not.Empty);
            foreach (var fichier in fichiers)
            {
                var relatif = fichier.Replace('\\', '/').Substring(Chemins.ART.Length + 1);
                var palette = new HashSet<int>(PaletteDe(relatif));
                foreach (var c in LirePng(fichier, out _, out _))
                {
                    Assert.That(c.a == 0 || c.a == 255, Is.True, relatif + " : demi-transparence");
                    if (c.a == 255) Assert.That(palette.Contains(Rgb(c)), Is.True, $"{relatif} : #{Rgb(c):X6} hors palette");
                }
            }
        }

        [Test, Description("le catalogue référence chaque sprite livré")]
        public void Le_catalogue_reference_chaque_sprite_livre()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueDArt>(Chemins.CATALOGUE);
            Assert.That(catalogue, Is.Not.Null, Chemins.CATALOGUE);
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++) Assert.That(catalogue.CorpsDe(stade, image), Is.Not.Null);
                foreach (var assise in RegistreDArt.ASSISES_DESSINEES) Assert.That(catalogue.MarqueDe(assise, stade), Is.Not.Null);
            }
            foreach (var espece in Chemins.EspecesLivrees())
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++) Assert.That(catalogue.EspeceDe(espece.Id, image), Is.Not.Null);
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                Assert.That(catalogue.DecorDe(assise).Fond, Is.Not.Null);
                Assert.That(catalogue.DecorDe(assise).Roche, Is.Not.Null);
                Assert.That(catalogue.DecorDe(assise).Rayons, Is.Not.Null);
            }
            Assert.That(catalogue.Voile, Is.Not.Null);
            Assert.That(catalogue.Eclat, Is.Not.Null);
        }

        [Test, Description("un sprite d'Art/ est importé net : 1 px par unité, filtrage Point, sans compression")]
        public void Un_sprite_est_importe_net()
        {
            var importeur = (TextureImporter)AssetImporter.GetAtPath(Art(Chemins.CorpsDuHeros(0, 0)));
            Assert.That(importeur.spritePixelsPerUnit, Is.EqualTo(1f));
            Assert.That(importeur.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importeur.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importeur.mipmapEnabled, Is.False);
        }
    }
}
