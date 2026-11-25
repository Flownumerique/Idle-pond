using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Editeur
{
    /// Où vit chaque dessin, relatif à `Art/`. Un vrai dessin prend la place d'un provisoire
    /// en portant le même nom (spec DA §5).
    public static class Chemins
    {
        public const string ART = "Assets/IdlePond/Art";
        public const string CATALOGUE = ART + "/Catalogue.asset";
        public const string VOILE = "Eau/voile.png";
        public const string ECLAT = "Eau/eclat.png";
        /// Une couche des caustiques de l'eau (2026-11-25).
        public static string Caustiques(int couche) => $"Eau/caustiques-{couche}.png";
        /// Le matériau non éclairé d'URP 2D (chemin absolu, dans le paquet, pas sous `Art/`).
        public const string SANS_LUMIERE = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        public static string CorpsDuHeros(int stade, int image) => $"Heros/corps-s{stade}-i{image}.png";
        public static string MarqueDAssise(string assise, int stade) => $"Heros/marques/{assise}-s{stade}.png";
        public static string Runes(int stade) => $"Heros/eveil/runes-s{stade}.png";
        public static string Voile(int stade, int image) => $"Heros/eveil/voile-s{stade}-i{image}.png";
        public static string Anneau(int stade) => $"Heros/eveil/anneau-s{stade}.png";
        public static string Aura(int stade) => $"Heros/eveil/aura-s{stade}.png";
        public const string ETOILE = "Heros/eveil/etoile.png";
        public const string OEUF = "Heros/renouveau/oeuf.png";
        public const string OEUF_FENDU = "Heros/renouveau/oeuf-fendu.png";
        public static string Larve(int image) => $"Heros/renouveau/larve-i{image}.png";
        public static string ImageDEspece(string espece, int image) => $"Especes/{espece}-i{image}.png";
        public static string ImageDuRepresentant(string espece, int image) => $"Especes/representants/{espece}-i{image}.png";
        public static string ImageDeFaune(string nom, int image) => $"Faune/{nom}-i{image}.png";
        /// Le puits (2026-11-25) : l'eau du `rang`-ième palier du lieu, une tranche d'un dégradé
        /// qui court sur tout le lieu ; ses parois ; sa corniche ; et le sol du fond du puits.
        public static string Eau(string assise, int rang) => $"Fonds/{assise}/eau-{rang}.png";
        public static string Parois(string assise, int rang) => $"Fonds/{assise}/parois-{rang}.png";
        public static string Corniche(string assise, int rang) => $"Fonds/{assise}/corniche-{rang}.png";
        public static string Lit(string assise) => $"Fonds/{assise}/lit.png";
        /// Les rangs des paliers d'un lieu : 0 pour son premier.
        public static IEnumerable<int> Rangs(string assise) => Enumerable.Range(0, Assises.Toutes.First(a => a.Id == assise).NombreDePaliers);
        public static int PremierPalier(string assise) => Assises.Toutes.First(a => a.Id == assise).IndexPremierPalier;
        public static string Roche(string assise) => $"Fonds/{assise}/roche.png";
        public static string Rayons(string assise) => $"Fonds/{assise}/rayons.png";
        public static string Berge(string assise) => $"Fonds/{assise}/berge.png";
        public static string Ambiance(string assise) => $"Fonds/{assise}/ambiance.png";
        /// La lèvre de la vasque d'un lieu, en haut de son premier palier (2026-11-25).
        public static string Seuil(string assise) => $"Fonds/{assise}/seuil.png";

        /// Les espèces des paliers livrés : la Noue s'arrête au palier 5, l'épinoche vit au 6.
        public static IEnumerable<Espece> EspecesLivrees() => Especes.Toutes.Where(e => e.Palier < Assises.PALIERS_LIVRES);
    }
}
