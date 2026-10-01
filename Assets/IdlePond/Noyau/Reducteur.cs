using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    public sealed record ResultatDeTick(EtatJeu Etat, IReadOnlyList<string> Declenches);

    /// <summary>
    /// IdlePond — le noyau. Tick(state, dt) -> state.
    ///
    /// Contrat du §5.1, sans exception :
    ///   - fonction pure : aucune horloge, aucun hasard non semé, aucun accès à
    ///     l'écran, aucune référence au moteur ;
    ///   - le PRNG est à graine et vit dans l'état ;
    ///   - aucun état hors du reducer : pas de variable statique mutable, pas de cache ;
    ///   - le jeu appelle Tick à 100 ms, le simulateur avec dt = 60 s ou 8 h, et
    ///     c'est un seul code.
    ///
    /// Le PRNG n'est jamais tiré sur le chemin continu. C'est une contrainte du
    /// §5.2 et non une commodité : un tirage par tick ferait diverger 480 pas de
    /// 60 s d'un pas de 8 h, et emporterait avec lui le hors ligne et le
    /// simulateur. Le hasard n'a droit de cité que sur des événements discrets
    /// (`Prng.Tirer`).
    ///
    /// Les ré-exports du module TypeScript ne sont pas portés : les appelants vont
    /// chercher `Economie`, `Renaissance` et `Voix` directement.
    /// </summary>
    public static class Reducteur
    {
        /* ─── État initial ──────────────────────────────────────────────────────────*/

        /// <summary>
        /// `limiteDeContenu` : combien de paliers le monde offre réellement.
        ///
        /// Le jeu passe ce qui est livré — l'assise I au jalon v0.2 — et le simulateur
        /// passe les 62 paliers, parce que c'est l'économie complète qu'il doit mesurer.
        /// Un seul reducer, deux mondes : le §12 veut qu'aucune assise ne soit produite
        /// avant que la précédente ait été mesurée, et c'est ce paramètre qui le tient.
        ///
        /// `reglage` null vaut `REGLAGE_CANONIQUE` (une valeur par défaut C# doit être
        /// une constante de compilation).
        /// </summary>
        public static EtatJeu EtatInitial(long graine, int limiteDeContenu = Constantes.NOMBRE_DE_PALIERS, Reglage reglage = null) =>
            new EtatJeu(
                VersionSave: Constantes.VERSION_SAVE,
                // `graine >>> 0` : les 32 bits bas, non signés.
                Prng: new EtatPrng((uint)(graine & 0xFFFFFFFF)),
                TempsJeuSecondes: 0,
                LimiteDeContenu: limiteDeContenu,
                Reglage: reglage ?? Constantes.REGLAGE_CANONIQUE,
                Cycle: Renaissance.CycleInitial(),
                Permanent: new EtatPermanent(
                    Densites: new double[Constantes.NOMBRE_DE_PALIERS],
                    Souffle: new Decimal(0),
                    ContenanceMana: new Decimal(Constantes.CONTENANCE_INITIALE),
                    Couches: Array.Empty<string>(),
                    ProfondeurMaxAtteinte: 0,
                    CompteursTechnique: new Dictionary<BrancheTechnique, double>
                    {
                        [BrancheTechnique.Creusement] = 0,
                        [BrancheTechnique.Amelioration] = 0,
                        [BrancheTechnique.Recrutement] = 0,
                        [BrancheTechnique.Entretien] = 0,
                        [BrancheTechnique.Construction] = 0,
                        [BrancheTechnique.Renaissance] = 0,
                    },
                    NoeudsTechnique: Array.Empty<string>(),
                    Succes: new Dictionary<string, EntreeDeSucces>(),
                    NombreDeRenaissances: 0,
                    EspecesAyantAtteintCent: Array.Empty<string>(),
                    ManaAmbiant: new Decimal(0),
                    HeuresHorsLigneCreditees: 0,
                    Insufflations: new Dictionary<string, int>()),
                Telemetrie: new EtatTelemetrie(
                    Cycles: Array.Empty<MesureDeCycle>(),
                    SecondesDepuisDernierSucces: 0,
                    IntervallesEntreSucces: Array.Empty<double>()));

        /* ─── Le tick ───────────────────────────────────────────────────────────────*/

        /// <summary>
        /// `τ` — le temps caractéristique du séjour, en secondes : `τ₀ × c^profondeur`
        /// (amendement v1.2, §2.B, amendé le 2026-09-16).
        ///
        /// La profondeur est celle ATTEINTE, `ProfondeurMaxAtteinte`, et non celle qui
        /// est ouverte dans la vie courante. Deux raisons, et la seconde est un
        /// invariant :
        ///
        ///   - c'est un acquis de l'être, pas de la plongée : on ne redevient pas jeune
        ///     en remontant, et la contenance ne doit pas se regagner plus vite parce
        ///     qu'on vient de renaître ;
        ///   - `ProfondeurMaxAtteinte` est monotone (Tier 0), donc `τ` l'est aussi. Un
        ///     `τ` qui pourrait redescendre ferait d'une renaissance un moyen
        ///     d'accélérer l'acquis, ce qui rendrait la décision du §6.4 dégénérée.
        ///
        /// Elle ne bouge que sur un ACTE du joueur, jamais pendant un tick : la forme
        /// exponentielle de l'acquis reste donc exacte pour n'importe quel `dt`, et le
        /// §5.2 tient. Un test d'équivalence de pas le garde.
        ///
        /// `τ` change le TEMPS, jamais la valeur : l'acquis sature toujours vers `A∞`.
        /// Descendre ne réduit pas ce qu'on peut porter, cela rallonge le temps qu'il
        /// faut pour le porter.
        /// </summary>
        public static double TauDuSejourSecondes(EtatJeu etat)
        {
            var croissance = etat.Reglage.CroissanceDuSejourParPalier;
            return Constantes.TAU_SEJOUR_HEURES * 3600 * Math.Pow(croissance, etat.Permanent.ProfondeurMaxAtteinte);
        }

        /// <summary>
        /// Avance l'état de `dt` secondes. Un seul pas, pour n'importe quel `dt`.
        ///
        /// Le pas est HOMOGÈNE, et c'est ce que le passage au modèle à niveau a acheté :
        /// la production ne dépend que de niveaux, qui ne changent qu'à l'achat, donc
        /// elle est constante sur tout l'intervalle. Plus rien ne peut tomber au milieu
        /// d'un pas — ni un effectif qui franchit un seuil, ni le drapeau des cent, qui
        /// tombe désormais quand on paie le centième niveau.
        ///
        /// La saturation de la jauge ne coupe pas davantage : à débit constant, le
        /// surplus qui expire vers l'ambiant est le même qu'on le calcule en un pas ou
        /// en quatre cent quatre-vingts. C'est ce qui rend l'équivalence de pas triviale
        /// au lieu de délicate, et pourquoi il n'y a plus de `prochaineCoupure`.
        /// </summary>
        public static ResultatDeTick TickDetaille(EtatJeu etat, double dt)
        {
            // Écrit tel quel, et non `dt <= 0` : c'est ce qui rejette aussi NaN.
            if (!(dt > 0)) return new ResultatDeTick(etat, Array.Empty<string>());

            var production = Economie.ProductionTotaleParSeconde(etat);

            // Acquis de séjour (§2.B) : accumulation saturante vers `A∞`, de temps
            // caractéristique `τ₀` CONSTANT. Forme exponentielle, donc exacte pour
            // n'importe quel `dt` — c'est ce qui permet à la contenance de monter
            // correctement au retour d'une absence de 8 h.
            //
            // La densité n'entre PAS ici. Elle vaut `pointe^α` et croît sans borne : un
            // `τ` divisé par elle tombait à 0,12 h de t₉₀ au deuxième cycle, à 0,05 s au
            // troisième, et les cycles suivants à quelques secondes. L'acquis saturait toujours avant
            // la renaissance, et la contenance dégénérait en forfait. La saturation borne la
            // VALEUR de l'acquis, pas le TEMPS pour l'atteindre. `τ₀` jauge une durée de
            // cycle constante par construction : il doit l'être aussi.
            var acquisDeSejour =
                Constantes.ACQUIS_MAX + (etat.Cycle.AcquisDeSejour - Constantes.ACQUIS_MAX) * Math.Exp(-dt / TauDuSejourSecondes(etat));

            // La contenance limite le stock, pas la production, et elle monte PENDANT le
            // cycle avec l'acquis : le plafond se lit donc à la FIN du pas.
            var brut = etat.Cycle.ManaCourant.Add(production.Mul(dt));
            var plafond = Economie.Contenance(etat with { Cycle = etat.Cycle with { AcquisDeSejour = acquisDeSejour } });
            var manaCourant = Decimal.Min(brut, plafond);
            // Le surplus n'est pas détruit : il expire vers l'ambiant (Tier 0 §5).
            var expire = brut.Sub(manaCourant);

            var avance = etat with
            {
                TempsJeuSecondes = etat.TempsJeuSecondes + dt,
                Cycle = etat.Cycle with
                {
                    ManaCourant = manaCourant,
                    ProductionPicParSeconde = Decimal.Max(etat.Cycle.ProductionPicParSeconde, production),
                    DureeSecondes = etat.Cycle.DureeSecondes + dt,
                    AcquisDeSejour = acquisDeSejour,
                },
                Permanent = etat.Permanent with
                {
                    ManaAmbiant = expire.Gt(0) ? etat.Permanent.ManaAmbiant.Add(expire) : etat.Permanent.ManaAmbiant,
                },
                Telemetrie = etat.Telemetrie with
                {
                    SecondesDepuisDernierSucces = etat.Telemetrie.SecondesDepuisDernierSucces + dt,
                },
            };

            var resultat = RegleDesSucces.VerifierSucces(avance);
            return new ResultatDeTick(resultat.Etat, resultat.Declenches);
        }

        /// Le contrat du §5.1. `TickDetaille` en rend en plus les succès déclenchés.
        public static EtatJeu Tick(EtatJeu etat, double dt) => TickDetaille(etat, dt).Etat;

        /* ─── Actes du joueur ───────────────────────────────────────────────────────
         * Des réducteurs purs, comme le tick. Un acte qui n'est pas payable rend l'état
         * inchangé : c'est au-dessus du noyau de ne pas le proposer.
         */

        /// Creuser le palier suivant. Bloqué doux si son coût dépasse la contenance.
        public static EtatJeu Creuser(EtatJeu etat)
        {
            if (Economie.ToutEstCreuse(etat)) return etat;
            var cible = etat.Cycle.PaliersOuverts;
            var cout = Economie.CoutDeDescente(etat, cible);
            if (cout.Gt(Economie.Contenance(etat))) return etat;
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    PaliersOuverts = cible + 1,
                },
                Permanent = etat.Permanent with
                {
                    ProfondeurMaxAtteinte = Math.Max(etat.Permanent.ProfondeurMaxAtteinte, cible + 1),
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Creusement, cout.ToNumber()),
                },
            };
        }

        /// <summary>
        /// La table des espèces vivantes avec `especeId` remplacée. Reconstruite dans
        /// l'ordre du registre : l'ordre d'énumération d'un dictionnaire C# n'a pas à
        /// dépendre de l'ordre des achats.
        /// </summary>
        static IReadOnlyDictionary<string, EtatEspece> AvecEspece(
            IReadOnlyDictionary<string, EtatEspece> especes, string especeId, EtatEspece valeur)
        {
            var table = new Dictionary<string, EtatEspece>();
            foreach (var espece in Especes.Toutes)
            {
                if (espece.Id == especeId) table[espece.Id] = valeur;
                else if (especes.TryGetValue(espece.Id, out var vivante)) table[espece.Id] = vivante;
            }
            return table;
        }

        /// Débloquer une espèce. Une fois par espèce et par vie ; elle démarre au niveau 1.
        public static EtatJeu Debloquer(EtatJeu etat, string especeId)
        {
            var espece = Especes.ParId(especeId);
            if (espece == null) return etat;
            if (espece.Palier >= etat.Cycle.PaliersOuverts) return etat;
            if (etat.Cycle.Especes.TryGetValue(especeId, out var deja) && deja.Debloquee) return etat;
            var cout = Economie.CoutDeDeblocage(etat, espece);
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    Especes = AvecEspece(etat.Cycle.Especes, especeId, new EtatEspece(true, 1)),
                },
                Permanent = etat.Permanent with
                {
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Recrutement, 1),
                },
            };
        }

        /// <summary>
        /// Monter une espèce d'un niveau. L'achat répétable de la boucle, ×1.15.
        ///
        /// Le drapeau des cent tombe ICI, à l'achat du centième niveau, et plus au
        /// milieu d'un pas de tick : il n'y a plus de population qui le franchit toute
        /// seule. C'est ce qui rend le pas homogène.
        /// </summary>
        public static EtatJeu Ameliorer(EtatJeu etat, string especeId)
        {
            var espece = Especes.ParId(especeId);
            if (espece == null) return etat;
            if (!etat.Cycle.Especes.TryGetValue(especeId, out var avant) || !avant.Debloquee) return etat;
            var cout = Economie.CoutDeNiveau(etat, espece, avant.Niveau);
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            var niveau = avant.Niveau + 1;
            var atteintCent =
                niveau >= Constantes.SEUIL_DU_DRAPEAU_PERMANENT &&
                !etat.Permanent.EspecesAyantAtteintCent.Contains(especeId);
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    Especes = AvecEspece(etat.Cycle.Especes, especeId, new EtatEspece(true, niveau)),
                },
                Permanent = etat.Permanent with
                {
                    // Reconstruite dans l'ordre du registre, jamais dans l'ordre des achats :
                    // deux parties qui achètent les mêmes niveaux dans un ordre différent ne
                    // doivent pas se sérialiser différemment.
                    EspecesAyantAtteintCent = atteintCent
                        ? Especes.Toutes
                            .Where(e => e.Id == especeId || etat.Permanent.EspecesAyantAtteintCent.Contains(e.Id))
                            .Select(e => e.Id)
                            .ToArray()
                        : etat.Permanent.EspecesAyantAtteintCent,
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Amelioration, cout.ToNumber()),
                },
            };
        }

        /// <summary>
        /// Faire grandir le héros d'un niveau — le quatrième achat, spec 2026-09-17.
        ///
        /// Même forme que les trois autres : payable ou rien ne change. Le compteur
        /// crédité est celui d'Amélioration : c'est du mana dépensé en niveaux, et
        /// l'arbre n'a pas de branche « héros ». Le niveau agit à l'instant où il est
        /// payé, jamais pendant un pas.
        /// </summary>
        public static EtatJeu Grandir(EtatJeu etat)
        {
            var cout = Economie.CoutDeCroissance(etat, etat.Cycle.NiveauDuHeros);
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    NiveauDuHeros = etat.Cycle.NiveauDuHeros + 1,
                },
                Permanent = etat.Permanent with
                {
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Amelioration, cout.ToNumber()),
                },
            };
        }

        /// <summary>
        /// Insuffler — noyau v1.0 §4. Payé en SOUFFLE, permanent, et le seul débouché du
        /// Souffle tant que les miracles sont gelés ([P26]).
        ///
        /// La table est reconstruite dans l'ordre du registre, jamais dans l'ordre des
        /// achats — même raison que `EspecesAyantAtteintCent` : l'ordre d'énumération
        /// d'un dictionnaire neuf est celui de ses insertions, et le test de
        /// déterminisme compare la chaîne de save.
        /// </summary>
        public static EtatJeu Insuffler(EtatJeu etat, string id)
        {
            var insufflation = Insufflations.ParId(id);
            if (insufflation == null) return etat;
            var cout = Economie.CoutDInsufflation(etat, insufflation);
            if (etat.Permanent.Souffle.Lt(cout)) return etat;
            var rangs = new Dictionary<string, int>();
            foreach (var paire in etat.Permanent.Insufflations) rangs[paire.Key] = paire.Value;
            rangs[id] = (etat.Permanent.Insufflations.TryGetValue(id, out var actuel) ? actuel : 0) + 1;
            var insufflations = new Dictionary<string, int>();
            foreach (var i in Insufflations.Toutes)
                if (rangs.TryGetValue(i.Id, out var rang) && rang > 0) insufflations[i.Id] = rang;
            return etat with
            {
                Permanent = etat.Permanent with
                {
                    Souffle = etat.Permanent.Souffle.Sub(cout),
                    Insufflations = insufflations,
                },
            };
        }
    }
}
