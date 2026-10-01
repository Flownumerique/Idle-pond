using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using IdlePond.Tests.Outils;
using NUnit.Framework;

namespace IdlePond.Tests
{
    /// <summary>
    /// §8.4 — plancher garanti sur l'assise I.
    ///
    /// | Garantie              | Valeur                                    |
    /// |-----------------------|-------------------------------------------|
    /// | Premier succès        | Dans les DEUX PREMIÈRES MINUTES           |
    /// | Première demi-heure   | Un déclenchement toutes les 3 à 5 minutes |
    /// | Première renaissance  | Un franchissement, OBLIGATOIREMENT        |
    ///
    /// Ce n'est pas une intention de design, c'est une garantie chiffrée : elle se
    /// mesure, ou elle n'existe pas. Le test joue les trente premières minutes avec
    /// un joueur qui achète le moins cher dès qu'il peut, et relève les instants.
    /// </summary>
    public class PlancherDeCadenceTests
    {
        static readonly IReadOnlyList<(string Id, double InstantSecondes)> Releve = Joueur.JoueUneDemiHeure();

        [Test, Description("le premier succès tombe en moins de deux minutes")]
        public void Le_premier_succes_tombe_en_moins_de_deux_minutes()
        {
            Assert.That(Releve.Count, Is.GreaterThan(0), "aucun succès déclenché");
            Assert.That(Releve[0].InstantSecondes, Is.LessThan(Constantes.PREMIER_SUCCES_AVANT_SECONDES));
        }

        [Test, Description("un déclenchement au moins toutes les cinq minutes sur la première demi-heure")]
        public void Un_declenchement_au_moins_toutes_les_cinq_minutes_sur_la_premiere_demi_heure()
        {
            var trous = new List<string>();
            double precedent = 0;
            foreach (var declenchement in Releve)
            {
                var ecart = declenchement.InstantSecondes - precedent;
                if (ecart > Constantes.CADENCE_MAX_ENTRE_SUCCES_SECONDES)
                    trous.Add($"{(ecart / 60).ToString("F1", CultureInfo.InvariantCulture)} min avant {declenchement.Id}");
                precedent = declenchement.InstantSecondes;
            }
            Assert.That(trous, Is.Empty, "la cadence du §8.4 est trouée entre deux succès");
        }

