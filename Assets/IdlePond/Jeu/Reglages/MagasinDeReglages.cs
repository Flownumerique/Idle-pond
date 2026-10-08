using System;
using System.IO;
using System.Text;

namespace IdlePond.Jeu
{
    /// <summary>
    /// IdlePond — le magasin des réglages : les réglages courants, leur fichier, et
    /// l'événement que l'interface, le son et l'affichage écoutent.
    ///
    /// Un changement notifie TOUT DE SUITE — le joueur voit et entend l'effet sous son
    /// doigt — mais n'est écrit qu'après `DELAI_D_ENREGISTREMENT_S` sans autre changement :
    /// dix crans de volume d'affilée font une seule écriture. La `Boucle` appelle
    /// `Avancer` à chaque image et `Enregistrer` en pause et à la sortie.
    ///
    /// Le dossier est INJECTÉ, comme pour `Persistance` : `reglages.json` vit à côté de
    /// `idlepond.json`, et un atelier qui redirige la sauvegarde redirige aussi ceci.
    /// </summary>
    public sealed class MagasinDeReglages
    {
        public const string NOM_DU_FICHIER = "reglages.json";
        public const double DELAI_D_ENREGISTREMENT_S = 0.5;
        const string SUFFIXE_TEMPORAIRE = ".tmp";

        readonly string dossier;
        bool aEcrire;
        double depuisLeChangement;

        MagasinDeReglages(string dossier, Reglages courants, bool illisible)
        {
            this.dossier = dossier;
            Courants = courants;
            FichierIllisible = illisible;
        }

        public Reglages Courants { get; private set; }

        /// Vrai si un fichier existait mais n'a pas pu être lu : les défauts sont en place,
        /// et le fichier ne sera remplacé qu'au prochain changement.
        public bool FichierIllisible { get; }

        public event Action<Reglages> Change;

        public string Chemin => Path.Combine(dossier, NOM_DU_FICHIER);

        public static MagasinDeReglages Ouvrir(string dossier)
        {
            var chemin = Path.Combine(dossier, NOM_DU_FICHIER);
            if (!File.Exists(chemin)) return new MagasinDeReglages(dossier, Reglages.ParDefaut, false);
            try
            {
                var reglages = Reglages.Deserialiser(File.ReadAllText(chemin, Encoding.UTF8), out var lisible);
                return new MagasinDeReglages(dossier, reglages, !lisible);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return new MagasinDeReglages(dossier, Reglages.ParDefaut, true);
            }
        }

        /// Les nouveaux réglages, bornés. Rien ne se passe s'ils sont égaux aux courants.
        public void Modifier(Func<Reglages, Reglages> changement)
        {
            var nouveaux = changement(Courants).Borner();
            if (nouveaux == Courants) return;
            Courants = nouveaux;
            aEcrire = true;
            depuisLeChangement = 0;
            Change?.Invoke(nouveaux);
        }

        /// <summary>
        /// Le temps qui passe. Une écriture ratée lève (l'appelant la journalise) et sera
        /// retentée après un nouveau délai : le compteur est remis à zéro AVANT d'écrire.
        /// </summary>
        public void Avancer(double dt)
        {
            if (!aEcrire || !(dt > 0)) return;
            depuisLeChangement += dt;
            if (depuisLeChangement < DELAI_D_ENREGISTREMENT_S) return;
            depuisLeChangement = 0;
            Enregistrer();
        }

        /// Écrit maintenant ce qui attend, par un fichier temporaire puis un remplacement.
        public void Enregistrer()
        {
            if (!aEcrire) return;
            Directory.CreateDirectory(dossier);
            var cible = Chemin;
            var temporaire = cible + SUFFIXE_TEMPORAIRE;
            File.WriteAllText(temporaire, Reglages.Serialiser(Courants), new UTF8Encoding(false));
            if (File.Exists(cible)) File.Replace(temporaire, cible, null);
            else File.Move(temporaire, cible);
            aEcrire = false;
        }
    }
}
