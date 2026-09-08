/**
 * Le détail de la captation (§8.2).
 *
 * Contrepartie OBLIGATOIRE d'un effet appliqué silencieusement : chaque terme
 * actif y est attribuable à sa source. C'est aussi ce qui rend tenable la règle
 * du §7.5.3 — un effet qui donne « +12 % » sans dire à quel terme n'a pas sa
 * place dans cette table, donc pas sa place dans le jeu.
 */
import type { EtatJeu } from '../noyau/types'
import { detailDeCaptation, productionDuBanc } from '../noyau/economie'
import { bancParId } from '../donnees/paliers'
import { montant, nomDeLEspece, profondeur, sourceDuTerme } from './format'

interface Props {
  readonly etat: EtatJeu
  readonly banc: string
  readonly surFermeture: () => void
}

export function Captation({ etat, banc: bancId, surFermeture }: Props) {
  const banc = bancParId(bancId)
  if (banc === undefined) return null
  const lignes = detailDeCaptation(etat, banc)

  return (
    <section className="space-y-3 rounded-lg border border-eau-clair bg-eau-fond p-4">
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="font-texte text-base">
          {nomDeLEspece(banc.espece)}{' '}
          <span className="font-chiffre text-xs text-jour-tu">{profondeur(banc.palier)}</span>
        </h3>
        <button
          type="button"
          onClick={surFermeture}
          className="text-sm text-jour-tu underline decoration-dotted underline-offset-4 hover:text-jour"
        >
          fermer
        </button>
      </div>

      <table className="w-full text-sm">
        <tbody>
          {lignes.map((ligne) => (
            <tr key={ligne.terme} className="border-b border-eau-bord/60 last:border-0">
              <td className="py-1 font-chiffre text-xs text-jour-tu">{ligne.terme}</td>
              <td className="py-1 text-right font-chiffre tabular-nums">
                {ligne.valeur < 10 ? ligne.valeur.toFixed(3) : ligne.valeur.toFixed(1)}
              </td>
              <td className="py-1 pl-3 text-right text-xs text-jour-tu">{sourceDuTerme(ligne.source)}</td>
            </tr>
          ))}
          <tr>
            <td className="pt-2 font-chiffre text-xs text-mana">production</td>
            <td className="pt-2 text-right font-chiffre text-mana tabular-nums">
              {montant(productionDuBanc(etat, banc))}
            </td>
            <td className="pt-2 pl-3 text-right text-xs text-jour-tu">par seconde</td>
          </tr>
        </tbody>
      </table>
    </section>
  )
}
