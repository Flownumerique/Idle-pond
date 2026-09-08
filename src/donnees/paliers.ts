/**
 * IdlePond — les 62 paliers et les bancs qui les occupent.
 *
 * Un banc est une espèce installée sur un palier : c'est l'unité que le joueur
 * convainc puis monte en niveau. Le mot vient du §7.3 (« Le banc suit »,
 * « Veille des bancs », « Compter les bancs »).
 *
 * [P] §4.2 — le placement des espèces est autorial. Celui-ci est PROVISOIRE :
 * il étale les espèces d'une assise sur ses paliers, un banc par palier. La
 * structure de données porte volontairement une LISTE de bancs par palier pour
 * que le placement définitif puisse en poser plusieurs sans réécriture — et
 * parce que le §6.3 avertit que `D` se mesure sur la production totale du
 * palier, jamais par espèce, précisément pour rester juste dans ce cas.
 */
import type { Banc, Palier } from '../noyau/types'
import { NOMBRE_DE_PALIERS } from '../noyau/constantes'
import { ASSISES } from './assises'
import { especesDeLAssise } from './especes'

export function idDeBanc(espece: string, palier: number): string {
  return `${espece}@${palier}`
}

function construirePaliers(): readonly Palier[] {
  const paliers: Palier[] = []
  for (const assise of ASSISES) {
    const especes = especesDeLAssise(assise.id)
    for (let local = 0; local < assise.nombreDePaliers; local += 1) {
      const index = assise.indexPremierPalier + local
      const espece = especes[Math.floor((local * especes.length) / assise.nombreDePaliers)]
      const banc: Banc = { id: idDeBanc(espece.id, index), espece: espece.id, palier: index }
      paliers.push({ index, assise: assise.id, bancs: [banc] })
    }
  }
  if (paliers.length !== NOMBRE_DE_PALIERS) {
    throw new Error(`Compte de paliers incohérent : ${paliers.length} au lieu de ${NOMBRE_DE_PALIERS}`)
  }
  return paliers
}

export const PALIERS: readonly Palier[] = construirePaliers()

export const BANCS: readonly Banc[] = PALIERS.flatMap((p) => p.bancs)

export function bancsDuPalier(index: number): readonly Banc[] {
  return PALIERS[index].bancs
}

const PAR_ID = new Map(BANCS.map((b) => [b.id, b]))

export function bancParId(id: string): Banc | undefined {
  return PAR_ID.get(id)
}
