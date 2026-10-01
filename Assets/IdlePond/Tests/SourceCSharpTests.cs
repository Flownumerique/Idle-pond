using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// L'outil qui blanchit commentaires et chaînes avant les tests d'architecture et
    /// de lexique. S'il se trompe, il efface du vrai code et ces tests deviennent
    /// aveugles sans rougir : on le vérifie donc directement.
    /// </summary>
    public class SourceCSharpTests
    {
        static string Blanchir(string source) => SourceCSharp.SansCommentairesNiChaines(source);

        [Test, Description("une chaîne simple est blanchie, échappements compris")]
        public void Une_chaine_simple_est_blanchie_echappements_compris()
        {
            Assert.That(Blanchir("var a = \"bonjour \\\"place\\\" \\\\\"; Suite();"), Is.EqualTo("var a = \"\"; Suite();"));
        }

        [Test, Description("une chaîne verbatim est blanchie, guillemets doublés compris")]
        public void Une_chaine_verbatim_est_blanchie_guillemets_doubles_compris()
        {
            Assert.That(Blanchir("var a = @\"c:\\dossier \"\"place\"\" fin\\\" + Suite;"), Is.EqualTo("var a = \"\" + Suite;"));
        }

        [Test, Description("le code d'un trou d'interpolation survit, même avec une chaîne imbriquée")]
        public void Le_code_d_un_trou_d_interpolation_survit_meme_avec_une_chaine_imbriquee()
        {
            var obtenu = Blanchir("throw new X($\"Pas positif (reçu {p.ToString(\"R\", Culture.Invariante)}) place\"); Apres();");
            Assert.That(obtenu, Is.EqualTo("throw new X(\"{p.ToString(\"\", Culture.Invariante)}\"); Apres();"));
            Assert.That(obtenu, Does.Not.Contain("place"));
            Assert.That(obtenu, Does.Not.Contain("reçu"));
        }

        [Test, Description("une interpolation imbriquée garde le code de tous ses trous")]
        public void Une_interpolation_imbriquee_garde_le_code_de_tous_ses_trous()
        {
            Assert.That(Blanchir("$\"a{F($\"b{X}c\")}d\" + Y"), Is.EqualTo("\"{F(\"{X}\")}\" + Y"));
        }

        [Test, Description("les accolades doublées sont du texte, le format d'un trou aussi")]
        public void Les_accolades_doublees_sont_du_texte_le_format_d_un_trou_aussi()
        {
            Assert.That(Blanchir("$\"{{place}} {Valeur} }}fin{{\" + $\"{X:F2} place\" + $\"{(A ? B : C),5}\""),
                Is.EqualTo("\"{Valeur}\" + \"{X}\" + \"{(A ? B : C),5}\""));
        }

        [Test, Description("une chaîne interpolée verbatim garde ses trous et blanchit le reste")]
        public void Une_chaine_interpolee_verbatim_garde_ses_trous_et_blanchit_le_reste()
        {
            Assert.That(Blanchir("$@\"x \"\"{Y}\"\" \\ z\" + @$\"{W}place\\\" + V"), Is.EqualTo("\"{Y}\" + \"{W}\" + V"));
        }

        [Test, Description("les littéraux de caractère sont blanchis, apostrophe et guillemet compris")]
        public void Les_litteraux_de_caractere_sont_blanchis_apostrophe_et_guillemet_compris()
        {
            Assert.That(Blanchir("var c = '\\''; var d = '\"'; var e = 'x'; var f = '\\u00e9'; Suite();"),
                Is.EqualTo("var c = ' '; var d = ' '; var e = ' '; var f = ' '; Suite();"));
        }

        [Test, Description("les commentaires de ligne et de bloc sont effacés")]
        public void Les_commentaires_de_ligne_et_de_bloc_sont_effaces()
        {
            Assert.That(Blanchir("A // place \"x\nB /* bloc \"place\"\n */ C"), Is.EqualTo("A \nB  C"));
        }
    }
}
