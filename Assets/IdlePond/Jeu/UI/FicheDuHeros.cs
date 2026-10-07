using System;
using System.Globalization;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// La fiche du héros, en tête du tiroir « Toi » (maquette du 2026-10-07, révision 2) : sa
    /// vignette, ce que rester lui a déjà gagné, et quatre chiffres en grille.
    ///
    /// Elle remplace l'ancien panneau de contenance : le mana et sa jauge vivent dans la
    /// `Barre`. La règle du §8.2 tient toujours — la tuile « Toi seul » rend au héros sa part
    /// du débit total, que la table de captation des espèces ne porte pas.
    ///
    /// « Rester t'a gagné » est l'acquis de séjour : la part que la contenance prendra à la
    /// renaissance (`Contenance = ContenanceMana × (1 + AcquisDeSejour)`). Une valeur, pas
    /// une jauge : l'acquis n'a pas de plafond à montrer.
    /// </summary>
    public sealed class FicheDuHeros
    {
        readonly VisualElement racine;
        readonly Label libelle;
        readonly Label sejour;
        readonly Label captation;
        readonly Label contenance;
        readonly Label debitPropre;
        readonly Label retours;
        readonly Label bloque;

        public FicheDuHeros(VisualElement racine)
        {
            this.racine = racine;

            var tete = Elements.Conteneur("fiche-tete");
            var vignette = Elements.Conteneur("vignette vignette-heros");
            vignette.Add(new Icone(Icones.TOI));
            tete.Add(vignette);
            var noms = Elements.Conteneur("fiche-noms");
            noms.Add(Elements.Texte("titre lg", E.HEROS));
            libelle = Elements.Texte("doux sm", "", "heros-libelle");
            noms.Add(libelle);
            tete.Add(noms);
            racine.Add(tete);

            var carte = Elements.Conteneur("carte");
            var ligne = Elements.Conteneur("rangee entre");
            ligne.Add(Elements.Texte("doux sm", E.GAIN_DU_SEJOUR));
            sejour = Elements.Texte("chiffre mana base");
            ligne.Add(sejour);
            carte.Add(ligne);
            racine.Add(carte);

            var grille = Elements.Conteneur("grille");
            captation = Tuile(grille, E.FICHE_CAPTATION);
            contenance = Tuile(grille, E.FICHE_CONTENANCE);
            debitPropre = Tuile(grille, E.FICHE_DEBIT_PROPRE);
            retours = Tuile(grille, E.FICHE_RETOURS);
            racine.Add(grille);

            bloque = Elements.Texte("doux base contenance-bloque", E.PLUS_DE_QUOI_PORTER);
            racine.Add(bloque);
        }

        static Label Tuile(VisualElement grille, string nom)
        {
            var tuile = Elements.Conteneur("tuile");
            tuile.Add(Elements.Texte("doux xs", nom));
            var valeur = Elements.Texte("chiffre lg");
            tuile.Add(valeur);
            grille.Add(tuile);
            return valeur;
        }

        public void Rafraichir(EtatJeu etat)
        {
            var plein = Economie.EstSature(etat);
            Elements.Poser(libelle, Format.LibelleDuHeros(etat.Cycle.NiveauDuHeros));
            var acquis = (int)Math.Round(etat.Cycle.AcquisDeSejour * 100, MidpointRounding.AwayFromZero);
            Elements.Poser(sejour, Format.Remplir(E.POURCENT_DE_CONTENANCE, acquis));
            // Saturé, on ne capte plus rien : le débit dit zéro, comme la barre du haut.
            Elements.Poser(captation, Format.Remplir(E.DEBIT, plein ? "0" : Format.Montant(Economie.ProductionTotaleParSeconde(etat))));
            Elements.Poser(contenance, Format.Montant(Economie.Contenance(etat)));
            Elements.Poser(debitPropre, Format.Remplir(E.DEBIT, plein ? "0" : Format.Montant(Economie.ProductionDuHeros(etat))));
            Elements.Poser(retours, etat.Permanent.NombreDeRenaissances.ToString(CultureInfo.InvariantCulture));
            Elements.Marquer(racine, "eau-trouble", Economie.EauTroublee(etat));
            Elements.Montrer(bloque, Economie.EstBloque(etat));
        }
    }
}
