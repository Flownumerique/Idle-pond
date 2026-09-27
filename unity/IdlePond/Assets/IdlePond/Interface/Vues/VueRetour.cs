/*
 * Ce qui s'est passé pendant l'absence. Port de `Retour.tsx`.
 *
 * On ne punit jamais l'absence, et on ne la fête pas non plus : la ligne dit ce
 * qui a été crédité, puis s'en va au premier geste.
 */
using System;
using IdlePond.Etat;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueRetour
    {
        private readonly Button _bouton;

        public VueRetour(Action surFermeture)
        {
            _bouton = Bouton(surFermeture, "", "retour", "bouton-large", "petit");
            Polices.Texte(_bouton);
            Racine = _bouton;
        }

        public VisualElement Racine { get; }

        public void Rafraichir(RetourAffiche retour)
        {
            Afficher(_bouton, retour != null);
            if (retour != null) Ecrire(_bouton, Modeles.Retour(retour.SecondesCreditees));
        }
    }
}
