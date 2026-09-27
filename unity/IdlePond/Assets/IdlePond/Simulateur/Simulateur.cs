/*
 * IdlePond — simulateur. Port de `src/simulateur/simulateur.ts`.
 *
 * Réutilise le noyau tel quel. C'est tout l'intérêt du contrat du §5.1 : le
 * simulateur n'a pas de moteur à lui, il appelle le même Tick que le jeu avec un
 * dt plus grand.
 *
 * Le simulateur porte les POLITIQUES — ce que le joueur fait et quand il revient.
 * Le noyau ne décide jamais à sa place.
 *
 * §13.4 : l'économie est invariante d'échelle. Seules les politiques de check-in
 * produisent une croissance apparente en temps calendaire. Toute cible exprimée
 * en heures actives par cycle sera rejetée ici.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Simulateur
{
    public sealed record Politique
    {
        /// <summary>Intervalle entre deux retours du joueur. LE réglage de temps calendaire du jeu.</summary>
        public double IntervalleDeCheckInSecondes { get; init; }
        /// <summary>Le joueur reste tant que les achats s'enchaînent sous ce délai.</summary>
        public double PatienceDansLaSessionSecondes { get; init; }
        /// <summary>Il ne reste jamais plus longtemps que ça d'affilée.</summary>
        public double DureeMaxDeSessionSecondes { get; init; }
        /// <summary>Granularité minimale d'un pas.</summary>
        public double DtMinSecondes { get; init; }
        /// <summary>Part de `A∞` au-delà de laquelle rester ne rapporte plus de profondeur.</summary>
        public double FractionDeSaturationPourEclore { get; init; }
        /// <summary>Garde-fou : un cycle qui dépasse cette durée est déclaré non convergent.</summary>
        public double DureeMaxParCycleSecondes { get; init; }

        public static readonly Politique ParDefaut = new Politique
        {
            IntervalleDeCheckInSecondes = 4 * 3600,
            PatienceDansLaSessionSecondes = 90,
            DureeMaxDeSessionSecondes = 15 * 60,
            DtMinSecondes = 5,
            FractionDeSaturationPourEclore = 0.95,
            DureeMaxParCycleSecondes = 4000 * 3600,
        };
    }

    public sealed record MesureDeSession(int Cycle, double SecondesActives);

    public sealed record ResultatDeSimulation
    {
        public EtatJeu Etat { get; init; }
        public Releve Releve { get; init; }
        public int CyclesDemandes { get; init; }
        public int CyclesAcheves { get; init; }
        public int? CycleNonConvergent { get; init; }
        /// <summary>Temps où le joueur était devant l'écran. La cible du §5.4 : ~38 h.</summary>
        public double TempsActifSecondes { get; init; }
        /// <summary>Temps de jeu écoulé, sessions et absences confondues. Cible : ~600 h.</summary>
        public double TempsEcouleSecondes { get; init; }
        public IReadOnlyList<MesureDeSession> Sessions { get; init; }
    }

    public static class Simulation
    {
        private sealed class Option
        {
            public GrandNombre Cout;
            /// <summary>Production supplémentaire à pleine charge, une fois la place peuplée.</summary>
            public GrandNombre Gain;
            public bool EstUnCreusement;
            public Func<EtatJeu, EtatJeu> Appliquer;
        }

        /// <summary>
        /// Toutes les dépenses ATTEIGNABLES à cet instant. Une dépense qui coûte plus que
        /// la contenance est hors de portée pour toujours — le blocage doux du §6.4.
        /// </summary>
        private static List<Option> OptionsOuvertes(EtatJeu etat)
        {
            var plafond = Economie.Contenance(etat);
            var options = new List<Option>();

            if (!Economie.ToutEstCreuse(etat))
            {
                var cout = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
                if (cout.Lte(plafond))
                {
                    options.Add(new Option { Cout = cout, Gain = GrandNombre.Zero, EstUnCreusement = true, Appliquer = Reducteur.Creuser });
                }
            }

            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
            {
                foreach (var banc in Paliers.Liste[palier].Bancs)
                {
                    var place = etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var b) ? b.Place : 0;
                    var id = banc.Id;
                    var cout = place == 0 ? Economie.CoutDeConviction(etat, banc) : Economie.CoutDePlace(etat, banc, place);
                    if (cout.Gt(plafond)) continue;
                    var avant = Economie.TauxParIndividu(etat, banc, place).Mul(place);
                    var apres = Economie.TauxParIndividu(etat, banc, place + 1).Mul(place + 1);
                    options.Add(new Option
                    {
                        Cout = cout,
                        Gain = apres.Sub(avant),
                        EstUnCreusement = false,
                        Appliquer = place == 0 ? (Func<EtatJeu, EtatJeu>)(e => Reducteur.Convaincre(e, id)) : e => Reducteur.AcheterPlace(e, id),
                    });
                }
            }

            return options;
        }

        /// <summary>
        /// L'achat qu'un joueur qui vise la profondeur ferait maintenant, ou null. La
        /// règle est celle du retour sur investissement : une place ne vaut que si
        /// elle se rembourse avant le creusement qu'on attend.
        /// </summary>
        private static Option MeilleurAchat(EtatJeu etat, List<Option> options)
        {
            var payables = options.Where(o => o.Cout.Lte(etat.Cycle.ManaCourant)).ToList();
            if (payables.Count == 0) return null;

            var creusement = options.FirstOrDefault(o => o.EstUnCreusement);
            var production = Economie.ProductionTotaleParSeconde(etat);

            // Plus rien à creuser : on peuple, du meilleur rapport au moins bon.
            if (creusement == null || production.Lte(0))
            {
                Option meilleur = null;
                foreach (var option in payables)
                {
                    if (option.Gain.Lte(0)) continue;
                    if (meilleur == null) meilleur = option;
                    else if (option.Gain.Div(option.Cout).Gt(meilleur.Gain.Div(meilleur.Cout))) meilleur = option;
                }
                return meilleur;
            }

            var creusementPayable = payables.FirstOrDefault(o => o.EstUnCreusement);
            var attenteAvantCreusement = GrandNombre.Max(0, creusement.Cout.Sub(etat.Cycle.ManaCourant)).Div(production);

            Option meilleure = null;
            GrandNombre? meilleurRemboursement = null;
            foreach (var option in payables)
            {
                if (option.EstUnCreusement || option.Gain.Lte(0)) continue;
                var remboursement = option.Cout.Div(option.Gain);
                // Se rembourse-t-elle avant le creusement qu'on attend ?
                if (remboursement.Gte(attenteAvantCreusement)) continue;
                if (meilleurRemboursement == null || remboursement.Lt(meilleurRemboursement.Value))
                {
                    meilleure = option;
                    meilleurRemboursement = remboursement;
                }
            }
            return meilleure ?? creusementPayable;
        }

        /// <summary>Dépense tant qu'un achat fait gagner du temps sur le creusement suivant.</summary>
        private static EtatJeu Depenser(EtatJeu etat)
        {
            var courant = etat;
            for (var garde = 0; garde < 2000; garde += 1)
            {
                var achat = MeilleurAchat(courant, OptionsOuvertes(courant));
                if (achat == null) return courant;
                var suivant = achat.Appliquer(courant);
                if (ReferenceEquals(suivant, courant)) return courant;
                courant = suivant;
            }
            return courant;
        }

        /// <summary>Secondes d'attente avant que l'achat visé devienne payable.</summary>
        private static double? AttenteAvantLeProchainAchat(EtatJeu etat, List<Option> options)
        {
            GrandNombre? cible = null;
            foreach (var option in options)
            {
                if (cible == null || option.Cout.Lt(cible.Value)) cible = option.Cout;
            }
            if (cible == null) return null;
            var production = Economie.ProductionTotaleParSeconde(etat);
            if (production.Lte(0)) return null;
            var manquant = cible.Value.Sub(etat.Cycle.ManaCourant);
            if (manquant.Lte(0)) return 0;
            return manquant.Div(production).ToNumber();
        }

        /// <summary>
        /// Le joueur rentre-t-il dans l'œuf ? Plus de profondeur à prendre dans cette
        /// vie, ET l'acquis de séjour a fait son travail. Aucun minuteur.
        /// </summary>
        private static bool DoitEclore(EtatJeu etat, Politique politique, List<Option> options)
        {
            if (options.Count == 0) return true;
            if (!Economie.EstBloque(etat)) return false;
            return etat.Cycle.AcquisDeSejour >= politique.FractionDeSaturationPourEclore * Constantes.AcquisMax;
        }

        /// <summary>
        /// Appelé après chaque pas et après chaque éclosion : c'est par là que les
        /// invariants du Tier 0 se vérifient sur la durée, et non seulement à l'arrivée.
        /// </summary>
        public static ResultatDeSimulation Simuler(
            int cycles,
            Politique politique = null,
            uint graine = 1,
            Action<EtatJeu> observer = null,
            int? limiteDeContenu = null)
        {
            politique = politique ?? Politique.ParDefaut;
            var etat = Reducteur.EtatInitial(graine, limiteDeContenu ?? Constantes.NombreDePaliers);
            int? cycleNonConvergent = null;
            var acheves = 0;

            var sessions = new List<MesureDeSession>();
            double tempsActif = 0;

            for (var cycle = 0; cycle < cycles; cycle += 1)
            {
                double dureeDuCycle = 0;

                // Le tick peut clore le cycle tout seul : la divergence non choisie du
                // §2.4 vit dans le noyau. Le simulateur doit s'en apercevoir.
                var eclosionsAuDebut = etat.Permanent.NombreEclosions;
                bool ADivergeSeul() => etat.Permanent.NombreEclosions > eclosionsAuDebut;

                for (;;)
                {
                    // ── Le joueur est là ──────────────────────────────────────────
                    double secondesDeSession = 0;
                    etat = Depenser(etat);
                    for (;;)
                    {
                        var options = OptionsOuvertes(etat);
                        if (DoitEclore(etat, politique, options)) break;
                        var attente = AttenteAvantLeProchainAchat(etat, options);
                        if (attente == null || attente.Value > politique.PatienceDansLaSessionSecondes) break;
                        var reste = politique.DureeMaxDeSessionSecondes - secondesDeSession;
                        if (reste <= 0) break;
                        var pas = Math.Min(Math.Max(attente.Value, politique.DtMinSecondes), reste);
                        etat = Depenser(Reducteur.Tick(etat, pas));
                        secondesDeSession += pas;
                        dureeDuCycle += pas;
                        observer?.Invoke(etat);
                    }
                    tempsActif += secondesDeSession;
                    sessions.Add(new MesureDeSession(cycle, secondesDeSession));

                    if (ADivergeSeul()) break;
                    if (DoitEclore(etat, politique, OptionsOuvertes(etat))) break;
                    if (dureeDuCycle >= politique.DureeMaxParCycleSecondes)
                    {
                        cycleNonConvergent = cycle;
                        break;
                    }

                    // ── Il s'en va, et la mare tourne sans lui ────────────────────
                    var absence = Math.Max(politique.IntervalleDeCheckInSecondes - secondesDeSession, politique.DtMinSecondes);
                    etat = Reducteur.Tick(etat, absence);
                    dureeDuCycle += absence;
                    observer?.Invoke(etat);
                    if (ADivergeSeul()) break;
                }

                if (cycleNonConvergent != null) break;
                if (!ADivergeSeul()) etat = Reducteur.Eclore(etat);
                acheves += 1;
                observer?.Invoke(etat);
            }

            return new ResultatDeSimulation
            {
                Etat = etat,
                Releve = Telemetrie.Relever(etat),
                CyclesDemandes = cycles,
                CyclesAcheves = acheves,
                CycleNonConvergent = cycleNonConvergent,
                TempsActifSecondes = tempsActif,
                TempsEcouleSecondes = etat.TempsJeuSecondes,
                Sessions = sessions,
            };
        }
    }
}
