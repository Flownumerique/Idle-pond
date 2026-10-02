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
            public int Bande;
            public Sprite[] Images;
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
                var images = catalogue.ImagesDeLEspece(p.Espece.Id);
                var effectif = VueDeScene.EffectifDesPoissons(p.Espece.Niveau);
                for (var numero = 0; numero < effectif; numero++)
                {
                    var trajet = Nage.TrajetDe(p.Index, numero, longueur);
                    var rendu = Briques.Poser(racine, p.Espece.Id, images != null && images.Length > 0 ? images[0] : null, trajet.X0, trajet.Y, OrdreDeRendu.NAGEURS);
                    tous.Add(new Nageur { Rendu = rendu, Trajet = trajet, Espece = p.Espece.Id, Largeur = largeur, Bande = p.Index, Images = images });
                }
            }
        }

        public int BandeDe(int numero) => tous[numero].Bande;
        public Vector3 PositionDe(int numero) => tous[numero].Rendu.transform.localPosition;

        /// Seuls les nageurs des bandes visibles bougent : les autres ne se voient pas, les
        /// animer coûterait pour rien. Le temps est un double, comme dans `Nage.Position` : un
        /// float perdrait sa précision au fil des heures de jeu.
        public void Animer(double secondes, int premiere = 0, int derniere = int.MaxValue)
        {
            foreach (var n in tous)
            {
                if (n.Rendu == null || n.Bande < premiere || n.Bande > derniere) continue;
                var (x, versLaDroite) = Nage.Position(n.Trajet, secondes);
                n.Rendu.flipX = !versLaDroite;
                n.Rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + n.Largeur, n.Trajet.Y, 0f);
                var image = (int)((secondes * 4 + n.Trajet.Phase * 3) % Gabarits.IMAGES_D_ESPECE);
                var sprite = n.Images != null && image < n.Images.Length ? n.Images[image] : null;
                if (sprite != null) n.Rendu.sprite = sprite;
            }
        }
    }
}
