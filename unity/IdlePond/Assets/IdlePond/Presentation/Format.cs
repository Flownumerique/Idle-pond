/*
 * IdlePond — mise en forme pour l'écran. Port de `src/ui/format.ts`.
 *
 * Règle d'UI absolue (§3) : l'interface n'affiche JAMAIS un nom générique de
 * couche. Pas de « Zone 3 », pas de « Assise II » — et pas davantage « palier 4 ».
 * Ce qui s'affiche à la place : le nom propre du lieu, et une PROFONDEUR.
 *
 * Chaque fonction rend la même chaîne que son homologue TypeScript, à l'octet
 * près — le test de parité le vérifie sur un échantillon de valeurs. C'est pour
 * ça que les nombres passent par `NombreJs` et jamais par les formats « F » ou
 * « N » de .NET, qui dépendent du runtime et de la culture.
 */
using System;
using System.Text;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Presentation
{
    public static class Format
    {
        private static readonly string[] Suffixes = { "", " k", " M", " G", " T", " P", " E" };

        /// <summary>`Math.round` de JavaScript : au plus proche, égalité vers +∞.</summary>
        private static double ArrondiJs(double x)
        {
            var bas = Math.Floor(x);
            return x - bas >= 0.5 ? bas + 1 : bas;
        }

        private static bool EstFini(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        /// <summary>Un montant de mana ou de Foi, lisible d'un coup d'œil.</summary>
        public static string Montant(GrandNombre valeur)
        {
            var nombre = valeur.ToNumber();
            if (!EstFini(nombre)) return valeur.ToExponential(2);
            if (nombre < 1000)
            {
                if (nombre == Math.Floor(nombre)) return NombreJs.VersTexte(nombre);
                return nombre < 10 ? NombreJs.ToFixed(nombre, 1) : NombreJs.VersTexte(Math.Floor(nombre));
            }

            var reste = nombre;
            var rang = 0;
            while (reste >= 1000 && rang < Suffixes.Length - 1)
            {
                reste /= 1000;
                rang += 1;
            }
            if (rang == Suffixes.Length - 1 && reste >= 1000) return valeur.ToExponential(2);
            return NombreJs.ToFixed(reste, reste < 10 ? 2 : 1) + Suffixes[rang];
        }

        /// <summary>
        /// Un coût, arrondi VERS LE HAUT. Un coût de 58,8 affiché « 58 » à côté d'une
        /// bourse de 58 donne un bouton grisé sans raison visible.
        /// </summary>
        public static string Cout(GrandNombre valeur)
        {
            var nombre = valeur.ToNumber();
            if (EstFini(nombre) && nombre < 1000)
            {
                return nombre < 10 ? NombreJs.ToFixed(nombre, 1) : NombreJs.VersTexte(Math.Ceiling(nombre));
            }
            return Montant(valeur);
        }

        /// <summary>`Math.floor(valeur).toLocaleString('fr-FR')` : groupes de trois, espace fine insécable.</summary>
        public static string Entier(double valeur)
        {
            var texte = NombreJs.VersTexte(Math.Floor(valeur));
            if (texte.IndexOf('e') >= 0) return texte;
            var negatif = texte.StartsWith("-", StringComparison.Ordinal);
            var chiffres = negatif ? texte.Substring(1) : texte;
            var sortie = new StringBuilder();
            for (var i = 0; i < chiffres.Length; i += 1)
            {
                if (i > 0 && (chiffres.Length - i) % 3 == 0) sortie.Append(' ');
                sortie.Append(chiffres[i]);
            }
            return (negatif ? "-" : "") + sortie;
        }

        public static string Duree(double secondes)
        {
            if (!EstFini(secondes) || secondes < 0) return "—";
            if (secondes < 90) return NombreJs.VersTexte(ArrondiJs(secondes)) + " s";
            if (secondes < 5400) return NombreJs.VersTexte(ArrondiJs(secondes / 60)) + " min";
            var heures = secondes / 3600;
            return heures < 24 ? NombreJs.ToFixed(heures, 1) + " h" : NombreJs.VersTexte(ArrondiJs(heures / 24)) + " j";
        }

        /// <summary>
        /// La profondeur d'un banc, en brasses. Une mesure, pas un nom de couche : le
        /// premier creux est à zéro brasse, on descend d'une brasse par creusement.
        /// </summary>
        public static string Profondeur(int palier) =>
            palier == 0 ? "à fleur d’eau" : palier + " brasse" + (palier > 1 ? "s" : "");

        public static string NomDeLAssise(string assise) =>
            TextesProvisoires.NomDesAssises.TryGetValue(assise, out var nom) ? nom : "plus bas";

        /// <summary>Le même nom, en tête de phrase.</summary>
        public static string NomDeLAssiseCapitale(string assise)
        {
            var nom = NomDeLAssise(assise);
            return nom.Length == 0 ? nom : char.ToUpperInvariant(nom[0]) + nom.Substring(1);
        }

        public static string NomDeLEspece(string espece) =>
            TextesProvisoires.NomDesEspeces.TryGetValue(espece, out var nom) ? nom : "un banc sans nom";

        /// <summary>La source d'un terme, mise en mots ici et pas dans le noyau.</summary>
        public static string SourceDuTerme(SourceDeTerme source)
        {
            switch (source.Quoi)
            {
                case QuoiSource.Population:
                    return "ce qui vit là";
                case QuoiSource.Palier:
                    return Profondeur(source.Palier);
                case QuoiSource.Acclimatation:
                    return "ce que tu supportes";
                case QuoiSource.Place:
                    return source.Place + " places faites";
                case QuoiSource.DrapeauxPermanents:
                    return source.Especes == 0
                        ? "aucune espèce au complet"
                        : source.Especes + " espèce" + (source.Especes > 1 ? "s" : "") + " déjà au complet";
                case QuoiSource.EauMurie:
                    // Ce que le joueur doit comprendre du §3.0 sans qu'on le lui
                    // explique : c'est le peuplement qui rajeunit l'eau.
                    if (source.Part >= 0.9) return "une eau vieille que rien n’a troublée";
                    if (source.Part >= 0.5) return "une eau qui se réveille";
                    if (source.Part >= 0.15) return "trop de monde pour que l’eau vieillisse";
                    return "une eau rendue jeune par ce qui l’habite";
                case QuoiSource.CanalAcclimate:
                    return "ce que tu prends à l’eau elle-même";
                default:
                    throw new ArgumentOutOfRangeException(nameof(source), source.Quoi, null);
            }
        }

        /// <summary>La valeur d'une ligne du détail de captation : trois décimales sous dix, une au-delà.</summary>
        public static string ValeurDeLigne(double valeur) => valeur < 10 ? NombreJs.ToFixed(valeur, 3) : NombreJs.ToFixed(valeur, 1);
    }
}
