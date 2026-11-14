using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Jeu
{
    /// L'absence qui vient d'être créditée, telle que l'écran de retour la montre.
    public sealed record AbsenceCreditee(double SecondesCreditees);

    /// <summary>
    /// Ce qu'un toucher a fait (2026-11-14) : le mana gagné, ce qu'il a ajouté à ce qu'on peut
    /// porter — l'acquis de séjour avance à chaque toucher —, et si la mare était pleine quand on
    /// l'a touchée. Ignoré ou sans effet : zéro, zéro, non.
    /// </summary>
    public readonly struct GainDuToucher
    {
        public Decimal Mana { get; }
        public Decimal APorter { get; }
        public bool Plein { get; }
        public GainDuToucher(Decimal mana, Decimal aPorter, bool plein) { Mana = mana; APorter = aPorter; Plein = plein; }
        public static readonly GainDuToucher Rien = new GainDuToucher(Decimal.Zero, Decimal.Zero, false);
    }

    /// <summary>
    /// Ce que l'absence a rapporté, pour l'écran de retour (2026-11-13). Rien n'est calculé
    /// ici qui ne se lise sur les deux états, celui du départ et celui du retour.
    ///
    ///   SecondesAbsentes   — le temps réellement écoulé ; `SecondesCreditees` en est la part
    ///                        comptée, bornée par le plafond hors ligne.
    ///   ManaGagne          — ce que le stock a pris. Zéro s'il était déjà plein.
    ///   ContenanceGagnee   — ce que la contenance a pris par le séjour : c'est elle qui
    ///                        rouvre la descente.
    ///   StockPlein         — le mana touche la contenance : une partie de l'absence s'est
    ///                        perdue vers l'ambiant, il est temps de dépenser.
    ///   DescenteRouverte   — le creux suivant était hors de portée au départ, il ne l'est plus.
    ///   PeutCreuser        — le creux suivant est payable tout de suite.
    ///   Succes             — les succès tombés pendant l'absence.
    /// </summary>
    public sealed record RapportDeRetour(
        double SecondesAbsentes,
        double SecondesCreditees,
        Decimal ManaGagne,
        Decimal ContenanceGagnee,
        bool StockPlein,
        bool DescenteRouverte,
        bool PeutCreuser,
        IReadOnlyList<string> Succes)
    {
        /// L'absence a dépassé le plafond hors ligne : le reste n'a pas compté.
        public bool Plafonnee => SecondesAbsentes > SecondesCreditees + 1;

        public static RapportDeRetour Entre(EtatJeu avant, EtatJeu apres, double secondesAbsentes, double secondesCreditees,
            IReadOnlyList<string> succes)
        {
            var gagne = apres.Cycle.ManaCourant.Sub(avant.Cycle.ManaCourant);
            var contenance = Economie.Contenance(apres).Sub(Economie.Contenance(avant));
            var toutEstCreuse = Economie.ToutEstCreuse(apres);
            return new RapportDeRetour(
                secondesAbsentes,
                secondesCreditees,
                gagne.Gt(0) ? gagne : Decimal.Zero,
                contenance.Gt(0) ? contenance : Decimal.Zero,
                StockPlein: Economie.EstSature(apres),
                DescenteRouverte: Economie.EstBloque(avant) && !Economie.EstBloque(apres),
                PeutCreuser: !toutEstCreuse && !Economie.EstBloque(apres)
                    && apres.Cycle.ManaCourant.Gte(Economie.CoutDeDescente(apres, apres.Cycle.PaliersOuverts)),
                Succes: succes ?? new string[0]);
        }
    }

    /// <summary>
    /// IdlePond — la partie. L'équivalent du magasin Zustand, et son contrat est le même :
    /// un MIROIR de `EtatJeu`, aucune logique métier (§5.3). Chaque acte du joueur est un
    /// appel au réducteur pur du noyau ; si une règle du jeu se retrouvait ici, elle serait
    /// invisible au simulateur et intestable — c'est exactement ce que le découpage cherche
    /// à empêcher.
    ///
    /// Un objet C# ordinaire, sans MonoBehaviour : elle se teste sans Unity, avec une
    /// `HorlogeFigee`. `Etat` est une référence vers un état immuable ; les actes
    /// remplacent la référence. Un acte que le noyau refuse (pas assez de mana) rend le
    /// MÊME état, sans exception, et n'émet rien : c'est au-dessus du noyau de ne pas le
    /// proposer.
    /// </summary>
    public sealed class Partie
    {
        readonly IHorloge horloge;
        readonly List<string> aAnnoncer = new List<string>();

        public Partie(IHorloge horloge, EtatJeu etat = null, long? dernierInstantMs = null)
        {
            this.horloge = horloge;
            Etat = etat ?? NouvelEtat(horloge);
            DernierInstantMs = dernierInstantMs ?? horloge.MaintenantMs();
        }

        public EtatJeu Etat { get; private set; }

        /// Dernier instant enregistré, pour le crédit hors ligne.
        public long DernierInstantMs { get; private set; }

        /// Le retour affiché, jusqu'à ce que le joueur l'écarte. Null si l'absence était
        /// trop brève pour valoir d'être annoncée.
        public AbsenceCreditee Retour { get; private set; }

        /// Le détail du même retour : ce que l'absence a rapporté. Null quand `Retour` l'est.
        public RapportDeRetour Rapport { get; private set; }

        /// Succès à annoncer : une ligne qui apparaît et s'efface, jamais une fenêtre.
        public IReadOnlyList<string> AAnnoncer => aAnnoncer;

        public event Action<EtatJeu> EtatChange;
        public event Action<IReadOnlyList<string>> SuccesDeclenches;
        public event Action<AbsenceCreditee> RetourAffiche;

        /// Ce qui vient de se passer (`Evenements`) : un achat, un creux, un seuil, un verbe
        /// qui a agi. Pour l'habillage et le son ; aucune règle n'en dépend.
        public event Action<IReadOnlyList<EvenementDeJeu>> EvenementsDeJeu;

        void Signaler(EtatJeu avant, EtatJeu apres, bool automatique = false)
        {
            if (EvenementsDeJeu == null) return;
            var evenements = Evenements.Entre(avant, apres, automatique);
            if (evenements.Count > 0) EvenementsDeJeu.Invoke(evenements);
        }

        /// <summary>
        /// L'état d'une nouvelle partie : ce que le jeu livre (`PALIERS_LIVRES`, l'assise I
        /// seule), pas les 62 paliers du simulateur. La graine vient de l'horloge — la
        /// seule source d'impureté autorisée.
        /// </summary>
        public static EtatJeu NouvelEtat(IHorloge horloge) =>
            Reducteur.EtatInitial(Horloge.GrainePourNouvellePartie(horloge), Assises.PALIERS_LIVRES);

        /// <summary>
        /// Le chemin du démarrage (§2) : charger, puis créditer l'absence, puis jouer. Le
        /// crédit est `Reprendre`, la même fonction qu'au retour de pause.
        /// </summary>
        public static Partie Ouvrir(IHorloge horloge, string dossier)
        {
            var chargement = Persistance.Charger(dossier, horloge);
            var partie = new Partie(horloge, chargement.Etat, chargement.DernierInstantMs);
            partie.Reprendre();
            return partie;
        }

        /* ─── Les actes du joueur ───────────────────────────────────────────────────*/

        /// Creuser, ou, avec « La file », demander le creux d'avance s'il n'est pas payable.
        public void Creuser() => Appliquer(Reducteur.DemanderUnCreux);

        public void Convaincre(string espece) => Appliquer(etat => Reducteur.Debloquer(etat, espece));

        public void Monter(string espece) => Appliquer(etat => Reducteur.Ameliorer(etat, espece));

        /// Autant de crans que le mana en paie, jusqu'au prochain seuil de l'espèce.
        public void MonterJusquAuSeuil(string espece) => Appliquer(etat => Reducteur.AmeliorerJusquAuSeuil(etat, espece));

        public void Grandir() => Appliquer(Reducteur.Grandir);

        public void AcheterUneAmelioration(string id) => Appliquer(etat => Reducteur.AcheterUneAmelioration(etat, id));

        /// Tout le Souffle payable, le rang le moins cher d'abord.
        public void ToutAmeliorer() => Appliquer(Reducteur.AcheterToutesLesAmeliorations);

        public void AcheterUnBonus(string id) => Appliquer(etat => Reducteur.AcheterUnBonus(etat, id));

        public void Renaitre() => Appliquer(Renaissance.Renaitre);

        /// Au plus ce nombre de touchers dans une fenêtre glissante d'une seconde.
        public const int TOUCHERS_PAR_SECONDE = Constantes.TOUCHERS_PAR_SECONDE_AU_PLUS;

        readonly Queue<long> derniersTouchers = new Queue<long>(TOUCHERS_PAR_SECONDE);

        /// <summary>
        /// Toucher la mare (specs du 2026-10-11 et du 2026-11-14) : `Reducteur.Toucher`, et ce
        /// qu'il a fait, pour le chiffre qui s'envole. Au-delà de dix touchers dans la même
        /// seconde, lue sur l'horloge de la partie, le toucher est ignoré : rien n'est rendu,
        /// rien n'est émis. La limite vit ici, pas dans le noyau, qui ne voit jamais le temps
        /// de mur. Un toucher compte dès qu'il change l'état, mare pleine comprise.
        /// </summary>
        public GainDuToucher Toucher()
        {
            var maintenant = horloge.MaintenantMs();
            while (derniersTouchers.Count > 0 && maintenant - derniersTouchers.Peek() >= 1000) derniersTouchers.Dequeue();
            if (derniersTouchers.Count >= TOUCHERS_PAR_SECONDE) return GainDuToucher.Rien;
            var avant = Etat;
            var (apres, gain) = Reducteur.Toucher(avant);
            if (ReferenceEquals(apres, avant)) return GainDuToucher.Rien;
            derniersTouchers.Enqueue(maintenant);
            Etat = apres;
            EtatChange?.Invoke(apres);
            // Plein : la mare l'était AVANT le toucher (le Tick plafonne le mana exactement sur la
            // contenance). Après lui, elle ne l'est presque jamais : en début de vie, un toucher
            // fait cent fois plus de place qu'il n'apporte de mana.
            var plein = avant.Cycle.ManaCourant.Gte(Economie.Contenance(avant));
            return new GainDuToucher(gain, Economie.Contenance(apres).Sub(Economie.Contenance(avant)), plein);
        }

        void Appliquer(Func<EtatJeu, EtatJeu> acte)
        {
            var apres = acte(Etat);
            if (ReferenceEquals(apres, Etat)) return;
            var avant = Etat;
            Etat = apres;
            EtatChange?.Invoke(apres);
            Signaler(avant, apres);
        }

        /* ─── Le temps ──────────────────────────────────────────────────────────────*/

        /// <summary>
        /// Un pas de `dt` secondes : le Tick du noyau, plus l'enregistrement de
        /// l'intervalle entre deux succès. Cette observation se fait ICI, à la cadence où
        /// elle est faite — pas dans le noyau, dont le pas doit rester homogène (§11) ; et
        /// le crédit hors ligne ne la fait pas, parce qu'il ne peut pas prétendre savoir
        /// qu'un succès est tombé il y a six heures.
        ///
        /// Un `dt` qui n'est pas strictement positif — recul d'horloge, NaN — est ignoré,
        /// jamais rattrapé à l'envers.
        ///
        /// Le temps joué est du temps déjà compté : le dernier instant avance avec lui, sans
        /// quoi un retour de pause créditerait une seconde fois tout ce qui a été joué
        /// depuis la dernière sauvegarde.
        /// </summary>
        public void Avancer(double dt)
        {
            if (!(dt > 0)) return;
            DernierInstantMs = Math.Max(DernierInstantMs, horloge.MaintenantMs());
            var avant = Etat;
            var resultat = Reducteur.TickDetaille(Etat, dt);
            // Les verbes des techniques jouent après le pas, jamais dedans (`Automatismes`).
            var apresLePas = RegleDesSucces.EnregistrerIntervalleDeSucces(resultat.Etat, resultat.Declenches);
            Etat = Automatismes.Appliquer(apresLePas);
            EtatChange?.Invoke(Etat);
            Signaler(avant, apresLePas);
            // Ce que les verbes ont fait est signalé à part, marqué comme tel.
            if (!ReferenceEquals(apresLePas, Etat)) Signaler(apresLePas, Etat, automatique: true);
            if (resultat.Declenches.Count == 0) return;
            aAnnoncer.AddRange(resultat.Declenches);
            SuccesDeclenches?.Invoke(resultat.Declenches);
        }

        /// <summary>
        /// Un seul appel à Tick pour toute l'absence. Rien ne s'est dégradé. Au démarrage
        /// comme au retour de pause : la même fonction.
        /// </summary>
        public void Reprendre()
        {
            var maintenant = horloge.MaintenantMs();
            var credit = HorsLigne.Crediter(Etat, DernierInstantMs, maintenant);
            var secondesAbsentes = Math.Max(0, (maintenant - DernierInstantMs) / 1000.0);
            DernierInstantMs = maintenant;
            // Un recul d'horloge ne crédite rien : l'état rendu est alors la même référence.
            var change = !ReferenceEquals(credit.Etat, Etat);
            var avant = Etat;
            Etat = credit.Etat;
            Retour = credit.SecondesCreditees >= Constantes.SECONDES_MINIMALES_POUR_ANNONCER_LE_RETOUR
                ? new AbsenceCreditee(credit.SecondesCreditees)
                : null;
            Rapport = Retour == null
                ? null
                : RapportDeRetour.Entre(avant, Etat, secondesAbsentes, credit.SecondesCreditees, credit.Declenches);
            // Un succès tombé pendant l'absence s'annonce au retour, comme les autres.
            if (credit.Declenches != null && credit.Declenches.Count > 0)
            {
                aAnnoncer.AddRange(credit.Declenches);
                SuccesDeclenches?.Invoke(credit.Declenches);
            }
            if (change) EtatChange?.Invoke(Etat);
            if (Retour != null) RetourAffiche?.Invoke(Retour);
        }

        /// Remplace l'état d'un bloc (une partie chargée, un état d'essai).
        public void Remplacer(EtatJeu etat)
        {
            Etat = etat;
            DernierInstantMs = horloge.MaintenantMs();
            EtatChange?.Invoke(etat);
        }

        /// <summary>
        /// Recommencer de zéro, depuis les réglages. L'état courant est d'abord écrit puis
        /// copié à part (`Persistance.CopierAvantReinitialisation`) — la sauvegarde du disque
        /// peut avoir dix secondes de retard, et c'est l'état d'à l'instant que le joueur
        /// voudrait retrouver. Puis la partie neuve remplace l'ancienne, et s'écrit aussitôt :
        /// quitter juste après ne doit pas ressusciter l'ancienne. Rend le chemin de la copie.
        /// </summary>
        public string Reinitialiser(string dossier)
        {
            Sauvegarder(dossier);
            var copie = Persistance.CopierAvantReinitialisation(dossier, horloge.MaintenantMs());
            aAnnoncer.Clear();
            Retour = null;
            Rapport = null;
            Remplacer(NouvelEtat(horloge));
            Sauvegarder(dossier);
            return copie;
        }

        public void OublierAnnonce(string id) => aAnnoncer.RemoveAll(autre => autre == id);

        public void OublierRetour()
        {
            Retour = null;
            Rapport = null;
        }

        /* ─── La sauvegarde ─────────────────────────────────────────────────────────*/

        /// <summary>
        /// Écrit la partie, et prend l'instant de l'écriture pour dernier instant : c'est de
        /// là que le crédit hors ligne repart au retour. Sans cela, une session jouée puis
        /// quittée serait créditée une seconde fois comme absence.
        /// </summary>
        public void Sauvegarder(string dossier)
        {
            DernierInstantMs = horloge.MaintenantMs();
            Persistance.Sauvegarder(dossier, Etat, DernierInstantMs);
        }
    }
}
