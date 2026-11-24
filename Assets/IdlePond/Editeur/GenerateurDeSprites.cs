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
            // Le héros et les six tribus n'ont plus de provisoire : ils sont dessinés — le héros
            // par `outils/da/heros.py` (bible v0.4), les poissons de troupe par
            // `outils/da/especes.py` (Codex v1.4, maquette validée).
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                var decor = RegistreDArt.DecorDe(assise);
                sortie[Chemins.Fond(assise)] = DessinerFond(decor).EnPng();
                sortie[Chemins.Roche(assise)] = DessinerRoche(decor).EnPng();
                sortie[Chemins.Rayons(assise)] = DessinerRayons(decor).EnPng();
                if (decor.Berge) sortie[Chemins.Berge(assise)] = DessinerBerge(decor).EnPng();
                if (decor.AUneAmbiance) sortie[Chemins.Ambiance(assise)] = DessinerAmbiance(decor).EnPng();
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

        /* ─── Le décor ───────────────────────────────────────────────────────────── */

        /// Une bande d'eau qui se répète en largeur : l'eau, tramée du clair en haut au sombre en
        /// bas (bible v0.3), et son fond : la vase et ses herbes, ou le lit de galets et sa
        /// mousse. La lumière, elle, vient des Light2D. Tout ce qui s'y dessine a une période
        /// qui divise la largeur (48) : la tuile se raccorde à elle-même. Dessinée pour la scène de
        /// 360 px : chaque mesure est une fois et demie celle de 240.
        static Toile DessinerFond(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    // Des aplats, du clair en haut au sombre en bas ; seul le dernier quart de
                    // chaque aplat est tramé vers le suivant, pour que l'eau reste lisse.
                    var profondeur = (1 - y / (double)(toile.Hauteur - 1)) * (d.Eau.Count - 1);
                    var i = Math.Min((int)profondeur, d.Eau.Count - 1);
                    var passage = (profondeur - i - 0.75) / 0.25;
                    var couleur = i + 1 < d.Eau.Count && Toile.Seuil(x, y) < passage ? d.Eau[i + 1] : d.Eau[i];
                    if (d.Galets.Count == 0 && (y <= 2 || (y == 3 && x % 2 == 0))) couleur = d.Vase;
                    // Le courant du Gour : des traits d'écume qui filent, en pointillés décalés
                    // d'une rangée à l'autre.
                    if (d.Courant && y > 9 && y % 13 == 6 && (x + y * 3) % 24 < 10 && x % 2 == 0)
                        couleur = d.Ecume.Count > 0 ? d.Ecume[(y / 13) % d.Ecume.Count] : d.Rayon;
                    toile.Poser(x, y, couleur);
                }
            if (d.Galets.Count > 0) DessinerGalets(toile, d);
            if (d.Herbes.Count > 0) DessinerHerbes(toile, d);
            return toile;
        }

        /// Les herbes qui montent de la vase et ondulent.
        static void DessinerHerbes(Toile toile, DecorDArt d)
        {
            var pieds = new[] { 3, 12, 20, 28, 36, 44 };
            var hauteurs = new[] { 13, 21, 9, 16, 24, 12 };
            for (var k = 0; k < pieds.Length; k++)
                for (var i = 0; i < hauteurs[k]; i++)
                {
                    var x = pieds[k] + (int)Math.Round(Math.Sin(i * 0.3 + k * 1.7) * 1.8);
                    {
                        // Une herbe de deux pixels d'épaisseur sur sa moitié basse.
                        toile.Poser((x + toile.Largeur) % toile.Largeur, 3 + i, d.Herbes[k % d.Herbes.Count]);
                        if (i < hauteurs[k] / 2) toile.Poser((x + 1 + toile.Largeur) % toile.Largeur, 3 + i, d.Herbes[k % d.Herbes.Count]);
                    }
                }
        }

        /// Le lit de galets du Gour : des galets gris-bleu sur un fond sombre, de la mousse
        /// sur le dessus de certains.
        static void DessinerGalets(Toile toile, DecorDArt d)
        {
            var galets = new (double X, double Y, double Rx, double Ry, int Couleur)[]
            {
                (4.5, 3, 4.5, 3, 0), (13.5, 2.25, 3.75, 2.25, 1), (21.75, 3.75, 4.5, 3.3, 2),
                (30.75, 2.25, 3.75, 2.4, 0), (39, 3.75, 4.5, 3, 1), (45.75, 1.5, 3, 1.95, 2),
            };
            var fond = d.Galets[d.Galets.Count - 1];
            for (var y = 0; y <= 8; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var couleur = y <= 2 ? fond : (int?)null;
                    for (var g = 0; g < galets.Length; g++)
                    {
                        var dx = Math.Abs(x - galets[g].X);
                        dx = Math.Min(dx, toile.Largeur - dx) / galets[g].Rx;
                        var dy = (y - galets[g].Y) / galets[g].Ry;
                        if (dx * dx + dy * dy > 1) continue;
                        couleur = d.Galets[galets[g].Couleur];
                        // La mousse : sur le dessus d'un galet sur deux.
                        if (d.Mousse.HasValue && g % 2 == 0 && dy > 0.5 && Toile.Seuil(x, y) < 0.6) couleur = d.Mousse.Value;
                    }
                    if (couleur.HasValue) toile.Poser(x, y, couleur.Value);
                }
        }

        /// <summary>
        /// La roche d'un palier pas encore creusé : des pierres irrégulières (une cellule de
        /// Voronoï chacune, autour d'un germe tiré au hasard dans une grille de 12 × 12), jointes
        /// par des fissures, éclairées d'une arête claire sur leur bord haut ; en haut de la
        /// bande, le seuil, la ligne sombre où l'eau creusée s'arrête. Les distances se
        /// mesurent en tournant sur la largeur : la tuile se raccorde à elle-même quand elle
        /// pave. Dessinée dans ses couleurs ; c'est l'absence de lumière qui la plonge dans
        /// l'obscurité.
        /// </summary>
        static Toile DessinerRoche(DecorDArt d)
        {
            const int CASE = 12;
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
                    if (ecart < 1.35) couleur = d.Roche[3];
                    else
                    {
                        couleur = germes[pierre].Claire ? d.Roche[1] : d.Roche[0];
                        // L'arête claire : le bord haut de la pierre, juste sous la fissure.
                        if (Pierre(x, y + 1).Ecart < 1.35 && Toile.Seuil(x, y) < 0.7) couleur = d.Roche[2];
                        // Le grain, tramé, pour que la pierre ne soit pas lisse.
                        else if (couleur == d.Roche[1] && Toile.Seuil(x, y) < 0.1) couleur = d.Roche[0];
                    }
                    if (y >= toile.Hauteur - 3) couleur = d.Roche[3];
                    toile.Poser(x, y, couleur);
                }
            return toile;
        }

        /// Les rayons du jour sur les deux premières bandes : trois rais étroits et obliques,
        /// tramés, qui s'effacent en descendant. Des pixels pleins, jamais de demi-transparence.
        static Toile DessinerRayons(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DES_RAYONS);
            var departs = new[] { 0.18, 0.47, 0.78 };
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var haut = y / (double)(toile.Hauteur - 1);
                    var u = x / (double)(toile.Largeur - 1);
                    var force = 0.0;
                    foreach (var depart in departs)
                        force = Math.Max(force, 1 - Math.Abs(u - (depart + (1 - haut) * 0.12)) * 22);
                    if (force * haut * 0.6 > Toile.Seuil(x, y)) toile.Poser(x, y, d.Rayon);
                }
            return toile;
        }

        /// Un hasard déterministe : deux passages tirent les mêmes nombres.
        static Func<double> Hasard(uint graine) => () =>
        {
            graine = graine * 1664525u + 1013904223u;
            return (graine >> 8) / 16777216.0;
        };

        /// La berge de la première bande : la ligne de la surface, les nénuphars qui s'y posent,
        /// et les racines qui pendent et bourgeonnent — l'inversion d'échelle (GDD §15.2).
        static Toile DessinerBerge(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE);
            var hasard = Hasard(7);
            var haut = toile.Hauteur - 1;
            const int RACINES = 18;
            for (var k = 0; k < RACINES; k++)
            {
                var x = (k + 0.5) * toile.Largeur / RACINES + (hasard() - 0.5) * 9;
                var longueur = (int)(toile.Hauteur * (0.35 + hasard() * 0.5));
                var epaisseur = hasard() < 0.4 ? 3 : 2;
                for (var i = 0; i < longueur; i++)
                {
                    x += (hasard() - 0.5) * 1.35;
                    // Le bout de la racine bourgeonne : la Sève y monte.
                    var bourgeon = d.Bourgeon.HasValue && i >= longueur - 3;
                    for (var e = 0; e < epaisseur; e++)
                        toile.Poser((int)Math.Round(x) + e, haut - 1 - i,
                            bourgeon ? d.Bourgeon.Value : d.Racines[e % d.Racines.Count]);
                }
            }
            if (d.Surface.HasValue)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    toile.Poser(x, haut, d.Surface.Value);
                    // La surface a deux pixels ; le second, tramé, se mêle à l'eau.
                    if (Toile.Seuil(x, haut - 1) < 0.5 && toile.Lire(x, haut - 1) == null) toile.Poser(x, haut - 1, d.Surface.Value);
                }
            if (d.Nenuphar.Count >= 4)
            {
                // Trois feuilles à la surface ; une seule a sa fleur.
                var feuilles = new[] { (X: 51, Demi: 12, Fleur: false), (X: 180, Demi: 10, Fleur: true), (X: 307, Demi: 13, Fleur: false) };
                foreach (var f in feuilles)
                {
                    for (var x = f.X - f.Demi; x <= f.X + f.Demi; x++)
                    {
                        toile.Poser(x, haut, d.Nenuphar[1]);
                        if (Math.Abs(x - f.X) < f.Demi) toile.Poser(x, haut - 1, d.Nenuphar[0]);
                        if (Math.Abs(x - f.X) < f.Demi - 3) toile.Poser(x, haut - 2, d.Nenuphar[0]);
                    }
                    // L'échancrure de la feuille.
                    toile.Poser(f.X + 3, haut, d.Surface ?? d.Nenuphar[0]);
                    toile.Poser(f.X + 3, haut - 1, d.Surface ?? d.Nenuphar[0]);
                    if (!f.Fleur) continue;
                    // La fleur : des pétales autour d'un cœur d'or, posée sur la feuille.
                    foreach (var dx in new[] { -3, -2, -1, 1, 2, 3 }) toile.Poser(f.X + dx, haut, d.Nenuphar[2]);
                    foreach (var dx in new[] { -2, -1, 0, 1, 2 }) toile.Poser(f.X + dx, haut - 1, d.Nenuphar[2]);
                    toile.Poser(f.X, haut, d.Nenuphar[3]);
                    toile.Poser(f.X, haut - 1, d.Nenuphar[3]);
                    toile.Poser(f.X - 1, haut - 2, d.Nenuphar[2]);
                    toile.Poser(f.X + 1, haut - 2, d.Nenuphar[2]);
                }
            }
            return toile;
        }

        /// Ce qui flotte dans chaque bande, sur 384 px pour ne pas se répéter tous les 48 :
        /// le pollen de la Sève ; les méduses et les grosses pierres du Gour.
        static Toile DessinerAmbiance(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE);
            var hasard = Hasard(11);
            if (d.Grains.Count > 0)
                for (var k = 0; k < 60; k++)
                {
                    var x = (int)(hasard() * toile.Largeur);
                    var y = 9 + (int)(hasard() * (toile.Hauteur - 15));
                    toile.Poser(x, y, d.Grains[k % d.Grains.Count]);
                }
            if (d.Pierres.Count > 0)
                foreach (var (cx, rx, ry) in new[] { (105.0, 15.0, 9.0), (294.0, 18.0, 10.5) })
                    for (var y = 0; y <= (int)ry + 3; y++)
                        for (var x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                        {
                            var u = (x - cx) / rx;
                            var v = (y - 3) / ry;
                            if (u * u + v * v > 1) continue;
                            var couleur = v > 0.45 ? d.Pierres[1] : d.Pierres[0];
                            if (d.Mousse.HasValue && v > 0.75 && Toile.Seuil(x, y) < 0.5) couleur = d.Mousse.Value;
                            toile.Poser(x, y, couleur);
                        }
            if (d.Meduses.Count > 0)
                foreach (var (cx, cy) in new[] { (60, 60), (225, 45), (333, 66) })
                {
                    // Une cloche de sept pixels, son bord, et ses filaments qui pendent.
                    for (var dx = -1; dx <= 1; dx++) toile.Poser(cx + dx, cy + 2, d.Meduses[0]);
                    for (var dx = -2; dx <= 2; dx++) toile.Poser(cx + dx, cy + 1, d.Meduses[0]);
                    for (var dx = -3; dx <= 3; dx++) toile.Poser(cx + dx, cy, d.Meduses[0]);
                    toile.Poser(cx - 3, cy - 1, d.Meduses[1]);
                    toile.Poser(cx + 3, cy - 1, d.Meduses[1]);
                    for (var i = 1; i <= 5; i++)
                    {
                        toile.Poser(cx - 1 + (i % 2), cy - i, d.Meduses[1]);
                        if (i < 4) toile.Poser(cx + 2 - (i % 2), cy - i, d.Meduses[1]);
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
