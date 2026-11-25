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
        [Serializable] public sealed class ImagesDeFaune { public string Nom; public Sprite[] Images = Array.Empty<Sprite>(); }
        [Serializable] public sealed class DecorDAssise { public string Assise; public Sprite Fond; public Sprite Roche; public Sprite Berge; public Sprite Rayons; public Sprite Ambiance; public Sprite Seuil; }

        /// L'éveil d'un stade (bible v0.4) : ses runes, les deux images de son voile, l'anneau,
        /// l'aura. Tous sauf les runes sont sur le cadre d'éveil, plus grand que le corps.
        [Serializable] public sealed class EveilDUnStade { public Sprite Runes; public Sprite[] Voile = Array.Empty<Sprite>(); public Sprite Anneau; public Sprite Aura; }

        public Images[] Corps = Array.Empty<Images>();
        public EveilDUnStade[] Eveils = Array.Empty<EveilDUnStade>();
        public Sprite Etoile;
        /// Le renouveau (bible v0.4, d'après da-1), joué à chaque renaissance : l'œuf, l'œuf
        /// fendu, les deux images de la larve.
        public Sprite Oeuf;
        public Sprite OeufFendu;
        public Sprite[] Larve = Array.Empty<Sprite>();
        public MarqueDAssise[] Marques = Array.Empty<MarqueDAssise>();
        public ImagesDEspece[] Especes = Array.Empty<ImagesDEspece>();
        /// Le représentant de chaque tribu (Codex v1.4) : quatre images de nage, comme le héros.
        public ImagesDEspece[] Representants = Array.Empty<ImagesDEspece>();
        /// La faune d'ambiance (Codex v1.3) : une entrée par bête, ses images.
        public ImagesDeFaune[] Faune = Array.Empty<ImagesDeFaune>();
        public DecorDAssise[] Decors = Array.Empty<DecorDAssise>();
        public Sprite Voile;
        public Sprite Eclat;

        static T A<T>(T[] tableau, int i) where T : class =>
            tableau != null && i >= 0 && i < tableau.Length ? tableau[i] : null;

        public Sprite CorpsDe(int stade, int image) => A(A(Corps, stade)?.Sprites, image);

        public EveilDUnStade EveilDe(int stade) => A(Eveils, stade);

        public Sprite MarqueDe(string assise, int stade) =>
            A(Marques?.FirstOrDefault(m => m != null && m.Assise == assise)?.ParStade, stade);

        /// Les images d'une espèce, cherchées UNE fois : la scène les garde ensuite sur chaque
        /// nageur, au lieu de refaire la recherche à chaque image.
        public Sprite[] ImagesDeLEspece(string espece) =>
            Especes?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images;

        /// Les images du représentant d'une tribu, ou null s'il n'est pas dessiné.
        public Sprite[] ImagesDuRepresentant(string espece) =>
            Representants?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images;

        /// Les images d'une bête de la faune d'ambiance, ou null.
        public Sprite[] ImagesDeLaFaune(string nom) => Faune?.FirstOrDefault(f => f != null && f.Nom == nom)?.Images;

        /// Son portrait : sa première image (la fiche du tiroir Espèces).
        public Sprite PortraitDe(string espece) => A(ImagesDuRepresentant(espece), 0);

        public Sprite EspeceDe(string espece, int image) =>
            A(Especes?.FirstOrDefault(e => e != null && e.Espece == espece)?.Images, image);

        /// Une assise sans dessin emprunte le décor de la première.
        public DecorDAssise DecorDe(string assise) =>
            Decors?.FirstOrDefault(d => d != null && d.Assise == assise) ?? A(Decors, 0);
    }
}
