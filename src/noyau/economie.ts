/**
 * IdlePond — production, coûts, seuils.
 *
 * UN SEUL CANAL DE REVENU — noyau v1.0 §10 : les espèces. Et une espèce est un
 * générateur avec un NIVEAU (§1.3) : on la débloque une fois, on monte son
 * niveau, l'effet est immédiat. Plus aucune population n'est simulée, donc plus
 * rien ne varie à l'intérieur d'un pas de tick.
 *
 * Les multiplicateurs qui s'ajoutent au canal sont des TermeDeFormule nommés,
 * jamais des facteurs anonymes : c'est ce qui rend le détail de captation
 * auditable (§7.5 règle 3, §8.2).
 */
import Decimal from 'break_infinity.js'
import type {
  Benediction,
  BenedictionId,
  Espece,
  EtatJeu,
  IndexPalier,
  LigneDeCaptation,
  TermeDeConfort,
  TermeDeCout,
} from './types'
import {
  BENEDICTION_CIBLEE_PAR_RANG,
  BENEDICTION_GLOBALE_PAR_RANG,
  BONUS_PAR_NIVEAU_DU_HEROS,
  COUT_CREUSER_AU_PALIER_1,
  COUT_DEBLOCAGE_RATIO,
  COUT_NIVEAU_PAR_DEBIT,
  BONUS_GLOBAL_A_CENT_INDIVIDUS,
  DEBIT_HEROS,
  ECHELLE_DE_PRODUCTION,
  FOI_COUT_DE_BENEDICTION_CIBLEE,
  FOI_COUT_DE_BENEDICTION_GLOBALE,
  NOMBRE_DE_PALIERS,
  RATIO_COUT_DE_BENEDICTION,
  RATIO_COUT_DE_CROISSANCE,
  SEUIL_D_ALERTE_DE_CONTENANCE,
  SEUILS_DE_JALON,
} from './constantes'
import { densiteDuSejour, multiplicateurDensite } from './densite'
import {
  debitBaseDuRang,
  puissanceDeG,
  puissanceDuCoutDeNiveau,
  puissanceDuMultiplicateurDePalier,
} from '../donnees/echelles'
import { ESPECES } from '../donnees/especes'
import { BENEDICTION_GLOBALE_ID, benedictionCibleeDe } from '../donnees/benedictions'
import { facteurDeTechnique } from './technique'
import { SUCCES } from '../donnees/succes/index'

/* ─── Seuils de jalon ───────────────────────────────────────────────────────*/

/**
 * Multiplicateur de seuil d'une espèce, d'après son NIVEAU (§2.C).
 *
 * La table donne le multiplicateur CUMULÉ lu au seuil : on retient celui du
 * seuil le plus haut franchi, on ne multiplie pas les colonnes entre elles.
 * Le niveau cent vaut ×16, jamais ×1024 — et `D = 2.31` a été calibré contre
 * cette lecture-là.
 *
 * Il se lit sur le niveau courant, donc il tombe à l'ACHAT et se reperd à
 * l'éclosion avec le niveau. Le seul acquis qui survit est le drapeau
 * permanent, plus bas.
 */
export function multiplicateurDeSeuil(niveau: number): number {
  let multiplicateur = 1
  for (const palier of SEUILS_DE_JALON) {
    if (niveau >= palier.seuil) multiplicateur = palier.multiplicateurCumule
  }
  return multiplicateur
}

/**
 * Bonus global des espèces ayant DÉJÀ atteint le niveau cent (§2.C).
 * Définitif, conservé à l'éclosion, additif entre espèces.
 */
export function multiplicateurDesDrapeaux(etat: EtatJeu): number {
  return 1 + BONUS_GLOBAL_A_CENT_INDIVIDUS * etat.permanent.especesAyantAtteintCent.length
}

/* ─── Production ────────────────────────────────────────────────────────────*/

/**
 * Débit de base d'une espèce, mana/s par niveau.
 *
 * « Chaque espèce nouvelle a un débit de base égal à la somme de toutes les
 * précédentes : elle double donc l'assiette additive à niveaux égaux. » Le
 * reste de `D` est porté par le multiplicateur de profondeur, plus bas — le
 * bestiaire seul ne le porte pas, puisque deux paliers sur trois n'apportent
 * aucune espèce.
 */
export function debitBaseDeLEspece(espece: Espece): Decimal {
  return debitBaseDuRang(espece.rang)
}

/* ─── Bénédictions — noyau v1.0 §4.2 ────────────────────────────────────────*/

