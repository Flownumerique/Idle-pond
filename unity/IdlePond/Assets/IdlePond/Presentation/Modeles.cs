/*
 * IdlePond — ce que chaque panneau affiche, calculé hors d'Unity.
 *
 * Chaque modèle est la traduction d'un composant React de `src/ui/` : mêmes
 * conditions, mêmes chaînes, même ordre. La couche Unity (`Interface/`) ne fait
 * plus que les dessiner — elle ne décide de rien, n'écrit aucune phrase, et ne
 * lit le noyau qu'à travers ce fichier.
 *
 * C'est ce qui rend l'écran testable sans éditeur : le test de canon « aucun
 * terme de couche ne sort dans un texte affiché » parcourt ces modèles, c'est-à-
 * dire les chaînes réellement produites, et pas seulement le source.
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Donnees;
using IdlePond.Nombres;
using IdlePond.Noyau;

namespace IdlePond.Presentation
{
    /* ─── L'en-tête ─────────────────────────────────────────────────────────── */

    public sealed record ModeleDEnTete(string Titre, string LibelleFoi, string Foi, string LibelleEclosions, string Eclosions);

    /* ─── La jauge (Contenance.tsx) ─────────────────────────────────────────── */

    public sealed record ModeleDeContenance(
        string Mana,
        string Plafond,
        double Part,
        bool Trouble,
        bool Plein,
        string Debit,
        string MessagePlein,
        string MessageBloque);

    /* ─── La mare (Mare.tsx) ────────────────────────────────────────────────── */

    public sealed record ModeleDeBanc(
        string BancId,
        bool Convaincu,
        string Nom,
        string Profondeur,
        string Effectif,
        string Production,
        string Action,
        string Cout,
        bool Payable);

    public sealed record ModeleDeCreusement(string Libelle, string Cout, bool Possible);

    public sealed record ModeleDeMare(string Titre, IReadOnlyList<ModeleDeBanc> Bancs, ModeleDeCreusement Creusement);

    /* ─── L'éclosion (Eclosion.tsx) ─────────────────────────────────────────── */

    public sealed record ModeleDEclosion(
        string Bouton,
        bool Bloque,
        string Phrase,
        string LibelleFoi,
        string Foi,
        string LibelleDensite,
        string Densite,
        string Arbitrage,
        string Rentrer,
        string Rester);

    /* ─── Le détail de captation (Captation.tsx) ────────────────────────────── */

    public sealed record LigneAffichee(string Terme, string Valeur, string Source);

    public sealed record ModeleDeCaptation(
        string Nom,
        string Profondeur,
        string Fermer,
        IReadOnlyList<LigneAffichee> Lignes,
        string LibelleNatif,
        string Natif,
        string ParSeconde,
        string LibelleEau,
        IReadOnlyList<LigneAffichee> LignesAcclimatees,
        string LibelleAcclimate,
        string Acclimate);

    /* ─── Les succès (Succes.tsx) ───────────────────────────────────────────── */

    public sealed record SuccesAcquisAffiche(string Id, string Nom, string Rapport);

    public sealed record SuccesEnCoursAffiche(string Id, string Nom, string Condition, double? Progression);

    public sealed record ModeleDesSucces(
        string Titre,
        IReadOnlyList<SuccesAcquisAffiche> Acquis,
        string Vide,
        string TitreEnChemin,
        IReadOnlyList<SuccesEnCoursAffiche> EnChemin,
        string TitrePlusLoin,
        IReadOnlyList<string> PlusLoin,
        string TitreSecrets,
        int Secrets);

    public static class Modeles
    {
        private static string AssiseAffichee => Assises.Liste[0].Id;

        public static ModeleDEnTete EnTete(EtatJeu etat) => new ModeleDEnTete(
            "IdlePond",
            "Foi",
            Format.Montant(etat.Permanent.Foi),
            "retours dans l’œuf",
            etat.Permanent.NombreEclosions.ToString(System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// §6.4 : la contenance limite le STOCK. GDD §2.4 : l'alerte est « un effet, pas
        /// un texte » — aucun compte à rebours, jamais.
        /// </summary>
        public static ModeleDeContenance Contenance(EtatJeu etat)
        {
            var plein = Economie.EstSature(etat);
            return new ModeleDeContenance(
                Format.Montant(etat.Cycle.ManaCourant),
                "sur " + Format.Montant(Economie.Contenance(etat)),
                Economie.PartDeContenance(etat),
                Economie.EauTroublee(etat),
                plein,
                // Saturé, la captation s'arrête : afficher un débit ferait mentir le
                // seul chiffre de cet écran.
                plein ? "+0 / s" : "+" + Format.Montant(Economie.ProductionTotaleParSeconde(etat)) + " / s",
                plein ? "tu ne captes plus rien" : null,
                Economie.EstBloque(etat)
                    ? "Il n’y a plus de quoi porter le prochain creusement. Rien n’empêche de continuer à faire venir du monde."
                    : null);
        }

        /// <summary>
        /// L'écran ne dit jamais « palier » ni « assise » (§3). Deux puits, deux verbes
        /// (GDD §4.1) : rouvrir une galerie effondrée n'est pas creuser.
        /// </summary>
        public static ModeleDeMare Mare(EtatJeu etat)
        {
            var mana = etat.Cycle.ManaCourant;
            var plafond = Economie.Contenance(etat);
            var bancs = new List<ModeleDeBanc>();

            for (var palier = 0; palier < etat.Cycle.PaliersOuverts && palier < Paliers.Liste.Count; palier += 1)
            {
                foreach (var banc in Paliers.Liste[palier].Bancs)
                {
                    etat.Cycle.Bancs.EssayerDeLire(banc.Id, out var vivant);
                    var place = vivant?.Place ?? 0;
                    var cout = place == 0 ? Economie.CoutDeConviction(etat, banc) : Economie.CoutDePlace(etat, banc, place);
                    var convaincu = place > 0;
                    bancs.Add(new ModeleDeBanc(
                        banc.Id,
                        convaincu,
                        convaincu ? Format.NomDeLEspece(banc.Espece) : "un banc s’attarde",
                        Format.Profondeur(banc.Palier),
                        convaincu
                            ? NombreJs.ToFixed(vivant?.Effectif ?? 0, 1) + " / " + NombreJs.VersTexte(Population.EffectifCible(place))
                            : null,
                        convaincu ? "+" + Format.Montant(Economie.ProductionDuBanc(etat, banc)) + " / s" : null,
                        convaincu ? "Faire de la place" : "Convaincre",
                        Format.Cout(cout),
                        mana.Gte(cout) && cout.Lte(plafond)));
                }
            }

            var coutDuCreusement = Economie.CoutDeDescente(etat, etat.Cycle.PaliersOuverts);
            var toutCreuse = Economie.ToutEstCreuse(etat);
            var possible = !toutCreuse && coutDuCreusement.Lte(plafond) && !mana.Lt(coutDuCreusement);
            var creusement = toutCreuse
                ? new ModeleDeCreusement("Il n’y a plus de roche à ouvrir ici", null, false)
                : new ModeleDeCreusement(
                    Economie.EstUnAmenagement(etat, etat.Cycle.PaliersOuverts) ? "Rendre le fond habitable" : "Creuser plus bas",
                    Format.Cout(coutDuCreusement),
                    possible);

            return new ModeleDeMare(Format.NomDeLAssise(AssiseAffichee), bancs, creusement);
        }

        /// <summary>
        /// Le gain prévu ne s'affiche PAS en permanence : « Lire l'eau » est un nœud
        /// verbe de la v0.4. Il se lit ici, au moment de décider, et nulle part ailleurs.
        /// </summary>
        public static ModeleDEclosion Eclosion(EtatJeu etat) => new ModeleDEclosion(
            "Rentrer dans l’œuf",
            Economie.EstBloque(etat),
            "Tout ce qui vit ici restera ici. Ce que tu as appris te suivra.",
            "Foi que tes fidèles ont émise",
            Format.Montant(Noyau.Eclosion.GainDeFoiPrevu(etat)),
            "Ce que la mare gardera de ta charge",
            "plus dense",
            "Rester plus longtemps fait monter la Foi. Partir maintenant fait descendre plus bas.",
            "Rentrer",
            "Rester");

        /// <summary>
        /// Les DEUX canaux du GDD §3, et séparément : les additionner cacherait qu'une
        /// part de ce qu'on capte ne vient pas du peuple, et que peupler la fait baisser.
        /// </summary>
        public static ModeleDeCaptation Captation(EtatJeu etat, string bancId)
        {
            var banc = Paliers.BancParId(bancId);
            if (banc == null) return null;
            LigneAffichee Afficher(LigneDeCaptation ligne) =>
                new LigneAffichee(Termes.Identifiant(ligne.Terme), Format.ValeurDeLigne(ligne.Valeur), Format.SourceDuTerme(ligne.Source));

            return new ModeleDeCaptation(
                Format.NomDeLEspece(banc.Espece),
                Format.Profondeur(banc.Palier),
                "fermer",
                Economie.DetailDeCaptation(etat, banc).Select(Afficher).ToArray(),
                "ce qu’ils te donnent",
                Format.Montant(Economie.ProductionDuBanc(etat, banc)),
                "par seconde",
                "et l’eau, à part",
                Economie.DetailDuCanalAcclimate(etat, banc.Palier).Select(Afficher).ToArray(),
                "ce que tu prends à l’eau",
                Format.Montant(Economie.ProductionAcclimateeDuPalier(etat, banc.Palier)));
        }

        /// <summary>
        /// §8.3 — ouvert : nom, condition, barre ; fermé : nom seul ; secret :
        /// emplacement vide. Groupés plutôt qu'entrelacés, ce qui est ARRIVÉ devant.
        /// </summary>
        public static ModeleDesSucces Succes(EtatJeu etat)
        {
            var assise = AssiseAffichee;
            var liste = SystemeDeSucces.SuccesListables(etat, assise);

            var acquis = liste.Where(e => e.Acquis).Reverse().Select(e =>
            {
                var texte = TextesProvisoires.TexteDuSucces(e.Succes.Id);
                return new SuccesAcquisAffiche(e.Succes.Id, texte.Nom, texte.Rapport);
            }).ToArray();

            var enChemin = liste.Where(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Ouvert).Select(e =>
            {
                var texte = TextesProvisoires.TexteDuSucces(e.Succes.Id);
                return new SuccesEnCoursAffiche(e.Succes.Id, texte.Nom, texte.Condition, SystemeDeSucces.ProgressionVersLeSucces(etat, e.Succes));
            }).ToArray();

            var plusLoin = liste.Where(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Ferme)
                .Select(e => TextesProvisoires.TexteDuSucces(e.Succes.Id).Nom).ToArray();

            var secrets = liste.Count(e => !e.Acquis && e.Visibilite == VisibiliteDeSucces.Secret);

            return new ModeleDesSucces(
                "Ce qui est arrivé — " + Format.NomDeLAssise(assise),
                acquis,
                "Rien encore. Ça vient vite.",
                "En chemin",
                enChemin,
                "Plus loin",
                plusLoin,
                "·",
                secrets);
        }

        /// <summary>On ne punit jamais l'absence, et on ne la fête pas non plus.</summary>
        public static string Retour(double secondesCreditees) =>
            Format.NomDeLAssiseCapitale(AssiseAffichee) + " a tourné sans toi pendant " + Format.Duree(secondesCreditees) + ".";

        /// <summary>§8.2 : une ligne qui apparaît et s'efface. Jamais une fenêtre.</summary>
        public static string Annonce(string succesId) => TextesProvisoires.TexteDuSucces(succesId).Rapport;

        /// <summary>Combien d'annonces l'écran montre à la fois — les trois dernières.</summary>
        public const int AnnoncesVisibles = 3;

        /// <summary>Durée de vie d'une annonce à l'écran.</summary>
        public const double DureeDUneAnnonceMs = 6000;
    }
}
