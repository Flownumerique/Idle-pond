/**
 * Le lieu, et les bancs qui l'habitent.
 *
 * L'écran ne dit jamais « palier » ni « assise » (§3) : il dit le nom propre du
 * lieu et une profondeur en brasses.
 *
 * Le joueur ne choisit jamais quelle espèce va où — le placement est fixé par
 * l'auteur (§4.2). Il convainc, il monte des niveaux, il creuse.
 */
import type { EtatJeu } from '../noyau/types'
import { PALIERS } from '../donnees/paliers'
import { ASSISES } from '../donnees/assises'
import {
  contenance,
  coutCreuser,
  coutDeblocage,
  coutDePlace,
  productionDuBanc,
  toutEstCreuse,
} from '../noyau/economie'
import { effectifCible } from '../noyau/population'
import { cout, nomDeLAssise, nomDeLEspece, montant, profondeur } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surConviction: (banc: string) => void
  readonly surPlace: (banc: string) => void
  readonly surCreusement: () => void
  readonly surCaptation: (banc: string) => void
}

export function Mare({ etat, surConviction, surPlace, surCreusement, surCaptation }: Props) {
  const mana = etat.cycle.manaCourant
  const coutDuCreusement = coutCreuser(etat, etat.cycle.paliersOuverts)
  const creusementPossible = !toutEstCreuse(etat) && coutDuCreusement.lte(contenance(etat))

  return (
    <section className="space-y-2">
      <h2 className="font-texte text-lg text-jour-doux">{nomDeLAssise(ASSISES[0].id)}</h2>

      <ol className="space-y-2">
        {PALIERS.slice(0, etat.cycle.paliersOuverts).map((palier) =>
          palier.bancs.map((banc) => {
            const vivant = etat.cycle.bancs[banc.id]
            const place = vivant?.place ?? 0
            const coutDuBanc = place === 0 ? coutDeblocage(etat, banc) : coutDePlace(etat, banc, place)
            const payable = mana.gte(coutDuBanc) && coutDuBanc.lte(contenance(etat))
            const cible = effectifCible(place)

            return (
              <li
                key={banc.id}
                className="rounded-lg border border-eau-bord bg-eau-fond/40 p-3 transition-colors hover:border-eau-clair"
              >
                <div className="flex items-baseline justify-between gap-3">
                  <span className="font-texte text-base">
                    {place === 0 ? <span className="text-jour-tu">un banc s’attarde</span> : nomDeLEspece(banc.espece)}
                  </span>
                  <span className="font-chiffre text-xs text-jour-tu">{profondeur(banc.palier)}</span>
                </div>

                {place > 0 ? (
                  <div className="mt-1 flex items-baseline gap-4 text-sm text-jour-doux">
                    <span className="font-chiffre tabular-nums">
                      {(vivant?.effectif ?? 0).toFixed(1)} / {cible}
                    </span>
                    <button
                      type="button"
                      onClick={() => surCaptation(banc.id)}
                      className="font-chiffre tabular-nums text-mana underline decoration-dotted underline-offset-4 hover:text-jour"
                    >
                      +{montant(productionDuBanc(etat, banc))} / s
                    </button>
                  </div>
                ) : null}

                <button
                  type="button"
                  disabled={!payable}
                  onClick={() => (place === 0 ? surConviction(banc.id) : surPlace(banc.id))}
                  className="mt-2 w-full rounded-md border border-eau-clair px-3 py-1.5 text-sm transition-colors enabled:hover:bg-eau-bord disabled:cursor-not-allowed disabled:opacity-40"
                >
                  {place === 0 ? 'Convaincre' : 'Faire de la place'}
                  <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(coutDuBanc)}</span>
                </button>
              </li>
            )
          }),
        )}
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