        /// <summary>
        /// PARQUÉ — défaut du PLAN, pas de la tâche 9. En TypeScript, cette assertion
        /// vivait sous `it.fails` : ceci DEVAIT échouer tant que le défaut tenait. Si
        /// l'assertion se remettait à réussir, le test TypeScript échouait À SON TOUR
        /// (c'est le sens d'`it.fails`) — ce qui forçait qui que ce soit à supprimer ce
        /// bloc plutôt que de laisser un test muet traîner. Ce n'était pas un `skip` :
        /// un test qui ne peut plus échouer est pire qu'aucun test — cette famille de
        /// défaut a déjà mordu ce plan trois fois.
        ///
        /// Le silence après le dernier succès atteignable dans la fenêtre de 30 min
        /// dépasse les 5 min permises. Mesuré (tâche 9, harnais `tests/joueur.ts`,
        /// assise I, seconde par seconde) :
        ///
        ///   dernier succès atteignable dans cette fenêtre : t ≈ 1381 s
        ///   silence jusqu'à la 30ᵉ minute (1800 s)         : 419 s
        ///   seuil autorisé (`CADENCE_MAX_ENTRE_SUCCES_SECONDES`) : 300 s
        ///   dépassement                                    : 119 s
        ///
        /// Isolé avec un contrôle propre : `DEBIT_HEROS = 0` fait passer ce test
        /// intégralement (dernier succès atteignable à t ≈ 1542 s, silence de 258 s,
        /// soit 42 s de marge sous les 300 s — la marge HISTORIQUE de ce test, avant
        /// que la tâche 9 touche quoi que ce soit). Toute valeur strictement
        /// positive testée — 0.05 (la graine du brief) jusqu'à 2× le plancher
        /// arithmétique du premier déblocage, sur deux sessions de mesure —
        /// reproduit un trou de fin. Ce n'est donc pas une question de magnitude :
        /// aucune valeur de `DEBIT_HEROS` ne peut satisfaire à la fois ce test et
        /// `RESULTATS.md` finding 3 (qui exige un débit strictement positif, sans
        /// quoi l'état dégénéré — plus rien n'est jamais affordable — redevient
        /// atteignable). `DEBIT_HEROS` reste à 0.05 : voir son commentaire dans
        /// `Constantes.cs`.
        ///
        /// POURQUOI ACCÉLÉRER AGGRAVE (contre-intuitif — c'est ce que quelqu'un
        /// tentera de « corriger » en remontant le débit) : un joueur plus rapide ne
        /// fait pas grossir le nombre de succès ATTEIGNABLES dans la fenêtre — ce
        /// nombre est fixé par le registre fini de la Noue — il se contente de les
        /// déclencher tous PLUS TÔT, ce qui allonge d'autant le silence après le
        /// dernier d'entre eux, jusqu'à la trentième minute.
        ///
        /// MESURÉ DEPUIS (tâche 12) : la politique de gain marginal ne ferme PAS ce
        /// trou, elle l'aggrave de 21 s. Même harnais, assise I, graine 1 —
        ///
        ///   le moins cher d'abord (politique d'ici) : 20 succès, dernier 1381 s, silence 419 s
        ///   gain marginal (tâche 12)                : 20 succès, dernier 1360 s, silence 440 s
        ///
        /// — ce que le paragraphe ci-dessus prédisait, exactement : le nombre de
        /// succès ATTEIGNABLES ne bouge pas, il est fixé par le registre fini de la
        /// Noue ; un joueur qui achète MIEUX les déclenche seulement plus tôt. La
        /// voie « une meilleure politique » est donc close, et close par la mesure.
        ///
        /// Ferme quand : la tâche 13 résout les nombres qui décident de
        /// l'atteignabilité. Une seconde voie — densifier les succès atteignables de
        /// la Noue entre la 25ᵉ et la 30ᵉ minute — existe mais sort de ce plan (GDD).
        ///
        /// Porté comme test normal le 2026-09-27 : il passait en TypeScript,
        /// l'it.fails était périmé.
        /// </summary>
        [Test, Description("le silence après le dernier succès atteignable ne dépasse pas cinq minutes — PARQUÉ, voir tâches 12/13")]
        public void Le_silence_apres_le_dernier_succes_atteignable_ne_depasse_pas_cinq_minutes_PARQUE_voir_taches_12_13()
        {
            var dernier = Releve.Count > 0 ? Releve[Releve.Count - 1].InstantSecondes : 0;
            var fin = Constantes.FENETRE_DU_PLANCHER_DE_CADENCE_SECONDES - dernier;
            Assert.That(fin, Is.LessThanOrEqualTo(Constantes.CADENCE_MAX_ENTRE_SUCCES_SECONDES));
        }

        [Test, Description("la première renaissance déclenche un franchissement, obligatoirement")]
        public void La_premiere_renaissance_declenche_un_franchissement_obligatoirement()
        {
            var etat = Reducteur.EtatInitial(1);
            etat = Renaissance.Renaitre(etat);
            var declenches = Reducteur.TickDetaille(etat, 0.1).Declenches;
            var franchissements = declenches
                .Where(id => RegistreDesSucces.Tous.FirstOrDefault(s => s.Id == id)?.Famille == FamilleDeSucces.Franchissement)
                .ToList();
            Assert.That(franchissements.Count, Is.GreaterThan(0));
        }

        [Test, Description("la cadence ne tient pas à un seul succès qui se répéterait")]
        public void La_cadence_ne_tient_pas_a_un_seul_succes_qui_se_repeterait()
        {
            var identifiants = new HashSet<string>(Releve.Select(d => d.Id));
            Assert.That(identifiants.Count, Is.EqualTo(Releve.Count));
        }
    }
}
