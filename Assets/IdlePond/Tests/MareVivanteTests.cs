using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// Ce qui fait vivre la mare sans rien toucher à l'économie : une mare qui change à chaque
    /// renaissance (chantier 6), les saisons cosmétiques (décision du 2026-11-18), et les
    /// retours visuels des événements de jeu.
    /// </summary>
    public class MareVivanteTests
    {
        static readonly double[] INSTANTS = Enumerable.Range(0, 300).Select(i => i * 0.173).ToArray();

        /* ─── Une mare qui change à chaque renaissance ─────────────────────────────── */

        [Test, Description("à chaque renaissance, la mare gagne : libellules, hydre, étincelles de Sève")]
        public void La_mare_change_a_chaque_renaissance()
        {
            Assert.That(new[] { 0, 3, 6, 50 }.Select(Faune.Libellules), Is.EqualTo(new[] { 2, 3, 4, 4 }));
            Assert.That(new[] { 0, 3, 4, 9 }.Select(Faune.Hydres), Is.EqualTo(new[] { 1, 1, 2, 2 }));
            Assert.That(new[] { 0, 1, 5, 50 }.Select(Faune.Etincelles), Is.EqualTo(new[] { 0, 2, 10, 12 }));
            for (var r = 0; r < 10; r++)
            {
                Assert.That(Faune.Libellules(r + 1), Is.GreaterThanOrEqualTo(Faune.Libellules(r)), "rien ne se perd");
                Assert.That(Faune.Etincelles(r + 1), Is.GreaterThanOrEqualTo(Faune.Etincelles(r)));
            }
        }

        [Test, Description("les libellules et les hydres de plus restent dans la première bande")]
        public void Les_betes_de_plus_restent_dans_le_champ()
        {
            foreach (var t in INSTANTS)
            {
                for (var n = 0; n < Faune.Libellules(10); n++)
                {
                    var p = Faune.Libellule(n, t);
                    Assert.That(p.X >= 0 && p.X + 14 <= Gabarits.LARGEUR_VISEE && p.Y >= -Gabarits.HAUTEUR_DE_BANDE && p.Y + 7 <= 0, Is.True, $"libellule {n}");
                }
                for (var n = 0; n < Faune.Hydres(10); n++)
                {
                    var p = Faune.Hydre(t, n);
                    Assert.That(p.X >= 0 && p.X + 7 <= Gabarits.LARGEUR_VISEE && p.Y + 12 <= 0, Is.True, $"hydre {n}");
                }
            }
        }

        [Test, Description("une étincelle de Sève monte dans sa bande, et s'éteint un moment")]
        public void Une_etincelle_monte_dans_sa_bande()
        {
            for (var n = 0; n < 12; n++)
            {
                var vues = INSTANTS.Select(t => Faune.Etincelle(3, n, t)).ToList();
                Assert.That(vues.Any(p => p == null), Is.True, "elle s'éteint en haut");
                foreach (var p in vues.Where(p => p != null))
                {
                    Assert.That(p.X >= 0 && p.X < Gabarits.LARGEUR_VISEE, Is.True);
                    Assert.That(p.Y >= -4 * Gabarits.HAUTEUR_DE_BANDE && p.Y < -3 * Gabarits.HAUTEUR_DE_BANDE, Is.True, "dans la bande 3");
                }
            }
        }

        /* ─── Les saisons cosmétiques ──────────────────────────────────────────────── */

        [Test, Description("le frai au printemps, la crue à l'automne, le gel l'hiver, rien l'été")]
        public void Les_saisons_suivent_le_calendrier()
        {
            Assert.That(Enumerable.Range(1, 12).Select(Saisons.De), Is.EqualTo(new[]
            {
                Saison.Gel, Saison.Gel, Saison.Frai, Saison.Frai, Saison.Frai, Saison.Aucune,
                Saison.Aucune, Saison.Aucune, Saison.Aucune, Saison.Crue, Saison.Crue, Saison.Gel,
            }));
            Assert.That(Saisons.BetesDeLaBelleSaison(Saison.Gel), Is.False, "l'hiver, ni libellules ni grenouilles");
            Assert.That(Saisons.BetesDeLaBelleSaison(Saison.Frai), Is.True);
        }

        [Test, Description("le mois se lit par l'horloge du jeu")]
        public void Le_mois_se_lit_par_l_horloge_du_jeu()
        {
            // Le 15 juillet 2026 à midi UTC : juillet partout sur Terre.
            var juillet = new HorlogeFigee(new System.DateTimeOffset(2026, 7, 15, 12, 0, 0, System.TimeSpan.Zero).ToUnixTimeMilliseconds());
            Assert.That(Horloge.MoisLocal(juillet), Is.EqualTo(7));
            Assert.That(Saisons.De(Horloge.MoisLocal(juillet)), Is.EqualTo(Saison.Aucune));
        }

        [Test, Description("les œufs du frai sur la corniche de leur bande ; la glace sur la surface")]
        public void Le_frai_et_la_glace_sont_a_leur_place()
        {
            var corniche = Puits.CornicheDe(2);
            foreach (var g in Saisons.Frai(2))
            {
                Assert.That(g.X >= corniche.Debut && g.X + 7 <= corniche.Fin, Is.True);
                Assert.That(g.Y, Is.EqualTo(corniche.Sol));
            }
            Assert.That(Saisons.Frai(0), Has.Count.EqualTo(Saisons.GRAPPES_PAR_BANDE));
            Assert.That(Saisons.YDeLaGlace + Faune.DESSINS[Faune.GLACE].Cadre.Hauteur, Is.EqualTo(0), "la glace affleure");
            Assert.That(Gabarits.LARGEUR_DU_DECOR % Faune.DESSINS[Faune.GLACE].Cadre.Largeur, Is.EqualTo(0), "la glace pave le décor");
        }

        [Test, Description("les feuilles de la crue traversent de gauche à droite, dans les bandes ouvertes")]
        public void Les_feuilles_de_la_crue_traversent()
        {
            foreach (var t in INSTANTS)
                for (var n = 0; n < Saisons.FEUILLES; n++)
                {
                    var f = Saisons.Feuille(n, t, 4);
                    Assert.That(f.X >= -6 && f.X <= Gabarits.LARGEUR_VISEE, Is.True);
                    Assert.That(f.Y >= -4 * Gabarits.HAUTEUR_DE_BANDE - 60 && f.Y < 0, Is.True, $"feuille {n} à {t}");
                    Assert.That(f.Image, Is.InRange(0, 1));
                }
            var avant = Saisons.Feuille(0, 1.0, 3).X;
            Assert.That(Saisons.Feuille(0, 2.0, 3).X, Is.GreaterThan(avant), "le courant les emporte à droite");
        }

        /* ─── Les retours visuels ──────────────────────────────────────────────────── */

        [Test, Description("chaque événement qui se voit a sa gerbe, au bon endroit")]
        public void Chaque_evenement_a_sa_gerbe()
        {
            var roche = Gerbes.De(new EvenementDeJeu(GenreDEvenement.CreuxOuvert, null, 4), 1, 5, null);
            Assert.That(roche.Genre, Is.EqualTo(GenreDeGerbe.RocheQuiCede));
            Assert.That(roche.Origine.Y, Is.EqualTo(-4 * Gabarits.HAUTEUR_DE_BANDE - 4), "en haut de la bande ouverte");

            var seuil = Gerbes.De(new EvenementDeJeu(GenreDEvenement.SeuilFranchi, "vairon", 10), 1, 5, id => new Pixel(42, -30));
            Assert.That(seuil.Origine, Is.EqualTo(new Pixel(42, -30)), "autour de la tribu");
            Assert.That(Gerbes.De(new EvenementDeJeu(GenreDEvenement.SeuilFranchi, "vairon", 10), 1, 5, id => null), Is.Null, "pas de tribu qui nage, pas de gerbe");

            var ralliement = Gerbes.De(new EvenementDeJeu(GenreDEvenement.EspeceConvaincue, "chabot", 1), 1, 9, null);
            Assert.That(ralliement.Genre, Is.EqualTo(GenreDeGerbe.Ralliement));

            var trouble = Gerbes.De(new EvenementDeJeu(GenreDEvenement.EauTroublee), 1, 3, null);
            Assert.That(trouble.Origine.Y, Is.EqualTo(-3 * Gabarits.HAUTEUR_DE_BANDE + 4), "au fond");

            Assert.That(Gerbes.De(new EvenementDeJeu(GenreDEvenement.HerosGrandi, null, 5), 1, 3, null), Is.Null);
        }

        [Test, Description("une gerbe jaillit, s'écarte, puis s'éteint ; ses couleurs sont les siennes")]
        public void Une_gerbe_jaillit_puis_s_eteint()
        {
            foreach (GenreDeGerbe genre in System.Enum.GetValues(typeof(GenreDeGerbe)))
            {
                var gerbe = new Gerbe(genre, new Pixel(180, -100), 10.0);
                Assert.That(Gerbes.Points(gerbe, 9.9), Is.Empty, "pas avant");
                Assert.That(Gerbes.Points(gerbe, 10.0 + Gerbes.DureeDe(genre)), Is.Empty, "pas après");
                Assert.That(Gerbes.EstFinie(gerbe, 10.0 + Gerbes.DureeDe(genre)), Is.True);
                var debut = Gerbes.Points(gerbe, 10.05);
                Assert.That(debut, Has.Count.EqualTo(Gerbes.NombreDe(genre)), genre.ToString());
                Assert.That(debut.All(p => p.Couleur >= 0 && p.Couleur < Gerbes.CouleursDe(genre).Count), Is.True);
                double Etendue(IReadOnlyList<PointDeGerbe> pts) => pts.Max(p => System.Math.Abs(p.X - 180) + System.Math.Abs(p.Y + 100));
                Assert.That(Etendue(Gerbes.Points(gerbe, 10.0 + Gerbes.DureeDe(genre) * 0.6)), Is.GreaterThan(Etendue(debut)), "elle s'écarte");
                Assert.That(Gerbes.Points(gerbe, 10.0 + Gerbes.DureeDe(genre) * 0.9).Count, Is.LessThan(debut.Count), "elle s'éteint en clignotant");
                Assert.That(Gerbes.Points(gerbe, 10.3), Is.EqualTo(Gerbes.Points(gerbe, 10.3)), "déterministe");
            }
        }
    }
}
