/*
 * Ce que le héros porte, et ce qu'il peut porter. Port de `Contenance.tsx`.
 *
 * GDD §2.4 : l'alerte est « un effet, pas un texte ». Passé le seuil, l'eau se
 * trouble et la barre perd sa couleur vive ; rien n'est écrit, rien ne s'ouvre,
 * aucun compte à rebours n'apparaît.
 */
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueContenance
    {
        private readonly Label _mana;
        private readonly Label _plafond;
        private readonly VisualElement _remplie;
        private readonly Label _debit;
        private readonly Label _plein;
        private readonly Label _bloque;

        public VueContenance()
        {
            Racine = Boite("panneau");

            var haut = Boite("ligne");
            _mana = Chiffre("", "mana-courant");
            _plafond = Chiffre("", "jour-tu", "petit");
            haut.Add(_mana);
            haut.Add(_plafond);
            Racine.Add(haut);

            var jauge = Boite("jauge");
            _remplie = Boite("jauge-remplie");
            jauge.Add(_remplie);
            Racine.Add(jauge);

            var bas = Boite("ligne");
            bas.style.marginTop = 8;
            _debit = Chiffre("", "jour-doux", "petit");
            _plein = Etiquette("", "trouble", "petit");
            bas.Add(_debit);
            bas.Add(_plein);
            Racine.Add(bas);

            _bloque = Etiquette("", "bloque");
            Racine.Add(_bloque);
        }

        public VisualElement Racine { get; }

        public void Rafraichir(EtatJeu etat)
        {
            var modele = Modeles.Contenance(etat);
            Ecrire(_mana, modele.Mana);
            Ecrire(_plafond, modele.Plafond);
            Largeur(_remplie, modele.Part);
            Racine.EnableInClassList("trouble-bord", modele.Trouble);
            _remplie.EnableInClassList("trouble-fond", modele.Trouble);
            Ecrire(_debit, modele.Debit);
            Afficher(_plein, modele.MessagePlein != null);
            Ecrire(_plein, modele.MessagePlein);
            Afficher(_bloque, modele.MessageBloque != null);
            Ecrire(_bloque, modele.MessageBloque);
        }
    }
}
