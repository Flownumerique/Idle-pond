/**
 * L'écran des succès (§8.3).
 *
 * | État    | Ce que le joueur voit                    |
 * |---------|------------------------------------------|
 * | Ouvert  | Nom + condition + barre de progression   |
 * | Fermé   | Nom seul, condition masquée              |
 * | Secret  | Emplacement vide, rien d'autre           |
 *
 * Verrouillage par assise : un succès n'est listé, quel que soit son état, que
 * lorsque son assise est atteinte. PAS de compteur global de secrets — des
 * emplacements vides par assise, ce qui dit qu'il y a quelque chose là sans
 * dire combien il en reste ailleurs.
 *
 * Les trois états sont groupés plutôt qu'entrelacés, et ce n'est pas cosmétique :
 * égrenés dans l'ordre du registre, neuf emplacements secrets consécutifs se
 * lisent comme un défaut d'affichage, et quarante lignes de conditions écrasent
 * la mare qui, elle, en fait quatre. Ce qui est ARRIVÉ passe donc devant : la
 * narration est une récompense distribuée à la cadence idle (§4.2), pas une
 * liste de courses.
 */
import type { EtatJeu, SuccesId } from '../noyau/types'
import { progressionVersLeSucces, succesListables, type SuccesAffichable } from '../noyau/succes'
import { ASSISES } from '../donnees/assises'
import { texteDuSucces } from '../donnees/textes-provisoires'
import { nomDeLAssise } from './format'

function Acquis({ id }: { readonly id: SuccesId }) {
  const texte = texteDuSucces(id)
  return (
    <li className="rounded-md border border-eau-clair bg-eau-fond/50 px-3 py-2">
      <p className="font-texte text-sm text-jour">{texte.nom}</p>
      <p className="mt-0.5 font-texte text-sm italic text-jour-tu">{texte.rapport}</p>
    </li>
  )
}

function EnCours({ etat, entree }: { readonly etat: EtatJeu; readonly entree: SuccesAffichable }) {
  const texte = texteDuSucces(entree.succes.id)
  const progression = progressionVersLeSucces(etat, entree.succes)
  return (
    <li className="rounded-md border border-eau-bord px-3 py-2">
      <p className="font-texte text-sm text-jour-doux">{texte.nom}</p>
      <p className="mt-0.5 text-xs text-jour-tu">{texte.condition}</p>
      {progression !== null ? (
        <div className="mt-1.5 h-0.5 overflow-hidden rounded-full bg-eau-abysse">
          <div className="h-full bg-eau-clair" style={{ width: `${progression * 100}%` }} />
        </div>
      ) : null}
    </li>
  )
}

export function Succes({ etat }: { readonly etat: EtatJeu }) {
  const assise = ASSISES[0].id
  const liste = succesListables(etat, assise)

  const acquis = liste.filter((e) => e.acquis)
  const ouverts = liste.filter((e) => !e.acquis && e.visibilite === 'ouvert')
  const fermes = liste.filter((e) => !e.acquis && e.visibilite === 'ferme')
  const secrets = liste.filter((e) => !e.acquis && e.visibilite === 'secret')

  return (
    <section className="flex max-h-[calc(100vh-8rem)] flex-col gap-3 md:sticky md:top-6">
      <h2 className="font-texte text-lg text-jour-doux">
        Ce qui est arrivé — {nomDeLAssise(assise)}
      </h2>

      <div className="min-h-0 flex-1 space-y-4 overflow-y-auto pr-1">
        {acquis.length > 0 ? (
          <ul className="space-y-1">
            {[...acquis].reverse().map((entree) => (
              <Acquis key={entree.succes.id} id={entree.succes.id} />
            ))}
          </ul>
        ) : (
          <p className="text-sm text-jour-tu">Rien encore. Ça vient vite.</p>
        )}

        {ouverts.length > 0 ? (
          <div className="space-y-1">
            <h3 className="text-xs uppercase tracking-wide text-jour-tu">En chemin</h3>
            <ul className="space-y-1">
              {ouverts.map((entree) => (
                <EnCours key={entree.succes.id} etat={etat} entree={entree} />
              ))}
            </ul>
          </div>
        ) : null}

        {fermes.length > 0 ? (
          <div className="space-y-1">
            <h3 className="text-xs uppercase tracking-wide text-jour-tu">Plus loin</h3>
            <ul className="flex flex-wrap gap-1">
              {fermes.map((entree) => (
                <li
                  key={entree.succes.id}
                  className="rounded border border-eau-bord px-2 py-1 font-texte text-xs text-jour-tu"
                >
                  {texteDuSucces(entree.succes.id).nom}
                </li>
              ))}
            </ul>
          </div>
        ) : null}

        {secrets.length > 0 ? (
          <div className="space-y-1">
            <h3 className="text-xs uppercase tracking-wide text-jour-tu">
              <span className="sr-only">Emplacements vides</span>
              <span aria-hidden>·</span>
            </h3>
            <ul aria-hidden className="flex flex-wrap gap-1">
              {secrets.map((entree) => (
                <li
                  key={entree.succes.id}
                  className="h-5 w-5 rounded border border-dashed border-eau-bord/60"
                />
              ))}
            </ul>
          </div>
        ) : null}
      </div>
    </section>
  )
}
