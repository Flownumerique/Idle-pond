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

        /* ─── Les poissons ───────────────────────────────────────────────────────── */

        /// Le battement de la queue, image par image : droite, haut, droite, bas.
        static readonly int[] BATTEMENTS = { 0, 1, 0, -1 };

        static Toile DessinerCorps(int stade, int image) =>
            DessinerPoisson(Gabarits.LONGUEURS_DU_CORPS[stade], RegistreDArt.HEROS, BATTEMENTS[image], image);

        static Toile DessinerEspece(Espece espece, int image) =>
            DessinerPoisson(Gabarits.LongueurDEspece(espece.Rang), RegistreDArt.CouleursDEspece(espece.Rang), image == 0 ? 1 : -1, image);

        /// Un poisson qui regarde à droite : un tronc elliptique (dos sombre, flanc, ventre
        /// clair), une queue en éventail échancré qui bat d'un pixel, un œil, un contour. Une
        /// espèce porte son accent sur quelques pixels ; le héros a ses nageoires, son reflet
        /// et sa joue.
        static Toile DessinerPoisson(int longueur, CouleursDePoisson c, int battement, int image)
        {
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var toile = new Toile(cadre.Largeur, cadre.Hauteur);
            var naissance = t.Cx - 0.85 * t.A;
            var queue = 0.3 * longueur;
            var nageoire = c.Nageoire ?? c.Dos;
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
                    if (Math.Abs(decale) > ouverture || echancrure) continue;
                    // La queue-feuille du vairon : le bout de la queue prend l'accent.
                    var bout = c.Pose == PoseDAccent.Queue && c.Accent.HasValue && q > 0.6 * queue;
                    toile.Poser(x, y, bout ? c.Accent.Value : nageoire);
                }
            var oeilX = (int)Math.Round(t.Cx + 0.28 * longueur);
            var oeilY = (int)Math.Round(t.Cy + 0.25 * t.B);
            if (c.Reflet.HasValue)
            {
                // Le reflet : une ligne claire sous le dos, de la naissance de la queue aux branchies.
                var ligne = (int)Math.Floor(t.Cy + 0.25 * t.B);
                for (var x = (int)Math.Round(t.Cx - 0.6 * t.A); x <= (int)Math.Round(t.Cx + 0.1 * longueur); x++)
                    if (toile.Lire(x, ligne) == c.Corps) toile.Poser(x, ligne, c.Reflet.Value);
            }
            if (c.Nageoire.HasValue)
                foreach (var p in Nageoires(longueur))
                    toile.Poser(p.X, p.Y, p.Part == PartDeNageoire.Rayon ? c.Dos : c.Nageoire.Value);
            if (c.Joue.HasValue)
                for (var x = oeilX - (longueur > 18 ? 1 : 0); x <= oeilX; x++)
                    if (t.Contient(x, oeilY - 2)) toile.Poser(x, oeilY - 2, c.Joue.Value);
            PoserLAccent(toile, t, longueur, c, image);
            var oeil = c.Oeil ?? RegistreDArt.CONTOUR;
            toile.Poser(oeilX, oeilY, oeil);
            if (longueur > 18) toile.Poser(oeilX - 1, oeilY, RegistreDArt.CONTOUR);
            toile.Contourner(RegistreDArt.CONTOUR);
            return toile;
        }

        /// Le plus haut (ou le plus bas) pixel du tronc dans une colonne, ou -1.
        static int BordDuTronc(Gabarits.Tronc t, int x, int hauteur, bool dessus)
        {
            if (dessus) { for (var y = hauteur - 1; y >= 0; y--) if (t.Contient(x, y)) return y; }
            else { for (var y = 0; y < hauteur; y++) if (t.Contient(x, y)) return y; }
            return -1;
        }

        /// L'accent d'une espèce (Codex v1.4, §2) : une couleur, sur quelques pixels.
        static void PoserLAccent(Toile toile, Gabarits.Tronc t, int longueur, CouleursDePoisson c, int image)
        {
            if (!c.Accent.HasValue) return;
            var accent = image % 2 == 1 && c.AccentDeLAutreImage.HasValue ? c.AccentDeLAutreImage.Value : c.Accent.Value;
            var milieu = (int)Math.Round(t.Cy);
            int X(double f) => (int)Math.Round(t.Cx + f * longueur);
            switch (c.Pose)
            {
                case PoseDAccent.Flanc:
                    // Les runes de la loche : deux points sur le flanc.
                    toile.Poser(X(-0.15), milieu, accent);
                    toile.Poser(X(0.05), milieu, accent);
                    break;
                case PoseDAccent.Epines:
                    // Les épines de l'épinoche : trois pointes au-dessus du dos.
                    foreach (var x in new[] { X(-0.2), X(0), X(0.15) })
                    {
                        var dessus = BordDuTronc(t, x, toile.Hauteur, true);
                        if (dessus >= 0 && dessus + 1 <= toile.Hauteur - 2) toile.Poser(x, dessus + 1, accent);
                    }
                    break;
                case PoseDAccent.Taches:
                    // Le lichen du chabot : deux taches sur le dos.
                    foreach (var x in new[] { X(-0.15), X(0.1) })
                    {
                        var dessus = BordDuTronc(t, x, toile.Hauteur, true);
                        if (dessus >= 0) toile.Poser(x, dessus, accent);
                    }
                    break;
                case PoseDAccent.Bouche:
                    // L'anneau de la lamproie : le bout du museau.
                    for (var x = toile.Largeur - 1; x >= 0; x--)
                        if (t.Contient(x, milieu)) { toile.Poser(x, milieu, accent); break; }
                    break;
                case PoseDAccent.Voile:
                    // Le voile de l'ombre : une haute dorsale, d'une couleur par image.
                    for (var x = X(-0.25); x <= X(0.1); x++)
                    {
                        var dessus = BordDuTronc(t, x, toile.Hauteur, true);
                        if (dessus < 0) continue;
                        for (var y = dessus + 1; y <= Math.Min(dessus + 2, toile.Hauteur - 2); y++) toile.Poser(x, y, accent);
                    }
                    break;
            }
        }

        enum PartDeNageoire { Rayon, Membrane, Toile }

        /// Les nageoires du dos et du ventre du héros, hors du tronc et immobiles : seule la
        /// queue bat. Un rayon toutes les trois colonnes ; entre deux rayons, une colonne de
        /// membrane (où le Gour pose son bleu) et une de toile (qui reste orange).
        static IEnumerable<(int X, int Y, PartDeNageoire Part)> Nageoires(int longueur)
        {
            var cadre = Gabarits.CadreDUnPoisson(longueur);
            var t = Gabarits.TroncDUnPoisson(longueur);
            var hauteur = longueur >= 40 ? 3 : 2;
            PartDeNageoire Part(int rang) => rang % 3 == 0 ? PartDeNageoire.Rayon : rang % 3 == 1 ? PartDeNageoire.Membrane : PartDeNageoire.Toile;
            // La dorsale : plus haute à l'avant, elle s'abaisse vers la queue.
            int debut = (int)Math.Round(t.Cx - 0.14 * longueur), fin = (int)Math.Round(t.Cx + 0.1 * longueur);
            for (var x = debut; x <= fin; x++)
            {
                var dessus = BordDuTronc(t, x, cadre.Hauteur, true);
                if (dessus < 0) continue;
                var h = x - debut < (fin - debut) / 3 ? hauteur - 1 : hauteur;
                for (var k = 1; k <= h && dessus + k <= cadre.Hauteur - 2; k++) yield return (x, dessus + k, Part(fin - x));
            }
            // La ventrale, plus petite, sous le milieu du ventre.
            debut = (int)Math.Round(t.Cx - 0.02 * longueur);
            fin = (int)Math.Round(t.Cx + 0.1 * longueur);
            for (var x = debut; x <= fin; x++)
            {
                var dessous = BordDuTronc(t, x, cadre.Hauteur, false);
                if (dessous < 0) continue;
                for (var k = 1; k <= hauteur - 1 && dessous - k >= 1; k++) yield return (x, dessous - k, Part(fin - x));
            }
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
                    // Les membranes du Gour (bible v0.3) : le bleu du Courant tendu entre les
                    // rayons des nageoires du dos et du ventre, une colonne sur trois ; le reste
                    // de la nageoire garde son orange. Les nageoires ne battent pas : la marque
                    // reste sur le corps dans les quatre images (spec DA §3).
                    foreach (var p in Nageoires(longueur))
                        if (p.Part == PartDeNageoire.Membrane) toile.Poser(p.X, p.Y, marque.Couleurs[0]);
                    break;
                default:
                    throw new NotSupportedException($"aucun provisoire pour une marque à l'ancrage {marque.Ancrage} ({assise})");
            }
            return toile;
        }

        /* ─── Le décor ───────────────────────────────────────────────────────────── */

        /// Une bande d'eau qui se répète en largeur : l'eau, tramée du clair en haut au sombre en
        /// bas (bible v0.3), et son fond : la vase et ses herbes, ou le lit de galets et sa
        /// mousse. La lumière, elle, vient des Light2D. Tout ce qui s'y dessine a une période
        /// qui divise la largeur (32) : la tuile se raccorde à elle-même.
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
                    if (d.Galets.Count == 0 && (y <= 1 || (y == 2 && x % 2 == 0))) couleur = d.Vase;
                    // Le courant du Gour : des traits d'écume qui filent, en pointillés décalés
                    // d'une rangée à l'autre.
                    if (d.Courant && y > 6 && y % 9 == 4 && (x + y * 3) % 16 < 7 && x % 2 == 0)
                        couleur = d.Ecume.Count > 0 ? d.Ecume[(y / 9) % d.Ecume.Count] : d.Rayon;
                    toile.Poser(x, y, couleur);
                }
            if (d.Galets.Count > 0) DessinerGalets(toile, d);
            if (d.Herbes.Count > 0) DessinerHerbes(toile, d);
            return toile;
        }

        /// Les herbes qui montent de la vase et ondulent.
        static void DessinerHerbes(Toile toile, DecorDArt d)
        {
            var pieds = new[] { 2, 8, 13, 19, 24, 29 };
            var hauteurs = new[] { 9, 14, 6, 11, 16, 8 };
            for (var k = 0; k < pieds.Length; k++)
                for (var i = 0; i < hauteurs[k]; i++)
                {
                    var x = pieds[k] + (int)Math.Round(Math.Sin(i * 0.45 + k * 1.7) * 1.2);
                    toile.Poser((x + toile.Largeur) % toile.Largeur, 2 + i, d.Herbes[k % d.Herbes.Count]);
                }
        }

        /// Le lit de galets du Gour : des galets gris-bleu sur un fond sombre, de la mousse
        /// sur le dessus de certains.
        static void DessinerGalets(Toile toile, DecorDArt d)
        {
            var galets = new (double X, double Y, double Rx, double Ry, int Couleur)[]
            {
                (3, 2, 3, 2, 0), (9, 1.5, 2.5, 1.5, 1), (14.5, 2.5, 3, 2.2, 2),
                (20.5, 1.5, 2.5, 1.6, 0), (26, 2.5, 3, 2, 1), (30.5, 1, 2, 1.3, 2),
            };
            var fond = d.Galets[d.Galets.Count - 1];
            for (var y = 0; y <= 5; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var couleur = y <= 1 ? fond : (int?)null;
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
            const int RACINES = 12;
            for (var k = 0; k < RACINES; k++)
            {
                var x = (k + 0.5) * toile.Largeur / RACINES + (hasard() - 0.5) * 6;
                var longueur = (int)(toile.Hauteur * (0.35 + hasard() * 0.5));
                var epaisseur = hasard() < 0.4 ? 2 : 1;
                for (var i = 0; i < longueur; i++)
                {
                    x += (hasard() - 0.5) * 0.9;
                    // Le bout de la racine bourgeonne : la Sève y monte.
                    var bourgeon = d.Bourgeon.HasValue && i >= longueur - 2;
                    for (var e = 0; e < epaisseur; e++)
                        toile.Poser((int)Math.Round(x) + e, haut - 1 - i,
                            bourgeon ? d.Bourgeon.Value : d.Racines[e % d.Racines.Count]);
                }
            }
            if (d.Surface.HasValue)
                for (var x = 0; x < toile.Largeur; x++) toile.Poser(x, haut, d.Surface.Value);
            if (d.Nenuphar.Count >= 4)
            {
                // Trois feuilles à la surface ; une seule a sa fleur.
                var feuilles = new[] { (X: 34, Demi: 8, Fleur: false), (X: 120, Demi: 7, Fleur: true), (X: 205, Demi: 9, Fleur: false) };
                foreach (var f in feuilles)
                {
                    for (var x = f.X - f.Demi; x <= f.X + f.Demi; x++)
                    {
                        toile.Poser(x, haut, d.Nenuphar[1]);
                        if (Math.Abs(x - f.X) < f.Demi) toile.Poser(x, haut - 1, d.Nenuphar[0]);
                    }
                    // L'échancrure de la feuille.
                    toile.Poser(f.X + 2, haut, d.Surface ?? d.Nenuphar[0]);
                    if (!f.Fleur) continue;
                    foreach (var dx in new[] { -2, -1, 1, 2 }) toile.Poser(f.X + dx, haut, d.Nenuphar[2]);
                    toile.Poser(f.X, haut, d.Nenuphar[3]);
                    toile.Poser(f.X - 1, haut - 1, d.Nenuphar[2]);
                    toile.Poser(f.X + 1, haut - 1, d.Nenuphar[2]);
                }
            }
            return toile;
        }

        /// Ce qui flotte dans chaque bande, sur 256 px pour ne pas se répéter tous les 32 :
        /// le pollen de la Sève ; les méduses et les grosses pierres du Gour.
        static Toile DessinerAmbiance(DecorDArt d)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_BERGE, Gabarits.HAUTEUR_DE_BANDE);
            var hasard = Hasard(11);
            if (d.Grains.Count > 0)
                for (var k = 0; k < 36; k++)
                {
                    var x = (int)(hasard() * toile.Largeur);
                    var y = 6 + (int)(hasard() * (toile.Hauteur - 10));
                    toile.Poser(x, y, d.Grains[k % d.Grains.Count]);
                }
            if (d.Pierres.Count > 0)
                foreach (var (cx, rx, ry) in new[] { (70.0, 10.0, 6.0), (196.0, 12.0, 7.0) })
                    for (var y = 0; y <= (int)ry + 2; y++)
                        for (var x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                        {
                            var u = (x - cx) / rx;
                            var v = (y - 2) / ry;
                            if (u * u + v * v > 1) continue;
                            var couleur = v > 0.45 ? d.Pierres[1] : d.Pierres[0];
                            if (d.Mousse.HasValue && v > 0.75 && Toile.Seuil(x, y) < 0.5) couleur = d.Mousse.Value;
                            toile.Poser(x, y, couleur);
                        }
            if (d.Meduses.Count > 0)
                foreach (var (cx, cy) in new[] { (40, 40), (150, 30), (222, 44) })
                {
                    // Une cloche de cinq pixels, et ses filaments qui pendent.
                    for (var dx = -1; dx <= 1; dx++) toile.Poser(cx + dx, cy + 1, d.Meduses[0]);
                    for (var dx = -2; dx <= 2; dx++) toile.Poser(cx + dx, cy, d.Meduses[0]);
                    toile.Poser(cx - 2, cy - 1, d.Meduses[1]);
                    toile.Poser(cx + 2, cy - 1, d.Meduses[1]);
                    for (var i = 1; i <= 3; i++)
                    {
                        toile.Poser(cx - 1, cy - i, d.Meduses[1]);
                        if (i < 3) toile.Poser(cx + 1, cy - i, d.Meduses[1]);
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
