using System.Collections.Generic;
using System.Linq;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// Le passage d'un lieu à l'autre (décidé par l'auteur le 2026-11-25) : on ne perce vers le
    /// lieu suivant qu'avec tout le peuple du lieu actuel ; et ce qui s'en voit — le seuil, la
    /// cascade, les lueurs du lieu qui vient, l'annonce de l'arrivée.
    /// </summary>
    public class PerceeTests
    {
        const long DEPART = 1_790_000_000_000L;

        /// Au bas de la Noue, riche, avec les tribus de la Noue ralliées ou non.
        static EtatJeu AuBasDeLaNoue(bool vairon, bool loche)
        {
            var etat = Reducteur.EtatInitial(1, Assises.PALIERS_LIVRES);
            etat = etat with
            {
                Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e30") },
                Permanent = etat.Permanent with { ContenanceMana = Decimal.Parse("1e40") },
            };
            if (vairon) etat = Reducteur.Debloquer(etat, "vairon");
            while (etat.Cycle.PaliersOuverts < 6)
            {
                if (loche && etat.Cycle.PaliersOuverts > 3) etat = Reducteur.Debloquer(etat, "loche");
                var suivant = Reducteur.Creuser(etat);
                Assume.That(suivant, Is.Not.SameAs(etat), "la Noue se creuse sans condition");
                etat = suivant;
            }
            return etat;
        }

        [Test, Description("le premier creux de chaque lieu, et lui seul, est une percée")]
        public void Le_premier_creux_d_un_lieu_est_une_percee()
        {
            var percees = Enumerable.Range(0, Constantes.NOMBRE_DE_PALIERS).Where(Economie.EstUnePercee).ToList();
            Assert.That(percees, Is.EqualTo(Assises.Toutes.Skip(1).Select(a => a.IndexPremierPalier).ToList()));
            Assert.That(Economie.EstUnePercee(0), Is.False, "la surface n'est pas une percée");
        }

        [Test, Description("sans tout le peuple de la Noue, on ne perce pas vers le Gour")]
        public void On_ne_perce_pas_sans_tout_le_peuple()
        {
            var sansLoche = AuBasDeLaNoue(vairon: true, loche: false);
            Assert.That(Economie.TribusARallierPourPercer(sansLoche).Select(e => e.Id), Is.EqualTo(new[] { "loche" }));
            Assert.That(Reducteur.Creuser(sansLoche), Is.SameAs(sansLoche), "la percée est fermée");
            Assert.That(Economie.EstBloque(sansLoche), Is.False, "fermée n'est pas bloquée : il faut rallier, pas attendre");

            var personne = AuBasDeLaNoue(vairon: false, loche: false);
            Assert.That(Economie.TribusARallierPourPercer(personne).Select(e => e.Id), Is.EqualTo(new[] { "vairon", "loche" }));

            var rallies = Reducteur.Debloquer(sansLoche, "loche");
            Assert.That(Economie.PerceeOuverte(rallies), Is.True);
            var perce = Reducteur.Creuser(rallies);
            Assert.That(perce.Cycle.PaliersOuverts, Is.EqualTo(7), "tout le peuple suit : on perce");
        }

        [Test, Description("dans un lieu, creuser reste libre : seule la percée demande le peuple")]
        public void Dans_un_lieu_creuser_reste_libre()
        {
            var etat = Reducteur.EtatInitial(1, Assises.PALIERS_LIVRES) with { };
            etat = etat with { Cycle = etat.Cycle with { ManaCourant = Decimal.Parse("1e30") }, Permanent = etat.Permanent with { ContenanceMana = Decimal.Parse("1e40") } };
            Assert.That(Economie.TribusARallierPourPercer(etat), Is.Empty);
            Assert.That(Reducteur.Creuser(etat).Cycle.PaliersOuverts, Is.EqualTo(etat.Cycle.PaliersOuverts + 1), "sans aucune tribu");
        }

        [Test, Description("une percée fermée ne se demande pas d'avance avec la file")]
        public void Une_percee_fermee_ne_se_met_pas_en_file()
        {
            var sansLoche = AuBasDeLaNoue(vairon: true, loche: false);
            var avecLaFile = sansLoche with { Permanent = sansLoche.Permanent with { NoeudsTechnique = new[] { "creusement-file" } } };
            Assert.That(Reducteur.DemanderUnCreux(avecLaFile).Cycle.CreuxDemandes, Is.EqualTo(0));
        }

        [Test, Description("la percée s'annonce : une ligne d'arrivée dans le lieu, comme un succès")]
        public void La_percee_s_annonce()
        {
            var partie = new Partie(new HorlogeFigee(DEPART), Reducteur.Debloquer(AuBasDeLaNoue(vairon: true, loche: false), "loche"));
            partie.Creuser();
            Assert.That(partie.AAnnoncer, Has.Member(Partie.ANNONCE_DE_LIEU + "gour"));
            Assert.That(Textes.ArriveeDans("gour"), Does.StartWith("Le Gour"));
            Assert.That(Textes.ArriveeDans("lieu-inconnu"), Is.EqualTo(Textes.ARRIVEE_AILLEURS));
        }

        /* ─── Ce qui s'en voit ─────────────────────────────────────────────────────── */

        [Test, Description("le seuil affleure la jonction ; la brèche est dans le champ")]
        public void Le_seuil_affleure_la_jonction()
        {
            Assert.That(Lieux.YDuSeuil(6) + Lieux.HAUTEUR_DU_SEUIL, Is.EqualTo(-6 * Gabarits.HAUTEUR_DE_BANDE));
            Assert.That(Lieux.XDeLaBreche, Is.InRange(20, Gabarits.LARGEUR_VISEE - 20));
            Assert.That(Lieux.OuvreUnLieu(6), Is.True);
            Assert.That(Lieux.OuvreUnLieu(7), Is.False);
        }

        [Test, Description("la cascade tombe par la brèche, dans le haut de la vasque")]
        public void La_cascade_tombe_par_la_breche()
        {
            for (var t = 0.0; t < 5; t += 0.13)
            {
                var gouttes = Lieux.Cascade(6, t);
                Assert.That(gouttes, Has.Count.EqualTo(Lieux.GOUTTES));
                foreach (var g in gouttes)
                {
                    Assert.That(System.Math.Abs(g.X - Lieux.XDeLaBreche), Is.LessThanOrEqualTo(8));
                    Assert.That(g.Y, Is.LessThan(-6 * Gabarits.HAUTEUR_DE_BANDE));
                    Assert.That(g.Y, Is.GreaterThan(-7 * Gabarits.HAUTEUR_DE_BANDE + Gabarits.HAUTEUR_DE_BANDE / 2));
                }
            }
        }

        [Test, Description("les lueurs du lieu qui vient s'allument dans sa roche, à la couleur de sa magie")]
        public void Les_lueurs_du_lieu_qui_vient()
        {
            var vues = 0;
            for (var t = 0.0; t < 6; t += 0.1)
                foreach (var l in Lieux.Lueurs(6, t))
                {
                    vues++;
                    Assert.That(l.X >= 0 && l.X < Gabarits.LARGEUR_VISEE, Is.True);
                    Assert.That(l.Y >= -7 * Gabarits.HAUTEUR_DE_BANDE && l.Y < -6 * Gabarits.HAUTEUR_DE_BANDE, Is.True);
                    // Dans le haut de la roche, là où la rive la montre quand l'eau dépasse le cadre ;
                    // halo compris (deux pixels de part et d'autre).
                    Assert.That(l.Y - 2 >= -6 * Gabarits.HAUTEUR_DE_BANDE - Cadrage.RIVE_DE_ROCHE, Is.True, $"lueur à {l.Y}");
                }
            Assert.That(vues, Is.GreaterThan(0));
            Assert.That(Lieux.CouleurDeLaMagie("gour"), Is.EqualTo(0x8FD8FF), "le bleu de Courant");
            Assert.That(Lieux.CouleurDeLaMagie("lieu-inconnu"), Is.EqualTo(0xFFE070));
        }

        [Test, Description("la roche qui vient se voit dès qu'elle touche le champ — elle est SOUS l'eau, jamais parmi ses bandes")]
        public void La_roche_qui_vient_se_voit_dans_le_champ()
        {
            var portrait = new DimensionsDuRendu(3, 360, 256);
            // La Noue au complet, la roche du Gour dessous : la rive la montre.
            Assert.That(Lieux.BandeDansLeChamp(6, Cadrage.Cadrer(portrait, 6, 7)), Is.True);
            // Sans roche dessinée, le champ s'arrête au fond de l'eau.
            Assert.That(Lieux.BandeDansLeChamp(6, Cadrage.Cadrer(portrait, 6, 6)), Is.False);
            // Une bande au-dessus du champ ne se voit pas ; celle du héros, si.
            Assert.That(Lieux.BandeDansLeChamp(0, Cadrage.Cadrer(portrait, 6, 7)), Is.False);
            Assert.That(Lieux.BandeDansLeChamp(5, Cadrage.Cadrer(portrait, 6, 7)), Is.True);
            Assert.That(Lieux.BandeDansLeChamp(0, null), Is.False);
        }

        [Test, Description("ce qui est lumière brille sans éclairage ; la roche et la vase restent dans la lumière de la scène")]
        public void Seules_les_gerbes_de_lumiere_brillent()
        {
            // La percée : l'écume et l'eau claire brillent, l'éclat de roche (sa 4ᵉ couleur) non.
            Assert.That(Enumerable.Range(0, 3).All(c => Gerbes.Brille(GenreDeGerbe.Percee, c)), Is.True);
            Assert.That(Gerbes.Brille(GenreDeGerbe.Percee, 3), Is.False);
            Assert.That(Gerbes.Brille(GenreDeGerbe.Seuil, 0), Is.True);
            Assert.That(Gerbes.Brille(GenreDeGerbe.Ralliement, 2), Is.True);
            Assert.That(Gerbes.Brille(GenreDeGerbe.RocheQuiCede, 0), Is.False);
            Assert.That(Gerbes.Brille(GenreDeGerbe.Trouble, 0), Is.False);
        }

        [Test, Description("la percée jaillit par la brèche ; un creux ordinaire fait céder la roche")]
        public void La_percee_jaillit_par_la_breche()
        {
            var percee = Gerbes.De(new EvenementDeJeu(GenreDEvenement.CreuxOuvert, null, 6), 0, 7, null);
            Assert.That(percee.Genre, Is.EqualTo(GenreDeGerbe.Percee));
            Assert.That(percee.Origine.X, Is.EqualTo(Lieux.XDeLaBreche));
            Assert.That(Gerbes.De(new EvenementDeJeu(GenreDEvenement.CreuxOuvert, null, 7), 0, 8, null).Genre, Is.EqualTo(GenreDeGerbe.RocheQuiCede));
        }
    }
}
