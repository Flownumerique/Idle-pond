/*
 * IdlePond — le magasin. Port de `src/etat/magasin.ts` (le store Zustand).
 *
 * MIROIR de `EtatJeu`, aucune logique métier (§5.3). Chaque acte du joueur est
 * un appel au réducteur pur du noyau : une règle du jeu qui se retrouverait ici
 * serait invisible au simulateur et intestable.
 *
 * Deux différences avec Zustand, et elles tiennent à la plateforme :
 *
 *   - la persistance n'a pas lieu à chaque `set`. Zustand écrit dans le
 *     localStorage à chaque tick de 100 ms ; un disque de téléphone ne mérite
 *     pas ce traitement. Le magasin écrit tout de suite après un ACTE du joueur
 *     (on ne perd jamais un achat), et l'hôte appelle `SauvegarderSiModifie` à
 *     son rythme pour le reste, et toujours à la mise en pause ;
 *   - le stockage est une interface (`IStockage`) : un fichier sur les
 *     plateformes natives, PlayerPrefs en WebGL, une table en mémoire dans les
 *     tests.
 *
 * Le format écrit est celui du web : `{ versionSave, contenu, dernierInstantMs }`.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Adaptateurs;
using IdlePond.Donnees;
using IdlePond.Noyau;

namespace IdlePond.Etat
{
    public interface IStockage
    {
        /// <summary>Le texte rangé sous cette clef, ou null.</summary>
        string Lire(string clef);

        void Ecrire(string clef, string valeur);

        void Effacer(string clef);
    }

    /// <summary>Un stockage en mémoire, pour les tests et le simulateur.</summary>
    public sealed class StockageEnMemoire : IStockage
    {
        private readonly Dictionary<string, string> _valeurs = new Dictionary<string, string>(StringComparer.Ordinal);

        public string Lire(string clef) => _valeurs.TryGetValue(clef, out var valeur) ? valeur : null;

        public void Ecrire(string clef, string valeur) => _valeurs[clef] = valeur;

        public void Effacer(string clef) => _valeurs.Remove(clef);
    }

    public sealed record RetourAffiche(double SecondesCreditees);

    public sealed class Magasin
    {
        /// <summary>La clef du localStorage de la version web. La même, pour qu'une save s'échange.</summary>
        public const string ClefDeSauvegarde = "idlepond";

        private readonly IStockage _stockage;
        private readonly IHorloge _horloge;
        private readonly List<string> _aAnnoncer = new List<string>();
        private bool _modifie;

        public Magasin(IStockage stockage, IHorloge horloge = null)
        {
            _stockage = stockage ?? throw new ArgumentNullException(nameof(stockage));
            _horloge = horloge ?? HorlogeSysteme.Instance;

            Etat = NouvellePartie();
            DernierInstantMs = _horloge.MaintenantMs();
            Rehydrater();
        }

        public EtatJeu Etat { get; private set; }

        /// <summary>Dernier instant enregistré, pour le crédit hors ligne.</summary>
        public double DernierInstantMs { get; private set; }

        public RetourAffiche Retour { get; private set; }

        /// <summary>Succès à annoncer : une ligne qui apparaît et s'efface, jamais une fenêtre.</summary>
        public IReadOnlyList<string> AAnnoncer => _aAnnoncer;

        /// <summary>Levé après chaque changement. L'UI s'y abonne pour se rafraîchir.</summary>
        public event Action Change;

        private EtatJeu NouvellePartie() => Reducteur.EtatInitial(Horloge.GrainePourNouvellePartie(_horloge), Assises.PaliersLivres);

        /* ─── Persistance ───────────────────────────────────────────────────── */

        /// <summary>Une save illisible ne doit pas bloquer le jeu sur un écran vide : on repart d'une partie neuve.</summary>
        private void Rehydrater()
        {
            string brut;
            try
            {
                brut = _stockage.Lire(ClefDeSauvegarde);
            }
            catch (Exception)
            {
                return;
            }
            if (brut == null) return;
            EssayerDImporter(brut);
        }

        /// <summary>
        /// Remplace la partie par une save au format web (`{ versionSave, contenu,
        /// dernierInstantMs }`). Rend faux, sans rien toucher, si elle est illisible.
        /// </summary>
        public bool EssayerDImporter(string texte)
        {
            try
            {
                var racine = Json.Lire(texte);
                var etat = Persistance.Deserialiser(Persistance.Enveloppe(racine), NouvellePartie());
                var instant = racine is JsonObjet objet && Json.EssayerNombre(objet.Lire("dernierInstantMs"), out var ms)
                    ? ms
                    : _horloge.MaintenantMs();
                Etat = etat;
                DernierInstantMs = instant;
                _modifie = true;
                Change?.Invoke();
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                // Une migration manquante : une save d'une version qu'on ne sait pas lire.
                return false;
            }
        }

        /// <summary>La save courante, telle qu'elle est écrite — et telle que le web la relirait.</summary>
        public string Exporter()
        {
            var save = Persistance.Serialiser(Etat);
            save.Poser("dernierInstantMs", new JsonNombre(DernierInstantMs));
            return save.EnTexte();
        }

        public void Sauvegarder()
        {
            _stockage.Ecrire(ClefDeSauvegarde, Exporter());
            _modifie = false;
        }

        public bool SauvegarderSiModifie()
        {
            if (!_modifie) return false;
            Sauvegarder();
            return true;
        }

        /// <summary>Efface la save et repart de l'œuf. N'est proposé qu'aux outils de l'éditeur.</summary>
        public void Reinitialiser()
        {
            _stockage.Effacer(ClefDeSauvegarde);
            Etat = NouvellePartie();
            DernierInstantMs = _horloge.MaintenantMs();
            Retour = null;
            _aAnnoncer.Clear();
            _modifie = false;
            Change?.Invoke();
        }

        /* ─── Écritures ─────────────────────────────────────────────────────── */

        private void Poser(EtatJeu etat, bool acte)
        {
            Etat = etat;
            _modifie = true;
            if (acte) Sauvegarder();
            Change?.Invoke();
        }

        /// <summary>Ce que la boucle écrit à chaque pas.</summary>
        public void Remplacer(EtatJeu etat)
        {
            DernierInstantMs = _horloge.MaintenantMs();
            Poser(etat, false);
        }

        /// <summary>Un acte qui n'est pas payable rend l'état inchangé : rien n'est écrit.</summary>
        private void Agir(Func<EtatJeu, EtatJeu> acte)
        {
            var suivant = acte(Etat);
            if (ReferenceEquals(suivant, Etat)) return;
            Poser(suivant, true);
        }

        public void Creuser() => Agir(Reducteur.Creuser);

        public void Convaincre(string banc) => Agir(e => Reducteur.Convaincre(e, banc));

        public void AcheterPlace(string banc) => Agir(e => Reducteur.AcheterPlace(e, banc));

        public void Eclore() => Agir(Reducteur.Eclore);

        /// <summary>Un seul appel à Tick pour toute l'absence. Rien ne s'est dégradé.</summary>
        public void Reprendre()
        {
            var maintenant = _horloge.MaintenantMs();
            var credit = HorsLigne.CrediterHorsLigne(Etat, DernierInstantMs, maintenant);
            Etat = credit.Etat;
            DernierInstantMs = maintenant;
            Retour = credit.SecondesCreditees >= Constantes.SecondesMinimalesPourAnnoncerLeRetour
                ? new RetourAffiche(credit.SecondesCreditees)
                : null;
            _modifie = true;
            Change?.Invoke();
        }

        public void Annoncer(IReadOnlyList<string> declenches)
        {
            if (declenches.Count == 0) return;
            _aAnnoncer.AddRange(declenches);
            Change?.Invoke();
        }

        public void OublierAnnonce(string id)
        {
            if (_aAnnoncer.RemoveAll(autre => autre == id) > 0) Change?.Invoke();
        }

        public void OublierRetour()
        {
            if (Retour == null) return;
            Retour = null;
            Change?.Invoke();
        }

        /// <summary>Les annonces, sans doublon, dans l'ordre d'arrivée.</summary>
        public IReadOnlyList<string> AnnoncesDistinctes() => _aAnnoncer.Distinct().ToArray();
    }
}
