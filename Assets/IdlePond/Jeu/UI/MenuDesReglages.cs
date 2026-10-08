using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le tiroir des réglages (spec du 2026-10-08) : quatre onglets — Son, Affichage, Jeu,
    /// Accessibilité. Chaque geste passe par `MagasinDeReglages.Modifier` et s'applique
    /// tout de suite ; il n'y a pas de bouton Valider.
    ///
    /// Les contrôles sont faits maison, comme le reste de l'écran : le `Slider` et le
    /// `Toggle` d'Unity arrivent avec le style du thème par défaut, et un curseur à
    /// glisser se vise mal au doigt. Un volume se règle par crans (− et +), un choix par
    /// des puces, comme les filtres du Journal.
    ///
    /// Le menu ne lit pas l'écran lui-même : ce qu'il ne sait pas (le format reconnu, le
    /// plein écran courant, la plateforme) lui est donné par la racine.
    /// </summary>
    public sealed class MenuDesReglages
    {
        const double CRAN_DE_VOLUME = 0.1;
        const double CRAN_DE_TAILLE = 0.05;

        enum Onglet { Son, Affichage, Jeu, Accessibilite }

        readonly MagasinDeReglages magasin;
        readonly Func<FormatDAffichage> formatReconnu;
        readonly Func<bool> pleinEcranCourant;
        readonly bool mobile;
        readonly List<Action<Reglages>> rafraichisseurs = new List<Action<Reglages>>();
        readonly Dictionary<Onglet, VisualElement> pages = new Dictionary<Onglet, VisualElement>();
        readonly Dictionary<Onglet, VisualElement> onglets = new Dictionary<Onglet, VisualElement>();
        readonly List<Action> annulations = new List<Action>();
        Onglet ouvert = Onglet.Son;

        public MenuDesReglages(VisualElement racine, MagasinDeReglages magasin, Func<FormatDAffichage> formatReconnu,
            Func<bool> pleinEcranCourant, bool pleinEcranDisponible, bool mobile, Action effacerLaPartie)
        {
            this.magasin = magasin;
            this.formatReconnu = formatReconnu;
            this.pleinEcranCourant = pleinEcranCourant;
            this.mobile = mobile;
            racine.AddToClassList("reglages");

            var barreDOnglets = Elements.Conteneur("filtres reglages-onglets");
            AjouterOnglet(barreDOnglets, Onglet.Son, E.ONGLET_SON, "onglet-son");
            AjouterOnglet(barreDOnglets, Onglet.Affichage, E.ONGLET_AFFICHAGE, "onglet-affichage");
            AjouterOnglet(barreDOnglets, Onglet.Jeu, E.ONGLET_JEU, "onglet-jeu");
            AjouterOnglet(barreDOnglets, Onglet.Accessibilite, E.ONGLET_ACCESSIBILITE, "onglet-accessibilite");
            racine.Add(barreDOnglets);

            ConstruireLeSon(Page(racine, Onglet.Son));
            ConstruireLAffichage(Page(racine, Onglet.Affichage), pleinEcranDisponible);
            ConstruireLeJeu(Page(racine, Onglet.Jeu), effacerLaPartie);
            ConstruireLAccessibilite(Page(racine, Onglet.Accessibilite));

            Ouvrir(ouvert);
            Rafraichir(magasin.Courants);
        }

        public void Rafraichir(Reglages r)
        {
            foreach (var rafraichir in rafraichisseurs) rafraichir(r);
        }

        /// Le tiroir se ferme : une confirmation armée ne doit pas attendre son retour.
        public void Fermer()
        {
            foreach (var annuler in annulations) annuler();
        }

        /* ─── Les onglets ─────────────────────────────────────────────────────────── */

        void AjouterOnglet(VisualElement parent, Onglet onglet, string libelle, string nom)
        {
            var puce = Elements.Conteneur("filtre", nom);
            puce.Add(Elements.Texte("sm", libelle));
            puce.AddManipulator(new Clickable(() => Ouvrir(onglet)));
            parent.Add(puce);
            onglets[onglet] = puce;
        }

        VisualElement Page(VisualElement racine, Onglet onglet)
        {
            var page = Elements.Conteneur("reglages-page");
            racine.Add(page);
            pages[onglet] = page;
            return page;
        }

        void Ouvrir(Onglet onglet)
        {
            ouvert = onglet;
            foreach (var paire in pages) Elements.Montrer(paire.Value, paire.Key == onglet);
            foreach (var paire in onglets) Elements.Marquer(paire.Value, "actif", paire.Key == onglet);
            Fermer();
        }

        /* ─── Les pages ───────────────────────────────────────────────────────────── */

        void ConstruireLeSon(VisualElement page)
        {
            page.Add(Elements.Texte("doux sm reglages-note", E.SONS_A_VENIR));
            PasAPas(page, E.VOLUME_GENERAL, "volume-general", r => r.VolumeGeneral, (r, v) => r with { VolumeGeneral = v }, CRAN_DE_VOLUME);
            PasAPas(page, E.VOLUME_MUSIQUE, "volume-musique", r => r.VolumeMusique, (r, v) => r with { VolumeMusique = v }, CRAN_DE_VOLUME);
            PasAPas(page, E.VOLUME_EFFETS, "volume-effets", r => r.VolumeEffets, (r, v) => r with { VolumeEffets = v }, CRAN_DE_VOLUME);
            OuiNon(page, E.COUPER_LE_SON, null, "muet", r => r.Muet, (r, v) => r with { Muet = v });
            OuiNon(page, E.SE_TAIRE_DERRIERE, null, "muet-derriere", r => r.MuetEnArrierePlan, (r, v) => r with { MuetEnArrierePlan = v });
        }

        void ConstruireLAffichage(VisualElement page, bool pleinEcranDisponible)
        {
            var reconnu = Choix(page, E.FORMAT_D_AFFICHAGE, "", new[]
            {
                Option(E.FORMAT_AUTO, "format-auto", r => r.Affichage == FormatDAffichage.Auto, r => r with { Affichage = FormatDAffichage.Auto }),
                Option(E.FORMAT_TELEPHONE, "format-telephone", r => r.Affichage == FormatDAffichage.Telephone, r => r with { Affichage = FormatDAffichage.Telephone }),
                Option(E.FORMAT_TABLETTE, "format-tablette", r => r.Affichage == FormatDAffichage.Tablette, r => r with { Affichage = FormatDAffichage.Tablette }),
                Option(E.FORMAT_PC, "format-pc", r => r.Affichage == FormatDAffichage.Pc, r => r with { Affichage = FormatDAffichage.Pc }),
            });
            rafraichisseurs.Add(_ => Elements.Poser(reconnu, Format.Remplir(E.FORMAT_DETECTE, NomDu(formatReconnu()))));

            PasAPas(page, E.TAILLE_DE_L_INTERFACE, "taille-interface", r => r.TailleDInterface, (r, v) => r with { TailleDInterface = v },
                CRAN_DE_TAILLE, Reglages.TAILLE_MIN, Reglages.TAILLE_MAX);

            if (pleinEcranDisponible)
                OuiNon(page, E.PLEIN_ECRAN, null, "plein-ecran", r => r.PleinEcran ?? pleinEcranCourant(), (r, v) => r with { PleinEcran = v });

            Choix(page, E.IMAGES_PAR_SECONDE, E.IMAGES_DETAIL, new[]
            {
                Option("30", "images-30", r => LimiteDe(r) == LimiteDImages.Trente, r => r with { Images = LimiteDImages.Trente }),
                Option("60", "images-60", r => LimiteDe(r) == LimiteDImages.Soixante, r => r with { Images = LimiteDImages.Soixante }),
                Option(E.IMAGES_SANS_LIMITE, "images-sans-limite", r => LimiteDe(r) == LimiteDImages.Illimitee, r => r with { Images = LimiteDImages.Illimitee }),
            });
        }

        void ConstruireLeJeu(VisualElement page, Action effacerLaPartie)
        {
            Choix(page, E.NOTATION, null, new[]
            {
                Option("1.25 M", "notation-suffixes", r => r.Notation == Notation.Suffixes, r => r with { Notation = Notation.Suffixes }),
                Option("1.25e+6", "notation-scientifique", r => r.Notation == Notation.Scientifique, r => r with { Notation = Notation.Scientifique }),
                Option("12.5e+6", "notation-ingenieur", r => r.Notation == Notation.Ingenieur, r => r with { Notation = Notation.Ingenieur }),
            });
            OuiNon(page, E.ANNONCES_DE_SUCCES, E.ANNONCES_DETAIL, "annonces-affichees", r => r.AnnoncesAffichees, (r, v) => r with { AnnoncesAffichees = v });

            var ligne = Ligne(page, E.EFFACER_LA_PARTIE, E.EFFACER_DETAIL);
            ligne.Add(BoutonAConfirmer(E.EFFACER_LA_PARTIE, "reinitialiser", "bouton-danger", effacerLaPartie));

            var pied = Ligne(page, E.RETABLIR_LES_REGLAGES, null);
            pied.Add(BoutonAConfirmer(E.RETABLIR_LES_REGLAGES, "retablir-reglages", "bouton-sobre",
                () => magasin.Modifier(_ => Reglages.ParDefaut)));
        }

        void ConstruireLAccessibilite(VisualElement page)
        {
            OuiNon(page, E.MOUVEMENT_REDUIT, null, "mouvement-reduit", r => r.MouvementReduit, (r, v) => r with { MouvementReduit = v });
            OuiNon(page, E.CONTRASTE_RENFORCE, null, "contraste-renforce", r => r.ContrasteRenforce, (r, v) => r with { ContrasteRenforce = v });
        }

        LimiteDImages LimiteDe(Reglages r) => r.Images ?? (mobile ? LimiteDImages.Trente : LimiteDImages.Soixante);

        static string NomDu(FormatDAffichage format)
        {
            switch (format)
            {
                case FormatDAffichage.Tablette: return E.FORMAT_TABLETTE;
                case FormatDAffichage.Pc: return E.FORMAT_PC;
                default: return E.FORMAT_TELEPHONE;
            }
        }

        /* ─── Les contrôles ───────────────────────────────────────────────────────── */

        /// Une ligne : son titre, une précision en dessous s'il y en a une, puis le contrôle.
        static VisualElement Ligne(VisualElement page, string titre, string detail)
        {
            var ligne = Elements.Conteneur("reglages-ligne");
            ligne.Add(Elements.Texte("titre base", titre));
            if (!string.IsNullOrEmpty(detail)) ligne.Add(Elements.Texte("doux sm reglages-detail", detail));
            page.Add(ligne);
            return ligne;
        }

        /// Un volume ou une taille : − [jauge] + et la valeur en pourcentage.
        void PasAPas(VisualElement page, string titre, string nom, Func<Reglages, double> lire,
            Func<Reglages, double, Reglages> ecrire, double cran, double min = 0, double max = 1)
        {
            var ligne = Ligne(page, titre, null);
            var rangee = Elements.Conteneur("pas-a-pas", nom);
            var moins = Elements.Conteneur("pas-bouton", nom + "-moins");
            moins.Add(Elements.Texte("xl titre", "−"));
            var rail = Elements.Conteneur("jauge pas-jauge");
            var remplissage = Elements.Conteneur("jauge-remplissage");
            rail.Add(remplissage);
            var plus = Elements.Conteneur("pas-bouton", nom + "-plus");
            plus.Add(Elements.Texte("xl titre", "+"));
            var valeur = Elements.Texte("chiffre base pas-valeur", "", nom + "-valeur");
            rangee.Add(moins);
            rangee.Add(rail);
            rangee.Add(plus);
            rangee.Add(valeur);
            ligne.Add(rangee);

            // Le cran se compte en entiers : 0,1 ajouté dix fois ne fait pas tout à fait 1.
            double Crans(Reglages r, int sens) => Math.Round(lire(r) / cran + sens) * cran;
            moins.AddManipulator(new Clickable(() => magasin.Modifier(r => ecrire(r, Math.Max(min, Crans(r, -1))))));
            plus.AddManipulator(new Clickable(() => magasin.Modifier(r => ecrire(r, Math.Min(max, Crans(r, +1))))));

            rafraichisseurs.Add(r =>
            {
                var v = lire(r);
                Elements.RegleLaLargeur(remplissage, (v - min) / (max - min));
                Elements.Poser(valeur, Format.Remplir(E.POURCENT_DU_REGLAGE, Math.Round(v * 100).ToString("0", CultureInfo.InvariantCulture)));
                moins.EnableInClassList("inactif", v <= min + 1e-9);
                plus.EnableInClassList("inactif", v >= max - 1e-9);
            });
        }

        void OuiNon(VisualElement page, string titre, string detail, string nom, Func<Reglages, bool> lire, Func<Reglages, bool, Reglages> ecrire)
        {
            Choix(page, titre, detail, new[]
            {
                Option(E.OUI, nom + "-oui", r => lire(r), r => ecrire(r, true)),
                Option(E.NON, nom + "-non", r => !lire(r), r => ecrire(r, false)),
            });
        }

        static (string Libelle, string Nom, Func<Reglages, bool> EstActive, Func<Reglages, Reglages> Choisir) Option(
            string libelle, string nom, Func<Reglages, bool> estActive, Func<Reglages, Reglages> choisir) =>
            (libelle, nom, estActive, choisir);

        /// Des puces, dont une seule est allumée. Rend l'étiquette de précision, que
        /// l'appelant peut réécrire (le format reconnu).
        Label Choix(VisualElement page, string titre, string detail,
            (string Libelle, string Nom, Func<Reglages, bool> EstActive, Func<Reglages, Reglages> Choisir)[] options)
        {
            var ligne = Elements.Conteneur("reglages-ligne");
            ligne.Add(Elements.Texte("titre base", titre));
            var precision = Elements.Texte("doux sm reglages-detail", detail ?? "");
            ligne.Add(precision);
            Elements.Montrer(precision, detail != null);
            var puces = Elements.Conteneur("filtres reglages-puces");
            foreach (var option in options)
            {
                var puce = Elements.Conteneur("filtre", option.Nom);
                puce.Add(Elements.Texte("sm", option.Libelle));
                var choisir = option.Choisir;
                puce.AddManipulator(new Clickable(() => magasin.Modifier(choisir)));
                puces.Add(puce);
                var estActive = option.EstActive;
                rafraichisseurs.Add(r => Elements.Marquer(puce, "actif", estActive(r)));
            }
            ligne.Add(puces);
            page.Add(ligne);
            return precision;
        }

        /// <summary>
        /// Un bouton qui efface : le premier toucher le fait changer de mot, le second dans
        /// les trois secondes agit. Le délai passe par le planificateur d'UI Toolkit (une
        /// interface ne lit pas l'horloge) : à son terme, la confirmation est annulée — d'où
        /// l'instant constant donné à `Confirmation`, qui n'a alors qu'à savoir si elle est armée.
        /// </summary>
        VisualElement BoutonAConfirmer(string libelle, string nom, string classe, Action agir)
        {
            var confirmation = new Confirmation();
            var bouton = Elements.Conteneur("bouton " + classe, nom);
            var texte = Elements.Texte("base titre", libelle);
            bouton.Add(texte);
            IVisualElementScheduledItem minuterie = null;

            void Desarmer()
            {
                confirmation.Annuler();
                minuterie?.Pause();
                Elements.Poser(texte, libelle);
                bouton.RemoveFromClassList("a-confirmer");
            }

            bouton.AddManipulator(new Clickable(() =>
            {
                if (confirmation.Toucher(0))
                {
                    Desarmer();
                    agir();
                    return;
                }
                Elements.Poser(texte, E.TOUCHER_POUR_CONFIRMER);
                bouton.AddToClassList("a-confirmer");
                minuterie?.Pause();
                minuterie = bouton.schedule.Execute(Desarmer).StartingIn(Confirmation.DELAI_MS);
            }));
            annulations.Add(Desarmer);
            return bouton;
        }
    }
}
