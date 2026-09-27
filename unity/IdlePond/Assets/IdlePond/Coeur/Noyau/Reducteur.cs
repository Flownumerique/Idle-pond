/*
 * IdlePond — le noyau. Tick(etat, dt) -> etat. Port de `src/noyau/noyau.ts`.
 *
 * Contrat du §5.1, sans exception :
 *   - fonction pure : aucune horloge, aucun hasard système, aucun accès à Unity ;
 *   - le PRNG est à graine et vit dans l'état ;
 *   - aucun état hors du réducteur : pas de champ statique mutable, pas de cache ;
 *   - le jeu appelle Tick à 100 ms, le simulateur avec dt = 60 s ou 8 h, et c'est
 *     un seul code.
 *
 * Le PRNG n'est jamais tiré sur le chemin continu : un tirage par tick ferait
 * diverger 480 pas de 60 s d'un pas de 8 h.
 *
 * La classe s'appelle `Reducteur` et non `Noyau` : C# ne sait pas nommer une
 * classe comme l'espace de noms qui la contient sans rendre ses appels ambigus.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;

namespace IdlePond.Noyau
{
    public sealed record ResultatDeTick(EtatJeu Etat, IReadOnlyList<string> Declenches);

    public static class Reducteur
    {
        private static readonly IReadOnlyList<string> AucunDeclenchement = Array.Empty<string>();

        /* ─── PRNG ──────────────────────────────────────────────────────────── */

        /// <summary>Tirage pur (mulberry32) : rend la valeur ET l'état suivant. Rien ne mute.</summary>
        public static (double Valeur, EtatPrng Suivant) Tirer(EtatPrng prng)
        {
            unchecked
            {
                var graine = prng.Graine + 0x6d2b79f5u;
                var x = graine;
                x = (x ^ (x >> 15)) * (x | 1u);
                x ^= x + (x ^ (x >> 7)) * (x | 61u);
                return ((x ^ (x >> 14)) / 4294967296.0, new EtatPrng(graine));
            }
        }

        /* ─── État initial ──────────────────────────────────────────────────── */

        /// <summary>
        /// `limiteDeContenu` : combien de paliers le monde offre réellement. Le jeu
        /// passe ce qui est livré, le simulateur les 62 paliers.
        /// </summary>
        public static EtatJeu EtatInitial(uint graine, int limiteDeContenu = Constantes.NombreDePaliers)
        {
            return new EtatJeu
            {
                VersionSave = Constantes.VersionSave,
                Prng = new EtatPrng(graine),
                TempsJeuSecondes = 0,
                LimiteDeContenu = limiteDeContenu,
                Cycle = Eclosion.CycleInitial(),
                Permanent = new EtatPermanent
                {
                    Densites = Enumerable.Repeat(0.0, Constantes.NombreDePaliers).ToArray(),
                    // Une eau que rien n'habite est mûre (GDD §6.5).
                    PartsMures = Enumerable.Repeat(Maturation.PartMureDUneEauIntouchee, Constantes.NombreDePaliers).ToArray(),
                    // Le type natal est acquis d'emblée et ne se repaie jamais (Tier 0).
                    Acclimatations = TableOrdonnee<double>.Vide.Avec(Assises.TypeManaNatal, 1),
                    Foi = GrandNombre.Zero,
                    ContenanceMana = GrandNombre.DepuisNombre(Constantes.ContenanceInitiale),
                    Couches = Array.Empty<string>(),
                    ProfondeurMaxAtteinte = 0,
                    CompteursTechnique = TableOrdonnee<double>.Depuis(
                        Branches.Toutes.Select(b => new KeyValuePair<string, double>(Branches.Identifiant(b), 0))),
                    NoeudsTechnique = Array.Empty<string>(),
                    Succes = TableOrdonnee<EntreeDeSucces>.Vide,
                    NombreEclosions = 0,
                    EspecesAyantAtteintCent = Array.Empty<string>(),
                    ManaAmbiant = GrandNombre.Zero,
                    HeuresHorsLigneCreditees = 0,
                },
                Telemetrie = new EtatTelemetrie
                {
                    Cycles = Array.Empty<MesureDeCycle>(),
                    SecondesEnRedescente = 0,
                    SecondesDepuisDernierSucces = 0,
                    IntervallesEntreSucces = Array.Empty<double>(),
                },
            };
        }

        /* ─── Le tick ───────────────────────────────────────────────────────── */

        /// <summary>
        /// Avance l'état de `dt` secondes. Un seul pas suffit pour n'importe quel `dt` :
        /// l'effectif suit une exponentielle dont la primitive est fermée, et tous les
        /// termes qui la multiplient sont constants sur l'intervalle.
        /// </summary>
        public static ResultatDeTick TickDetaille(EtatJeu etat, double dt)
        {
            if (!(dt > 0)) return new ResultatDeTick(etat, AucunDeclenchement);

            // La version web se rappelle récursivement après chaque coupure. Une
            // boucle fait la même chose sans risquer la pile sur une longue absence.
            List<string> declenches = null;
            var courant = etat;
            var reste = dt;
            for (;;)
            {
                var coupure = ProchaineCoupure(courant, reste);
                var pas = coupure ?? reste;
                var resultat = ApresLePas(PasEntier(courant, pas));
                if (resultat.Declenches.Count > 0) (declenches ??= new List<string>()).AddRange(resultat.Declenches);
                courant = resultat.Etat;
                if (coupure == null) break;
                reste -= coupure.Value;
                if (!(reste > 0)) break;
            }
            return new ResultatDeTick(courant, (IReadOnlyList<string>)declenches ?? AucunDeclenchement);
        }

        /// <summary>
        /// La divergence non choisie, appliquée à la fin du pas où son délai échoit.
        /// Dans le tick, parce que c'est une règle du monde, pas une décision de joueur.
        /// </summary>
        private static ResultatDeTick ApresLePas(ResultatDeTick resultat)
        {
            if (!Economie.DivergenceNonChoisieEstDue(resultat.Etat)) return resultat;
            return resultat with { Etat = Eclosion.Eclore(resultat.Etat, false) };
        }

        /// <summary>
        /// Le premier instant de ]0, dt[ où le pas cesse d'être homogène : le drapeau
        /// des cent individus, la saturation de la jauge, le délai de la divergence.
        /// On coupe au plus tôt des trois — une partition analytique bornée.
        /// </summary>
        private static double? ProchaineCoupure(EtatJeu etat, double dt)
        {
            double? coupure = null;
            void Retenir(double? instant)
            {
                if (instant == null || !(instant.Value > 0) || !(instant.Value < dt)) return;
                if (coupure == null || instant.Value < coupure.Value) coupure = instant;
            }
            Retenir(InstantDuProchainDrapeau(etat, dt));
            Retenir(InstantDeSaturation(etat, dt));
            Retenir(InstantDeLaDivergence(etat));
            return coupure;
        }

        /// <summary>
        /// Instant où la jauge se remplit, s'il tombe dans ce pas. Dichotomie qui
        /// converge par le HAUT : l'instant rendu porte toujours un stock déjà plein.
        /// </summary>
        private static double? InstantDeSaturation(EtatJeu etat, double dt)
        {
            var plafond = Economie.Contenance(etat);
            if (etat.Cycle.ManaCourant.Gte(plafond)) return null;
            if (etat.Cycle.ManaCourant.Add(ManaProduitSur(etat, dt)).Lt(plafond)) return null;

            double bas = 0;
            var haut = dt;
            for (var i = 0; i < 60; i += 1)
            {
                var milieu = (bas + haut) / 2;
                if (etat.Cycle.ManaCourant.Add(ManaProduitSur(etat, milieu)).Gte(plafond)) haut = milieu;
                else bas = milieu;
            }
            return haut;
        }

        /// <summary>Temps restant avant que le délai du §2.4 n'échoie. null hors saturation.</summary>
        private static double? InstantDeLaDivergence(EtatJeu etat)
        {
            if (etat.Cycle.ManaCourant.Lt(Economie.Contenance(etat))) return null;
            return Constantes.DelaiDeDivergenceNonChoisieHeures * 3600 - etat.Cycle.SecondesEnSaturation;
        }

        private sealed class AvanceeDesBancs
        {
            public TableOrdonnee<EtatBanc> Bancs;
            /// <summary>Part mûre de chaque palier à la fin de l'intervalle (GDD §3.0).</summary>
            public double[] PartsMures;
            /// <summary>Mana capté sur l'intervalle, LES DEUX CANAUX.</summary>
            public GrandNombre ManaProduit;
            /// <summary>
            /// Débit du seul canal NATIF à la fin de l'intervalle — ce qui indexe la
            /// pointe. [P] Décision du 2026-09-08 : le canal acclimaté n'est pas
            /// monotone sur un pas, et il ne PRODUIT rien — il prélève une charge
            /// déjà là. Seul le vivant produit (Tier 0 §5).
            /// </summary>
            public GrandNombre ProductionNativeFinale;
        }

        /// <summary>Avance les deux canaux de captation sur `dt` secondes. Pure, sans état.</summary>
        private static AvanceeDesBancs AvancerLesBancs(EtatJeu etat, double dt)
        {
            var bancs = new List<KeyValuePair<string, EtatBanc>>();
            var partsMures = etat.Permanent.PartsMures.ToArray();
            var manaProduit = GrandNombre.Zero;
            var productionNativeFinale = GrandNombre.Zero;

            // Uniforme depuis V11 : le repeuplement ne dépend plus du palier.
            var k = Densite.VitesseDeRepeuplement();

            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
            {
                // ── Canal natif : ce que la population vivante capte ──────────────
                foreach (var banc in Paliers.Liste[palier].Bancs)
                {
                    if (!etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var avant) || avant.Place <= 0) continue;
                    var avancee = Population.AvancerBanc(avant.Effectif, Population.EffectifCible(avant.Place), k, dt, Constantes.SeuilsDeJalon);
                    bancs.Add(new KeyValuePair<string, EtatBanc>(banc.Id, new EtatBanc(avant.Place, avancee.Effectif)));
                    var taux = Economie.TauxParIndividuHorsSeuil(etat, banc);
                    manaProduit = manaProduit.Add(taux.Mul(avancee.IntegralePonderee));
                    productionNativeFinale = productionNativeFinale.Add(taux.Mul(avancee.MultiplicateurFinal).Mul(avancee.Effectif));
                }

                // ── Canal acclimaté : ce que l'eau capte toute seule ──────────────
                var partAvant = Economie.PartMureDuPalier(etat, palier);
                var cible = Maturation.CibleDeMaturation(Economie.PlaceDuPalier(etat, palier));
                var maturation = Maturation.AvancerMaturation(partAvant, cible, dt);
                if (palier < partsMures.Length) partsMures[palier] = maturation.Part;

                var debitParPart = Economie.TauxBaseDuPalier(palier)
                    .Mul(Constantes.IndividusEquivalentsDuCanalAcclimate)
                    .Mul(Economie.RendementAcclimatation(etat, palier));
                // Le mana acclimaté entre en poche ; il n'entre PAS dans la pointe.
                manaProduit = manaProduit.Add(debitParPart.Mul(maturation.Integrale));
            }

            return new AvanceeDesBancs
            {
                Bancs = TableOrdonnee<EtatBanc>.Depuis(bancs),
                PartsMures = partsMures,
                ManaProduit = manaProduit,
                ProductionNativeFinale = productionNativeFinale,
            };
        }

        /// <summary>Ce que la mare produirait sur `dt`, sans rien avancer. Pour la dichotomie.</summary>
        private static GrandNombre ManaProduitSur(EtatJeu etat, double dt) => AvancerLesBancs(etat, dt).ManaProduit;

        private static ResultatDeTick PasEntier(EtatJeu etat, double dt)
        {
            var avancee = AvancerLesBancs(etat, dt);

            // La contenance limite le stock, pas la production. Le surplus expire
            // vers l'ambiant (Tier 0 §5).
            var brut = etat.Cycle.ManaCourant.Add(avancee.ManaProduit);
            var plafond = Economie.Contenance(etat);
            var manaCourant = GrandNombre.Min(brut, plafond);
            var expire = brut.Sub(manaCourant);

            // §2.4 — le décompte de la jauge pleine. Le pas est homogène par
            // construction de la coupure.
            var secondesEnSaturation = etat.Cycle.ManaCourant.Gte(plafond) ? etat.Cycle.SecondesEnSaturation + dt : 0;

            var enRedescente = etat.Cycle.PaliersOuverts < etat.Permanent.ProfondeurMaxAtteinte;

            // Acquis de séjour (§2.B) : accumulation saturante vers `A∞`, dont le
            // temps caractéristique décroît quand la densité monte.
            var tauEffSecondes = Constantes.TauSejourHeures * 3600 / Densite.MultiplicateurDensite(DensiteDuSejour(etat));
            var acquisDeSejour = Constantes.AcquisMax
                                 + (etat.Cycle.AcquisDeSejour - Constantes.AcquisMax) * Math.Exp(-dt / tauEffSecondes);

            var avance = etat with
            {
                TempsJeuSecondes = etat.TempsJeuSecondes + dt,
                Cycle = etat.Cycle with
                {
                    ManaCourant = manaCourant,
                    Bancs = etat.Cycle.Bancs.Fusionner(avancee.Bancs),
                    ProductionPicParSeconde = GrandNombre.Max(etat.Cycle.ProductionPicParSeconde, avancee.ProductionNativeFinale),
                    DureeSecondes = etat.Cycle.DureeSecondes + dt,
                    AcquisDeSejour = acquisDeSejour,
                    SecondesEnSaturation = secondesEnSaturation,
                },
                Permanent = etat.Permanent with
                {
                    PartsMures = avancee.PartsMures,
                    ManaAmbiant = expire.Gt(0) ? etat.Permanent.ManaAmbiant.Add(expire) : etat.Permanent.ManaAmbiant,
                },
                Telemetrie = etat.Telemetrie with
                {
                    SecondesEnRedescente = etat.Telemetrie.SecondesEnRedescente + (enRedescente ? dt : 0),
                    SecondesDepuisDernierSucces = etat.Telemetrie.SecondesDepuisDernierSucces + dt,
                },
            };

            var verifie = SystemeDeSucces.VerifierSucces(PoserLesDrapeauxPermanents(avance));
            return new ResultatDeTick(verifie.Etat, verifie.Declenches);
        }

        /// <summary>
        /// Densité du séjour : la plus dense des eaux où le héros se tient. [P] le
        /// maximum sur les paliers ouverts est retenu.
        /// </summary>
        private static double DensiteDuSejour(EtatJeu etat)
        {
            double densite = 0;
            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
            {
                densite = Math.Max(densite, Densite.DensiteDuPalier(etat, palier));
            }
            return densite;
        }

        /// <summary>
        /// Pose le drapeau permanent des espèces ayant atteint cent individus (§2.C).
        /// La liste est reconstruite dans l'ordre du registre.
        /// </summary>
        private static EtatJeu PoserLesDrapeauxPermanents(EtatJeu etat)
        {
            var effectifs = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var paire in etat.Cycle.Bancs)
            {
                var espece = Paliers.BancParId(paire.Key)?.Espece;
                if (espece == null) continue;
                effectifs.TryGetValue(espece, out var deja);
                effectifs[espece] = deja + paire.Value.Effectif;
            }

            var acquis = new HashSet<string>(etat.Permanent.EspecesAyantAtteintCent, StringComparer.Ordinal);
            var nouveau = false;
            foreach (var espece in Especes.Liste)
            {
                if (acquis.Contains(espece.Id)) continue;
                effectifs.TryGetValue(espece.Id, out var effectif);
                if (effectif < Constantes.SeuilDuDrapeauPermanent) continue;
                acquis.Add(espece.Id);
                nouveau = true;
            }
            if (!nouveau) return etat;

            return etat with
            {
                Permanent = etat.Permanent with
                {
                    EspecesAyantAtteintCent = Especes.Liste.Where(e => acquis.Contains(e.Id)).Select(e => e.Id).ToArray(),
                },
            };
        }

        /// <summary>
        /// Instant, dans ]0, dt[, où une espèce atteindra cent individus pour la
        /// première fois. Dichotomie : une somme d'exponentielles ne s'inverse pas.
        /// </summary>
        private static double? InstantDuProchainDrapeau(EtatJeu etat, double dt)
        {
            var acquis = new HashSet<string>(etat.Permanent.EspecesAyantAtteintCent, StringComparer.Ordinal);
            double? coupure = null;

            foreach (var espece in Especes.Liste)
            {
                if (acquis.Contains(espece.Id)) continue;
                if (EffectifDEspeceA(etat, espece.Id, 0) >= Constantes.SeuilDuDrapeauPermanent) continue;
                if (EffectifDEspeceA(etat, espece.Id, dt) < Constantes.SeuilDuDrapeauPermanent) continue;

                double bas = 0;
                var haut = dt;
                for (var i = 0; i < 60; i += 1)
                {
                    var milieu = (bas + haut) / 2;
                    if (EffectifDEspeceA(etat, espece.Id, milieu) >= Constantes.SeuilDuDrapeauPermanent) haut = milieu;
                    else bas = milieu;
                }
                if (haut > 0 && haut < dt && (coupure == null || haut < coupure.Value)) coupure = haut;
            }
            return coupure;
        }

        /// <summary>Effectif d'une espèce à `t` secondes, tous ses bancs sommés.</summary>
        private static double EffectifDEspeceA(EtatJeu etat, string espece, double t)
        {
            double total = 0;
            var k = Densite.VitesseDeRepeuplement();
            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
            {
                foreach (var banc in Paliers.Liste[palier].Bancs)
                {
                    if (banc.Espece != espece) continue;
                    if (!etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var avant) || avant.Place <= 0) continue;
                    var cible = Population.EffectifCible(avant.Place);
                    total += cible + (avant.Effectif - cible) * Math.Exp(-k * t);
                }
            }
            return total;
        }

        /// <summary>Le contrat du §5.1. `TickDetaille` en rend en plus les succès déclenchés.</summary>
        public static EtatJeu Tick(EtatJeu etat, double dt) => TickDetaille(etat, dt).Etat;

        /* ─── Actes du joueur ───────────────────────────────────────────────────
         * Des réducteurs purs, comme le tick. Un acte qui n'est pas payable rend
         * l'état INCHANGÉ — la même instance, pour que l'appelant puisse le voir.
         */

        /// <summary>Creuser le palier suivant. Bloqué doux si son coût dépasse la contenance.</summary>
        public static EtatJeu Creuser(EtatJeu etat)
        {
            if (Economie.ToutEstCreuse(etat)) return etat;
            var cible = etat.Cycle.PaliersOuverts;
            var cout = Economie.CoutDeDescente(etat, cible);
            if (cout.Gt(Economie.Contenance(etat))) return etat;
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with { ManaCourant = etat.Cycle.ManaCourant.Sub(cout), PaliersOuverts = cible + 1 },
                Permanent = etat.Permanent with
                {
                    ProfondeurMaxAtteinte = Math.Max(etat.Permanent.ProfondeurMaxAtteinte, cible + 1),
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Creusement, cout.ToNumber()),
                },
            };
        }

        /// <summary>Convaincre un banc : le recruter. Jamais « acheter » (§3).</summary>
        public static EtatJeu Convaincre(EtatJeu etat, string bancId)
        {
            var banc = Paliers.BancParId(bancId);
            if (banc == null) return etat;
            if (banc.Palier >= etat.Cycle.PaliersOuverts) return etat;
            if (etat.Cycle.Bancs.EssayerDeLire(bancId, out var deja) && deja.Place > 0) return etat;
            var cout = Economie.CoutDeConviction(etat, banc);
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    Bancs = etat.Cycle.Bancs.Avec(bancId, new EtatBanc(1, 0)),
                },
                Permanent = etat.Permanent with
                {
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Recrutement, 1),
                },
            };
        }

        /// <summary>
        /// Acheter une place de plus : l'achat répétable de la boucle, ×1.15. De la
        /// PLACE, pas des individus : la population monte seule vers le plafond.
        /// </summary>
        public static EtatJeu AcheterPlace(EtatJeu etat, string bancId)
        {
            var banc = Paliers.BancParId(bancId);
            if (banc == null) return etat;
            if (banc.Palier >= etat.Cycle.PaliersOuverts) return etat;
            if (!etat.Cycle.Bancs.EssayerDeLire(bancId, out var avant) || avant.Place <= 0) return etat;
            var cout = Economie.CoutDePlace(etat, banc, avant.Place);
            if (etat.Cycle.ManaCourant.Lt(cout)) return etat;
            return etat with
            {
                Cycle = etat.Cycle with
                {
                    ManaCourant = etat.Cycle.ManaCourant.Sub(cout),
                    Bancs = etat.Cycle.Bancs.Avec(bancId, new EtatBanc(avant.Place + 1, avant.Effectif)),
                },
                Permanent = etat.Permanent with
                {
                    CompteursTechnique = Technique.CreditCompteur(etat.Permanent.CompteursTechnique, BrancheTechnique.Amelioration, cout.ToNumber()),
                },
            };
        }

        /// <summary>L'éclosion choisie : le seul geste volontaire du jeu (§10.1).</summary>
        public static EtatJeu Eclore(EtatJeu etat) => Eclosion.Eclore(etat, true);
    }
}
