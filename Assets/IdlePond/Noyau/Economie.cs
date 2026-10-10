using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — production, coûts, seuils.
    ///
    /// UN SEUL CANAL DE REVENU — noyau v1.0 §10 : les espèces. Et une espèce est un
    /// générateur avec un NIVEAU (§1.3) : on la débloque une fois, on monte son
    /// niveau, l'effet est immédiat. Plus aucune population n'est simulée, donc plus
    /// rien ne varie à l'intérieur d'un pas de tick.
    ///
    /// Les multiplicateurs qui s'ajoutent au canal sont des TermeDeFormule nommés,
    /// jamais des facteurs anonymes : c'est ce qui rend le détail de captation
    /// auditable (§7.5 règle 3, §8.2).
    /// </summary>
    public static class Economie
    {
        /* ─── Seuils de jalon ───────────────────────────────────────────────────────*/

        /// <summary>
        /// Multiplicateur de seuil d'une espèce, d'après son NIVEAU (§2.C).
        ///
        /// La table donne le multiplicateur CUMULÉ lu au seuil : on retient celui du
        /// seuil le plus haut franchi, on ne multiplie pas les colonnes entre elles.
        /// Le niveau cent vaut ×16, jamais ×1024 — et `D = 2.31` a été calibré contre
        /// cette lecture-là.
        ///
        /// Il se lit sur le niveau courant, donc il tombe à l'ACHAT et se reperd à
        /// la renaissance avec le niveau. Le seul acquis qui survit est le drapeau
        /// permanent, plus bas.
        /// </summary>
        public static double MultiplicateurDeSeuil(int niveau)
        {
            var multiplicateur = 1.0;
            foreach (var palier in Constantes.SEUILS_DE_JALON)
                if (niveau >= palier.Seuil) multiplicateur = palier.MultiplicateurCumule;
            return multiplicateur;
        }

        /// <summary>
        /// Bonus global des espèces ayant DÉJÀ atteint le niveau cent (§2.C).
        /// Définitif, conservé à la renaissance, additif entre espèces.
        /// </summary>
        public static double MultiplicateurDesDrapeaux(EtatJeu etat) =>
            1 + Constantes.BONUS_GLOBAL_A_CENT_INDIVIDUS * etat.Permanent.EspecesAyantAtteintCent.Count;

        /* ─── Production ────────────────────────────────────────────────────────────*/

        /// <summary>
        /// Débit de base d'une espèce, mana/s par niveau.
        ///
        /// « Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
        /// précédentes : elle double donc l'assiette additive à niveaux égaux. » Le
        /// reste de `D` est porté par le multiplicateur de profondeur, plus bas — le
        /// bestiaire seul ne le porte pas, puisque deux paliers sur trois n'apportent
        /// aucune espèce.
        /// </summary>
        public static Decimal DebitBaseDeLEspece(Espece espece) => Echelles.DebitBaseDuRang(espece.Rang);

        /* ─── Améliorations — noyau v1.0 §4.2 ───────────────────────────────────────*/

        public static int RangDAmelioration(EtatJeu etat, string id) =>
            etat.Permanent.AmeliorationsDeRenaissance.TryGetValue(id, out var rang) ? rang : 0;

        /// <summary>
        /// Le débit de base d'une espèce, augmenté de l'amélioration GLOBALE : additif,
        /// « sur le débit de base de toutes les espèces, présentes et futures ». Il
        /// domine quand les débits sont minuscules et s'efface une fois les
        /// multiplicateurs décollés — aucun ratio à régler.
        ///
        /// Le coût d'un niveau ne le lit PAS : il lit `DebitBaseDeLEspece`. Améliorer ne
        /// renchérit rien.
        /// </summary>
        public static Decimal DebitAmeliore(EtatJeu etat, Espece espece)
        {
            var rang = RangDAmelioration(etat, AmeliorationsDeRenaissance.GLOBALE_ID);
            if (rang == 0) return DebitBaseDeLEspece(espece);
            return DebitBaseDeLEspece(espece).Add(Constantes.AMELIORATION_GLOBALE_PAR_RANG * rang);
        }

        /// L'amélioration CIBLÉE de l'espèce : `(1 + c) ^ rang`, empilable, 1 à rang 0.
        public static double MultiplicateurDAmelioration(EtatJeu etat, Espece espece) =>
            Math.Pow(1 + Constantes.AMELIORATION_CIBLEE_PAR_RANG, RangDAmelioration(etat, AmeliorationsDeRenaissance.CibleeDe(espece.Id).Id));

        /// <summary>
        /// Multiplicateur global accordé par la profondeur ouverte.
        ///
        /// Il porte la part de `D` que le bestiaire ne porte pas : sans lui, la
        /// production croîtrait de 26 % par palier là où le coût croît de 140 %, et
        /// l'écart se composerait jusqu'à rendre les cycles profonds interminables.
        /// </summary>
        public static Decimal MultiplicateurDeProfondeur(EtatJeu etat) =>
            Echelles.PuissanceDuMultiplicateurDePalier(Math.Max(0, etat.Cycle.PaliersOuverts - 1));

        /// <summary>
        /// Multiplicateur global du niveau du héros — spec 2026-09-17 [D2].
        ///
        /// `(1 + b) ^ (niveau − 1)` : au niveau 1 il vaut exactement 1, et un cycle
        /// dont le héros n'a jamais grandi produit ce qu'il produisait avant ce terme.
        /// Sa part de `D` est retirée au multiplicateur de profondeur, pas ajoutée
        /// par-dessus : voir `MultiplicateurDePalier` dans `Constantes.cs`.
        /// </summary>
        public static double MultiplicateurDuHeros(EtatJeu etat) =>
            Math.Pow(1 + Constantes.BONUS_PAR_NIVEAU_DU_HEROS, Math.Max(0, etat.Cycle.NiveauDuHeros - 1));

        /// <summary>
        /// Tous les multiplicateurs globaux de la production, un seul produit — la
        /// source commune à chaque espèce, au total, ET à la politique du simulateur
        /// (`Simulateur`). Un multiplicateur ajouté ici vaut pour les trois sans
        /// resaisie : une liste recopiée à la main, plutôt que prise ici, est
        /// exactement ce qui avait laissé la densité hors du calcul du gain simulé
        /// (revue de qualité de la tâche 9, finding 2).
        ///
        /// `ECHELLE_DE_PRODUCTION` y entre aussi, malgré son statut de simple cadran :
        /// elle doit multiplier TOUT ce qui produit, pas seulement le total, sous
        /// peine de refaire diverger la somme par espèce du total dès qu'elle
        /// bougera (revue de qualité de la tâche 9, minor A).
        /// </summary>
        public static Decimal MultiplicateursGlobaux(EtatJeu etat) =>
            MultiplicateurDeProfondeur(etat)
                .Mul(Densite.Multiplicateur(Densite.DuSejour(etat)))
                .Mul(MultiplicateurDuHeros(etat))
                .Mul(MultiplicateurDesDrapeaux(etat))
                .Mul(Constantes.ECHELLE_DE_PRODUCTION);

        /// Ce qu'une espèce apporte à l'assiette additive, avant les multiplicateurs globaux.
        static Decimal DebitDeLEspece(EtatJeu etat, Espece espece)
        {
            if (!etat.Cycle.Especes.TryGetValue(espece.Id, out var vivante) || !vivante.Debloquee || vivante.Niveau == 0)
                return new Decimal(0);
            return DebitAmeliore(etat, espece)
                .Mul(vivante.Niveau)
                .Mul(MultiplicateurDeSeuil(vivante.Niveau))
                .Mul(MultiplicateurDAmelioration(etat, espece))
                .Fois(MultiplicateurDeLieu(etat, espece));
        }

        /// Ce qu'une espèce donne réellement par seconde, tous termes nommés appliqués.
        public static Decimal ProductionDeLEspece(EtatJeu etat, Espece espece)
        {
            if (espece.Palier >= etat.Cycle.PaliersOuverts) return new Decimal(0);
            return DebitDeLEspece(etat, espece).Mul(MultiplicateursGlobaux(etat));
        }

        /// <summary>
        /// Ce que le débit du héros apporte RÉELLEMENT à la production, multiplicateurs
        /// globaux compris — §7.5 règle 3 : même un débit qui n'appartient à aucune
        /// espèce doit cibler un `TermeDeFormule`, jamais flotter hors du registre.
        /// `DEBIT_HEROS` brut (voir `DetailDuHeros` plus bas) ne suffit pas à expliquer
        /// l'écart entre le total affiché et la somme des espèces à l'écran — c'est
        /// cette valeur, multipliée, qui le fait.
        /// </summary>
        public static Decimal ProductionDuHeros(EtatJeu etat) =>
            new Decimal(Constantes.DEBIT_HEROS).Mul(etat.Cycle.NiveauDuHeros).Mul(MultiplicateursGlobaux(etat));

        /// <summary>
        /// La somme des espèces débloquées, PLUS le débit propre du héros — noyau v1.0
        /// §10 : un seul canal pour le bestiaire, et sa mutation à lui pour empêcher
        /// l'état DÉGÉNÉRÉ où plus rien ne produirait jamais (RESULTATS.md, finding 3,
        /// tâche 9). Le premier achat, lui, est tenu par la charge de départ
        /// (`MANA_A_LA_SORTIE_DE_L_OEUF`, voir son commentaire dans `Constantes.cs`) —
        /// les deux mécanismes répondent à des besoins différents et ne se remplacent
        /// pas l'un l'autre.
        ///
        /// Construite à partir de `ProductionDeLEspece` et `ProductionDuHeros`, pas
        /// d'un second calcul de l'assiette : deux formules tenues manuellement en
        /// synchronisation sont exactement ce qui avait fait nourrir le multiplicateur
        /// de densité de deux grandeurs différentes — une somme sur les paliers d'un
        /// côté, un maximum de l'autre. Un seul
        /// calcul, appelé une fois par espèce plus une fois pour le héros, ne peut plus
        /// diverger de lui-même.
        /// </summary>
        public static Decimal ProductionTotaleParSeconde(EtatJeu etat)
        {
            var especes = new Decimal(0);
            foreach (var espece in Especes.Toutes) especes = especes.Add(ProductionDeLEspece(etat, espece));
            return especes.Add(ProductionDuHeros(etat));
        }

        /// <summary>
        /// Ce que le débit BRUT du héros vaut, nommé — `DEBIT_HEROS` avant tout
        /// multiplicateur. Vit ICI, à côté du total qu'il explique
        /// (`ProductionTotaleParSeconde`, juste au-dessus), plutôt que dans
        /// `DetailDeCaptation` plus bas : ce dernier est attributable à UNE espèce, et
        /// le héros n'en porte aucune. La valeur RÉELLEMENT captée — celle à afficher —
        /// est `ProductionDuHeros`, pas la valeur brute d'ici : §8.2 veut la
        /// contrepartie d'un effet, pas son seul nom.
        /// </summary>
        public static IReadOnlyList<LigneDeCaptation> DetailDuHeros(EtatJeu etat)
        {
            var source = new SourceDeTerme(QuoiSource.Heros, etat.Cycle.NiveauDuHeros);
            return new[]
            {
                new LigneDeCaptation(TermeDeFormule.DebitHeros, Constantes.DEBIT_HEROS, source),
                new LigneDeCaptation(TermeDeFormule.MultiplicateurHeros, MultiplicateurDuHeros(etat), source),
            };
        }

        /// <summary>
        /// Détail de la captation (§8.2) : chaque terme actif attribuable à sa source.
        /// C'est la contrepartie obligatoire d'un effet appliqué silencieusement.
        /// </summary>
        public static IReadOnlyList<LigneDeCaptation> DetailDeCaptation(EtatJeu etat, Espece espece)
        {
            var niveau = etat.Cycle.Especes.TryGetValue(espece.Id, out var vivante) ? vivante.Niveau : 0;
            var densite = Densite.DuSejour(etat);
            var rangDeLaGlobale = RangDAmelioration(etat, AmeliorationsDeRenaissance.GLOBALE_ID);
            var rangDeLaCiblee = RangDAmelioration(etat, AmeliorationsDeRenaissance.CibleeDe(espece.Id).Id);
            var lignes = new List<LigneDeCaptation>
            {
                new LigneDeCaptation(TermeDeFormule.Niveau, niveau, new SourceDeTerme(QuoiSource.Niveau, niveau)),
                new LigneDeCaptation(
                    TermeDeFormule.TauxBase, DebitBaseDeLEspece(espece).ToNumber(), new SourceDeTerme(QuoiSource.Palier, espece.Palier)),
                new LigneDeCaptation(
                    TermeDeFormule.AmeliorationGlobale,
                    DebitAmeliore(etat, espece).Div(DebitBaseDeLEspece(espece)).ToNumber(),
                    new SourceDeTerme(QuoiSource.AmeliorationDeRenaissance, rangDeLaGlobale)),
            };
            lignes.Add(new LigneDeCaptation(TermeDeFormule.MultiplicateurJalon, MultiplicateurDeSeuil(niveau), new SourceDeTerme(QuoiSource.Niveau, niveau)));
            lignes.Add(new LigneDeCaptation(
                TermeDeFormule.MultiplicateurAmelioration, MultiplicateurDAmelioration(etat, espece), new SourceDeTerme(QuoiSource.AmeliorationDeRenaissance, rangDeLaCiblee)));
            lignes.Add(new LigneDeCaptation(
                TermeDeFormule.MultiplicateurDrapeau, MultiplicateurDesDrapeaux(etat),
                new SourceDeTerme(QuoiSource.DrapeauxPermanents, etat.Permanent.EspecesAyantAtteintCent.Count)));
            lignes.Add(new LigneDeCaptation(
                TermeDeFormule.MultiplicateurProfondeur, MultiplicateurDeProfondeur(etat).ToNumber(), new SourceDeTerme(QuoiSource.Profondeur, etat.Cycle.PaliersOuverts)));
            lignes.Add(new LigneDeCaptation(TermeDeFormule.MultiplicateurDensite, Densite.Multiplicateur(densite), new SourceDeTerme(QuoiSource.Densite, densite)));
            lignes.Add(new LigneDeCaptation(TermeDeFormule.MultiplicateurHeros, MultiplicateurDuHeros(etat), new SourceDeTerme(QuoiSource.Heros, etat.Cycle.NiveauDuHeros)));
            lignes.Add(new LigneDeCaptation(
                TermeDeFormule.MultiplicateurDeLieu, MultiplicateurDeLieu(etat, espece),
                new SourceDeTerme(QuoiSource.BonusDeLieu, RegistreDesBonus.DuLieu(espece.Assise).Sum(b => RangDeBonus(etat, b.Id)))));
            return lignes;
        }

        /* ─── Coûts ─────────────────────────────────────────────────────────────────*/

        /// <summary>
        /// Facteur appliqué à un terme de coût par les succès acquis.
        ///
        /// Il vit ici plutôt que dans `RegleDesSucces` pour que les dépendances restent à
        /// sens unique : `RegleDesSucces` lit la production, l'économie lit les effets. Un
        /// cycle d'imports entre les deux tiendrait à l'exécution et tomberait au
        /// premier changement d'ordre d'initialisation.
        /// </summary>
        public static double FacteurDeSucces(EtatJeu etat, TermeDeFormule terme)
        {
            var facteur = 1.0;
            foreach (var succes in RegistreDesSucces.Tous)
            {
                var effet = succes.Effet;
                if (effet == null || effet.Genre == GenreDEffetDeSucces.Verbe) continue;
                if (effet.Terme != terme) continue;
                // Lu directement, jamais via `RegleDesSucces` : ce module y est importé,
                // et un cycle d'imports tiendrait à l'exécution pour tomber au premier
                // changement d'ordre d'initialisation.
                if (!etat.Permanent.Succes.ContainsKey(succes.Id)) continue;
                // `Part` est la fraction retirée d'un coût, ou ajoutée à un plafond.
                facteur *= effet.Genre == GenreDEffetDeSucces.ReductionCout ? 1 - effet.Part : 1 + effet.Part;
            }
            return facteur;
        }

        /// <summary>
        /// Technique, succès et bonus se composent sur un même terme, chacun nommé et
        /// attribuable. `assise` est le lieu de ce qu'on paie — l'espèce, le palier — et ne
        /// sert qu'aux bonus de lieu ; null, seuls les bonus valables partout comptent.
        /// </summary>
        static double FacteurDeCout(EtatJeu etat, TermeDeFormule terme, string assise = null) =>
            Technique.FacteurDeTechnique(etat, terme) * FacteurDeSucces(etat, terme) * FacteurDeBonus(etat, terme, assise);

        /// Coût d'origine d'un palier, avant tout levier. Le palier 0 est ouvert au départ.
        public static Decimal CoutBaseDuPalier(int cible) =>
            Echelles.PuissanceDeG(Math.Max(0, cible - 1)).Mul(Constantes.COUT_CREUSER_AU_PALIER_1);

        /// <summary>
        /// Ce que coûte de descendre d'un palier — un seul puits, noyau v1.0 §3.1.
        ///
        ///   coût_base(palier) × technique × succès
        ///
        /// `f` valait 1 depuis toujours : le noyau v1.0 §3.1 ferme [P5] et retire le
        /// tarif réduit qu'un palier déjà atteint dans une vie passée payait avant le
        /// 2026-09-09. Retraverser coûte exactement ce qu'un creusement neuf coûterait
        /// — la profondeur maximale atteinte n'entre plus dans ce calcul.
        /// </summary>
        public static Decimal CoutDeDescente(EtatJeu etat, int cible) =>
            CoutBaseDuPalier(cible)
                .Mul(Technique.FacteurDeTechnique(etat, TermeDeFormule.CoutCreuser))
                .Mul(FacteurDeSucces(etat, TermeDeFormule.CoutCreuser))
                .Fois(FacteurDeBonus(etat, TermeDeFormule.CoutCreuser, LieuDuPalier(cible)));

        /// <summary>
        /// Ce que coûte de débloquer une espèce — noyau v1.0 §1.3.
        ///
        ///   coût_base(palier de l'espèce) × COUT_DEBLOCAGE_RATIO
        ///
        /// Une fois par espèce et par vie. Le déblocage suit le coût de son palier
        /// plutôt qu'une échelle à lui : c'est la profondeur où elle vit qui dit ce
        /// qu'il en coûte de l'atteindre.
        ///
        /// La densité n'entre plus ici. Elle payait la reconviction (GDD §7.1) tant
        /// qu'il y avait une population à reconvaincre ; elle n'a plus qu'un seul
        /// débouché, la production, par le multiplicateur de densité, et le coût de
        /// déblocage est redevenu un levier ordinaire.
        /// </summary>
        public static Decimal CoutDeDeblocage(EtatJeu etat, Espece espece) =>
            CoutBaseDuPalier(espece.Palier)
                .Mul(Constantes.COUT_DEBLOCAGE_RATIO)
                .Mul(FacteurDeCout(etat, TermeDeFormule.CoutDeblocage, espece.Assise));

        /// <summary>
        /// Coût du niveau suivant. Achat répétable de la boucle, ×1.15.
        ///
        /// Il suit le débit de base de SON espèce, donc le temps de remboursement d'un
        /// niveau est le même pour la première espèce et pour la vingt et unième.
        /// </summary>
        public static Decimal CoutDeNiveau(EtatJeu etat, Espece espece, int niveau) =>
            DebitBaseDeLEspece(espece)
                .Mul(Constantes.COUT_NIVEAU_PAR_DEBIT)
                .Mul(Echelles.PuissanceDuCoutDeNiveau(Math.Max(0, niveau)))
                .Mul(FacteurDeCout(etat, TermeDeFormule.CoutNiveau, espece.Assise));

        /// <summary>
        /// Ce que coûte de faire grandir le héros de `niveau` à `niveau + 1` — spec
        /// 2026-09-17 [D3] : une fraction du coût du palier de même rang, donc `g^(n−1)`.
        /// Le joueur optimal en paie à peu près un par palier, et c'est ce qui autorise
        /// le rebudget de `D` dans `MultiplicateurDePalier`.
        /// </summary>
        public static Decimal CoutDeCroissance(EtatJeu etat, int niveau) =>
            Echelles.PuissanceDeG(Math.Max(0, niveau - 1))
                .Mul(Constantes.COUT_CREUSER_AU_PALIER_1)
                .Mul(Constantes.RATIO_COUT_DE_CROISSANCE)
                .Mul(FacteurDeCout(etat, TermeDeFormule.CoutCroissance));

        /// <summary>
        /// Ce que coûte le rang suivant d'une amélioration, EN SOUFFLE — spec 2026-09-17
        /// [D6]. Géométrique : `base × ratio ^ rang`. `CoutAmelioration` est un terme
        /// de coût nommé, donc la technique et les succès pourront le viser.
        /// </summary>
        public static Decimal CoutDAmelioration(EtatJeu etat, AmeliorationDeRenaissance amelioration)
        {
            var @base = amelioration.Portee == PorteeDAmelioration.Globale
                ? Constantes.SOUFFLE_COUT_D_AMELIORATION_GLOBALE
                : Constantes.SOUFFLE_COUT_D_AMELIORATION_CIBLEE;
            return new Decimal(@base)
                .Mul(Decimal.Pow(Constantes.RATIO_COUT_D_AMELIORATION, RangDAmelioration(etat, amelioration.Id)))
                .Mul(FacteurDeCout(etat, TermeDeFormule.CoutAmelioration));
        }

        /* ─── Bonus de lieu et techniques — spec du 2026-10-10 ──────────────────────
         *
         * L'onglet « Débloquer ». Des achats au MANA, perdus à la renaissance comme les quatre
         * autres. Un bonus de lieu ne touche que son lieu ; une technique vaut partout. Ils ne
         * changent qu'à l'achat, jamais pendant un pas : le pas reste homogène et le §5.2
         * tient sans rien faire. Un facteur neutre n'est jamais multiplié (`Fois`), pour que
         * les parties qui n'en achètent aucun restent identiques au bit près — c'est ce que
         * la parité vérifie.
         */

        public static int RangDeBonus(EtatJeu etat, string id) =>
            etat.Cycle.Bonus != null && etat.Cycle.Bonus.TryGetValue(id, out var rang) ? rang : 0;

        /// <summary>
        /// La maîtrise d'un lieu : combien de ses paliers sont ouverts dans cette vie, de 0
        /// (pas encore atteint) à son nombre de paliers. Elle se perd à la renaissance avec
        /// les paliers : c'est la profondeur de la vie courante, pas un acquis.
        /// </summary>
        public static int MaitriseDuLieu(EtatJeu etat, Assise assise) =>
            Math.Max(0, Math.Min(assise.NombreDePaliers, etat.Cycle.PaliersOuverts - assise.IndexPremierPalier));

        /// Ce que `MaitriseRequise` compare : la maîtrise du lieu, ou les paliers ouverts en tout pour une technique.
        public static int MaitrisePourLeBonus(EtatJeu etat, Bonus bonus) =>
            bonus.Assise == null ? etat.Cycle.PaliersOuverts : MaitriseDuLieu(etat, Assises.ParId(bonus.Assise));

        /// Le bonus est proposé : son lieu est atteint, et assez creusé.
        public static bool BonusOuvert(EtatJeu etat, Bonus bonus) =>
            MaitrisePourLeBonus(etat, bonus) >= Math.Max(1, bonus.MaitriseRequise);

        public static bool BonusAuMaximum(EtatJeu etat, Bonus bonus) => RangDeBonus(etat, bonus.Id) >= bonus.RangMax;

        /// <summary>
        /// Le prix du rang suivant : `coût_base(palier de prix) × relatif × ratio ^ rang`. Il
        /// suit la profondeur où le bonus s'ouvre, comme le déblocage suit celle de son espèce.
        /// </summary>
        public static Decimal CoutDeBonus(EtatJeu etat, Bonus bonus) =>
            CoutBaseDuPalier(bonus.PalierDePrix)
                .Mul(Constantes.COUT_DE_BONUS_RELATIF)
                .Mul(Decimal.Pow(Constantes.RATIO_COUT_DE_BONUS, RangDeBonus(etat, bonus.Id)));

        /// <summary>
        /// Le facteur des bonus achetés sur un terme, pour ce qui se paie ou produit dans
        /// `assise`. Neutre = 1. Une réduction compose `(1 − part) ^ rang` — jamais négative ;
        /// une production `(1 + part) ^ rang` ; un plafond `1 + part × rang`.
        /// </summary>
        public static double FacteurDeBonus(EtatJeu etat, TermeDeFormule terme, string assise)
        {
            var facteur = 1.0;
            foreach (var bonus in RegistreDesBonus.Tous)
            {
                if (bonus.Terme != terme) continue;
                if (bonus.Assise != null && bonus.Assise != assise) continue;
                var rang = RangDeBonus(etat, bonus.Id);
                if (rang == 0) continue;
                switch (bonus.Genre)
                {
                    case GenreDeBonus.ReductionDeCout: facteur *= Math.Pow(1 - bonus.Part, rang); break;
                    case GenreDeBonus.Production: facteur *= Math.Pow(1 + bonus.Part, rang); break;
                    case GenreDeBonus.Confort: facteur *= 1 + bonus.Part * rang; break;
                }
            }
            return facteur;
        }

        /// Le multiplicateur que les bonus de son lieu donnent à une espèce. 1 sans bonus.
        public static double MultiplicateurDeLieu(EtatJeu etat, Espece espece) =>
            FacteurDeBonus(etat, TermeDeFormule.MultiplicateurDeLieu, espece.Assise);

        /// Les verbes ouverts par les techniques achetées dans cette vie.
        public static IReadOnlyCollection<CapaciteId> CapacitesDesBonus(EtatJeu etat)
        {
            var ouvertes = new HashSet<CapaciteId>();
            foreach (var bonus in RegistreDesBonus.Tous)
                if (bonus.Genre == GenreDeBonus.Verbe && bonus.Capacite.HasValue && RangDeBonus(etat, bonus.Id) > 0)
                    ouvertes.Add(bonus.Capacite.Value);
            return ouvertes;
        }

        static string LieuDuPalier(int index) =>
            index >= 0 && index < Constantes.NOMBRE_DE_PALIERS ? Assises.DuPalier(index).Id : null;

        /// Multiplie par un facteur, sauf s'il est neutre : une partie sans bonus reste exacte au bit près.
        static Decimal Fois(this Decimal valeur, double facteur) => facteur == 1 ? valeur : valeur.Mul(facteur);

        /* ─── Contenance et blocage doux (§6.4) ─────────────────────────────────────*/

        /// La contenance limite le stock, pas la production.
        public static Decimal Contenance(EtatJeu etat) => etat.Permanent.ContenanceMana.Mul(1 + etat.Cycle.AcquisDeSejour);

        /* ─── La jauge et sa saturation — GDD §2.4 ──────────────────────────────────
         *
         * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
         * partie. C'est la seule pénalité du jeu, et elle est douce. »
         */

        /// Part du plafond effectivement portée, de 0 à 1.
        public static double PartDeContenance(EtatJeu etat)
        {
            var plafond = Contenance(etat);
            if (plafond.Lte(0)) return 0;
            return Math.Min(1, etat.Cycle.ManaCourant.Div(plafond).ToNumber());
        }

        /// <summary>
        /// L'alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. »
        ///
        /// Le noyau rend l'état, jamais l'effet : c'est à l'écran de le montrer sans
        /// l'écrire.
        /// </summary>
        public static bool EauTroublee(EtatJeu etat) => PartDeContenance(etat) >= Constantes.SEUIL_D_ALERTE_DE_CONTENANCE;

        /// Saturation : « la captation s'arrête. Il dépense encore, il ne gagne plus. »
        public static bool EstSature(EtatJeu etat) => etat.Cycle.ManaCourant.Gte(Contenance(etat));

        /// Plus rien à creuser : soit la roche est finie, soit le contenu l'est.
        public static bool ToutEstCreuse(EtatJeu etat) =>
            etat.Cycle.PaliersOuverts >= Math.Min(etat.LimiteDeContenu, Constantes.NOMBRE_DE_PALIERS);

        /// <summary>
        /// Le blocage doux : le palier suivant coûte plus que ce que la contenance peut
        /// porter. Le joueur peut continuer à monter des niveaux et à faire grossir son
        /// Souffle ; il ne peut simplement plus descendre. C'est la raison diégétique de
        /// la renaissance, et sa seule vraie décision : partir maintenant pour la
        /// profondeur, ou rester pour le Souffle.
        /// </summary>
        public static bool EstBloque(EtatJeu etat)
        {
            if (ToutEstCreuse(etat)) return true;
            return CoutDeDescente(etat, etat.Cycle.PaliersOuverts).Gt(Contenance(etat));
        }
    }
}
