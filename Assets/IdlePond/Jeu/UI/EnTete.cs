using System.Globalization;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// L'en-tête (`App.tsx`) : le titre, le Souffle, et le nombre de retours dans l'œuf.
    /// Ce sont les deux seules mesures permanentes que l'écran garde toujours sous les
    /// yeux ; le reste se lit dans le panneau qui le concerne.
    /// </summary>
    public sealed class EnTete
    {
        readonly Label souffle;
        readonly Label retours;

        public EnTete(VisualElement racine)
        {
            racine.Add(Elements.Texte("titre xl", E.TITRE));

            var mesures = Elements.Conteneur("entete-mesures");
            souffle = Mesure(mesures, E.SOUFFLE, "souffle-valeur", "chiffre souffle");
            retours = Mesure(mesures, E.RETOURS_DANS_L_OEUF, "retours-valeur", "chiffre");
            racine.Add(mesures);
        }

        static Label Mesure(VisualElement parent, string nom, string nomDeLaValeur, string classesDeLaValeur)
        {
            var mesure = Elements.Conteneur("mesure");
            mesure.Add(Elements.Texte("tu sm", nom));
            var valeur = Elements.Texte(classesDeLaValeur + " sm", "", nomDeLaValeur);
            mesure.Add(valeur);
            parent.Add(mesure);
            return valeur;
        }

        public void Rafraichir(EtatJeu etat)
        {
            Elements.Poser(souffle, Format.Montant(etat.Permanent.Souffle));
            Elements.Poser(retours, etat.Permanent.NombreDeRenaissances.ToString(CultureInfo.InvariantCulture));
        }
    }
}
