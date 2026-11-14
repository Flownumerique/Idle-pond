using System;
using System.Globalization;
using IdlePond.Noyau;
using UnityEngine;
using UnityEngine.UIElements;
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
    /// monter). Six libellés réutilisés à tour de rôle : aucune allocation par toucher. Mare
    /// pleine, il dit ce que le toucher a fait porter de plus (`+N à porter`, 2026-11-14) ;
    /// toucher sans effet, ou ignoré par la cadence : pas de chiffre.
    /// </summary>
    public sealed class CoucheDuToucher
    {
        public const int LIBELLES = 6;
        const long DUREE_MS = 700;

        readonly Label[] envols = new Label[LIBELLES];
        readonly IVisualElementScheduledItem[] fins = new IVisualElementScheduledItem[LIBELLES];
        int prochain;

        public CoucheDuToucher(VisualElement racine, Func<GainDuToucher> toucher)
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
                var texte = TexteDeLEnvol(toucher());
                if (texte != null) Envoler(texte, evenement.localPosition);
            });
        }

        /// <summary>
        /// Ce que dit l'envol d'un toucher, ou null. Mare pleine, ce qui compte est la place que
        /// le toucher a faite, pas le peu de mana qui l'a remplie : c'est elle qu'on dit. Sinon
        /// le mana. Un montant qui s'écrirait zéro (« 0.0 », acquis presque saturé) ne s'envole pas.
        /// </summary>
        public static string TexteDeLEnvol(GainDuToucher gain)
        {
            var montant = gain.Plein ? gain.APorter : gain.Mana;
            if (!montant.Gt(0)) return null;
            var ecrit = Format.Montant(montant);
            if (double.TryParse(ecrit, NumberStyles.Float, CultureInfo.InvariantCulture, out var lu) && lu == 0) return null;
            return Format.Remplir(gain.Plein ? E.GAIN_A_PORTER : E.GAIN_DU_TOUCHER, ecrit);
        }

        void Envoler(string texte, Vector2 point)
        {
            var i = prochain;
            prochain = (prochain + 1) % LIBELLES;
            var envol = envols[i];
            fins[i]?.Pause();
            envol.RemoveFromClassList("envol-parti");
            Elements.Poser(envol, texte);
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
