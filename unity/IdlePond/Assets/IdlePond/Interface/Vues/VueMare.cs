/*
 * Le lieu, et les bancs qui l'habitent. Port de `Mare.tsx`.
 *
 * L'écran ne dit jamais « palier » ni « assise » (§3) : il dit le nom propre du
 * lieu et une profondeur en brasses. Le joueur ne choisit jamais quelle espèce
 * va où — le placement est fixé par l'auteur (§4.2). Il convainc, il fait de la
 * place, il creuse.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using IdlePond.Noyau;
using IdlePond.Presentation;
using UnityEngine.UIElements;
using static IdlePond.Interface.Elements;

namespace IdlePond.Interface.Vues
{
    internal sealed class VueMare
    {
        private sealed class VueBanc
        {
            public VisualElement Racine;
            public Label Nom;
            public Label Profondeur;
            public VisualElement Details;
            public Label Effectif;
            public Button Production;
            public Button Action;
            public Label Libelle;
            public Label Cout;
        }

        private readonly Action<string> _surConviction;
        private readonly Action<string> _surPlace;
        private readonly Action<string> _surCaptation;
        private readonly Label _titre;
        private readonly VisualElement _liste;
        private readonly Button _creuser;
        private readonly Label _libelleCreuser;
        private readonly Label _coutCreuser;
        private readonly List<VueBanc> _bancs = new List<VueBanc>();
        private readonly List<string> _identifiants = new List<string>();
        private IReadOnlyList<ModeleDeBanc> _modeles = Array.Empty<ModeleDeBanc>();

        public VueMare(Action<string> surConviction, Action<string> surPlace, Action surCreusement, Action<string> surCaptation)
        {
            _surConviction = surConviction;
            _surPlace = surPlace;
            _surCaptation = surCaptation;

            Racine = Boite("pile-2");
            _titre = Texte("", "titre-2");
            Racine.Add(_titre);
            _liste = Boite();
            Racine.Add(_liste);
            _creuser = BoutonAvecCout(surCreusement, out _libelleCreuser, out _coutCreuser, "bouton-creuser", "bouton-large");
            Racine.Add(_creuser);
        }

        public VisualElement Racine { get; }

        private VueBanc Construire(int index)
        {
            var vue = new VueBanc { Racine = Boite("banc") };
            var entete = Boite("ligne");
            vue.Nom = Texte("", "titre-3");
            vue.Profondeur = Chiffre("", "jour-tu", "tres-petit");
            entete.Add(vue.Nom);
            entete.Add(vue.Profondeur);
            vue.Racine.Add(entete);

            vue.Details = Boite("ligne-gauche", "details");
            vue.Effectif = Chiffre("", "jour-doux", "petit");
            vue.Production = Bouton(() => _surCaptation(_modeles[index].BancId), "", "bouton-lien", "petit");
            Polices.Chiffre(vue.Production);
            vue.Details.Add(vue.Effectif);
            vue.Details.Add(vue.Production);
            vue.Racine.Add(vue.Details);

            vue.Action = BoutonAvecCout(() =>
            {
                var modele = _modeles[index];
                if (modele.Convaincu) _surPlace(modele.BancId);
                else _surConviction(modele.BancId);
            }, out vue.Libelle, out vue.Cout, "action", "petit");
            vue.Racine.Add(vue.Action);
            return vue;
        }

        public void Rafraichir(EtatJeu etat)
        {
            var modele = Modeles.Mare(etat);
            _modeles = modele.Bancs;
            Ecrire(_titre, modele.Titre);

            // La liste ne se reconstruit que lorsqu'un creusement l'allonge ou qu'une
            // éclosion la vide : entre les deux, on ne fait que réécrire des textes.
            var identifiants = modele.Bancs.Select(b => b.BancId).ToList();
            if (!identifiants.SequenceEqual(_identifiants))
            {
                _liste.Clear();
                _bancs.Clear();
                _identifiants.Clear();
                _identifiants.AddRange(identifiants);
                for (var i = 0; i < modele.Bancs.Count; i += 1)
                {
                    var vue = Construire(i);
                    _bancs.Add(vue);
                    _liste.Add(vue.Racine);
                }
            }

            for (var i = 0; i < modele.Bancs.Count; i += 1)
            {
                var banc = modele.Bancs[i];
                var vue = _bancs[i];
                Ecrire(vue.Nom, banc.Nom);
                vue.Nom.EnableInClassList("jour-tu", !banc.Convaincu);
                Ecrire(vue.Profondeur, banc.Profondeur);
                Afficher(vue.Details, banc.Convaincu);
                Ecrire(vue.Effectif, banc.Effectif);
                Ecrire(vue.Production, banc.Production);
                Ecrire(vue.Libelle, banc.Action);
                Ecrire(vue.Cout, banc.Cout);
                vue.Action.SetEnabled(banc.Payable);
            }

            Ecrire(_libelleCreuser, modele.Creusement.Libelle);
            Afficher(_coutCreuser, modele.Creusement.Cout != null);
            Ecrire(_coutCreuser, modele.Creusement.Cout);
            _creuser.SetEnabled(modele.Creusement.Possible);
        }
    }
}
