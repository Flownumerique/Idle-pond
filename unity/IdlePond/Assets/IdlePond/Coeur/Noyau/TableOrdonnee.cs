/*
 * IdlePond — une table immuable qui retient l'ordre d'insertion.
 *
 * C'est le `Record<string, T>` de la version web, avec la propriété que le code
 * TypeScript utilise sans la nommer : l'ordre des clefs d'un objet JavaScript
 * est celui de leur insertion, et il fait partie de la chaîne de sauvegarde. Le
 * test de déterminisme compare cette chaîne ; un `Dictionary` .NET, dont l'ordre
 * n'est pas garanti, la rendrait aléatoire.
 *
 * Immuable : `Avec` et `Fusionner` rendent une nouvelle table. Le noyau est un
 * réducteur pur, et une table qu'on muterait en place ferait mentir l'état
 * précédent qu'un appelant garde encore en main.
 */
using System;
using System.Collections;
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    public sealed class TableOrdonnee<T> : IEnumerable<KeyValuePair<string, T>>
    {
        public static readonly TableOrdonnee<T> Vide = new TableOrdonnee<T>(Array.Empty<string>(), Array.Empty<T>());

        private readonly string[] _clefs;
        private readonly T[] _valeurs;
        private readonly Dictionary<string, int> _index;

        private TableOrdonnee(string[] clefs, T[] valeurs)
        {
            _clefs = clefs;
            _valeurs = valeurs;
            _index = new Dictionary<string, int>(clefs.Length, StringComparer.Ordinal);
            for (var i = 0; i < clefs.Length; i += 1) _index[clefs[i]] = i;
        }

        public static TableOrdonnee<T> Depuis(IEnumerable<KeyValuePair<string, T>> paires)
        {
            var clefs = new List<string>();
            var valeurs = new List<T>();
            var vues = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var paire in paires)
            {
                if (vues.TryGetValue(paire.Key, out var position))
                {
                    valeurs[position] = paire.Value;
                    continue;
                }
                vues[paire.Key] = clefs.Count;
                clefs.Add(paire.Key);
                valeurs.Add(paire.Value);
            }
            return clefs.Count == 0 ? Vide : new TableOrdonnee<T>(clefs.ToArray(), valeurs.ToArray());
        }

        public int Count => _clefs.Length;

        public IReadOnlyList<string> Clefs => _clefs;

        public IReadOnlyList<T> Valeurs => _valeurs;

        public bool Contient(string clef) => _index.ContainsKey(clef);

        public bool EssayerDeLire(string clef, out T valeur)
        {
            if (_index.TryGetValue(clef, out var position))
            {
                valeur = _valeurs[position];
                return true;
            }
            valeur = default;
            return false;
        }

        /// <summary>La valeur, ou `defaut` — le `?? defaut` de la version web.</summary>
        public T Lire(string clef, T defaut) => _index.TryGetValue(clef, out var position) ? _valeurs[position] : defaut;

        public T this[string clef] => _valeurs[_index[clef]];

        /// <summary>`{ ...table, [clef]: valeur }` : une clef connue garde sa place, une neuve va à la fin.</summary>
        public TableOrdonnee<T> Avec(string clef, T valeur)
        {
            if (_index.TryGetValue(clef, out var position))
            {
                var valeurs = (T[])_valeurs.Clone();
                valeurs[position] = valeur;
                return new TableOrdonnee<T>(_clefs, valeurs);
            }
            var clefs = new string[_clefs.Length + 1];
            var nouvelles = new T[_valeurs.Length + 1];
            Array.Copy(_clefs, clefs, _clefs.Length);
            Array.Copy(_valeurs, nouvelles, _valeurs.Length);
            clefs[_clefs.Length] = clef;
            nouvelles[_valeurs.Length] = valeur;
            return new TableOrdonnee<T>(clefs, nouvelles);
        }

        /// <summary>`{ ...this, ...autre }`.</summary>
        public TableOrdonnee<T> Fusionner(TableOrdonnee<T> autre)
        {
            if (autre.Count == 0) return this;
            if (Count == 0) return autre;
            var clefs = new List<string>(_clefs);
            var valeurs = new List<T>(_valeurs);
            for (var i = 0; i < autre._clefs.Length; i += 1)
            {
                if (_index.TryGetValue(autre._clefs[i], out var position)) valeurs[position] = autre._valeurs[i];
                else
                {
                    clefs.Add(autre._clefs[i]);
                    valeurs.Add(autre._valeurs[i]);
                }
            }
            return new TableOrdonnee<T>(clefs.ToArray(), valeurs.ToArray());
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            for (var i = 0; i < _clefs.Length; i += 1) yield return new KeyValuePair<string, T>(_clefs[i], _valeurs[i]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
