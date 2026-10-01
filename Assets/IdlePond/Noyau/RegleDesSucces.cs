using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Noyau
{
    public sealed record ResultatDeSucces(EtatJeu Etat, IReadOnlyList<string> Declenches);

    /// `Visibilite` : ce que le joueur a le droit de voir, une fois l'assise atteinte.
    /// `Entree` : ce qu'on en a retenu, s'il est acquis — porte le registre figé
    /// (§14.5) ; null sinon.
    public sealed record SuccesAffichable(Succes Succes, bool Acquis, VisibiliteDeSucces Visibilite, EntreeDeSucces Entree);

    /// <summary>
    /// IdlePond — succès : déclencheurs, effets, visibilité.
    ///
    /// Système de plein droit, porteur d'effets ET de la distribution narrative
    /// continue. La narration est une récompense distribuée à la cadence idle, pas
    /// une couche séparée (§4.2) : elle passe par ici et par le journal.
    ///
    /// Trois choses ne se rétrofitent pas (§8) et sont donc posées avant le
    /// contenu : le typage par famille, l'état de visibilité, et le registre figé.
    ///
    /// Un effet est appliqué SILENCIEUSEMENT au déclenchement. Ses deux
    /// contreparties sont obligatoires mais appartiennent à l'UI : une notification
    /// discrète et non bloquante — une ligne qui apparaît et s'efface, jamais une
    /// fenêtre — et le détail de la captation, consultable, où chaque terme actif
    /// est attribuable à sa source. Le noyau rend la liste des déclenchements ;
    /// l'adaptateur en fait une ligne.
    /// </summary>
    public static class RegleDesSucces
    {
        /* ─── Déclencheurs ──────────────────────────────────────────────────────────*/

        /// Espèces débloquées dans la vie courante.
        static int EspecesDebloquees(EtatJeu etat) => etat.Cycle.Especes.Values.Count(e => e.Debloquee);

        /// Le niveau d'une espèce. Zéro tant qu'elle n'est pas débloquée.
        static int NiveauDEspece(EtatJeu etat, string espece) =>
            etat.Cycle.Especes.TryGetValue(espece, out var vivante) && vivante.Debloquee ? vivante.Niveau : 0;

        /// <summary>
        /// Somme des niveaux de la mare entière.
        ///
        /// Remplace l'effectif total : c'est la même idée — ce qui vit là, tout
        /// confondu — mesurée sur la seule quantité que le noyau v1.0 connaisse encore.
        /// </summary>
        static int NiveauxCumules(EtatJeu etat)
        {
            var total = 0;
            foreach (var espece in etat.Cycle.Especes.Values)
                if (espece.Debloquee) total += espece.Niveau;
            return total;
        }

        /// <summary>
        /// Un palier ne peut plus rien recevoir.
        ///
        /// Sans population il n'y a plus de cible d'effectif à rejoindre : « plein » se
        /// lit sur le niveau de l'espèce que le palier ouvre, au seuil du drapeau
        /// permanent — c'est le plus haut que le canon connaisse. Deux paliers sur trois
        /// n'ouvrent aucune espèce ; ceux-là ne prendront jamais personne, et sont donc
        /// au complet dès qu'ils sont ouverts.
        /// </summary>
        static bool PalierAuComplet(EtatJeu etat, int palier)
        {
            if (palier >= etat.Cycle.PaliersOuverts) return false;
            var espece = Paliers.EspeceDuPalier(palier);
            if (espece == null) return true;
            return NiveauDEspece(etat, espece.Id) >= Constantes.SEUIL_DU_DRAPEAU_PERMANENT;
        }

        /// `densites[palier] ?? 0` : un index hors borne lit zéro, il ne lève pas.
        static double DensiteDuPalier(EtatJeu etat, int palier) =>
            palier >= 0 && palier < etat.Permanent.Densites.Count ? etat.Permanent.Densites[palier] : 0;

        /// Lecture de seuil sur l'état de fin de tick. Aucun événement consommé au vol.
        public static bool EstAtteint(EtatJeu etat, DeclencheurDeSucces declencheur)
        {
            switch (declencheur.Quoi)
            {
                case QuoiDeclencheur.Renaissances:
                    return etat.Permanent.NombreDeRenaissances >= declencheur.Seuil;
                case QuoiDeclencheur.PaliersOuverts:
                    return etat.Cycle.PaliersOuverts >= declencheur.Seuil;
                case QuoiDeclencheur.ProfondeurMax:
                    return etat.Permanent.ProfondeurMaxAtteinte >= declencheur.Seuil;
                case QuoiDeclencheur.EspecesDebloquees:
                    return EspecesDebloquees(etat) >= declencheur.Seuil;
                case QuoiDeclencheur.NiveauDEspece:
                    return NiveauDEspece(etat, declencheur.Espece) >= declencheur.Seuil;
                case QuoiDeclencheur.NiveauxCumules:
                    return NiveauxCumules(etat) >= declencheur.Seuil;
                case QuoiDeclencheur.ProductionParSeconde:
                    return Economie.ProductionTotaleParSeconde(etat).Gte(declencheur.Seuil);
                case QuoiDeclencheur.Souffle:
                    return etat.Permanent.Souffle.Gte(declencheur.Seuil);
                case QuoiDeclencheur.DensiteDePalier:
                    return DensiteDuPalier(etat, declencheur.Palier) >= declencheur.Seuil;
                case QuoiDeclencheur.PalierAuComplet:
                    return PalierAuComplet(etat, declencheur.Palier);
                default:
                    throw new InvalidOperationException($"Déclencheur inconnu : {declencheur.Quoi}");
            }
        }

        /* ─── Déclenchement ─────────────────────────────────────────────────────────*/

        /// <summary>
        /// Relit tous les seuils et acquiert ceux qui viennent d'être franchis.
        ///
        /// Le parcours est un balayage du registre à chaque tick, et c'est licite : le
        /// filtre du §5.2 interdit d'itérer sur une FILE d'événements, pas de relire un
        /// registre figé dont la taille est connue à la compilation. Le coût ne dépend
        /// ni de `dt` ni de l'histoire de la partie.
        /// </summary>
        public static ResultatDeSucces VerifierSucces(EtatJeu etat)
        {
            List<string> declenches = null;
            foreach (var succes in RegistreDesSucces.Tous)
            {
                if (EstAcquis(etat, succes.Id)) continue;
                if (!EstAtteint(etat, succes.Declencheur)) continue;
                (declenches ??= new List<string>()).Add(succes.Id);
            }
            if (declenches == null) return new ResultatDeSucces(etat, Array.Empty<string>());

            // Le registre de l'entrée est figé ICI, au déclenchement, et ne sera jamais
            // réécrit (§14.5). C'est le seul endroit du code où la voix courante est lue
            // pour être conservée : partout ailleurs elle se dérive.
            var entree = new EntreeDeSucces(etat.Permanent.NombreDeRenaissances, Voix.PalierDe(etat));

            return new ResultatDeSucces(
                etat with
                {
                    Permanent = etat.Permanent with
                    {
                        Succes = EnOrdreDuRegistre(etat.Permanent.Succes, new HashSet<string>(declenches), entree),
                    },
                },
                declenches);
        }

        public static bool EstAcquis(EtatJeu etat, string id) => etat.Permanent.Succes.ContainsKey(id);

        /// <summary>
        /// Reconstruit la table entière dans l'ordre du registre, jamais dans l'ordre
        /// d'arrivée.
        ///
        /// Deux succès franchis pendant le même intervalle arrivent dans un ordre qui
        /// dépend de la taille du pas : un pas de 8 h les voit ensemble, 480 pas de 60 s
        /// les voient l'un après l'autre. L'ordre d'énumération d'un dictionnaire neuf
        /// étant ici celui de ses insertions, insérer à l'arrivée ferait diverger la
        /// chaîne de sauvegarde de deux parties par ailleurs identiques — et le test de
        /// déterminisme compare précisément cette chaîne. L'ordre du registre, lui, ne
        /// dépend de rien.
        /// </summary>
        static IReadOnlyDictionary<string, EntreeDeSucces> EnOrdreDuRegistre(
            IReadOnlyDictionary<string, EntreeDeSucces> acquis,
            IReadOnlyCollection<string> nouveaux,
            EntreeDeSucces entree)
        {
            var table = new Dictionary<string, EntreeDeSucces>();
            foreach (var succes in RegistreDesSucces.Tous)
            {
                if (acquis.TryGetValue(succes.Id, out var deja)) table[succes.Id] = deja;
                else if (nouveaux.Contains(succes.Id)) table[succes.Id] = entree;
            }
            return table;
        }

        /// <summary>
        /// Enregistre l'intervalle écoulé depuis le succès précédent (§11).
        ///
        /// Séparé du tick, et il faut l'être : le tick doit rendre le même état pour un
        /// pas de 8 h et pour 480 pas de 60 s, or l'instant précis où un seuil a été
        /// franchi À L'INTÉRIEUR d'un intervalle n'est pas connaissable d'un seul pas.
        /// C'est une OBSERVATION, pas une mécanique — elle appartient à celui qui
        /// observe le déclenchement, à la cadence à laquelle il l'observe.
        ///
        /// Le jeu l'appelle à 100 ms et mesure donc l'intervalle vrai. Le crédit hors
        /// ligne ne l'appelle pas : il ne peut pas prétendre savoir qu'un succès est
        /// tombé il y a six heures.
        /// </summary>
        public static EtatJeu EnregistrerIntervalleDeSucces(EtatJeu etat, IReadOnlyList<string> declenches)
        {
            if (declenches.Count == 0) return etat;
            var intervalles = new List<double>(etat.Telemetrie.IntervallesEntreSucces)
            {
                etat.Telemetrie.SecondesDepuisDernierSucces,
            };
            return etat with
            {
                Telemetrie = etat.Telemetrie with
                {
                    IntervallesEntreSucces = intervalles,
                    SecondesDepuisDernierSucces = 0,
                },
            };
        }

        /* ─── Effets ────────────────────────────────────────────────────────────────*/

        /// Capacités ouvertes par les succès. Jamais les mêmes que celles de l'arbre.
        public static IReadOnlyCollection<CapaciteId> CapacitesDesSucces(EtatJeu etat)
        {
            var ouvertes = new HashSet<CapaciteId>();
            foreach (var succes in RegistreDesSucces.Tous)
            {
                if (succes.Effet?.Genre != GenreDEffetDeSucces.Verbe) continue;
                if (!EstAcquis(etat, succes.Id)) continue;
                ouvertes.Add(succes.Effet.Capacite.Value);
            }
            return ouvertes;
        }

        /* ─── Visibilité (§8.3) ─────────────────────────────────────────────────────*/

        /// Une assise est atteinte dès qu'un de ses paliers a été ouvert, une fois.
        public static IReadOnlyCollection<string> AssisesAtteintes(EtatJeu etat)
        {
            var atteintes = new HashSet<string>();
            var profondeur = Math.Max(etat.Permanent.ProfondeurMaxAtteinte, etat.Cycle.PaliersOuverts);
            for (var palier = 0; palier < profondeur; palier += 1)
                atteintes.Add(Assises.DuPalier(palier).Id);
            if (atteintes.Count == 0) atteintes.Add(Assises.Toutes[0].Id);
            return atteintes;
        }

        /// <summary>
        /// Ce que l'écran des succès a le droit de lister.
        ///
        /// Verrouillage par assise : un succès n'est listé, quel que soit son état, que
        /// lorsque son assise est atteinte. Pas de compteur global de secrets — des
        /// emplacements vides par assise, ce qui dit qu'il y a quelque chose là sans
        /// dire combien il en reste ailleurs.
        ///
        /// Un succès acquis se montre toujours en clair : on ne cache pas au joueur ce
        /// qu'il vient d'obtenir.
        /// </summary>
        public static IReadOnlyList<SuccesAffichable> SuccesListables(EtatJeu etat, string assise)
        {
            if (!AssisesAtteintes(etat).Contains(assise)) return Array.Empty<SuccesAffichable>();
            return RegistreDesSucces.Tous.Where(s => s.Assise == assise).Select(succes =>
            {
                var entree = etat.Permanent.Succes.TryGetValue(succes.Id, out var e) ? e : null;
                var acquis = entree != null;
                return new SuccesAffichable(succes, acquis, acquis ? VisibiliteDeSucces.Ouvert : succes.Visibilite, entree);
            }).ToList();
        }

        /// Progression vers un seuil, pour la barre des succès ouverts. 0 à 1.
        public static double? ProgressionVersLeSucces(EtatJeu etat, Succes succes)
        {
            var declencheur = succes.Declencheur;
            if (declencheur.Quoi == QuoiDeclencheur.PalierAuComplet) return null;
            var seuil = declencheur.Seuil;
            if (!(seuil > 0)) return null;
            var courant = ValeurCourante(etat, declencheur);
            if (courant == null) return null;
            return Math.Max(0, Math.Min(1, courant.Value / seuil));
        }

        static double? ValeurCourante(EtatJeu etat, DeclencheurDeSucces declencheur)
        {
            switch (declencheur.Quoi)
            {
                case QuoiDeclencheur.Renaissances:
                    return etat.Permanent.NombreDeRenaissances;
                case QuoiDeclencheur.PaliersOuverts:
                    return etat.Cycle.PaliersOuverts;
                case QuoiDeclencheur.ProfondeurMax:
                    return etat.Permanent.ProfondeurMaxAtteinte;
                case QuoiDeclencheur.EspecesDebloquees:
                    return EspecesDebloquees(etat);
                case QuoiDeclencheur.NiveauDEspece:
                    return NiveauDEspece(etat, declencheur.Espece);
                case QuoiDeclencheur.NiveauxCumules:
                    return NiveauxCumules(etat);
                case QuoiDeclencheur.ProductionParSeconde:
                    return Economie.ProductionTotaleParSeconde(etat).ToNumber();
                case QuoiDeclencheur.Souffle:
                    return etat.Permanent.Souffle.ToNumber();
                case QuoiDeclencheur.DensiteDePalier:
                    return DensiteDuPalier(etat, declencheur.Palier);
                case QuoiDeclencheur.PalierAuComplet:
                    return null;
                default:
                    throw new InvalidOperationException($"Déclencheur inconnu : {declencheur.Quoi}");
            }
        }
    }
}