export function rangDeBenediction(etat: EtatJeu, id: BenedictionId): number {
  return etat.permanent.benedictions[id] ?? 0
}

/**
 * Le débit de base d'une espèce, augmenté de la bénédiction GLOBALE : additif,
 * « sur le débit de base de toutes les espèces, présentes et futures ». Il
 * domine quand les débits sont minuscules et s'efface une fois les
 * multiplicateurs décollés — aucun ratio à régler.
 *
 * Le coût d'un niveau ne le lit PAS : il lit `debitBaseDeLEspece`. Bénir ne
 * renchérit rien.
 */
export function debitBeni(etat: EtatJeu, espece: Espece): Decimal {
  const rang = rangDeBenediction(etat, BENEDICTION_GLOBALE_ID)
  if (rang === 0) return debitBaseDeLEspece(espece)
  return debitBaseDeLEspece(espece).add(BENEDICTION_GLOBALE_PAR_RANG * rang)
}

/** La bénédiction CIBLÉE de l'espèce : `(1 + c) ^ rang`, empilable, 1 à rang 0. */
export function multiplicateurDeBenediction(etat: EtatJeu, espece: Espece): number {
  return Math.pow(1 + BENEDICTION_CIBLEE_PAR_RANG, rangDeBenediction(etat, benedictionCibleeDe(espece.id).id))
}

/**
 * Multiplicateur global accordé par la profondeur ouverte.
 *
 * Il porte la part de `D` que le bestiaire ne porte pas : sans lui, la
 * production croîtrait de 26 % par palier là où le coût croît de 140 %, et
 * l'écart se composerait jusqu'à rendre les cycles profonds interminables.
 */
export function multiplicateurDeProfondeur(etat: EtatJeu): Decimal {
  return puissanceDuMultiplicateurDePalier(Math.max(0, etat.cycle.paliersOuverts - 1))
}

/**
 * Multiplicateur global du niveau du héros — spec 2026-09-17 [D2].
 *
 * `(1 + b) ^ (niveau − 1)` : au niveau 1 il vaut exactement 1, et un cycle
 * dont le héros n'a jamais grandi produit ce qu'il produisait avant ce terme.
 * Sa part de `D` est retirée au multiplicateur de profondeur, pas ajoutée
 * par-dessus : voir `multiplicateurDePalier` dans `constantes.ts`.
 */
export function multiplicateurDuHeros(etat: EtatJeu): number {
  return Math.pow(1 + BONUS_PAR_NIVEAU_DU_HEROS, Math.max(0, etat.cycle.niveauDuHeros - 1))
}

/**
 * Tous les multiplicateurs globaux de la production, un seul produit — la
 * source commune à chaque espèce, au total, ET à la politique du simulateur
 * (`simulateur.ts`). Un multiplicateur ajouté ici vaut pour les trois sans
 * resaisie : une liste recopiée à la main, plutôt que prise ici, est
 * exactement ce qui avait laissé la densité hors du calcul du gain simulé
 * (revue de qualité de la tâche 9, finding 2).
 *
 * `ECHELLE_DE_PRODUCTION` y entre aussi, malgré son statut de simple cadran :
 * elle doit multiplier TOUT ce qui produit, pas seulement le total, sous
 * peine de refaire diverger la somme par espèce du total dès qu'elle
 * bougera (revue de qualité de la tâche 9, minor A).
 */
export function multiplicateursGlobaux(etat: EtatJeu): Decimal {
  return multiplicateurDeProfondeur(etat)
    .mul(multiplicateurDensite(densiteDuSejour(etat)))
    .mul(multiplicateurDuHeros(etat))
    .mul(multiplicateurDesDrapeaux(etat))
    .mul(ECHELLE_DE_PRODUCTION)
}

/** Ce qu'une espèce apporte à l'assiette additive, avant les multiplicateurs globaux. */
function debitDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  const vivante = etat.cycle.especes[espece.id]
  if (vivante === undefined || !vivante.debloquee || vivante.niveau === 0) return new Decimal(0)
  return debitBeni(etat, espece)
    .mul(vivante.niveau)
    .mul(multiplicateurDeSeuil(vivante.niveau))
    .mul(multiplicateurDeBenediction(etat, espece))
}

/** Ce qu'une espèce donne réellement par seconde, tous termes nommés appliqués. */
export function productionDeLEspece(etat: EtatJeu, espece: Espece): Decimal {
  if (espece.palier >= etat.cycle.paliersOuverts) return new Decimal(0)
  return debitDeLEspece(etat, espece).mul(multiplicateursGlobaux(etat))
}

