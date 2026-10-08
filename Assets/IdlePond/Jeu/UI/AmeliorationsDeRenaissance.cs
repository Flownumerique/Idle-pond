using System;
using System.Collections.Generic;
using System.Globalization;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;
using Registre = IdlePond.Noyau.Donnees.AmeliorationsDeRenaissance;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Les améliorations de renaissance — noyau v1.0 §4.1 : « l'écran d'améliorations du jeu, comme le
    /// veut la convention du genre ». Permanent, payé en Souffle. `Benedictions.tsx`.
    ///
    /// Spec 2026-09-17 [D7] : ouvert tout le temps. Le Souffle n'est crédité qu'à la
    /// renaissance, donc ce que cet écran permet ne change qu'en rentrant dans l'œuf ; mais
    /// un joueur qui revient d'une absence ne doit pas trouver une porte fermée.
    ///
    /// Le registre entier est toujours affiché, sans filtre — comme le simulateur, qui
    /// considère lui aussi toutes les améliorations sans distinction. La donnée est petite
    /// (une entrée par espèce, plus une globale) : ce n'est pas un enjeu d'affichage ou de
    /// performance, et les cartes sont créées une fois.
    /// </summary>
    public sealed class AmeliorationsDeRenaissance
    {
        sealed class Carte
        {
            public AmeliorationDeRenaissance AmeliorationDeRenaissance;
            public Label Rang;
            public BoutonDAchat Achat;
        }

        readonly Label souffle;
        readonly List<Carte> cartes = new List<Carte>();

        // Le clic lit l'état d'au moment du clic, comme `Mare` : le prix a pu bouger.
        EtatJeu dernier;

        public AmeliorationsDeRenaissance(VisualElement racine, Action<string> surAmelioration)
        {
            racine.AddToClassList("panneau");

            var haut = Elements.Conteneur("rangee entre");
            haut.Add(Elements.Texte("titre doux lg", E.AMELIORATIONS_TITRE));
            souffle = Elements.Texte("chiffre souffle sm", "", "ameliorations-souffle");
            haut.Add(souffle);
            racine.Add(haut);

            var liste = Elements.Conteneur("liste");
            foreach (var amelioration in Registre.Toutes)
            {
                var carte = new Carte { AmeliorationDeRenaissance = amelioration };
                var racineDeCarte = Elements.Conteneur("carte");
                var entete = Elements.Conteneur("rangee entre");
                var estGlobale = amelioration.Id == Registre.GLOBALE_ID;
                var nom = estGlobale
                    ? Textes.AMELIORATION_GLOBALE.Nom
                    : Format.Remplir(E.AMELIORER_UNE_ESPECE, Format.NomDeLEspece(amelioration.Espece));
                var effet = estGlobale ? Textes.AMELIORATION_GLOBALE.Effet : Textes.AMELIORATION_CIBLEE.Effet;
                entete.Add(Elements.Texte("titre base", nom));
                carte.Rang = Elements.Texte("chiffre tu xs");
                entete.Add(carte.Rang);
                racineDeCarte.Add(entete);
                racineDeCarte.Add(Elements.Texte("tu xs", effet));

                var id = amelioration.Id;
                carte.Achat = new BoutonDAchat("bouton-souffle", E.AMELIORER, () => surAmelioration(id));
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
                var rang = Economie.RangDAmelioration(etat, carte.AmeliorationDeRenaissance.Id);
                var prix = Economie.CoutDAmelioration(etat, carte.AmeliorationDeRenaissance);
                Elements.Poser(carte.Rang, rang == 0 ? E.JAMAIS : Format.Remplir(E.FOIS, rang.ToString(CultureInfo.InvariantCulture)));
                Elements.Poser(carte.Achat.Cout, Format.Remplir(E.SOUFFLE_EN_RESERVE, Format.Cout(prix)));
                carte.Achat.Progresser(reserve, prix);
                carte.Achat.Regler(reserve.Gte(prix));
            }
        }
    }
}
