using System;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le détail de captation (§8.2, GDD §14.3) et l'invariant qu'il protège :
    /// « aucun effet chiffré flottant, un nœud cible toujours un TermeDeFormule
    /// nommé » (§7.5 règle 3).
    ///
    /// `MultiplicateurDeProfondeur` multipliait la production sans être nommé dans
    /// le registre depuis une tâche antérieure, et aucun test ne l'avait remarqué.
    /// La tâche 9 ferme ce trou en même temps qu'elle ajoute un troisième
    /// multiplicateur global — la densité — à la production (finding 3,
    /// RESULTATS.md) : c'est l'occasion de verrouiller que production et registre
    /// ne peuvent plus diverger en silence.
    /// </summary>
    public class CaptationTests
    {
        /* ─── la production totale ne double-compte aucun multiplicateur global ─── */

        [Test, Description("somme des productions par espèce, plus le débit du héros, égale le total")]
        public void Somme_des_productions_par_espece_plus_le_debit_du_heros_egale_le_total()
        {
            // État non trivial : plusieurs paliers ouverts, plusieurs espèces à des
            // niveaux différents, des densités inégales — un état plat ne prouverait
            // rien, tous les multiplicateurs y valant 1.
            var etat = EtatDeTravail.Creer();

            // Le terme du héros vient du détail PUBLIÉ, pas d'une valeur refabriquée
            // ici : une copie à la main de `DEBIT_HEROS` et des multiplicateurs
            // globaux est exactement ce qui avait laissé la densité de côté (revue de
            // qualité de la tâche 9) sans que ce test s'en aperçoive. En passant par
            // `DetailDuHeros` et par `MultiplicateursGlobaux` — la même fonction que
            // `ProductionTotaleParSeconde` utilise —, un multiplicateur global ajouté
            // demain sans y être répercuté fait diverger ce test, pas seulement la
            // production réelle.
            var ligneDebitHeros = Economie.DetailDuHeros(etat).FirstOrDefault(ligne => ligne.Terme == TermeDeFormule.DebitHeros);
            if (ligneDebitHeros == null) throw new InvalidOperationException("le détail publié ne porte plus debit_heros");
            var termeDuHeros = new Decimal(ligneDebitHeros.Valeur).Mul(Economie.MultiplicateursGlobaux(etat));

            var sommeDesEspeces = Especes.Toutes.Aggregate(
                new Decimal(0),
                (somme, espece) => somme.Add(Economie.ProductionDeLEspece(etat, espece)));

            Comparateur.ComparerATolerance(sommeDesEspeces.Add(termeDuHeros), Economie.ProductionTotaleParSeconde(etat));
        }

        /* ─── aucun multiplicateur global ne flotte hors du registre (§7.5 règle 3) ─── */

        [Test, Description("le produit des termes nommés du détail de captation égale la production réelle")]
        public void Le_produit_des_termes_nommes_du_detail_de_captation_egale_la_production_reelle()
        {
            // Un test qui se contenterait de LISTER les termes d'aujourd'hui ne dirait
            // rien d'un cinquième multiplicateur ajouté en silence demain : la liste
            // contiendrait toujours les mêmes noms. Celui-ci recalcule la production à
            // partir du SEUL détail publié — le produit de ses lignes — et le compare à
            // la vraie fonction de production : tout multiplicateur ajouté à
            // `ProductionDeLEspece` sans ligne correspondante ici fait diverger les
            // deux, quel que soit le nom qu'il porterait.
            var etat = EtatDeTravail.Creer();
            var especesOuvertes = Especes.Toutes.Where(e => e.Palier < etat.Cycle.PaliersOuverts).ToList();
            Assert.That(especesOuvertes.Count, Is.GreaterThan(0));

            foreach (var espece in especesOuvertes)
            {
                var produit = Economie.DetailDeCaptation(etat, espece).Aggregate(
                    new Decimal(1),
                    (acc, ligne) => acc.Mul(ligne.Valeur));
                Comparateur.ComparerATolerance(produit, Economie.ProductionDeLEspece(etat, espece));
            }
        }
    }
}