/**
 * Ce que le débit du héros apporte RÉELLEMENT à la production, multiplicateurs
 * globaux compris — §7.5 règle 3 : même un débit qui n'appartient à aucune
 * espèce doit cibler un `TermeDeFormule`, jamais flotter hors du registre.
 * `DEBIT_HEROS` brut (voir `detailDuHeros` plus bas) ne suffit pas à expliquer
 * l'écart entre le total affiché et la somme des espèces à l'écran — c'est
 * cette valeur, multipliée, qui le fait, et c'est elle que
 * `src/ui/Contenance.tsx` affiche en regard du total.
 */
export function productionDuHeros(etat: EtatJeu): Decimal {
  return new Decimal(DEBIT_HEROS).mul(etat.cycle.niveauDuHeros).mul(multiplicateursGlobaux(etat))
}

/**
 * La somme des espèces débloquées, PLUS le débit propre du héros — noyau v1.0
 * §10 : un seul canal pour le bestiaire, et sa mutation à lui pour empêcher
 * l'état DÉGÉNÉRÉ où plus rien ne produirait jamais (RESULTATS.md, finding 3,
 * tâche 9). Le premier achat, lui, est tenu par la charge de départ
 * (`MANA_A_LA_SORTIE_DE_L_OEUF`, voir son commentaire dans `constantes.ts`) —
 * les deux mécanismes répondent à des besoins différents et ne se remplacent
 * pas l'un l'autre.
 *
 * Construite à partir de `productionDeLEspece` et `productionDuHeros`, pas
 * d'un second calcul de l'assiette : deux formules tenues manuellement en
 * synchronisation sont exactement ce qui avait fait nourrir le multiplicateur
 * de densité de deux grandeurs différentes — une somme sur les paliers d'un
 * côté, un maximum de l'autre. Un seul
 * calcul, appelé une fois par espèce plus une fois pour le héros, ne peut plus
 * diverger de lui-même.
 */
export function productionTotaleParSeconde(etat: EtatJeu): Decimal {
  const especes = ESPECES.reduce(
    (somme, espece) => somme.add(productionDeLEspece(etat, espece)),
    new Decimal(0),
  )
  return especes.add(productionDuHeros(etat))
}

/**
 * Ce que le débit BRUT du héros vaut, nommé — `DEBIT_HEROS` avant tout
 * multiplicateur. Vit ICI, à côté du total qu'il explique
 * (`productionTotaleParSeconde`, juste au-dessus), plutôt que dans
 * `detailDeCaptation` plus bas : ce dernier est attributable à UNE espèce, et
 * le héros n'en porte aucune. La valeur RÉELLEMENT captée — celle à afficher —
 * est `productionDuHeros`, pas la valeur brute d'ici : §8.2 veut la
 * contrepartie d'un effet, pas son seul nom.
 */
export function detailDuHeros(etat: EtatJeu): readonly LigneDeCaptation[] {
  const source = { quoi: 'heros', niveau: etat.cycle.niveauDuHeros } as const
  return [
    { terme: 'debit_heros', valeur: DEBIT_HEROS, source },
    { terme: 'multiplicateur_heros', valeur: multiplicateurDuHeros(etat), source },
  ]
}

/**
 * Détail de la captation (§8.2) : chaque terme actif attribuable à sa source.
 * C'est la contrepartie obligatoire d'un effet appliqué silencieusement.
 */
export function detailDeCaptation(etat: EtatJeu, espece: Espece): readonly LigneDeCaptation[] {
  const niveau = etat.cycle.especes[espece.id]?.niveau ?? 0
  const densite = densiteDuSejour(etat)
  const beniRang = rangDeBenediction(etat, BENEDICTION_GLOBALE_ID)
  const beniCibleeRang = rangDeBenediction(etat, benedictionCibleeDe(espece.id).id)
  const lignes: LigneDeCaptation[] = [
    { terme: 'niveau', valeur: niveau, source: { quoi: 'niveau', niveau } },
    {
      terme: 'taux_base',
      valeur: debitBaseDeLEspece(espece).toNumber(),
      source: { quoi: 'palier', palier: espece.palier },
    },
    {
      terme: 'benediction_globale',
      valeur: debitBeni(etat, espece).div(debitBaseDeLEspece(espece)).toNumber(),
      source: { quoi: 'benediction', rang: beniRang },
    },
  ]
  lignes.push({
    terme: 'multiplicateur_jalon',
    valeur: multiplicateurDeSeuil(niveau),
    source: { quoi: 'niveau', niveau },
  })
  lignes.push({
    terme: 'multiplicateur_benediction',
    valeur: multiplicateurDeBenediction(etat, espece),
    source: { quoi: 'benediction', rang: beniCibleeRang },
  })
  lignes.push(
    {
      terme: 'multiplicateur_drapeau',
      valeur: multiplicateurDesDrapeaux(etat),
      source: { quoi: 'drapeaux_permanents', especes: etat.permanent.especesAyantAtteintCent.length },
    },
    {
      terme: 'multiplicateur_profondeur',
      valeur: multiplicateurDeProfondeur(etat).toNumber(),
      source: { quoi: 'profondeur', paliersOuverts: etat.cycle.paliersOuverts },
    },
    {
      terme: 'multiplicateur_densite',
      valeur: multiplicateurDensite(densite),
      source: { quoi: 'densite', densite },
    },
    {
      terme: 'multiplicateur_heros',
      valeur: multiplicateurDuHeros(etat),
      source: { quoi: 'heros', niveau: etat.cycle.niveauDuHeros },
    },
  )
  return lignes
}

