/*
 * Un joueur simulé à la cadence du jeu. Port de `tests/joueur.ts` : il achète le
 * moins cher dès qu'il peut, à chaque seconde, sur l'assise I telle qu'elle est
 * LIVRÉE. Sert au plancher du §8.4.
 */
using System;
using System.Collections.Generic;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Tests
{
    public sealed record Declenchement(string Id, double InstantSecondes);

    public static class Joueur
    {
        private static EtatJeu Depenser(EtatJeu etat)
        {
            var courant = etat;
            for (var garde = 0; garde < 200; garde += 1)
            {
                var plafond = Economie.Contenance(courant);
                GrandNombre? meilleurCout = null;
                Func<EtatJeu, EtatJeu> meilleurActe = null;
                void Retenir(GrandNombre cout, Func<EtatJeu, EtatJeu> appliquer)
                {
                    if (cout.Gt(plafond)) return;
                    if (meilleurCout == null || cout.Lt(meilleurCout.Value))
                    {
                        meilleurCout = cout;
                        meilleurActe = appliquer;
                    }
                }

                if (!Economie.ToutEstCreuse(courant)) Retenir(Economie.CoutDeDescente(courant, courant.Cycle.PaliersOuverts), Reducteur.Creuser);
                for (var palier = 0; palier < courant.Cycle.PaliersOuverts; palier += 1)
                {
                    foreach (var banc in Paliers.Liste[palier].Bancs)
                    {
                        var place = courant.Cycle.Bancs.EssayerDeLire(banc.Id, out var b) ? b.Place : 0;
                        var id = banc.Id;
                        if (place == 0) Retenir(Economie.CoutDeConviction(courant, banc), e => Reducteur.Convaincre(e, id));
                        else Retenir(Economie.CoutDePlace(courant, banc, place), e => Reducteur.AcheterPlace(e, id));
                    }
                }

                if (meilleurCout == null) return courant;
                if (meilleurCout.Value.Gt(courant.Cycle.ManaCourant)) return courant;
                var suivant = meilleurActe(courant);
                if (ReferenceEquals(suivant, courant)) return courant;
                courant = suivant;
            }
            return courant;
        }

        /// <summary>Les trente premières minutes, seconde par seconde, sur l'assise I livrée.</summary>
        public static IReadOnlyList<Declenchement> JoueUneDemiHeure(uint graine = 1)
        {
            var etat = Reducteur.EtatInitial(graine, Assises.PaliersLivres);
            var releve = new List<Declenchement>();
            etat = Depenser(etat);
            for (var instant = 1; instant <= Constantes.FenetreDuPlancherDeCadenceSecondes; instant += 1)
            {
                var resultat = Reducteur.TickDetaille(etat, 1);
                etat = Depenser(resultat.Etat);
                foreach (var id in resultat.Declenches) releve.Add(new Declenchement(id, instant));
            }
            return releve;
        }

        /// <summary>Rejoue `secondes` de partie, à la cadence du jeu, sur le monde livré.</summary>
        public static EtatJeu Rejoue(int secondes, int? limite = null)
        {
            var etat = Reducteur.EtatInitial(3, limite ?? Assises.PaliersLivres);
            etat = Depenser(etat);
            for (var t = 0; t < secondes; t += 1)
            {
                var r = Reducteur.TickDetaille(etat, 1);
                etat = Depenser(SystemeDeSucces.EnregistrerIntervalleDeSucces(r.Etat, r.Declenches));
            }
            return etat;
        }
    }
}
