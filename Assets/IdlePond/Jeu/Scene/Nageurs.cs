using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les espèces qui nagent dans leur bande (spec DA §4). L'effectif dessiné est
    /// le logarithme du niveau, plafonné : le joueur doit voir le groupe grossir, pas le
    /// compter. Un sprite retourné garde son pied : on le décale de la largeur de son cadre.
    /// </summary>
    public sealed class Nageurs
    {
        sealed class Nageur
        {
            public SpriteRenderer Rendu;
            public Trajet Trajet;
            public string Espece;
            public int Largeur;
        }

        readonly Transform racine;
        readonly CatalogueDArt catalogue;
        readonly List<Nageur> tous = new List<Nageur>();

        public Nageurs(Transform parent, CatalogueDArt catalogue)
        {
            racine = new GameObject("Nageurs").transform;
            racine.SetParent(parent, false);
            this.catalogue = catalogue;
        }

        public int Nombre => tous.Count;

        public void Dessiner(VueDeScene vue)
        {
            Briques.Vider(racine);
            tous.Clear();
            if (catalogue == null) return;
            foreach (var p in vue.Paliers)
            {
                if (p.Espece == null) continue;
                var longueur = Gabarits.LongueurDEspece(p.Espece.Rang);
                var largeur = Gabarits.CadreDUnPoisson(longueur).Largeur;
                var effectif = VueDeScene.EffectifDesPoissons(p.Espece.Niveau);
                for (var numero = 0; numero < effectif; numero++)
                {
                    var trajet = Nage.TrajetDe(p.Index, numero, longueur);
                    var rendu = Briques.Poser(racine, p.Espece.Id, catalogue.EspeceDe(p.Espece.Id, 0), trajet.X0, trajet.Y, OrdreDeRendu.NAGEURS);
                    tous.Add(new Nageur { Rendu = rendu, Trajet = trajet, Espece = p.Espece.Id, Largeur = largeur });
                }
            }
        }

        public void Animer(float secondes)
        {
            foreach (var n in tous)
            {
                if (n.Rendu == null) continue;
                var (x, versLaDroite) = Nage.Position(n.Trajet, secondes);
                n.Rendu.flipX = !versLaDroite;
                n.Rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + n.Largeur, n.Trajet.Y, 0f);
                var image = (int)(secondes * 4 + n.Trajet.Phase * 3) % Gabarits.IMAGES_D_ESPECE;
                var sprite = catalogue.EspeceDe(n.Espece, image);
                if (sprite != null) n.Rendu.sprite = sprite;
            }
        }
    }
}
