using System;
using System.Linq;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les sprites du jeu, rangés par ce qu'ils montrent. Rempli par l'éditeur
    /// (« IdlePond ▸ Générer les sprites provisoires »), jamais à la main ; la scène le lit et
    /// ne charge aucun fichier. Toutes les lectures sont sûres : un dessin manquant rend
    /// null, et la scène ne dessine rien à cet endroit.
    /// </summary>
    public sealed class CatalogueDArt : ScriptableObject
    {
        [Serializable] public sealed class Images { public Sprite[] Sprites = Array.Empty<Sprite>(); }
        [Serializable] public sealed class MarqueDAssise { public string Assise; public Sprite[] ParStade = Array.Empty<Sprite>(); }
        [Serializable] public sealed class ImagesDEspece { public string Espece; public Sprite[] Images = Array.Empty<Sprite>(); }
        [Serializable] public sealed class DecorDAssise { public string Assise; public Sprite Fond; public Sprite Berge; public Sprite Rayons; }

        public Images[] Corps = Array.Empty<Images>();
        public MarqueDAssise[] Marques = Array.Empty<MarqueDAssise>();
        public ImagesDEspece[] Especes = Array.Empty<ImagesDEspece>();
        public DecorDAssise[] Decors = Array.Empty<DecorDAssise>();
        public Sprite Voile;
        public Sprite Eclat;

        static T A<T>(T[] tableau, int i) where T : class =>
            tableau != null && i >= 0 && i < tableau.Length ? tableau[i] : null;

        public Sprite CorpsDe(int stade, int image) => A(A(Corps, stade)?.Sprites, image);

        public Sprite MarqueDe(string assise, int stade) =>
            A(Marques?.FirstOrDefault(m => m != null && m.Assise == assise)?.ParStade, stade);

        /// Les images d'une espèce, cherchées UNE fois : la scène les garde ensuite sur chaque
        /// nageur, au lieu de refaire la recherche à chaque image.
        public Sprite[] ImagesDeLEspece(string espece) =>
            Especes?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images;

        public Sprite EspeceDe(string espece, int image) =>
            A(Especes?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images, image);

        /// Une assise sans dessin emprunte le décor de la première.
        public DecorDAssise DecorDe(string assise) =>
            Decors?.FirstOrDefault(d => d != null && d.Assise == assise) ?? A(Decors, 0);
    }
}
