using System.Collections;
using IdlePond.Jeu;
using IdlePond.Jeu.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace IdlePond.TestsDeJeu
{
    /// <summary>
    /// L'écran mobile (spec du 2026-10-07) : la mare au centre, un tiroir à la fois, et
    /// Creuser posé sur la mare tant qu'il reste de la roche.
    /// </summary>
    public class TiroirsTests
    {
        static readonly (Tiroir Quel, string Nom)[] TIROIRS =
        {
            (Tiroir.Toi, "tiroir-toi"), (Tiroir.Especes, "tiroir-especes"), (Tiroir.Oeuf, "tiroir-oeuf"),
            (Tiroir.Journal, "tiroir-journal"),
        };

        RacineDeLInterface racine;
        VisualElement arbre;

        [UnitySetUp]
        public IEnumerator Preparer()
        {
            ServicesDePartie.Oublier();
            SauvegardeDeTest.Rediriger();
            ServicesDePartie.Installer(new Partie(new HorlogeFigee(1_700_000_000_000L)));
            yield return ScenePixelTests.ChargerLaMare(_ => { });
            for (var i = 0; i < 60 && (racine == null || racine.CadreDeScene == null); i++)
            {
                yield return null;
                racine = Object.FindAnyObjectByType<RacineDeLInterface>();
            }
            Assert.That(racine, Is.Not.Null);
            arbre = racine.GetComponent<UIDocument>().rootVisualElement;
        }

        [TearDown]
        public void Ranger() => ServicesDePartie.Oublier();

        static bool Visible(VisualElement e) => e.resolvedStyle.display != DisplayStyle.None;

        /// Le tiroir glisse : il est ouvert quand il porte la classe, qu'il reçoit une image
        /// après avoir été montré.
        static bool Ouvert(VisualElement e) => Visible(e) && e.ClassListContains("ouvert");

        [UnityTest]
        public IEnumerator Au_depart_la_mare_est_entiere()
        {
            yield return null;
            Assert.That(racine.TiroirOuvert, Is.EqualTo(Tiroir.Aucun));
            Assert.That(Visible(arbre.Q("tiroir")), Is.False);
            Assert.That(Visible(arbre.Q("voile-du-tiroir")), Is.False);
            Assert.That(arbre.Q("dock").childCount, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Un_seul_tiroir_est_ouvert_a_la_fois()
        {
            foreach (var (quel, nom) in TIROIRS)
            {
                racine.Ouvrir(quel);
                yield return null;
                yield return null;
                Assert.That(Ouvert(arbre.Q("tiroir")), Is.True, nom);
                Assert.That(Ouvert(arbre.Q("voile-du-tiroir")), Is.True, nom);
                Assert.That(arbre.Q<Label>("tiroir-titre").text, Is.Not.Empty, nom + " a un titre");
                Assert.That(arbre.Q<Label>("tiroir-sous-titre").text, Is.Not.Empty, nom + " a un sous-titre");
                foreach (var (autre, autreNom) in TIROIRS)
                    Assert.That(Visible(arbre.Q(autreNom)), Is.EqualTo(autre == quel), $"{nom} ouvert, {autreNom}");
            }
            racine.Ouvrir(Tiroir.Aucun);
            yield return null;
            Assert.That(arbre.Q("tiroir").ClassListContains("ouvert"), Is.False, "le tiroir redescend aussitôt");
            Assert.That(arbre.Q("voile-du-tiroir").pickingMode, Is.EqualTo(PickingMode.Ignore), "le voile fermé laisse passer les touchers");
            // Une fois redescendu, il n'est plus là du tout.
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Visible(arbre.Q("tiroir")), Is.False);
            Assert.That(Visible(arbre.Q("voile-du-tiroir")), Is.False);
        }

        [UnityTest]
        public IEnumerator Creuser_est_pose_sur_la_mare_tant_qu_il_reste_de_la_roche()
        {
            yield return null;
            var creuser = arbre.Q("creuser");
            Assert.That(creuser.parent, Is.EqualTo(arbre.Q("centre")), "Creuser vit sur la mare, pas dans un tiroir");
            Assert.That(Visible(creuser), Is.True);
            Assert.That(arbre.Q("lieu").Q<Label>().text, Is.Not.Empty, "le lieu porte son nom");
        }
    }
}
