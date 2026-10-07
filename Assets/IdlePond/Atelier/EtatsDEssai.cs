using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Atelier
{
    /// <summary>
    /// Les états d'essai des ateliers. Des fonctions pures : un `EtatJeu` entre, un autre
    /// sort. Ce sont les SEULES qui fabriquent un état hors des actes du joueur — tout ce qui
    /// a un acte (grandir, renaître) passe par la `Partie`, pas par ici.
    /// </summary>
    public static class EtatsDEssai
    {
        /// Les espèces que le jeu livre : celles des paliers livrés (la Noue et le Gour).
        public static readonly IReadOnlyList<Espece> EspecesLivrees =
            Especes.Toutes.Where(e => e.Palier < Assises.PALIERS_LIVRES).ToList();

        /* ─── Les briques ───────────────────────────────────────────────────────────*/

        /// La Noue ouverte jusqu'au fond, et sa marque sur le héros.
        public static EtatJeu Noue(EtatJeu e)
        {
            var couches = e.Permanent.Couches.Contains(Assises.Toutes[0].Id)
                ? e.Permanent.Couches
                : e.Permanent.Couches.Append(Assises.Toutes[0].Id).ToArray();
            return AvecPaliersOuverts(e, Assises.Toutes[0].NombreDePaliers) with { Permanent = e.Permanent with { Couches = couches } };
        }

        public static EtatJeu AvecNiveauDuHeros(EtatJeu e, int niveau) =>
            e with { Cycle = e.Cycle with { NiveauDuHeros = Math.Max(1, niveau) } };

        /// Le niveau du prochain seuil de stade ; le même niveau s'il n'y en a plus.
        public static int NiveauDuStadeSuivant(int niveau)
        {
            foreach (var seuil in Gabarits.SEUILS_DE_STADE) if (seuil > niveau) return seuil;
            return niveau;
        }

        /// Une espèce débloquée au niveau donné ; au niveau 0, elle redevient verrouillée.
        public static EtatJeu AvecEspece(EtatJeu e, string id, int niveau)
        {
            var especes = new Dictionary<string, EtatEspece>(e.Cycle.Especes);
            if (niveau <= 0) especes.Remove(id);
            else especes[id] = new EtatEspece(true, niveau);
            return e with { Cycle = e.Cycle with { Especes = especes } };
        }

        public static EtatJeu AvecToutesLesEspeces(EtatJeu e, int niveau) =>
            EspecesLivrees.Aggregate(e, (etat, espece) => AvecEspece(etat, espece.Id, niveau));

        /// Le mana courant à une part de la contenance : au-delà de 0,85, l'eau se trouble.
        public static EtatJeu AvecPartDeContenance(EtatJeu e, double part) =>
            AvecMana(e, Economie.Contenance(e).Mul(Math.Max(0, part)));

        public static EtatJeu AvecMana(EtatJeu e, Decimal mana) =>
            e with { Cycle = e.Cycle with { ManaCourant = mana } };

        /// Entre 1 et le contenu livré : le noyau ne connaît pas de palier au-delà.
        public static EtatJeu AvecPaliersOuverts(EtatJeu e, int paliers) =>
            e with { Cycle = e.Cycle with { PaliersOuverts = Math.Max(1, Math.Min(e.LimiteDeContenu, paliers)) } };

        public static EtatJeu AvecSouffleEnPlus(EtatJeu e, double souffle) =>
            e with { Permanent = e.Permanent with { Souffle = e.Permanent.Souffle.Add(souffle) } };

        /* ─── Les états types ───────────────────────────────────────────────────────*/

        public static EtatJeu Depart(IHorloge horloge) => Partie.NouvelEtat(horloge);

        /// La Noue ouverte, toutes ses espèces, le héros au stade 2, l'eau claire.
        public static EtatJeu MiPartie(IHorloge horloge)
        {
            var e = AvecToutesLesEspeces(Noue(Depart(horloge)), 25);
            return AvecPartDeContenance(AvecNiveauDuHeros(e, 16), 0.4);
        }

        public static EtatJeu ContenancePleine(IHorloge horloge) => AvecPartDeContenance(MiPartie(horloge), 1.0);

        public static EtatJeu ApresRenaissance(IHorloge horloge) => Renaissance.Renaitre(MiPartie(horloge));
    }
}
