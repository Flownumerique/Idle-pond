using System;
using System.Globalization;
using System.Text;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// IdlePond — mise en forme pour l'écran. Portage de `format.ts`, et PUR : pas de
    /// UnityEngine ici, pour que le balayage de la mise en forme se teste sans moteur.
    ///
    /// Règle d'UI absolue (§3) : l'interface n'affiche JAMAIS un nom générique de couche.
    /// Pas de « Zone 3 », pas de « Assise II » — et pas davantage « palier 4 », puisque
    /// `assise` et `palier` sont des termes de code et de GDD, pas d'écran.
    ///
    /// Ce qui s'affiche à la place : le nom propre du lieu, et une PROFONDEUR. Une mesure
    /// n'est pas un nom de couche.
    ///
    /// Toute mise en forme passe par la culture invariante : un joueur dont le téléphone
    /// est réglé en anglais ne doit pas lire « 1.5 » là où l'on écrit « 1,5 » — et
    /// surtout, jamais l'inverse selon la machine. Le séparateur décimal est celui du
    /// jeu, pas celui de la machine.
    /// </summary>
    public static class Format
    {
        static readonly string[] SUFFIXES = { "", " k", " M", " G", " T", " P", " E" };

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>
        /// L'écriture des grands nombres, choisie dans les réglages. Posée par
        /// `RacineDeLInterface` quand le réglage change, puis tout l'écran est réécrit. Sous
        /// 1000, les trois notations écrivent le nombre tel quel.
        /// </summary>
        public static Notation Notation { get; set; } = Notation.Suffixes;

        /// Un montant de mana ou de Souffle, lisible d'un coup d'œil.
        public static string Montant(Decimal valeur)
        {
            var nombre = valeur.ToNumber();
            // Au-delà du double (10⁴⁰⁰), le nombre ne se lit plus que par sa mantisse et son exposant.
            if (double.IsNaN(nombre) || double.IsInfinity(nombre))
                return Notation == Notation.Ingenieur ? Ingenieur(valeur) : Exponentielle(valeur, 2);
            if (nombre < 1000)
            {
                if (EstEntier(nombre)) return nombre.ToString("0", Inv);
                return nombre < 10 ? nombre.ToString("F1", Inv) : Math.Floor(nombre).ToString("0", Inv);
            }
            if (Notation == Notation.Scientifique) return Exponentielle(valeur, 2);
            if (Notation == Notation.Ingenieur) return Ingenieur(valeur);

            var reste = nombre;
            var rang = 0;
            while (reste >= 1000 && rang < SUFFIXES.Length - 1)
            {
                reste /= 1000;
                rang += 1;
            }
            if (rang == SUFFIXES.Length - 1 && reste >= 1000) return Exponentielle(valeur, 2);
            return reste.ToString(reste < 10 ? "F2" : "F1", Inv) + SUFFIXES[rang];
        }

        /// <summary>
        /// Un coût, arrondi VERS LE HAUT.
        ///
        /// Un coût de 58,8 affiché « 58 » à côté d'une bourse de 58 donne un bouton grisé
        /// sans raison visible. Sur un montant produit l'arrondi par défaut est honnête ;
        /// sur un montant à payer, il ment.
        /// </summary>
        public static string Cout(Decimal valeur)
        {
            var nombre = valeur.ToNumber();
            if (!double.IsNaN(nombre) && !double.IsInfinity(nombre) && nombre < 1000)
                return nombre < 10 ? nombre.ToString("F1", Inv) : Math.Ceiling(nombre).ToString("0", Inv);
            return Montant(valeur);
        }

        /// Un entier, par tranches de trois chiffres séparées d'une espace fine insécable.
        public static string Entier(double valeur)
        {
            var chiffres = Math.Floor(Math.Abs(valeur)).ToString("0", Inv);
            var sortie = new StringBuilder();
            for (var i = 0; i < chiffres.Length; i++)
            {
                if (i > 0 && (chiffres.Length - i) % 3 == 0) sortie.Append(' ');
                sortie.Append(chiffres[i]);
            }
            return (valeur < 0 ? "−" : "") + sortie;
        }

        public static string Duree(double secondes)
        {
            if (double.IsNaN(secondes) || double.IsInfinity(secondes) || secondes < 0) return E.DUREE_INCONNUE;
            if (secondes < 90) return Decimal.JsRound(secondes).ToString("0", Inv) + " s";
            if (secondes < 5400) return Decimal.JsRound(secondes / 60).ToString("0", Inv) + " min";
            var heures = secondes / 3600;
            return heures < 24
                ? heures.ToString("F1", Inv) + " h"
                : Decimal.JsRound(heures / 24).ToString("0", Inv) + " j";
        }

        /// <summary>
        /// La profondeur d'un creux, en brasses. Une mesure, pas un nom de couche : le
        /// premier creux est à zéro brasse, on descend d'une brasse par creusement.
        /// </summary>
        public static string Profondeur(int palier) =>
            palier == 0 ? E.A_FLEUR_D_EAU : Remplir(palier > 1 ? E.BRASSES : E.BRASSE, palier);

        public static string NomDeLAssise(string assise) =>
            assise != null && Textes.NOM_DES_ASSISES.TryGetValue(assise, out var nom) ? nom : E.PLUS_BAS;

        /// Le même nom, en tête de phrase.
        public static string NomDeLAssiseCapitale(string assise)
        {
            var nom = NomDeLAssise(assise);
            return char.ToUpperInvariant(nom[0]) + nom.Substring(1);
        }

        public static string NomDeLEspece(string espece) =>
            espece != null && Textes.NOM_DES_ESPECES.TryGetValue(espece, out var nom) ? nom : E.ESPECE_SANS_NOM;

        /// « grandi n fois », ou « alevin » tant que le héros n'a jamais grandi.
        public static string LibelleDuHeros(int niveau) =>
            niveau <= 1 ? E.ALEVIN : Remplir(E.GRANDI, niveau - 1);

        /// La source d'un terme, mise en mots ici et pas dans le noyau.
        public static string SourceDuTerme(SourceDeTerme source)
        {
            var valeur = (int)Math.Round(source.Valeur, MidpointRounding.AwayFromZero);
            switch (source.Quoi)
            {
                case QuoiSource.Niveau:
                    return valeur == 0 ? E.PERSONNE_ENCORE : Remplir(E.CRANS_TENUS, valeur);
                case QuoiSource.Palier:
                    return Profondeur(valeur);
                case QuoiSource.DrapeauxPermanents:
                    return valeur == 0
                        ? E.AUCUNE_ESPECE_AU_COMPLET
                        : Remplir(valeur > 1 ? E.ESPECES_AU_COMPLET : E.ESPECE_AU_COMPLET, valeur);
                case QuoiSource.Profondeur:
                    return Profondeur(Math.Max(0, valeur - 1));
                case QuoiSource.Densite:
                    return source.Valeur <= 0 ? E.EAU_NEUTRE : Remplir(E.EAU_A_DENSITE, source.Valeur.ToString("F1", Inv));
                case QuoiSource.Heros:
                    return valeur <= 1 ? E.TOI_QUI_CAPTES_SEUL : Remplir(E.TOI_GRANDI, valeur - 1);
                case QuoiSource.AmeliorationDeRenaissance:
                    return valeur == 0 ? E.AUCUNE_AMELIORATION : Remplir(E.AMELIORE_FOIS, valeur);
                case QuoiSource.BonusDeLieu:
                    return valeur == 0 ? E.AUCUN_BONUS_DE_LIEU : Remplir(E.BONUS_DE_LIEU_RANGS, valeur);
                default:
                    return E.PLUS_BAS;
            }
        }

        /// La part d'un rang de bonus, en pour cent, sans décimale inutile : 0,08 → « 8 », 0,5 → « 50 ».
        public static string Pourcent(double part) => (part * 100).ToString("0.#", Inv);

        /// <summary>
        /// Le nombre d'une ligne de captation : trois décimales sous dix, une au-dessus —
        /// les termes de la table sont des facteurs proches de 1, que « 1,0 » écraserait.
        /// </summary>
        public static string Terme(double valeur) => valeur < 10 ? valeur.ToString("F3", Inv) : valeur.ToString("F1", Inv);

        /// Remplit les trous `{0}`, `{1}` d'une phrase de `Textes.Ecran`.
        public static string Remplir(string phrase, params object[] valeurs) => string.Format(Inv, phrase, valeurs);

        static bool EstEntier(double x) => Math.Floor(x) == x;

        /// <summary>
        /// L'écriture ingénieur : l'exposant ramené au multiple de 3 inférieur, trois
        /// chiffres significatifs — `1.25e+6`, `12.5e+6`, `125e+6`. L'arrondi se fait sur la
        /// mantisse d'abord, pour que la retenue (9,996 → 10,0) change l'exposant avant le
        /// choix du multiple de 3.
        /// </summary>
        static string Ingenieur(Decimal valeur)
        {
            if (double.IsNaN(valeur.Mantisse) || double.IsNaN(valeur.Exposant)) return "NaN";
            var exposant = valeur.Exposant;
            var arrondie = Math.Round(Math.Abs(valeur.Mantisse), 2, MidpointRounding.AwayFromZero);
            if (arrondie >= 10) { arrondie /= 10; exposant += 1; }
            var decalage = (int)(((exposant % 3) + 3) % 3);
            var mantisse = arrondie * Math.Pow(10, decalage);
            exposant -= decalage;
            return (valeur.Mantisse < 0 ? "-" : "") + mantisse.ToString("F" + (2 - decalage), Inv)
                + "e" + (exposant >= 0 ? "+" : "-") + Math.Abs(exposant).ToString("0", Inv);
        }

        /// <summary>
        /// L'écriture exponentielle à `decimales` chiffres, `1.23e+400`, que `double` ne sait
        /// plus porter : la mantisse et l'exposant du `Decimal` sont lus tels quels.
        /// </summary>
        static string Exponentielle(Decimal valeur, int decimales)
        {
            if (double.IsNaN(valeur.Mantisse) || double.IsNaN(valeur.Exposant)) return "NaN";
            if (valeur.Mantisse == 0) return (0.0).ToString("F" + decimales, Inv) + "e+0";
            var mantisse = valeur.Mantisse;
            var exposant = valeur.Exposant;
            var arrondie = Math.Round(Math.Abs(mantisse), decimales, MidpointRounding.AwayFromZero);
            // 9,999 s'arrondit à 10,00 : la retenue passe dans l'exposant.
            if (arrondie >= 10) { arrondie /= 10; exposant += 1; }
            return (mantisse < 0 ? "-" : "") + arrondie.ToString("F" + decimales, Inv)
                + "e" + (exposant >= 0 ? "+" : "-") + Math.Abs(exposant).ToString("0", Inv);
        }
    }
}
