using System.Collections;
using System.IO;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Decimal = IdlePond.Noyau.Decimal;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// L'écran d'accueil dans la vraie Mare (spec du 2026-10-08), touché au doigt : il ne
    /// paraît que si l'amorce l'a demandé, et chacun de ses boutons fait ce qu'il dit.
    /// </summary>
    public class AccueilJouableTests
    {
        RacineDeLInterface interfaceDeLaMare;
        VisualElement racine;

        IEnumerator Charger(bool accueil, bool premiereFois = false)
        {
            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            var partie = new Partie(new HorlogeFigee(1_700_000_000_000L));
            partie.Remplacer(partie.Etat with
            {
                Cycle = partie.Etat.Cycle with { ManaCourant = new Decimal(5e5) },
                Permanent = partie.Etat.Permanent with { ContenanceMana = new Decimal(1e9) },
            });
            ServicesDePartie.Installer(partie);
            if (accueil) ServicesDePartie.DemanderLAccueil(premiereFois);
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;
            racine = null;
            for (var i = 0; i < 120 && racine == null; i++)
            {
                yield return null;
                var document = Object.FindAnyObjectByType<UIDocument>();
                racine = document != null ? document.rootVisualElement?.Q<VisualElement>("racine") : null;
                interfaceDeLaMare = document != null ? document.GetComponent<RacineDeLInterface>() : null;
            }
            Assert.That(racine, Is.Not.Null, "l'interface ne s'est jamais branchée");
            // La mise en page du premier calcul : les boutons ont leur place avant qu'on les touche.
            yield return new WaitForSeconds(0.3f);
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        VisualElement Accueil => racine.Q<VisualElement>("accueil");

        bool AccueilVisible => Accueil.resolvedStyle.display == DisplayStyle.Flex;

        static double Mana => ServicesDePartie.Partie.Etat.Cycle.ManaCourant.ToNumber();

        [UnityTest]
        public IEnumerator Sans_demande_la_mare_s_ouvre_sur_le_jeu()
        {
            yield return Charger(accueil: false);
            Assert.That(AccueilVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator Continuer_entre_dans_la_mare_et_l_accueil_ne_revient_pas()
        {
            yield return Charger(accueil: true);
            Assert.That(AccueilVisible, Is.True);
            Assert.That(racine.Q<Label>("accueil-principal-libelle").text, Is.EqualTo(E.CONTINUER));
            Assert.That(ServicesDePartie.AccueilDemande, Is.False, "la demande est consommée à l'affichage");

            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-principal"));
            yield return new WaitForSeconds(0.6f);
            Assert.That(AccueilVisible, Is.False);
            Assert.That(Mana, Is.GreaterThanOrEqualTo(5e5), "continuer n'efface rien");
        }

        [UnityTest]
        public IEnumerator Nouvelle_partie_demande_deux_touchers()
        {
            yield return Charger(accueil: true);
            var nouvelle = racine.Q<VisualElement>("accueil-nouvelle");

            yield return Doigt.Toucher(nouvelle);
            Assert.That(Mana, Is.GreaterThanOrEqualTo(5e5), "un seul toucher ne fait rien");
            Assert.That(AccueilVisible, Is.True);

            yield return Doigt.Toucher(nouvelle);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Mana, Is.LessThan(1e3), "deux touchers recommencent");
            Assert.That(AccueilVisible, Is.False);
            foreach (var copie in Directory.GetFiles(Boucle.DossierDeSauvegarde(), "idlepond.avant-reinitialisation-*.json"))
                File.Delete(copie);
        }

        [UnityTest]
        public IEnumerator Une_nouvelle_partie_armee_puis_continuer_n_efface_rien()
        {
            yield return Charger(accueil: true);
            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-nouvelle"));
            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-principal"));
            yield return new WaitForSeconds(0.6f);
            Assert.That(Mana, Is.GreaterThanOrEqualTo(5e5));
        }

        [UnityTest]
        public IEnumerator Les_reglages_s_ouvrent_par_dessus_et_ramenent_a_l_accueil()
        {
            yield return Charger(accueil: true);
            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-reglages"));
            Assert.That(interfaceDeLaMare.TiroirOuvert, Is.EqualTo(Tiroir.Reglages));
            Assert.That(AccueilVisible, Is.False, "le tiroir se voit, l'accueil s'efface le temps des réglages");

            interfaceDeLaMare.Ouvrir(Tiroir.Aucun);
            yield return new WaitForSeconds(0.5f);
            Assert.That(AccueilVisible, Is.True);
            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-principal"));
            yield return new WaitForSeconds(0.6f);
            Assert.That(AccueilVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator A_la_premiere_partie_commencer_seul()
        {
            yield return Charger(accueil: true, premiereFois: true);
            Assert.That(racine.Q<Label>("accueil-principal-libelle").text, Is.EqualTo(E.COMMENCER));
            Assert.That(racine.Q<VisualElement>("accueil-nouvelle").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(racine.Q<VisualElement>("accueil-resume").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
    
        [UnityTest]
        public IEnumerator Un_succes_arrive_pendant_l_accueil_s_annonce_apres_l_entree()
        {
            yield return Charger(accueil: true);
            ServicesDePartie.Partie.Convaincre("vairon");
            yield return new WaitForSeconds(0.5f);
            Assert.That(ServicesDePartie.Partie.AAnnoncer, Is.Not.Empty, "le succès est bien tombé");
            Assert.That(racine.Q<VisualElement>("annonces").childCount, Is.EqualTo(0), "rien ne s'annonce sous l'accueil");

            yield return new WaitForSeconds(6.5f);
            Assert.That(ServicesDePartie.Partie.AAnnoncer, Is.Not.Empty, "l'annonce attend l'entrée, elle n'expire pas");

            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-principal"));
            yield return new WaitForSeconds(0.3f);
            Assert.That(racine.Q<VisualElement>("annonces").childCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator Effacer_depuis_les_reglages_de_l_accueil_rend_un_accueil_de_premiere_partie()
        {
            yield return Charger(accueil: true);
            yield return Doigt.Toucher(racine.Q<VisualElement>("accueil-reglages"));
            yield return new WaitForSeconds(0.5f);
            yield return Doigt.Toucher(racine.Q<VisualElement>("onglet-jeu"));
            var effacer = racine.Q<VisualElement>("reinitialiser");
            yield return Doigt.Toucher(effacer);
            yield return Doigt.Toucher(effacer);
            yield return new WaitForSeconds(0.5f);

            Assert.That(Mana, Is.LessThan(1e3));
            Assert.That(AccueilVisible, Is.True);
            Assert.That(racine.Q<Label>("accueil-principal-libelle").text, Is.EqualTo(E.COMMENCER));
            Assert.That(racine.Q<VisualElement>("accueil-nouvelle").resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            foreach (var copie in Directory.GetFiles(Boucle.DossierDeSauvegarde(), "idlepond.avant-reinitialisation-*.json"))
                File.Delete(copie);
        }
    }
}
