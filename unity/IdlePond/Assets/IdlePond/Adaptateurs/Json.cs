/*
 * IdlePond — JSON, lu et écrit comme `JSON.parse` et `JSON.stringify`.
 *
 * Pourquoi pas `JsonUtility` : il ne sait ni les tables à clefs libres
 * (`bancs`, `succes`) ni les valeurs absentes, et il vit dans UnityEngine — or
 * la persistance doit rester vérifiable hors Unity. Pourquoi pas Newtonsoft : un
 * paquet de plus, pour trois cents lignes, et une écriture des nombres qui n'est
 * pas celle du web.
 *
 * Deux propriétés comptent, et elles sont celles de JavaScript :
 *   - un objet garde l'ordre de ses clefs (le test de déterminisme compare des
 *     chaînes) ;
 *   - un nombre s'écrit comme `String(nombre)` le ferait (`NombreJs`), et un
 *     nombre non fini s'écrit `null`.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using IdlePond.Nombres;

namespace IdlePond.Adaptateurs
{
    public abstract class JsonValeur
    {
        public string EnTexte()
        {
            var sortie = new StringBuilder();
            Ecrire(sortie);
            return sortie.ToString();
        }

        internal abstract void Ecrire(StringBuilder sortie);
    }

    public sealed class JsonNul : JsonValeur
    {
        public static readonly JsonNul Valeur = new JsonNul();

        private JsonNul()
        {
        }

        internal override void Ecrire(StringBuilder sortie) => sortie.Append("null");
    }

    public sealed class JsonBooleen : JsonValeur
    {
        public readonly bool Valeur;

        public JsonBooleen(bool valeur) => Valeur = valeur;

        internal override void Ecrire(StringBuilder sortie) => sortie.Append(Valeur ? "true" : "false");
    }

    public sealed class JsonNombre : JsonValeur
    {
        public readonly double Valeur;

        public JsonNombre(double valeur) => Valeur = valeur;

        internal override void Ecrire(StringBuilder sortie)
        {
            if (double.IsNaN(Valeur) || double.IsInfinity(Valeur)) sortie.Append("null");
            else sortie.Append(NombreJs.VersTexte(Valeur));
        }
    }

    public sealed class JsonTexte : JsonValeur
    {
        public readonly string Valeur;

        public JsonTexte(string valeur) => Valeur = valeur ?? "";

        internal override void Ecrire(StringBuilder sortie) => Json.EcrireChaine(sortie, Valeur);
    }

    public sealed class JsonTableau : JsonValeur
    {
        public readonly List<JsonValeur> Elements = new List<JsonValeur>();

        public JsonTableau()
        {
        }

        public JsonTableau(IEnumerable<JsonValeur> elements) => Elements.AddRange(elements);

        public int Count => Elements.Count;

        public JsonValeur this[int index] => Elements[index];

        public void Ajouter(JsonValeur valeur) => Elements.Add(valeur ?? JsonNul.Valeur);

        internal override void Ecrire(StringBuilder sortie)
        {
            sortie.Append('[');
            for (var i = 0; i < Elements.Count; i += 1)
            {
                if (i > 0) sortie.Append(',');
                Elements[i].Ecrire(sortie);
            }
            sortie.Append(']');
        }
    }

    /// <summary>Un objet JSON dont les clefs gardent leur ordre d'insertion, comme en JavaScript.</summary>
    public sealed class JsonObjet : JsonValeur
    {
        private readonly List<string> _clefs = new List<string>();
        private readonly Dictionary<string, JsonValeur> _valeurs = new Dictionary<string, JsonValeur>(StringComparer.Ordinal);

        public IReadOnlyList<string> Clefs => _clefs;

        public bool Contient(string clef) => _valeurs.ContainsKey(clef);

        /// <summary>La valeur, ou null si la clef est absente — le `undefined` de JavaScript.</summary>
        public JsonValeur Lire(string clef) => _valeurs.TryGetValue(clef, out var valeur) ? valeur : null;

        /// <summary>Une clef connue garde sa place ; une neuve va à la fin.</summary>
        public JsonObjet Poser(string clef, JsonValeur valeur)
        {
            if (!_valeurs.ContainsKey(clef)) _clefs.Add(clef);
            _valeurs[clef] = valeur ?? JsonNul.Valeur;
            return this;
        }

        public JsonObjet Retirer(string clef)
        {
            if (_valeurs.Remove(clef)) _clefs.Remove(clef);
            return this;
        }

        /// <summary>Copie superficielle : `{ ...objet }`.</summary>
        public JsonObjet Copier()
        {
            var copie = new JsonObjet();
            foreach (var clef in _clefs) copie.Poser(clef, _valeurs[clef]);
            return copie;
        }

        internal override void Ecrire(StringBuilder sortie)
        {
            sortie.Append('{');
            for (var i = 0; i < _clefs.Count; i += 1)
            {
                if (i > 0) sortie.Append(',');
                Json.EcrireChaine(sortie, _clefs[i]);
                sortie.Append(':');
                _valeurs[_clefs[i]].Ecrire(sortie);
            }
            sortie.Append('}');
        }
    }

    public static class Json
    {
        /* ─── Écriture ──────────────────────────────────────────────────────── */

        /// <summary>L'échappement de `JSON.stringify` (ES2019, chaînes bien formées).</summary>
        internal static void EcrireChaine(StringBuilder sortie, string texte)
        {
            sortie.Append('"');
            for (var i = 0; i < texte.Length; i += 1)
            {
                var c = texte[i];
                switch (c)
                {
                    case '"': sortie.Append("\\\""); break;
                    case '\\': sortie.Append("\\\\"); break;
                    case '\b': sortie.Append("\\b"); break;
                    case '\f': sortie.Append("\\f"); break;
                    case '\n': sortie.Append("\\n"); break;
                    case '\r': sortie.Append("\\r"); break;
                    case '\t': sortie.Append("\\t"); break;
                    default:
                        if (c < 0x20) EcrireEchappement(sortie, c);
                        else if (char.IsHighSurrogate(c) && i + 1 < texte.Length && char.IsLowSurrogate(texte[i + 1]))
                        {
                            sortie.Append(c).Append(texte[i + 1]);
                            i += 1;
                        }
                        else if (char.IsSurrogate(c)) EcrireEchappement(sortie, c);
                        else sortie.Append(c);
                        break;
                }
            }
            sortie.Append('"');
        }

        private static void EcrireEchappement(StringBuilder sortie, char c)
        {
            sortie.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
        }

        /* ─── Lecture ───────────────────────────────────────────────────────── */

        /// <summary>`JSON.parse`. Lève une FormatException sur un texte mal formé.</summary>
        public static JsonValeur Lire(string texte)
        {
            if (texte == null) throw new FormatException("JSON vide");
            var lecteur = new Lecteur(texte);
            lecteur.Blancs();
            var valeur = lecteur.Valeur(0);
            lecteur.Blancs();
            if (!lecteur.Fini) throw lecteur.Erreur("caractères après la valeur");
            return valeur;
        }

        public static bool EssayerDeLire(string texte, out JsonValeur valeur)
        {
            try
            {
                valeur = Lire(texte);
                return true;
            }
            catch (FormatException)
            {
                valeur = null;
                return false;
            }
        }

        private sealed class Lecteur
        {
            /// <summary>Une save n'a pas trente niveaux d'imbrication ; un texte hostile, si.</summary>
            private const int ProfondeurMaximale = 64;

            private readonly string _texte;
            private int _position;

            public Lecteur(string texte) => _texte = texte;

            public bool Fini => _position >= _texte.Length;

            public FormatException Erreur(string quoi) =>
                new FormatException("JSON invalide à la position " + _position + " : " + quoi);

            public void Blancs()
            {
                while (_position < _texte.Length)
                {
                    var c = _texte[_position];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _position += 1;
                    else break;
                }
            }

            public JsonValeur Valeur(int profondeur)
            {
                if (profondeur > ProfondeurMaximale) throw Erreur("imbrication trop profonde");
                if (Fini) throw Erreur("fin inattendue");
                var c = _texte[_position];
                switch (c)
                {
                    case '{': return Objet(profondeur);
                    case '[': return Tableau(profondeur);
                    case '"': return new JsonTexte(Chaine());
                    case 't': Mot("true"); return new JsonBooleen(true);
                    case 'f': Mot("false"); return new JsonBooleen(false);
                    case 'n': Mot("null"); return JsonNul.Valeur;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return Nombre();
                        throw Erreur("caractère inattendu « " + c + " »");
                }
            }

            private void Mot(string mot)
            {
                if (string.CompareOrdinal(_texte, _position, mot, 0, mot.Length) != 0) throw Erreur("« " + mot + " » attendu");
                _position += mot.Length;
            }

            private JsonObjet Objet(int profondeur)
            {
                var objet = new JsonObjet();
                _position += 1;
                Blancs();
                if (!Fini && _texte[_position] == '}')
                {
                    _position += 1;
                    return objet;
                }
                for (;;)
                {
                    Blancs();
                    if (Fini || _texte[_position] != '"') throw Erreur("clef attendue");
                    var clef = Chaine();
                    Blancs();
                    if (Fini || _texte[_position] != ':') throw Erreur("« : » attendu");
                    _position += 1;
                    Blancs();
                    objet.Poser(clef, Valeur(profondeur + 1));
                    Blancs();
                    if (Fini) throw Erreur("objet non fermé");
                    if (_texte[_position] == ',')
                    {
                        _position += 1;
                        continue;
                    }
                    if (_texte[_position] == '}')
                    {
                        _position += 1;
                        return objet;
                    }
                    throw Erreur("« , » ou « } » attendu");
                }
            }

            private JsonTableau Tableau(int profondeur)
            {
                var tableau = new JsonTableau();
                _position += 1;
                Blancs();
                if (!Fini && _texte[_position] == ']')
                {
                    _position += 1;
                    return tableau;
                }
                for (;;)
                {
                    Blancs();
                    tableau.Ajouter(Valeur(profondeur + 1));
                    Blancs();
                    if (Fini) throw Erreur("tableau non fermé");
                    if (_texte[_position] == ',')
                    {
                        _position += 1;
                        continue;
                    }
                    if (_texte[_position] == ']')
                    {
                        _position += 1;
                        return tableau;
                    }
                    throw Erreur("« , » ou « ] » attendu");
                }
            }

            private string Chaine()
            {
                _position += 1;
                var sortie = new StringBuilder();
                while (!Fini)
                {
                    var c = _texte[_position++];
                    if (c == '"') return sortie.ToString();
                    if (c < 0x20) throw Erreur("caractère de contrôle dans une chaîne");
                    if (c != '\\')
                    {
                        sortie.Append(c);
                        continue;
                    }
                    if (Fini) break;
                    var e = _texte[_position++];
                    switch (e)
                    {
                        case '"': sortie.Append('"'); break;
                        case '\\': sortie.Append('\\'); break;
                        case '/': sortie.Append('/'); break;
                        case 'b': sortie.Append('\b'); break;
                        case 'f': sortie.Append('\f'); break;
                        case 'n': sortie.Append('\n'); break;
                        case 'r': sortie.Append('\r'); break;
                        case 't': sortie.Append('\t'); break;
                        case 'u':
                            if (_position + 4 > _texte.Length) throw Erreur("échappement \\u tronqué");
                            if (!int.TryParse(_texte.Substring(_position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            {
                                throw Erreur("échappement \\u invalide");
                            }
                            sortie.Append((char)code);
                            _position += 4;
                            break;
                        default: throw Erreur("échappement inconnu");
                    }
                }
                throw Erreur("chaîne non fermée");
            }

            private JsonNombre Nombre()
            {
                var debut = _position;
                if (_texte[_position] == '-') _position += 1;
                Chiffres(true);
                if (!Fini && _texte[_position] == '.')
                {
                    _position += 1;
                    Chiffres(false);
                }
                if (!Fini && (_texte[_position] == 'e' || _texte[_position] == 'E'))
                {
                    _position += 1;
                    if (!Fini && (_texte[_position] == '+' || _texte[_position] == '-')) _position += 1;
                    Chiffres(false);
                }
                var lu = _texte.Substring(debut, _position - debut);
                if (!double.TryParse(lu, NumberStyles.Float, CultureInfo.InvariantCulture, out var valeur)) throw Erreur("nombre invalide");
                return new JsonNombre(valeur);
            }

            private void Chiffres(bool entier)
            {
                var debut = _position;
                while (!Fini && _texte[_position] >= '0' && _texte[_position] <= '9') _position += 1;
                if (_position == debut) throw Erreur("chiffre attendu");
                // JSON refuse les zéros de tête : « 012 » n'est pas un nombre.
                if (entier && _position - debut > 1 && _texte[debut] == '0') throw Erreur("zéro de tête");
            }
        }

        /* ─── Lecture tolérante, pour la désérialisation ─────────────────────── */

        public static JsonObjet CommeObjet(JsonValeur valeur) => valeur as JsonObjet;

        public static JsonTableau CommeTableau(JsonValeur valeur) => valeur as JsonTableau;

        public static bool EssayerNombre(JsonValeur valeur, out double nombre)
        {
            if (valeur is JsonNombre n)
            {
                nombre = n.Valeur;
                return true;
            }
            nombre = 0;
            return false;
        }

        public static double NombreOu(JsonValeur valeur, double repli) => EssayerNombre(valeur, out var n) ? n : repli;

        public static int EntierOu(JsonValeur valeur, int repli)
        {
            if (!EssayerNombre(valeur, out var n) || double.IsNaN(n) || double.IsInfinity(n)) return repli;
            if (n > int.MaxValue || n < int.MinValue) return repli;
            return (int)n;
        }

        public static string TexteOu(JsonValeur valeur, string repli) => valeur is JsonTexte t ? t.Valeur : repli;
    }
}
