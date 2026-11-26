using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la lumière (spec DA §2). Une ambiante faible, et une lumière par nappe — une
    /// suite de paliers où la lumière glisse sans marche (`RegistreDArt.NappesDeLumiere`) :
    /// rectangle de la largeur du décor, dont le cookie suit la courbe de chaque palier — la
    /// lumière baisse en descendant (GDD §15.2). Seules les nappes qui touchent les bandes
    /// visibles sont allumées : une ou deux, plus la roche — quatre au plus à l'écran, le budget
    /// d'un téléphone (2026-11-26 ; avant, une lumière par palier en allumait jusqu'à six).
    /// </summary>
    public sealed class Eclairage : IDisposable
    {
        readonly Transform racine;
        readonly List<Light2D> nappes = new List<Light2D>();
        readonly List<(int Premier, int Dernier)> paliersDesNappes = new List<(int, int)>();
        // Le dégradé de chaque nappe, palier par palier : recréés à chaque dessin, détruits avec lui.
        readonly List<UnityEngine.Object> degradesDesBandes = new List<UnityEngine.Object>();
        const int MARCHES_D_UNE_BANDE = 16;
        Light2D surLaRoche;
        // Le dégradé de la roche, du haut (clair) vers le bas (éteint) : voir `EclairerLaRoche`.
        readonly Texture2D degrade;
        readonly Sprite rectangleDegrade;
        // Un pixel blanc, étiré au rectangle de la bande : voir `Dessiner`.
        readonly Texture2D blanc;
        readonly Sprite rectangle;

        public Light2D Ambiante { get; }
        /// Les lumières des nappes, de haut en bas, et les paliers de chacune.
        public IReadOnlyList<Light2D> Nappes => nappes;
        public IReadOnlyList<(int Premier, int Dernier)> PaliersDesNappes => paliersDesNappes;

        public Eclairage(Transform parent)
        {
            blanc = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Lumiere blanche" };
            blanc.SetPixel(0, 0, Color.white);
            blanc.Apply();
            rectangle = Sprite.Create(blanc, new Rect(0, 0, 1, 1), Vector2.zero, 1f);
            const int MARCHES = 16;
            degrade = new Texture2D(1, MARCHES, TextureFormat.RGBA32, false) { name = "Lumiere de roche", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < MARCHES; y++)
            {
                // y = 0 en bas : le bas de la roche, le plus loin de l'eau.
                var t = y / (float)(MARCHES - 1);
                var force = Mathf.Lerp((float)RegistreDArt.LUMIERE_DE_ROCHE_EN_BAS, (float)RegistreDArt.LUMIERE_DE_ROCHE_EN_HAUT, t * t);
                degrade.SetPixel(0, y, new Color(force, force, force, 1f));
            }
            degrade.Apply();
            rectangleDegrade = Sprite.Create(degrade, new Rect(0, 0, 1, MARCHES), Vector2.zero, 1f);
            racine = new GameObject("Eclairage").transform;
            racine.SetParent(parent, false);
            var go = new GameObject("Ambiante");
            go.transform.SetParent(racine, false);
            Ambiante = go.AddComponent<Light2D>();
            Ambiante.lightType = Light2D.LightType.Global;
            Ambiante.intensity = (float)RegistreDArt.LUMIERE_AMBIANTE;
            Ambiante.color = Color.white;
        }

        public void Dessiner(VueDeScene vue)
        {
            foreach (var lumiere in nappes) if (lumiere != null) UnityEngine.Object.Destroy(lumiere.gameObject);
            nappes.Clear();
            paliersDesNappes.Clear();
            foreach (var o in degradesDesBandes) if (o != null) UnityEngine.Object.Destroy(o);
            degradesDesBandes.Clear();
            var l = Gabarits.LARGEUR_DU_DECOR;
            var h = Gabarits.HAUTEUR_DE_BANDE;
            foreach (var (premier, dernier) in RegistreDArt.NappesDeLumiere(vue.Paliers.Select(p => p.Index)))
            {
                var nombre = dernier - premier + 1;
                var go = new GameObject($"Lumiere {premier}-{dernier}");
                go.transform.SetParent(racine, false);
                go.transform.localPosition = new Vector3(Gabarits.GAUCHE_DU_DECOR, -(dernier + 1) * h, 0f);
                var lumiere = go.AddComponent<Light2D>();
                // Une lumière de sprite, et non de forme libre : sans le paquet 2D Common, URP ne
                // triangule pas l'intérieur d'une forme libre, qui ne s'éclaire alors que sur son
                // pourtour. Son cookie est un dégradé d'une unité de large, pivot en bas à gauche,
                // étiré à la nappe par l'échelle : `MARCHES_D_UNE_BANDE` rangées par palier, où la
                // lumière et sa teinte glissent du haut du palier à celles du suivant (le puits,
                // 2026-11-25) — pas de marche, la nappe s'arrête à une cassure. URP multiplie la
                // couleur de la lumière par le RGB du cookie : l'intensité est le maximum de la
                // nappe, le cookie le reste.
                var hauts = new Color[nombre];
                var bas = new Color[nombre];
                var plus = 1e-4f;
                for (var k = 0; k < nombre; k++)
                {
                    var index = premier + k;
                    hauts[k] = Ponderer(RegistreDArt.TeinteDuPalier(index), RegistreDArt.LumiereDuPalier(index));
                    var (lumiereDuBas, teinteDuBas) = RegistreDArt.LumiereAuBasDu(index);
                    bas[k] = Ponderer(teinteDuBas, lumiereDuBas);
                    plus = Mathf.Max(plus, Mathf.Max(hauts[k].maxColorComponent, bas[k].maxColorComponent));
                }
                var rangees = MARCHES_D_UNE_BANDE * nombre;
                var degrade = new Texture2D(1, rangees, TextureFormat.RGBA32, false)
                    { name = $"Lumiere de nappe {premier}-{dernier}", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < rangees; y++)
                {
                    // y = 0 en bas : le bas du dernier palier de la nappe.
                    var k = nombre - 1 - y / MARCHES_D_UNE_BANDE;
                    var t = (y % MARCHES_D_UNE_BANDE) / (float)(MARCHES_D_UNE_BANDE - 1);
                    var c = Color.Lerp(bas[k], hauts[k], t) / plus;
                    degrade.SetPixel(0, y, new Color(c.r, c.g, c.b, 1f));
                }
                degrade.Apply();
                var sprite = Sprite.Create(degrade, new Rect(0, 0, 1, rangees), Vector2.zero, 1f);
                degradesDesBandes.Add(sprite);
                degradesDesBandes.Add(degrade);
                go.transform.localScale = new Vector3(l, h / (float)MARCHES_D_UNE_BANDE, 1f);
                lumiere.lightType = Light2D.LightType.Sprite;
                lumiere.lightCookieSprite = sprite;
                lumiere.intensity = plus;
                lumiere.color = Color.white;
                nappes.Add(lumiere);
                paliersDesNappes.Add((premier, dernier));
            }
            EclairerLaRoche(vue);
        }

        static Color Ponderer(int teinte, double force)
        {
            var c = Briques.Couleur(teinte);
            return new Color(c.r * (float)force, c.g * (float)force, c.b * (float)force, 1f);
        }

        /// <summary>
        /// Une seule lumière pour toute la roche, quel que soit le nombre de bandes : le budget
        /// d'un téléphone compte les lumières, pas leur taille. Son cookie est un dégradé —
        /// la lumière de l'eau déborde sur la première pierre, puis s'éteint en descendant.
        /// </summary>
        void EclairerLaRoche(VueDeScene vue)
        {
            if (surLaRoche != null) UnityEngine.Object.Destroy(surLaRoche.gameObject);
            surLaRoche = null;
            if (vue.Roches.Count == 0) return;
            var h = Gabarits.HAUTEUR_DE_BANDE;
            var premiere = vue.Roches[0].Index;
            var derniere = vue.Roches[vue.Roches.Count - 1].Index;
            var go = new GameObject("Lumiere de roche");
            go.transform.SetParent(racine, false);
            go.transform.localPosition = new Vector3(Gabarits.GAUCHE_DU_DECOR, -(derniere + 1) * h, 0f);
            // Le sprite fait 1 × 16 unités : l'échelle l'étire à la largeur du décor et à la
            // hauteur de toute la roche.
            go.transform.localScale = new Vector3(Gabarits.LARGEUR_DU_DECOR, (derniere - premiere + 1) * h / (float)degrade.height, 1f);
            surLaRoche = go.AddComponent<Light2D>();
            surLaRoche.lightType = Light2D.LightType.Sprite;
            surLaRoche.lightCookieSprite = rectangleDegrade;
            surLaRoche.intensity = 1f;
            surLaRoche.color = Briques.Couleur(RegistreDArt.TEINTE_DE_ROCHE);
        }

        public void Dispose()
        {
            foreach (var o in degradesDesBandes) if (o != null) UnityEngine.Object.Destroy(o);
            UnityEngine.Object.Destroy(rectangle);
            UnityEngine.Object.Destroy(blanc);
            UnityEngine.Object.Destroy(rectangleDegrade);
            UnityEngine.Object.Destroy(degrade);
        }

        /// Allume les nappes qui touchent les bandes visibles, de `premiere` à `derniere`.
        public void Activer(int premiere, int derniere)
        {
            for (var i = 0; i < nappes.Count; i++)
                if (nappes[i] != null)
                    nappes[i].enabled = paliersDesNappes[i].Dernier >= premiere && paliersDesNappes[i].Premier <= derniere;
        }

        /// La force de la lumière au haut du `palier`, telle que sa nappe la rend : l'intensité
        /// fois le cookie. Pour les tests ; 0 hors de toute nappe.
        public float ForceAuHautDu(int palier)
        {
            for (var i = 0; i < nappes.Count; i++)
            {
                var (premier, dernier) = paliersDesNappes[i];
                if (palier < premier || palier > dernier || nappes[i] == null) continue;
                var texture = nappes[i].lightCookieSprite.texture;
                var y = (dernier - palier + 1) * MARCHES_D_UNE_BANDE - 1;
                return texture.GetPixel(0, y).maxColorComponent * nappes[i].intensity;
            }
            return 0f;
        }
    }
}
