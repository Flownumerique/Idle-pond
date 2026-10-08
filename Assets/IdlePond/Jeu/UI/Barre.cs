using System;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// La barre du haut (spec du 2026-10-07, §1) : ce que l'écran garde toujours sous les
    /// yeux. Le mana et son débit, le Souffle, la jauge de contenance.
    ///
    /// La jauge porte en fantôme, à sa droite, la part de la contenance que l'acquis de séjour
    /// ajoute — `Contenance = ContenanceMana × (1 + AcquisDeSejour)` : ce que rester a déjà
    /// gagné, et que la renaissance fixera. Comme dans `Contenance`, l'eau trouble éteint la
    /// jauge sans rien écrire (GDD §2.4) ; à saturation le débit dit zéro, et pourquoi.
    /// </summary>
    public sealed class Barre
    {
        readonly Label mana;
        readonly Label debit;
        readonly Label souffle;
        readonly VisualElement remplissage;
        readonly VisualElement fantome;

        /// `surReglages` : la roue, à droite du Souffle, ouvre et referme le tiroir des réglages.
        public Barre(VisualElement racine, Action surReglages)
        {
            var haut = Elements.Conteneur("barre-haut");
            var gauche = Elements.Conteneur("barre-mana");
            var goutte = new Icone(Icones.GOUTTE);
            goutte.AddToClassList("barre-goutte");
            gauche.Add(goutte);
            mana = Elements.Texte("chiffre mana barre-valeur", "", "mana-valeur");
            debit = Elements.Texte("doux sm barre-debit");
            gauche.Add(mana);
            gauche.Add(debit);
            haut.Add(gauche);

            var pastille = Elements.Conteneur("pastille-souffle");
            var etincelle = new Icone(Icones.ETINCELLE);
            etincelle.AddToClassList("pastille-etincelle");
            pastille.Add(etincelle);
            souffle =Elements.Texte("chiffre souffle base", "", "souffle-valeur");
            pastille.Add(souffle);
            pastille.Add(Elements.Texte("souffle-doux xs", E.SOUFFLE));
            var droite = Elements.Conteneur("barre-droite");
            droite.Add(pastille);
            var roue = Elements.Conteneur("barre-reglages", "roue-reglages");
            roue.Add(new Icone(Icones.ENGRENAGE));
            roue.AddManipulator(new Clickable(surReglages));
            droite.Add(roue);
            haut.Add(droite);
            racine.Add(haut);

            var rail = Elements.Conteneur("jauge barre-jauge");
            fantome = Elements.Conteneur("jauge-fantome");
            remplissage = Elements.Conteneur("jauge-remplissage");
            rail.Add(fantome);
            rail.Add(remplissage);
            racine.Add(rail);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var plein = Economie.EstSature(etat);
            var trouble = Economie.EauTroublee(etat);

            Elements.Poser(mana, Format.Montant(etat.Cycle.ManaCourant));
            Elements.Poser(debit, plein
                ? E.CAPTATION_ARRETEE
                : Format.Remplir(E.DEBIT, Format.Montant(Economie.ProductionTotaleParSeconde(etat))));
            Elements.Marquer(debit, "trouble", plein);
            Elements.Poser(souffle, Format.Montant(etat.Permanent.Souffle));

            Elements.RegleLaLargeur(remplissage, Economie.PartDeContenance(etat));
            Elements.Marquer(remplissage, "eau-trouble", trouble);
            var acquis = etat.Cycle.AcquisDeSejour;
            Elements.RegleLaLargeur(fantome, acquis > 0 ? acquis / (1 + acquis) : 0);
        }
    }
}
