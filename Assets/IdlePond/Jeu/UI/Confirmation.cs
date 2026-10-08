using System;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Un geste qui efface se confirme : le premier toucher arme, un second dans
    /// `DELAI_MS` confirme. Passé le délai, on repart de zéro. Pur — l'instant est donné
    /// par l'appelant — pour se tester sans moteur.
    /// </summary>
    public sealed class Confirmation
    {
        public const long DELAI_MS = 3000;

        long? armeeA;

        /// Vrai si ce toucher confirme ; faux s'il ne fait qu'armer.
        public bool Toucher(long maintenantMs)
        {
            if (EnAttente(maintenantMs))
            {
                armeeA = null;
                return true;
            }
            armeeA = maintenantMs;
            return false;
        }

        public bool EnAttente(long maintenantMs) => armeeA.HasValue && maintenantMs - armeeA.Value <= DELAI_MS;

        public void Annuler() => armeeA = null;
    }

    /// <summary>
    /// Un bouton qui efface : le premier toucher le fait changer de mot, le second dans
    /// les trois secondes agit. Le délai passe par le planificateur d'UI Toolkit (une
    /// interface ne lit pas l'horloge) : à son terme, la confirmation est annulée — d'où
    /// l'instant constant donné à `Confirmation`, qui n'a alors qu'à savoir si elle est armée.
    /// </summary>
    public sealed class BoutonAConfirmer
    {
        public readonly VisualElement Racine;
        readonly Confirmation confirmation = new Confirmation();
        readonly Label texte;
        readonly string libelle;
        IVisualElementScheduledItem minuterie;

        public BoutonAConfirmer(string libelle, string nom, string classe, Action agir)
        {
            this.libelle = libelle;
            Racine = Elements.Conteneur("bouton " + classe, nom);
            texte = Elements.Texte("base titre", libelle);
            Racine.Add(texte);
            Racine.AddManipulator(new Clickable(() =>
            {
                if (confirmation.Toucher(0))
                {
                    Desarmer();
                    agir();
                    return;
                }
                Elements.Poser(texte, E.TOUCHER_POUR_CONFIRMER);
                Racine.AddToClassList("a-confirmer");
                minuterie?.Pause();
                minuterie = Racine.schedule.Execute(Desarmer).StartingIn(Confirmation.DELAI_MS);
            }));
        }

        public void Desarmer()
        {
            confirmation.Annuler();
            minuterie?.Pause();
            Elements.Poser(texte, libelle);
            Racine.RemoveFromClassList("a-confirmer");
        }
    }
}
