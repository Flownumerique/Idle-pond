using System;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;
using NoyauRenaissance = IdlePond.Noyau.Renaissance;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// La renaissance. Le héros ENTRE dans l'œuf (`Eclosion.tsx`).
    ///
    /// Une contrainte non évidente commande cet écran : le gain prévu ne s'affiche PAS en
    /// permanence. « Lire l'eau » est un nœud verbe de la branche Renaissance (§7.3), donc
    /// du contenu v0.4 ; l'afficher tout le temps dès maintenant viderait ce nœud de sa
    /// substance avant même de l'avoir écrit.
    ///
    /// Il se lit donc ici, au moment de décider, et nulle part ailleurs : tant que le
    /// panneau est fermé, le gain n'est même pas calculé.
    /// </summary>
    public sealed class Renaissance
    {
        readonly VisualElement ferme;
        readonly VisualElement ouvert;
        readonly VisualElement ouvrir;
        readonly Label gain;

        bool estOuvert;

        public Renaissance(VisualElement racine, Action surRenaissance)
        {
            racine.AddToClassList("panneau");

            ouvrir = Elements.Conteneur("bouton bouton-renaissance");
            ouvrir.Add(Elements.Texte("base", E.RENTRER_DANS_L_OEUF));
            ouvrir.AddManipulator(new Clickable(() => Basculer(true)));
            ferme = ouvrir;
            racine.Add(ouvrir);

            ouvert = Elements.Conteneur("carte carte-souffle");
            ouvert.Add(Elements.Texte("titre lg", E.TOUT_RESTERA_ICI));

            var lignes = Elements.Conteneur("renaissance-lignes");
            var ligneDuGain = Elements.Conteneur("rangee entre");
            ligneDuGain.Add(Elements.Texte("doux sm", E.SOUFFLE_LAISSE));
            gain = Elements.Texte("chiffre souffle sm", "", "gain-prevu");
            ligneDuGain.Add(gain);
            lignes.Add(ligneDuGain);
            var ligneDeLaCharge = Elements.Conteneur("rangee entre");
            ligneDeLaCharge.Add(Elements.Texte("doux sm", E.CHARGE_GARDEE));
            ligneDeLaCharge.Add(Elements.Texte("chiffre densite sm", E.PLUS_DENSE));
            lignes.Add(ligneDeLaCharge);
            ouvert.Add(lignes);

            ouvert.Add(Elements.Texte("tu sm", E.RESTER_OU_PARTIR));

            var choix = Elements.Conteneur("rangee");
            var rentrer = Elements.Conteneur("bouton bouton-souffle bouton-large");
            rentrer.Add(Elements.Texte("souffle base", E.RENTRER));
            rentrer.AddManipulator(new Clickable(() =>
            {
                surRenaissance();
                Basculer(false);
            }));
            var rester = Elements.Conteneur("bouton");
            rester.Add(Elements.Texte("doux base", E.RESTER));
            rester.AddManipulator(new Clickable(() => Basculer(false)));
            choix.Add(rentrer);
            choix.Add(rester);
            ouvert.Add(choix);
            racine.Add(ouvert);

            Basculer(false);
        }

        void Basculer(bool valeur)
        {
            estOuvert = valeur;
            Elements.Montrer(ouvert, valeur);
            Elements.Montrer(ouvrir, !valeur);
        }

        public void Rafraichir(EtatJeu etat)
        {
            // Fermé, le bouton dit seulement s'il y a une raison de l'appuyer : le blocage
            // doux (§6.4) lui donne la couleur du Souffle, rien de plus.
            Elements.Marquer(ferme, "pressant", Economie.EstBloque(etat));
            if (!estOuvert) return;
            Elements.Poser(gain, Format.Montant(NoyauRenaissance.GainDeSoufflePrevu(etat)));
        }
    }
}
