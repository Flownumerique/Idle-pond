/**
 * L'éclosion. Le héros ENTRE dans l'œuf.
 *
 * Une contrainte non évidente commande cet écran : le gain prévu ne s'affiche
 * PAS en permanence. « Lire l'eau » est un nœud verbe de la branche Éclosion
 * (§7.3), donc du contenu v0.4 ; l'afficher tout le temps dès maintenant
 * viderait ce nœud de sa substance avant même de l'avoir écrit.
 *
 * Il se lit donc ici, au moment de décider, et nulle part ailleurs.
 */
import { useState } from 'react'
import type { EtatJeu } from '../noyau/types'
import { gainDeFoiPrevu } from '../noyau/eclosion'
import { estBloque } from '../noyau/economie'
import { montant } from './format'

export function Eclosion({ etat, surEclosion }: { readonly etat: EtatJeu; readonly surEclosion: () => void }) {
  const [ouvert, setOuvert] = useState(false)
  const gain = gainDeFoiPrevu(etat)
  const bloque = estBloque(etat)

  if (!ouvert) {
    return (
      <button
        type="button"
        onClick={() => setOuvert(true)}
        className={`w-full rounded-lg border px-3 py-2.5 text-sm transition-colors ${
          bloque
            ? 'border-foi/60 text-foi hover:bg-foi/10'
            : 'border-eau-bord text-jour-doux hover:border-eau-clair'
        }`}
      >
        Rentrer dans l’œuf
      </button>
    )
  }

  return (
    <section className="space-y-3 rounded-lg border border-foi/50 bg-eau-fond/60 p-4">
      <p className="font-texte text-base">
        Tout ce qui vit ici restera ici. Ce que tu as appris te suivra.
      </p>

      <dl className="space-y-1 text-sm text-jour-doux">
        <div className="flex justify-between">
          <dt>Foi que tes fidèles ont émise</dt>
          <dd className="font-chiffre text-foi tabular-nums">{montant(gain)}</dd>
        </div>
        <div className="flex justify-between">
          <dt>Ce que la mare gardera de ta charge</dt>
          <dd className="font-chiffre text-densite tabular-nums">plus dense</dd>
        </div>
      </dl>

      <p className="text-sm text-jour-tu">
        Rester plus longtemps fait monter la Foi. Partir maintenant fait descendre plus bas.
      </p>

      <div className="flex gap-2">
        <button
          type="button"
          onClick={() => {
            surEclosion()
            setOuvert(false)
          }}
          className="flex-1 rounded-md border border-foi/70 px-3 py-2 text-sm text-foi transition-colors hover:bg-foi/10"
        >
          Rentrer
        </button>
        <button
          type="button"
          onClick={() => setOuvert(false)}
          className="rounded-md border border-eau-bord px-3 py-2 text-sm text-jour-doux transition-colors hover:border-eau-clair"
        >
          Rester
        </button>
      </div>
    </section>
  )
}
