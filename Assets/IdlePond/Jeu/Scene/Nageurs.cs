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
            public int Hauteur;
            public int Bande;
            public int Numero;
            public int Effectif;
            public bool Accroche;
            /// Le représentant de la tribu (Codex v1.4) : en tête de sa troupe, plus grand, quatre
            /// images de nage.
            public bool Representant;
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
                var cadre = Gabarits.CadreDEspece(p.Espece.Rang);
                var largeur = cadre.Largeur;
                var images = catalogue.ImagesDeLEspece(p.Espece.Id);
                var effectif = VueDeScene.EffectifDesPoissons(p.Espece.Niveau);
                var accroche = p.Espece.Id == Tribus.LAMPROIE;

                // Le représentant d'abord : c'est lui qu'on suit des yeux (le fil des lamproies
                // part de lui, le mirage se pose près de lui). Un seul par tribu.
                var portraits = catalogue.ImagesDuRepresentant(p.Espece.Id);
                if (portraits != null && portraits.Length > 0 && portraits[0] != null)
                {
                    var cadreR = Gabarits.CADRE_DE_REPRESENTANT;
                    // Une lamproie mère tient la paroi d'en face de la première de sa troupe.
                    var trajetR = accroche
                        ? Nage.TrajetAccroche(p.Index, 1, cadreR)
                        : Nage.TrajetDe(p.Index, Tribus.NUMERO_DU_REPRESENTANT, cadreR);
                    tous.Add(new Nageur
                    {
                        Rendu = Briques.Poser(racine, p.Espece.Id + " (représentant)", portraits[0], trajetR.X0, trajetR.Y, OrdreDeRendu.REPRESENTANTS),
                        Trajet = trajetR, Espece = p.Espece.Id, Largeur = cadreR.Largeur, Hauteur = cadreR.Hauteur, Bande = p.Index,
                        Numero = accroche ? 1 : -1, Effectif = effectif, Accroche = accroche, Representant = true, Images = portraits,
                    });
                }

                for (var numero = 0; numero < effectif; numero++)
                {
                    var trajet = accroche ? Nage.TrajetAccroche(p.Index, numero, cadre) : Nage.TrajetDe(p.Index, numero, cadre);
                    var rendu = Briques.Poser(racine, p.Espece.Id, images != null && images.Length > 0 ? images[0] : null, trajet.X0, trajet.Y, OrdreDeRendu.NAGEURS);
                    tous.Add(new Nageur
                    {
                        Rendu = rendu, Trajet = trajet, Espece = p.Espece.Id, Largeur = largeur, Hauteur = cadre.Hauteur, Bande = p.Index,
                        Numero = numero, Effectif = effectif, Accroche = accroche, Images = images,
                    });
                }
            }
        }

        public int BandeDe(int numero) => tous[numero].Bande;
        public Vector3 PositionDe(int numero) => tous[numero].Rendu.transform.localPosition;

        /// Le milieu du premier poisson d'une tribu — son représentant s'il est dessiné —, au pixel : d'où part un fil, où se pose
        /// un mirage. Null si la tribu ne nage pas.
        public Pixel CentreDuPremier(string espece)
        {
            foreach (var n in tous)
            {
                if (n.Espece != espece || n.Rendu == null) continue;
                var p = n.Rendu.transform.localPosition;
                var gauche = n.Rendu.flipX ? (int)p.x - n.Largeur : (int)p.x;
                return new Pixel(gauche + n.Largeur / 2, (int)p.y + n.Hauteur / 2);
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
            EtatDesTribus tribus = null, ReactionsDesTribus reactions = null)
        {
            tribus ??= EtatDesTribus.CALME;
            var vague = reactions?.VagueEnCours(secondes);
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

                // À la conviction, le représentant entre par la gauche et vient prendre sa place.
                var entree = n.Representant ? reactions?.EntreeDe(n.Espece, secondes) : null;
                if (entree != null)
                {
                    x += Tribus.DecalageDEntree(entree.Avancement(secondes), x + n.Largeur + 4);
                    versLaDroite = true;
                }

                n.Rendu.flipX = !versLaDroite;
                n.Rendu.transform.localPosition = new Vector3(versLaDroite ? x : x + n.Largeur, y, 0f);
                // Le Coryphée rougit le premier, puis la vague parcourt la troupe.
                var rang = n.Representant ? 0 : n.Numero + 1;
                n.Rendu.color = n.Espece == Tribus.VAIRON && vague != null && Tribus.EstRouge(rang, n.Effectif + 1, avancementDeLaVague)
                    ? ROUGE_DU_CHOEUR
                    : Color.white;

                // Une lamproie accrochée ne bat pas de la queue : elle tient. Le représentant a
                // quatre images, comme le héros.
                var images = n.Representant ? Gabarits.IMAGES_DE_NAGE : Gabarits.IMAGES_D_ESPECE;
                var image = n.Accroche ? 0 : (int)((secondes * (n.Representant ? 6 : 4) + n.Trajet.Phase * 3) % images);
                var sprite = n.Images != null && image < n.Images.Length ? n.Images[image] : null;
                if (sprite != null) n.Rendu.sprite = sprite;
            }
        }
    }
}
