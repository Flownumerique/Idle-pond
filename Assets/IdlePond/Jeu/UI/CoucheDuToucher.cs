using System;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;
using Decimal = IdlePond.Noyau.Decimal;
using E = IdlePond.Noyau.Donnees.Textes.Ecran;

namespace IdlePond.Jeu.UI
{
    /// <summary>
    /// Toucher la mare — spec du 2026-10-11, §2. Une couche sur toute la mare, au-dessus de la
    /// scène et sous tout le reste : on touche l'eau entière, sauf ses boutons. Tiroir ouvert,
    /// le voile est au-dessus d'elle et garde son rôle (toucher referme, ne récolte pas).
    ///
    /// Le gain s'envole du point touché : `+gain` en couleur mana, qui monte d'environ 48 px
    /// et s'efface en 0,7 s (le .uss porte le mouvement ; mouvement réduit, il s'efface sans
    /// monter). Six libellés réutilisés à tour de rôle : aucune allocation par toucher. Gain
    /// nul — mare pleine, ou toucher ignoré par la cadence — : pas de chiffre.
    /// </summary>
    public sealed class CoucheDuToucher
    {
        public const int LIBELLES = 6;
        const long DUREE_MS = 700;

        readonly Label[] envols = new Label[LIBELLES];
        readonly IVisualElementScheduledItem[] fins = new IVisualElementScheduledItem[LIBELLES];
        int prochain;

        public CoucheDuToucher(VisualElement racine, Func<Decimal> toucher)
        {
            racine.pickingMode = PickingMode.Position;
            for (var i = 0; i < LIBELLES; i++)
            {
                var envol = Elements.Texte("chiffre mana lg envol");
                envol.pickingMode = PickingMode.Ignore;
                Elements.Montrer(envol, false);
                racine.Add(envol);
                envols[i] = envol;
            }
            racine.RegisterCallback<PointerDownEvent>(evenement =>
            {
                var gain = toucher();
                if (gain.Gt(0)) Envoler(gain, evenement.localPosition);
            });
        }

        void Envoler(Decimal gain, Vector2 point)
        {
            var i = prochain;
            prochain = (prochain + 1) % LIBELLES;
            var envol = envols[i];
            fins[i]?.Pause();
            envol.RemoveFromClassList("envol-parti");
            Elements.Poser(envol, Format.Remplir(E.GAIN_DU_TOUCHER, Format.Montant(gain)));
            envol.style.left = point.x;
            envol.style.top = point.y;
            Elements.Montrer(envol, true);
            envol.BringToFront();
            // Une image plus tard : posée dans le même temps que l'affichage, la classe
            // n'enclencherait aucune transition, et le chiffre sauterait à sa fin.
            envol.schedule.Execute(() => envol.AddToClassList("envol-parti")).StartingIn(16);
            fins[i] = envol.schedule.Execute(() =>
            {
                Elements.Montrer(envol, false);
                envol.RemoveFromClassList("envol-parti");
            }).StartingIn(DUREE_MS + 16);
        }
    }
}
