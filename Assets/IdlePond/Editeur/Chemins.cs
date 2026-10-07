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

        public static string CorpsDuHeros(int stade, int image) => $"Heros/corps-s{stade}-i{image}.png";
        public static string MarqueDAssise(string assise, int stade) => $"Heros/marques/{assise}-s{stade}.png";
        public static string ImageDEspece(string espece, int image) => $"Especes/{espece}-i{image}.png";
        public static string Fond(string assise) => $"Fonds/{assise}/fond.png";
        public static string Roche(string assise) => $"Fonds/{assise}/roche.png";
        public static string Rayons(string assise) => $"Fonds/{assise}/rayons.png";
        public static string Berge(string assise) => $"Fonds/{assise}/berge.png";

        /// Les espèces des paliers livrés : la Noue s'arrête au palier 5, l'épinoche vit au 6.
        public static IEnumerable<Espece> EspecesLivrees() => Especes.Toutes.Where(e => e.Palier < Assises.PALIERS_LIVRES);
    }
}
