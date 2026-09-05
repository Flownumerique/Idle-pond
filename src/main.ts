/**
 * IdlePond — banc d'essai du jalon v0.1.
 *
 * Ce n'est pas l'écran du jeu. Le §12 est explicite : au jalon v0.1, « le
 * noyau, les trois tests, le versionnage de save. Rien d'autre. Pas d'UI, pas
 * de Phaser, pas de contenu. » Ce fichier existe pour que `vite build` ait une
 * entrée et pour lire une sortie du simulateur sans passer par les tests ; il
 * n'affiche aucun nom de lieu, aucune espèce, aucun élément de jeu — et donc
 * jamais de nom générique de couche, ce que le §3 interdit à l'écran.
 *
 * L'UI arrive au jalon v0.2, une fois la v0.1 acceptée.
 */
import { NOMBRE_D_ECLOSIONS_VISE } from './noyau/constantes'
import { simuler } from './simulateur/simulateur'

const resultat = simuler(NOMBRE_D_ECLOSIONS_VISE)

const lignes = [
  `IdlePond — jalon v0.1, banc d'essai`,
  ``,
  `cycles demandés    : ${resultat.cyclesDemandes}`,
  `cycles achevés     : ${resultat.cyclesAcheves}`,
  `cycle non convergent : ${resultat.cycleNonConvergent ?? 'aucun'}`,
  `temps de jeu actif : ${(resultat.releve.tempsJeuActifSecondes / 3600).toFixed(1)} h`,
  ``,
  `cycle | durée (h) | paliers | redescente | pic mana/s | Foi`,
  ...resultat.releve.cycles.map(
    (c) =>
      `${String(c.index + 1).padStart(5)} | ` +
      `${(c.dureeActiveSecondes / 3600).toFixed(2).padStart(9)} | ` +
      `${String(c.paliersOuverts).padStart(7)} | ` +
      `${(c.fractionEnRedescente * 100).toFixed(0).padStart(9)}% | ` +
      `${c.productionPicParSeconde.toExponential(2).padStart(10)} | ` +
      `${c.foiGagnee}`,
  ),
]

const racine = document.getElementById('root')
if (racine !== null) {
  const bloc = document.createElement('pre')
  bloc.textContent = lignes.join('\n')
  bloc.style.padding = '1rem'
  bloc.style.fontFamily = 'ui-monospace, monospace'
  racine.appendChild(bloc)
}
