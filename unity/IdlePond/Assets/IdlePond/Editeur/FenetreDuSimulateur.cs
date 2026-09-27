/*
 * IdlePond — le simulateur, dans l'éditeur.
 *
 * `IdlePond ▸ Simulateur` lance le MÊME simulateur que les tests — le même Tick
 * que le jeu, avec un dt plus grand — et affiche le relevé du §11 cycle par
 * cycle : durée écoulée, fraction en redescente (le risque n° 1), paliers
 * ouverts, pic de production, Foi gagnée. C'est l'outil de calibrage des
 * graines du §13.3 : on change une constante, on relance, on lit.
 *
 * Rappel du §13.4, à garder sous les yeux en lisant le tableau : seules les
 * politiques de check-in produisent une croissance en temps calendaire. Une
 * cible exprimée en heures ACTIVES par cycle n'a pas de sens ici.
 */
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Noyau;
using IdlePond.Simulateur;
using UnityEditor;
using UnityEngine;

namespace IdlePond.Editeur
{
    public sealed class FenetreDuSimulateur : EditorWindow
    {
        private int _cycles = Constantes.NombreDEclosionsVise;
        private int _graine = 1;
        private bool _mondeLivreSeulement;
        private float _checkInHeures = 4;
        private float _patienceSecondes = 90;
        private float _sessionMaxMinutes = 15;
        private float _fractionDeSaturation = 0.95f;
        private ResultatDeSimulation _resultat;
        private double _dureeDeCalculSecondes;
        private Vector2 _defilement;

        [MenuItem("IdlePond/Simulateur", priority = 1)]
        public static void Ouvrir() => GetWindow<FenetreDuSimulateur>("Simulateur IdlePond");

        private static string Heures(double secondes) => (secondes / 3600).ToString("0.0", CultureInfo.InvariantCulture) + " h";

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Partie", EditorStyles.boldLabel);
            _cycles = Mathf.Clamp(EditorGUILayout.IntField("Cycles", _cycles), 1, 60);
            _graine = EditorGUILayout.IntField("Graine", _graine);
            _mondeLivreSeulement = EditorGUILayout.Toggle(
                new GUIContent("Monde livré seulement", "La Noue et ses six paliers, comme le jeu. Sinon : les 62 paliers de l'économie complète."),
                _mondeLivreSeulement);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Politique du joueur", EditorStyles.boldLabel);
            _checkInHeures = EditorGUILayout.Slider(new GUIContent("Retour toutes les (h)", "LE réglage de temps calendaire du jeu."), _checkInHeures, 0.5f, 24);
            _patienceSecondes = EditorGUILayout.Slider("Patience en session (s)", _patienceSecondes, 5, 600);
            _sessionMaxMinutes = EditorGUILayout.Slider("Session au plus (min)", _sessionMaxMinutes, 1, 120);
            _fractionDeSaturation = EditorGUILayout.Slider(
                new GUIContent("Éclore à (part de A∞)", "Passé ce point, rester n'achète plus que de la Foi (§6.4)."),
                _fractionDeSaturation, 0.5f, 1);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Simuler", GUILayout.Height(28))) Simuler();
                using (new EditorGUI.DisabledScope(_resultat == null))
                {
                    if (GUILayout.Button("Copier le relevé (CSV)", GUILayout.Height(28)))
                    {
                        EditorGUIUtility.systemCopyBuffer = EnCsv(_resultat);
                        ShowNotification(new GUIContent("Relevé copié"));
                    }
                }
            }

