/*
 * Tests de canon — les phrases qui, si elles sont violées, invalident le build.
 * Port de `tests/canon.test.ts`.
 *
 * Ils portent sur des registres aujourd'hui vides. C'est délibéré : le typage,
 * la visibilité et le registre figé « ne se rétrofitent pas », et la même chose
 * vaut pour les gardes.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;
using IdlePond.Presentation;
using IdlePond.Simulateur;
using NUnit.Framework;

namespace IdlePond.Tests
{
    [TestFixture]
    public sealed class CanonTests
    {
        /* ─── GDD §4.2 — la Foi n'achète que des miracles ───────────────────── */

        [Test]
        public void AucunNoeudDeTechniqueNeMonteUneProduction()
        {
            foreach (var noeud in NoeudsTechnique.Liste)
            {
                if (!(noeud.Effet is EffetChiffre chiffre)) continue;
                Assert.AreNotEqual(FamilleDeTerme.Production, Termes.Famille(chiffre.Terme), "le nœud " + noeud.Id + " cible " + chiffre.Terme);
            }
        }

        [Test]
        public void AucuneSourceDeBenedictionNeSubsisteDansLeCode()
        {
            // La migration de save est la seule exception : pour RETIRER une clef
            // morte d'une save v2, il faut la nommer.
            var migrations = Path.Combine("Adaptateurs", "Persistance.cs");
            var fautes = OutilsDeTest.FichiersCs(OutilsDeTest.DossierDuJeu())
                .Where(f => !f.EndsWith(migrations, StringComparison.Ordinal))
                .Where(f => Regex.IsMatch(OutilsDeTest.SansCommentaires(File.ReadAllText(f)), "b[ée]n[ée]diction", RegexOptions.IgnoreCase))
                .Select(OutilsDeTest.Relatif)
                .ToList();
            CollectionAssert.IsEmpty(fautes);
        }

        [Test]
        public void AucunSuccesNeMonteUneProduction()
        {
            // « Un succès ne peut porter qu'un effet qui existe déjà comme terme de
            // technique : réduction de coût, relèvement de plafond, ou verbe. »
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (succes.Effet == null || succes.Effet.Genre == GenreDEffet.Verbe) continue;
                Assert.AreNotEqual(FamilleDeTerme.Production, Termes.Famille(succes.Effet.Terme),
                    "le succès " + succes.Id + " cible le terme de production " + succes.Effet.Terme);
            }
        }

        [Test]
        public void UnPuitsUnLevierRienNeDoubleLaDensiteSurLaConviction()
        {
            foreach (var noeud in NoeudsTechnique.Liste)
            {
                if (noeud.Effet is EffetChiffre chiffre) Assert.AreNotEqual(TermeDeFormule.CoutReconviction, chiffre.Terme, "le nœud " + noeud.Id);
            }
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (succes.Effet == null || succes.Effet.Genre == GenreDEffet.Verbe) continue;
                Assert.AreNotEqual(TermeDeFormule.CoutReconviction, succes.Effet.Terme, "le succès " + succes.Id);
            }
        }

        /* ─── §7.5 — les trois règles dures de l'arbre ──────────────────────── */

        [Test]
        public void UneCapaciteAExactementUneSource()
        {
            var parLArbre = new HashSet<CapaciteId>(NoeudsTechnique.Liste.Select(n => n.Effet).OfType<EffetVerbe>().Select(v => v.Capacite));
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (succes.Effet == null || succes.Effet.Genre != GenreDEffet.Verbe) continue;
                Assert.IsFalse(parLArbre.Contains(succes.Effet.Capacite),
                    succes.Effet.Capacite + " est atteignable par l'arbre ET par le succès " + succes.Id);
            }
        }

        [Test]
        public void LeBudgetDeVerbesEstCommunEtTenu()
        {
            var verbesDeLArbre = NoeudsTechnique.Liste.Count(n => n.Effet is EffetVerbe);
            var verbesDesSucces = RegistreDesSucces.Liste.Count(s => s.Effet != null && s.Effet.Genre == GenreDEffet.Verbe);
            Assert.LessOrEqual(verbesDeLArbre, Constantes.BudgetDeVerbesArbre);
            Assert.LessOrEqual(verbesDeLArbre + verbesDesSucces, Constantes.BudgetDeVerbesTotal);
        }

        /* ─── §13 — les valeurs fixées et leurs dérivations ──────────────────── */

        [Test]
        public void GSurDSeDeriveDeLaCroissanceEtDesPaliersParCycle()
        {
            Assert.AreEqual(Math.Pow(Constantes.CroissanceParCycleVisee, 1 / Constantes.PaliersParCycleVise), Constantes.RapportGSurD, 5e-13);
            Assert.Less(Math.Abs(Constantes.RapportGSurD - 1.039), 1e-3);
        }

        [Test]
        public void DVautGSur1Virgule039Soit2Virgule31()
        {
            Assert.AreEqual(Constantes.GCoutPalier / Constantes.RapportGSurD, Constantes.DProductionParPalier, 5e-13);
            Assert.AreEqual(2.31, Constantes.DProductionParPalier, 0.005);
        }

        [Test]
        public void DDifferentDeGSinonLaDureeDUnCycleEstPlate()
        {
            Assert.Greater(Math.Abs(Constantes.DProductionParPalier - Constantes.GCoutPalier), 0.005);
        }

        [Test]
        public void FVautUnResetCompletAucuneFractionConservee()
        {
            Assert.AreEqual(1, Constantes.FTarifRedescente);
            Assert.AreEqual(0, Eclosion.FractionConservee);
        }

        [Test]
        public void LesSeuilsSontCumulesCentIndividusValentSeizePasMilleVingtQuatre()
        {
            CollectionAssert.AreEqual(new[] { 2.0, 4, 8, 16 }, Constantes.SeuilsDeJalon.Select(s => s.MultiplicateurCumule).ToArray());
            CollectionAssert.AreEqual(new[] { 10.0, 25, 50, 100 }, Constantes.SeuilsDeJalon.Select(s => s.Seuil).ToArray());
        }

        [Test]
        public void ThetaBorneEtLExposantDeDensiteDeriveDeThetaSurAlpha()
        {
            Assert.GreaterOrEqual(Constantes.ThetaPartCompensee, 0);
            Assert.LessOrEqual(Constantes.ThetaPartCompensee, 1);
            Assert.AreEqual(Constantes.ThetaPartCompensee / Constantes.AlphaGainDeDensite, Constantes.DensiteExposant(), 5e-13);
        }

        [Test]
        public void SoixanteDeuxPaliersSixAssisesVingtEtUneEspeces()
        {
            Assert.AreEqual(Constantes.NombreDePaliers, Paliers.Liste.Count);
            Assert.AreEqual(6, Assises.Liste.Count);
            Assert.AreEqual(Constantes.NombreDEspecesDeBase, Especes.Liste.Count);
        }

        [Test]
        public void ChaquePalierAppartientAUneAssiseEtPorteAuMoinsUnBanc()
        {
            foreach (var palier in Paliers.Liste)
            {
                Assert.IsTrue(Assises.Liste.Any(a => a.Id == palier.Assise));
                Assert.Greater(palier.Bancs.Count, 0);
            }
        }

        /* ─── §3 — le lexique s'applique au code, pas seulement à la prose ──── */

        private static readonly (Regex Motif, string Quoi)[] Perimes =
        {
            (new Regex(@"\bprestige\b", RegexOptions.IgnoreCase), "prestige"),
            (new Regex(@"\brebirth\b", RegexOptions.IgnoreCase), "rebirth"),
            (new Regex(@"\bgemmes?\b", RegexOptions.IgnoreCase), "gemme"),
            (new Regex(@"\bperles?\b", RegexOptions.IgnoreCase), "perle"),
            (new Regex(@"\bcorail\b", RegexOptions.IgnoreCase), "Corail"),
            (new Regex(@"\bbuyFish\b"), "buyFish (achat par exemplaire)"),
            (new Regex(@"\bpondDepth\b|\bzoneId?\b|\bbiomes?\b", RegexOptions.IgnoreCase), "ancien vocabulaire de zones/biomes"),
            (new Regex(@"\blayers?\b", RegexOptions.IgnoreCase), "layer (dire assise)"),
            (new Regex(@"\b[ée]tages?\b", RegexOptions.IgnoreCase), "étage (toléré à l’oral, jamais dans le code)"),
            (new Regex(@"\bstrates?\b", RegexOptions.IgnoreCase), "strate (réservé au plan des dieux)"),
        };

        [Test]
        public void AucunTermePerimeNeSubsisteDansLeCode()
        {
            var fautes = new List<string>();
            foreach (var fichier in OutilsDeTest.FichiersCs(OutilsDeTest.DossierDuJeu()))
            {
                var code = OutilsDeTest.SansCommentaires(File.ReadAllText(fichier));
                foreach (var (motif, quoi) in Perimes)
                {
                    if (motif.IsMatch(code)) fautes.Add(OutilsDeTest.Relatif(fichier) + " : " + quoi);
                }
            }
            CollectionAssert.IsEmpty(fautes);
        }

        /* ─── §3 — la règle d'UI absolue ────────────────────────────────────────
         * « L'interface n'affiche JAMAIS un nom générique de couche. » Le test porte
         * sur les chaînes réellement PRODUITES — les modèles de présentation que
         * l'écran Unity dessine tels quels.
         */

        private static readonly Regex InterditsALEcran =
            new Regex(@"\b(paliers?|assises?|[ée]tages?|strates?|couches?|zones?|biomes?)\b", RegexOptions.IgnoreCase);

        private static readonly SourceDeTerme[] SourcesAVerifier =
        {
            new SourceDeTerme { Quoi = QuoiSource.Population },
            new SourceDeTerme { Quoi = QuoiSource.Palier, Palier = 0 },
            new SourceDeTerme { Quoi = QuoiSource.Palier, Palier = 4 },
            new SourceDeTerme { Quoi = QuoiSource.Acclimatation, TypeMana = "type-mana-1" },
            new SourceDeTerme { Quoi = QuoiSource.Place, Place = 12 },
            new SourceDeTerme { Quoi = QuoiSource.DrapeauxPermanents, Especes = 0 },
            new SourceDeTerme { Quoi = QuoiSource.DrapeauxPermanents, Especes = 3 },
            new SourceDeTerme { Quoi = QuoiSource.EauMurie, Part = 1 },
            new SourceDeTerme { Quoi = QuoiSource.EauMurie, Part = 0.6 },
            new SourceDeTerme { Quoi = QuoiSource.EauMurie, Part = 0.2 },
            new SourceDeTerme { Quoi = QuoiSource.EauMurie, Part = 0.01 },
            new SourceDeTerme { Quoi = QuoiSource.CanalAcclimate },
        };

        /// <summary>Toutes les chaînes que les modèles produisent pour un état donné.</summary>
        private static IEnumerable<(string Ou, string Texte)> TextesAffiches(EtatJeu etat)
        {
            var enTete = Modeles.EnTete(etat);
            yield return ("en-tête", enTete.Titre);
            yield return ("en-tête", enTete.LibelleFoi);
            yield return ("en-tête", enTete.LibelleEclosions);

            var contenance = Modeles.Contenance(etat);
            yield return ("jauge", contenance.MessagePlein);
            yield return ("jauge", contenance.MessageBloque);

            var mare = Modeles.Mare(etat);
            yield return ("mare", mare.Titre);
            yield return ("mare", mare.Creusement.Libelle);
            foreach (var banc in mare.Bancs)
            {
                yield return ("banc", banc.Nom);
                yield return ("banc", banc.Profondeur);
                yield return ("banc", banc.Action);
                var captation = Modeles.Captation(etat, banc.BancId);
                yield return ("captation", captation.Nom);
                yield return ("captation", captation.Profondeur);
                yield return ("captation", captation.LibelleNatif);
                yield return ("captation", captation.LibelleEau);
                yield return ("captation", captation.LibelleAcclimate);
                foreach (var ligne in captation.Lignes.Concat(captation.LignesAcclimatees)) yield return ("captation", ligne.Source);
            }

            var eclosion = Modeles.Eclosion(etat);
            yield return ("éclosion", eclosion.Bouton);
            yield return ("éclosion", eclosion.Phrase);
            yield return ("éclosion", eclosion.LibelleFoi);
            yield return ("éclosion", eclosion.LibelleDensite);
            yield return ("éclosion", eclosion.Arbitrage);

            var succes = Modeles.Succes(etat);
            yield return ("succès", succes.Titre);
            yield return ("succès", succes.Vide);
            yield return ("succès", succes.TitreEnChemin);
            yield return ("succès", succes.TitrePlusLoin);

            yield return ("retour", Modeles.Retour(7200));
        }

        [Test]
        public void AucunTermeDeCoucheNeSortDansUnTexteAffiche()
        {
            var fautes = new List<string>();
            void Verifier(string ou, string texte)
            {
                if (texte != null && InterditsALEcran.IsMatch(texte)) fautes.Add(ou + " : « " + texte + " »");
            }

            foreach (var nom in TextesProvisoires.NomDesAssises.Values) Verifier("nom de lieu", nom);
            foreach (var nom in TextesProvisoires.NomDesEspeces.Values) Verifier("nom d’espèce", nom);
            foreach (var succes in RegistreDesSucces.Liste)
            {
                var texte = TextesProvisoires.TexteDuSucces(succes.Id);
                Verifier(succes.Id + ".nom", texte.Nom);
                Verifier(succes.Id + ".condition", texte.Condition);
                Verifier(succes.Id + ".rapport", texte.Rapport);
            }
            foreach (var source in SourcesAVerifier) Verifier("source de terme", Format.SourceDuTerme(source));
            for (var palier = 0; palier < 12; palier += 1) Verifier("profondeur", Format.Profondeur(palier));

            // Et l'écran entier, sur des états qui montrent tout ce qu'il peut montrer :
            // un départ, une mare peuplée, et une vie avancée de la partie simulée.
            var etats = new[]
            {
                Reducteur.EtatInitial(1, Assises.PaliersLivres),
                Joueur.Rejoue(1800),
                Simulation.Simuler(2, null, 1, null, Assises.PaliersLivres).Etat,
            };
            foreach (var etat in etats)
            {
                foreach (var (ou, texte) in TextesAffiches(etat)) Verifier(ou, texte);
            }

            CollectionAssert.IsEmpty(fautes);
        }

        [Test]
        public void TancheNEstAssigneeAAucunGenerateur()
        {
            CollectionAssert.DoesNotContain(Especes.Liste.Select(e => e.Id).ToArray(), Especes.EspeceReservee);
        }

        [Test]
        public void ChaqueSuccesLivrePorteUnTexteJamaisLeRepli()
        {
            var sansTexte = RegistreDesSucces.Liste
                .Where(s => ReferenceEquals(TextesProvisoires.TexteDuSucces(s.Id), TextesProvisoires.TexteDeSuccesInconnu))
                .Select(s => s.Id)
                .ToList();
            CollectionAssert.IsEmpty(sansTexte);
        }
    }
}
