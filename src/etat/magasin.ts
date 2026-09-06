/**
 * IdlePond — le magasin Zustand.
 *
 * MIROIR de `EtatJeu`, aucune logique métier (§5.3). Chaque acte du joueur est
 * un appel au réducteur pur du noyau : si une règle du jeu se retrouvait ici,
 * elle serait invisible au simulateur et intestable — c'est exactement ce que
 * le découpage cherche à empêcher.
 */
import { create } from 'zustand'
import { persist, type PersistStorage } from 'zustand/middleware'
import type { BancId, EtatJeu, SuccesId } from '../noyau/types'
import { convaincre, creuser, eclore, etatInitial, monterNiveau } from '../noyau/noyau'
import { PALIERS_LIVRES } from '../donnees/assises'
import { SECONDES_MINIMALES_POUR_ANNONCER_LE_RETOUR } from '../noyau/constantes'
import { deserialiser, serialiser, type SaveSerialisee } from '../adaptateurs/persistance'
import { crediterHorsLigne } from '../adaptateurs/hors-ligne'
import { grainePourNouvellePartie, horlogeSysteme } from '../adaptateurs/horloge'

export const CLEF_DE_SAUVEGARDE = 'idlepond'

export interface RetourAffiche {
  readonly secondesCreditees: number
}

interface Magasin {
  etat: EtatJeu
  /** Dernier instant enregistré, pour le crédit hors ligne. */
  dernierInstantMs: number
  retour: RetourAffiche | null
  /** Succès à annoncer : une ligne qui apparaît et s'efface, jamais une fenêtre. */
  aAnnoncer: readonly SuccesId[]

  remplacer(etat: EtatJeu): void
  creuser(): void
  convaincre(banc: BancId): void
  monterNiveau(banc: BancId): void
  eclore(): void
  reprendre(): void
  annoncer(declenches: readonly SuccesId[]): void
  oublierAnnonce(id: SuccesId): void
  oublierRetour(): void
}

function nouvellePartie(): EtatJeu {
  return etatInitial(grainePourNouvellePartie(), PALIERS_LIVRES)
}

/**
 * Le stockage passe par la sérialisation du noyau : `versionSave` et la chaîne
 * de migrations restent les nôtres. Le versionnage de `persist` ferait un
 * second compteur de version à côté du premier, et deux compteurs de version
 * finissent toujours par diverger.
 */
const stockage: PersistStorage<{ etat: EtatJeu; dernierInstantMs: number }> = {
  getItem: (nom) => {
    const brut = localStorage.getItem(nom)
    if (brut === null) return null
    try {
      const save = JSON.parse(brut) as SaveSerialisee & { dernierInstantMs?: number }
      return {
        state: {
          etat: deserialiser(save, nouvellePartie()),
          dernierInstantMs: save.dernierInstantMs ?? horlogeSysteme.maintenantMs(),
        },
      }
    } catch {
      // Une save illisible ne doit pas bloquer le jeu sur un écran blanc.
      return null
    }
  },
  setItem: (nom, valeur) => {
    localStorage.setItem(
      nom,
      JSON.stringify({ ...serialiser(valeur.state.etat), dernierInstantMs: valeur.state.dernierInstantMs }),
    )
  },
  removeItem: (nom) => localStorage.removeItem(nom),
}

export const useMagasin = create<Magasin>()(
  persist(
    (set, get) => ({
      etat: nouvellePartie(),
      dernierInstantMs: horlogeSysteme.maintenantMs(),
      retour: null,
      aAnnoncer: [],

      remplacer: (etat) => set({ etat, dernierInstantMs: horlogeSysteme.maintenantMs() }),
      creuser: () => set({ etat: creuser(get().etat) }),
      convaincre: (banc) => set({ etat: convaincre(get().etat, banc) }),
      monterNiveau: (banc) => set({ etat: monterNiveau(get().etat, banc) }),
      eclore: () => set({ etat: eclore(get().etat) }),

      /** Un seul appel à tick pour toute l'absence. Rien ne s'est dégradé. */
      reprendre: () => {
        const { etat, dernierInstantMs } = get()
        const maintenant = horlogeSysteme.maintenantMs()
        const credit = crediterHorsLigne(etat, dernierInstantMs, maintenant)
        set({
          etat: credit.etat,
          dernierInstantMs: maintenant,
          retour:
            credit.secondesCreditees >= SECONDES_MINIMALES_POUR_ANNONCER_LE_RETOUR
              ? { secondesCreditees: credit.secondesCreditees }
              : null,
        })
      },

      annoncer: (declenches) => set({ aAnnoncer: [...get().aAnnoncer, ...declenches] }),
      oublierAnnonce: (id) => set({ aAnnoncer: get().aAnnoncer.filter((autre) => autre !== id) }),
      oublierRetour: () => set({ retour: null }),
    }),
    {
      name: CLEF_DE_SAUVEGARDE,
      storage: stockage,
      partialize: (magasin) => ({ etat: magasin.etat, dernierInstantMs: magasin.dernierInstantMs }),
    },
  ),
)
