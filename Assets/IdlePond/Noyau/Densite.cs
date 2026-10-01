using System;
using System.Collections.Generic;

namespace IdlePond.Noyau
{
    /// <summary>
    /// IdlePond — densité.
    ///
    /// Tier 0, invariant : la densité ne redescend pas. Elle est monotone croissante
    /// par palier et survit à la renaissance. Rien dans ce module ne doit pouvoir la
    /// faire baisser, et le test de canon parcourt les 15 cycles du simulateur pour
    /// s'en assurer.
    ///
    /// §6.5 : le gain de densité est indexé sur la production de pic du cycle, pas
    /// sur la profondeur. Elle n'a plus qu'UN débouché : la production (§10), via
    /// `Multiplicateur(DuSejour(etat))`, au même titre que le multiplicateur de
    /// profondeur. Voir le commentaire de `DuSejour` pour ce qui interdit qu'une
    /// autre grandeur porte ce nom.
    ///
    /// Elle a eu deux autres débouchés, retirés tous deux pour la même raison : la
    /// densité vaut `pointe^α` et croît sans borne, donc un temps caractéristique
    /// divisé par elle s'effondre.
    ///   - le repeuplement (V11, 2026-09-08) : `τ` tombait de 300 s à 10⁻⁴ s en
    ///     quinze cycles. `vitesseDeRepeuplement` est ensuite partie avec le modèle
    ///     à population, le 2026-09-09 ;
    ///   - l'acquis de séjour (2026-09-11) : `t₉₀` tombait de 2 h à 0,12 h au
    ///     deuxième cycle et à 0,05 s au troisième, et l'acquis saturait toujours
    ///     avant la renaissance — la loi
    ///     de contenance lisait un forfait. Son temps est désormais `τ₀`, constant
    ///     (amendement v1.1, §2.B).
    /// </summary>
    public static class Densite
    {
        public static double DuPalier(EtatJeu etat, int palier) =>
            palier >= 0 && palier < etat.Permanent.Densites.Count ? etat.Permanent.Densites[palier] : 0;

        /// <summary>
        /// Densité du séjour : la plus dense des eaux où le héros se tient — le
        /// maximum sur les paliers OUVERTS, pas une somme.
        ///
        /// [P] — le §2.A écrit `multiplicateurDensite(s)` pour l'état entier, alors que
        /// la densité est portée par palier. Le maximum sur les paliers ouverts est
        /// retenu : c'est celle qu'il peut effectivement habiter. En pratique la
        /// question est peu sensible — la renaissance porte tous les paliers occupés
        /// à la même valeur —, mais elle le deviendrait si une assise cessait d'être
        /// revisitée à chaque vie.
        ///
        /// C'EST LA SEULE GRANDEUR NOMMÉE « densité » qui doit nourrir
        /// `Multiplicateur` — dans la production, son seul usage, comme dans le
        /// détail de captation qui l'affiche : la
        /// dérivation de `DensiteExposant` (`Constantes.cs`) suppose que son argument
        /// EST la densité, la grandeur qui vaut `pointe^α` — un scalaire, jamais une
        /// somme. Une SOMME sur les paliers ouverts croît aussi avec leur NOMBRE, et
        /// glisse un `(p_new/p_old)^(θ/α)` non budgété sur le `g^(paliers × θ)` voulu à
        /// chaque renaissance — mesuré : environ ×18 sur une partie complète.
        /// </summary>
        public static double DuSejour(EtatJeu etat)
        {
            var densite = 0.0;
            for (var palier = 0; palier < etat.Cycle.PaliersOuverts; palier += 1)
                densite = Math.Max(densite, DuPalier(etat, palier));
            return densite;
        }

        /// <summary>
        /// Multiplicateur de densité : `(1 + densité / d₀) ^ (θ/α)` (amendement v1.1
        /// §2.A, forme des contraintes globales du plan).
        ///
        /// Il multiplie la production (§10), au même titre que le multiplicateur de
        /// profondeur : les deux sont des TermeDeFormule nommés dans le détail de
        /// captation, jamais des facteurs flottants (§7.5 règle 3). C'est son SEUL
        /// usage, et c'est par là que passe « séjour en mana DENSE ». Il ne touche plus
        /// au temps du séjour, qui vaut `τ₀`, constant (amendement v1.1, §2.B, amendé
        /// le 2026-09-11).
        ///
        /// À densité nulle il vaut exactement 1 : une eau neutre ne multiplie ni ne
        /// divise la production.
        /// </summary>
        public static double Multiplicateur(double densite) =>
            Math.Pow(1 + densite / Constantes.DENSITE_DE_REFERENCE, Constantes.DensiteExposant());

        /// <summary>
        /// Densité qu'un cycle laisse derrière lui : `pointe ^ α` (§2.A, étape 2).
        ///
        /// C'est de là que part toute la chaîne : la pointe étant multipliée par
        /// `g^paliers` à chaque renaissance, la densité l'est par `g^(paliers × α)`, et
        /// le multiplicateur par `g^(paliers × θ)`.
        /// </summary>
        public static double LaisseeParLeCycle(Decimal productionDePic)
        {
            var rapport = productionDePic.Div(Constantes.PRODUCTION_DE_REFERENCE);
            if (rapport.Lte(0)) return 0;
            return Math.Pow(rapport.ToNumber(), Constantes.ALPHA_GAIN_DE_DENSITE);
        }

        /// <summary>
        /// Porte la densité des paliers occupés au niveau que le cycle a laissé.
        ///
        /// `max`, jamais une affectation : la densité ne redescend JAMAIS (Tier 0). Un
        /// cycle plus court que le précédent laisse moins de charge derrière lui, et ne
        /// doit pas pouvoir défaire ce qui a été acquis.
        /// </summary>
        public static IReadOnlyList<double> AppliquerGain(EtatJeu etat, int paliersOuverts, Decimal productionDePic)
        {
            var conservation = Technique.FacteurDeTechnique(etat, TermeDeFormule.DensiteConservee);
            var laissee = LaisseeParLeCycle(productionDePic) * conservation;
            if (!(laissee > 0)) return etat.Permanent.Densites;
            var resultat = new List<double>(etat.Permanent.Densites.Count);
            for (var index = 0; index < etat.Permanent.Densites.Count; index += 1)
                resultat.Add(index < paliersOuverts ? Math.Max(etat.Permanent.Densites[index], laissee) : etat.Permanent.Densites[index]);
            return resultat;
        }
    }
}
