/**
 * Ce que le héros porte, et ce qu'il peut porter.
 *
 * §6.4 : la contenance limite le STOCK, pas la production. Le blocage est doux —
 * quand elle sature, le joueur continue à monter des niveaux, il ne peut plus
 * que descendre. L'écran doit donc dire « plein », pas « bloqué ».
 *
 * GDD §2.4, et c'est la règle qui commande cet écran : l'alerte est « un effet,
 * pas un texte ». Passé le seuil, l'eau se trouble et la barre perd sa couleur
 * vive ; rien n'est écrit, rien ne s'ouvre, aucun compte à rebours n'apparaît.
 * Un décompte affiché ferait de la seule pénalité douce du jeu un minuteur.
 */
import type { EtatJeu } from '../noyau/types'
import {
  contenance,
  eauTroublee,
  estBloque,
  estSature,
  partDeContenance,
  productionTotaleParSeconde,
} from '../noyau/economie'
import { montant } from './format'

export function Contenance({ etat }: { readonly etat: EtatJeu }) {
  const plafond = contenance(etat)
  const part = partDeContenance(etat)
  const production = productionTotaleParSeconde(etat)
  const trouble = eauTroublee(etat)
  const plein = estSature(etat)

  return (
    <section
      className={`rounded-lg border bg-eau-fond/60 p-4 transition-colors duration-1000 ${
        trouble ? 'border-trouble/50' : 'border-eau-bord'
      }`}
    >
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-chiffre text-2xl text-mana tabular-nums">{montant(etat.cycle.manaCourant)}</span>
        <span className="font-chiffre text-sm text-jour-tu tabular-nums">
          sur {montant(plafond)}
        </span>
      </div>

      <div className="mt-2 h-1.5 overflow-hidden rounded-full bg-eau-abysse">
        <div
          className={`h-full rounded-full transition-[width,background-color] duration-1000 ${
            trouble ? 'bg-trouble' : 'bg-mana'
          }`}
          style={{ width: `${part * 100}%` }}
        />
      </div>

      <p className="mt-2 flex items-baseline justify-between text-sm text-jour-doux">
        {/* Saturation : « la captation s'arrête. Il dépense encore, il ne gagne
            plus. » Afficher un débit pendant que la jauge ne bouge pas ferait
            mentir le seul chiffre de cet écran. */}
        <span className="font-chiffre tabular-nums">
          {plein ? '+0 / s' : `+${montant(production)} / s`}
        </span>
        {plein ? <span className="text-trouble">tu ne captes plus rien</span> : null}
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