            if (_resultat == null) return;
            Dessiner(_resultat);
        }

        private void Simuler()
        {
            var politique = Politique.ParDefaut with
            {
                IntervalleDeCheckInSecondes = _checkInHeures * 3600,
                PatienceDansLaSessionSecondes = _patienceSecondes,
                DureeMaxDeSessionSecondes = _sessionMaxMinutes * 60,
                FractionDeSaturationPourEclore = _fractionDeSaturation,
            };
            var debut = EditorApplication.timeSinceStartup;
            try
            {
                EditorUtility.DisplayProgressBar("IdlePond", "Simulation de " + _cycles + " cycles…", 0.5f);
                _resultat = Simulation.Simuler(
                    _cycles, politique, unchecked((uint)_graine), null, _mondeLivreSeulement ? Assises.PaliersLivres : (int?)null);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            _dureeDeCalculSecondes = EditorApplication.timeSinceStartup - debut;
        }

        private void Dessiner(ResultatDeSimulation resultat)
        {
            EditorGUILayout.Space();
            if (resultat.CycleNonConvergent != null)
            {
                EditorGUILayout.HelpBox("Le cycle " + resultat.CycleNonConvergent + " n'a pas convergé.", MessageType.Warning);
            }
            EditorGUILayout.HelpBox(
                resultat.CyclesAcheves + " cycles — temps ACTIF " + Heures(resultat.TempsActifSecondes)
                + " (cible ~38 h), temps ÉCOULÉ " + Heures(resultat.TempsEcouleSecondes)
                + " (cible ~600 h). Calculé en " + _dureeDeCalculSecondes.ToString("0.0", CultureInfo.InvariantCulture) + " s.",
                MessageType.Info);

            var releve = resultat.Releve;
            var intervalle = releve.IntervalleMoyenEntreSuccesSecondes;
            EditorGUILayout.LabelField("Intervalle moyen entre succès",
                intervalle == null ? "—" : (intervalle.Value / 60).ToString("0.0", CultureInfo.InvariantCulture) + " min");
            EditorGUILayout.LabelField("τ de repeuplement", releve.TauDeRepeuplementSecondes.ToString("0", CultureInfo.InvariantCulture) + " s");

            EditorGUILayout.Space();
            _defilement = EditorGUILayout.BeginScrollView(_defilement);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var titre in new[] { "Cycle", "Durée écoulée", "En redescente", "Paliers", "Pic /s", "Foi" })
                {
                    GUILayout.Label(titre, EditorStyles.boldLabel, GUILayout.Width(110));
                }
            }
            foreach (var cycle in releve.Cycles)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((cycle.Index + 1).ToString(CultureInfo.InvariantCulture), GUILayout.Width(110));
                    GUILayout.Label(Heures(cycle.DureeEcouleeSecondes), GUILayout.Width(110));
                    GUILayout.Label((cycle.FractionEnRedescente * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %", GUILayout.Width(110));
                    GUILayout.Label(cycle.PaliersOuverts.ToString(CultureInfo.InvariantCulture), GUILayout.Width(110));
                    GUILayout.Label(cycle.ProductionPicParSeconde.ToString("0.###e+0", CultureInfo.InvariantCulture), GUILayout.Width(110));
                    GUILayout.Label(cycle.FoiGagnee.ToString("0", CultureInfo.InvariantCulture), GUILayout.Width(110));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static string EnCsv(ResultatDeSimulation resultat)
        {
            var sortie = new StringBuilder("cycle;duree_ecoulee_s;fraction_en_redescente;paliers_ouverts;pic_par_s;foi_gagnee\n");
            foreach (var c in resultat.Releve.Cycles)
            {
                sortie.Append(string.Join(";", new[]
                {
                    (c.Index + 1).ToString(CultureInfo.InvariantCulture),
                    c.DureeEcouleeSecondes.ToString("R", CultureInfo.InvariantCulture),
                    c.FractionEnRedescente.ToString("R", CultureInfo.InvariantCulture),
                    c.PaliersOuverts.ToString(CultureInfo.InvariantCulture),
                    c.ProductionPicParSeconde.ToString("R", CultureInfo.InvariantCulture),
                    c.FoiGagnee.ToString("R", CultureInfo.InvariantCulture),
                })).Append('\n');
            }
            return sortie.ToString();
        }
    }
}
