using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    public class LexiqueTests
    {
        static readonly string[] Dossiers = { "Assets/IdlePond/Noyau", "Assets/IdlePond/Simulateur" };

        /// Codex §5, « Les mots morts » : ni en identifiant, ni à l'écran.
        static readonly string[] MotsMorts =
        {
            "foi", "fidele", "fideles", "benediction", "benedictions", "benir", "beni", "eclore", "eclosion", "eclosions",
            "ponte", "pondre", "population", "maturation", "acclimatation", "banc", "bancs", "place", "places",
            "prestige", "rebirth", "gemme", "gemmes", "perle", "perles", "corail", "layer", "layers",
            "zone", "zones", "biome", "biomes", "etage", "etages", "strate", "strates",
        };

        /// Codex §5, « Les mots interdits à l'écran seulement ».
        static readonly Regex InterditsALEcran = new Regex(@"\b(paliers?|assises?|couches?|zones?|[ée]tages?|strates?|biomes?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static readonly Regex MotsMortsALEcran = new Regex(
            @"\b(foi|fid[èe]les?|b[ée]n[ée]dictions?|b[ée]nir|b[ée]ni[est]?|[ée]clore|ponte|pondre|population|maturation|acclimatation|prestige|rebirth|gemmes?|perles?|corail)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// Codex §7 : « bénédiction » survit une fois, comme terme de fiction.
        const string ExceptionDeFiction = "la bénédiction de l’esprit";

        [Test, Description("aucun mot mort dans un identifiant du noyau ou du simulateur")]
        public void Aucun_mot_mort_dans_un_identifiant()
        {
            var fautes = (from dossier in Dossiers
                          from fichier in SourceCSharp.Fichiers(dossier)
                          let code = SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier))
                          from mot in SourceCSharp.Mots(code).Distinct()
                          where MotsMorts.Contains(mot)
                          select $"{Path.GetFileName(fichier)} : {mot}").Distinct().ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("aucun terme de couche ni mot mort ne sort dans un texte affiché")]
        public void Aucun_terme_de_couche_ni_mot_mort_dans_un_texte_affiche()
        {
            var fautes = Textes.TousLesTextesAffiches()
                .Where(t => t.Texte != null)
                .Where(t => InterditsALEcran.IsMatch(t.Texte)
                            || MotsMortsALEcran.IsMatch(t.Texte.Replace(ExceptionDeFiction, "")))
                .Select(t => $"{t.Ou} : « {t.Texte} »").ToList();
            Assert.That(fautes, Is.Empty);
        }

        [Test, Description("les identifiants de succès figés par le Codex §7 sont intacts")]
        public void Les_identifiants_figes_sont_intacts()
        {
            var ids = RegistreDesSucces.Tous.Select(s => s.Id).ToList();
            Assert.That(ids, Has.Member("franchissement-premiere-eclosion"));
            Assert.That(ids, Has.Member("franchissement-deuxieme-eclosion"));
            Assert.That(ids, Has.Member("franchissement-troisieme-eclosion"));
            Assert.That(ids, Has.Member("acte-premier-banc-de-cinq"));
        }

        /* ─── Portage des autres gardes §3 de canon.test.ts ───────────────────
         * Le TypeScript cherche des sous-chaînes camelCase (`partMure`) ; le C#
         * écrit les mêmes noms en PascalCase ou en MAJUSCULES_SOULIGNEES. On
         * compare donc des SUITES DE MOTS, découpées par `SourceCSharp.Mots` :
         * `partMure`, `PartMure` et `PART_MURE` donnent tous « part mure ». Un
         * motif terminé par `*` est un préfixe (`acclimat*` attrape
         * `acclimate`, `acclimatation`…), comme le `toContain` d'origine. */

        /// Le code des dossiers donnés, réduit à la suite de ses mots, entre espaces.
        static string SuiteDeMots(params string[] dossiers) =>
            " " + string.Join(" ", from dossier in dossiers
                                   from fichier in SourceCSharp.Fichiers(dossier)
                                   from mot in SourceCSharp.Mots(SourceCSharp.SansCommentairesNiChaines(File.ReadAllText(fichier)))
                                   select mot) + " ";

        static List<string> Subsistent(string suite, IEnumerable<string> motifs) =>
            motifs.Where(motif =>
            {
                var prefixe = motif.EndsWith("*");
                var mots = motif.TrimEnd('*');
                return Regex.IsMatch(suite, " " + Regex.Escape(mots) + (prefixe ? "" : " "));
            }).ToList();

        [Test, Description("la maturation ne survit nulle part dans le noyau")]
        public void La_part_mure_ne_survit_nulle_part_dans_le_noyau()
        {
            // Noyau v1.0 : ce mot gouverne ce qu'un lieu peut DEVENIR dans la fiction,
            // jamais ce que le héros GAGNE. Il n'a plus sa place dans le revenu.
            var subsistent = Subsistent(SuiteDeMots("Assets/IdlePond/Noyau"),
                new[] { "part mure", "parts mures", "maturation", "cible de maturation" });
            Assert.That(subsistent, Is.Empty, "subsiste dans le noyau");
        }

        [Test, Description("un seul canal de revenu : les espèces (noyau v1.0 §10)")]
        public void Un_seul_canal_de_revenu_les_especes_noyau_v1_0_10()
        {
            var subsistent = Subsistent(SuiteDeMots("Assets/IdlePond/Noyau"),
                new[] { "acclimat*", "debit acclimate", "canal acclimate" });
            Assert.That(subsistent, Is.Empty, "subsiste dans le noyau");
        }

        [Test, Description("le modèle à population ne subsiste pas dans le noyau (noyau v1.0 §1.3)")]
        public void Le_modele_a_effectif_ne_subsiste_pas_dans_le_noyau_noyau_v1_0_1_3()
        {
            // Le banc, la place et l'effectif sont morts ensemble le 2026-09-09 : une
            // espèce est un générateur avec un niveau. Le balayage porte sur le
            // dossier entier.
            var subsistent = Subsistent(SuiteDeMots("Assets/IdlePond/Noyau"), new[]
            {
                "effectif", "banc id", "etat banc", "convaincre", "acheter place", "cout de place",
                "cout place", "cout reconviction", "vitesse de repeuplement",
            });
            Assert.That(subsistent, Is.Empty, "subsiste dans le noyau");
        }

        [Test, Description("le modèle mort ne subsiste nulle part dans src/ (spec §4)")]
        public void Le_modele_retire_ne_subsiste_nulle_part_spec_4()
        {
            // Le C# n'a ni migration de vieilles sauvegardes (il repart en version 1)
            // ni scène : le balayage porte sur le noyau ET le simulateur, sans
            // exclusion. Les identifiants de succès figés sont des chaînes, blanchies.
            var subsistent = Subsistent(SuiteDeMots(Dossiers), new[]
            {
                "population", "maturation", "acclimat*", "part mure",
                "cout place", "convaincre", "acheter place", "bancs", "banc par id",
                "acclimatations", "secondes en saturation",
                "secondes en redescente", "fraction en redescente",
            });
            Assert.That(subsistent, Is.Empty, "subsiste dans le noyau ou le simulateur");
        }
    }
}
