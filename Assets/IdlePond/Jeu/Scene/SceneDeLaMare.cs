using System.Collections.Generic;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la scène : une coupe verticale, le héros, ses poissons.
    ///
    /// Elle ne connaît pas l'état du jeu. Elle écoute `Partie.EtatChange`, en tire une
    /// `VueDeScene` et ne redessine que si la vue a changé (comparée par sa clef). Tout
    /// est dessiné au trait — aucune image importée, spec [D10] : sprites générés à
    /// l'exécution (un carré blanc, un disque blanc, teintés) et `LineRenderer` pour les
    /// traits. Ce que la DA remplacera, c'est ce fichier, jamais `VueDeScene.cs`.
    ///
    /// Ce qu'elle montre, et rien d'autre :
    ///   - une bande par palier ouvert, colorée par son assise, la lumière qui baisse ;
    ///   - dans chaque bande qui porte une espèce, un banc dont l'effectif dessiné croît
    ///     avec le logarithme du niveau ;
    ///   - le héros, dans la bande la plus basse, à l'échelle de son niveau, avec une
    ///     marque par couche ;
    ///   - l'eau qui se trouble quand la jauge dépasse l'alerte — un voile, pas un texte
    ///     (GDD §2.4).
    ///
    /// Les nombres du TypeScript sont gardés tels quels, en « pixels du monde » (une bande
    /// fait 72, le monde 720 de large, l'axe y descend) ; `K` les convertit en unités
    /// Unity (y vers le haut) au seul moment de poser un objet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneDeLaMare : MonoBehaviour
    {
        /// Un pixel du TypeScript, en unités Unity.
        const float K = 0.01f;
        const float LARGEUR = 720f;
        const float HAUTEUR_DE_BANDE = 72f;
        const float MARGE = 12f;

        // Ordres de superposition : le décor, puis les poissons, puis le héros, puis le voile.
        const int ORDRE_FOND = 0;
        const int ORDRE_EAU = 1;
        const int ORDRE_LUMIERE = 2;
        const int ORDRE_POISSONS = 10;
        const int ORDRE_CORPS = 20;
        const int ORDRE_MARQUES = 25;
        const int ORDRE_VOILE = 100;

        /// Opacité du voile quand l'eau est trouble, et temps qu'il met à monter.
        const float OPACITE_DU_VOILE = 0.28f;
        const float DUREE_DU_VOILE_S = 0.9f;

        [SerializeField] Camera cameraDeScene;

        sealed class Poisson
        {
            public Transform Transform;
            public float X0, Y, Amplitude, Duree;
        }

        Camera camera_;
        Camera cameraDeFond;
        Partie partie;
        bool vueAJour;
        VueDeScene vue;
        string derniereClef;

        Transform bandes, poissonsRacine, heros;
        float herosX, herosY;
        float debutDuDessin;
        SpriteRenderer voile;
        float opaciteDuVoile, opaciteVisee;

        Sprite carre, disque;
        Material materielDeTrait;
        bool materielCree;
        readonly List<Object> temporaires = new List<Object>();
        readonly List<Poisson> poissons = new List<Poisson>();

        // Le cadrage de la caméra : on ne le recalcule que si l'un de ses ingrédients a bougé.
        UIDocument document;
        VisualElement cadreDeScene;
        float prochaineRechercheDuCadre;
        Rect rectCourant = new Rect(-1, -1, 0, 0);
        Vector2Int ecranCourant;
        int paliersCourants = -1;

        /* ─── Cycle de vie ──────────────────────────────────────────────────────────*/

        void Awake()
        {
            camera_ = cameraDeScene != null ? cameraDeScene : Camera.main;
            carre = CreerLeCarre();
            disque = CreerLeDisque();
            materielDeTrait = CreerLeMaterielDeTrait();
            bandes = Conteneur("Bandes");
            poissonsRacine = Conteneur("Poissons");
            heros = Conteneur("Heros");
            voile = CreerLeVoile();
            CreerLaCameraDeFond();
        }

        void OnEnable()
        {
            partie = Boucle.ObtenirOuCreerLaPartie();
            partie.EtatChange += SurEtatChange;
            vueAJour = false;
        }

        void OnDisable()
        {
            if (partie != null) partie.EtatChange -= SurEtatChange;
        }

        void OnDestroy()
        {
            foreach (var objet in temporaires) if (objet != null) Destroy(objet);
            if (carre != null) { Destroy(carre.texture); Destroy(carre); }
            if (disque != null) { Destroy(disque.texture); Destroy(disque); }
            if (materielCree && materielDeTrait != null) Destroy(materielDeTrait);
        }

        void SurEtatChange(EtatJeu _) => vueAJour = false;

        void Update()
        {
            AnimerLesPoissons();
            AnimerLeHeros();
            AnimerLeVoile();
        }

        // Après tous les `Update` : la partie a fini d'avancer pour cette image, et
        // l'interface a eu sa mise en page.
        void LateUpdate()
        {
            if (!vueAJour && partie != null)
            {
                vueAJour = true;
                var nouvelle = VueDeScene.Depuis(partie.Etat);
                // La vue est petite : comparer deux chaînes coûte moins qu'un redessin.
                if (nouvelle.Clef != derniereClef)
                {
                    derniereClef = nouvelle.Clef;
                    vue = nouvelle;
                    Redessiner(nouvelle);
                }
            }
            Cadrer();
        }

        /* ─── Le dessin ─────────────────────────────────────────────────────────────*/

        void Redessiner(VueDeScene v)
        {
            Vider(bandes);
            Vider(poissonsRacine);
            Vider(heros);
            foreach (var objet in temporaires) if (objet != null) Destroy(objet);
            temporaires.Clear();
            poissons.Clear();
            debutDuDessin = Time.time;

            DessinerLesBandes(v);
            DessinerLesPoissons(v);
            DessinerLeHeros(v);
            opaciteVisee = v.EauTroublee ? OPACITE_DU_VOILE : 0f;
            paliersCourants = -1; // la hauteur du monde a pu changer : le cadrage se refait
        }

        void DessinerLesBandes(VueDeScene v)
        {
            foreach (var palier in v.Paliers)
            {
                var palette = Palette.PaletteDe(palier.Assise);
                var y = palier.Index * HAUTEUR_DE_BANDE;
                Rectangle(bandes, 0, y, LARGEUR, HAUTEUR_DE_BANDE, Couleur(palette.Fond), ORDRE_FOND);
                Rectangle(bandes, MARGE, y + 2, LARGEUR - 2 * MARGE, HAUTEUR_DE_BANDE - 4,
                    Couleur(palette.Eau, (float)(0.55 + 0.4 * palette.Lumiere)), ORDRE_EAU);
                // La lumière : un voile clair qui s'amincit en descendant.
                Rectangle(bandes, MARGE, y + 2, LARGEUR - 2 * MARGE, 10,
                    Couleur(Palette.LUMIERE_DU_JOUR, (float)(0.12 * palette.Lumiere)), ORDRE_LUMIERE);
            }
        }

        void DessinerLesPoissons(VueDeScene v)
        {
            foreach (var palier in v.Paliers)
            {
                if (palier.Espece == null) continue;
                var effectif = VueDeScene.EffectifDesPoissons(palier.Espece.Niveau);
                var couleur = Couleur(Palette.CouleurDesPoissons(palier.Espece.Rang), 0.9f);
                var y0 = palier.Index * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2;
                for (var k = 0; k < effectif; k++)
                {
                    var x = MARGE + 30 + ((k * 53) % (int)(LARGEUR - 2 * MARGE - 120));
                    var y = y0 + ((k * 17) % 28) - 14;
                    var corps = Disque(poissonsRacine, 0, 0, 14, 7, couleur, ORDRE_POISSONS);
                    poissons.Add(new Poisson
                    {
                        Transform = corps.transform,
                        X0 = x,
                        Y = y,
                        Amplitude = 18 + (k % 3) * 6,
                        // Chacun sa durée : un banc qui oscillerait d'un bloc ressemblerait à une courbe.
                        Duree = (1800 + (k % 5) * 300) / 1000f,
                    });
                }
            }
            AnimerLesPoissons();
        }

        void DessinerLeHeros(VueDeScene v)
        {
            var bandeDuBas = Mathf.Max(0, v.Paliers.Count - 1);
            herosX = LARGEUR - MARGE - 60;
            herosY = bandeDuBas * HAUTEUR_DE_BANDE + HAUTEUR_DE_BANDE / 2;
            // L'échelle s'applique à chaque coordonnée et chaque épaisseur, jamais au
            // transform : un `LineRenderer` n'a pas la même idée qu'un sprite de ce que
            // vaut une échelle héritée, et le héros doit se dessiner pareil des deux façons.
            var e = (float)v.Heros.Echelle;
            heros.localPosition = new Vector3(herosX * K, -herosY * K, 0);

            Disque(heros, 0, 0, 34, 16, Couleur(Palette.CORPS_DU_HEROS), ORDRE_CORPS, e);
            Triangle(heros, new Vector2(-16, 0), new Vector2(-28, -8), new Vector2(-28, 8), Couleur(Palette.CORPS_DU_HEROS), ORDRE_CORPS, e);
            Disque(heros, 9, -2, 3.2f, 3.2f, Couleur(Palette.OEIL_DU_HEROS), ORDRE_CORPS + 1, e);

            var rang = 0;
            foreach (var assise in v.Heros.Couches)
            {
                if (Palette.MarqueParAssise.TryGetValue(assise, out var marque))
                    DessinerUneMarque(marque, e, ORDRE_MARQUES + rang);
                rang++;
            }
        }

        /// Une marque du corps — GDD §15.1. Des primitives, jusqu'à la DA.
        void DessinerUneMarque(MarqueId marque, float e, int ordre)
        {
            switch (marque)
            {
                case MarqueId.Branchies:
                    for (var i = 0; i < 3; i++)
                        Trait(heros, new[] { new Vector2(2 + i * 2.5f, -5), new Vector2(2 + i * 2.5f, 5) }, 1.2f,
                            Couleur(0x5D7A74), false, ordre, e);
                    break;
                case MarqueId.Membranes:
                    var membrane = Couleur(0x7FB3A8, 0.55f);
                    Triangle(heros, new Vector2(-4, -7), new Vector2(6, -12), new Vector2(10, -6), membrane, ordre, e);
                    Triangle(heros, new Vector2(-4, 7), new Vector2(6, 12), new Vector2(10, 6), membrane, ordre, e);
                    break;
                case MarqueId.Luminescence:
                    Disque(heros, -6, 0, 10, 10, Couleur(0x9EF0E0, 0.5f), ordre, e);
                    Disque(heros, -6, 0, 4, 4, Couleur(0xD6FFF7, 0.9f), ordre + 1, e);
                    break;
                case MarqueId.Mineralisation:
                    for (var i = 0; i < 5; i++)
                        Rectangle(heros, -12 + i * 5, -8 + (i % 2) * 2, 2, 2, Couleur(0x8C8F7A), ordre, e);
                    break;
                case MarqueId.Epaississement:
                    Trait(heros, Ellipse(18, 9), 2.5f, Couleur(0x8A9A95, 0.9f), true, ordre, e);
                    break;
                case MarqueId.Halo:
                    Trait(heros, Ellipse(24, 24), 1f, Couleur(0xF1E4A8, 0.7f), true, ordre, e);
                    break;
            }
        }

        static Vector2[] Ellipse(float rayonX, float rayonY)
        {
            const int POINTS = 48;
            var points = new Vector2[POINTS];
            for (var i = 0; i < POINTS; i++)
            {
                var angle = i * Mathf.PI * 2f / POINTS;
                points[i] = new Vector2(Mathf.Cos(angle) * rayonX, Mathf.Sin(angle) * rayonY);
            }
            return points;
        }

        /* ─── Les animations ────────────────────────────────────────────────────────*/

        /// Sine.easeInOut en aller-retour infini : 0 → 1 → 0 sur `2 × duree`.
        static float AllerRetour(float t, float duree)
        {
            var phase = Mathf.Repeat(t / duree, 2f);
            var p = phase <= 1f ? phase : 2f - phase;
            return 0.5f * (1f - Mathf.Cos(Mathf.PI * p));
        }

        void AnimerLesPoissons()
        {
            var t = Time.time - debutDuDessin;
            foreach (var p in poissons)
            {
                if (p.Transform == null) continue;
                var x = p.X0 + p.Amplitude * AllerRetour(t, p.Duree);
                p.Transform.localPosition = new Vector3(x * K, -p.Y * K, 0);
            }
        }

        void AnimerLeHeros()
        {
            if (heros == null) return;
            // Le héros respire : 4 px vers le haut et retour, sur 2,2 s.
            var monte = 4f * AllerRetour(Time.time - debutDuDessin, 2.2f);
            heros.localPosition = new Vector3(herosX * K, -(herosY - monte) * K, 0);
        }

        void AnimerLeVoile()
        {
            if (voile == null) return;
            opaciteDuVoile = Mathf.MoveTowards(opaciteDuVoile, opaciteVisee, OPACITE_DU_VOILE / DUREE_DU_VOILE_S * Time.deltaTime);
            voile.enabled = opaciteDuVoile > 0.0001f;
            voile.color = Couleur(Palette.VOILE_DE_TROUBLE, opaciteDuVoile);
        }

        /* ─── La caméra ─────────────────────────────────────────────────────────────*/

        /// <summary>
        /// Borne la caméra au rectangle de l'élément `#scene` de l'interface, et la fait
        /// défiler pour que la bande la plus basse reste visible : la scène grandit vers le
        /// bas, et c'est là que vit le héros. On interroge l'interface plutôt que de
        /// l'écouter : l'élément n'existe qu'après l'`OnEnable` du `UIDocument`, qui peut
        /// venir après le nôtre, et sa géométrie change à chaque rotation de l'écran.
        /// Sans interface (`Mare.unity` sans `UIDocument`), la scène occupe tout l'écran.
        /// </summary>
        void Cadrer()
        {
            if (camera_ == null) return;
            var rect = RectangleDuCadre();
            var ecran = new Vector2Int(Screen.width, Screen.height);
            var nombre = vue != null ? vue.Paliers.Count : 0;
            if (rect == rectCourant && ecran == ecranCourant && nombre == paliersCourants) return;
            rectCourant = rect;
            ecranCourant = ecran;
            paliersCourants = nombre;

            camera_.rect = rect;
            var pixels = camera_.pixelRect;
            if (pixels.width < 1f || pixels.height < 1f) return;

            // On fixe la LARGEUR visible du monde ; la hauteur visible suit le rapport du rectangle.
            var hauteurVisible = LARGEUR * K * pixels.height / pixels.width;
            camera_.orthographic = true;
            camera_.orthographicSize = hauteurVisible / 2f;
            var hauteurTotale = nombre * HAUTEUR_DE_BANDE * K;
            // Tout tient : la coupe s'accroche en haut. Sinon, le bas du monde reste en bas du cadre.
            var centreY = hauteurTotale <= hauteurVisible ? -hauteurVisible / 2f : -(hauteurTotale - hauteurVisible / 2f);
            camera_.transform.position = new Vector3(LARGEUR * K / 2f, centreY, -10f);

            if (voile != null)
            {
                voile.transform.position = new Vector3(LARGEUR * K / 2f, centreY, 0f);
                voile.transform.localScale = new Vector3(LARGEUR * K + 0.2f, hauteurVisible + 0.2f, 1f);
            }
        }

        /// Le rectangle de `#scene` en fraction de l'écran, origine en bas à gauche — ce
        /// qu'attend `Camera.rect`. Le panneau couvre tout l'écran, quelle que soit son
        /// échelle : une fraction du panneau est une fraction de l'écran.
        Rect RectangleDuCadre()
        {
            var plein = new Rect(0, 0, 1, 1);
            if (cadreDeScene == null || cadreDeScene.panel == null) TrouverLeCadre();
            if (cadreDeScene == null || cadreDeScene.panel == null) return plein;
            var panneau = cadreDeScene.panel.visualTree.worldBound;
            var cadre = cadreDeScene.worldBound;
            if (!(panneau.width > 0f && panneau.height > 0f && cadre.width > 0f && cadre.height > 0f)) return plein;
            var x = Mathf.Clamp01((cadre.xMin - panneau.xMin) / panneau.width);
            var yHaut = Mathf.Clamp01((cadre.yMin - panneau.yMin) / panneau.height);
            var yBas = Mathf.Clamp01((cadre.yMax - panneau.yMin) / panneau.height);
            var largeur = Mathf.Clamp01((cadre.xMax - panneau.xMin) / panneau.width) - x;
            if (largeur <= 0f || yBas <= yHaut) return plein;
            return new Rect(x, 1f - yBas, largeur, yBas - yHaut);
        }

        void TrouverLeCadre()
        {
            // Une recherche par demi-seconde : inutile de fouiller la scène à chaque image
            // tant que l'interface n'est pas là.
            if (Time.unscaledTime < prochaineRechercheDuCadre) return;
            prochaineRechercheDuCadre = Time.unscaledTime + 0.5f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            cadreDeScene = document != null && document.rootVisualElement != null
                ? document.rootVisualElement.Q<VisualElement>("scene")
                : null;
        }

        /* ─── Les briques ───────────────────────────────────────────────────────────*/

        static Color Couleur(int rgb, float alpha = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

        Transform Conteneur(string nom)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        static void Vider(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);
        }

        /// Un rectangle, en coordonnées du TypeScript : coin haut-gauche, y vers le bas.
        /// `e` est l'échelle du héros (1 pour le décor).
        SpriteRenderer Rectangle(Transform parent, float x, float y, float largeur, float hauteur, Color couleur, int ordre, float e = 1f)
        {
            var go = new GameObject("Rect");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((x + largeur / 2f) * K * e, -(y + hauteur / 2f) * K * e, 0f);
            go.transform.localScale = new Vector3(largeur * K * e, hauteur * K * e, 1f);
            return Peindre(go, carre, couleur, ordre);
        }

        /// Une ellipse pleine, par son centre : le disque blanc, étiré.
        SpriteRenderer Disque(Transform parent, float cx, float cy, float largeur, float hauteur, Color couleur, int ordre, float e = 1f)
        {
            var go = new GameObject("Disque");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(cx * K * e, -cy * K * e, 0f);
            go.transform.localScale = new Vector3(largeur * K * e, hauteur * K * e, 1f);
            return Peindre(go, disque, couleur, ordre);
        }

        static SpriteRenderer Peindre(GameObject go, Sprite sprite, Color couleur, int ordre)
        {
            var rendu = go.AddComponent<SpriteRenderer>();
            rendu.sprite = sprite;
            rendu.color = couleur;
            rendu.sortingOrder = ordre;
            return rendu;
        }

        /// Un trait (ou un contour, si `boucle`). Les positions sont en pixels du
        /// TypeScript, relatives au parent, y vers le bas.
        void Trait(Transform parent, Vector2[] points, float epaisseur, Color couleur, bool boucle, int ordre, float e = 1f)
        {
            var go = new GameObject("Trait");
            go.transform.SetParent(parent, false);
            var ligne = go.AddComponent<LineRenderer>();
            ligne.useWorldSpace = false;
            ligne.loop = boucle;
            ligne.positionCount = points.Length;
            for (var i = 0; i < points.Length; i++)
                ligne.SetPosition(i, new Vector3(points[i].x * K * e, -points[i].y * K * e, 0f));
            ligne.widthMultiplier = epaisseur * K * e;
            ligne.startColor = couleur;
            ligne.endColor = couleur;
            ligne.numCapVertices = 0;
            ligne.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ligne.receiveShadows = false;
            if (materielDeTrait != null) ligne.sharedMaterial = materielDeTrait;
            ligne.sortingOrder = ordre;
        }

        /// <summary>
        /// Un triangle plein. Il n'existe pas de sprite pour un triangle quelconque : on le
        /// rastérise dans une petite texture, à six texels par pixel du TypeScript, avec un
        /// sur-échantillonnage 2×2 pour le bord. Texture et sprite sont jetés au prochain
        /// dessin (`temporaires`).
        /// </summary>
        void Triangle(Transform parent, Vector2 a, Vector2 b, Vector2 c, Color couleur, int ordre, float e)
        {
            // En unités Unity, y vers le haut, relatif au parent.
            var pa = new Vector2(a.x, -a.y) * K * e;
            var pb = new Vector2(b.x, -b.y) * K * e;
            var pc = new Vector2(c.x, -c.y) * K * e;
            var texel = K * e / 6f;
            var min = Vector2.Min(pa, Vector2.Min(pb, pc)) - new Vector2(texel, texel);
            var max = Vector2.Max(pa, Vector2.Max(pb, pc)) + new Vector2(texel, texel);
            var largeur = Mathf.Max(2, Mathf.CeilToInt((max.x - min.x) / texel));
            var hauteur = Mathf.Max(2, Mathf.CeilToInt((max.y - min.y) / texel));

            var pixels = new Color32[largeur * hauteur];
            for (var j = 0; j < hauteur; j++)
            {
                for (var i = 0; i < largeur; i++)
                {
                    var dedans = 0;
                    for (var sj = 0; sj < 2; sj++)
                        for (var si = 0; si < 2; si++)
                        {
                            var p = min + new Vector2((i + 0.25f + 0.5f * si) * texel, (j + 0.25f + 0.5f * sj) * texel);
                            if (DansLeTriangle(p, pa, pb, pc)) dedans++;
                        }
                    pixels[j * largeur + i] = new Color32(255, 255, 255, (byte)(dedans * 255 / 4));
                }
            }
            var texture = new Texture2D(largeur, hauteur, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, largeur, hauteur), Vector2.zero, 1f / texel, 0, SpriteMeshType.FullRect);
            temporaires.Add(texture);
            temporaires.Add(sprite);

            var go = new GameObject("Triangle");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(min.x, min.y, 0f);
            Peindre(go, sprite, couleur, ordre);
        }

        static bool DansLeTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var d1 = Aire(p, a, b);
            var d2 = Aire(p, b, c);
            var d3 = Aire(p, c, a);
            var negatif = d1 < 0f || d2 < 0f || d3 < 0f;
            var positif = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negatif && positif);
        }

        static float Aire(Vector2 p, Vector2 a, Vector2 b) => (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);

        /// Le carré blanc : 4×4 texels, 4 texels par unité, donc un carré d'une unité de côté.
        static Sprite CreerLeCarre()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var blanc = new Color32[16];
            for (var i = 0; i < blanc.Length; i++) blanc[i] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(blanc);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
        }

        /// Le disque blanc : 64 texels, 64 par unité, donc un disque d'une unité de diamètre,
        /// au bord adouci sur un texel.
        static Sprite CreerLeDisque()
        {
            const int TAILLE = 64;
            var texture = new Texture2D(TAILLE, TAILLE, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[TAILLE * TAILLE];
            var centre = (TAILLE - 1) / 2f;
            var rayon = TAILLE / 2f;
            for (var j = 0; j < TAILLE; j++)
            {
                for (var i = 0; i < TAILLE; i++)
                {
                    var distance = Mathf.Sqrt((i - centre) * (i - centre) + (j - centre) * (j - centre));
                    var alpha = Mathf.Clamp01(rayon - distance);
                    pixels[j * TAILLE + i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TAILLE, TAILLE), new Vector2(0.5f, 0.5f), TAILLE, 0, SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Le matériau des `LineRenderer` : celui que reçoit un `SpriteRenderer` neuf, donc
        /// celui du pipeline du projet. Sans matériau, un trait s'affiche en rose.
        /// </summary>
        Material CreerLeMaterielDeTrait()
        {
            var sonde = new GameObject("sonde");
            var materiel = sonde.AddComponent<SpriteRenderer>().sharedMaterial;
            Destroy(sonde);
            if (materiel != null) return materiel;
            var shader = Shader.Find("Sprites/Default");
            materielCree = shader != null;
            return shader != null ? new Material(shader) : null;
        }

        SpriteRenderer CreerLeVoile()
        {
            var go = new GameObject("Voile");
            go.transform.SetParent(transform, false);
            var rendu = Peindre(go, carre, Couleur(Palette.VOILE_DE_TROUBLE, 0f), ORDRE_VOILE);
            rendu.enabled = false;
            return rendu;
        }

        /// <summary>
        /// Quand la caméra n'occupe qu'une partie de l'écran, le reste n'est effacé par
        /// personne : une caméra de fond, qui ne rend rien, le peint de l'eau-abysse.
        /// </summary>
        void CreerLaCameraDeFond()
        {
            var go = new GameObject("FondDeLaMare");
            go.transform.SetParent(transform, false);
            cameraDeFond = go.AddComponent<Camera>();
            cameraDeFond.clearFlags = CameraClearFlags.SolidColor;
            cameraDeFond.backgroundColor = Couleur(Palette.EAU_ABYSSE);
            cameraDeFond.cullingMask = 0;
            cameraDeFond.depth = (camera_ != null ? camera_.depth : 0f) - 1f;
        }
    }
}
