/*
 * IdlePond — la boucle de jeu. Port de `src/adaptateurs/boucle.ts`.
 *
 * Elle lit l'heure, appelle Tick, écrit l'état. C'est tout — tout le reste est
 * dans le noyau, où c'est testable.
 *
 * Une différence avec la version web, et elle est voulue : la boucle n'a pas de
 * minuterie à elle. Le web appelle `setInterval` ; ici, c'est l'hôte — le
 * MonoBehaviour `Jeu` — qui appelle `Pas()` à la cadence de
 * `Constantes.PeriodeDeTickMs`. La boucle reste ainsi hors d'Unity, et un test
 * peut la faire tourner à la main.
 *
 * Le dt vient de l'HORLOGE, pas du temps de frame : c'est ce qui rend le jeu
 * insensible à un framerate qui tombe. En contrepartie, un trou dans la suite
 * des pas — l'application mise en pause, la machine en veille, une horloge
 * avancée à la main — arriverait d'un bloc dans le pas suivant, SANS le plafond
 * du hors ligne. La version web a ce défaut : un onglet réveillé après une nuit
 * crédite la nuit entière, et avancer l'horloge pendant la partie contourne la
 * « seule protection anti-triche » de `horloge.ts`.
 *
 * Ici, deux gardes :
 *   - l'hôte ARRÊTE la boucle pendant une mise en pause, et le retour passe par
 *     le crédit hors ligne ;
 *   - un pas dont le dt dépasse `EcartMaximalEntreDeuxPasSecondes` n'est pas
 *     joué : il est rendu à `surAbsence`, qui le crédite comme une absence —
 *     plafonnée, comptée dans les heures créditées, et annoncée.
 */
using System;
using System.Collections.Generic;
using IdlePond.Noyau;

namespace IdlePond.Adaptateurs
{
    public sealed class Boucle
    {
        /// <summary>
        /// Au-delà, deux pas consécutifs ne sont plus « la même session » : l'écart
        /// est une absence. Cinquante fois la période de tick — assez pour qu'une
        /// frame lente ou un ramasse-miettes ne soient jamais pris pour une absence,
        /// assez peu pour qu'aucune absence ne soit prise pour une frame.
        /// </summary>
        public const double EcartMaximalEntreDeuxPasSecondes = 50 * Constantes.PeriodeDeTickMs / 1000;

        private readonly Func<EtatJeu> _lire;
        private readonly Action<EtatJeu> _ecrire;
        private readonly Action<IReadOnlyList<string>> _surSucces;
        private readonly IHorloge _horloge;
        private readonly Action _surAbsence;
        private double _dernierInstantMs;

        /// <param name="surAbsence">
        /// Appelé à la place d'un pas trop long. Sans lui, la boucle se comporte comme
        /// celle du web et joue l'écart tel quel — c'est ce que font les tests de
        /// parité, et ce que l'hôte Unity ne fait pas.
        /// </param>
        public Boucle(
            Func<EtatJeu> lire,
            Action<EtatJeu> ecrire,
            Action<IReadOnlyList<string>> surSucces = null,
            IHorloge horloge = null,
            Action surAbsence = null)
        {
            _lire = lire ?? throw new ArgumentNullException(nameof(lire));
            _ecrire = ecrire ?? throw new ArgumentNullException(nameof(ecrire));
            _surSucces = surSucces;
            _horloge = horloge ?? HorlogeSysteme.Instance;
            _surAbsence = surAbsence;
            _dernierInstantMs = _horloge.MaintenantMs();
        }

        public bool EnMarche { get; private set; }

        public void Demarrer()
        {
            if (EnMarche) return;
            _dernierInstantMs = _horloge.MaintenantMs();
            EnMarche = true;
        }

        public void Arreter() => EnMarche = false;

        /// <summary>Un pas de boucle. Sans effet si la boucle est arrêtée ou si l'horloge a reculé.</summary>
        public void Pas()
        {
            if (!EnMarche) return;
            var maintenant = _horloge.MaintenantMs();
            var dt = (maintenant - _dernierInstantMs) / 1000;
            _dernierInstantMs = maintenant;
            // Recul d'horloge : ignoré, jamais rattrapé à l'envers.
            if (!(dt > 0)) return;
            if (_surAbsence != null && dt > EcartMaximalEntreDeuxPasSecondes)
            {
                _surAbsence();
                return;
            }

            var resultat = Reducteur.TickDetaille(_lire(), dt);
            // L'intervalle entre deux succès est une observation, faite ici, à la
            // cadence où elle est faite — pas une mécanique du noyau (§11).
            _ecrire(SystemeDeSucces.EnregistrerIntervalleDeSucces(resultat.Etat, resultat.Declenches));
            if (resultat.Declenches.Count > 0) _surSucces?.Invoke(resultat.Declenches);
        }
    }
}
