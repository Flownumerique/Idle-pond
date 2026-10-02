using System.Linq;
using IdlePond.Editeur;
using IdlePond.Jeu.Scene;
using NUnit.Framework;
using UnityEngine;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le générateur de provisoires (spec DA §5) : déterministe, et il ne touche jamais à un
    /// vrai dessin.
    /// </summary>
    public class GenerateurDeSpritesTests
    {
        [Test, Description("deux passages produisent les mêmes fichiers, aux mêmes octets")]
        public void Deux_passages_produisent_les_memes_octets()
        {
            var a = GenerateurDeSprites.Produire();
            var b = GenerateurDeSprites.Produire();
            Assert.That(b.Keys, Is.EqualTo(a.Keys));
            foreach (var chemin in a.Keys) Assert.That(b[chemin], Is.EqualTo(a[chemin]), chemin);
        }

        [Test, Description("un fichier absent s'écrit ; un provisoire intact se réécrit ; un vrai dessin est protégé")]
        public void Un_vrai_dessin_est_protege()
        {
            var provisoire = new byte[] { 1, 2, 3 };
            var dessin = new byte[] { 9, 9, 9 };
            Assert.That(GenerateurDeSprites.PeutEcrire(false, null, null), Is.True);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, GenerateurDeSprites.Etiquette(provisoire), provisoire), Is.True);
            // Un dessin déposé à la place d'un provisoire garde son .meta : l'empreinte le trahit.
            Assert.That(GenerateurDeSprites.PeutEcrire(true, GenerateurDeSprites.Etiquette(provisoire), dessin), Is.False);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, null, dessin), Is.False);
            Assert.That(GenerateurDeSprites.PeutEcrire(true, "autre chose", dessin), Is.False);
        }

        [Test, Description("un catalogue vide ou incomplet ne lève jamais : il rend null")]
        public void Un_catalogue_incomplet_ne_leve_jamais()
        {
            var catalogue = ScriptableObject.CreateInstance<CatalogueDArt>();
            try
            {
                Assert.That(catalogue.CorpsDe(0, 0), Is.Null);
                Assert.That(catalogue.CorpsDe(9, -1), Is.Null);
                Assert.That(catalogue.MarqueDe("noue", 0), Is.Null);
                Assert.That(catalogue.MarqueDe(null, 0), Is.Null);
                Assert.That(catalogue.EspeceDe("vairon", 0), Is.Null);
                Assert.That(catalogue.DecorDe("assise-4"), Is.Null);
                catalogue.Corps = new[] { new CatalogueDArt.Images { Sprites = null } };
                Assert.That(catalogue.CorpsDe(0, 0), Is.Null);
            }
            finally { Object.DestroyImmediate(catalogue); }
        }
    }
}
