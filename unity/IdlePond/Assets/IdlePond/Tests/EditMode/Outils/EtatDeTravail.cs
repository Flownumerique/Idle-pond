/*
 * Un état de jeu non trivial pour les tests du noyau. Port de
 * `tests/etat-de-travail.ts` : plusieurs paliers ouverts, des bancs à des places
 * différentes, des effectifs en cours de convergence et des densités inégales.
 * Un état plat ne prouverait pas grand-chose.
 */
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Tests
{
    public static class EtatDeTravail
    {
        public static EtatJeu Creer(uint graine = 12345, string contenance = "1e14")
        {
            var etat = Reducteur.EtatInitial(graine);
            etat = etat with
            {
                Cycle = etat.Cycle with { ManaCourant = GrandNombre.Lire("1e12") },
                Permanent = etat.Permanent with
                {
                    ContenanceMana = GrandNombre.Lire(contenance),
                    Densites = etat.Permanent.Densites.Select((_, index) => index * 0.13).ToArray(),
                    ProfondeurMaxAtteinte = 9,
                },
            };
            for (var i = 0; i < 6; i += 1) etat = Reducteur.Creuser(etat);
            foreach (var banc in Paliers.Bancs.Where(b => b.Palier < etat.Cycle.PaliersOuverts).ToArray())
            {
                etat = Reducteur.Convaincre(etat, banc.Id);
                for (var n = 0; n < 12 + banc.Palier; n += 1) etat = Reducteur.AcheterPlace(etat, banc.Id);
            }
            // Un peu d'avance pour que les effectifs soient en cours de route, ni à
            // zéro ni à la cible : c'est le régime où l'exponentielle peut mentir.
            return Reducteur.Tick(etat, 137);
        }
    }
}
