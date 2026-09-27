/* HARNAIS DE MESURE — NON INTÉGRÉ AU JEU.
 * Ces fichiers ont produit docs/RESULTATS.md. Ils servent de référence de
 * lecture pour le modèle du noyau v1.0, jamais de code de production : le jeu
 * vit dans src/, avec Decimal et l'état immuable. Ne pas importer depuis src/.
 */

/**
 * IdlePond — noyau pur.
 *
 * Contraintes d'architecture (§8.2 du noyau v1.0) :
 *  - aucun Date.now() ici ;
 *  - aucun état hors de GameState ;
 *  - tout se calcule en UN pas pour dt = 8 h.
 *
 * Le jeu appellera tick() à 100 ms, le simulateur à dt = 60 s.
 * C'est le même code.
 */

import * as K from './constantes.js';

// ============================================================ contenu généré

export interface Palier {
  index: number;          // 0..61, global
  assise: number;         // 0..5
  indexDansAssise: number;
  espece: number | null;  // index d'espèce si ce palier en apporte une
  cout: number;
}

export interface Espece {
  index: number;
  assise: number;
  palier: number;
  debitBase: number;
  coutDeblocage: number;
  coutNiveauBase: number;
}

export interface Contenu {
  paliers: Palier[];
  especes: Espece[];
}

/** Construit la géographie et le bestiaire depuis les seules constantes. */
export function construireContenu(): Contenu {
  const paliers: Palier[] = [];
  const especes: Espece[] = [];

  let index = 0;
  for (let assise = 0; assise < K.PALIERS_PAR_ASSISE.length; assise++) {
    const n = K.PALIERS_PAR_ASSISE[assise];
    for (let i = 0; i < n; i++) {
      const apporteEspece = i % K.ESPECE_TOUS_LES === 0;
      const cout = K.COUT_CREUSER_BASE * Math.pow(K.G, index);

      let especeIndex: number | null = null;
      if (apporteEspece) {
        especeIndex = especes.length;
        especes.push({
          index: especeIndex,
          assise,
          palier: index,
          debitBase: K.DEBIT_BASE * Math.pow(K.DEBIT_RATIO_ESPECE, especeIndex),
          coutDeblocage: cout * K.COUT_DEBLOCAGE_RATIO,
          coutNiveauBase: K.COUT_NIVEAU_BASE * Math.pow(K.DEBIT_RATIO_ESPECE, especeIndex),
        });
      }

      paliers.push({ index, assise, indexDansAssise: i, espece: especeIndex, cout });
      index++;
    }
  }

  return { paliers, especes };
}

// ==================================================================== état

export interface GameState {
  mana: number;
  contenance: number;
  densite: number;
  foi: number;
  eclosions: number;

  paliersOuverts: number;          // les paliers s'ouvrent dans l'ordre
  especes: { debloquee: boolean; niveau: number }[];

  profondeurMax: number;           // record absolu, sur toutes les vies
  prodMaxCycle: number;            // production de pointe du cycle en cours
  bonusGlobalPermanent: number;    // nb d'espèces ayant atteint 100 au moins une fois
  tempsJoue: number;               // secondes, monotone
  tempsCycle: number;              // secondes depuis la dernière éclosion
}

export function etatInitial(contenu: Contenu): GameState {
  return {
    mana: 0,
    contenance: K.CONTENANCE_INITIALE,
    densite: 0,
    foi: 0,
    eclosions: 0,
    paliersOuverts: 1,             // le premier palier est ouvert d'office
    especes: contenu.especes.map(() => ({ debloquee: false, niveau: 0 })),
    profondeurMax: 1,
    prodMaxCycle: 0,
    bonusGlobalPermanent: 0,
    tempsJoue: 0,
    tempsCycle: 0,
  };
}

// ============================================================== production

function multSeuil(niveau: number): number {
  let m = 1;
  for (const [seuil, mult] of K.SEUILS) if (niveau >= seuil) m = mult;
  return m;
}

export function multDensite(s: GameState): number {
  return Math.pow(1 + s.densite / K.DENSITE_REF, K.densiteExposant());
}

/** Bonus global des espèces ayant atteint le niveau 100 — DÉFINITIF, survit à l'éclosion. */
function multBonusGlobal(s: GameState): number {
  return 1 + s.bonusGlobalPermanent * K.BONUS_GLOBAL_NIVEAU_100;
}

export function assietteDe(s: GameState, c: Contenu): number {
  let assiette = K.DEBIT_HEROS;
  for (let i = 0; i < s.especes.length; i++) {
    const e = s.especes[i];
    if (!e.debloquee || e.niveau === 0) continue;
    assiette += c.especes[i].debitBase * e.niveau * multSeuil(e.niveau);
  }
  return assiette;
}

