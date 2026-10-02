using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la caméra de la scène et sa texture (spec DA §2). La caméra dessine dans
    /// une `RenderTexture` de ≈240 px de large, sans lissage ; l'interface l'agrandit d'un
    /// facteur entier dans #scene. Une nouvelle taille de cadre recrée la texture et détruit
    /// l'ancienne.
    /// </summary>
    public sealed class RenduPixel : IDisposable
    {
        public Camera Camera { get; }
        public RenderTexture Texture { get; private set; }
        public DimensionsDuRendu Dimensions { get; private set; }
        public ChampDeCamera Champ { get; private set; }

        public RenduPixel(Transform parent)
        {
            var go = new GameObject("Camera pixel");
            go.transform.SetParent(parent, false);
            Camera = go.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Briques.Couleur(Palette.EAU_ABYSSE);
            Camera.enabled = false;
        }

        /// Vrai si la taille a changé (la texture a été recréée, sauf sans affichage).
        public bool Redimensionner(int largeurPx, int hauteurPx)
        {
            var d = Cadrage.Dimensions(largeurPx, hauteurPx);
            if (d == null || d == Dimensions) return false;
            Liberer();
            Dimensions = d;
            // Sans carte graphique (batch -nographics), `RenderTexture.Create` échoue et
            // journalise une erreur : on garde les dimensions, qui se calculent, sans texture.
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return true;
            // Profondeur et pochoir exigés par le rendu 2D (graphe de rendu), sans quoi les lumières ne s'appliquent pas.
            Texture = new RenderTexture(d.Largeur, d.Hauteur, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Rendu pixel",
            };
            Texture.Create();
            Camera.targetTexture = Texture;
            Camera.orthographicSize = d.Hauteur / 2f;
            Camera.enabled = true;
            return true;
        }

        /// La position de la caméra suit le champ : des demi-pixels seulement quand une
        /// dimension est impaire, ce qui laisse les bords du champ sur la grille.
        public void Cadrer(int nombreDeBandes)
        {
            if (Dimensions == null) return;
            Champ = Cadrage.Cadrer(Dimensions, nombreDeBandes);
            Camera.transform.position = new Vector3(Champ.Gauche + Champ.Largeur / 2f, Champ.Haut - Champ.Hauteur / 2f, -10f);
        }

        void Liberer()
        {
            if (Texture == null) return;
            Camera.targetTexture = null;
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }

        public void Dispose() => Liberer();
    }
}