/* ─── Coûts ─────────────────────────────────────────────────────────────────*/

/**
 * Facteur appliqué à un terme de coût par les succès acquis.
 *
 * Il vit ici plutôt que dans succes.ts pour que les dépendances restent à sens
 * unique : succes.ts lit la production, l'économie lit les effets. Un cycle
 * d'imports entre les deux tiendrait à l'exécution et tomberait au premier
 * changement d'ordre d'initialisation.
 */
export function facteurDeSucces(etat: EtatJeu, terme: TermeDeCout | TermeDeConfort): number {
  let facteur = 1
  for (const succes of SUCCES) {
    const effet = succes.effet
    if (effet === null || effet.genre === 'verbe') continue
    if (effet.terme !== terme) continue
    // Lu directement, jamais via `succes.ts` : ce module y est importé, et un
    // cycle d'imports tiendrait à l'exécution pour tomber au premier
    // changement d'ordre d'initialisation.
    if (etat.permanent.succes[succes.id] === undefined) continue
    // `part` est la fraction retirée d'un coût, ou ajoutée à un plafond.
    facteur *= effet.genre === 'reduction_cout' ? 1 - effet.part : 1 + effet.part
  }
  return facteur
}

/** Technique et succès se composent sur un même terme, chacun nommé et attribuable. */
function facteurDeCout(etat: EtatJeu, terme: TermeDeCout): number {
  return facteurDeTechnique(etat, terme) * facteurDeSucces(etat, terme)
}

/** Coût d'origine d'un palier, avant tout levier. Le palier 0 est ouvert au départ. */
export function coutBaseDuPalier(cible: IndexPalier): Decimal {
  return puissanceDeG(Math.max(0, cible - 1)).mul(COUT_CREUSER_AU_PALIER_1)
}

/**
 * Ce que coûte de descendre d'un palier — un seul puits, noyau v1.0 §3.1.
 *
 *   coût_base(palier) × technique × succès
 *
 * `f` valait 1 depuis toujours : le noyau v1.0 §3.1 ferme [P5] et retire le
 * tarif réduit qu'un palier déjà atteint dans une vie passée payait avant le
 * 2026-09-09. Retraverser coûte exactement ce qu'un creusement neuf coûterait
 * — la profondeur maximale atteinte n'entre plus dans ce calcul.
 */
export function coutDeDescente(etat: EtatJeu, cible: IndexPalier): Decimal {
  return coutBaseDuPalier(cible)
    .mul(facteurDeTechnique(etat, 'cout_creuser'))
    .mul(facteurDeSucces(etat, 'cout_creuser'))
}

/**
 * Ce que coûte de débloquer une espèce — noyau v1.0 §1.3.
 *
 *   coût_base(palier de l'espèce) × COUT_DEBLOCAGE_RATIO
 *
 * Une fois par espèce et par vie. Le déblocage suit le coût de son palier
 * plutôt qu'une échelle à lui : c'est la profondeur où elle vit qui dit ce
 * qu'il en coûte de l'atteindre.
 *
 * La densité n'entre plus ici. Elle payait la reconviction (GDD §7.1) tant
 * qu'il y avait une population à reconvaincre ; elle n'a plus qu'un seul
 * débouché, la production, par le multiplicateur de densité, et le coût de
 * déblocage est redevenu un levier ordinaire.
 */
export function coutDeDeblocage(etat: EtatJeu, espece: Espece): Decimal {
  return coutBaseDuPalier(espece.palier)
    .mul(COUT_DEBLOCAGE_RATIO)
    .mul(facteurDeCout(etat, 'cout_deblocage'))
}

