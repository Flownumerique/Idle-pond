using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Simulateur
{
    /// <summary>
    /// IdlePond — simulateur.
    ///
    /// Réutilise `IdlePond.Noyau` tel quel. C'est tout l'intérêt du contrat du §5.1 : le
    /// simulateur n'a pas de moteur à lui, il appelle le même `Tick` que le jeu avec
    /// un `dt` plus grand. Une divergence entre les deux serait une divergence entre
    /// ce qui est mesuré et ce qui est joué.
    ///
    /// Le simulateur porte les POLITIQUES — ce que le joueur fait et quand il
    /// revient. Le noyau ne décide jamais à sa place : aucune décision de joueur ne
    /// vit dans le reducer.
    ///
    /// §13.4, à garder en tête en lisant toute sortie d'ici : l'économie est
    /// invariante d'échelle. Chaque cycle est le même problème économique à une plus
    /// grande échelle, et le réglage de paramètres ne peut donc pas produire de
    /// croissance de cycle en temps ACTIF. Seul l'intervalle entre deux relevés
    /// produit une croissance apparente en temps calendaire (RESULTATS.md,
    /// finding 2). Toute cible exprimée en heures actives par cycle sera rejetée
    /// ici.
    ///
    /// Champs :
    ///   SecondesEntreReleves — intervalle entre deux relevés du joueur, en secondes.
    ///     0 = achat continu : le joueur optimal du finding 2 (RESULTATS.md), présent
    ///     à chaque pas. 4 h = un relevé toutes les quatre heures : le joueur relâché,
    ///     qui ne joue pas plus mal, mais moins souvent. C'est LE réglage de temps
    ///     calendaire du jeu, et le seul. Entre deux relevés le mana s'accumule,
    ///     plafonne à la contenance, et le surplus expire vers l'ambiant : revenir
    ///     moins souvent coûte donc quelque chose.
    ///   Pas — pas d'intégration entre deux décisions, en secondes. En achat continu,
    ///     c'est l'intervalle entre deux relevés ; sinon, la granularité de l'absence.
    ///     C'est aussi ce que dure un relevé : le temps que le joueur passe devant
    ///     l'écran à chaque retour.
    ///   FractionDeSaturationPourRenaitre — part de `A∞` au-delà de laquelle rester ne
    ///     rapporte plus de profondeur. C'est la forme opérationnelle de la seule
    ///     vraie décision du joueur (§6.4). L'acquis de séjour sature ; passé ce
    ///     point, une heure de plus dans la même vie n'achète que du Souffle, alors
    ///     qu'une renaissance achète de la profondeur. Le joueur optimal part. Un
    ///     minuteur de patience, à sa place, ne mesurerait que l'impatience du
    ///     simulateur.
    ///   DureeMaxParCycleSecondes — garde-fou : un cycle qui dépasse cette durée est
    ///     déclaré non convergent, et la simulation s'arrête là. Le calibrage balaie
    ///     des réglages dont certains ne convergent pas : sans lui, il bouclerait.
    /// </summary>
    public sealed record Politique(
        double SecondesEntreReleves,
        double Pas,
        double FractionDeSaturationPourRenaitre,
        double DureeMaxParCycleSecondes);

    /// Les quatre achats du noyau v1.0, chacun avec son coût et la production qu'il ajoute.
    public enum TypeDAchat { Creuser, Grandir, Debloquer, Niveau }

    /// `Espece` est null pour `Creuser` et `Grandir`, qui ne portent aucune espèce.
    public sealed record Achat(TypeDAchat Type, Espece Espece, Decimal Cout, Decimal Gain);

    /// <summary>
    /// Ce que rend une simulation.
    ///
    /// `Cycles` : les cycles clos, dans l'ordre — la télémétrie du noyau, telle quelle.
    /// `SecondesActives` : somme des relevés — le temps passé devant l'écran. Un
    ///   relevé dure un pas ; en achat continu les relevés se touchent et l'actif
    ///   égale l'écoulé : ce sont les ~38 h du finding 2, la quantité de jeu que le
    ///   contenu porte.
    /// `SecondesEcoulees` : temps de jeu total, plafonnements de contenance inclus —
    ///   ce que vit le joueur au calendrier. Les ~25 jours du finding 2, à deux
    ///   relevés par jour.
    /// </summary>
    public sealed record ResultatDeSimulation(
        EtatJeu Etat,
        Releve Releve,
        IReadOnlyList<MesureDeCycle> Cycles,
        int CyclesDemandes,
        int CyclesAcheves,
        int? CycleNonConvergent,
        double SecondesActives,
        double SecondesEcoulees);

    public static class Simulateur
    {
        public static readonly Politique POLITIQUE_PAR_DEFAUT = new Politique(
            SecondesEntreReleves: 0,
            Pas: 60,
            FractionDeSaturationPourRenaitre: 0.95,
            DureeMaxParCycleSecondes: 4000 * 3600);

        /// <summary>
        /// Les achats ouverts, avec leur gain marginal — la production par seconde
        /// qu'ils ajoutent à l'instant où ils sont payés.
        ///
        /// La production est une assiette (espèces et héros) multipliée par des
        /// facteurs globaux, et ces facteurs viennent de `MultiplicateursGlobaux`, la
        /// source que partagent la production, la production par espèce et le terme du
        /// héros. Ils ne sont PAS relistés ici : une liste recopiée à la main dans le
        /// simulateur avait laissé la densité hors du gain marginal et biaisé toute
        /// mesure en silence (revue de qualité de la tâche 9). Un facteur ajouté là-bas
        /// vaut ici sans resaisie.
        ///
        ///   débloquer : l'espèce entre au niveau 1, soit `débit × seuil(1) × globaux` ;
        ///   niveau    : `débit × Δ(n × seuil(n)) × globaux`, et si ce niveau pose le
        ///               drapeau des cent — mêmes conditions qu'`Ameliorer` —, le
        ///               multiplicateur des drapeaux passe de `m` à `m + 0,03` : toute
        ///               la production, ce niveau compris, gagne `0,03 / m` ;
        ///   creuser   : le palier ouvert change les facteurs globaux — la profondeur,
        ///               et la densité du séjour si ce palier en porte plus —, donc la
        ///               production entière est multipliée par leur rapport. Ce rapport
        ///               se lit sur `MultiplicateursGlobaux` d'un état où ce palier est
        ///               ouvert, et non sur `m_p` seul, qui ignorerait la densité.
        ///
        /// `budget`, s'il est donné, retire de la liste les achats qu'il ne paie pas —
        /// `cout > budget`, le test exact que la politique appliquerait ensuite — et
        /// leur gain n'est alors pas calculé. C'est le seul endroit où ce test est
        /// écrit : `MeilleurAchat` lui passe le mana courant et ne le refait pas. Sans
        /// budget, la liste est complète — c'est sous cette forme que le test du gain
        /// marginal la confronte au noyau. Un relevé évalue jusqu'à toutes les espèces
        /// pour n'en payer qu'une, et le gain vaut à lui seul la moitié du prix d'une
        /// évaluation.
        ///
        /// Creuser n'est proposé que s'il ne bloque pas (`EstBloque`, qui lit
        /// `Contenance`) : au-delà, il est hors de portée pour toujours, puisque le stock
        /// ne peut pas monter jusque-là. Ce gain ne compte pas l'accès aux espèces que
        /// le palier ouvre ; c'est la règle du harnais de RESULTATS.md, et creuser y
        /// entre dans la comparaison sans traitement de faveur.
        /// </summary>
        public static IReadOnlyList<Achat> AchatsDisponibles(EtatJeu etat, Decimal? budget = null)
        {
            var achats = new List<Achat>();
            bool HorsDePortee(Decimal cout) => budget.HasValue && cout.Gt(budget.Value);

            // La production totale et les multiplicateurs globaux coûtent à eux seuls
            // plus que tout le reste de cette fonction — la première parcourt les
            // espèces, et chacune redemande les seconds. Ils ne sont tirés qu'à la
            // première DEMANDE : sous un budget serré, aucun candidat n'y arrive, et un
            // relevé qui ne peut rien payer ne paie plus pour le savoir.
            Decimal? production = null;
            Decimal ProductionTotale() { production ??= Economie.ProductionTotaleParSeconde(etat); return production.Value; }
            Decimal? globaux = null;
            Decimal Multiplicateurs() { globaux ??= Economie.MultiplicateursGlobaux(etat); return globaux.Value; }

            if (!Economie.EstBloque(etat))
            {
                var cible = etat.Cycle.PaliersOuverts;
                var cout = Economie.CoutDeDescente(etat, cible);
                if (!HorsDePortee(cout))
                {
                    var ouvert = etat with { Cycle = etat.Cycle with { PaliersOuverts = cible + 1 } };
                    achats.Add(new Achat(
                        TypeDAchat.Creuser, null, cout,
                        ProductionTotale().Mul(Economie.MultiplicateursGlobaux(ouvert).Div(Multiplicateurs()).Sub(1))));
                }
            }

            // Grandir — spec 2026-09-17 [D2]. Après l'achat, TOUTE la production est
            // multipliée par (1 + b), et le débit propre du héros passe de n à n + 1 :
            //   P' = (S + D·(n+1)) · M · (1 + b)  avec  P = (S + D·n) · M
            //   P' − P = P·b + D·M·(1 + b)
            // où M est `MultiplicateursGlobaux` de l'état courant (héros compris).
            {
                var cout = Economie.CoutDeCroissance(etat, etat.Cycle.NiveauDuHeros);
                if (!HorsDePortee(cout))
                {
                    var propre = new Decimal(Constantes.DEBIT_HEROS).Mul(Multiplicateurs()).Mul(1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS);
                    achats.Add(new Achat(
                        TypeDAchat.Grandir, null, cout,
                        ProductionTotale().Mul(Constantes.BONUS_PAR_NIVEAU_DU_HEROS).Add(propre)));
                }
            }

            foreach (var espece in Especes.Toutes)
            {
                if (espece.Palier >= etat.Cycle.PaliersOuverts) continue;
                var vivante = etat.Cycle.Especes.TryGetValue(espece.Id, out var v) ? v : null;
                if (vivante == null || !vivante.Debloquee)
                {
                    var cout = Economie.CoutDeDeblocage(etat, espece);
                    if (HorsDePortee(cout)) continue;
                    achats.Add(new Achat(
                        TypeDAchat.Debloquer, espece, cout,
                        Economie.DebitInsuffle(etat, espece).Mul(Multiplicateurs()).Mul(Economie.MultiplicateurDeSeuil(1)).Mul(Economie.MultiplicateurDInsufflation(etat, espece))));
                    continue;
                }
                var n = vivante.Niveau;
                var coutNiveau = Economie.CoutDeNiveau(etat, espece, n);
                if (HorsDePortee(coutNiveau)) continue;
                var propreNiveau = Economie.DebitInsuffle(etat, espece)
                    .Mul(Multiplicateurs())
                    .Mul((n + 1) * Economie.MultiplicateurDeSeuil(n + 1) - n * Economie.MultiplicateurDeSeuil(n))
                    .Mul(Economie.MultiplicateurDInsufflation(etat, espece));
                var poseLeDrapeau =
                    n + 1 >= Constantes.SEUIL_DU_DRAPEAU_PERMANENT && !etat.Permanent.EspecesAyantAtteintCent.Contains(espece.Id);
                var gainNiveau = poseLeDrapeau
                    ? propreNiveau.Add(ProductionTotale().Add(propreNiveau).Mul(Constantes.BONUS_GLOBAL_A_CENT_INDIVIDUS / Economie.MultiplicateurDesDrapeaux(etat)))
                    : propreNiveau;
                achats.Add(new Achat(TypeDAchat.Niveau, espece, coutNiveau, gainNiveau));
            }

            return achats;
        }

        /// <summary>
        /// Parmi les achats payables, celui qui se rembourse le plus vite — `coût / gain`
        /// le plus bas. C'est la règle du harnais qui a produit RESULTATS.md : le joueur
        /// optimal ne met rien de côté, il achète le meilleur rapport dès qu'il le peut.
        /// </summary>
        static Achat MeilleurAchat(EtatJeu etat)
        {
            Achat meilleur = null;
            Decimal? meilleurRetour = null;
            // Le mana EST le budget : `AchatsDisponibles` ne rend déjà que ce qu'il paie.
            // Le test de portée n'est écrit qu'une fois, et il est écrit là-bas.
            foreach (var achat in AchatsDisponibles(etat, etat.Cycle.ManaCourant))
            {
                if (achat.Gain.Lte(0)) continue;
                var retour = achat.Cout.Div(achat.Gain);
                if (meilleurRetour == null || retour.Lt(meilleurRetour.Value))
                {
                    meilleur = achat;
                    meilleurRetour = retour;
                }
            }
            return meilleur;
        }

        static EtatJeu Appliquer(EtatJeu etat, Achat achat)
        {
            switch (achat.Type)
            {
                case TypeDAchat.Creuser: return Reducteur.Creuser(etat);
                case TypeDAchat.Grandir: return Reducteur.Grandir(etat);
                case TypeDAchat.Debloquer: return Reducteur.Debloquer(etat, achat.Espece.Id);
                default: return Reducteur.Ameliorer(etat, achat.Espece.Id);
            }
        }

        /// <summary>
        /// Un relevé : le joueur dépense tant qu'un achat est payable.
        ///
        /// La boucle termine d'elle-même — chaque achat coûte, et le coût d'un niveau
        /// croît de ×1,15. La borne n'est là que contre un défaut du noyau, et elle
        /// CRIE : un relevé tronqué en silence fausserait toute mesure sans le dire.
        /// </summary>
        static EtatJeu Depenser(EtatJeu etat)
        {
            var courant = etat;
            for (var achats = 0; ; achats += 1)
            {
                if (achats > 100_000)
                    throw new InvalidOperationException("Un relevé ne finit pas de dépenser : le noyau refuse-t-il un achat payable ?");
                var achat = MeilleurAchat(courant);
                if (achat == null) return courant;
                var suivant = Appliquer(courant, achat);
                // Même symptôme que la borne ci-dessus, donc même traitement : la politique
                // a cru payable un achat que le noyau refuse. Inatteignable aujourd'hui —
                // les conditions d'`AchatsDisponibles` couvrent les trois refus du noyau —,
                // et c'est justement pourquoi le fermer ne coûte rien : c'était le dernier
                // chemin par lequel une divergence politique/noyau passerait sans un mot.
                if (ReferenceEquals(suivant, courant))
                    throw new InvalidOperationException($"Le noyau refuse un achat que la politique croyait payable : {achat.Type}");
                courant = suivant;
            }
        }

        /// <summary>
        /// Le joueur rentre-t-il dans l'œuf ?
        ///
        /// Deux conditions, et aucun minuteur : il n'y a plus de profondeur à prendre
        /// dans cette vie, ET l'acquis de séjour a fait son travail. Rester au-delà
        /// n'achète plus que du Souffle — c'est exactement l'arbitrage du §6.4, et c'est
        /// le §2.B qui le rend réel en faisant saturer l'acquis.
        ///
        /// Une troisième condition a été RETIRÉE le 2026-09-09 : « plus aucune dépense
        /// ouverte ⇒ renaître ». Elle était un terminateur sûr tant qu'une population
        /// mettait des heures à rejoindre sa place ; depuis que le niveau agit à
        /// l'instant où il est payé, elle tombe au bout de quelques minutes, et faisait
        /// partir le joueur avant que la contenance ait rien gagné. Ne plus avoir quoi
        /// acheter n'est pas une raison de partir — c'est exactement le moment où
        /// rester ne rapporte plus que du Souffle et de la contenance, donc le moment
        /// que le §2.B veut voir arriver.
        ///
        /// Exportée (tâche 11) : c'est la seule vraie décision du jeu, et elle ne doit
        /// vivre qu'ICI. Une partie headless qui écrirait sa propre règle de renaissance —
        /// même équivalente en apparence — dériverait en silence le jour où l'une des
        /// deux bouge sans l'autre (c'est la leçon de la tâche 9 sur les listes
        /// recopiées à la main). Une réécriture ultérieure du simulateur doit
        /// préserver cet export.
        /// </summary>
        public static bool DoitRenaitre(EtatJeu etat, Politique politique)
        {
            if (!Economie.EstBloque(etat)) return false;
            return etat.Cycle.AcquisDeSejour >= politique.FractionDeSaturationPourRenaitre * Constantes.ACQUIS_MAX;
        }

        /// <summary>
        /// Ce que le joueur fait de son Souffle : il insuffle, la moins chère d'abord, tant
        /// qu'il peut payer. Une politique, pas une règle du noyau — la globale et les
        /// ciblées ont chacune leur échelle de prix, et le simulateur n'a pas à savoir
        /// laquelle rapporte le plus dans une vie qui n'a pas encore commencé.
        ///
        /// Appelée juste après `Renaitre` : c'est là que le Souffle est crédité. Elle
        /// termine d'elle-même — chaque rang multiplie le prix par le ratio.
        /// </summary>
        public static EtatJeu InsufflerAuMieux(EtatJeu etat)
        {
            var courant = etat;
            for (var garde = 0; garde < 10_000; garde += 1)
            {
                (string Id, Decimal Cout)? choix = null;
                foreach (var insufflation in Insufflations.Toutes)
                {
                    var cout = Economie.CoutDInsufflation(courant, insufflation);
                    if (cout.Gt(courant.Permanent.Souffle)) continue;
                    if (choix == null || cout.Lt(choix.Value.Cout)) choix = (insufflation.Id, cout);
                }
                if (choix == null) return courant;
                var suivant = Reducteur.Insuffler(courant, choix.Value.Id);
                if (ReferenceEquals(suivant, courant))
                    throw new InvalidOperationException($"Le noyau refuse une insufflation que la politique croyait payable : {choix.Value.Id}");
                courant = suivant;
            }
            throw new InvalidOperationException("La politique d'insufflation ne termine pas");
        }

        // `reglage` : le réglage de la courbe, pour le calibreur — il balaie des
        // valeurs, et le §5.1 lui interdit de muter un module pour le faire. Par
        // défaut : le canon. La tâche 13 y ajoutera `θ` et l'échelle.
        public static ResultatDeSimulation Simuler(
            int cycles,
            Politique politique = null,
            long graine = 1,
            Action<EtatJeu> observer = null,
            int? limiteDeContenu = null,
            Reglage reglage = null)
        {
            politique ??= POLITIQUE_PAR_DEFAUT;
            if (!(politique.Pas > 0))
                throw new InvalidOperationException($"Le pas de la politique doit être positif (reçu {politique.Pas})");
            if (!(politique.SecondesEntreReleves >= 0))
                throw new InvalidOperationException($"L'intervalle entre relevés ne peut pas être négatif (reçu {politique.SecondesEntreReleves})");
            var intervalle = politique.SecondesEntreReleves > 0 ? politique.SecondesEntreReleves : politique.Pas;

            var etat = Reducteur.EtatInitial(graine, limiteDeContenu ?? Constantes.NOMBRE_DE_PALIERS, reglage);
            double secondesActives = 0;
            int? cycleNonConvergent = null;
            var acheves = 0;

            for (var cycle = 0; cycle < cycles; cycle += 1)
            {
                while (true)
                {
                    // ── Un relevé : le joueur est là ────────────────────────────────────
                    etat = Depenser(etat);
                    observer?.Invoke(etat);
                    if (DoitRenaitre(etat, politique)) break;
                    if (etat.Cycle.DureeSecondes >= politique.DureeMaxParCycleSecondes)
                    {
                        cycleNonConvergent = cycle;
                        break;
                    }

                    // ── Jusqu'au relevé suivant ──────────────────────────────────────────
                    // Le premier pas se passe devant l'écran ; le reste de l'intervalle, la
                    // mare tourne sans lui — le mana plafonne à la contenance et le surplus
                    // expire vers l'ambiant. En achat continu, l'intervalle EST ce premier pas.
                    // Un relevé qui fait renaître se prolonge dans le premier relevé du cycle
                    // suivant, au même instant : c'est une seule présence, comptée une fois.
                    // `reste > 1e-9`, et non `> 0` : un intervalle qui n'est pas un multiple
                    // du pas laisse un résidu flottant, et un tick de 1e-14 s n'est pas un
                    // pas de simulation. La tâche 13 balaiera des politiques.
                    var present = true;
                    for (var reste = intervalle; reste > 1e-9; present = false)
                    {
                        var dt = Math.Min(politique.Pas, reste);
                        etat = Reducteur.Tick(etat, dt);
                        if (present) secondesActives += dt;
                        reste -= dt;
                        observer?.Invoke(etat);
                    }
                }

                if (cycleNonConvergent != null) break;
                etat = InsufflerAuMieux(Renaissance.Renaitre(etat));
                acheves += 1;
                observer?.Invoke(etat);
            }

            return new ResultatDeSimulation(
                Etat: etat,
                Releve: Telemetrie.Relever(etat),
                Cycles: etat.Telemetrie.Cycles,
                CyclesDemandes: cycles,
                CyclesAcheves: acheves,
                CycleNonConvergent: cycleNonConvergent,
                SecondesActives: secondesActives,
                SecondesEcoulees: etat.TempsJeuSecondes);
        }
    }
}
