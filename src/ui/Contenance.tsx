/**
 * Ce que le héros porte, et ce qu'il peut porter.
 *
 * §6.4 : la contenance limite le STOCK, pas la production. Le blocage est doux —
 * quand elle sature, le joueur continue à monter des niveaux, il ne peut plus
 * que descendre. L'écran doit donc dire « plein », pas « bloqué ».
 */
import type { EtatJeu } from '../noyau/types'
import { contenance, estBloque, productionTotaleParSeconde } from '../noyau/economie'
import { montant } from './format'

export function Contenance({ etat }: { readonly etat: EtatJeu }) {
  const plafond = contenance(etat)
  const part = plafond.gt(0) ? Math.min(1, etat.cycle.manaCourant.div(plafond).toNumber()) : 0
  const production = productionTotaleParSeconde(etat)
  const plein = part >= 0.999

  return (
    <section className="rounded-lg border border-eau-bord bg-eau-fond/60 p-4">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-chiffre text-2xl text-mana tabular-nums">{montant(etat.cycle.manaCourant)}</span>
        <span className="font-chiffre text-sm text-jour-tu tabular-nums">
          sur {montant(plafond)}
        </span>
      </div>

      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-eau-abysse">
        <div
          className="h-full rounded-full bg-mana transition-[width] duration-200"
          style={{ width: `${part * 100}%` }}
        />
      </div>

      <p className="mt-2 flex items-baseline justify-between text-sm text-jour-doux">
        <span className="font-chiffre tabular-nums">+{montant(production)} / s</span>
        {plein ? (
          <span className="text-foi">l’eau déborde — ce qui passe s’en va vers le large</span>
        ) : null}
      </p>

      {estBloque(etat) ? (
        <p className="mt-3 border-t border-eau-bord pt-3 text-sm text-jour-doux">
          Il n’y a plus de quoi porter le prochain creusement. Rien n’empêche de
          continuer à faire venir du monde.
        </p>
      ) : null}
    </section>
  )
}
