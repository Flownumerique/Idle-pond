/*
 * L'écran des succès (§8.3). Port de `Succes.tsx`.
 *
 * | État    | Ce que le joueur voit                    |
 * |---------|------------------------------------------|
 * | Ouvert  | Nom + condition + barre de progression   |
 * | Fermé   | Nom seul, condition masquée              |
 * | Secret  | Emplacement vide, rien d'autre           |
 *
 * Les trois états sont groupés plutôt qu'entrelacés, et ce qui est ARRIVÉ passe
 * devant.
 */
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueSucces
    {
        private readonly Label _titre;
        private readonly VisualElement _acquis;
        private readonly Label _vide;
        private readonly VisualElement _groupeEnChemin;
        private readonly Label _titreEnChemin;
        private readonly VisualElement _enChemin;
        private readonly VisualElement _groupePlusLoin;
        private readonly Label _titrePlusLoin;
        private readonly VisualElement _plusLoin;
        private readonly VisualElement _groupeSecrets;
        private readonly Label _titreSecrets;
        private readonly VisualElement _secrets;
        private readonly Dictionary<string, VisualElement> _barres = new Dictionary<string, VisualElement>();
        private string _signature;

        public VueSucces()
        {
            Racine = Boite("pile-3");
            _titre = Texte("", "titre-2");
            Racine.Add(_titre);

            var contenu = Boite("pile-4");
            _acquis = Boite();
            _vide = Etiquette("", "jour-tu", "petit");
            contenu.Add(_acquis);
            contenu.Add(_vide);

            _groupeEnChemin = Groupe(out _titreEnChemin, out _enChemin, false);
            _groupePlusLoin = Groupe(out _titrePlusLoin, out _plusLoin, true);
            _groupeSecrets = Groupe(out _titreSecrets, out _secrets, true);
            // Un emplacement secret ne se lit pas : il se voit. Rien à annoncer.
            _secrets.pickingMode = PickingMode.Ignore;
            contenu.Add(_groupeEnChemin);
            contenu.Add(_groupePlusLoin);
            contenu.Add(_groupeSecrets);
            Racine.Add(contenu);
        }

        public VisualElement Racine { get; }

        private static VisualElement Groupe(out Label titre, out VisualElement liste, bool enveloppe)
        {
            var groupe = Boite("pile-1");
            titre = Etiquette("", "majuscules");
            liste = enveloppe ? Boite("enveloppe") : Boite();
            groupe.Add(titre);
            groupe.Add(liste);
            return groupe;
        }

        public void Rafraichir(EtatJeu etat)
        {
            var modele = Modeles.Succes(etat);
            Ecrire(_titre, modele.Titre);

            // On ne reconstruit que si la composition des groupes a changé ; entre
            // deux succès, seules les barres de progression bougent.
            var signature = string.Join("|", modele.Acquis.Select(a => a.Id)) + "#"
                            + string.Join("|", modele.EnChemin.Select(e => e.Id)) + "#"
                            + string.Join("|", modele.PlusLoin) + "#" + modele.Secrets;
            if (signature != _signature)
            {
                _signature = signature;
                Reconstruire(modele);
            }

            foreach (var enCours in modele.EnChemin)
            {
                if (!_barres.TryGetValue(enCours.Id, out var barre)) continue;
                Afficher(barre.parent, enCours.Progression != null);
                if (enCours.Progression != null) Largeur(barre, enCours.Progression.Value);
            }
        }

        private void Reconstruire(ModeleDesSucces modele)
        {
            _acquis.Clear();
            foreach (var acquis in modele.Acquis)
            {
                var carte = Boite("succes-acquis");
                carte.Add(Texte(acquis.Nom, "jour", "petit"));
                var rapport = Texte(acquis.Rapport, "jour-tu", "petit", "italique");
                rapport.style.marginTop = 2;
                carte.Add(rapport);
                _acquis.Add(carte);
            }
            Afficher(_vide, modele.Acquis.Count == 0);
            Ecrire(_vide, modele.Vide);

            _enChemin.Clear();
            _barres.Clear();
            foreach (var enCours in modele.EnChemin)
            {
                var carte = Boite("succes-en-cours");
                carte.Add(Texte(enCours.Nom, "jour-doux", "petit"));
                var condition = Etiquette(enCours.Condition, "jour-tu", "tres-petit");
                condition.style.marginTop = 2;
                carte.Add(condition);
                var progression = Boite("progression");
                var remplie = Boite("progression-remplie");
                progression.Add(remplie);
                carte.Add(progression);
                _barres[enCours.Id] = remplie;
                _enChemin.Add(carte);
            }
            Afficher(_groupeEnChemin, modele.EnChemin.Count > 0);
            Ecrire(_titreEnChemin, modele.TitreEnChemin);

            _plusLoin.Clear();
            foreach (var nom in modele.PlusLoin) _plusLoin.Add(Texte(nom, "succes-ferme"));
            Afficher(_groupePlusLoin, modele.PlusLoin.Count > 0);
            Ecrire(_titrePlusLoin, modele.TitrePlusLoin);

            _secrets.Clear();
            for (var i = 0; i < modele.Secrets; i += 1) _secrets.Add(Boite("succes-secret"));
            Afficher(_groupeSecrets, modele.Secrets > 0);
            Ecrire(_titreSecrets, modele.TitreSecrets);
        }
    }
}
