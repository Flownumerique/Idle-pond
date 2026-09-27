/* HARNAIS DE MESURE — NON INTÉGRÉ AU JEU.
 * Ces fichiers ont produit docs/RESULTATS.md. Ils servent de référence de
 * lecture pour le modèle du noyau v1.0, jamais de code de production : le jeu
 * vit dans src/, avec Decimal et l'état immuable. Ne pas importer depuis src/.
 */

/**
 * IdlePond — calibreur.
 *
 * Les trois inconnues du noyau v1.0 forment une boucle fermée :
 *   éclosion → densité → production → profondeur → densité
 * Aucune ne se règle isolément. Ce fichier les résout numériquement.
 *
 * Méthode :
 *   1. `D` fixe la FORME — le ratio de durée entre deux cycles.
 *   2. `ECHELLE` fixe la DURÉE ABSOLUE — elle n'a aucun effet sur la forme.
 *   Les deux sont donc séparables, et se résolvent par bissection l'un après
 *   l'autre. `ALPHA` est fixé par l'utilisateur : il déplace la forme, donc
 *   on recalibre `D` pour chaque valeur de `ALPHA` testée.
 */

import * as K from './constantes.js';
import { simuler, rapport, type Resultat } from './simulateur.js';

const CIBLE_CYCLE1_H = 3;
const CIBLE_TOTAL_H = 182;

function ratioMoyen(r: Resultat): number {
  const ratios = r.cycles.slice(1).map((cy, i) => cy.dureeH / r.cycles[i].dureeH);
  if (ratios.length === 0) return 0;
  const somme = ratios.reduce((a, b) => a + Math.log(b), 0);
  return Math.exp(somme / ratios.length);
}

/** Part du cycle le plus long dans la durée totale. */
function partDuPlusLong(r: Resultat): number {
  return Math.max(...r.cycles.map((c) => c.dureeH)) / r.totalH;
}

/**
 * `ECHELLE` règle la durée du PREMIER cycle, et rien d'autre : au cycle 1 la
 * densité est nulle, donc la production est purement proportionnelle à
 * l'échelle. C'est un réglage exact, pas un compromis.
 */
function calibrerEchelle(): number {
  let bas = 1e-6, haut = 1e4;
  for (let i = 0; i < 30; i++) {
    const mid = Math.sqrt(bas * haut);
    K.P.ECHELLE = mid;
    const t = simuler('optimale').cycles[0].dureeH;
    if (t > CIBLE_CYCLE1_H) bas = mid; else haut = mid;
  }
  return Math.sqrt(bas * haut);
}

/**
 * `THETA` règle la DURÉE TOTALE : c'est la part du besoin que la densité
 * compense d'une éclosion à l'autre. Plus elle compense, plus les cycles
 * tardifs sont courts, plus le total est bas.
 */
function calibrerTheta(): number {
  let bas = 0.05, haut = 1.5;
  for (let i = 0; i < 22; i++) {
    const mid = (bas + haut) / 2;
    K.P.THETA = mid;
    const t = simuler('optimale').totalH;
    if (t > CIBLE_TOTAL_H) bas = mid; else haut = mid;
  }
  return (bas + haut) / 2;
}

/** Les deux réglages interagissent faiblement : trois passes suffisent. */
function calibrer(alpha: number): { theta: number; echelle: number } {
  K.P.ALPHA = alpha;
  let theta = 0.6, echelle = 1;
  for (let pass = 0; pass < 3; pass++) {
    K.P.THETA = theta;
    echelle = calibrerEchelle();
    K.P.ECHELLE = echelle;
    theta = calibrerTheta();
  }
  K.P.THETA = theta;
  K.P.ECHELLE = echelle;
  return { theta, echelle };
}

// ==================================================================== main

console.log(`contenu : ${K.NB_PALIERS} paliers`);
console.log(`cibles  : cycle 1 = ${CIBLE_CYCLE1_H} h, total ${CIBLE_TOTAL_H} h\n`);

console.log('alpha |  theta   |  exp.  | cyc.1 | total h | ratio | + long');
console.log('------+----------+--------+-------+---------+-------+-------');

let meilleur = { alpha: 0, theta: 0, echelle: 0 };
for (const alpha of [0.35, 0.50, 0.65]) {
  const { theta, echelle } = calibrer(alpha);
  const r = simuler('optimale');
  console.log(
    `${alpha.toFixed(2).padStart(5)} |` +
    `${theta.toFixed(4).padStart(9)} |` +
    `${K.densiteExposant().toFixed(3).padStart(7)} |` +
    `${r.cycles[0].dureeH.toFixed(2).padStart(6)} |` +
    `${r.totalH.toFixed(1).padStart(8)} |` +
    `${ratioMoyen(r).toFixed(3).padStart(6)} |` +
    `${(partDuPlusLong(r) * 100).toFixed(0).padStart(5)}%`,
  );
  if (alpha === 0.50) meilleur = { alpha, theta, echelle };
}

console.log('\n\n########## solution retenue (alpha = 0.6) ##########');
K.P.ALPHA = meilleur.alpha;
K.P.THETA = meilleur.theta;
K.P.ECHELLE = meilleur.echelle;
console.log(`ALPHA   = ${meilleur.alpha}`);
console.log(`THETA   = ${meilleur.theta.toFixed(4)}`);
console.log(`exposant densité = ${K.densiteExposant().toFixed(4)}`);
console.log(`D       = ${K.P.D}  (valeur de conception, non ajustée)`);
console.log(`m_p     = ${K.multPalier().toFixed(4)}`);
console.log(`ECHELLE = ${meilleur.echelle.toExponential(3)}`);
console.log(`  -> débit héros  = ${(K.DEBIT_HEROS * meilleur.echelle).toExponential(3)} mana/s`);
console.log(`  -> débit espèce = ${(K.DEBIT_BASE * meilleur.echelle).toExponential(3)} mana/s`);

console.log(rapport(simuler('optimale')));
console.log(rapport(simuler('relachee')));
