using System;
using IdlePond.Noyau;
using UnityEngine.UIElements;
using Decimal = IdlePond.Noyau.Decimal;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Toi — le héros. Le quatrième achat (spec 2026-09-17), `Heros.tsx`.
    ///
    /// Même gabarit que la carte d'une espèce dans `Mare` : ce que grandir ajoute à un
    /// toucher (spec du 2026-10-11 : le héros ne capte plus par seconde), un bouton avec son coût. Le héros n'a pas de nom propre (GDD : « aucun nom
    /// propre définitif ») ; l'écran dit « toi ».
    /// </summary>
    public sealed class Heros
    {
        readonly Label production;
        readonly Label bonus;
        readonly BoutonDAchat grandir;

        public Heros(VisualElement racine, Action surCroissance)
        {
            racine.AddToClassList("carte");
            racine.AddToClassList("carte-souffle");

            // Le nom et la taille du héros sont dans la fiche, juste au-dessus : la carte ne
            // porte que ce qu'il capte et de quoi grandir.
            var milieu = Elements.Conteneur("rangee wrap");
            production = Elements.Texte("chiffre mana base");
            bonus = Elements.Texte("chiffre tu sm heros-bonus");
            milieu.Add(production);
            milieu.Add(bonus);
            racine.Add(milieu);

            grandir = new BoutonDAchat("bouton-souffle", E.GRANDIR, surCroissance);
            racine.Add(grandir.Racine);
        }

        public void Rafraichir(EtatJeu etat)
        {
            var niveauDuHeros = etat.Cycle.NiveauDuHeros;
            var prix = Economie.CoutDeCroissance(etat, niveauDuHeros);
            var payable = etat.Cycle.ManaCourant.Gte(prix) && prix.Lte(Economie.Contenance(etat));
            var pourcentage = (int)Decimal.JsRound((Economie.MultiplicateurDuHeros(etat) - 1) * 100);

            // Ce que grandir ajoute à un toucher : le niveau ET le multiplicateur global montent.
            var grandi = etat with { Cycle = etat.Cycle with { NiveauDuHeros = niveauDuHeros + 1 } };
            var ajout = Economie.ValeurDuToucher(grandi).Sub(Economie.ValeurDuToucher(etat));
            Elements.Poser(production, Format.Remplir(E.PAR_TOUCHER_AU_PROCHAIN, Format.Montant(ajout)));
            Elements.Montrer(bonus, pourcentage > 0);
            if (pourcentage > 0) Elements.Poser(bonus, Format.Remplir(E.BONUS_DU_HEROS, pourcentage));
            Elements.Poser(grandir.Cout, Format.Cout(prix));
            grandir.Progresser(etat.Cycle.ManaCourant, prix);
            grandir.Regler(payable);
        }
    }
}
