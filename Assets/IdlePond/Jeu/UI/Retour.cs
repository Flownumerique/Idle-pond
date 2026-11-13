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
        readonly Label plafonne;
        readonly Label mana;
        readonly Label contenance;
        readonly Label plein;
        readonly Label descente;
        readonly Label succes;

        public Retour(VisualElement racine, Action surFermeture)
        {
            this.racine = racine;
            // L'enveloppe porte le fond plein : tout ce qui n'est pas la scène doit être
            // opaque, la caméra ne dessinant que dans son rectangle.
            racine.AddToClassList("retour");
            var carte = Elements.Conteneur("retour-ligne");
            ligne = Elements.Texte("doux sm");
            carte.Add(ligne);
            // Le détail : une ligne par chose qui a bougé, montrée seulement si elle a bougé.
            mana = Elements.Texte("chiffre mana base retour-detail", "", "retour-mana");
            contenance = Elements.Texte("sm retour-detail", "", "retour-contenance");
            plein = Elements.Texte("souffle-doux sm retour-detail", "", "retour-plein");
            descente = Elements.Texte("sm retour-detail", "", "retour-descente");
            succes = Elements.Texte("doux sm retour-detail", "", "retour-succes");
            plafonne = Elements.Texte("doux xs retour-detail", "", "retour-plafonne");
            foreach (var detail in new[] { mana, contenance, plein, descente, succes, plafonne })
            {
                Elements.Montrer(detail, false);
                carte.Add(detail);
            }
            carte.Add(Elements.Texte("doux xs retour-detail", E.RETOUR_FERMER));
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
        public void Afficher(AbsenceCreditee absence, RapportDeRetour rapport = null)
        {
            Elements.Montrer(racine, absence != null);
            Detailler(absence == null ? null : rapport);
            if (absence == null) return;
            Elements.Poser(ligne, Format.Remplir(
                E.RETOUR_ABSENCE,
                Format.NomDeLAssiseCapitale(IdlePond.Noyau.Donnees.Assises.Toutes[0].Id),
                Format.Duree(absence.SecondesCreditees)));
        }

        void Detailler(RapportDeRetour rapport)
        {
            Ligne(mana, rapport != null && rapport.ManaGagne.Gt(0),
                () => Format.Remplir(E.RETOUR_MANA, Format.Montant(rapport.ManaGagne)));
            Ligne(contenance, rapport != null && rapport.ContenanceGagnee.Gt(0),
                () => Format.Remplir(E.RETOUR_CONTENANCE, Format.Montant(rapport.ContenanceGagnee)));
            Ligne(plein, rapport != null && rapport.StockPlein, () => E.RETOUR_STOCK_PLEIN);
            Ligne(descente, rapport != null && (rapport.PeutCreuser || rapport.DescenteRouverte),
                () => rapport.PeutCreuser ? E.RETOUR_PEUT_CREUSER : E.RETOUR_DESCENTE_ROUVERTE);
            Ligne(succes, rapport != null && rapport.Succes.Count > 0,
                () => rapport.Succes.Count == 1 ? E.RETOUR_UN_SUCCES : Format.Remplir(E.RETOUR_SUCCES, rapport.Succes.Count));
            Ligne(plafonne, rapport != null && rapport.Plafonnee,
                () => Format.Remplir(E.RETOUR_PLAFONNE, Format.Duree(rapport.SecondesAbsentes), Format.Duree(rapport.SecondesCreditees)));
        }

        static void Ligne(Label etiquette, bool visible, Func<string> texte)
        {
            Elements.Montrer(etiquette, visible);
            if (visible) Elements.Poser(etiquette, texte());
        }
    }
}
