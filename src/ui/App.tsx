/**
 * IdlePond — l'écran du jalon v0.2 : l'assise I est jouable.
 *
 * La boucle du genre, sans exception : débloquer → améliorer → réinitialiser.
 * Pas de clic obligatoire, pas de mini-jeu, pas de fenêtre de temps — aucune
 * présence active n'est requise (§4.2). Tout ce qui se passe ici se passerait
 * aussi bien sans personne devant l'écran.
 */
import { useEffect, useState } from 'react'
import { useMagasin } from '../etat/magasin'
import { creerBoucle } from '../adaptateurs/boucle'
import { montant } from './format'
import { Contenance } from './Contenance'
import { Mare } from './Mare'
import { Eclosion } from './Eclosion'
import { Succes } from './Succes'
import { Captation } from './Captation'
import { Annonces } from './Annonces'
import { Retour } from './Retour'

export function App() {
  const etat = useMagasin((m) => m.etat)
  const annonces = useMagasin((m) => m.aAnnoncer)
  const retour = useMagasin((m) => m.retour)
  const [captation, setCaptation] = useState<string | null>(null)

  useEffect(() => {
    const magasin = useMagasin.getState()
    magasin.reprendre()
    const boucle = creerBoucle({
      lire: () => useMagasin.getState().etat,
      ecrire: (suivant) => useMagasin.getState().remplacer(suivant),
      surSucces: (declenches) => useMagasin.getState().annoncer(declenches),
    })
    boucle.demarrer()
    return () => boucle.arreter()
  }, [])

  return (
    <div className="mx-auto flex min-h-full max-w-5xl flex-col gap-6 p-4 sm:p-6">
      <header className="flex items-baseline justify-between gap-4 border-b border-eau-bord pb-3">
        <h1 className="font-texte text-xl">IdlePond</h1>
        <dl className="flex items-baseline gap-5 text-sm">
          <div className="flex items-baseline gap-1.5">
            <dt className="text-jour-tu">Foi</dt>
            <dd className="font-chiffre text-foi tabular-nums">{montant(etat.permanent.foi)}</dd>
          </div>
          <div className="flex items-baseline gap-1.5">
            <dt className="text-jour-tu">retours dans l’œuf</dt>
            <dd className="font-chiffre tabular-nums">{etat.permanent.nombreEclosions}</dd>
          </div>
        </dl>
      </header>

      <Retour retour={retour} surFermeture={() => useMagasin.getState().oublierRetour()} />

      <div className="grid gap-6 md:grid-cols-[minmax(0,1fr)_minmax(0,20rem)]">
        <main className="space-y-4">
          <Contenance etat={etat} />
          {captation !== null ? (
            <Captation etat={etat} banc={captation} surFermeture={() => setCaptation(null)} />
          ) : null}
          <Mare
            etat={etat}
            surConviction={(banc) => useMagasin.getState().convaincre(banc)}
            surNiveau={(banc) => useMagasin.getState().monterNiveau(banc)}
            surCreusement={() => useMagasin.getState().creuser()}
            surCaptation={setCaptation}
          />
          <Eclosion etat={etat} surEclosion={() => useMagasin.getState().eclore()} />
        </main>

        <aside>
          <Succes etat={etat} />
        </aside>
      </div>

      <Annonces annonces={annonces} surExpiration={(id) => useMagasin.getState().oublierAnnonce(id)} />
    </div>
  )
}
