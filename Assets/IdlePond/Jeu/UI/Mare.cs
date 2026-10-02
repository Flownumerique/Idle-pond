using System;
using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le lieu, et les bancs qui l'habitent (`Mare.tsx`).
    ///
    /// L'écran ne dit jamais « palier » ni « assise » (§3) : il dit le nom propre du lieu
    /// et une profondeur en brasses.
    ///
    /// Le joueur ne choisit jamais quelle espèce va où — le placement est fixé par l'auteur
    /// (§4.2). Il débloque, il monte des crans, il creuse. L'effet est immédiat : il n'y a
    /// plus de population qui rejoint lentement une cible.
    ///
    /// Une carte par espèce, créée une fois puis montrée ou cachée : c'est le nombre de
    /// creux ouverts qui décide de ce qui se voit, pas une liste reconstruite.
    /// </summary>
    public sealed class Mare
    {
        sealed class Carte
        {
            public string Id;
            public VisualElement Racine;
            public Label Nom;
            public Label Profondeur;
            public VisualElement Detail;
            public Label Niveau;
            public Label Production;
            public BoutonDAchat Achat;
        }

        readonly Action<string> surDeblocage;
        readonly Action<string> surNiveau;
        readonly Action<string> surCaptation;
        readonly VisualElement liste;
        readonly Dictionary<string, Carte> cartes = new Dictionary<string, Carte>();
        readonly BoutonDAchat creuser;

        // Le clic lit l'état d'AU MOMENT du clic : le niveau de l'espèce décide si l'on
        // débloque ou si l'on monte, et il a pu changer depuis le dernier tick.
        EtatJeu dernier;

        public Mare(VisualElement racine, Action<string> surDeblocage, Action<string> surNiveau,
            Action surCreusement, Action<string> surCaptation)
        {
            this.surDeblocage = surDeblocage;
            this.surNiveau = surNiveau;
            this.surCaptation = surCaptation;

            racine.AddToClassList("panneau");
            racine.Add(Elements.Texte("titre doux lg", Format.NomDeLAssise(Assises.Toutes[0].Id)));
            liste = Elements.Conteneur("liste");
            racine.Add(liste);

            creuser = new BoutonDAchat("bouton-creuser", E.CREUSER, surCreusement);
            racine.Add(creuser.Racine);
        }

        public void Rafraichir(EtatJeu etat)
        {
            dernier = etat;
            var mana = etat.Cycle.ManaCourant;
            var limite = Economie.Contenance(etat);

            foreach (var espece in Especes.Toutes)
            {
                // Une espèce que la version livrée n'ouvre jamais n'a pas de carte du tout.
                if (espece.Palier >= etat.LimiteDeContenu) continue;
                if (!cartes.TryGetValue(espece.Id, out var carte)) carte = Creer(espece);

                var visible = espece.Palier < etat.Cycle.PaliersOuverts;
                Elements.Montrer(carte.Racine, visible);
                if (!visible) continue;

                var niveau = NiveauDe(etat, espece.Id);
                var coutDeLEspece = niveau == 0
                    ? Economie.CoutDeDeblocage(etat, espece)
                    : Economie.CoutDeNiveau(etat, espece, niveau);

                Elements.Poser(carte.Nom, niveau == 0 ? E.ESPECE_QUI_S_ATTARDE : Format.NomDeLEspece(espece.Id));
                Elements.Marquer(carte.Nom, "tu", niveau == 0);
                Elements.Montrer(carte.Detail, niveau > 0);
                if (niveau > 0)
                {
                    Elements.Poser(carte.Niveau, niveau.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    Elements.Poser(carte.Production, Format.Remplir(E.DEBIT, Format.Montant(Economie.ProductionDeLEspece(etat, espece))));
                }
                Elements.Poser(carte.Achat.Libelle, niveau == 0 ? E.VERBE_DEBLOQUER : E.MONTER);
                Elements.Poser(carte.Achat.Cout, Format.Cout(coutDeLEspece));
                carte.Achat.Regler(mana.Gte(coutDeLEspece) && coutDeLEspece.Lte(limite));
            }

            if (Economie.ToutEstCreuse(etat))
            {
                Elements.Poser(creuser.Libelle, E.PLUS_DE_ROCHE);
                Elements.Montrer(creuser.Cout, false);
                creuser.Regler(false);
            }
            else
            {
                var cout = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
                Elements.Poser(creuser.Libelle, E.CREUSER);
                Elements.Montrer(creuser.Cout, true);
                Elements.Poser(creuser.Cout, Format.Cout(cout));
                creuser.Regler(mana.Gte(cout) && cout.Lte(limite));
            }
        }

        static int NiveauDe(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee ? vivante.Niveau : 0;

        Carte Creer(Espece espece)
        {
            var carte = new Carte { Id = espece.Id };
            carte.Racine = Elements.Conteneur("carte carte-espece", "espece-" + espece.Id);

            var haut = Elements.Conteneur("rangee entre");
            carte.Nom = Elements.Texte("titre base");
            carte.Profondeur = Elements.Texte("chiffre tu sm", Format.Profondeur(espece.Palier));
            haut.Add(carte.Nom);
            haut.Add(carte.Profondeur);
            carte.Racine.Add(haut);

            carte.Detail = Elements.Conteneur("rangee");
            carte.Niveau = Elements.Texte("chiffre doux base");
            carte.Production = Elements.Texte("chiffre mana base");
            var captation = Elements.Conteneur("lien");
            captation.Add(carte.Production);
            captation.AddManipulator(new Clickable(() => surCaptation(espece.Id)));
            carte.Detail.Add(carte.Niveau);
            carte.Detail.Add(captation);
            carte.Racine.Add(carte.Detail);

            carte.Achat = new BoutonDAchat("bouton-espece", E.VERBE_DEBLOQUER, () =>
            {
                if (dernier == null) return;
                if (NiveauDe(dernier, espece.Id) == 0) surDeblocage(espece.Id);
                else surNiveau(espece.Id);
            });
            carte.Racine.Add(carte.Achat.Racine);

            liste.Add(carte.Racine);
            cartes[espece.Id] = carte;
            return carte;
        }
    }
}
