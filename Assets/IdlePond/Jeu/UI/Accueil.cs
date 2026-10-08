using System;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// L'écran d'accueil (spec du 2026-10-08) : une couche posée sur toute l'interface, la
    /// mare dessinée vivant derrière un voile. Le titre, ce qu'on sait de la partie, et
    /// quatre gestes : continuer (ou commencer), recommencer, régler, quitter.
    ///
    /// La partie avance pendant ce temps — un idle ne s'arrête pas pour un menu. Ouvrir les
    /// réglages efface la couche le temps du tiroir ; le refermer la ramène.
    /// </summary>
    public sealed class Accueil
    {
        const long DUREE_DU_FONDU_MS = 400;

        readonly VisualElement racine;
        readonly Func<bool> mouvementReduit;
        readonly Label principal;
        readonly VisualElement resume;
        readonly Label lieu;
        readonly Label mana;
        readonly Label absence;
        readonly BoutonAConfirmer nouvelle;

        /// Vrai tant que le joueur n'est pas entré dans la mare.
        public bool Ouvert { get; private set; }

        public Accueil(VisualElement racine, Action nouvellePartie, Action reglages,
            bool quitterDisponible, Action quitter, string version, Func<bool> mouvementReduit)
        {
            this.racine = racine;
            this.mouvementReduit = mouvementReduit;
            racine.AddToClassList("accueil");

            var colonne = Elements.Conteneur("accueil-colonne");
            colonne.Add(Elements.Texte("titre accueil-titre", E.TITRE));
            colonne.Add(Elements.Texte("doux base accueil-sous-titre", E.ACCUEIL_SOUS_TITRE));

            resume = Elements.Conteneur("carte accueil-resume", "accueil-resume");
            lieu = Elements.Texte("titre lg");
            mana = Elements.Texte("chiffre mana base");
            absence = Elements.Texte("doux sm");
            resume.Add(lieu);
            resume.Add(mana);
            resume.Add(absence);
            colonne.Add(resume);

            var bouton = Elements.Conteneur("bouton accueil-principal", "accueil-principal");
            principal = Elements.Texte("base titre accueil-principal-texte", "", "accueil-principal-libelle");
            bouton.Add(principal);
            bouton.AddManipulator(new Clickable(Entrer));
            colonne.Add(bouton);

            nouvelle = new BoutonAConfirmer(E.NOUVELLE_PARTIE, "accueil-nouvelle", "bouton-sobre", () =>
            {
                nouvellePartie();
                Entrer();
            });
            colonne.Add(nouvelle.Racine);

            colonne.Add(BoutonSobre(E.REGLAGES, "accueil-reglages", reglages));
            if (quitterDisponible) colonne.Add(BoutonSobre(E.QUITTER, "accueil-quitter", quitter));

            racine.Add(colonne);
            racine.Add(Elements.Texte("tu xs accueil-version", Format.Remplir(E.VERSION, version)));
            Elements.Montrer(racine, false);
        }

        static VisualElement BoutonSobre(string libelle, string nom, Action agir)
        {
            var bouton = Elements.Conteneur("bouton bouton-sobre", nom);
            bouton.Add(Elements.Texte("base titre", libelle));
            bouton.AddManipulator(new Clickable(agir));
            return bouton;
        }

        public void Ouvrir(ResumeDeLaPartie r)
        {
            Elements.Poser(principal, r.Principal);
            Elements.Montrer(resume, r.Lieu != null);
            Elements.Poser(lieu, r.Lieu ?? "");
            Elements.Poser(mana, r.Mana ?? "");
            Elements.Poser(absence, r.Absence ?? "");
            Elements.Montrer(absence, r.Absence != null);
            Elements.Montrer(nouvelle.Racine, r.NouvellePartiePossible);
            nouvelle.Desarmer();
            Ouvert = true;
            racine.RemoveFromClassList("accueil-sortant");
            Elements.Montrer(racine, true);
        }

        /// Le tiroir des réglages ouvert depuis l'accueil passe devant : la couche s'efface
        /// le temps qu'il soit ouvert.
        public void Recouvrir(bool tiroirOuvert)
        {
            if (!Ouvert) return;
            if (tiroirOuvert) nouvelle.Desarmer();
            Elements.Montrer(racine, !tiroirOuvert);
        }

        /// Entrer dans la mare : la couche se fond, puis disparaît.
        void Entrer()
        {
            if (!Ouvert) return;
            Ouvert = false;
            nouvelle.Desarmer();
            racine.AddToClassList("accueil-sortant");
            var duree = mouvementReduit() ? 0 : DUREE_DU_FONDU_MS;
            racine.schedule.Execute(() =>
            {
                if (!Ouvert) Elements.Montrer(racine, false);
            }).StartingIn(duree);
        }
    }
}
