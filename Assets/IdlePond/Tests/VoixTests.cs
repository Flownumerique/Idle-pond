using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Les paliers de voix et le registre figé — GDD §13.1 et §14.5.
    ///
    /// « Une entrée est rédigée dans la langue que le héros avait au moment où il
    /// l'a obtenue, et n'est jamais réécrite. »
    ///
    /// Le §14.9 range cette propriété parmi les trois qui « ne se rétrofitent pas ».
    /// C'est vrai au sens le plus dur : le palier de voix d'un succès déjà tombé
    /// n'est reconstituable depuis aucune autre donnée. S'il n'est pas inscrit au
    /// moment du déclenchement, il est perdu — et aucun test ajouté plus tard ne
    /// fera autre chose que constater la perte.
    /// </summary>
    public class VoixTests
    {
        const double H = 3600;

        /// Enchaîne `n` cycles complets, chacun assez long pour valoir un franchissement.
        static EtatJeu ApresFranchissements(int n)
        {
            var etat = Reducteur.EtatInitial(1);
            for (var i = 0; i < n; i += 1) etat = Renaissance.Renaitre(Reducteur.Tick(etat, 3 * H));
            return etat;
        }

        /* ─── §13.1 — les paliers de voix ─────────────────────────────────────── */

        [Test, Description("la pente au départ, les signes au premier franchissement, les directives au troisième")]
        public void La_pente_au_depart_les_signes_au_premier_franchissement_les_directives_au_troisieme()
        {
            Assert.That(Voix.PalierApres(0), Is.EqualTo(PalierDeVoix.Pente));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES - 1), Is.EqualTo(PalierDeVoix.Pente));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES), Is.EqualTo(PalierDeVoix.Signes));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES - 1), Is.EqualTo(PalierDeVoix.Signes));
            Assert.That(Voix.PalierApres(Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES), Is.EqualTo(PalierDeVoix.Directives));
        }

        [Test, Description("le dialogue reste inatteignable : il vient du relais, pas d’un compte")]
        public void Le_dialogue_reste_inatteignable_il_vient_du_relais_pas_d_un_compte()
        {
            // §12.2, contenu de la v1.0. Il figure dans le type parce que le registre
            // d'un succès est figé pour toujours : un type qui gagnerait une valeur
            // plus tard rendrait les saves d'aujourd'hui ambiguës.
            Assert.That(Voix.PalierApres(1000), Is.Not.EqualTo(PalierDeVoix.Dialogue));
        }

        [Test, Description("la voix ne redescend jamais")]
        public void La_voix_ne_redescend_jamais()
        {
            var precedent = -1;
            var ordre = new List<PalierDeVoix> { PalierDeVoix.Pente, PalierDeVoix.Signes, PalierDeVoix.Directives, PalierDeVoix.Dialogue };
            for (var n = 0; n < 20; n += 1)
            {
                var rang = ordre.IndexOf(Voix.PalierApres(n));
                Assert.That(rang, Is.GreaterThanOrEqualTo(precedent));
                precedent = rang;
            }
        }

        [Test, Description("voixAuMoins ordonne les paliers")]
        public void VoixAuMoins_ordonne_les_paliers()
        {
            Assert.That(Voix.AuMoins(PalierDeVoix.Directives, PalierDeVoix.Signes), Is.True);
            Assert.That(Voix.AuMoins(PalierDeVoix.Signes, PalierDeVoix.Signes), Is.True);
            Assert.That(Voix.AuMoins(PalierDeVoix.Pente, PalierDeVoix.Signes), Is.False);
        }

        /* ─── §14.5 — le registre figé ────────────────────────────────────────── */

        [Test, Description("une entrée obtenue sous la pente reste sous la pente, des cycles plus tard")]
        public void Une_entree_obtenue_sous_la_pente_reste_sous_la_pente_des_cycles_plus_tard()
        {
            // C'est tout l'objet du système : « la bibliothèque devient la preuve du
            // chemin parcouru — le joueur qui relit ses vieilles entrées voit à quel
            // point il se trompait, et personne n'a eu besoin de le lui dire ».
            var etat = Reducteur.Debloquer(Reducteur.EtatInitial(1), Especes.Toutes[0].Id);
            etat = Reducteur.Tick(etat, 1);

            var premiers = etat.Permanent.Succes.ToList();
            Assert.That(premiers.Count, Is.GreaterThan(0));
            foreach (var paire in premiers)
            {
                Assert.That(paire.Value.Registre, Is.EqualTo(PalierDeVoix.Pente));
                Assert.That(paire.Value.ObtenuAuCycle, Is.EqualTo(0));
            }

            for (var i = 0; i < Constantes.FRANCHISSEMENTS_POUR_LES_DIRECTIVES; i += 1)
                etat = Renaissance.Renaitre(Reducteur.Tick(etat, 3 * H));
            Assert.That(Voix.PalierDe(etat), Is.EqualTo(PalierDeVoix.Directives));

            // Rien n'a été réécrit.
            foreach (var paire in premiers)
                Assert.That(etat.Permanent.Succes[paire.Key], Is.EqualTo(paire.Value));
        }

        [Test, Description("une entrée tombée plus tard porte la langue de ce moment-là")]
        public void Une_entree_tombee_plus_tard_porte_la_langue_de_ce_moment_la()
        {
            var etat = ApresFranchissements(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES);
            Assert.That(Voix.PalierDe(etat), Is.EqualTo(PalierDeVoix.Signes));

            var avant = new HashSet<string>(etat.Permanent.Succes.Keys);
            etat = Reducteur.Tick(Reducteur.Debloquer(etat, Especes.Toutes[0].Id), 1);
            var nouveaux = etat.Permanent.Succes.Where(p => !avant.Contains(p.Key)).ToList();

            Assert.That(nouveaux.Count, Is.GreaterThan(0));
            foreach (var paire in nouveaux)
            {
                Assert.That(paire.Value.Registre, Is.EqualTo(PalierDeVoix.Signes));
                Assert.That(paire.Value.ObtenuAuCycle, Is.EqualTo(Constantes.FRANCHISSEMENTS_POUR_LES_SIGNES));
            }
        }

        [Test, Description("un succès acquis ne se ré-obtient jamais, et son entrée ne bouge pas")]
        public void Un_succes_acquis_ne_se_re_obtient_jamais_et_son_entree_ne_bouge_pas()
        {
            // §14.4 : « les succès ne sont pas re-déclenchables. Un succès marque la
            // première fois. » Sinon le gain devient une rente indexée sur le nombre
            // de renaissances, et la courbe casse.
            var etat = Reducteur.Tick(Reducteur.Debloquer(Reducteur.EtatInitial(1), Especes.Toutes[0].Id), 1);
            var premiere = etat.Permanent.Succes.ToDictionary(p => p.Key, p => p.Value);

            etat = Renaissance.Renaitre(Reducteur.Tick(etat, 3 * H));
            etat = Reducteur.Tick(Reducteur.Debloquer(etat, Especes.Toutes[0].Id), 1);

            foreach (var paire in premiere)
                Assert.That(etat.Permanent.Succes[paire.Key], Is.EqualTo(paire.Value));
        }

        [Test, Description("la table est ordonnée par le registre, jamais par l’arrivée")]
        public void La_table_est_ordonnee_par_le_registre_jamais_par_l_arrivee()
        {
            // `EtatDeTravail` a six paliers ouverts et des espèces montées : de quoi
            // faire tomber plusieurs succès, ce qu'une partie neuve ne permet pas —
            // elle démarre avec de quoi débloquer une seule espèce.
            var etat = Reducteur.Tick(EtatDeTravail.Creer(), 600);

            var obtenus = etat.Permanent.Succes.Keys.ToList();
            Assert.That(obtenus.Count, Is.GreaterThan(1));
            Assert.That(obtenus,
                Is.EqualTo(RegistreDesSucces.Tous.Where(s => etat.Permanent.Succes.ContainsKey(s.Id)).Select(s => s.Id).ToList()));
        }

        [Test, Description("l’ordre des entrées ne dépend pas de la taille du pas")]
        public void L_ordre_des_entrees_ne_depend_pas_de_la_taille_du_pas()
        {
            // L'ordre des clefs d'un dictionnaire est ici celui de leur insertion, et le
            // test de déterminisme compare des chaînes de sérialisation. Deux succès
            // franchis dans le même intervalle arrivent ensemble sous un grand pas et
            // l'un après l'autre sous de petits pas : insérer à l'arrivée ferait
            // diverger deux parties identiques sur leur seule sauvegarde.
            var depart = EtatDeTravail.Creer();

            var enUnPas = Reducteur.Tick(depart, 600);
            var parPetitsPas = depart;
            for (var i = 0; i < 600; i += 1) parPetitsPas = Reducteur.Tick(parPetitsPas, 1);

            Assert.That(parPetitsPas.Permanent.Succes.Keys.ToList(), Is.EqualTo(enUnPas.Permanent.Succes.Keys.ToList()));
        }
    }
}