export function production(s: GameState, c: Contenu): number {
  const assiette = assietteDe(s, c);
  const multProfondeur = Math.pow(K.multPalier(), s.paliersOuverts - 1);
  return assiette * multProfondeur * multDensite(s) * multBonusGlobal(s) * K.P.ECHELLE;
}

// =================================================================== achats

export type Achat =
  | { type: 'creuser'; cout: number; gain: number }
  | { type: 'debloquer'; espece: number; cout: number; gain: number }
  | { type: 'niveau'; espece: number; cout: number; gain: number };

export function coutNiveau(c: Contenu, espece: number, niveau: number): number {
  return c.especes[espece].coutNiveauBase * Math.pow(K.COUT_NIVEAU_CROISSANCE, niveau);
}

/**
 * Gains marginaux, calculés analytiquement.
 *
 * La production est un produit de facteurs dont un seul dépend de l'achat :
 *   prod = assiette × multProfondeur × multDensité × multBonus × échelle
 * On peut donc dériver chaque gain en O(1), sans copier l'état. C'est ce qui
 * rend le calibrage possible : la version naïve recopiait l'état des milliards
 * de fois.
 */
export function achatsDisponibles(s: GameState, c: Contenu): Achat[] {
  const out: Achat[] = [];
  const assiette = assietteDe(s, c);
  const prod = production(s, c);
  const parUniteAssiette = prod / assiette;

  if (s.paliersOuverts < c.paliers.length) {
    out.push({
      type: 'creuser',
      cout: c.paliers[s.paliersOuverts].cout,
      gain: prod * (K.multPalier() - 1),
    });
  }

  for (let i = 0; i < s.especes.length; i++) {
    const esp = c.especes[i];
    if (esp.palier >= s.paliersOuverts) continue;
    const e = s.especes[i];

    if (!e.debloquee) {
      out.push({
        type: 'debloquer',
        espece: i,
        cout: esp.coutDeblocage,
        gain: esp.debitBase * 1 * multSeuil(1) * parUniteAssiette,
      });
    } else {
      const avant = e.niveau * multSeuil(e.niveau);
      const apres = (e.niveau + 1) * multSeuil(e.niveau + 1);
      let gain = esp.debitBase * (apres - avant) * parUniteAssiette;
      // le seuil 100 ouvre en plus un bonus de production GLOBALE, définitif
      if (e.niveau + 1 === 100) gain += prod * K.BONUS_GLOBAL_NIVEAU_100;
      out.push({ type: 'niveau', espece: i, cout: coutNiveau(c, i, e.niveau), gain });
    }
  }

  return out;
}

export function appliquer(s: GameState, c: Contenu, a: Achat): void {
  s.mana -= a.cout;
  if (a.type === 'creuser') {
    s.paliersOuverts++;
    if (s.paliersOuverts > s.profondeurMax) s.profondeurMax = s.paliersOuverts;
  } else if (a.type === 'debloquer') {
    s.especes[a.espece].debloquee = true;
    s.especes[a.espece].niveau = 1;
  } else {
    s.especes[a.espece].niveau++;
    if (s.especes[a.espece].niveau === 100) s.bonusGlobalPermanent++;
  }
}

// ==================================================================== tick

/** Pas d'intégration. Pur, sans effet de bord hors de `s`. */
export function tick(s: GameState, c: Contenu, dt: number): void {
  const prod = production(s, c);
  if (prod > s.prodMaxCycle) s.prodMaxCycle = prod;
  s.mana = Math.min(s.contenance, s.mana + prod * dt);
  // La Foi est log-amortie pour ne pas exploser avec la production.
  s.foi += K.FOI_TAUX * Math.log10(1 + prod) * dt;
  s.tempsJoue += dt;
  s.tempsCycle += dt;
}

// ================================================================ éclosion

/** Le joueur est bloqué : le palier suivant coûte plus qu'il ne peut contenir. */
export function bloque(s: GameState, c: Contenu): boolean {
  if (s.paliersOuverts >= c.paliers.length) return true;
  return c.paliers[s.paliersOuverts].cout > s.contenance;
}

export function eclore(s: GameState, c: Contenu): void {
  // Le gain de densité est indexé sur la PRODUCTION DE POINTE, pas sur la
  // profondeur. Une prestige-currency indexée sur la profondeur croît
  // linéairement, donc son effet relatif s'effondre : elle accélère fort au
  // début et plus du tout à la fin, ce qui creuse la courbe en U.
  const gainDensite = Math.pow(s.prodMaxCycle, K.P.ALPHA);

  s.densite += gainDensite;
  s.contenance *= Math.pow(K.G, K.paliersGagnes(s.eclosions));
  s.eclosions++;

  // reset complet : f = 1
  s.mana = 0;
  s.paliersOuverts = 1;
  s.especes = s.especes.map(() => ({ debloquee: false, niveau: 0 }));
  s.tempsCycle = 0;
  s.prodMaxCycle = 0;
}
