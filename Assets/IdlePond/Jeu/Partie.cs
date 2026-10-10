using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Noyau.Donnees;

namespace IdlePond.Jeu
{
    /// L'absence qui vient d'être créditée, telle que l'écran de retour la montre.
    public sealed record AbsenceCreditee(double SecondesCreditees);

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

        /// Succès à annoncer : une ligne qui apparaît et s'efface, jamais une fenêtre.
        public IReadOnlyList<string> AAnnoncer => aAnnoncer;

        public event Action<EtatJeu> EtatChange;
        public event Action<IReadOnlyList<string>> SuccesDeclenches;
        public event Action<AbsenceCreditee> RetourAffiche;

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

        public void Creuser() => Appliquer(Reducteur.Creuser);

        public void Convaincre(string espece) => Appliquer(etat => Reducteur.Debloquer(etat, espece));

        public void Monter(string espece) => Appliquer(etat => Reducteur.Ameliorer(etat, espece));

        public void Grandir() => Appliquer(Reducteur.Grandir);

        public void AcheterUneAmelioration(string id) => Appliquer(etat => Reducteur.AcheterUneAmelioration(etat, id));

        public void AcheterUnBonus(string id) => Appliquer(etat => Reducteur.AcheterUnBonus(etat, id));

        public void Renaitre() => Appliquer(Renaissance.Renaitre);

        void Appliquer(Func<EtatJeu, EtatJeu> acte)
        {
            var apres = acte(Etat);
            if (ReferenceEquals(apres, Etat)) return;
            Etat = apres;
            EtatChange?.Invoke(apres);
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
            var resultat = Reducteur.TickDetaille(Etat, dt);
            // Les verbes des techniques jouent après le pas, jamais dedans (`Automatismes`).
            Etat = Automatismes.Appliquer(RegleDesSucces.EnregistrerIntervalleDeSucces(resultat.Etat, resultat.Declenches));
            EtatChange?.Invoke(Etat);
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
            DernierInstantMs = maintenant;
            // Un recul d'horloge ne crédite rien : l'état rendu est alors la même référence.
            var change = !ReferenceEquals(credit.Etat, Etat);
            Etat = credit.Etat;
            Retour = credit.SecondesCreditees >= Constantes.SECONDES_MINIMALES_POUR_ANNONCER_LE_RETOUR
                ? new AbsenceCreditee(credit.SecondesCreditees)
                : null;
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
            Remplacer(NouvelEtat(horloge));
            Sauvegarder(dossier);
            return copie;
        }

        public void OublierAnnonce(string id) => aAnnoncer.RemoveAll(autre => autre == id);

        public void OublierRetour() => Retour = null;

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
