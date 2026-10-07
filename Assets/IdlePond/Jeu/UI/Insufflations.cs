using System;
using System.Collections.Generic;
using System.Globalization;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;
using Registre = IdlePond.Noyau.Donnees.Insufflations;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Ce que tu insuffles — noyau v1.0 §4.1 : « l'écran d'améliorations du jeu, comme le
    /// veut la convention du genre ». Permanent, payé en Souffle. `Benedictions.tsx`.
    ///
    /// Spec 2026-09-17 [D7] : ouvert tout le temps. Le Souffle n'est crédité qu'à la
    /// renaissance, donc ce que cet écran permet ne change qu'en rentrant dans l'œuf ; mais
    /// un joueur qui revient d'une absence ne doit pas trouver une porte fermée.
    ///
    /// Le registre entier est toujours affiché, sans filtre — comme le simulateur, qui
    /// considère lui aussi toutes les insufflations sans distinction. La donnée est petite
    /// (une entrée par espèce, plus une globale) : ce n'est pas un enjeu d'affichage ou de
    /// performance, et les cartes sont créées une fois.
    /// </summary>
    public sealed class Insufflations
    {
        sealed class Carte
        {
            public Insufflation Insufflation;
            public Label Rang;
            public BoutonDAchat Achat;
        }

        readonly Label souffle;
        readonly List<Carte> cartes = new List<Carte>();

        // Le clic lit l'état d'au moment du clic, comme `Mare` : le prix a pu bouger.
        EtatJeu dernier;

        public Insufflations(VisualElement racine, Action<string> surInsufflation)
        {
            racine.AddToClassList("panneau");

            var haut = Elements.Conteneur("rangee entre");
            haut.Add(Elements.Texte("titre doux lg", E.INSUFFLATIONS_TITRE));
            souffle = Elements.Texte("chiffre souffle sm", "", "insufflations-souffle");
            haut.Add(souffle);
            racine.Add(haut);

            var liste = Elements.Conteneur("liste");
            foreach (var insufflation in Registre.Toutes)
            {
                var carte = new Carte { Insufflation = insufflation };
                var racineDeCarte = Elements.Conteneur("carte");
                var entete = Elements.Conteneur("rangee entre");
                var estGlobale = insufflation.Id == Registre.GLOBALE_ID;
                var nom = estGlobale
                    ? Textes.INSUFFLATION_GLOBALE.Nom
                    : Format.Remplir(E.INSUFFLER_UNE_ESPECE, Format.NomDeLEspece(insufflation.Espece));
                var effet = estGlobale ? Textes.INSUFFLATION_GLOBALE.Effet : Textes.INSUFFLATION_CIBLEE.Effet;
                entete.Add(Elements.Texte("titre base", nom));
                carte.Rang = Elements.Texte("chiffre tu xs");
                entete.Add(carte.Rang);
                racineDeCarte.Add(entete);
                racineDeCarte.Add(Elements.Texte("tu xs", effet));

                var id = insufflation.Id;
                carte.Achat = new BoutonDAchat("bouton-souffle", E.INSUFFLER, () => surInsufflation(id));
                racineDeCarte.Add(carte.Achat.Racine);
                liste.Add(racineDeCarte);
                cartes.Add(carte);
            }
            racine.Add(liste);
        }

        public void Rafraichir(EtatJeu etat)
        {
            dernier = etat;
            var reserve = etat.Permanent.Souffle;
            Elements.Poser(souffle, Format.Remplir(E.SOUFFLE_EN_RESERVE, Format.Montant(reserve)));

            foreach (var carte in cartes)
            {
                var rang = Economie.RangDInsufflation(etat, carte.Insufflation.Id);
                var prix = Economie.CoutDInsufflation(etat, carte.Insufflation);
                Elements.Poser(carte.Rang, rang == 0 ? E.JAMAIS : Format.Remplir(E.FOIS, rang.ToString(CultureInfo.InvariantCulture)));
                Elements.Poser(carte.Achat.Cout, Format.Remplir(E.SOUFFLE_EN_RESERVE, Format.Cout(prix)));
                carte.Achat.Progresser(reserve, prix);
                carte.Achat.Regler(reserve.Gte(prix));
            }
        }
    }
}
