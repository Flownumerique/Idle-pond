using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.Scene
{
    /// <summary>
    /// IdlePond — la scène de la mare, en pixel art (spec DA). Elle ne connaît pas l'état du
    /// jeu : elle écoute `Partie.EtatChange`, en tire une `VueDeScene`, et ne redessine que si
    /// la vue a changé. Elle distribue le dessin à des pièces qui ont chacune un rôle, et
    /// affiche la texture du rendu pixel en fond de l'élément #scene de l'interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneDeLaMare : MonoBehaviour
    {
        [SerializeField] CatalogueDArt catalogue;

        public RenduPixel Rendu { get; private set; }
        public VueDeScene Vue { get; private set; }
        public Eclairage Eclairage { get; private set; }
        public Nageurs Nageurs { get; private set; }
        public Voile Voile { get; private set; }

        Partie partie;
        bool vueAJour;
        Decor decor;

        UIDocument document;
        VisualElement cadre;
        float prochaineRecherche;
        DimensionsDuRendu affichee;
        Vector2Int? tailleImposee;

        void Awake()
        {
            Rendu = new RenduPixel(transform);
            decor = new Decor(transform, catalogue);
            Eclairage = new Eclairage(transform);
            Nageurs = new Nageurs(transform, catalogue);
            Voile = new Voile(transform, catalogue);
        }

        void OnEnable()
        {
            partie = Boucle.ObtenirOuCreerLaPartie();
            partie.EtatChange += SurEtatChange;
            vueAJour = false;
        }

        void OnDisable()
        {
            if (partie != null) partie.EtatChange -= SurEtatChange;
        }

        void OnDestroy()
        {
            Rendu?.Dispose();
            Eclairage?.Dispose();
        }

        void Update()
        {
            Nageurs?.Animer(Time.time);
            Voile?.Animer(Time.deltaTime);
        }

        void SurEtatChange(EtatJeu _) => vueAJour = false;

        /// Pour les tests et les captures : une taille de cadre en pixels d'écran, à la place
        /// de celle de #scene.
        public void ImposerLaTaille(int largeurPx, int hauteurPx) => tailleImposee = new Vector2Int(largeurPx, hauteurPx);

        // Après tous les `Update` : la partie a fini d'avancer pour cette image, et
        // l'interface a eu sa mise en page.
        void LateUpdate()
        {
            if (!vueAJour && partie != null)
            {
                vueAJour = true;
                var nouvelle = VueDeScene.Depuis(partie.Etat);
                if (Vue == null || nouvelle.Clef != Vue.Clef)
                {
                    Vue = nouvelle;
                    Redessiner(nouvelle);
                }
            }
            Cadrer();
        }

        void Redessiner(VueDeScene v)
        {
            decor.Dessiner(v);
            Eclairage.Dessiner(v);
            Nageurs.Dessiner(v);
            var assiseDuBas = v.Paliers.Count > 0 ? v.Paliers[v.Paliers.Count - 1].Assise : RegistreDArt.ASSISES_DESSINEES[0];
            Voile.Viser(v.EauTroublee, RegistreDArt.DecorDe(assiseDuBas).Voile);
        }

        void Cadrer()
        {
            if (!TailleDuCadre(out var largeur, out var hauteur)) return;
            if (Rendu.Redimensionner(largeur, hauteur)) affichee = null;
            if (Rendu.Dimensions == null) return;
            Rendu.Cadrer(Vue != null ? Vue.Paliers.Count : 0);
            var (premiere, derniere) = Cadrage.BandesVisibles(Rendu.Champ, Vue != null ? Vue.Paliers.Count : 0);
            Eclairage.Activer(premiere, derniere);
            Voile.Couvrir(Rendu.Champ);
            if (Rendu.Texture != null && cadre != null && cadre.panel != null && affichee != Rendu.Dimensions) Afficher();
        }

        /// La taille de #scene en pixels d'écran. Le panneau couvre tout l'écran, quelle que
        /// soit son échelle : une fraction du panneau est une fraction de l'écran.
        bool TailleDuCadre(out int largeur, out int hauteur)
        {
            largeur = hauteur = 0;
            if (cadre == null || cadre.panel == null) TrouverLeCadre();
            if (tailleImposee.HasValue)
            {
                largeur = tailleImposee.Value.x;
                hauteur = tailleImposee.Value.y;
                return true;
            }
            if (cadre == null || cadre.panel == null) return false;
            var panneau = cadre.panel.visualTree.worldBound;
            var b = cadre.worldBound;
            if (!(panneau.width > 0f && b.width > 0f && b.height > 0f)) return false;
            var parUnite = Screen.width / panneau.width;
            largeur = Mathf.FloorToInt(b.width * parUnite);
            hauteur = Mathf.FloorToInt(b.height * parUnite);
            return true;
        }

        void TrouverLeCadre()
        {
            // Une recherche par demi-seconde : l'élément n'existe qu'après l'activation du
            // UIDocument, qui peut venir après la nôtre.
            if (Time.unscaledTime < prochaineRecherche) return;
            prochaineRecherche = Time.unscaledTime + 0.5f;
            if (document == null) document = FindFirstObjectByType<UIDocument>();
            cadre = document != null && document.rootVisualElement != null
                ? document.rootVisualElement.Q<VisualElement>("scene")
                : null;
        }

        /// La texture en fond de #scene, à sa taille exacte de k × texture, centrée : chaque
        /// pixel du dessin couvre k × k pixels d'écran.
        void Afficher()
        {
            var d = Rendu.Dimensions;
            var parPixel = cadre.panel.visualTree.worldBound.width / Screen.width;
            cadre.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(Rendu.Texture));
            cadre.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(
                new Length(d.Largeur * d.Facteur * parPixel), new Length(d.Hauteur * d.Facteur * parPixel)));
            cadre.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
            cadre.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            cadre.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
            affichee = d;
        }
    }
}
