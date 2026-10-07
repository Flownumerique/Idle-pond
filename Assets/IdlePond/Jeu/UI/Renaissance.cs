using System;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;
using NoyauRenaissance = IdlePond.Noyau.Renaissance;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// La renaissance, en tête du tiroir de l'œuf (maquette du 2026-10-07, révision 2). Le
    /// héros ENTRE dans l'œuf (`Eclosion.tsx`).
    ///
    /// Une contrainte non évidente commande cet écran : le gain prévu ne s'affiche PAS en
    /// permanence. « Lire l'eau » est un nœud verbe de la branche Renaissance (§7.3), donc
    /// du contenu v0.4 ; l'afficher tout le temps dès maintenant viderait ce nœud de sa
    /// substance avant même de l'avoir écrit. Il se lit donc ici, au moment de décider —
    /// tiroir ouvert — et nulle part ailleurs : tiroir fermé, il n'est même pas calculé.
    ///
    /// Ce que le tiroir montre, en plus du Souffle : ce que la contenance gardera (l'acquis
    /// de séjour, que `Renaitre` fixe dans `ContenanceMana`), et ce qu'on emporte et laisse.
    /// Rentrer est le seul geste volontaire et sans retour du jeu (§10.1) : il passe par une
    /// confirmation, « Rentrer » ou « Rester ».
    /// </summary>
    public sealed class Renaissance
    {
        readonly Func<bool> estVisible;
        readonly VisualElement ouvrir;
        readonly VisualElement choix;
        readonly Label gain;
        readonly Label contenance;

        public Renaissance(VisualElement racine, Action surRenaissance, Func<bool> estVisible)
        {
            this.estVisible = estVisible;
            racine.AddToClassList("panneau");

            var tete = Elements.Conteneur("fiche-tete");
            var vignette = Elements.Conteneur("vignette vignette-oeuf");
            vignette.Add(new Icone(Icones.OEUF));
            tete.Add(vignette);
            var noms = Elements.Conteneur("fiche-noms");
            noms.Add(Elements.Texte("titre lg", E.RENTRER_DANS_L_OEUF));
            noms.Add(Elements.Texte("doux sm", E.TOUT_RESTERA_ICI));
            tete.Add(noms);
            racine.Add(tete);

            var grille = Elements.Conteneur("grille");
            var tuileDuSouffle = Elements.Conteneur("tuile tuile-souffle");
            tuileDuSouffle.Add(Elements.Texte("souffle-doux xs", E.SOUFFLE_LAISSE));
            gain = Elements.Texte("chiffre souffle xl", "", "gain-prevu");
            tuileDuSouffle.Add(gain);
            grille.Add(tuileDuSouffle);
            var tuileDeContenance = Elements.Conteneur("tuile");
            tuileDeContenance.Add(Elements.Texte("doux xs", E.CONTENANCE_GARDEE));
            contenance = Elements.Texte("chiffre mana xl");
            tuileDeContenance.Add(contenance);
            grille.Add(tuileDeContenance);
            racine.Add(grille);

            var charge = Elements.Conteneur("rangee entre renaissance-lignes");
            charge.Add(Elements.Texte("doux sm", E.CHARGE_GARDEE));
            charge.Add(Elements.Texte("chiffre densite sm", E.PLUS_DENSE));
            racine.Add(charge);

            var colonnes = Elements.Conteneur("deux-colonnes");
            colonnes.Add(Colonne(E.TU_EMPORTES, E.EMPORTE_SOUFFLE, E.EMPORTE_JOURNAL, E.EMPORTE_MARQUES));
            colonnes.Add(Colonne(E.TU_LAISSES, E.LAISSE_MANA, E.LAISSE_ESPECES, E.LAISSE_PROFONDEUR, E.LAISSE_TAILLE));
            racine.Add(colonnes);

            ouvrir = Elements.Conteneur("bouton bouton-souffle bouton-renaissance pressant");
            ouvrir.Add(Elements.Texte("base gras", E.RENTRER_DANS_L_OEUF));
            ouvrir.AddManipulator(new Clickable(() => Confirmer(true)));
            racine.Add(ouvrir);

            choix = Elements.Conteneur("rangee");
            var rentrer = Elements.Conteneur("bouton bouton-souffle bouton-large");
            rentrer.Add(Elements.Texte("base gras", E.RENTRER));
            rentrer.AddManipulator(new Clickable(() =>
            {
                surRenaissance();
                Confirmer(false);
            }));
            var rester = Elements.Conteneur("bouton bouton-renaissance");
            rester.Add(Elements.Texte("base", E.RESTER));
            rester.AddManipulator(new Clickable(() => Confirmer(false)));
            choix.Add(rentrer);
            choix.Add(rester);
            racine.Add(choix);
            racine.Add(Elements.Texte("tu sm renaissance-conseil", E.RESTER_OU_PARTIR));

            Confirmer(false);
        }

        static VisualElement Colonne(string titre, params string[] lignes)
        {
            var colonne = Elements.Conteneur("colonne");
            colonne.Add(Elements.Texte("gras sm", titre));
            foreach (var ligne in lignes) colonne.Add(Elements.Texte("doux sm", ligne));
            return colonne;
        }

        void Confirmer(bool demande)
        {
            Elements.Montrer(choix, demande);
            Elements.Montrer(ouvrir, !demande);
        }

        public void Rafraichir(EtatJeu etat)
        {
            if (!estVisible()) return;
            Elements.Poser(gain, Format.Montant(NoyauRenaissance.GainDeSoufflePrevu(etat)));
            var acquis = (int)Math.Round(etat.Cycle.AcquisDeSejour * 100, MidpointRounding.AwayFromZero);
            Elements.Poser(contenance, Format.Remplir(E.POURCENT, acquis));
        }
    }
}
