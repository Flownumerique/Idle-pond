/*
 * IdlePond — l'hôte Unity du jeu.
 *
 * Il fait, dans Unity, ce que `App.tsx` et `main.tsx` font dans le navigateur :
 * il crée le magasin, crédite l'absence, démarre la boucle et monte l'écran.
 * Rien d'autre — aucune règle du jeu ne vit dans un MonoBehaviour.
 *
 * Ce que la plateforme ajoute, et que le web n'avait pas à gérer :
 *
 *   - la mise en pause de l'application (mobile). La boucle tire son dt de
 *     l'horloge : si elle tournait encore au retour, son premier pas créditerait
 *     toute l'absence sans son plafond. Elle est donc ARRÊTÉE à la pause, la save
 *     écrite, et au retour c'est le crédit hors ligne — plafonné — qui reprend la
 *     main, exactement comme au lancement ;
 *   - la sauvegarde, qui n'a pas lieu à chaque pas mais toutes les quelques
 *     secondes, après chaque acte, à la pause et à la fermeture.
 */
using IdlePond.Adaptateurs;
using IdlePond.Etat;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Interface
{
    [DisallowMultipleComponent]
    public sealed class Jeu : MonoBehaviour
    {
        /// <summary>Au-delà, une coupure brutale ne coûte que ces quelques secondes de jeu.</summary>
        public const float IntervalleDeSauvegardeSecondes = 5f;

        [Tooltip("Résolution de référence du panneau : l'écran se met à l'échelle à partir d'elle.")]
        [SerializeField] private Vector2Int _resolutionDeReference = new Vector2Int(1280, 800);

        [Tooltip("Images par seconde visées. Un jeu idle n'a pas besoin de plus, et la batterie le sait.")]
        [SerializeField] private int _imagesParSeconde = 30;

        private Magasin _magasin;
        private Boucle _boucle;
        private Ecran _ecran;
        private UIDocument _document;
        private float _depuisLeDernierPas;
        private float _depuisLaDerniereSauvegarde;
        private bool _aRafraichir;
        private bool _suspendu;

        /// <summary>L'instance en cours, pour les outils de l'éditeur. null hors jeu.</summary>
        public static Jeu Courant { get; private set; }

        public Magasin Magasin => _magasin;

        private void Awake()
        {
            if (Courant != null && Courant != this)
            {
                Destroy(gameObject);
                return;
            }
            Courant = this;
            DontDestroyOnLoad(gameObject);

            // Desktop et WebGL : la mare tourne aussi quand la fenêtre n'a pas le
            // focus, comme un onglet de navigateur en arrière-plan.
            Application.runInBackground = true;
            Application.targetFrameRate = _imagesParSeconde;

            _magasin = new Magasin(StockageUnity.ParDefaut(), HorlogeSysteme.Instance);
            _magasin.Change += () => _aRafraichir = true;
            // Un trou entre deux pas — veille, horloge avancée — est une absence, et
            // se crédite comme telle : plafonnée (voir `Boucle`).
            _boucle = new Boucle(
                () => _magasin.Etat, _magasin.Remplacer, _magasin.Annoncer, HorlogeSysteme.Instance,
                () => _magasin.Reprendre());

            PreparerLaCamera();
            _document = MonterLeDocument();
        }

        private void Start()
        {
            // Un doublon détruit dans Awake reçoit encore Start dans la même frame.
            if (_magasin == null) return;
            // Un seul appel à Tick pour toute l'absence. Rien ne s'est dégradé.
            _magasin.Reprendre();
            _boucle.Demarrer();
            _ecran = new Ecran(_document.rootVisualElement, _magasin);
        }

        private void Update()
        {
            if (_magasin == null || _suspendu) return;

            _depuisLeDernierPas += Time.unscaledDeltaTime;
            if (_depuisLeDernierPas * 1000 >= Constantes.PeriodeDeTickMs)
            {
                _depuisLeDernierPas = 0;
                _boucle.Pas();
            }

            _depuisLaDerniereSauvegarde += Time.unscaledDeltaTime;
            if (_depuisLaDerniereSauvegarde >= IntervalleDeSauvegardeSecondes)
            {
                _depuisLaDerniereSauvegarde = 0;
                _magasin.SauvegarderSiModifie();
            }

            if (_aRafraichir && _ecran != null)
            {
                _aRafraichir = false;
                _ecran.Rafraichir();
            }
        }

        private void OnApplicationPause(bool enPause)
        {
            if (_magasin == null) return;
            if (enPause) Suspendre();
            else ReprendreApresUnePause();
        }

        private void OnApplicationQuit()
        {
            _magasin?.Sauvegarder();
        }

        private void OnDestroy()
        {
            if (Courant != this) return;
            _magasin?.Sauvegarder();
            Courant = null;
        }

        private void Suspendre()
        {
            if (_suspendu) return;
            _suspendu = true;
            _boucle.Arreter();
            _magasin.Sauvegarder();
        }

        private void ReprendreApresUnePause()
        {
            // Certaines plateformes annoncent une « reprise » au lancement : sans
            // pause préalable, il n'y a rien à créditer, et surtout pas le retour
            // déjà affiché à effacer.
            if (!_suspendu) return;
            _suspendu = false;
            _magasin.Reprendre();
            _boucle.Demarrer();
            _depuisLeDernierPas = 0;
        }

        /// <summary>Remplace la partie par une save au format web. Pour l'outil d'import de l'éditeur.</summary>
        public bool Importer(string texte)
        {
            if (!_magasin.EssayerDImporter(texte)) return false;
            _magasin.Reprendre();
            _magasin.Sauvegarder();
            return true;
        }

        public void Reinitialiser() => _magasin.Reinitialiser();

        /* ─── Mise en place ─────────────────────────────────────────────────── */

        private static Color Couleur(string hexadecimal) =>
            ColorUtility.TryParseHtmlString(hexadecimal, out var couleur) ? couleur : Color.black;

        /// <summary>
        /// L'écran est tout entier en UI Toolkit ; la caméra ne sert qu'à peindre le
        /// fond de l'eau et à éviter « aucune caméra » dans la vue de jeu.
        /// </summary>
        private void PreparerLaCamera()
        {
            if (Camera.main != null) return;
            var objet = new GameObject("Caméra");
            objet.transform.SetParent(transform, false);
            var camera = objet.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Couleur("#03111b");
            camera.cullingMask = 0;
            camera.orthographic = true;
            objet.tag = "MainCamera";
        }

        private UIDocument MonterLeDocument()
        {
            var reglages = ScriptableObject.CreateInstance<PanelSettings>();
            reglages.name = "IdlePond — panneau";
            reglages.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            reglages.referenceResolution = _resolutionDeReference;
            reglages.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            reglages.match = 0.5f;
            var theme = Resources.Load<ThemeStyleSheet>("IdlePond/ThemeParDefaut");
            if (theme != null) reglages.themeStyleSheet = theme;

            // Le composant est ajouté sur un objet inactif, pour que ses réglages
            // soient en place avant qu'il ne s'attache à un panneau.
            var objet = new GameObject("Écran");
            objet.SetActive(false);
            objet.transform.SetParent(transform, false);
            var document = objet.AddComponent<UIDocument>();
            document.panelSettings = reglages;
            objet.SetActive(true);

            if (theme == null) Polices.AppliquerLaPoliceIntegree(document.rootVisualElement);
            document.rootVisualElement.style.flexGrow = 1;
            return document;
        }
    }
}
