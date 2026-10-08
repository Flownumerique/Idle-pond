using System.Collections;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// Le menu des réglages dans la vraie Mare (spec du 2026-10-08) : il s'ouvre, et ce
    /// qu'on y change se voit sur la racine et sur le panneau. Les réglages sont modifiés
    /// par le magasin, comme le menu le fait : on vérifie l'effet, pas le doigt.
    /// </summary>
    public class ReglagesJouablesTests
    {
        UIDocument document;
        RacineDeLInterface interfaceDeLaMare;
        VisualElement racine;
        MagasinDeReglages magasin;

        [UnitySetUp]
        public IEnumerator Charger()
        {
            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            ServicesDePartie.Installer(new Partie(new HorlogeFigee(1_700_000_000_000L)));
            var chargement = SceneManager.LoadSceneAsync("Mare");
            while (!chargement.isDone) yield return null;
            racine = null;
            for (var i = 0; i < 120 && racine == null; i++)
            {
                yield return null;
                document = Object.FindAnyObjectByType<UIDocument>();
                racine = document != null ? document.rootVisualElement?.Q<VisualElement>("racine") : null;
            }
            Assert.That(racine, Is.Not.Null, "l'interface ne s'est jamais branchée");
            interfaceDeLaMare = document.GetComponent<RacineDeLInterface>();
            magasin = Boucle.ObtenirOuCreerLesReglages();
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        [UnityTest]
        public IEnumerator La_roue_est_dans_la_barre_et_le_tiroir_des_reglages_s_ouvre()
        {
            Assert.That(racine.Q<VisualElement>("roue-reglages"), Is.Not.Null);
            interfaceDeLaMare.Ouvrir(Tiroir.Reglages);
            yield return null;
            Assert.That(interfaceDeLaMare.TiroirOuvert, Is.EqualTo(Tiroir.Reglages));
            Assert.That(racine.Q<VisualElement>("tiroir-reglages").resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            foreach (var nom in new[] { "onglet-son", "onglet-affichage", "onglet-jeu", "onglet-accessibilite", "reinitialiser" })
                Assert.That(racine.Q<VisualElement>(nom), Is.Not.Null, nom);
        }

        [UnityTest]
        public IEnumerator Forcer_un_format_pose_sa_classe_et_elle_seule()
        {
            foreach (var format in new[] { FormatDAffichage.Telephone, FormatDAffichage.Tablette, FormatDAffichage.Pc })
            {
                magasin.Modifier(r => r with { Affichage = format });
                yield return null;
                var posees = Adaptation.CLASSES_DE_FORMAT.Where(racine.ClassListContains).ToList();
                Assert.That(posees, Is.EqualTo(new[] { Adaptation.ClasseDu(format) }), format.ToString());
            }
        }

        [UnityTest]
        public IEnumerator L_accessibilite_pose_ses_classes()
        {
            magasin.Modifier(r => r with { MouvementReduit = true, ContrasteRenforce = true });
            yield return null;
            Assert.That(racine.ClassListContains("mouvement-reduit"), Is.True);
            Assert.That(racine.ClassListContains("contraste"), Is.True);
            magasin.Modifier(r => r with { MouvementReduit = false, ContrasteRenforce = false });
            yield return null;
            Assert.That(racine.ClassListContains("mouvement-reduit"), Is.False);
            Assert.That(racine.ClassListContains("contraste"), Is.False);
        }

        [UnityTest]
        public IEnumerator La_taille_change_l_echelle_du_panneau_qui_est_remise_a_l_arret()
        {
            // La résolution courante est déjà celle du format reconnu (PC en batch) : la base
            // est celle que le débranchement rend à l'asset.
            magasin.Modifier(r => r with { Affichage = FormatDAffichage.Telephone, TailleDInterface = 1.25 });
            yield return null;
            var agrandie = document.panelSettings.referenceResolution;
            interfaceDeLaMare.enabled = false;
            var rendue = document.panelSettings.referenceResolution;
            Assert.That(rendue, Is.EqualTo(new Vector2Int(1080, 1920)), "l'asset ne doit pas garder l'échelle du joueur");
            Assert.That(agrandie.x, Is.EqualTo(Mathf.RoundToInt(rendue.x / 1.25f)));
        }

        [UnityTest]
        public IEnumerator La_notation_reecrit_le_mana_tout_de_suite()
        {
            var partie = ServicesDePartie.Partie;
            partie.Remplacer(partie.Etat with
            {
                Cycle = partie.Etat.Cycle with { ManaCourant = new Decimal(1.5e6) },
                Permanent = partie.Etat.Permanent with { ContenanceMana = new Decimal(1e9) },
            });
            magasin.Modifier(r => r with { Notation = Notation.Scientifique });
            Assert.That(racine.Q<Label>("mana-valeur").text, Does.Contain("e+6"));
            magasin.Modifier(r => r with { Notation = Notation.Suffixes });
            Assert.That(racine.Q<Label>("mana-valeur").text, Does.Contain("M"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Masquer_les_annonces_oublie_celles_qui_attendaient()
        {
            magasin.Modifier(r => r with { AnnoncesAffichees = false });
            yield return null;
            Assert.That(ServicesDePartie.Partie.AAnnoncer, Is.Empty);
            Assert.That(racine.Q<VisualElement>("annonces").childCount, Is.EqualTo(0));
        }
    }
}
