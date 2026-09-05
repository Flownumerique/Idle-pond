/**
 * IdlePond — succès : déclencheurs, effets, visibilité.
 *
 * Système de plein droit, porteur d'effets et de la distribution narrative
 * continue. Le typage par famille, l'état de visibilité et le registre figé ne
 * se rétrofitent pas (§8) : ils sont posés ici avant la première ligne de
 * contenu, et le registre reste vide jusqu'au jalon v0.2.
 *
 * Un effet est appliqué SILENCIEUSEMENT au déclenchement. Ses deux
 * contreparties — notification discrète et non bloquante, détail de la
 * captation consultable — sont de l'UI et n'appartiennent pas au noyau ; le
 * noyau rend la liste des déclenchements, l'adaptateur en fait une ligne qui
 * s'efface. Jamais une fenêtre.
 */
import type { EtatJeu, SuccesId } from './types'
import { SUCCES } from '../donnees/succes/index'

export interface ResultatDeSucces {
  readonly etat: EtatJeu
  readonly declenches: readonly SuccesId[]
}

/**
 * Le déclencheur d'un succès est un SEUIL relu sur l'état de fin de tick, et
 * jamais un événement consommé au vol : c'est ce qui permet à un pas de 8 h de
 * donner le même résultat que 480 pas de 60 s (§5.2). Un succès qui aurait
 * besoin d'observer l'intérieur d'un intervalle est du décor narratif, pas une
 * mécanique du cœur.
 */
export function verifierSucces(etat: EtatJeu): ResultatDeSucces {
  if (SUCCES.length === 0) return { etat, declenches: [] }
  const declenches: SuccesId[] = []
  for (const succes of SUCCES) {
    if (etat.permanent.succesDebloques.includes(succes.id)) continue
    declenches.push(succes.id)
  }
  if (declenches.length === 0) return { etat, declenches: [] }
  return {
    etat: {
      ...etat,
      permanent: {
        ...etat.permanent,
        succesDebloques: [...etat.permanent.succesDebloques, ...declenches],
      },
      telemetrie: {
        ...etat.telemetrie,
        intervallesEntreSucces: [
          ...etat.telemetrie.intervallesEntreSucces,
          etat.telemetrie.secondesDepuisDernierSucces,
        ],
        secondesDepuisDernierSucces: 0,
      },
    },
    declenches,
  }
}
