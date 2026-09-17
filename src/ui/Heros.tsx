/**
 * Toi — le héros. Le quatrième achat (spec 2026-09-17).
 *
 * Même gabarit que la carte d'une espèce dans `Mare.tsx` : un nom, ce que ça
 * donne par seconde, un bouton avec son coût. Le héros n'a pas de nom propre
 * (GDD : « aucun nom propre définitif ») ; l'écran dit « toi ».
 */
import type { EtatJeu } from '../noyau/types'
import { contenance, coutDeCroissance, multiplicateurDuHeros, productionDuHeros } from '../noyau/economie'
import { cout, montant } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly surCroissance: () => void
}

export function Heros({ etat, surCroissance }: Props) {
  const niveau = etat.cycle.niveauDuHeros
  const prix = coutDeCroissance(etat, niveau)
  const payable = etat.cycle.manaCourant.gte(prix) && prix.lte(contenance(etat))
  const bonus = Math.round((multiplicateurDuHeros(etat) - 1) * 100)

  return (
    <section className="rounded-lg border border-foi/40 bg-eau-fond/40 p-3">
      <div className="flex items-baseline justify-between gap-3">
        <span className="font-texte text-base">toi</span>
        <span className="font-chiffre text-xs text-jour-tu tabular-nums">
          {niveau === 1 ? 'alevin' : `grandi ${niveau - 1} fois`}
        </span>
      </div>
      <div className="mt-1 flex items-baseline gap-4 text-sm text-jour-doux">
        <span className="font-chiffre text-mana tabular-nums">+{montant(productionDuHeros(etat))} / s</span>
        {bonus > 0 ? (
          <span className="font-chiffre text-jour-tu tabular-nums">et tout ce que tu convaincs donne +{bonus} %</span>
        ) : null}
      </div>
      <button
        type="button"
        disabled={!payable}
        onClick={surCroissance}
        className="mt-2 w-full rounded-md border border-foi/60 px-3 py-1.5 text-sm transition-colors enabled:hover:bg-foi/10 disabled:cursor-not-allowed disabled:opacity-40"
      >
        Grandir
        <span className="ml-2 font-chiffre text-jour-tu tabular-nums">{cout(prix)}</span>
      </button>
    </section>
  )
}
