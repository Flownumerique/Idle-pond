using System.Linq;
using IdlePond.Noyau.Donnees;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — le décor (spec DA §4 ; le puits, 2026-11-25). Creuser ouvre un puits, pas les
    /// étages d'une tour : l'eau de chaque palier est une tranche d'un seul dégradé qui court sur
    /// tout le lieu ; deux parois de roche la bordent, et à chaque palier une corniche sort de
    /// l'une d'elles ; au fond seulement, un vrai sol. Sur la première bande, la berge et ses
    /// racines et les rayons du jour. Une assise sans dessin emprunte celui de la Noue ; la
    /// lumière, elle, vient d'`Eclairage`.
    ///
    /// Le sol qui s'ouvre : quand on creuse, la roche du palier neuf recule vers le bas
    /// (`Puits.RocheRestante`), et le sol du fond descend avec elle.
    /// </summary>
    public sealed class Decor
    {
        readonly Transform racine;
        readonly Transform ouverture;
        readonly CatalogueDArt catalogue;

        // Le sol du fond du puits, et le palier où il est posé.
        SpriteRenderer lit;
        int palierDuLit = -1;

        // L'ouverture en cours : le palier qu'on creuse, quand, et la roche qui recule.
        int palierOuvert = -1;
        double debutDeLOuverture;
        double maintenant;
        SpriteRenderer rocheQuiRecule;

        public Decor(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Decor").transform;
            racine.SetParent(parent, false);
            ouverture = new GameObject("Ouverture").transform;
            ouverture.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        /// Le palier dont le sol s'ouvre, ou −1 : pour les tests.
        public int PalierQuiSOuvre => palierOuvert;

        public void Dessiner(VueDeScene vue)
        {
            Briques.Vider(racine);
            lit = null;
            palierDuLit = -1;
            if (catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                var art = catalogue.DecorDe(p.Assise);
                if (art == null) continue;
                var bas = -(p.Index + 1) * Gabarits.HAUTEUR_DE_BANDE;
                var rang = p.Index - Assises.DuPalier(p.Index).IndexPremierPalier;
                var eau = art.EauDu(rang);
                if (eau != null)
                    Briques.Paver(racine, "Eau " + p.Index, eau, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.FOND);
                if (art.Ambiance != null)
                    Briques.Paver(racine, "Ambiance " + p.Index, art.Ambiance, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.AMBIANCE);
                var parois = art.ParoisDu(rang);
                if (parois != null) Briques.Poser(racine, "Parois " + p.Index, parois, Gabarits.GAUCHE_DU_DECOR, bas, OrdreDeRendu.PAROIS);
                var corniche = art.CornicheDu(rang);
                // Elle pend sous le bas de son palier (`PENDANT_DE_CORNICHE` : un septième de bande).
                if (corniche != null)
                    Briques.Poser(racine, "Corniche " + p.Index, corniche, Gabarits.GAUCHE_DU_DECOR,
                        bas - Gabarits.HAUTEUR_DE_BANDE / 7, OrdreDeRendu.CORNICHES);
                if (p.Index != 0) continue;
                if (RegistreDArt.DecorDe(p.Assise).Berge && art.Berge != null)
                    Briques.Paver(racine, "Berge", art.Berge, Gabarits.GAUCHE_DU_DECOR, bas,
                        Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.BORD);
                if (art.Rayons != null)
                    Briques.Poser(racine, "Rayons", art.Rayons, (Gabarits.LARGEUR_VISEE - Gabarits.LARGEUR_DE_BERGE) / 2,
                        -Gabarits.HAUTEUR_DES_RAYONS, OrdreDeRendu.RAYONS);
            }
            // Le sol : au fond du puits seulement.
            if (vue.Paliers.Count > 0)
            {
                var fond = vue.Paliers[vue.Paliers.Count - 1];
                var art = catalogue.DecorDe(fond.Assise);
                if (art?.Lit != null)
                {
                    lit = Briques.Paver(racine, "Sol du fond", art.Lit, Gabarits.GAUCHE_DU_DECOR, -(fond.Index + 1) * Gabarits.HAUTEUR_DE_BANDE,
                        Gabarits.LARGEUR_DU_DECOR, (int)art.Lit.rect.height, OrdreDeRendu.LIT);
                    palierDuLit = fond.Index;
                }
            }
            // La lèvre de la vasque de chaque lieu qui s'ouvre par une cassure (2026-11-25),
            // qu'il soit déjà ouvert ou encore dans la roche qu'on voit venir.
            foreach (var index in vue.Paliers.Select(p => (p.Index, p.Assise)).Concat(vue.Roches.Select(r => (r.Index, r.Assise))))
            {
                if (!Lieux.ACassure(index.Index)) continue;
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
            // Le redessin vient souvent dans la même image que l'ouverture : le sol reprend tout
            // de suite sa place sur la roche qui recule.
            Animer(maintenant);
        }

        /// Le sol du palier `palier` s'ouvre, à `instant` : sa roche va reculer vers le bas.
        public void Ouvrir(int palier, double instant)
        {
            if (rocheQuiRecule != null) Object.Destroy(rocheQuiRecule.gameObject);
            rocheQuiRecule = null;
            palierOuvert = palier;
            debutDeLOuverture = instant;
            maintenant = instant;
            var roche = catalogue != null ? catalogue.DecorDe(Assises.DuPalier(palier).Id)?.Roche : null;
            if (roche == null) return;
            rocheQuiRecule = Briques.Paver(ouverture, "Roche qui recule " + palier, roche, Gabarits.GAUCHE_DU_DECOR,
                -(palier + 1) * Gabarits.HAUTEUR_DE_BANDE, Gabarits.LARGEUR_DU_DECOR, Gabarits.HAUTEUR_DE_BANDE, OrdreDeRendu.ROCHE);
            // Dans la lumière de l'eau, la roche fraîchement entamée resterait trop claire.
            rocheQuiRecule.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        public void Animer(double secondes)
        {
            maintenant = secondes;
            var reste = palierOuvert >= 0 ? Puits.RocheRestante(secondes - debutDeLOuverture) : 0;
            if (palierOuvert >= 0 && reste == 0)
            {
                if (rocheQuiRecule != null) Object.Destroy(rocheQuiRecule.gameObject);
                rocheQuiRecule = null;
                palierOuvert = -1;
            }
            if (rocheQuiRecule != null)
                rocheQuiRecule.size = new Vector2(Gabarits.LARGEUR_DU_DECOR, reste);
            if (lit != null)
            {
                var bas = -(palierDuLit + 1) * Gabarits.HAUTEUR_DE_BANDE;
                var leve = palierDuLit == palierOuvert ? reste : 0;
                lit.transform.localPosition = new Vector3(Gabarits.GAUCHE_DU_DECOR, bas + leve, 0f);
            }
        }
    }
}
