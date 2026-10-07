using System;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// L'étiquette posée sur la mare, en haut à gauche : le nom du lieu et jusqu'où l'on a
    /// creusé. Jamais « palier » (§3) : une profondeur en brasses.
    /// </summary>
    public sealed class Lieu
    {
        readonly Label nom;
        readonly Label profondeur;

        public Lieu(VisualElement racine)
        {
            nom = Elements.Texte("titre base");
            profondeur = Elements.Texte("doux sm lieu-profondeur");
            racine.Add(nom);
            racine.Add(profondeur);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var plusBas = Math.Max(0, etat.Cycle.PaliersOuverts - 1);
            Elements.Poser(nom, Format.NomDeLAssiseCapitale(Assises.DuPalier(plusBas).Id));
            Elements.Poser(profondeur, Format.Profondeur(plusBas));
        }
    }

    /// <summary>
    /// Le bouton « Creuser plus bas », posé sur la mare au-dessus du dock : c'est le geste
    /// qui fait progresser, il se voit sans ouvrir de tiroir. Il suit la règle des achats —
    /// éteint quand on ne peut pas payer, jamais caché pour ça — et ne disparaît que quand
    /// il n'y a plus de roche à ouvrir.
    /// </summary>
    public sealed class Creusement
    {
        readonly VisualElement racine;
        readonly BoutonDAchat bouton;

        public Creusement(VisualElement racine, Action surCreusement)
        {
            this.racine = racine;
            bouton = new BoutonDAchat("bouton-creuser", E.CREUSER, surCreusement);
            racine.Add(bouton.Racine);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var toutEstCreuse = Economie.ToutEstCreuse(etat);
            Elements.Montrer(racine, !toutEstCreuse);
            if (toutEstCreuse) return;
            var cout = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
            Elements.Poser(bouton.Cout, Format.Cout(cout));
            bouton.Progresser(etat.Cycle.ManaCourant, cout);
            bouton.Regler(etat.Cycle.ManaCourant.Gte(cout) && cout.Lte(Economie.Contenance(etat)));
        }
    }
}
