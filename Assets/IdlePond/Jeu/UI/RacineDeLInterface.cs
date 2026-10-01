using System;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// IdlePond — le contrôleur racine de l'interface. Posé sur le GameObject de
    /// l'`UIDocument` de `Mare.unity` par le générateur de scènes.
    ///
    /// Il ne contient AUCUNE règle du jeu (§5.3) : il lit `Partie`, câble un contrôleur par
    /// panneau, et renvoie chaque geste du joueur à la partie. Tout ce que le joueur lit
    /// vient d'un état immuable, relu à chaque changement.
    ///
    /// Il possède trois choses, et trois seulement : l'abonnement aux événements de la
    /// partie (pris à l'activation, rendu à la désactivation, sans quoi une scène rechargée
    /// laisserait des fantômes qui écrivent dans un arbre détruit), l'adaptation au
    /// portrait et au paysage, et l'élément `#scene` que la scène dessinée emprunte.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RacineDeLInterface : MonoBehaviour
    {
        UIDocument document;
        Partie partie;
        VisualElement racine;
        VisualElement laterale;
        VisualElement panneauDesSucces;
        VisualElement panneauDesInsufflations;
        bool paysage;

        EnTete enTete;
        Retour retour;
        Contenance contenance;
        Heros heros;
        Captation captation;
        Mare mare;
        Renaissance renaissance;
        Succes succes;
        Insufflations insufflations;
        Annonces annonces;
        Onglets onglets;
        Action<EtatJeu>[] rafraichisseurs = Array.Empty<Action<EtatJeu>>();

        /// <summary>
        /// L'élément `#scene` : le rectangle que la scène dessinée remplit (elle le retrouve
        /// aussi seule, par son nom). Null avant
        /// `OnEnable` et après `OnDisable`. L'élément est transparent exprès : la caméra
        /// dessine DERRIÈRE l'interface, et tout fond opaque posé derrière lui la cacherait.
        /// </summary>
        public VisualElement CadreDeScene { get; private set; }

        void OnEnable() => Brancher();

        // `UIDocument` construit son arbre dans son propre `OnEnable`. L'ordre d'exécution
        // ci-dessus le place avant nous, mais si un autre ordre s'était glissé, la racine
        // serait vide ici : on réessaie une fois, au démarrage, au lieu de s'éteindre sans rien dire.
        void Start()
        {
            if (racine != null || !isActiveAndEnabled) return;
            Brancher();
            if (racine == null)
                Debug.LogError("RacineDeLInterface : l'UIDocument n'a pas d'arbre (Mare.uxml et PanelSettings.asset reliés ?).", this);
        }

        void OnDisable() => Debrancher();

        void Brancher()
        {
            if (racine != null) return;
            document = GetComponent<UIDocument>();
            var cible = document != null ? document.rootVisualElement : null;
            var trouvee = cible?.Q<VisualElement>("racine");
            if (trouvee == null) return;

            // Le seul chemin de création de la partie : Mare.unity lancée seule, sans
            // Amorce, la crée elle-même (§2) pour rester jouable.
            partie = Boucle.ObtenirOuCreerLaPartie();

            racine = trouvee;
            CadreDeScene = racine.Q<VisualElement>("scene");

            // Le `TemplateContainer` que l'UIDocument glisse entre le panneau et notre
            // racine ne remplit pas l'écran de lui-même : sans cela, `height: 100%` se
            // mesure sur un parent de hauteur nulle.
            for (var parent = racine.parent; parent != null; parent = parent.parent)
                parent.style.flexGrow = 1;

            ConstruirePanneaux();

            racine.RegisterCallback<GeometryChangedEvent>(SurGeometrie);
            partie.EtatChange += SurEtat;
            partie.SuccesDeclenches += SurSucces;
            partie.RetourAffiche += SurRetour;

            // Ce qui est arrivé AVANT que l'interface existe : le crédit hors ligne du
            // démarrage, les succès déjà déclenchés. Les événements, eux, sont passés.
            SurEtat(partie.Etat);
            retour.Afficher(partie.Retour);
            annonces.Afficher(partie.AAnnoncer);
            Adapter(racine.layout.width, racine.layout.height);
        }

        void Debrancher()
        {
            if (partie != null)
            {
                partie.EtatChange -= SurEtat;
                partie.SuccesDeclenches -= SurSucces;
                partie.RetourAffiche -= SurRetour;
            }
            racine?.UnregisterCallback<GeometryChangedEvent>(SurGeometrie);
            racine = null;
            CadreDeScene = null;
            partie = null;
            rafraichisseurs = Array.Empty<Action<EtatJeu>>();
        }

        void ConstruirePanneaux()
        {
            var p = partie;

            var defilement = racine.Q<ScrollView>("defilement");
            ConfigurerLeDefilement(defilement);
            ConfigurerLeDefilement(racine.Q<ScrollView>("defilement-lateral"));
            laterale = racine.Q<VisualElement>("laterale");
            panneauDesSucces = racine.Q<VisualElement>("succes");
            panneauDesInsufflations = racine.Q<VisualElement>("insufflations");

            enTete = new EnTete(racine.Q<VisualElement>("entete"));
            retour = new Retour(racine.Q<VisualElement>("retour"), () => p.OublierRetour());
            contenance = new Contenance(racine.Q<VisualElement>("contenance"));
            heros = new Heros(racine.Q<VisualElement>("heros"), () => p.Grandir());
            captation = new Captation(racine.Q<VisualElement>("captation"), () =>
            {
                captation.Fermer();
            });
            mare = new Mare(racine.Q<VisualElement>("mare"),
                id => p.Convaincre(id),
                id => p.Monter(id),
                () => p.Creuser(),
                id =>
                {
                    captation.Ouvrir(id);
                    captation.Rafraichir(p.Etat);
                });
            renaissance = new Renaissance(racine.Q<VisualElement>("renaissance"), () => p.Renaitre());
            succes = new Succes(panneauDesSucces);
            insufflations = new Insufflations(panneauDesInsufflations, id => p.Insuffler(id));
            annonces = new Annonces(racine.Q<VisualElement>("annonces"), id => p.OublierAnnonce(id));
            onglets = new Onglets(racine.Q<VisualElement>("onglets"), AppliquerLesOnglets);

            rafraichisseurs = new Action<EtatJeu>[]
            {
                enTete.Rafraichir, contenance.Rafraichir, heros.Rafraichir, captation.Rafraichir,
                mare.Rafraichir, renaissance.Rafraichir, succes.Rafraichir, insufflations.Rafraichir,
            };
        }

        static void ConfigurerLeDefilement(ScrollView defilement)
        {
            if (defilement == null) return;
            defilement.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            // Au doigt, la liste glisse et revient ; à la souris, la molette suffit.
            defilement.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
        }

        /* ─── Ce que la partie raconte ─────────────────────────────────────────────── */

        // Une panne d'un panneau ne doit pas arrêter le jeu. Ces gestionnaires sont appelés
        // PAR la partie, au milieu de `Avancer` : une exception ici sauterait l'annonce des
        // succès qui suit, dans le tick même. On la journalise, panneau par panneau, et le
        // reste de l'écran continue de vivre. Les délégués sont créés une fois : dix ticks
        // par seconde ne doivent pas allouer dix fois huit fermetures.
        void SurEtat(EtatJeu etat)
        {
            foreach (var rafraichir in rafraichisseurs)
            {
                try { rafraichir(etat); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
        }

        void SurSucces(System.Collections.Generic.IReadOnlyList<string> _)
        {
            try { annonces.Afficher(partie.AAnnoncer); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        void SurRetour(AbsenceCreditee absence)
        {
            try { retour.Afficher(absence); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        /* ─── L'adaptation ─────────────────────────────────────────────────────────── */

        void SurGeometrie(GeometryChangedEvent evenement) => Adapter(evenement.newRect.width, evenement.newRect.height);

        /// <summary>
        /// Paysage si la largeur vaut au moins 1,2 fois la hauteur, sinon portrait. Le
        /// contrôleur ne pose que la classe : la mise en page est dans `Mare.uss`.
        /// </summary>
        void Adapter(float largeur, float hauteur)
        {
            if (!(largeur > 0 && hauteur > 0)) return;
            var estPaysage = Adaptation.EstPaysage(largeur, hauteur);
            if (estPaysage == paysage && racine.ClassListContains(estPaysage ? Adaptation.CLASSE_PAYSAGE : Adaptation.CLASSE_PORTRAIT))
                return;
            paysage = estPaysage;
            racine.EnableInClassList(Adaptation.CLASSE_PAYSAGE, estPaysage);
            racine.EnableInClassList(Adaptation.CLASSE_PORTRAIT, !estPaysage);
            AppliquerLesOnglets();
        }

        /// <summary>
        /// En paysage la colonne de droite montre les deux panneaux ; en portrait elle
        /// n'existe que derrière un onglet, et ne montre que celui-là.
        /// </summary>
        void AppliquerLesOnglets()
        {
            if (racine == null) return;
            var actif = onglets.Actif;
            Elements.Montrer(laterale, paysage || actif != OngletActif.Aucun);
            Elements.Montrer(panneauDesSucces, paysage || actif == OngletActif.Succes);
            Elements.Montrer(panneauDesInsufflations, paysage || actif == OngletActif.Insufflations);
        }
    }
}