/**
 * Coût du niveau suivant. Achat répétable de la boucle, ×1.15.
 *
 * Il suit le débit de base de SON espèce, donc le temps de remboursement d'un
 * niveau est le même pour la première espèce et pour la vingt et unième.
 */
export function coutDeNiveau(etat: EtatJeu, espece: Espece, niveau: number): Decimal {
  return debitBaseDeLEspece(espece)
    .mul(COUT_NIVEAU_PAR_DEBIT)
    .mul(puissanceDuCoutDeNiveau(Math.max(0, niveau)))
    .mul(facteurDeCout(etat, 'cout_niveau'))
}

/**
 * Ce que coûte de faire grandir le héros de `niveau` à `niveau + 1` — spec
 * 2026-09-17 [D3] : une fraction du coût du palier de même rang, donc `g^(n−1)`.
 * Le joueur optimal en paie à peu près un par palier, et c'est ce qui autorise
 * le rebudget de `D` dans `multiplicateurDePalier`.
 */
export function coutDeCroissance(etat: EtatJeu, niveau: number): Decimal {
  return puissanceDeG(Math.max(0, niveau - 1))
    .mul(COUT_CREUSER_AU_PALIER_1)
    .mul(RATIO_COUT_DE_CROISSANCE)
    .mul(facteurDeCout(etat, 'cout_croissance'))
}

/**
 * Ce que coûte le rang suivant d'une bénédiction, EN FOI — spec 2026-09-17
 * [D6]. Géométrique : `base × ratio ^ rang`. `cout_benediction` est un terme
 * de coût nommé, donc la technique et les succès pourront le viser.
 */
export function coutDeBenediction(etat: EtatJeu, benediction: Benediction): Decimal {
  const base = benediction.portee === 'globale' ? FOI_COUT_DE_BENEDICTION_GLOBALE : FOI_COUT_DE_BENEDICTION_CIBLEE
  return new Decimal(base)
    .mul(Decimal.pow(RATIO_COUT_DE_BENEDICTION, rangDeBenediction(etat, benediction.id)))
    .mul(facteurDeCout(etat, 'cout_benediction'))
}

/* ─── Contenance et blocage doux (§6.4) ─────────────────────────────────────*/

/** La contenance limite le stock, pas la production. */
export function contenance(etat: EtatJeu): Decimal {
  return etat.permanent.contenanceMana.mul(1 + etat.cycle.acquisDeSejour)
}

/* ─── La jauge et sa saturation — GDD §2.4 ──────────────────────────────────
 *
 * « Un joueur qui ignore sa jauge n'est jamais bloqué et ne perd jamais sa
 * partie. C'est la seule pénalité du jeu, et elle est douce. »
 */

/** Part du plafond effectivement portée, de 0 à 1. */
export function partDeContenance(etat: EtatJeu): number {
  const plafond = contenance(etat)
  if (plafond.lte(0)) return 0
  return Math.min(1, etat.cycle.manaCourant.div(plafond).toNumber())
}

/**
 * L'alerte : « l'eau se trouble, la faune s'écarte. Un effet, pas un texte. »
 *
 * Le noyau rend l'état, jamais l'effet : c'est à l'écran de le montrer sans
 * l'écrire.
 */
export function eauTroublee(etat: EtatJeu): boolean {
  return partDeContenance(etat) >= SEUIL_D_ALERTE_DE_CONTENANCE
}

/** Saturation : « la captation s'arrête. Il dépense encore, il ne gagne plus. » */
export function estSature(etat: EtatJeu): boolean {
  return etat.cycle.manaCourant.gte(contenance(etat))
}

/** Plus rien à creuser : soit la roche est finie, soit le contenu l'est. */
export function toutEstCreuse(etat: EtatJeu): boolean {
  return etat.cycle.paliersOuverts >= Math.min(etat.limiteDeContenu, NOMBRE_DE_PALIERS)
}

/**
 * Le blocage doux : le palier suivant coûte plus que ce que la contenance peut
 * porter. Le joueur peut continuer à monter des niveaux et à faire grossir sa
 * Foi ; il ne peut simplement plus descendre. C'est la raison diégétique de
 * l'éclosion, et sa seule vraie décision : partir maintenant pour la
 * profondeur, ou rester pour la Foi.
 */
export function estBloque(etat: EtatJeu): boolean {
  if (toutEstCreuse(etat)) return true
  return coutDeDescente(etat, etat.cycle.paliersOuverts).gt(contenance(etat))
}
