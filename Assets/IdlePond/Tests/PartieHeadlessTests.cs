using System;
using System.Collections.Generic;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;
using static IdlePond.Simulateur.Simulateur;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le critère du §12 : « une partie sans UI atteint la renaissance 2 en headless ».
    ///
    /// Politique naïve — acheter le moins cher qui soit payable — pour les ACHATS
    /// seulement : si elle n'y arrive pas, c'est l'économie qui est fausse, pas la
    /// politique.
    ///
    /// La RENAISSANCE, elle, ne suit pas cette politique naïve : elle suit
    /// `DoitRenaitre` du simulateur, LA décision canonique du jeu (§6.4 — rester pour
    /// la contenance jusqu'à saturation de l'acquis de séjour, puis renaître). Écrire
    /// une seconde règle à la main ici — par exemple « renaître dès `EstBloque` » —
    /// ignorerait cette décision : tant que l'acquis n'a pas fait son travail, une
    /// renaissance précoce ne gagne presque aucune contenance, et le test cesserait
    /// de prouver l'économie pour se mettre à prouver une politique de renaissance
    /// différente de celle du jeu. Une seule définition, partagée par import
    /// (tâche 10 → tâche 11) : c'est la leçon de la tâche 9 sur les listes
    /// recopiées à la main, qui avaient dérivé en silence.
    /// </summary>
    public class PartieHeadlessTests
    {
        sealed class OptionAchat
        {
            public Decimal Cout;
            public Func<EtatJeu, EtatJeu> Appliquer;
        }

        /// Toutes les dépenses payables MAINTENANT, moins-cher-d'abord — le tri se fait au retour.
        static IReadOnlyList<OptionAchat> OptionsPayables(EtatJeu etat)
        {
            var options = new List<OptionAchat>();
            if (!Economie.ToutEstCreuse(etat))
            {
                var cible = etat.Cycle.PaliersOuverts;
                var cout = Economie.CoutDeDescente(etat, cible);
                // Hors de portée pour toujours si ça dépasse la contenance (§6.4), pas
                // seulement pour l'instant si ça dépasse le mana courant.
                // `Economie.Contenance(etat)`, jamais `Permanent.ContenanceMana` : depuis
                // que le plafond monte pendant le cycle, les deux ont cessé d'être la
                // même chose, et lire la seconde faisait croire le creusement fermé alors
                // que le noyau le rouvrait — la partie ne pouvait plus renaître du tout.
                // La règle vit dans le noyau ; on l'appelle, on ne la recopie pas.
                if (cout.Lte(Economie.Contenance(etat)) && etat.Cycle.ManaCourant.Gte(cout))
                    options.Add(new OptionAchat { Cout = cout, Appliquer = Reducteur.Creuser });
            }
            {
                var cout = Economie.CoutDeCroissance(etat, etat.Cycle.NiveauDuHeros);
                if (etat.Cycle.ManaCourant.Gte(cout)) options.Add(new OptionAchat { Cout = cout, Appliquer = Reducteur.Grandir });
            }
            foreach (var espece in Especes.Toutes)
            {
                if (espece.Palier >= etat.Cycle.PaliersOuverts) continue;
                var vivante = etat.Cycle.Especes.TryGetValue(espece.Id, out var v) ? v : null;
                var debloquee = vivante != null && vivante.Debloquee;
                var cout = debloquee ? Economie.CoutDeNiveau(etat, espece, vivante.Niveau) : Economie.CoutDeDeblocage(etat, espece);
                if (etat.Cycle.ManaCourant.Lt(cout)) continue;
                var id = espece.Id;
                options.Add(new OptionAchat
                {
                    Cout = cout,
                    Appliquer = debloquee ? (e => Reducteur.Ameliorer(e, id)) : (e => Reducteur.Debloquer(e, id)),
                });
            }
            return options;
        }

        static EtatJeu AcheterLeMoinsCher(EtatJeu etat)
        {
            OptionAchat meilleure = null;
            foreach (var option in OptionsPayables(etat))
                if (meilleure == null || option.Cout.Lt(meilleure.Cout)) meilleure = option;
            return meilleure == null ? etat : meilleure.Appliquer(etat);
        }

        [Test, Description("atteint la renaissance 2 sans interface, en moins de 200 heures de jeu")]
        public void Atteint_la_renaissance_2_sans_interface_en_moins_de_200_heures_de_jeu()
        {
            var etat = Reducteur.EtatInitial(2026);
            double secondes = 0;
            while (etat.Permanent.NombreDeRenaissances < 2 && secondes < 200 * 3600)
            {
                var avant = etat;
                etat = AcheterLeMoinsCher(etat);
                if (ReferenceEquals(etat, avant))
                {
                    // Plus aucun achat naïf possible : soit la décision canonique de
                    // renaître est mûre, soit il faut simplement laisser l'acquis de
                    // séjour avancer vers sa saturation.
                    if (DoitRenaitre(etat, POLITIQUE_PAR_DEFAUT)) etat = Renaissance.Renaitre(etat);
                    else
                    {
                        etat = Reducteur.Tick(etat, 60);
                        secondes += 60;
                    }
                }
            }
            Assert.That(etat.Permanent.NombreDeRenaissances, Is.GreaterThanOrEqualTo(2));
            Assert.That(etat.Permanent.ProfondeurMaxAtteinte, Is.GreaterThan(6));
        }
    }
}
