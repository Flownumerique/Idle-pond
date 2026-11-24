using System.Collections.Generic;
using UnityEngine;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — les espèces qui nagent dans leur bande (spec DA §4). L'effectif dessiné est
    /// le logarithme du niveau, plafonné : le joueur doit voir le groupe grossir, pas le
    /// compter. Un sprite retourné garde son pied : on le décale de la largeur de son cadre.
    ///
    /// Chaque tribu a sa manière d'être là (Codex v1.4, « Les six tribus à l'écran ») : les
    /// lamproies s'accrochent aux parois, les chabots se figent en pierre dans l'eau trouble,
    /// les loches frissonnent avant que l'eau se trouble, la vague du Chœur rougit les vairons
    /// l'un après l'autre. Le QUAND est dans `Tribus`, qui est pur ; ici, seulement le dessin.
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
            public int Numero;
            public int Effectif;
            public bool Accroche;
            public Sprite[] Images;
        }

        /// Un chabot pétrifié : la teinte de la pierre, multipliée à son dessin.
        static readonly Color PIERRE = new Color(0.62f, 0.64f, 0.66f, 1f);
        /// Le rouge de la vague du Chœur : le ventre du vairon, sur tout le corps.
        static readonly Color ROUGE_DU_CHOEUR = Briques.Couleur(0xFF6050);

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
                var accroche = p.Espece.Id == Tribus.LAMPROIE;
                for (var numero = 0; numero < effectif; numero++)
                {
                    var trajet = accroche ? Nage.TrajetAccroche(p.Index, numero, longueur) : Nage.TrajetDe(p.Index, numero, longueur);
                    var rendu = Briques.Poser(racine, p.Espece.Id, images != null && images.Length > 0 ? images[0] : null, trajet.X0, trajet.Y, OrdreDeRendu.NAGEURS);
                    tous.Add(new Nageur
                    {
                        Rendu = rendu, Trajet = trajet, Espece = p.Espece.Id, Largeur = largeur, Bande = p.Index,
                        Numero = numero, Effectif = effectif, Accroche = accroche, Images = images,
                    });
                }
            }
        }

        public int BandeDe(int numero) => tous[numero].Bande;
        public Vector3 PositionDe(int numero) => tous[numero].Rendu.transform.localPosition;

        /// Le milieu du premier poisson d'une tribu, au pixel : d'où part un fil, où se pose
        /// un mirage. Null si la tribu ne nage pas.
        public Pixel CentreDuPremier(string espece)
        {
            foreach (var n in tous)
            {
                if (n.Espece != espece || n.Rendu == null) continue;
                var p = n.Rendu.transform.localPosition;
                var gauche = n.Rendu.flipX ? (int)p.x - n.Largeur : (int)p.x;
                return new Pixel(gauche + n.Largeur / 2, (int)p.y + 3);
            }
            return null;
        }

        /// Le premier poisson d'une tribu regarde-t-il à droite ?
        public bool PremierVersLaDroite(string espece)
        {
            foreach (var n in tous)
                if (n.Espece == espece && n.Rendu != null) return !n.Rendu.flipX;
            return true;
        }

        /// Seuls les nageurs des bandes visibles bougent : les autres ne se voient pas, les
        /// animer coûterait pour rien. Le temps est un double, comme dans `Nage.Position` : un
        /// float perdrait sa précision au fil des heures de jeu.
        public void Animer(double secondes, int premiere = 0, int derniere = int.MaxValue,
            EtatDesTribus tribus = null, Reaction vague = null)
        {
            tribus ??= EtatDesTribus.CALME;
            var avancementDeLaVague = vague != null ? vague.Avancement(secondes) : 0;
            foreach (var n in tous)
            {
                if (n.Rendu == null || n.Bande < premiere || n.Bande > derniere) continue;

                // La pierre ne nage pas : le chabot reste où l'eau trouble l'a pris.
                if (n.Espece == Tribus.CHABOT && tribus.ChabotsPetrifies)
                {
                    n.Rendu.color = PIERRE;
                    if (n.Images != null && n.Images.Length > 0 && n.Images[0] != null) n.Rendu.sprite = n.Images[0];
                    continue;
                }

                int x;
                bool versLaDroite;
                var y = n.Trajet.Y;
                if (n.Accroche)
                {
                    // La bouche vers la paroi.
                    x = n.Trajet.X0;
                    versLaDroite = !Tribus.AccrocheeAGauche(n.Numero);
                    y += Tribus.Respiration(secondes, n.Trajet.Phase);
                }
                else
                {
                    (x, versLaDroite) = Nage.Position(n.Trajet, secondes);
                    if (n.Espece == Tribus.LOCHE && tribus.LochesAgitees) x += Tribus.Frisson(secondes, n.Trajet.Phase);
                }

                n.Rendu.flipX = !versLaDroite;
                n.Rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + n.Largeur, y, 0f);
                n.Rendu.color = n.Espece == Tribus.VAIRON && vague != null && Tribus.EstRouge(n.Numero, n.Effectif, avancementDeLaVague)
                    ? ROUGE_DU_CHOEUR
                    : Color.white;

                // Une lamproie accrochée ne bat pas de la queue : elle tient.
                var image = n.Accroche ? 0 : (int)((secondes * 4 + n.Trajet.Phase * 3) % Gabarits.IMAGES_D_ESPECE);
                var sprite = n.Images != null && image < n.Images.Length ? n.Images[image] : null;
                if (sprite != null) n.Rendu.sprite = sprite;
            }
        }
    }
}
