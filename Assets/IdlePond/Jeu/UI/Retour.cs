using System;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Ce qui s'est passé pendant l'absence (`Retour.tsx`).
    ///
    /// On ne punit jamais l'absence, et on ne la fête pas non plus : la ligne dit ce qui a
    /// été crédité, puis s'en va. Le rapport détaillé est un nœud verbe de la branche
    /// Entretien (§7.3), donc v0.4 — pas ici. Un toucher l'écarte ; rien ne le force.
    /// </summary>
    public sealed class Retour
    {
        readonly VisualElement racine;
        readonly Label ligne;

        public Retour(VisualElement racine, Action surFermeture)
        {
            this.racine = racine;
            // L'enveloppe porte le fond plein : tout ce qui n'est pas la scène doit être
            // opaque, la caméra ne dessinant que dans son rectangle.
            racine.AddToClassList("retour");
            var carte = Elements.Conteneur("retour-ligne");
            ligne = Elements.Texte("doux sm");
            carte.Add(ligne);
            carte.AddManipulator(new Clickable(() =>
            {
                surFermeture();
                Afficher(null);
            }));
            racine.Add(carte);
            Elements.Montrer(racine, false);
        }

        /// Null efface la ligne : l'absence était trop brève pour valoir d'être annoncée,
        /// ou le joueur l'a écartée.
        public void Afficher(AbsenceCreditee absence)
        {
            Elements.Montrer(racine, absence != null);
            if (absence == null) return;
            Elements.Poser(ligne, Format.Remplir(
                E.RETOUR_ABSENCE,
                Format.NomDeLAssiseCapitale(IdlePond.Noyau.Donnees.Assises.Toutes[0].Id),
                Format.Duree(absence.SecondesCreditees)));
        }
    }
}
