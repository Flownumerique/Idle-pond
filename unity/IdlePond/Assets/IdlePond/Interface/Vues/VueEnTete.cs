/* L'en-tête : le nom du jeu, la Foi, les retours dans l'œuf. Port de l'en-tête de `App.tsx`. */
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueEnTete
    {
        private readonly Label _titre;
        private readonly Label _libelleFoi;
        private readonly Label _foi;
        private readonly Label _libelleEclosions;
        private readonly Label _eclosions;

        public VueEnTete()
        {
            Racine = Boite("en-tete", "ligne");
            _titre = Texte("", "titre");
            Racine.Add(_titre);

            var releves = Boite("ligne-gauche");
            var foi = Boite("releve");
            _libelleFoi = Etiquette("", "jour-tu", "petit");
            _foi = Chiffre("", "foi");
            foi.Add(_libelleFoi);
            foi.Add(_foi);
            var eclosions = Boite("releve");
            _libelleEclosions = Etiquette("", "jour-tu", "petit");
            _eclosions = Chiffre("");
            eclosions.Add(_libelleEclosions);
            eclosions.Add(_eclosions);
            releves.Add(foi);
            releves.Add(eclosions);
            Racine.Add(releves);
        }

        public VisualElement Racine { get; }

        public void Rafraichir(EtatJeu etat)
        {
            var modele = Modeles.EnTete(etat);
            Ecrire(_titre, modele.Titre);
            Ecrire(_libelleFoi, modele.LibelleFoi);
            Ecrire(_foi, modele.Foi);
            Ecrire(_libelleEclosions, modele.LibelleEclosions);
            Ecrire(_eclosions, modele.Eclosions);
        }
    }
}
