using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    /// <summary>
    /// IdlePond — les sprites provisoires (spec DA §5), dessinés par code : formes, contour
    /// d'un pixel, tramage ordonné, couleurs du registre d'art. Déterministe : deux passages
    /// rendent les mêmes octets. Il ne remplace JAMAIS un vrai dessin : chaque fichier qu'il
    /// écrit porte dans son `.meta` l'empreinte de ce qu'il a écrit ; un fichier dont le
    /// contenu ne correspond plus à cette empreinte est un dessin déposé, et il est protégé.
    ///
    /// Menu « IdlePond ▸ Générer les sprites provisoires », ou en batch :
    /// `outils/unity.sh methode IdlePond.Editeur.GenerateurDeSprites.Generer`.
    /// </summary>
    public static class GenerateurDeSprites
    {
        public const string PREFIXE = "idlepond:provisoire:";

        [MenuItem("IdlePond/Générer les sprites provisoires")]
        public static void Generer()
        {
            try
            {
                Ecrire(Produire());
                Catalogue.Reconstruire();
                AssetDatabase.SaveAssets();
                Debug.Log("IdlePond : sprites provisoires générés.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        public static string Etiquette(byte[] octets)
        {
            using (var sha = SHA1.Create())
                return PREFIXE + BitConverter.ToString(sha.ComputeHash(octets)).Replace("-", "");
        }

        public static bool PeutEcrire(bool existe, string userData, byte[] actuel) =>
            !existe || (userData != null && actuel != null && userData == Etiquette(actuel));

        /// Chemin relatif à `Art/` → octets PNG.
        public static SortedDictionary<string, byte[]> Produire()
        {
            var sortie = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
            for (var stade = 0; stade < Gabarits.LONGUEURS_DU_CORPS.Count; stade++)
            {
                for (var image = 0; image < Gabarits.IMAGES_DE_NAGE; image++)
                    sortie[Chemins.CorpsDuHeros(stade, image)] = DessinerCorps(stade, image).EnPng();
                foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
                    sortie[Chemins.MarqueDAssise(assise, stade)] = DessinerMarque(assise, stade).EnPng();
            }
            foreach (var espece in Chemins.EspecesLivrees())
                for (var image = 0; image < Gabarits.IMAGES_D_ESPECE; image++)
                    sortie[Chemins.ImageDEspece(espece.Id, image)] = DessinerEspece(espece, image).EnPng();
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                var decor = RegistreDArt.DecorDe(assise);
                sortie[Chemins.Fond(assise)] = DessinerFond(decor).EnPng();
                sortie[Chemins.Roche(assise)] = DessinerRoche(decor).EnPng();
                sortie[Chemins.Rayons(assise)] = DessinerRayons(decor).EnPng();
                if (decor.Berge) sortie[Chemins.Berge(assise)] = DessinerBerge(decor).EnPng();
            }
            sortie[Chemins.VOILE] = DessinerVoile().EnPng();
            sortie[Chemins.ECLAT] = DessinerEclat().EnPng();
            return sortie;
        }

        static void Ecrire(SortedDictionary<string, byte[]> fichiers)
        {
            foreach (var fichier in fichiers)
            {
                var chemin = Chemins.ART + "/" + fichier.Key;
                var existe = File.Exists(chemin);
                var actuel = existe ? File.ReadAllBytes(chemin) : null;
                var importeur = existe ? AssetImporter.GetAtPath(chemin) : null;
                if (!PeutEcrire(existe, importeur?.userData, actuel))
                {
                    Debug.Log($"IdlePond : {chemin} est un vrai dessin, laissé tel quel.");
                    continue;
                }
                if (existe && actuel.SequenceEqual(fichier.Value)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(chemin));
                File.WriteAllBytes(chemin, fichier.Value);
                AssetDatabase.ImportAsset(chemin, ImportAssetOptions.ForceUpdate);
                importeur = AssetImporter.GetAtPath(chemin);
                importeur.userData = Etiquette(fichier.Value);
                importeur.SaveAndReimport();
            }
        }

        /* ─── Les poissons ───────────────────────────────────────────────────────── */

        /// Le battement de la queue, image par image : droite, haut, droite, bas.
        static readonly int[] BATTEMENTS = { 0, 1, 0, -1 };

        static Toile DessinerCorps(int stade, int image) =>
            DessinerPoisson(Gabarits.LONGUEURS_DU_CORPS[stade], RegistreDArt.HEROS, BATTEMENTS[image]);

        static Toile DessinerEspece(Espece espece, int image) =>
            DessinerPoisson(Gabarits.LongueurDEspece(espece.Rang), RegistreDArt.CouleursDEspece(espece.Rang), image == 0 ? 1 : -1);

        /// Un poisson qui regarde à droite : un tronc elliptique (dos sombre, flanc, ventre
        /// clair), une queue en éventail échancré qui bat d'un pixel, un œil, un contour.
        static Toile DessinerPoisson(int longueur, CouleursDePoisson c, int battement)
        {
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var toile = new Toile(cadre.Largeur, cadre.Hauteur);
            var naissance = t.Cx - 0.85 * t.A;
            var queue = 0.3 * longueur;
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var dy = y - t.Cy;
                    if (t.Contient(x, y))
                    {
                        toile.Poser(x, y, dy > 0.25 * t.B ? c.Dos : dy < -0.3 * t.B ? c.Ventre : c.Corps);
                        continue;
                    }
                    var q = naissance - x;
                    if (q <= 0 || q > queue) continue;
                    var decale = dy - battement * (q / queue);
                    var ouverture = 0.2 * t.B + q * 0.55;
                    var echancrure = q > 0.6 * queue && Math.Abs(decale) < q * 0.35;
                    if (Math.Abs(decale) <= ouverture && !echancrure) toile.Poser(x, y, c.Dos);
                }
            var oeilX = (int)Math.Round(t.Cx + 0.28 * longueur);
            var oeilY = (int)Math.Round(t.Cy + 0.25 * t.B);
            toile.Poser(oeilX, oeilY, RegistreDArt.CONTOUR);
            if (longueur > 18) toile.Poser(oeilX - 1, oeilY, RegistreDArt.CONTOUR);
            toile.Contourner(RegistreDArt.CONTOUR);
            return toile;
        }

        /// Une marque, dans le cadre du corps de son stade, seulement sur le tronc.
        static Toile DessinerMarque(string assise, int stade)
        {
            var marque = RegistreDArt.MarqueDe(assise);
            var longueur = Gabarits.LONGUEURS_DU_CORPS[stade];
            var cadre = Gabarits.CadreDuCorps(stade);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var toile = new Toile(cadre.Largeur, cadre.Hauteur);
            var a = Gabarits.Ancrage(stade, marque.Ancrage);
            switch (marque.Ancrage)
            {
                case AncrageId.Branchies:
                    // Une fente par stade : les branchies se creusent à mesure qu'il grandit.
                    var demi = (int)Math.Round(0.55 * t.B);
                    for (var fente = 0; fente <= stade; fente++)
                    {
                        var x = a.X - 2 * fente;
                        for (var y = a.Y - demi; y <= a.Y + demi; y++)
                            if (t.Contient(x, y)) toile.Poser(x, y, marque.Couleurs[0]);
                    }
                    break;
                case AncrageId.Dos:
                    // Les membranes du Gour : une crête membraneuse le long du dos, plus longue
                    // et plus épaisse à chaque stade. Elle reste DANS la silhouette (une marque
                    // reste sur le corps, spec DA §3) : elle descend du haut du dos, en dents.
                    var demiLongueur = (int)Math.Round((0.18 + 0.05 * stade) * longueur);
                    var hauteur = 1 + (stade + 1) / 2;
                    for (var x = a.X - demiLongueur; x <= a.X + demiLongueur; x++)
                    {
                        var dessus = -1;
                        for (var y = toile.Hauteur - 1; y >= 0; y--)
                            if (t.Contient(x, y)) { dessus = y; break; }
                        if (dessus < 0) continue;
                        // Une crête en dents : plus haute au milieu, une dent sur deux.
                        var milieu = 1 - Math.Abs(x - a.X) / (double)(demiLongueur + 1);
                        var haut = (int)Math.Round(hauteur * milieu) + ((x & 1) == 0 ? 1 : 0);
                        for (var y = dessus; y >= dessus - haut && t.Contient(x, y); y--)
                            toile.Poser(x, y, y == dessus - haut ? marque.Couleurs[1] : marque.Couleurs[0]);
                    }
                    break;
                default:
                    throw new NotSupportedException($"aucun provisoire pour une marque à l'ancrage {marque.Ancrage} ({assise})");
            }
            return toile;
        }

        /* ─── Le décor ───────────────────────────────────────────────────────────── */

        /// Une bande d'eau qui se répète en largeur : l'eau claire, tramée vers le sombre en
        /// bas, et un liseré de vase. La lumière, elle, vient des Light2D.
        static Toile DessinerFond(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var t = y / (double)(toile.Hauteur - 1);
                    var couleur = d.Eau[0];
                    if (t < 0.35 && Toile.Seuil(x, y) < (0.35 - t) / 0.35) couleur = d.Eau[1];
                    if (t < 0.12 && Toile.Seuil(x, y) < (0.12 - t) / 0.12) couleur = d.Eau[2];
                    if (y <= 1 || (y == 2 && x % 2 == 0)) couleur = d.Vase;
                    // Le courant du Gour : des traits d'eau qui file, en tirets décalés d'une
                    // rangée à l'autre ; la période (16) divise la largeur (32), la tuile se raccorde.
                    if (d.Courant && y > 4 && y % 9 == 4 && (x + y * 3) % 16 < 6) couleur = d.Rayon;
                    toile.Poser(x, y, couleur);
                }
            return toile;
        }

        /// <summary>
        /// La roche d'un palier pas encore creusé : des pierres irrégulières (une cellule de
        /// Voronoï chacune, autour d'un germe tiré au hasard dans une grille de 8 × 8), jointes
        /// par des fissures, éclairées d'une arête claire sur leur bord haut ; en haut de la
        /// bande, le seuil, la ligne sombre où l'eau creusée s'arrête. Les distances se
        /// mesurent en tournant sur la largeur : la tuile se raccorde à elle-même quand elle
        /// pave. Dessinée dans ses couleurs ; c'est l'absence de lumière qui la plonge dans
        /// l'obscurité.
        /// </summary>
        static Toile DessinerRoche(DecorDArt d)
        {
            const int CASE = 8;
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            var colonnes = toile.Largeur / CASE;
            var rangees = (toile.Hauteur + CASE - 1) / CASE;
            uint Hacher(int a, int b)
            {
                var h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return h;
            }
            var germes = new List<(double X, double Y, bool Claire)>();
            for (var r = 0; r < rangees; r++)
                for (var c = 0; c < colonnes; c++)
                {
                    var h = Hacher(c, r);
                    germes.Add((c * CASE + 1 + (h & 0xFF) / 255.0 * (CASE - 2),
                        r * CASE + 1 + ((h >> 8) & 0xFF) / 255.0 * (CASE - 2), (h >> 16 & 3) != 0));
                }
            (int Plus, double Ecart) Pierre(int x, int y)
            {
                double premiere = double.MaxValue, seconde = double.MaxValue;
                var plus = 0;
                for (var g = 0; g < germes.Count; g++)
                {
                    var dx = Math.Abs(x - germes[g].X);
                    dx = Math.Min(dx, toile.Largeur - dx) * 0.8;
                    var dy = y - germes[g].Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance < premiere) { seconde = premiere; premiere = distance; plus = g; }
                    else if (distance < seconde) seconde = distance;
                }
                return (plus, seconde - premiere);
            }
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var (pierre, ecart) = Pierre(x, y);
                    int couleur;
                    if (ecart < 0.9) couleur = d.Roche[3];
                    else
                    {
                        couleur = germes[pierre].Claire ? d.Roche[1] : d.Roche[0];
                        // L'arête claire : le bord haut de la pierre, juste sous la fissure.
                        if (Pierre(x, y + 1).Ecart < 0.9 && Toile.Seuil(x, y) < 0.7) couleur = d.Roche[2];
                        // Le grain, tramé, pour que la pierre ne soit pas lisse.
                        else if (couleur == d.Roche[1] && Toile.Seuil(x, y) < 0.1) couleur = d.Roche[0];
                    }
                    if (y >= toile.Hauteur - 2) couleur = d.Roche[3];
                    toile.Poser(x, y, couleur);
                }
            return toile;
        }

        /// Les rayons du jour sur les deux premières bandes, tramés : des pixels pleins, jamais
        /// de demi-transparence.
        static Toile DessinerRayons(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DES_RAYONS);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var haut = y / (double)(toile.Hauteur - 1);
                    var centre = 0.3 + (1 - haut) * 0.2;
                    var force = Math.Max(0, 1 - Math.Abs(x / (double)(toile.Largeur - 1) - centre) * 3.2) * haut;
                    if (force * 0.45 > Toile.Seuil(x, y)) toile.Poser(x, y, d.Rayon);
                }
            return toile;
        }

        /// Les racines de la berge, qui pendent du haut : l'inversion d'échelle (GDD §15.2).
        static Toile DessinerBerge(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE);
            uint graine = 7;
            double Hasard()
            {
                graine = graine * 1664525u + 1013904223u;
                return (graine >> 8) / 16777216.0;
            }
            const int RACINES = 12;
            for (var k = 0; k < RACINES; k++)
            {
                var x = (k + 0.5) * toile.Largeur / RACINES + (Hasard() - 0.5) * 6;
                var longueur = (int)(toile.Hauteur * (0.35 + Hasard() * 0.5));
                var epaisseur = Hasard() < 0.4 ? 2 : 1;
                for (var i = 0; i < longueur; i++)
                {
                    x += (Hasard() - 0.5) * 0.9;
                    for (var e = 0; e < epaisseur; e++)
                        toile.Poser((int)Math.Round(x) + e, toile.Hauteur - 1 - i, e == 0 ? d.Racines[1] : d.Racines[0]);
                }
            }
            return toile;
        }

        /// Le voile de l'eau trouble : un damier, teinté et dosé au rendu.
        static Toile DessinerVoile()
        {
            var toile = new Toile(Gabarits.COTE_DU_VOILE, Gabarits.COTE_DU_VOILE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                    if ((x + y) % 2 == 0) toile.Poser(x, y, RegistreDArt.BLANC);
            return toile;
        }

        /// Un éclat de mue : un carré de 2 px.
        static Toile DessinerEclat()
        {
            var toile = new Toile(2, 2);
            for (var y = 0; y < 2; y++)
                for (var x = 0; x < 2; x++)
                    toile.Poser(x, y, RegistreDArt.BLANC);
            return toile;
        }
    }
}
