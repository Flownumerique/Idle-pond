using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using UnityEngine.UIElements;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Le tiroir « Débloquer » (spec du 2026-10-10, maquette du canevas « IdlePond — Écran
    /// principal mobile ») : trois vues, choisies par des puces en tête.
    ///
    ///   - Lieux : chaque lieu atteint, sa maîtrise (ses creux ouverts dans cette vie) et ce
    ///     qu'elle ouvre ; le lieu suivant, fermé, avec ce qu'il faut pour l'atteindre ;
    ///     au-delà de ce qui est livré, « ??? ».
    ///   - Bonus de lieu : les achats propres à chaque lieu.
    ///   - Techniques : les achats qui valent partout.
    ///
    /// L'écran ne dit jamais « palier », « assise » ni « zone » (Codex §5) : il dit le nom du
    /// lieu, la maîtrise, des creux. Aucune règle ici (§5.3) : ce qui est ouvert, ce qui
    /// coûte combien, tout vient d'`Economie`. Les rangées sont créées une fois puis mises à
    /// jour, comme celles des espèces.
    /// </summary>
    public sealed class Debloquer
    {
        enum Vue { Lieux, Bonus, Techniques }

        static readonly (Vue Vue, string Libelle)[] VUES =
        {
            (Vue.Lieux, E.ONGLET_LIEUX),
            (Vue.Bonus, E.ONGLET_BONUS_DE_LIEU),
            (Vue.Techniques, E.ONGLET_TECHNIQUES),
        };

        sealed class Rangee
        {
            public Bonus Bonus;
            public VisualElement Racine;
            public Label Rang;
            public VisualElement Remplissage;
            public Label Indice;
            public Label Max;
            public BoutonDAchat Achat;
        }

        sealed class Jalon
        {
            public Bonus Bonus;
            public VisualElement Racine;
            public Icone Atteint;
            public Icone Ferme;
        }

        sealed class CarteDeLieu
        {
            public Assise Lieu;
            public VisualElement Racine;
            public Icone Ouvert;
            public Icone Ferme;
            public Label SousTitre;
            public Label Etat;
            public VisualElement Maitrise;
            public Label Compte;
            public VisualElement Remplissage;
            public readonly List<Jalon> Jalons = new List<Jalon>();
        }

        sealed class GroupeDeBonus
        {
            public Assise Lieu;
            public VisualElement Tete;
            public Label Compte;
            public VisualElement Liste;
        }

        readonly Action<string> surAchat;
        readonly List<VisualElement> puces = new List<VisualElement>();
        readonly Dictionary<Vue, VisualElement> vues = new Dictionary<Vue, VisualElement>();
        readonly List<Rangee> rangees = new List<Rangee>();
        readonly List<CarteDeLieu> cartes = new List<CarteDeLieu>();
        readonly List<GroupeDeBonus> groupes = new List<GroupeDeBonus>();
        readonly Label lieuxAtteints;
        readonly VisualElement aVenir;

        static IEnumerable<Assise> LieuxLivres() => Assises.Toutes.Where(a => a.IndexPremierPalier < Assises.PALIERS_LIVRES);

        public Debloquer(VisualElement racine, Action<string> surAchat)
        {
            this.surAchat = surAchat;
            racine.AddToClassList("panneau");

            var filtres = Elements.Conteneur("filtres filtres-mana", "debloquer-vues");
            for (var i = 0; i < VUES.Length; i++)
            {
                var choisie = VUES[i].Vue;
                var puce = Elements.Conteneur("filtre", "debloquer-vue-" + choisie.ToString().ToLowerInvariant());
                puce.Add(Elements.Texte("sm", VUES[i].Libelle));
                puce.AddManipulator(new Clickable(() => Choisir(choisie)));
                filtres.Add(puce);
                puces.Add(puce);
            }
            racine.Add(filtres);

            /* — Les lieux ———————————————————————————————————————————————————————— */
            var lieux = Elements.Conteneur("debloquer-vue", "debloquer-lieux");
            lieuxAtteints = Elements.Texte("doux sm debloquer-explique");
            lieux.Add(lieuxAtteints);
            foreach (var lieu in LieuxLivres())
            {
                var carte = CreerCarteDeLieu(lieu);
                lieux.Add(carte.Racine);
                cartes.Add(carte);
            }
            aVenir = Elements.Conteneur("lieu-a-venir");
            aVenir.Add(new Icone(Icones.CADENAS));
            aVenir.Add(Elements.Texte("doux base", E.INCONNU));
            lieux.Add(aVenir);
            racine.Add(lieux);
            vues[Vue.Lieux] = lieux;

            /* — Les bonus de lieu ———————————————————————————————————————————————— */
            var bonus = Elements.Conteneur("debloquer-vue", "debloquer-bonus");
            bonus.Add(Elements.Texte("doux sm debloquer-explique", E.BONUS_DU_LIEU_EXPLIQUE));
            foreach (var lieu in LieuxLivres())
            {
                var groupe = new GroupeDeBonus { Lieu = lieu };
                groupe.Tete = Elements.Conteneur("entete-de-lieu");
                groupe.Tete.Add(new Icone(Icones.ESPECES));
                var noms = Elements.Conteneur("entete-de-lieu-noms");
                noms.Add(Elements.Texte("titre base", Format.NomDeLAssiseCapitale(lieu.Id)));
                groupe.Tete.Add(noms);
                groupe.Compte = Elements.Texte("chiffre sm");
                groupe.Tete.Add(groupe.Compte);
                groupe.Liste = Elements.Conteneur("liste");
                foreach (var b in RegistreDesBonus.DuLieu(lieu.Id)) groupe.Liste.Add(CreerRangee(b));
                bonus.Add(groupe.Tete);
                bonus.Add(groupe.Liste);
                groupes.Add(groupe);
            }
            racine.Add(bonus);
            vues[Vue.Bonus] = bonus;

            /* — Les techniques ——————————————————————————————————————————————————— */
            var techniques = Elements.Conteneur("debloquer-vue", "debloquer-techniques");
            techniques.Add(Elements.Texte("doux sm debloquer-explique", E.TECHNIQUES_EXPLIQUE));
            var liste = Elements.Conteneur("liste");
            foreach (var b in RegistreDesBonus.Techniques()) liste.Add(CreerRangee(b));
            techniques.Add(liste);
            racine.Add(techniques);
            vues[Vue.Techniques] = techniques;

            Choisir(Vue.Lieux);
        }

        void Choisir(Vue choisie)
        {
            for (var i = 0; i < VUES.Length; i++) Elements.Marquer(puces[i], "actif", VUES[i].Vue == choisie);
            foreach (var paire in vues) Elements.Montrer(paire.Value, paire.Key == choisie);
        }

        /* ─── La construction ─────────────────────────────────────────────────────── */

        static string[] IconeDu(GenreDeBonus genre)
        {
            switch (genre)
            {
                case GenreDeBonus.Production: return Icones.GOUTTE;
                case GenreDeBonus.Confort: return Icones.SABLIER;
                case GenreDeBonus.Verbe: return Icones.ENGRENAGE;
                default: return Icones.BAISSE;
            }
        }

        static string GenreEcrit(GenreDeBonus genre)
        {
            switch (genre)
            {
                case GenreDeBonus.Production: return E.GENRE_PRODUCTION;
                case GenreDeBonus.Confort: return E.GENRE_CONFORT;
                case GenreDeBonus.Verbe: return E.GENRE_VERBE;
                default: return E.GENRE_COUT;
            }
        }

        VisualElement CreerRangee(Bonus bonus)
        {
            var texte = Textes.DuBonus(bonus.Id);
            var rangee = new Rangee { Bonus = bonus };
            rangee.Racine = Elements.Conteneur("rangee-espece rangee-bonus", "bonus-" + bonus.Id);

            var vignette = Elements.Conteneur("vignette vignette-bonus");
            vignette.Add(new Icone(IconeDu(bonus.Genre)));
            rangee.Racine.Add(vignette);

            var infos = Elements.Conteneur("rangee-espece-infos");
            var tete = Elements.Conteneur("rangee-bonus-tete");
            tete.Add(Elements.Texte("titre base", texte.Nom));
            tete.Add(Elements.Texte("xs etiquette-de-genre", GenreEcrit(bonus.Genre)));
            infos.Add(tete);
            infos.Add(Elements.Texte("doux sm", Format.Remplir(texte.Effet, Format.Pourcent(bonus.Part))));
            var rail = Elements.Conteneur("rail-de-seuil");
            rangee.Remplissage = Elements.Conteneur("rail-de-seuil-remplissage");
            rail.Add(rangee.Remplissage);
            infos.Add(rail);
            rangee.Rang = Elements.Texte("chiffre doux xs");
            infos.Add(rangee.Rang);
            rangee.Indice = Elements.Texte("tu xs");
            infos.Add(rangee.Indice);
            rangee.Racine.Add(infos);

            rangee.Achat = new BoutonDAchat("bouton-compact", E.ACHETER, () => surAchat(bonus.Id));
            rangee.Racine.Add(rangee.Achat.Racine);
            rangee.Max = Elements.Texte("chiffre mana base bonus-max", E.AU_MAXIMUM);
            rangee.Racine.Add(rangee.Max);

            rangees.Add(rangee);
            return rangee.Racine;
        }

        static CarteDeLieu CreerCarteDeLieu(Assise lieu)
        {
            var carte = new CarteDeLieu { Lieu = lieu };
            carte.Racine = Elements.Conteneur("carte carte-de-lieu", "lieu-" + lieu.Id);

            var tete = Elements.Conteneur("carte-de-lieu-tete");
            carte.Ouvert = new Icone(Icones.ESPECES);
            carte.Ferme = new Icone(Icones.CADENAS);
            tete.Add(carte.Ouvert);
            tete.Add(carte.Ferme);
            var noms = Elements.Conteneur("entete-de-lieu-noms");
            noms.Add(Elements.Texte("titre base", Format.NomDeLAssiseCapitale(lieu.Id)));
            carte.SousTitre = Elements.Texte("doux xs");
            noms.Add(carte.SousTitre);
            tete.Add(noms);
            carte.Etat = Elements.Texte("sm carte-de-lieu-etat");
            tete.Add(carte.Etat);
            carte.Racine.Add(tete);

            carte.Maitrise = Elements.Conteneur("carte-de-lieu-maitrise");
            var ligne = Elements.Conteneur("rangee entre");
            ligne.Add(Elements.Texte("doux sm", E.MAITRISE));
            carte.Compte = Elements.Texte("chiffre doux sm");
            ligne.Add(carte.Compte);
            carte.Maitrise.Add(ligne);
            var rail = Elements.Conteneur("rail-de-seuil");
            carte.Remplissage = Elements.Conteneur("rail-de-seuil-remplissage");
            rail.Add(carte.Remplissage);
            carte.Maitrise.Add(rail);

            // Ce que la maîtrise ouvre, dans l'ordre où elle l'ouvre.
            foreach (var bonus in RegistreDesBonus.DuLieu(lieu.Id).OrderBy(b => b.MaitriseRequise))
            {
                var jalon = new Jalon { Bonus = bonus };
                jalon.Racine = Elements.Conteneur("carte-de-lieu-jalon");
                jalon.Atteint = new Icone(Icones.ETINCELLE);
                jalon.Ferme = new Icone(Icones.CADENAS);
                jalon.Racine.Add(jalon.Atteint);
                jalon.Racine.Add(jalon.Ferme);
                jalon.Racine.Add(Elements.Texte("chiffre sm carte-de-lieu-jalon-niveau",
                    Format.Remplir(E.MAITRISE_SUR, bonus.MaitriseRequise, lieu.NombreDePaliers)));
                jalon.Racine.Add(Elements.Texte("sm", Textes.DuBonus(bonus.Id).Nom));
                carte.Maitrise.Add(jalon.Racine);
                carte.Jalons.Add(jalon);
            }
            carte.Racine.Add(carte.Maitrise);
            return carte;
        }

        /* ─── La mise à jour ──────────────────────────────────────────────────────── */

        public void Rafraichir(EtatJeu etat)
        {
            var mana = etat.Cycle.ManaCourant;

            foreach (var rangee in rangees)
            {
                var bonus = rangee.Bonus;
                var rang = Economie.RangDeBonus(etat, bonus.Id);
                var ouvert = Economie.BonusOuvert(etat, bonus);
                var auMax = rang >= bonus.RangMax;

                Elements.Marquer(rangee.Racine, "verrouillee", !ouvert);
                Elements.Poser(rangee.Rang, Format.Remplir(E.RANG_SUR, rang, bonus.RangMax));
                Elements.RegleLaLargeur(rangee.Remplissage, rang / (double)bonus.RangMax);
                Elements.Montrer(rangee.Indice, !ouvert);
                Elements.Montrer(rangee.Max, ouvert && auMax);
                Elements.Montrer(rangee.Achat.Racine, ouvert && !auMax);
                if (!ouvert)
                {
                    Elements.Poser(rangee.Indice, bonus.Assise == null
                        ? Format.Remplir(E.TECHNIQUE_S_OUVRE_A, bonus.MaitriseRequise)
                        : Format.Remplir(E.BONUS_S_OUVRE_A, bonus.MaitriseRequise));
                    continue;
                }
                if (auMax) continue;
                var cout = Economie.CoutDeBonus(etat, bonus);
                Elements.Poser(rangee.Achat.Cout, Format.Cout(cout));
                rangee.Achat.Progresser(mana, cout);
                rangee.Achat.Regler(mana.Gte(cout));
            }

            // Les lieux : les atteints, et le premier qu'on n'atteint pas, fermé.
            var premierFerme = true;
            Assise precedent = null;
            var atteints = 0;
            foreach (var carte in cartes)
            {
                var lieu = carte.Lieu;
                var atteint = lieu.IndexPremierPalier < etat.Cycle.PaliersOuverts;
                if (atteint) atteints++;
                var visible = atteint || premierFerme;
                if (!atteint) premierFerme = false;
                Elements.Montrer(carte.Racine, visible);
                Elements.Marquer(carte.Racine, "verrouillee", !atteint);
                Elements.Montrer(carte.Ouvert, atteint);
                Elements.Montrer(carte.Ferme, !atteint);
                Elements.Montrer(carte.Maitrise, atteint);
                Elements.Poser(carte.Etat, atteint ? E.LIEU_OUVERT : E.VERROUILLE);
                if (atteint)
                {
                    var maitrise = Economie.MaitriseDuLieu(etat, lieu);
                    Elements.Poser(carte.Compte, Format.Remplir(E.MAITRISE_SUR, maitrise, lieu.NombreDePaliers));
                    Elements.RegleLaLargeur(carte.Remplissage, maitrise / (double)lieu.NombreDePaliers);
                    var plusBas = Math.Max(0, Math.Min(etat.Cycle.PaliersOuverts, lieu.IndexPremierPalier + lieu.NombreDePaliers) - 1);
                    Elements.Poser(carte.SousTitre, Format.Remplir(E.JUSQU_A, Format.Profondeur(plusBas)));
                    foreach (var jalon in carte.Jalons)
                    {
                        var ouvert = maitrise >= jalon.Bonus.MaitriseRequise;
                        Elements.Montrer(jalon.Atteint, ouvert);
                        Elements.Montrer(jalon.Ferme, !ouvert);
                        Elements.Marquer(jalon.Racine, "a-venir", !ouvert);
                    }
                }
                else if (precedent != null)
                    Elements.Poser(carte.SousTitre, Format.Remplir(E.LIEU_FERME_CONDITION, Format.NomDeLAssise(precedent.Id)));
                precedent = lieu;
            }
            Elements.Poser(lieuxAtteints, Format.Remplir(E.LIEUX_ATTEINTS, atteints, Assises.Toutes.Count));
            Elements.Montrer(aVenir, premierFerme && Assises.Toutes.Any(a => a.IndexPremierPalier >= Assises.PALIERS_LIVRES));

            // Un lieu pas encore atteint n'a pas encore de bonus à montrer.
            foreach (var groupe in groupes)
            {
                var atteint = groupe.Lieu.IndexPremierPalier < etat.Cycle.PaliersOuverts;
                Elements.Montrer(groupe.Tete, atteint);
                Elements.Montrer(groupe.Liste, atteint);
                var achetes = RegistreDesBonus.DuLieu(groupe.Lieu.Id).Sum(b => Economie.RangDeBonus(etat, b.Id));
                var total = RegistreDesBonus.DuLieu(groupe.Lieu.Id).Sum(b => b.RangMax);
                Elements.Poser(groupe.Compte, Format.Remplir(E.MAITRISE_SUR, achetes, total));
            }
        }

        /// <summary>
        /// Combien de bonus on peut acheter là, maintenant : la pastille du dock. Ouvert, pas
        /// au maximum, et payable.
        /// </summary>
        public static int Achetables(EtatJeu etat) =>
            RegistreDesBonus.Tous.Count(b =>
                Economie.BonusOuvert(etat, b) && !Economie.BonusAuMaximum(etat, b)
                && etat.Cycle.ManaCourant.Gte(Economie.CoutDeBonus(etat, b)));
    }
}
