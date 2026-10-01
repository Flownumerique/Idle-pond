using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Ce que le héros porte, et ce qu'il peut porter (`Contenance.tsx`).
    ///
    /// §6.4 : la contenance limite le STOCK, pas la production. Le blocage est doux —
    /// quand elle sature, le joueur continue à monter des niveaux, il ne peut plus que
    /// descendre. L'écran doit donc dire « plein », pas « bloqué ».
    ///
    /// GDD §2.4, et c'est la règle qui commande cet écran : l'alerte est « un effet, pas un
    /// texte ». Passé le seuil, l'eau se trouble et la barre perd sa couleur vive ; rien
    /// n'est écrit, rien ne s'ouvre, aucun compte à rebours n'apparaît. Un décompte
    /// affiché ferait de la seule pénalité douce du jeu un minuteur. Ici l'effet est la
    /// classe `trouble`, que le .uss traduit en couleur.
    /// </summary>
    public sealed class Contenance
    {
        readonly VisualElement racine;
        readonly Label mana;
        readonly Label plafond;
        readonly VisualElement remplissage;
        readonly Label debit;
        readonly Label saturee;
        readonly Label productionDuHeros;
        readonly Label sourceDuHeros;
        readonly Label bloque;

        public Contenance(VisualElement racine)
        {
            this.racine = racine;
            racine.AddToClassList("carte");

            var haut = Elements.Conteneur("rangee entre");
            mana = Elements.Texte("chiffre mana xxl", "", "mana-valeur");
            plafond = Elements.Texte("chiffre tu base");
            haut.Add(mana);
            haut.Add(plafond);
            racine.Add(haut);

            var rail = Elements.Conteneur("jauge");
            remplissage = Elements.Conteneur("jauge-remplissage");
            rail.Add(remplissage);
            racine.Add(rail);

            var milieu = Elements.Conteneur("rangee entre contenance-ligne");
            debit = Elements.Texte("chiffre doux base");
            // Saturation : « la captation s'arrête. Il dépense encore, il ne gagne plus. »
            // Afficher un débit pendant que la jauge ne bouge pas ferait mentir le seul
            // chiffre de cet écran ; on écrit donc zéro, et on le dit.
            saturee = Elements.Texte("trouble base", E.CAPTATION_ARRETEE);
            milieu.Add(debit);
            milieu.Add(saturee);
            racine.Add(milieu);

            // §8.2 : « la contrepartie obligatoire d'un effet appliqué silencieusement. » Le
            // total ci-dessus porte la mutation du héros sans l'expliquer ; cette ligne dit
            // d'où vient l'écart avec la somme des espèces que la table de captation détaille.
            var bas = Elements.Conteneur("rangee entre");
            productionDuHeros = Elements.Texte("chiffre tu sm");
            sourceDuHeros = Elements.Texte("tu sm");
            bas.Add(productionDuHeros);
            bas.Add(sourceDuHeros);
            racine.Add(bas);

            bloque = Elements.Texte("doux base contenance-bloque", E.PLUS_DE_QUOI_PORTER);
            racine.Add(bloque);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var limite = Economie.Contenance(etat);
            var trouble = Economie.EauTroublee(etat);
            var plein = Economie.EstSature(etat);

            Elements.Poser(mana, Format.Montant(etat.Cycle.ManaCourant));
            Elements.Poser(plafond, Format.Remplir(E.SUR_LA_CONTENANCE, Format.Montant(limite)));
            Elements.RegleLaLargeur(remplissage, Economie.PartDeContenance(etat));
            Elements.Marquer(racine, "eau-trouble", trouble);
            Elements.Marquer(remplissage, "eau-trouble", trouble);

            Elements.Poser(debit, plein
                ? Format.Remplir(E.DEBIT, 0)
                : Format.Remplir(E.DEBIT, Format.Montant(Economie.ProductionTotaleParSeconde(etat))));
            Elements.Montrer(saturee, plein);

            Elements.Poser(productionDuHeros, Format.Remplir(
                E.DONT_DEBIT, plein ? "0" : Format.Montant(Economie.ProductionDuHeros(etat))));
            Elements.Poser(sourceDuHeros, Format.SourceDuTerme(Economie.DetailDuHeros(etat)[0].Source));

            Elements.Montrer(bloque, Economie.EstBloque(etat));
        }
    }
}
