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
            DecorDArt precedent = null;
            foreach (var assise in RegistreDArt.ASSISES_DESSINEES)
            {
                var decor = RegistreDArt.DecorDe(assise);
                var dessus = precedent;
                // Le puits (2026-11-25) : l'eau de chaque palier, tranche d'un dégradé qui court
                // sur tout le lieu, fondue dans celle d'au-dessus si le lieu se raccorde en fondu ;
                // les parois et la corniche de chaque palier ; le sol du fond.
                var rangs = Chemins.Rangs(assise).ToList();
                var fondu = decor.Jonction == JonctionDeLieu.Fondu ? precedent : null;
                foreach (var rang in rangs)
                {
                    var palier = Chemins.PremierPalier(assise) + rang;
                    sortie[Chemins.Eau(assise, rang)] = DessinerEau(decor, rang, rangs.Count, fondu).EnPng();
                    sortie[Chemins.Parois(assise, rang)] = DessinerParois(decor, palier).EnPng();
                    sortie[Chemins.Corniche(assise, rang)] = DessinerCorniche(decor, palier).EnPng();
                }
                sortie[Chemins.Lit(assise)] = DessinerLit(decor).EnPng();
                precedent = decor;
                sortie[Chemins.Roche(assise)] = DessinerRoche(decor).EnPng();
                sortie[Chemins.Rayons(assise)] = DessinerRayons(decor).EnPng();
                if (decor.Berge) sortie[Chemins.Berge(assise)] = DessinerBerge(decor).EnPng();
                if (decor.AUneAmbiance) sortie[Chemins.Ambiance(assise)] = DessinerAmbiance(decor).EnPng();
                // Le seuil : seulement pour un lieu qu'on atteint en perçant, pas pour la surface.
                if (assise != RegistreDArt.ASSISES_DESSINEES[0]) sortie[Chemins.Seuil(assise)] = DessinerSeuil(decor, dessus).EnPng();
            }
            sortie[Chemins.VOILE] = DessinerVoile().EnPng();
            sortie[Chemins.ECLAT] = DessinerEclat().EnPng();
            for (var c = 0; c < Eau.COUCHES_DE_CAUSTIQUES; c++) sortie[Chemins.Caustiques(c)] = DessinerCaustiques(c).EnPng();
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

        /// <summary>
        /// L'eau du `rang`-ième des `nombre` paliers d'un lieu (le puits, 2026-11-25) : une
        /// tranche d'un seul dégradé qui court du haut du lieu à son fond — du clair au sombre
        /// (bible v0.3). Progressif de bout en bout (2026-11-26) : entre deux couleurs du registre,
        /// des nuances intermédiaires (`DecorDArt.EauFine`), et de l'une à la suivante un tramage
        /// qui avance pixel à pixel — ni aplat, ni marche. Plus de sol ici : il n'y en a qu'au fond
        /// du puits (`DessinerLit`). Un lieu en fondu mêle tout son premier palier à l'eau la plus
        /// sombre de celui d'au-dessus (`fondu`), de moins en moins en descendant, sur une courbe
        /// douce. La lumière, elle, vient des Light2D. Se répète en largeur : tout y a une période
        /// qui divise 48.
        /// </summary>
        static Toile DessinerEau(DecorDArt d, int rang, int nombre, DecorDArt fondu)
        {
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            var fine = d.EauFine;
            var hauteurDuLieu = nombre * toile.Hauteur - 1.0;
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var descente = rang * toile.Hauteur + (toile.Hauteur - 1 - y);
                    // Les nappes ondulent : leur passage de l'une à l'autre n'est pas une règle.
                    var onde = 3 * Math.Sin(2 * Math.PI * x / toile.Largeur) + 2 * Math.Sin(4 * Math.PI * x / toile.Largeur + 1);
                    var profondeur = Math.Max(0, Math.Min(hauteurDuLieu, descente + onde)) / hauteurDuLieu * (fine.Count - 1);
                    var i = Math.Min((int)profondeur, fine.Count - 1);
                    var couleur = i + 1 < fine.Count && Toile.Seuil(x, y) < profondeur - i ? fine[i + 1] : fine[i];
                    // Le fondu : l'eau d'au-dessus descend dans la première bande et s'y perd.
                    if (fondu != null && rang == 0)
                    {
                        var u = Math.Min(1.0, descente / (toile.Hauteur - 1.0));
                        if (Toile.Seuil(x, y) >= u * u * (3 - 2 * u)) couleur = fondu.Eau[fondu.Eau.Count - 1];
                    }
                    toile.Poser(x, y, couleur);
                }
            return toile;
        }

        public const int HAUTEUR_DU_LIT = 30;
        const int LARGEUR_DU_LIT = 192;

        /// <summary>
        /// Le sol du fond du puits : la vase et ses herbes, ou le lit de galets et sa mousse, et
        /// les grosses pierres du Gour. Posé au bas du dernier palier ouvert seulement ; il
        /// descend quand on creuse. Pave le décor (192 divise 576).
        /// </summary>
        static Toile DessinerLit(DecorDArt d)
        {
            var motif = new Toile(Gabarits.LARGEUR_DE_FOND, HAUTEUR_DU_LIT);
            if (d.Galets.Count == 0)
                for (var x = 0; x < motif.Largeur; x++)
                {
                    for (var y = 0; y <= 2; y++) motif.Poser(x, y, d.Vase);
                    if (x % 2 == 0) motif.Poser(x, 3, d.Vase);
                }
            if (d.Galets.Count > 0) DessinerGalets(motif, d);
            if (d.Herbes.Count > 0) DessinerHerbes(motif, d);
            var toile = new Toile(LARGEUR_DU_LIT, HAUTEUR_DU_LIT);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var c = motif.Lire(x % motif.Largeur, y);
                    if (c.HasValue) toile.Poser(x, y, c.Value);
                }
            if (d.Pierres.Count > 0)
                foreach (var (cx, rx, ry) in new[] { (52.0, 15.0, 9.0), (141.0, 18.0, 10.5) })
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
            return toile;
        }

        /* ─── Les parois et les corniches du puits ─────────────────────────────── */

        /// Une pierre de paroi, en coordonnées du MONDE : une cellule de Voronoï autour d'un
        /// germe tiré dans une grille de 12 × 12. Les parois de deux paliers se raccordent donc
        /// sans couture. `Ecart` : la distance à la fissure la plus proche.
        static (bool Claire, double Ecart) PierreDuMonde(int xw, int yw)
        {
            const int CASE = 12;
            int Plancher(int a) => a >= 0 ? a / CASE : -((-a + CASE - 1) / CASE);
            var cx = Plancher(xw);
            var cy = Plancher(yw);
            double premiere = double.MaxValue, seconde = double.MaxValue;
            var claire = false;
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var c = cx + dx;
                    var r = cy + dy;
                    var h = (uint)(c * 73856093) ^ (uint)(r * 19349663) ^ 0x9E3779B9u;
                    h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                    var gx = c * CASE + 1 + (h & 0xFF) / 255.0 * (CASE - 2);
                    var gy = r * CASE + 1 + ((h >> 8) & 0xFF) / 255.0 * (CASE - 2);
                    var ddx = (xw - gx) * 0.8;
                    var ddy = yw - gy;
                    var distance = Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (distance < premiere) { seconde = premiere; premiere = distance; claire = (h >> 16 & 3) != 0; }
                    else if (distance < seconde) seconde = distance;
                }
            return (claire, seconde - premiere);
        }

        /// La couleur d'un pixel de roche à `profond` pixels du bord de l'eau : l'arête éclairée
        /// au bord, puis la matière du lieu. Le calcaire porte en plus, le long du bord, la croûte
        /// de calcite que l'eau y dépose : des plaques d'épaisseur inégale, et de loin en loin une
        /// draperie qui coule plus loin dans la roche, cernée d'une ligne sombre.
        static int CouleurDeParoi(DecorDArt d, int xw, int yw, int profond)
        {
            if (profond == 0) return d.Roche[2];
            if (profond == 1) return Toile.Seuil(xw & 3, yw & 3) < 0.5 ? d.Roche[2] : d.Roche[1];
            if (d.Paroi == MatiereDeParoi.Calcaire && d.Inclusions.Count >= 2)
            {
                var h = HacherLaParoi(Plancher(yw, 7), 61);
                var epaisseur = h % 4u == 0 ? 0 : 2 + (int)((h >> 4) % 3u);
                if ((h >> 8) % 5u == 0) epaisseur += 2 + Boucler(yw, 7) / 2;
                if (profond < epaisseur)
                    return profond == 2 || Toile.Seuil(xw & 3, yw & 3) >= 0.8 ? d.Inclusions[1] : d.Inclusions[0];
                if (profond == epaisseur) return d.Roche[3];
            }
            return Matiere(d, xw, yw, 0, 0);
        }

        /* ─── Les matières (2026-11-26) ──────────────────────────────────────────
         * Une couleur par pixel de roche, en coordonnées du monde, d'après `DecorDArt.Paroi`.
         * `periodeX` et `periodeY` (0 : aucune) font raccorder le motif à lui-même : la roche à
         * creuser pave en 48 × 84 ; tout ce qui s'y répète a une période qui les divise. */

        static int Plancher(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);

        static int Boucler(int k, int cycle) => cycle > 0 ? ((k % cycle) + cycle) % cycle : k;

        static uint HacherLaCase(int cx, int cy, int cycleX, int cycleY, uint sel) =>
            HacherLaParoi(Boucler(cx, cycleX) * 92821 + Boucler(cy, cycleY), sel);

        /// Le lit de roche où tombe la hauteur `y` : son numéro, sa limite basse (comprise) et sa
        /// limite haute (exclue). Les limites sont espacées de `pas`, chacune décalée au hasard
        /// d'un quart de pas au plus : les lits n'ont pas tous la même épaisseur.
        static (int Numero, int Bas, int Haut) LitDeRoche(int y, int pas, int cycle, uint sel)
        {
            int Limite(int n) => n * pas + (int)(HacherLaParoi(Boucler(n, cycle), sel) % (uint)(pas / 2)) - pas / 4;
            var k = Plancher(y, pas);
            if (y < Limite(k)) k--;
            else if (y >= Limite(k + 1)) k++;
            return (k, Limite(k), Limite(k + 1));
        }

        static int Matiere(DecorDArt d, int x, int y, int periodeX, int periodeY)
        {
            if (d.Paroi == MatiereDeParoi.Terre && d.Inclusions.Count >= 2) return Terre(d, x, y, periodeX, periodeY);
            if (d.Paroi == MatiereDeParoi.Calcaire && d.Inclusions.Count >= 2) return Calcaire(d, x, y, periodeX, periodeY);
            // Les pierres jointes, celles d'avant : sans période (la roche à creuser des pierres a
            // son propre dessin, `DessinerRocheDePierres`).
            var (claire, ecart) = PierreDuMonde(x, y);
            if (ecart < 1.35) return d.Roche[3];
            if (PierreDuMonde(x, y + 1).Ecart < 1.35 && Toile.Seuil(x & 3, y & 3) < 0.7) return d.Roche[2];
            return claire ? d.Roche[1] : d.Roche[0];
        }

        /// <summary>
        /// La terre de la Noue : des lits de sept pixels environ qui ondulent, chacun d'une des
        /// quatre teintes (deux bruns, un limon clair tramé, un humus sombre piqueté), fondus
        /// l'un dans l'autre par une rangée tramée ; et de loin en loin un galet pris dedans,
        /// éclairé en haut à gauche, son ombre portée dessous.
        /// </summary>
        static int Terre(DecorDArt d, int x, int y, int periodeX, int periodeY)
        {
            const int PAS = 7, CASE = 12;
            var r = d.Roche;
            var inclusions = d.Inclusions;
            var cycle = periodeY / PAS;
            var yy = y + (int)Math.Round(1.6 * Math.Sin(2 * Math.PI * x / 48) + 0.9 * Math.Sin(2 * Math.PI * x / 16 + 1.3));
            var (k, _, haut) = LitDeRoche(yy, PAS, cycle, 41);
            int Teinte(int lit)
            {
                switch (HacherLaParoi(Boucler(lit, cycle), 43) % 4u)
                {
                    case 0: return r[0];
                    case 1: return r[1];
                    case 2: return Toile.Seuil(x, yy) < 0.3 ? r[2] : r[1];
                    default: return Toile.Seuil(x, yy) < 0.2 ? r[3] : r[0];
                }
            }
            var couleur = yy == haut - 1 && Toile.Seuil(x, yy) < 0.5 ? Teinte(k + 1) : Teinte(k);
            var gx = Plancher(x, CASE);
            var gy = Plancher(y, CASE);
            var h = HacherLaCase(gx, gy, periodeX / CASE, periodeY / CASE, 47);
            if (h % 5u != 0) return couleur;
            var ox = gx * CASE + 3 + (int)((h >> 4) % 6u);
            var oy = gy * CASE + 3 + (int)((h >> 8) % 6u);
            var rayon = 1.0 + ((h >> 12) % 4u) * 0.5;
            var u = (x - ox) / (rayon * 1.4);
            var v = (y - oy) / rayon;
            if (u * u + v * v <= 1)
            {
                var clair = (h >> 16) % 2u == 1 ? inclusions[1] : inclusions[0];
                return v > 0.2 && u < 0.5 ? clair : clair == inclusions[1] ? inclusions[0] : r[2];
            }
            var sous = (y + 1 - oy) / rayon;
            return u * u + sous * sous <= 1 ? r[3] : couleur;
        }

        /// <summary>
        /// Le calcaire du Gour : des lits épais de douze pixels environ, presque droits, d'une
        /// teinte ou de l'autre ; le joint sombre qui les sépare, leur dessus éclairé ; et dans
        /// chaque lit, de loin en loin, une fente presque verticale qui penche un peu.
        /// </summary>
        static int Calcaire(DecorDArt d, int x, int y, int periodeX, int periodeY)
        {
            const int PAS = 12, FENTES = 24;
            var r = d.Roche;
            var cycle = periodeY / PAS;
            var yy = y + (int)Math.Round(0.8 * Math.Sin(2 * Math.PI * x / 48 + 0.5));
            var (k, bas, haut) = LitDeRoche(yy, PAS, cycle, 51);
            var lit = Boucler(k, cycle);
            if (yy == bas) return r[3];
            var hl = HacherLaParoi(lit, 53);
            var jc = Plancher(x, FENTES);
            var hj = HacherLaCase(jc, lit, periodeX / FENTES, 0, 57);
            var fente = hj % 3u != 0;
            var xf = jc * FENTES + 4 + (int)(hj % 16u) + Plancher((yy - bas) * ((int)((hj >> 8) % 3u) - 1), 5);
            if (fente && x == xf) return r[3];
            var corps = hl % 2u == 1 ? r[1] : r[0];
            if (yy == haut - 1) return Toile.Seuil(x, yy) < 0.75 ? r[2] : corps;
            var couleur = fente && x == xf + 1 ? r[0] : corps;
            if (Toile.Seuil(x, yy) < 0.07) couleur = couleur == r[1] ? r[0] : r[1];
            return couleur;
        }

        static uint HacherLaParoi(int a, uint sel)
        {
            var h = (uint)(a * 7919) + sel * 104729u;
            h ^= h >> 15; h *= 0x2c1b3c6dU; h ^= h >> 12;
            return h;
        }

        /// <summary>
        /// Les parois d'un palier (le puits, 2026-11-25) : de la roche sur toute la largeur du
        /// décor, sauf l'eau entre `Puits.BordGauche` et `Puits.BordDroit` — un bord qui ondule
        /// et se raccorde au palier d'au-dessus et d'au-dessous. Des touffes d'herbe (ou de
        /// mousse) s'accrochent au bord.
        /// </summary>
        static Toile DessinerParois(DecorDArt d, int palier)
        {
            var toile = new Toile(Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE);
            var bas = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE;
            var touffe = d.Mousse ?? (d.Herbes.Count > 0 ? d.Herbes[0] : d.Roche[2]);
            for (var y = 0; y < toile.Hauteur; y++)
            {
                var yw = bas + y;
                var gauche = Puits.BordGauche(yw);
                var droit = Puits.BordDroit(yw);
                for (var x = 0; x < toile.Largeur; x++)
                {
                    var xw = Gabarits.GAUCHE_DU_DECOR + x;
                    if (xw < gauche) toile.Poser(x, y, CouleurDeParoi(d, xw, yw, gauche - 1 - xw));
                    else if (xw > droit) toile.Poser(x, y, CouleurDeParoi(d, xw, yw, xw - droit - 1));
                }
                // Une touffe de temps en temps, qui déborde du bord sur l'eau.
                var h = HacherLaParoi(yw, 3);
                if (h % 9u == 0)
                    for (var i = 0; i < 1 + (int)((h >> 8) % 3u); i++) toile.Poser(gauche + i - Gabarits.GAUCHE_DU_DECOR, y, touffe);
                if ((h >> 4) % 9u == 0)
                    for (var i = 0; i < 1 + (int)((h >> 12) % 3u); i++) toile.Poser(droit - i - Gabarits.GAUCHE_DU_DECOR, y, touffe);
            }
            // Les racines qui sortent de la berge de terre et pendent sur l'eau, leur bout qui
            // bourgeonne : celles de la surface (`berge.png`) descendent jusqu'ici.
            if (d.Paroi == MatiereDeParoi.Terre && d.Racines.Count > 0)
                for (var y = 0; y < toile.Hauteur; y++)
                {
                    var yw = bas + y;
                    var h = HacherLaParoi(yw, 67);
                    if (h % 17u != 0) continue;
                    var aGauche = (h >> 5) % 2u == 0;
                    var sens = aGauche ? 1 : -1;
                    var x = (aGauche ? Puits.BordGauche(yw) : Puits.BordDroit(yw)) - sens - Gabarits.GAUCHE_DU_DECOR;
                    var sortie = 2 + (int)((h >> 7) % 4u);
                    var chute = 4 + (int)((h >> 10) % 9u);
                    var yr = y;
                    for (var i = 0; i < sortie; i++)
                    {
                        x += sens;
                        toile.Poser(x, yr, d.Racines[0]);
                    }
                    for (var i = 0; i < chute; i++)
                    {
                        yr--;
                        if (i % 4 == 2) x += sens;
                        var bourgeon = d.Bourgeon.HasValue && i >= chute - 2;
                        toile.Poser(x, yr, bourgeon ? d.Bourgeon.Value : d.Racines[0]);
                    }
                }
            return toile;
        }

        public const int HAUTEUR_DE_CORNICHE = 44;
        /// La corniche pend sous le bas de son palier : sa toile commence ce nombre de pixels
        /// plus bas (`Decor` la pose là).
        public const int PENDANT_DE_CORNICHE = Gabarits.HAUTEUR_DE_BANDE / 7;

        /// <summary>
        /// La corniche d'un palier (`Puits.CornicheDe`) : un bloc de roche qui sort de sa paroi,
        /// épais contre elle, qui s'amincit en pointe ; son dessus presque plat (on s'y pose, à
        /// `Corniche.Sol`), son dessous bosselé qui pend sous le palier, avec quelques gouttes de
        /// pierre ; le dessus éclairé, et ce qui y pousse — des herbes dans la Noue, de la mousse
        /// et des galets dans le Gour. Le calcaire retient au bout de sa corniche une flaque derrière
        /// un rebord de calcite : un gour, le bassin que l'eau se bâtit (2026-11-26). Sa toile couvre
        /// la largeur du décor et commence `PENDANT_DE_CORNICHE` pixels sous le bas du palier.
        /// </summary>
        static Toile DessinerCorniche(DecorDArt d, int palier)
        {
            var toile = new Toile(Gabarits.LARGEUR_DU_DECOR, HAUTEUR_DE_CORNICHE);
            var c = Puits.CornicheDe(palier);
            var pied = -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE - PENDANT_DE_CORNICHE;
            // Le dessus : la rangée juste sous `Sol`.
            var dessus = c.Sol - 1 - pied;
            var hasard = Hasard(31u + (uint)palier);
            const int EPAISSEUR_A_LA_PAROI = 16;
            var calcaire = d.Paroi == MatiereDeParoi.Calcaire && d.Inclusions.Count >= 2 && d.Eau.Count >= 2;
            for (var x = 0; x < toile.Largeur; x++)
            {
                var xw = Gabarits.GAUCHE_DU_DECOR + x;
                // La distance à la paroi, le long de la corniche : négative dans la paroi.
                var s = c.AGauche ? xw - c.Debut : c.Fin - 1 - xw;
                if (s >= c.Longueur) continue;
                var u = Math.Max(0.0, s / (double)c.Longueur);
                // Épaisse à la paroi, en pointe au bout ; le dessous ondule et se bosselle.
                var epaisseur = 3 + (EPAISSEUR_A_LA_PAROI - 3) * Math.Pow(1 - u, 1.4)
                    + 1.6 * Math.Sin(xw * 0.23 + palier) + 1.1 * Math.Sin(xw * 0.57 + 2 * palier);
                var reste = c.Longueur - s;
                var haut = dessus - (reste < 4 ? 4 - reste : 0) + ((xw + palier) % 23 == 0 && reste > 8 ? 1 : 0);
                var flaque = calcaire && reste >= 4 && reste <= 15;
                var rebord = calcaire && reste >= 2 && reste <= 3;
                if (flaque) haut--;
                var dessous = Math.Max(0, haut - (int)Math.Round(Math.Max(2, epaisseur)));
                // Une goutte de pierre qui pend, de loin en loin.
                if (reste > 10 && s > 4 && HacherLaParoi(xw, (uint)palier + 7) % 29u == 0) dessous = Math.Max(0, dessous - 2 - (int)(HacherLaParoi(xw, 9) % 3u));
                for (var y = dessous; y <= haut; y++)
                {
                    int couleur;
                    if (y == haut) couleur = d.Roche[2];
                    else if (y == haut - 1 && Toile.Seuil(x, y) < 0.5) couleur = d.Roche[2];
                    else if (y == dessous) couleur = d.Roche[3];
                    else couleur = Matiere(d, xw, pied + y, 0, 0);
                    toile.Poser(x, y, couleur);
                }
                if (flaque)
                {
                    toile.Poser(x, haut, reste == 4 || reste == 15 ? d.Inclusions[0] : d.Eau[1]);
                    toile.Poser(x, haut + 1, reste >= 5 && reste <= 14 ? d.Eau[0] : d.Inclusions[1]);
                }
                if (rebord)
                {
                    toile.Poser(x, haut, d.Inclusions[0]);
                    toile.Poser(x, haut + 1, d.Inclusions[1]);
                    if (reste == 3) toile.Poser(x, haut + 2, d.Inclusions[1]);
                }
                if (s < 0 || reste < 3) continue;
                // Ce qui pousse dessus.
                if (d.Herbes.Count > 0 && hasard() < 0.16)
                {
                    var brin = d.Herbes[(int)(hasard() * d.Herbes.Count)];
                    var longueur = 3 + (int)(hasard() * 8);
                    for (var i = 1; i <= longueur; i++)
                        toile.Poser(x + (int)Math.Round(Math.Sin(i * 0.5 + x) * 0.8), haut + i, brin);
                }
                if (d.Mousse.HasValue && !flaque && Toile.Seuil(x, haut) < 0.45) toile.Poser(x, haut, d.Mousse.Value);
                if (d.Galets.Count > 1 && hasard() < 0.05 && !flaque)
                {
                    var galet = d.Galets[(int)(hasard() * (d.Galets.Count - 1))];
                    toile.Poser(x, haut + 1, galet);
                    toile.Poser(x + 1, haut + 1, galet);
                    toile.Poser(x + 1, haut + 2, galet);
                }
            }
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
        /// La roche d'un palier pas encore creusé, dans la matière de ses parois (2026-11-26) ; en
        /// haut de la bande, le seuil, la ligne sombre où l'eau creusée s'arrête. Se raccorde à
        /// elle-même en largeur comme en hauteur. Dessinée dans ses couleurs ; c'est l'absence de
        /// lumière qui la plonge dans l'obscurité.
        /// </summary>
        static Toile DessinerRoche(DecorDArt d)
        {
            if (d.Paroi == MatiereDeParoi.Pierres || d.Inclusions.Count < 2)
                return DessinerRocheDePierres(d);
            var toile = new Toile(Gabarits.LARGEUR_DE_FOND, Gabarits.HAUTEUR_DE_BANDE);
            for (var y = 0; y < toile.Hauteur; y++)
                for (var x = 0; x < toile.Largeur; x++)
                    toile.Poser(x, y, y >= toile.Hauteur - 3 ? d.Roche[3] : Matiere(d, x, y, toile.Largeur, toile.Hauteur));
            return toile;
        }

        /// <summary>
        /// La roche des pierres jointes : des pierres irrégulières (une cellule de
        /// Voronoï chacune, autour d'un germe tiré au hasard dans une grille de 12 × 12), jointes
        /// par des fissures, éclairées d'une arête claire sur leur bord haut ; en haut de la
        /// bande, le seuil, la ligne sombre où l'eau creusée s'arrête. Les distances se
        /// mesurent en tournant sur la largeur : la tuile se raccorde à elle-même quand elle
        /// pave. Dessinée dans ses couleurs ; c'est l'absence de lumière qui la plonge dans
        /// l'obscurité.
        /// </summary>
        static Toile DessinerRocheDePierres(DecorDArt d)
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
        /// le pollen de la Sève ; les méduses du Gour et les traits de son courant (2026-11-26 :
        /// ils étaient dans l'eau, en pointillés réguliers que la tuile de 48 px répétait ; ici,
        /// seize traits d'écume de longueurs diverses, semés au hasard, effilés aux deux bouts, dont
        /// certains montent ou descendent d'un pixel à mi-course).
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
            if (d.Meduses.Count > 0)
                // (60, 48) : pavée depuis `GAUCHE_DU_DECOR`, elle revient à x = 336 du champ, où la
                // membrane d'une cassure descend à une trentaine de pixels (2026-11-26).
                foreach (var (cx, cy) in new[] { (60, 48), (225, 45), (333, 66) })
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
            if (d.Courant && d.Ecume.Count > 0)
            {
                var filets = Hasard(23);
                for (var k = 0; k < 16; k++)
                {
                    var x0 = (int)(filets() * toile.Largeur);
                    var y = 8 + (int)(filets() * (toile.Hauteur - 16));
                    var longueur = 5 + (int)(filets() * 12);
                    var couleur = d.Ecume[k % d.Ecume.Count];
                    var tirage = filets();
                    var marche = tirage < 0.5 ? 0 : tirage < 0.75 ? 1 : -1;
                    for (var i = 0; i < longueur; i++)
                    {
                        var bout = i < 2 || i >= longueur - 2;
                        if (bout && i % 2 == 1) continue;
                        var dy = marche != 0 && i >= longueur / 2 ? marche : 0;
                        toile.Poser((x0 + i) % toile.Largeur, y + dy, couleur);
                    }
                }
            }
            return toile;
        }

        /// <summary>
        /// Le seuil d'un lieu qui s'ouvre par une cassure (2026-11-26) : l'eau la plus sombre du
        /// lieu d'au-dessus (`dessus`), qui appuie sur la membrane de mana du lieu — au-dessus de
        /// son arc (`Lieux.CreuxDeLaMembrane`), sur toute la largeur du décor ; les parois passent
        /// devant. Son bord se trame sur deux rangées, que la membrane, tracée vivante par la
        /// scène, recouvre en ondulant.
        /// </summary>
        static Toile DessinerSeuil(DecorDArt d, DecorDArt dessus)
        {
            var toile = new Toile(Gabarits.LARGEUR_DU_DECOR, Lieux.HAUTEUR_DU_SEUIL);
            var eau = dessus != null && dessus.Eau.Count > 0 ? dessus.Eau[dessus.Eau.Count - 1] : d.Eau[d.Eau.Count - 1];
            var haut = toile.Hauteur - 1;
            for (var x = 0; x < toile.Largeur; x++)
            {
                var creux = (int)Math.Round(Lieux.CreuxDeLaMembrane(x + Gabarits.GAUCHE_DU_DECOR));
                // `i` : les pixels sous la jonction. La membrane au repos occupe `creux - 1` et `creux`.
                for (var i = 0; i <= creux && i < toile.Hauteur; i++)
                {
                    var y = haut - i;
                    var tenue = i < creux - 1 ? 1.0 : i == creux - 1 ? 0.75 : 0.4;
                    if (Toile.Seuil(x, y) < tenue) toile.Poser(x, y, eau);
                }
            }
            return toile;
        }

        /// <summary>
        /// Une couche des caustiques (2026-11-25) : le réseau de lumière que la surface projette
        /// dans l'eau — les arêtes d'un pavage de cellules aplaties, d'un pixel d'épaisseur, en
        /// blanc teinté au rendu. Égal sur toute la bande et raccordé à lui-même en largeur
        /// comme en hauteur : d'une bande à l'autre, pas de couture (le puits). C'est la lumière
        /// de la scène qui l'éteint en descendant.
        /// </summary>
        static Toile DessinerCaustiques(int couche)
        {
            var toile = new Toile(Eau.LARGEUR_DES_CAUSTIQUES, Gabarits.HAUTEUR_DE_BANDE);
            var hasard = Hasard(couche == 0 ? 101u : 202u);
            const int CELLULES = 9;
            var centres = new (double X, double Y)[CELLULES];
            for (var i = 0; i < CELLULES; i++) centres[i] = (hasard() * toile.Largeur, hasard() * toile.Hauteur);
            for (var y = 0; y < toile.Hauteur; y++)
            {
                for (var x = 0; x < toile.Largeur; x++)
                {
                    double premier = double.MaxValue, second = double.MaxValue;
                    foreach (var (cx, cy) in centres)
                    {
                        var dx = Math.Abs(x - cx);
                        dx = Math.Min(dx, toile.Largeur - dx);
                        var ady = Math.Abs(y - cy);
                        // Aplaties : vues de biais, les cellules sont plus larges que hautes.
                        var dy = Math.Min(ady, toile.Hauteur - ady) * 1.6;
                        var dist = Math.Sqrt(dx * dx + dy * dy);
                        if (dist < premier) { second = premier; premier = dist; }
                        else if (dist < second) second = dist;
                    }
                    if (second - premier < 1.3) toile.Poser(x, y, RegistreDArt.BLANC);
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
