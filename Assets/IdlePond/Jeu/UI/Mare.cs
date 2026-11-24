using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le tiroir « Espèces » : ceux qui vivent ici, lieu par lieu (maquette du 2026-10-07,
    /// révision 2).
    ///
    /// L'écran ne dit jamais « palier » ni « assise » (§3) : il dit le nom propre du lieu
    /// et une profondeur en brasses. Le joueur ne choisit jamais quelle espèce va où — le
    /// placement est fixé par l'auteur (§4.2). Il convainc, il monte des crans, il creuse.
    ///
    /// Trois états par espèce :
    ///   - atteinte et convaincue : son nom, ses crans, son débit (qui ouvre la table de
    ///     captation) — en pente, sans chiffre, tant que le héros ne sait pas compter les
    ///     siens —, la barre vers le prochain seuil de jalon, et « Monter » ;
    ///   - atteinte, pas encore convaincue : « un banc s'attarde », et « Convaincre » ;
    ///   - plus bas : grisée, sans nom, avec la profondeur où on la trouvera.
    /// Sous le lieu, le suivant, verrouillé : on voit qu'il y a de la suite, pas ce qu'elle est.
    ///
    /// Une carte par espèce, créée une fois puis mise à jour : c'est le nombre de creux ouverts
    /// qui décide de son état, pas une liste reconstruite.
    /// </summary>
    public sealed class Mare
    {
        sealed class Carte
        {
            public Espece Espece;
            public VisualElement Racine;
            public Icone Icone;
            public VisualElement Vignette;
            /// Le portrait posé dans la vignette (le représentant de la tribu), ou null.
            public UnityEngine.Sprite Portrait;
            public UnityEngine.Color Couleur;
            public Label Nom;
            public VisualElement Detail;
            public Label Crans;
            public Label Production;
            public VisualElement Lien;
            public VisualElement Remplissage;
            public Label Seuil;
            public Label Indice;
            public BoutonDAchat Achat;
        }

        readonly Action<string> surDeblocage;
        readonly Action<string> surNiveau;
        readonly Action<string> surLot;
        readonly Action<string> surCaptation;
        readonly Func<string, UnityEngine.Sprite> portraitDe;
        readonly VisualElement puceUnCran;
        readonly VisualElement puceAuSeuil;

        /// Comment « Monter » achète : un cran, ou un lot jusqu'au prochain seuil. Un choix
        /// d'écran, gardé le temps de la séance — il ne change aucune règle.
        bool parLot;
        sealed class Groupe
        {
            public Assise Lieu;
            public VisualElement Tete;
            public VisualElement Liste;
            public Icone Ouvert;
            public Icone Ferme;
            public Label SousTitre;
            public Label Compte;
            public readonly List<Carte> Cartes = new List<Carte>();
        }

        readonly List<Carte> cartes = new List<Carte>();
        readonly List<Groupe> groupes = new List<Groupe>();

        // Le clic lit l'état d'AU MOMENT du clic : le niveau de l'espèce décide si l'on
        // débloque ou si l'on monte, et il a pu changer depuis le dernier tick.
        EtatJeu dernier;

        /// Creuser n'est plus ici : c'est un bouton posé sur la mare (`Creusement`, spec du
        /// 2026-10-07), visible sans ouvrir ce tiroir.
        /// `portraitDe` : le portrait du représentant d'une tribu (Codex v1.4), pris au
        /// catalogue d'art ; null tant qu'il n'est pas dessiné — la vignette garde son icône.
        public Mare(VisualElement racine, Action<string> surDeblocage, Action<string> surNiveau,
            Action<string> surCaptation, Action<string> surLot = null, Func<string, UnityEngine.Sprite> portraitDe = null)
        {
            this.portraitDe = portraitDe;
            this.surDeblocage = surDeblocage;
            this.surNiveau = surNiveau;
            this.surLot = surLot ?? surNiveau;
            this.surCaptation = surCaptation;
            racine.AddToClassList("panneau");

            // En tête : la façon de monter. Deux puces, comme les vues de « Débloquer ».
            var facon = Elements.Conteneur("filtres filtres-mana", "facon-de-monter");
            facon.Add(Elements.Texte("doux sm facon-de-monter-titre", E.FACON_DE_MONTER));
            puceUnCran = Puce("monter-un-cran", E.MONTER_UN_CRAN, false);
            puceAuSeuil = Puce("monter-au-seuil", E.MONTER_JUSQU_AU_SEUIL, true);
            facon.Add(puceUnCran);
            facon.Add(puceAuSeuil);
            racine.Add(facon);
            ChoisirLaFacon(false);

            // Un groupe par lieu que le jeu livre. Seuls les lieux atteints, et le premier
            // qu'on n'atteint pas encore, se montrent ; au-delà de ce qui est livré, « ??? ».
            foreach (var lieu in Assises.Toutes.Where(a => a.IndexPremierPalier < Assises.PALIERS_LIVRES))
            {
                var groupe = EnTeteDeLieu(lieu);
                racine.Add(groupe.Tete);
                foreach (var espece in Especes.DeLAssise(lieu.Id)) groupe.Liste.Add(Creer(espece, groupe));
                racine.Add(groupe.Liste);
                groupes.Add(groupe);
            }
            if (Assises.Toutes.Any(a => a.IndexPremierPalier >= Assises.PALIERS_LIVRES))
            {
                var plusLoin = Elements.Conteneur("lieu-a-venir");
                plusLoin.Add(new Icone(Icones.CADENAS));
                plusLoin.Add(Elements.Texte("doux base", E.INCONNU));
                racine.Add(plusLoin);
            }
        }

        VisualElement Puce(string nom, string libelle, bool lot)
        {
            var puce = Elements.Conteneur("filtre", nom);
            puce.Add(Elements.Texte("sm", libelle));
            puce.AddManipulator(new Clickable(() =>
            {
                ChoisirLaFacon(lot);
                if (dernier != null) Rafraichir(dernier);
            }));
            return puce;
        }

        void ChoisirLaFacon(bool lot)
        {
            parLot = lot;
            Elements.Marquer(puceUnCran, "actif", !lot);
            Elements.Marquer(puceAuSeuil, "actif", lot);
        }

        static Groupe EnTeteDeLieu(Assise lieu)
        {
            var groupe = new Groupe { Lieu = lieu };
            groupe.Tete = Elements.Conteneur("entete-de-lieu");
            groupe.Ouvert = new Icone(Icones.ESPECES);
            groupe.Ferme = new Icone(Icones.CADENAS);
            groupe.Tete.Add(groupe.Ouvert);
            groupe.Tete.Add(groupe.Ferme);
            var noms = Elements.Conteneur("entete-de-lieu-noms");
            noms.Add(Elements.Texte("titre base", Format.NomDeLAssiseCapitale(lieu.Id)));
            groupe.SousTitre = Elements.Texte("doux xs");
            noms.Add(groupe.SousTitre);
            groupe.Tete.Add(noms);
            groupe.Compte = Elements.Texte("chiffre sm");
            groupe.Tete.Add(groupe.Compte);
            groupe.Liste = Elements.Conteneur("liste");
            return groupe;
        }

        public void Rafraichir(EtatJeu etat)
        {
            dernier = etat;
            var mana = etat.Cycle.ManaCourant;
            var limite = Economie.Contenance(etat);

            foreach (var carte in cartes)
            {
                var espece = carte.Espece;
                var atteinte = espece.Palier < etat.Cycle.PaliersOuverts;
                var niveau = NiveauDe(etat, espece.Id);

                Elements.Marquer(carte.Racine, "verrouillee", !atteinte);
                // Inatteinte, sa vignette est grise (celle du .uss) : sa couleur la trahirait.
                carte.Icone.style.color = atteinte ? new StyleColor(carte.Couleur) : new StyleColor(StyleKeyword.Null);
                Elements.Montrer(carte.Achat.Racine, atteinte);
                Elements.Montrer(carte.Detail, atteinte && niveau > 0);
                PoserLePortrait(carte, atteinte && niveau > 0 ? portraitDe?.Invoke(espece.Id) : null);
                Elements.Montrer(carte.Indice, !(atteinte && niveau > 0));
                if (!atteinte)
                {
                    Elements.Poser(carte.Nom, E.INCONNU);
                    Elements.Marquer(carte.Nom, "tu", true);
                    Elements.Poser(carte.Indice, Format.Remplir(E.A_PROFONDEUR, Format.Profondeur(espece.Palier)));
                    continue;
                }

                var cout = niveau == 0 ? Economie.CoutDeDeblocage(etat, espece) : Economie.CoutDeNiveau(etat, espece, niveau);
                Elements.Poser(carte.Nom, niveau == 0 ? E.ESPECE_QUI_S_ATTARDE : Format.NomDeLEspece(espece.Id));
                Elements.Marquer(carte.Nom, "tu", niveau == 0);
                if (niveau == 0) Elements.Poser(carte.Indice, Format.Profondeur(espece.Palier));
                else
                {
                    Elements.Poser(carte.Crans, Format.CransDeLaCarte(niveau));
                    var compte = Economie.LitLesDebits(etat);
                    Elements.Poser(carte.Production, compte
                        ? Format.Remplir(E.DEBIT, Format.Montant(Economie.ProductionDeLEspece(etat, espece)))
                        : Format.PenteDeLEspece(Economie.PartDeLEspece(etat, espece)));
                    Elements.Marquer(carte.Lien, "lien-muet", !compte);
                    var prochain = Constantes.SEUILS_DE_JALON.FirstOrDefault(s => s.Seuil > niveau);
                    Elements.RegleLaLargeur(carte.Remplissage, prochain == null ? 1 : niveau / (double)prochain.Seuil);
                    Elements.Poser(carte.Seuil, prochain == null
                        ? E.TOUS_LES_SEUILS
                        : Format.Remplir(E.PROCHAIN_SEUIL, prochain.MultiplicateurCumule.ToString("0", CultureInfo.InvariantCulture), prochain.Seuil));
                }
                var libelle = niveau == 0 ? E.VERBE_DEBLOQUER : E.MONTER;
                if (parLot && niveau > 0)
                {
                    // Le lot payable à l'instant : son nombre de crans et son prix entier. S'il
                    // n'y en a aucun, le bouton garde le prix d'un cran, celui qu'on attend.
                    var lot = Economie.LotDeNiveaux(etat, espece);
                    cout = lot.Cout;
                    if (lot.Crans > 1) libelle = Format.Remplir(E.MONTER_PLUSIEURS, lot.Crans);
                }
                Elements.Poser(carte.Achat.Libelle, libelle);
                Elements.Poser(carte.Achat.Cout, Format.Cout(cout));
                carte.Achat.Progresser(mana, cout);
                carte.Achat.Regler(mana.Gte(cout) && cout.Lte(limite));
            }

            var premierFerme = true;
            Assise precedent = null;
            foreach (var groupe in groupes)
            {
                var lieu = groupe.Lieu;
                var atteint = lieu.IndexPremierPalier < etat.Cycle.PaliersOuverts;
                // Le premier lieu qu'on n'atteint pas se montre, verrouillé ; les suivants non.
                var visible = atteint || premierFerme;
                if (!atteint) premierFerme = false;
                Elements.Montrer(groupe.Tete, visible);
                Elements.Montrer(groupe.Liste, visible);
                Elements.Marquer(groupe.Tete, "verrouillee", !atteint);
                Elements.Montrer(groupe.Ouvert, atteint);
                Elements.Montrer(groupe.Ferme, !atteint);
                var convaincues = groupe.Cartes.Count(c => NiveauDe(etat, c.Espece.Id) > 0);
                Elements.Poser(groupe.Compte, Format.Remplir(E.CONVAINCUS_SUR, convaincues, groupe.Cartes.Count));
                if (atteint)
                {
                    var finDuLieu = lieu.IndexPremierPalier + lieu.NombreDePaliers;
                    var plusBas = Math.Max(0, Math.Min(etat.Cycle.PaliersOuverts, finDuLieu) - 1);
                    Elements.Poser(groupe.SousTitre, Format.Remplir(E.JUSQU_A, Format.Profondeur(plusBas)));
                }
                else if (precedent != null)
                    Elements.Poser(groupe.SousTitre, Format.Remplir(E.PLUS_BAS_QUE, Format.NomDeLAssise(precedent.Id)));
                precedent = lieu;
            }
        }

        /// Convaincue, une tribu montre son représentant dans la vignette ; sinon, l'icône à sa
        /// couleur (ou grise). On ne retouche le style que s'il change.
        static void PoserLePortrait(Carte carte, UnityEngine.Sprite portrait)
        {
            if (carte.Portrait == portrait) return;
            carte.Portrait = portrait;
            carte.Vignette.style.backgroundImage = portrait != null ? new StyleBackground(portrait) : new StyleBackground(StyleKeyword.Null);
            Elements.Marquer(carte.Vignette, "vignette-portrait", portrait != null);
            Elements.Montrer(carte.Icone, portrait == null);
        }

        static int NiveauDe(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee ? vivante.Niveau : 0;

        VisualElement Creer(Espece espece, Groupe groupe)
        {
            var carte = new Carte { Espece = espece };
            groupe.Cartes.Add(carte);
            carte.Racine = Elements.Conteneur("rangee-espece", "espece-" + espece.Id);

            // La vignette porte la couleur de l'espèce, celle de ses nageurs dans la scène.
            var vignette = Elements.Conteneur("vignette");
            carte.Vignette = vignette;
            carte.Icone = new Icone(Icones.TOI);
            carte.Couleur = Briques.Couleur(RegistreDArt.CouleursDEspece(espece.Rang).Corps);
            vignette.Add(carte.Icone);
            carte.Racine.Add(vignette);

            var infos = Elements.Conteneur("rangee-espece-infos");
            carte.Nom = Elements.Texte("titre base");
            infos.Add(carte.Nom);

            carte.Detail = Elements.Conteneur("rangee-espece-detail");
            var ligne = Elements.Conteneur("rangee-espece-ligne");
            carte.Crans = Elements.Texte("chiffre doux sm");
            carte.Production = Elements.Texte("chiffre mana sm");
            var captation = Elements.Conteneur("lien");
            captation.Add(carte.Production);
            // Le détail de captation ne s'ouvre qu'à qui sait compter les siens.
            captation.AddManipulator(new Clickable(() =>
            {
                if (dernier != null && Economie.LitLesDebits(dernier)) surCaptation(espece.Id);
            }));
            carte.Lien = captation;
            ligne.Add(carte.Crans);
            ligne.Add(captation);
            carte.Detail.Add(ligne);
            var rail = Elements.Conteneur("rail-de-seuil");
            carte.Remplissage = Elements.Conteneur("rail-de-seuil-remplissage");
            rail.Add(carte.Remplissage);
            carte.Detail.Add(rail);
            carte.Seuil = Elements.Texte("souffle-doux xs");
            carte.Detail.Add(carte.Seuil);
            infos.Add(carte.Detail);

            carte.Indice = Elements.Texte("doux xs");
            infos.Add(carte.Indice);
            carte.Racine.Add(infos);

            carte.Achat = new BoutonDAchat("bouton-compact", E.VERBE_DEBLOQUER, () =>
            {
                if (dernier == null) return;
                if (NiveauDe(dernier, espece.Id) == 0) surDeblocage(espece.Id);
                else if (parLot) surLot(espece.Id);
                else surNiveau(espece.Id);
            });
            carte.Racine.Add(carte.Achat.Racine);

            cartes.Add(carte);
            return carte.Racine;
        }
    }
}
