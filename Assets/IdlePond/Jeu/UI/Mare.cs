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
    ///     captation), la barre vers le prochain seuil de jalon, et « Monter » ;
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
            public UnityEngine.Color Couleur;
            public Label Nom;
            public VisualElement Detail;
            public Label Crans;
            public Label Production;
            public VisualElement Remplissage;
            public Label Seuil;
            public Label Indice;
            public BoutonDAchat Achat;
        }

        readonly Action<string> surDeblocage;
        readonly Action<string> surNiveau;
        readonly Action<string> surCaptation;
        readonly List<Carte> cartes = new List<Carte>();
        readonly Label sousTitre;
        readonly Label compte;
        readonly Assise lieu = Assises.Toutes[0];

        // Le clic lit l'état d'AU MOMENT du clic : le niveau de l'espèce décide si l'on
        // débloque ou si l'on monte, et il a pu changer depuis le dernier tick.
        EtatJeu dernier;

        /// Creuser n'est plus ici : c'est un bouton posé sur la mare (`Creusement`, spec du
        /// 2026-10-07), visible sans ouvrir ce tiroir.
        public Mare(VisualElement racine, Action<string> surDeblocage, Action<string> surNiveau,
            Action<string> surCaptation)
        {
            this.surDeblocage = surDeblocage;
            this.surNiveau = surNiveau;
            this.surCaptation = surCaptation;
            racine.AddToClassList("panneau");

            var (tete, sous, nombre) = EnTeteDeLieu(Format.NomDeLAssiseCapitale(lieu.Id), false);
            sousTitre = sous;
            compte = nombre;
            racine.Add(tete);
            var liste = Elements.Conteneur("liste");
            foreach (var espece in Especes.DeLAssise(lieu.Id)) liste.Add(Creer(espece));
            racine.Add(liste);

            // Le lieu suivant, verrouillé : son nom s'il en a un, ses espèces en « ??? ».
            if (Assises.Toutes.Count > 1)
            {
                var suivant = Assises.Toutes[1];
                var especes = Especes.DeLAssise(suivant.Id);
                var (teteSuivante, sousSuivant, nombreSuivant) = EnTeteDeLieu(Format.NomDeLAssiseCapitale(suivant.Id), true);
                Elements.Poser(sousSuivant, Format.Remplir(E.PLUS_BAS_QUE, Format.NomDeLAssise(lieu.Id)));
                Elements.Poser(nombreSuivant, Format.Remplir(E.CONVAINCUS_SUR, 0, especes.Count));
                racine.Add(teteSuivante);
                var inconnues = Elements.Conteneur("liste");
                foreach (var _ in especes) inconnues.Add(Inconnue());
                racine.Add(inconnues);
            }
            if (Assises.Toutes.Count > 2)
            {
                var plusLoin = Elements.Conteneur("lieu-a-venir");
                plusLoin.Add(new Icone(Icones.CADENAS));
                plusLoin.Add(Elements.Texte("doux base", E.INCONNU));
                racine.Add(plusLoin);
            }
        }

        static (VisualElement Tete, Label SousTitre, Label Compte) EnTeteDeLieu(string nom, bool verrouille)
        {
            var tete = Elements.Conteneur("entete-de-lieu" + (verrouille ? " verrouillee" : ""));
            tete.Add(new Icone(verrouille ? Icones.CADENAS : Icones.ESPECES));
            var noms = Elements.Conteneur("entete-de-lieu-noms");
            noms.Add(Elements.Texte("titre base", nom));
            var sous = Elements.Texte("doux xs");
            noms.Add(sous);
            tete.Add(noms);
            var compte = Elements.Texte("chiffre sm");
            tete.Add(compte);
            return (tete, sous, compte);
        }

        static VisualElement Inconnue()
        {
            var carte = Elements.Conteneur("rangee-espece verrouillee");
            var vignette = Elements.Conteneur("vignette");
            vignette.Add(new Icone(Icones.CADENAS));
            carte.Add(vignette);
            carte.Add(Elements.Texte("doux base", E.INCONNU));
            return carte;
        }

        public void Rafraichir(EtatJeu etat)
        {
            dernier = etat;
            var mana = etat.Cycle.ManaCourant;
            var limite = Economie.Contenance(etat);
            var convaincues = 0;

            foreach (var carte in cartes)
            {
                var espece = carte.Espece;
                var atteinte = espece.Palier < etat.Cycle.PaliersOuverts;
                var niveau = NiveauDe(etat, espece.Id);
                if (niveau > 0) convaincues++;

                Elements.Marquer(carte.Racine, "verrouillee", !atteinte);
                // Inatteinte, sa vignette est grise (celle du .uss) : sa couleur la trahirait.
                carte.Icone.style.color = atteinte ? new StyleColor(carte.Couleur) : new StyleColor(StyleKeyword.Null);
                Elements.Montrer(carte.Achat.Racine, atteinte);
                Elements.Montrer(carte.Detail, atteinte && niveau > 0);
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
                    Elements.Poser(carte.Crans, Format.Remplir(E.CRANS_ET_DEBIT, niveau.ToString(CultureInfo.InvariantCulture)));
                    Elements.Poser(carte.Production, Format.Remplir(E.DEBIT, Format.Montant(Economie.ProductionDeLEspece(etat, espece))));
                    var prochain = Constantes.SEUILS_DE_JALON.FirstOrDefault(s => s.Seuil > niveau);
                    Elements.RegleLaLargeur(carte.Remplissage, prochain == null ? 1 : niveau / (double)prochain.Seuil);
                    Elements.Poser(carte.Seuil, prochain == null
                        ? E.TOUS_LES_SEUILS
                        : Format.Remplir(E.PROCHAIN_SEUIL, prochain.MultiplicateurCumule.ToString("0", CultureInfo.InvariantCulture), prochain.Seuil));
                }
                Elements.Poser(carte.Achat.Libelle, niveau == 0 ? E.VERBE_DEBLOQUER : E.MONTER);
                Elements.Poser(carte.Achat.Cout, Format.Cout(cout));
                carte.Achat.Progresser(mana, cout);
                carte.Achat.Regler(mana.Gte(cout) && cout.Lte(limite));
            }

            var finDuLieu = lieu.IndexPremierPalier + lieu.NombreDePaliers;
            var plusBas = Math.Max(0, Math.Min(etat.Cycle.PaliersOuverts, finDuLieu) - 1);
            Elements.Poser(sousTitre, Format.Remplir(E.JUSQU_A, Format.Profondeur(plusBas)));
            Elements.Poser(compte, Format.Remplir(E.CONVAINCUS_SUR, convaincues, cartes.Count));
        }

        static int NiveauDe(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee ? vivante.Niveau : 0;

        VisualElement Creer(Espece espece)
        {
            var carte = new Carte { Espece = espece };
            carte.Racine = Elements.Conteneur("rangee-espece", "espece-" + espece.Id);

            // La vignette porte la couleur de l'espèce, celle de ses nageurs dans la scène.
            var vignette = Elements.Conteneur("vignette");
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
            captation.AddManipulator(new Clickable(() => surCaptation(espece.Id)));
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
                else surNiveau(espece.Id);
            });
            carte.Racine.Add(carte.Achat.Racine);

            cartes.Add(carte);
            return carte.Racine;
        }
    }
}
