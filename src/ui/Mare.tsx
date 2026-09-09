/**
 * Le lieu, et les bancs qui l'habitent.
 *
 * L'écran ne dit jamais « palier » ni « assise » (§3) : il dit le nom propre du
 * lieu et une profondeur en brasses.
 *
 * Le joueur ne choisit jamais quelle espèce va où — le placement est fixé par
 * l'auteur (§4.2). Il débloque, il monte des crans, il creuse. L'effet est
 * immédiat : il n'y a plus de population qui rejoint lentement une cible.
 */
import type { EtatJeu } from '../noyau/types'
import { ESPECES } from '../donnees/especes'
import { ASSISES } from '../donnees/assises'
import {
  contenance,
  coutDeDescente,
  coutDeDeblocage,
  coutDeNiveau,
  productionDeLEspece,
  toutEstCreuse,
} from '../noyau/economie'
import { cout, nomDeLAssise, nomDeLEspece, montant, profondeur } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surDeblocage: (espece: string) => void
  readonly surNiveau: (espece: string) => void
  readonly surCreusement: () => void
  readonly surCaptation: (espece: string) => void
}

export function Mare({ etat, surDeblocage, surNiveau, surCreusement, surCaptation }: Props) {
  const mana = etat.cycle.manaCourant
  const coutDuCreusement = coutDeDescente(etat, etat.cycle.paliersOuverts)
  const creusementPossible = !toutEstCreuse(etat) && coutDuCreusement.lte(contenance(etat))

  return (
    <section className="space-y-2">
      <h2 className="font-texte text-lg text-jour-doux">{nomDeLAssise(ASSISES[0].id)}</h2>

      <ol className="space-y-2">
        {ESPECES.filter((espece) => espece.palier < etat.cycle.paliersOuverts).map((espece) => {
          const vivante = etat.cycle.especes[espece.id]
          const niveau = vivante?.debloquee === true ? vivante.niveau : 0
          const coutDeLEspece =
            niveau === 0 ? coutDeDeblocage(etat, espece) : coutDeNiveau(etat, espece, niveau)
          const payable = mana.gte(coutDeLEspece) && coutDeLEspece.lte(contenance(etat))

          return (
            <li
              key={espece.id}
              className="rounded-lg border border-eau-bord bg-eau-fond/40 p-3 transition-colors hover:border-eau-clair"
            >
              <div className="flex items-baseline justify-between gap-3">
                <span className="font-texte text-base">
                  {niveau === 0 ? (
                    <span className="text-jour-tu">un banc s’attarde</span>
                  ) : (
                    nomDeLEspece(espece.id)
                  )}
                </span>
                <span className="font-chiffre text-xs text-jour-tu">{profondeur(espece.palier)}</span>
              </div>

              {niveau > 0 ? (
                <div className="mt-1 flex items-baseline gap-4 text-sm text-jour-doux">
                  <span className="font-chiffre tabular-nums">{niveau}</span>
                  <button
                    type="button"
                    onClick={() => surCaptation(espece.id)}
                    className="font-chiffre tabular-nums text-mana underline decoration-dotted underline-offset-4 hover:text-jour"
                  >
                    +{montant(productionDeLEspece(etat, espece))} / s
                  </button>
                </div>
              ) : null}

              <button
                type="button"
                disabled={!payable}
                onClick={() => (niveau === 0 ? surDeblocage(espece.id) : surNiveau(espece.id))}
                className="mt-2 w-full rounded-md border border-eau-clair px-3 py-1.5 text-sm transition-colors enabled:hover:bg-eau-bord disabled:cursor-not-allowed disabled:opacity-40"
              >
                {niveau === 0 ? 'Convaincre' : 'Les faire venir en nombre'}
                <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(coutDeLEspece)}</span>
              </button>
            </li>
          )
        })}
      </ol>

      <button
        type="button"
        disabled={!creusementPossible || mana.lt(coutDuCreusement)}
        onClick={surCreusement}
        className="w-full rounded-lg border border-dashed border-eau-clair px-3 py-3 text-sm transition-colors enabled:hover:bg-eau-fond disabled:cursor-not-allowed disabled:opacity-40"
      >
        {toutEstCreuse(etat) ? (
          'Il n’y a plus de roche à ouvrir ici'
        ) : (
          <>
            Creuser plus bas
            <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(coutDuCreusement)}</span>
          </>
        )}
      </button>
    </section>
  )
}
