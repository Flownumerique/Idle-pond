using System.Linq;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le décor (spec DA §4) : une bande d'eau par palier ouvert, pavée du fond de
    /// son assise ; sur la première, la berge et ses racines (l'inversion d'échelle) et les
    /// rayons du jour. Une assise sans dessin emprunte celui de la Noue ; la lumière, elle,
    /// vient d'`Eclairage`.
    /// </summary>
    public sealed class Decor
    {
        readonly Transform racine;
        readonly CatalogueDArt catalogue;

        public Decor(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Decor").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public void Dessiner(VueDeScene vue)
        {
            Briques.Vider(racine);
            if (catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                var art = catalogue.DecorDe(p.Assise);
                if (art == null) continue;
                var bas = -(p.Index + 1) * Gabarits.HAUTEUR_DE_BANDE;
                Briques.Paver(racine, "Bande " + p.Index, art.Fond, Gabarits.GAUCHE_DU_DECOR, bas,
                    Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.FOND);
                if (art.Ambiance != null)
                    Briques.Paver(racine, "Ambiance " + p.Index, art.Ambiance, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.AMBIANCE);
                if (p.Index != 0) continue;
                if (RegistreDArt.DecorDe(p.Assise).Berge && art.Berge != null)
                    Briques.Paver(racine, "Berge", art.Berge, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.BORD);
                if (art.Rayons != null)
                    Briques.Poser(racine, "Rayons", art.Rayons, (Gabarits.LARGEUR_VISEE - Gabarits.LARGEUR_DE_BERGE) / 2,
                        -Gabarits.HAUTEUR_DES_RAYONS, OrdreDeRendu.RAYONS);
            }
            // La lèvre de la vasque de chaque lieu atteint en perçant (2026-11-25), qu'il soit
            // déjà ouvert ou encore dans la roche qu'on voit venir.
            foreach (var index in vue.Paliers.Select(p => (p.Index, p.Assise)).Concat(vue.Roches.Select(r => (r.Index, r.Assise))))
            {
                if (!Lieux.OuvreUnLieu(index.Index)) continue;
                var seuil = catalogue.DecorDe(index.Assise)?.Seuil;
                if (seuil != null && catalogue.DecorDe(index.Assise).Assise == index.Assise)
                    Briques.Poser(racine, "Seuil " + index.Index, seuil, Gabarits.GAUCHE_DU_DECOR, Lieux.YDuSeuil(index.Index), OrdreDeRendu.SEUIL);
            }
            // La roche à creuser, sous l'eau. Aucune lumière de palier ne l'atteint : seule
            // l'ambiante la touche, et elle reste dans l'obscurité jusqu'à ce qu'on la creuse.
            foreach (var r in vue.Roches)
            {
                var roche = catalogue.DecorDe(r.Assise)?.Roche;
                if (roche == null) continue;
                Briques.Paver(racine, "Roche " + r.Index, roche, Gabarits.GAUCHE_DU_DECOR, -(r.Index + 1) * Gabarits.HAUTEUR_DE_BANDE,
                    Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.ROCHE);
            }
        }
    }
}
