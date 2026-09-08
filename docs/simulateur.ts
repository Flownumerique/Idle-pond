/* HARNAIS DE MESURE — NON INTÉGRÉ AU JEU.
 * Ces fichiers ont produit docs/RESULTATS.md. Ils servent de référence de
 * lecture pour le modèle du noyau v1.0, jamais de code de production : le jeu
 * vit dans src/, avec Decimal et l'état immuable. Ne pas importer depuis src/.
 */

/**
 * IdlePond — harnais de simulation.
 *
 * Il n'ajoute AUCUNE règle. Il appelle le même noyau que le jeu, avec un pas
 * long, et une politique d'achat qui remplace le joueur.
 */

import * as K from './constantes.js';
import {
  construireContenu, etatInitial, tick, production, achatsDisponibles,
  appliquer, bloque, eclore, multDensite,
  type Contenu, type GameState, type Achat,
} from './noyau.js';

// ================================================================ politiques

export type Politique = 'optimale' | 'relachee';

/**
 * Choisit un achat parmi les abordables.
 *
 * Critère : temps de retour = coût / gain marginal de production.
 * Le plus court gagne. Creuser entre dans la comparaison comme les autres,
 * sans traitement de faveur — c'est ce qui rend la mesure honnête.
 */
function choisir(s: GameState, c: Contenu, marge: number): Achat | null {
  const dispo = achatsDisponibles(s, c).filter(
    (a) => a.cout * marge <= s.mana && a.gain > 0,
  );
  if (dispo.length === 0) return null;

  let meilleur = dispo[0];
  let meilleurRetour = meilleur.cout / meilleur.gain;
  for (const a of dispo) {
    const r = a.cout / a.gain;
    if (r < meilleurRetour) { meilleur = a; meilleurRetour = r; }
  }
  return meilleur;
}

/**
 * Le premier achat de la partie ne peut pas être choisi par temps de retour :
 * tant qu'aucune espèce n'est débloquée, la production est nulle et tous les
 * gains marginaux sont nuls. On force le déblocage de la première espèce.
 */
function amorcer(s: GameState, c: Contenu): boolean {
  if (production(s, c) > 0) return false;
  for (let i = 0; i < s.especes.length; i++) {
    if (s.especes[i].debloquee) continue;
    if (c.especes[i].palier >= s.paliersOuverts) continue;
    if (s.mana >= c.especes[i].coutDeblocage) {
      appliquer(s, c, { type: 'debloquer', espece: i, cout: c.especes[i].coutDeblocage, gain: 0 });
      return true;
    }
  }
  return false;
}

// ================================================================== mesures

export interface Cycle {
  numero: number;
  dureeH: number;
  profondeur: number;
  paliersGagnes: number;
  productionFin: number;
  densiteApres: number;
  foi: number;
}

export interface Resultat {
  politique: Politique;
  cycles: Cycle[];
  totalH: number;
  profondeurFinale: number;
  atteintLaFin: boolean;
}

export function simuler(politique: Politique): Resultat {
  const c = construireContenu();
  const s = etatInitial(c);

  // marge : un joueur relâché n'achète que s'il a confortablement de quoi,
  // et n'agit qu'à intervalles espacés.
  // Un joueur réel n'est pas moins malin, il est moins PRÉSENT. La politique
  // relâchée achète la même chose, mais seulement à ses relevés.
  const marge = 1;
  const intervalleAction = politique === 'optimale' ? K.PAS_SIM : 4 * 3600;

  const cycles: Cycle[] = [];
  let depuisAction = 0;

  for (let e = 0; e < K.NB_ECLOSIONS; e++) {
    const profondeurDepart = s.paliersOuverts;
    let gardeFou = 0;

    while (true) {
      tick(s, c, K.PAS_SIM);
      depuisAction += K.PAS_SIM;

      if (depuisAction >= intervalleAction) {
        depuisAction = 0;
        // On achète tant qu'on peut : c'est ce que fait un joueur présent.
        for (let n = 0; n < 200; n++) {
          if (amorcer(s, c)) continue;
          const a = choisir(s, c, marge);
          if (!a) break;
          appliquer(s, c, a);
        }
      }

      gardeFou += K.PAS_SIM;
      if (gardeFou > K.CYCLE_MAX_H * 3600) break;
      if (bloque(s, c) && s.mana >= s.contenance * 0.99) break;
    }

    cycles.push({
      numero: e + 1,
      dureeH: s.tempsCycle / 3600,
      profondeur: s.paliersOuverts,
      paliersGagnes: s.paliersOuverts - profondeurDepart,
      productionFin: production(s, c),
      densiteApres: s.densite,
      foi: s.foi,
    });

    if (s.paliersOuverts >= c.paliers.length) { eclore(s, c); break; }
    eclore(s, c);
  }

  return {
    politique,
    cycles,
    totalH: cycles.reduce((a, b) => a + b.dureeH, 0),
    profondeurFinale: s.profondeurMax,
    atteintLaFin: s.profondeurMax >= c.paliers.length,
  };
}

// ================================================================ rapport

function fmt(n: number, d = 1): string {
  if (!isFinite(n)) return '∞';
  if (Math.abs(n) >= 1e6) return n.toExponential(2);
  return n.toFixed(d);
}

export function rapport(r: Resultat): string {
  const l: string[] = [];
  l.push(`\n=== politique : ${r.politique} ===`);
  l.push('cycle |  durée h | ratio | profond. | +pal |  production | densité');
  l.push('------+----------+-------+----------+------+-------------+---------');
  for (let i = 0; i < r.cycles.length; i++) {
    const cy = r.cycles[i];
    const ratio = i === 0 ? 0 : cy.dureeH / r.cycles[i - 1].dureeH;
    l.push(
      `${String(cy.numero).padStart(5)} |` +
      `${fmt(cy.dureeH, 2).padStart(9)} |` +
      `${(i === 0 ? '  —  ' : fmt(ratio, 2).padStart(5))} |` +
      `${String(cy.profondeur).padStart(9)} |` +
      `${String(cy.paliersGagnes).padStart(5)} |` +
      `${fmt(cy.productionFin, 1).padStart(12)} |` +
      `${fmt(cy.densiteApres, 1).padStart(8)}`,
    );
  }
  l.push('');
  l.push(`total          : ${fmt(r.totalH, 1)} h   (cible ~182 h)`);
  l.push(`profondeur     : ${r.profondeurFinale} / ${K.NB_PALIERS}`);
  l.push(`fin atteinte   : ${r.atteintLaFin ? 'oui' : 'NON'}`);
  const ratios = r.cycles.slice(1).map((cy, i) => cy.dureeH / r.cycles[i].dureeH);
  const moy = ratios.reduce((a, b) => a + b, 0) / Math.max(1, ratios.length);
  l.push(`ratio moyen    : ${fmt(moy, 3)}   (cible 1.18)`);
  return l.join('\n');
}

// ==================================================================== main

if (process.argv[1]?.endsWith('simulateur.ts')) {
  const contenu = construireContenu();
  console.log(`contenu : ${contenu.paliers.length} paliers, ${contenu.especes.length} espèces`);
  console.log(`m_p     : ${K.multPalier().toFixed(4)}`);
  console.log(rapport(simuler('optimale')));
  console.log(rapport(simuler('relachee')));
}
