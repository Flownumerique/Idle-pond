using System;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Ce que l'écran d'accueil dit de la partie (spec du 2026-10-08). Pur : il se teste
    /// sans moteur. À la première partie, il n'y a rien à résumer ; sinon, le lieu le plus
    /// bas ouvert, le mana porté, et l'absence que le démarrage vient de créditer.
    /// </summary>
    public sealed record ResumeDeLaPartie(string Principal, string Lieu, string Mana, string Absence, bool NouvellePartiePossible)
    {
        public static ResumeDeLaPartie De(EtatJeu etat, AbsenceCreditee absence, bool premiereFois)
        {
            if (premiereFois) return new ResumeDeLaPartie(E.COMMENCER, null, null, null, false);
            var plusBas = Math.Max(0, etat.Cycle.PaliersOuverts - 1);
            return new ResumeDeLaPartie(
                E.CONTINUER,
                Format.NomDeLAssiseCapitale(Assises.DuPalier(plusBas).Id),
                Format.Remplir(E.ACCUEIL_MANA, Format.Montant(etat.Cycle.ManaCourant)),
                absence != null ? Format.Remplir(E.ACCUEIL_ABSENT, Format.Duree(absence.SecondesCreditees)) : null,
                true);
        }
    }
}
