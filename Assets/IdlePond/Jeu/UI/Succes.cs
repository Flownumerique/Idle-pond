using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// L'écran des succès (§8.3), `Succes.tsx`.
    ///
    /// | État    | Ce que le joueur voit                    |
    /// |---------|------------------------------------------|
    /// | Ouvert  | Nom + condition + barre de progression   |
    /// | Fermé   | Nom seul, condition masquée              |
    /// | Secret  | Emplacement vide, rien d'autre           |
    ///
    /// Verrouillage par assise : un succès n'est listé, quel que soit son état, que lorsque
    /// son assise est atteinte. PAS de compteur global de secrets — des emplacements vides
    /// par assise, ce qui dit qu'il y a quelque chose là sans dire combien il en reste
    /// ailleurs.
    ///
    /// Les trois états sont groupés plutôt qu'entrelacés, et ce n'est pas cosmétique :
    /// égrenés dans l'ordre du registre, neuf emplacements secrets consécutifs se lisent
    /// comme un défaut d'affichage, et quarante lignes de conditions écrasent la mare qui,
    /// elle, en fait quatre. Ce qui est ARRIVÉ passe donc devant : la narration est une
    /// récompense distribuée à la cadence idle (§4.2), pas une liste de courses.
    ///
    /// La liste ne se reconstruit que lorsqu'un succès tombe : le dictionnaire des succès
    /// obtenus est remplacé à ce moment-là et jamais autrement, donc sa référence suffit à
    /// savoir si la liste a changé. Entre deux, seules les barres de progression bougent.
    /// </summary>
    public sealed class Succes
    {
        readonly VisualElement contenu;
        readonly Label titre;
        readonly List<(SuccesAffichable Entree, VisualElement Barre)> enCours =
            new List<(SuccesAffichable, VisualElement)>();
        readonly Dictionary<VisualElement, float> largeurs = new Dictionary<VisualElement, float>();

        object signature;
        readonly string assise = Assises.Toutes[0].Id;

        public Succes(VisualElement racine)
        {
            racine.AddToClassList("panneau");
            titre = Elements.Texte("titre doux lg", Format.Remplir(E.SUCCES_TITRE, Format.NomDeLAssise(assise)));
            racine.Add(titre);
            contenu = Elements.Conteneur("succes-contenu");
            racine.Add(contenu);
        }

        public void Rafraichir(EtatJeu etat)
        {
            if (!ReferenceEquals(signature, etat.Permanent.Succes))
            {
                signature = etat.Permanent.Succes;
                Reconstruire(etat);
            }
            foreach (var (entree, barre) in enCours)
            {
                var progression = RegleDesSucces.ProgressionVersLeSucces(etat, entree.Succes);
                if (progression == null) continue;
                // Un centième de pourcent ne se voit pas : on n'écrit pas à chaque tick.
                var part = (float)System.Math.Round(progression.Value * 100, 1);
                if (largeurs.TryGetValue(barre, out var avant) && avant == part) continue;
                largeurs[barre] = part;
                Elements.RegleLaLargeur(barre, part / 100.0);
            }
        }

        void Reconstruire(EtatJeu etat)
        {
            contenu.Clear();
            enCours.Clear();
            largeurs.Clear();

            var liste = RegleDesSucces.SuccesListables(etat, assise);
            var acquis = new List<SuccesAffichable>();
            var ouverts = new List<SuccesAffichable>();
            var fermes = new List<SuccesAffichable>();
            var secrets = 0;
            foreach (var entree in liste)
            {
                if (entree.Acquis) acquis.Add(entree);
                else if (entree.Visibilite == VisibiliteDeSucces.Ouvert) ouverts.Add(entree);
                else if (entree.Visibilite == VisibiliteDeSucces.Ferme) fermes.Add(entree);
                else secrets++;
            }

            if (acquis.Count > 0)
            {
                var groupe = Elements.Conteneur("liste");
                // Le plus récent d'abord : la dernière chose arrivée est celle qu'on cherche.
                for (var i = acquis.Count - 1; i >= 0; i--) groupe.Add(Acquis(acquis[i]));
                contenu.Add(groupe);
            }
            else
            {
                contenu.Add(Elements.Texte("tu sm", E.SUCCES_RIEN_ENCORE));
            }

            if (ouverts.Count > 0)
            {
                contenu.Add(Elements.Texte("tu xs sous-titre", E.SUCCES_EN_CHEMIN));
                var groupe = Elements.Conteneur("liste");
                foreach (var entree in ouverts) groupe.Add(EnCours(etat, entree));
                contenu.Add(groupe);
            }

            if (fermes.Count > 0)
            {
                contenu.Add(Elements.Texte("tu xs sous-titre", E.SUCCES_PLUS_LOIN));
                var groupe = Elements.Conteneur("rangee wrap");
                foreach (var entree in fermes)
                {
                    var puce = Elements.Conteneur("puce");
                    puce.Add(Elements.Texte("tu xs titre", Textes.DuSucces(entree.Succes.Id).Nom));
                    groupe.Add(puce);
                }
                contenu.Add(groupe);
            }

            if (secrets > 0)
            {
                // Rien d'écrit : un emplacement vide dit qu'il y a quelque chose, pas quoi.
                // Le seul libellé, en infobulle, est « Emplacements vides ».
                var marque = Elements.Texte("tu xs sous-titre", E.SUCCES_MARQUE_DES_VIDES);
                marque.tooltip = E.SUCCES_EMPLACEMENTS_VIDES;
                contenu.Add(marque);
                var groupe = Elements.Conteneur("rangee wrap");
                for (var i = 0; i < secrets; i++) groupe.Add(Elements.Conteneur("emplacement-vide"));
                contenu.Add(groupe);
            }
        }

        static VisualElement Acquis(SuccesAffichable entree)
        {
            var texte = Textes.DuSucces(entree.Succes.Id);
            var carte = Elements.Conteneur("succes-acquis");
            carte.Add(Elements.Texte("titre base", texte.Nom));
            carte.Add(Elements.Texte("titre tu sm italique", texte.Rapport));
            return carte;
        }

        VisualElement EnCours(EtatJeu etat, SuccesAffichable entree)
        {
            var texte = Textes.DuSucces(entree.Succes.Id);
            var carte = Elements.Conteneur("succes-en-cours");
            carte.Add(Elements.Texte("titre doux base", texte.Nom));
            carte.Add(Elements.Texte("tu xs", texte.Condition));
            // Sans seuil mesurable (creux au complet), pas de barre : une barre fixe à zéro
            // dirait une progression qui n'existe pas.
            if (RegleDesSucces.ProgressionVersLeSucces(etat, entree.Succes) != null)
            {
                var rail = Elements.Conteneur("succes-rail");
                var barre = Elements.Conteneur("succes-barre");
                rail.Add(barre);
                carte.Add(rail);
                enCours.Add((entree, barre));
            }
            return carte;
        }
    }
}
