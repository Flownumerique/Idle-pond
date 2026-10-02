using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la lumière (spec DA §2). Une ambiante faible, et une lumière par palier,
    /// rectangle de la largeur du décor, dont l'intensité suit la courbe de son assise : la
    /// lumière baisse en descendant (GDD §15.2). Seules les lumières des bandes visibles sont
    /// allumées — quatre au plus à l'écran, le budget d'un téléphone.
    /// </summary>
    public sealed class Eclairage : IDisposable
    {
        readonly Transform racine;
        readonly List<Light2D> parBande = new List<Light2D>();
        // Un pixel blanc, étiré au rectangle de la bande : voir `Dessiner`.
        readonly Texture2D blanc;
        readonly Sprite rectangle;

        public Light2D Ambiante { get; }
        public IReadOnlyList<Light2D> ParBande => parBande;

        public Eclairage(Transform parent)
        {
            blanc = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Lumiere blanche" };
            blanc.SetPixel(0, 0, Color.white);
            blanc.Apply();
            rectangle = Sprite.Create(blanc, new Rect(0, 0, 1, 1), Vector2.zero, 1f);
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
            foreach (var lumiere in parBande) if (lumiere != null) UnityEngine.Object.Destroy(lumiere.gameObject);
            parBande.Clear();
            var l = Gabarits.LARGEUR_DU_DECOR;
            var h = Gabarits.HAUTEUR_DE_BANDE;
            foreach (var p in vue.Paliers)
            {
                var go = new GameObject("Lumiere " + p.Index);
                go.transform.SetParent(racine, false);
                go.transform.localPosition = new Vector3(Gabarits.GAUCHE_DU_DECOR, -(p.Index + 1) * h, 0f);
                var lumiere = go.AddComponent<Light2D>();
                // Une lumière de sprite, et non de forme libre : sans le paquet 2D Common, URP ne
                // triangule pas l'intérieur d'une forme libre, qui ne s'éclaire alors que sur son
                // pourtour. Le pixel blanc, d'une unité et pivot en bas à gauche, s'étire au
                // rectangle de la bande par l'échelle.
                go.transform.localScale = new Vector3(l, h, 1f);
                lumiere.lightType = Light2D.LightType.Sprite;
                lumiere.lightCookieSprite = rectangle;
                lumiere.intensity = (float)RegistreDArt.LumiereDuPalier(p.Index);
                lumiere.color = Briques.Couleur(RegistreDArt.DecorDe(p.Assise).TeinteDeLumiere);
                parBande.Add(lumiere);
            }
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(rectangle);
            UnityEngine.Object.Destroy(blanc);
        }

        public void Activer(int premiere, int derniere)
        {
            for (var i = 0; i < parBande.Count; i++)
                if (parBande[i] != null) parBande[i].enabled = i >= premiere && i <= derniere;
        }
    }
}
