using IdlePond.Jeu.UI;
using IdlePond.Noyau;
using NUnit.Framework;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Tests
{
    /// <summary>
    /// `format.ts` n'avait pas de test dédié : ses cas clés sont fixés ici, avec les
    /// valeurs que le TypeScript rendait. Ce sont les chiffres que le joueur lit — un
    /// arrondi qui change se voit à l'écran avant de se voir dans une assertion.
    /// </summary>
    public class FormatTests
    {
        [Test, Description("un montant tient en peu de caractères, avec son suffixe")]
        public void Un_montant_tient_en_peu_de_caracteres()
        {
            foreach (var (v, attendu) in new[]
            {
                (0.0, "0"),
                (5.0, "5"),
                (5.43, "5.4"),
                (12.7, "12"),
                (999.0, "999"),
                (1000.0, "1.00 k"),
                (12345.0, "12.3 k"),
                (1.5e6, "1.50 M"),
                (2.5e9, "2.50 G"),
            })
                Assert.That(Format.Montant(new Decimal(v)), Is.EqualTo(attendu), $"{v}");
        }

        [Test, Description("au-delà du dernier suffixe, un montant passe en écriture exponentielle")]
        public void Au_dela_du_dernier_suffixe_un_montant_passe_en_exponentielle()
        {
            Assert.That(Format.Montant(Decimal.Pow10(21)), Is.EqualTo("1.00e+21"));
            Assert.That(Format.Montant(Decimal.Pow10(400)), Is.EqualTo("1.00e+400"));
            // 9,996e400 s'arrondit à 10,00 : la retenue passe dans l'exposant.
            Assert.That(Format.Montant(new Decimal(9.996).Mul(Decimal.Pow10(400))), Is.EqualTo("1.00e+401"));
        }

        [Test, Description("un coût s'arrondit vers le haut : jamais un bouton grisé sans raison visible")]
        public void Un_cout_s_arrondit_vers_le_haut()
        {
            foreach (var (v, attendu) in new[]
            {
                (58.8, "59"),
                (58.0, "58"),
                (5.04, "5.0"),
                (999.1, "1000"),
                (1500.0, "1.50 k"),
            })
                Assert.That(Format.Cout(new Decimal(v)), Is.EqualTo(attendu), $"{v}");
        }

        [Test, Description("une durée choisit son unité : secondes, minutes, heures, jours")]
        public void Une_duree_choisit_son_unite()
        {
            foreach (var (v, attendu) in new[]
            {
                (0.0, "0 s"),
                (45.0, "45 s"),
                (89.4, "89 s"),
                (90.0, "2 min"),
                (3600.0, "60 min"),
                (5400.0, "1.5 h"),
                (43200.0, "12.0 h"),
                (86400.0, "1 j"),
                (-1.0, "—"),
                (double.NaN, "—"),
            })
                Assert.That(Format.Duree(v), Is.EqualTo(attendu), $"{v}");
        }

        [Test, Description("le premier creux est à fleur d'eau, puis une brasse par creusement")]
        public void La_profondeur_se_mesure_en_brasses()
        {
            Assert.That(Format.Profondeur(0), Is.EqualTo("à fleur d’eau"));
            Assert.That(Format.Profondeur(1), Is.EqualTo("1 brasse"));
            Assert.That(Format.Profondeur(3), Is.EqualTo("3 brasses"));
        }

        [Test, Description("l'écran nomme le lieu et les espèces, jamais la couche")]
        public void L_ecran_nomme_le_lieu_et_les_especes()
        {
            Assert.That(Format.NomDeLAssise("noue"), Is.EqualTo("la Noue"));
            Assert.That(Format.NomDeLAssiseCapitale("noue"), Is.EqualTo("La Noue"));
            Assert.That(Format.NomDeLAssise("inconnue"), Is.EqualTo("plus bas"));
            Assert.That(Format.NomDeLEspece("vairon"), Is.EqualTo("le vairon"));
            Assert.That(Format.NomDeLEspece("inconnue"), Is.EqualTo("un banc sans nom"));
        }

        [Test, Description("un entier se lit par tranches de trois chiffres")]
        public void Un_entier_se_lit_par_tranches()
        {
            Assert.That(Format.Entier(999), Is.EqualTo("999"));
            Assert.That(Format.Entier(1234567.9), Is.EqualTo("1 234 567"));
        }

        [Test, Description("la source d'un terme se met en mots ici, pas dans le noyau")]
        public void La_source_d_un_terme_se_met_en_mots()
        {
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Niveau, 0)), Is.EqualTo("personne encore"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Niveau, 7)), Is.EqualTo("7 crans tenus"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Palier, 2)), Is.EqualTo("2 brasses"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.DrapeauxPermanents, 0)), Is.EqualTo("aucune espèce au complet"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.DrapeauxPermanents, 1)), Is.EqualTo("1 espèce déjà au complet"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.DrapeauxPermanents, 2)), Is.EqualTo("2 espèces déjà au complet"));
            // `paliersOuverts` compte les creux ; la profondeur est celle du dernier.
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Profondeur, 1)), Is.EqualTo("à fleur d’eau"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Densite, 0)), Is.EqualTo("eau neutre"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Densite, 1.26)), Is.EqualTo("eau à 1.3 de densité"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Heros, 1)), Is.EqualTo("toi, qui captes seul"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Heros, 4)), Is.EqualTo("toi, grandi 3 fois"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Insufflation, 0)), Is.EqualTo("rien d’insufflé"));
            Assert.That(Format.SourceDuTerme(new SourceDeTerme(QuoiSource.Insufflation, 2)), Is.EqualTo("insufflé 2 fois"));
        }

        [Test, Description("le héros est un alevin tant qu'il n'a pas grandi")]
        public void Le_heros_est_un_alevin_tant_qu_il_n_a_pas_grandi()
        {
            Assert.That(Format.LibelleDuHeros(1), Is.EqualTo("alevin"));
            Assert.That(Format.LibelleDuHeros(5), Is.EqualTo("grandi 4 fois"));
        }

        [Test, Description("le séparateur décimal ne dépend pas de la culture de la machine")]
        public void Le_separateur_decimal_ne_depend_pas_de_la_culture()
        {
            var avant = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                Assert.That(Format.Montant(new Decimal(5.43)), Is.EqualTo("5.4"));
                Assert.That(Format.Terme(1.5), Is.EqualTo("1.500"));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = avant; }
        }
    }
}
