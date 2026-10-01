using System;
using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests.Outils
{
    /// <summary>
    /// Un joueur simulé à la cadence du jeu, pour mesurer ce qui se voit à l'écran :
    /// il achète le moins cher dès qu'il peut, à chaque seconde.
    ///
    /// Ce n'est pas le simulateur du §12 — celui-là mesure l'économie complète sur
    /// 62 paliers et quinze cycles. Celui-ci joue l'assise I telle qu'elle est
    /// LIVRÉE, à la vitesse où un humain la verrait, et sert au plancher du §8.4.
    /// </summary>
    public static class Joueur
    {
        static EtatJeu Depenser(EtatJeu etat)
        {
            var courant = etat;
            for (var garde = 0; garde < 200; garde += 1)
            {
                var plafond = Economie.Contenance(courant);
                (Decimal Cout, Func<EtatJeu, EtatJeu> Appliquer)? meilleure = null;
                void Retenir(Decimal cout, Func<EtatJeu, EtatJeu> appliquer)
                {
                    if (cout.Gt(plafond)) return;
                    if (meilleure == null || cout.Lt(meilleure.Value.Cout)) meilleure = (cout, appliquer);
                }
                if (!Economie.ToutEstCreuse(courant)) Retenir(Economie.CoutDeDescente(courant, courant.Cycle.PaliersOuverts), Reducteur.Creuser);
                Retenir(Economie.CoutDeCroissance(courant, courant.Cycle.NiveauDuHeros), Reducteur.Grandir);
                foreach (var espece in Especes.Toutes)
                {
                    if (espece.Palier >= courant.Cycle.PaliersOuverts) continue;
                    var vivante = courant.Cycle.Especes.TryGetValue(espece.Id, out var v) ? v : null;
                    var niveau = vivante != null && vivante.Debloquee ? vivante.Niveau : 0;
                    var id = espece.Id;
                    Retenir(
                        niveau == 0 ? Economie.CoutDeDeblocage(courant, espece) : Economie.CoutDeNiveau(courant, espece, niveau),
                        niveau == 0 ? (e => Reducteur.Debloquer(e, id)) : (e => Reducteur.Ameliorer(e, id)));
                }
                if (meilleure == null) return courant;
                var choix = meilleure.Value;
                if (choix.Cout.Gt(courant.Cycle.ManaCourant)) return courant;
                var suivant = choix.Appliquer(courant);
                if (ReferenceEquals(suivant, courant)) return courant;
                courant = suivant;
            }
            return courant;
        }

        /// Les trente premières minutes, seconde par seconde, sur l'assise I livrée.
        public static IReadOnlyList<(string Id, double InstantSecondes)> JoueUneDemiHeure(long graine = 1)
        {
            var etat = Reducteur.EtatInitial(graine, Assises.PALIERS_LIVRES);
            var releve = new List<(string Id, double InstantSecondes)>();
            etat = Depenser(etat);
            for (var instant = 1; instant <= Constantes.FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES; instant += 1)
            {
                var resultat = Reducteur.TickDetaille(etat, 1);
                etat = Depenser(resultat.Etat);
                foreach (var id in resultat.Declenches) releve.Add((id, instant));
            }
            return releve;
        }

        /// <summary>
        /// Rejoue `secondes` de partie, à la cadence du jeu, sur le monde livré.
        ///
        /// `limite` par défaut `Assises.PALIERS_LIVRES` — déporté en `int?` parce que
        /// `PALIERS_LIVRES` est dérivé à l'exécution (`Toutes[0].NombreDePaliers`), pas
        /// une constante de compilation : un paramètre par défaut C# exige l'inverse.
        /// </summary>
        public static EtatJeu Rejoue(double secondes, int? limite = null)
        {
            var etat = Reducteur.EtatInitial(3, limite ?? Assises.PALIERS_LIVRES);
            etat = Depenser(etat);
            for (var t = 0; t < secondes; t += 1)
            {
                var r = Reducteur.TickDetaille(etat, 1);
                etat = Depenser(RegleDesSucces.EnregistrerIntervalleDeSucces(r.Etat, r.Declenches));
            }
            return etat;
        }
    }
}
