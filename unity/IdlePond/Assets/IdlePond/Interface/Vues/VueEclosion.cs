/*
 * L'éclosion. Le héros ENTRE dans l'œuf. Port de `Eclosion.tsx`.
 *
 * Le gain prévu ne s'affiche PAS en permanence : « Lire l'eau » est un nœud
 * verbe de la v0.4. Il se lit ici, au moment de décider, et nulle part ailleurs.
 */
using System;
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueEclosion
    {
        private readonly Button _ouvrir;
        private readonly VisualElement _panneau;
        private readonly Label _phrase;
        private readonly Label _libelleFoi;
        private readonly Label _foi;
        private readonly Label _libelleDensite;
        private readonly Label _densite;
        private readonly Label _arbitrage;
        private readonly Button _rentrer;
        private readonly Button _rester;
        private bool _ouvert;

        public VueEclosion(Action surEclosion)
        {
            Racine = Boite();

            _ouvrir = Bouton(() => Basculer(true), "", "bouton-discret", "bouton-large", "petit");
            Racine.Add(_ouvrir);

            _panneau = Boite("panneau", "panneau-foi", "pile-3");
            _phrase = Texte("", "titre-3");
            _panneau.Add(_phrase);

            var releve = Boite("pile-1");
            var ligneFoi = Boite("ligne");
            _libelleFoi = Etiquette("", "jour-doux", "petit");
            _foi = Chiffre("", "foi", "petit");
            ligneFoi.Add(_libelleFoi);
            ligneFoi.Add(_foi);
            var ligneDensite = Boite("ligne");
            _libelleDensite = Etiquette("", "jour-doux", "petit");
            _densite = Chiffre("", "densite", "petit");
            ligneDensite.Add(_libelleDensite);
            ligneDensite.Add(_densite);
            releve.Add(ligneFoi);
            releve.Add(ligneDensite);
            _panneau.Add(releve);

            _arbitrage = Etiquette("", "jour-tu", "petit");
            _panneau.Add(_arbitrage);

            var boutons = Boite("ligne");
            _rentrer = Bouton(() =>
            {
                surEclosion();
                Basculer(false);
            }, "", "bouton-foi", "petit");
            _rentrer.style.flexGrow = 1;
            _rester = Bouton(() => Basculer(false), "", "bouton-rester", "petit");
            boutons.Add(_rentrer);
            boutons.Add(_rester);
            _panneau.Add(boutons);
            Racine.Add(_panneau);

            Basculer(false);
        }

        public VisualElement Racine { get; }

        private void Basculer(bool ouvert)
        {
            _ouvert = ouvert;
            Afficher(_ouvrir, !ouvert);
            Afficher(_panneau, ouvert);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var modele = Modeles.Eclosion(etat);
            Ecrire(_ouvrir, modele.Bouton);
            // Bloqué : partir est la seule chose qui fasse encore descendre. Le bouton
            // prend la couleur de la Foi — une invitation, jamais une alerte.
            _ouvrir.EnableInClassList("bouton-foi", modele.Bloque);
            if (!_ouvert) return;
            Ecrire(_phrase, modele.Phrase);
            Ecrire(_libelleFoi, modele.LibelleFoi);
            Ecrire(_foi, modele.Foi);
            Ecrire(_libelleDensite, modele.LibelleDensite);
            Ecrire(_densite, modele.Densite);
            Ecrire(_arbitrage, modele.Arbitrage);
            Ecrire(_rentrer, modele.Rentrer);
            Ecrire(_rester, modele.Rester);
        }
    }
}
