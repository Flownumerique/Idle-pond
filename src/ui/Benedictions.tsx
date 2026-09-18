/**
 * Ce que tu bénis — noyau v1.0 §4.1 : « l'écran d'améliorations du jeu, comme
 * le veut la convention du genre ». Permanent, payé en Foi.
 *
 * Spec 2026-09-17 [D7] : ouvert tout le temps. La Foi n'est créditée qu'à
 * l'éclosion, donc ce que cet écran permet ne change qu'en rentrant dans
 * l'œuf ; mais un joueur qui revient d'une absence ne doit pas trouver une
 * porte fermée.
 *
 * Les ciblées ne sont listées que pour les espèces déjà convaincues au moins
 * une fois dans cette vie ou une autre — on ne bénit pas ce qu'on n'a jamais
 * vu. Le registre entier existe dans la donnée ; l'écran le filtre.
 */
import type { EtatJeu } from '../noyau/types'
import { BENEDICTIONS, BENEDICTION_GLOBALE_ID } from '../donnees/benedictions'
import { coutDeBenediction, rangDeBenediction } from '../noyau/economie'
import { TEXTE_DE_BENEDICTION_CIBLEE, TEXTE_DE_LA_BENEDICTION_GLOBALE } from '../donnees/textes-provisoires'
import { cout, montant, nomDeLEspece } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surBenediction: (id: string) => void
}

export function Benedictions({ etat, surBenediction }: Props) {
  const foi = etat.permanent.foi
  const connues = new Set<string>([
    ...Object.keys(etat.cycle.especes).filter((id) => etat.cycle.especes[id].debloquee),
    ...etat.permanent.especesAyantAtteintCent,
    ...BENEDICTIONS.filter((b) => b.espece !== null && rangDeBenediction(etat, b.id) > 0).map((b) => b.espece as string),
  ])
  const visibles = BENEDICTIONS.filter((b) => b.portee === 'globale' || (b.espece !== null && connues.has(b.espece)))

  return (
    <section className="space-y-2">
      <div className="flex items-baseline justify-between">
        <h2 className="font-texte text-lg text-jour-doux">Ce que tu bénis</h2>
        <span className="font-chiffre text-sm text-foi tabular-nums">{montant(foi)} de Foi</span>
      </div>
      <ol className="space-y-2">
        {visibles.map((b) => {
          const rang = rangDeBenediction(etat, b.id)
          const prix = coutDeBenediction(etat, b)
          const payable = foi.gte(prix)
          const nom = b.id === BENEDICTION_GLOBALE_ID ? TEXTE_DE_LA_BENEDICTION_GLOBALE.nom : `Bénir ${nomDeLEspece(b.espece as string)}`
          const effet = b.id === BENEDICTION_GLOBALE_ID ? TEXTE_DE_LA_BENEDICTION_GLOBALE.effet : TEXTE_DE_BENEDICTION_CIBLEE.effet
          return (
            <li key={b.id} className="rounded-lg border border-eau-bord bg-eau-fond/40 p-3">
              <div className="flex items-baseline justify-between gap-3">
                <span className="font-texte text-base">{nom}</span>
                <span className="font-chiffre text-xs text-jour-tu tabular-nums">{rang === 0 ? 'jamais' : `${rang} fois`}</span>
              </div>
              <p className="mt-0.5 text-xs text-jour-tu">{effet}</p>
              <button
                type="button"
                disabled={!payable}
                onClick={() => surBenediction(b.id)}
                className="mt-2 w-full rounded-md border border-foi/60 px-3 py-1.5 text-sm text-foi transition-colors enabled:hover:bg-foi/10 disabled:cursor-not-allowed disabled:opacity-40"
              >
                Bénir
                <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(prix)} de Foi</span>
              </button>
            </li>
          )
        })}
      </ol>
    </section>
  )
}
