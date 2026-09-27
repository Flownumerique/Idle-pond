/*
 * IdlePond — succès : déclencheurs, effets, visibilité. Port de
 * `src/noyau/succes.ts`.
 *
 * Un effet est appliqué SILENCIEUSEMENT au déclenchement. Ses deux contreparties
 * appartiennent à l'UI : une ligne qui apparaît et s'efface, jamais une fenêtre,
 * et le détail de la captation. Le noyau rend la liste des déclenchements.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;

namespace IdlePond.Noyau
{
    public sealed record ResultatDeSucces(EtatJeu Etat, IReadOnlyList<string> Declenches);

    /// <summary>Ce que l'écran des succès a le droit de lister (§8.3).</summary>
    public sealed record SuccesAffichable(Succes Succes, bool Acquis, VisibiliteDeSucces Visibilite, EntreeDeSucces Entree);

    public static class SystemeDeSucces
    {
        private static readonly IReadOnlyList<string> AucunDeclenchement = Array.Empty<string>();

        /* ─── Déclencheurs ──────────────────────────────────────────────────── */

        private static double EffectifTotal(EtatJeu etat)
        {
            double total = 0;
            foreach (var banc in etat.Cycle.Bancs.Valeurs) total += banc.Effectif;
            return total;
        }

        /// <summary>Effectif d'une espèce, tous paliers confondus : les seuils du §8.1 comptent des INDIVIDUS.</summary>
        private static double EffectifDEspece(EtatJeu etat, string espece)
        {
            double total = 0;
            foreach (var paire in etat.Cycle.Bancs)
            {
                if (Paliers.BancParId(paire.Key)?.Espece == espece) total += paire.Value.Effectif;
            }
            return total;
        }

        private static int BancsConvaincus(EtatJeu etat) => etat.Cycle.Bancs.Valeurs.Count(b => b.Place > 0);

        /// <summary>
        /// Un palier est saturé quand tous ses bancs sont convaincus et que leur
        /// effectif a rejoint sa cible — « à une fraction près » : l'exponentielle ne
        /// l'atteint jamais exactement.
        /// </summary>
        private static bool PalierSature(EtatJeu etat, int palier)
        {
            if (palier >= etat.Cycle.PaliersOuverts) return false;
            foreach (var banc in Paliers.Liste[palier].Bancs)
            {
                if (!etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var etatDuBanc) || etatDuBanc.Place <= 0) return false;
                if (etatDuBanc.Effectif < Population.EffectifCible(etatDuBanc.Place) * Constantes.SaturationDUnPalier) return false;
            }
            return true;
        }

        private static double PlaceDeBanc(EtatJeu etat, string banc) =>
            etat.Cycle.Bancs.EssayerDeLire(banc, out var b) ? b.Place : 0;

        private static double EffectifDeBanc(EtatJeu etat, string banc) =>
            etat.Cycle.Bancs.EssayerDeLire(banc, out var b) ? b.Effectif : 0;

        /// <summary>Lecture de seuil sur l'état de fin de tick. Aucun événement consommé au vol.</summary>
        public static bool EstAtteint(EtatJeu etat, DeclencheurDeSucces d)
        {
            switch (d.Quoi)
            {
                case QuoiDeclencheur.Eclosions: return etat.Permanent.NombreEclosions >= d.Seuil;
                case QuoiDeclencheur.PaliersOuverts: return etat.Cycle.PaliersOuverts >= d.Seuil;
                case QuoiDeclencheur.ProfondeurMax: return etat.Permanent.ProfondeurMaxAtteinte >= d.Seuil;
                case QuoiDeclencheur.BancsConvaincus: return BancsConvaincus(etat) >= d.Seuil;
                case QuoiDeclencheur.EffectifDeBanc: return EffectifDeBanc(etat, d.Banc) >= d.Seuil;
                case QuoiDeclencheur.EffectifDEspece: return EffectifDEspece(etat, d.Espece) >= d.Seuil;
                case QuoiDeclencheur.PlaceDeBanc: return PlaceDeBanc(etat, d.Banc) >= d.Seuil;
                case QuoiDeclencheur.EffectifTotal: return EffectifTotal(etat) >= d.Seuil;
                case QuoiDeclencheur.ProductionParSeconde: return Economie.ProductionTotaleParSeconde(etat).Gte(d.Seuil);
                case QuoiDeclencheur.Foi: return etat.Permanent.Foi.Gte(d.Seuil);
                case QuoiDeclencheur.DensiteDePalier: return Densite.DensiteDuPalier(etat, d.Palier) >= d.Seuil;
                case QuoiDeclencheur.PalierSature: return PalierSature(etat, d.Palier);
                default: throw new ArgumentOutOfRangeException(nameof(d), d.Quoi, null);
            }
        }

        /* ─── Déclenchement ─────────────────────────────────────────────────── */

        /// <summary>
        /// Relit tous les seuils et acquiert ceux qui viennent d'être franchis. Un
        /// balayage d'un registre figé, pas une file d'événements (§5.2).
        /// </summary>
        public static ResultatDeSucces VerifierSucces(EtatJeu etat)
        {
            List<string> declenches = null;
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (EstAcquis(etat, succes.Id)) continue;
                if (!EstAtteint(etat, succes.Declencheur)) continue;
                (declenches ??= new List<string>()).Add(succes.Id);
            }
            if (declenches == null) return new ResultatDeSucces(etat, AucunDeclenchement);

            // Le registre de l'entrée est figé ICI, au déclenchement, et ne sera
            // jamais réécrit (§14.5).
            var entree = new EntreeDeSucces(etat.Permanent.NombreEclosions, Voix.PalierCourant(etat));
            var nouveaux = new HashSet<string>(declenches, StringComparer.Ordinal);

            return new ResultatDeSucces(
                etat with { Permanent = etat.Permanent with { Succes = EnOrdreDuRegistre(etat.Permanent.Succes, nouveaux, entree) } },
                declenches);
        }

        public static bool EstAcquis(EtatJeu etat, string id) => etat.Permanent.Succes.Contient(id);

        /// <summary>
        /// Reconstruit la table entière dans l'ordre du registre, jamais dans l'ordre
        /// d'arrivée — qui dépend de la taille du pas.
        /// </summary>
        private static TableOrdonnee<EntreeDeSucces> EnOrdreDuRegistre(
            TableOrdonnee<EntreeDeSucces> acquis, HashSet<string> nouveaux, EntreeDeSucces entree)
        {
            var paires = new List<KeyValuePair<string, EntreeDeSucces>>();
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (acquis.EssayerDeLire(succes.Id, out var deja)) paires.Add(new KeyValuePair<string, EntreeDeSucces>(succes.Id, deja));
                else if (nouveaux.Contains(succes.Id)) paires.Add(new KeyValuePair<string, EntreeDeSucces>(succes.Id, entree));
            }
            return TableOrdonnee<EntreeDeSucces>.Depuis(paires);
        }

        /// <summary>
        /// Enregistre l'intervalle écoulé depuis le succès précédent (§11). Une
        /// OBSERVATION, pas une mécanique : le jeu l'appelle à 100 ms ; le crédit hors
        /// ligne ne l'appelle pas.
        /// </summary>
        public static EtatJeu EnregistrerIntervalleDeSucces(EtatJeu etat, IReadOnlyList<string> declenches)
        {
            if (declenches.Count == 0) return etat;
            return etat with
            {
                Telemetrie = etat.Telemetrie with
                {
                    IntervallesEntreSucces = etat.Telemetrie.IntervallesEntreSucces
                        .Append(etat.Telemetrie.SecondesDepuisDernierSucces).ToArray(),
                    SecondesDepuisDernierSucces = 0,
                },
            };
        }

        /* ─── Effets ────────────────────────────────────────────────────────── */

        /// <summary>Capacités ouvertes par les succès. Jamais les mêmes que celles de l'arbre.</summary>
        public static IReadOnlyCollection<CapaciteId> CapacitesDesSucces(EtatJeu etat)
        {
            var ouvertes = new HashSet<CapaciteId>();
            foreach (var succes in RegistreDesSucces.Liste)
            {
                if (succes.Effet == null || succes.Effet.Genre != GenreDEffet.Verbe) continue;
                if (!EstAcquis(etat, succes.Id)) continue;
                ouvertes.Add(succes.Effet.Capacite);
            }
            return ouvertes;
        }

        /* ─── Visibilité (§8.3) ─────────────────────────────────────────────── */

        /// <summary>Une assise est atteinte dès qu'un de ses paliers a été ouvert, une fois.</summary>
        public static IReadOnlyCollection<string> AssisesAtteintes(EtatJeu etat)
        {
            var atteintes = new HashSet<string>(StringComparer.Ordinal);
            var profondeur = Math.Max(etat.Permanent.ProfondeurMaxAtteinte, etat.Cycle.PaliersOuverts);
            for (var palier = 0; palier < profondeur; palier += 1) atteintes.Add(Assises.DuPalier(palier).Id);
            if (atteintes.Count == 0) atteintes.Add(Assises.Liste[0].Id);
            return atteintes;
        }

        /// <summary>
        /// Verrouillage par assise : un succès n'est listé que lorsque son assise est
        /// atteinte. Un succès acquis se montre toujours en clair.
        /// </summary>
        public static IReadOnlyList<SuccesAffichable> SuccesListables(EtatJeu etat, string assise)
        {
            if (!AssisesAtteintes(etat).Contains(assise)) return Array.Empty<SuccesAffichable>();
            return RegistreDesSucces.Liste
                .Where(s => s.Assise == assise)
                .Select(succes =>
                {
                    etat.Permanent.Succes.EssayerDeLire(succes.Id, out var entree);
                    var acquis = entree != null;
                    return new SuccesAffichable(succes, acquis, acquis ? VisibiliteDeSucces.Ouvert : succes.Visibilite, entree);
                })
                .ToArray();
        }

        /// <summary>Progression vers un seuil, pour la barre des succès ouverts. 0 à 1, ou null.</summary>
        public static double? ProgressionVersLeSucces(EtatJeu etat, Succes succes)
        {
            var d = succes.Declencheur;
            if (d.Quoi == QuoiDeclencheur.PalierSature) return null;
            var seuil = d.Seuil;
            if (!(seuil > 0)) return null;
            var courant = ValeurCourante(etat, d);
            if (courant == null) return null;
            return Math.Max(0, Math.Min(1, courant.Value / seuil));
        }

        private static double? ValeurCourante(EtatJeu etat, DeclencheurDeSucces d)
        {
            switch (d.Quoi)
            {
                case QuoiDeclencheur.Eclosions: return etat.Permanent.NombreEclosions;
                case QuoiDeclencheur.PaliersOuverts: return etat.Cycle.PaliersOuverts;
                case QuoiDeclencheur.ProfondeurMax: return etat.Permanent.ProfondeurMaxAtteinte;
                case QuoiDeclencheur.BancsConvaincus: return BancsConvaincus(etat);
                case QuoiDeclencheur.EffectifDeBanc: return EffectifDeBanc(etat, d.Banc);
                case QuoiDeclencheur.EffectifDEspece: return EffectifDEspece(etat, d.Espece);
                case QuoiDeclencheur.PlaceDeBanc: return PlaceDeBanc(etat, d.Banc);
                case QuoiDeclencheur.EffectifTotal: return EffectifTotal(etat);
                case QuoiDeclencheur.ProductionParSeconde: return Economie.ProductionTotaleParSeconde(etat).ToNumber();
                case QuoiDeclencheur.Foi: return etat.Permanent.Foi.ToNumber();
                case QuoiDeclencheur.DensiteDePalier: return Densite.DensiteDuPalier(etat, d.Palier);
                default: return null;
            }
        }
    }
}
