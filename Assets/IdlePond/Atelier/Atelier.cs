using System;
using System.IO;
using IdlePond.Jeu;
using IdlePond.Jeu.Scene;
using IdlePond.Jeu.UI;
using IdlePond.Noyau;
using UnityEngine;
using Decimal = IdlePond.Noyau.Decimal;

namespace IdlePond.Atelier
{
    /// <summary>
    /// IdlePond — l'atelier : un bac à sable posé sur la Mare (spec du 2026-10-07). Il installe
    /// sa propre partie, sur une horloge qu'il peut décaler, et redirige la sauvegarde vers un
    /// dossier temporaire — la partie du joueur n'est ni lue ni écrite. Un panneau IMGUI, par-
    /// dessus le jeu, en change l'état.
    ///
    /// Avant la `Boucle` (−1000) : la partie et la redirection doivent exister quand elle
    /// s'éveille, sans quoi elle ouvrirait la sauvegarde du joueur.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    [DisallowMultipleComponent]
    public sealed class Atelier : MonoBehaviour
    {
        public enum Section { Heros, Especes, Economie, Interface }
        public enum Depart { PartieNeuve, Noue, NoueDeuxEspeces, MiPartie }

        static readonly string[] NOMS_DES_SECTIONS = { "Héros", "Espèces", "Économie", "Interface" };
        static readonly int[] VITESSES = { 1, 10, 100 };
        static readonly string[] NOMS_DES_VITESSES = { "×1", "×10", "×100" };
        static readonly int[] NIVEAUX_DES_STADES = { 1, 4, 16, 256 };
        const double PERIODE_DE_TICK_S = Constantes.PERIODE_DE_TICK_MS / 1000.0;
        /// Un seul atelier par scène (DisallowMultipleComponent) : un identifiant fixe suffit.
        const int ID_DE_LA_FENETRE = 0x1D7E;

        [SerializeField] Section section = Section.Heros;
        [SerializeField] Depart depart = Depart.PartieNeuve;

        HorlogeDecalee horloge;
        Partie partie;
        string dossier;
        int vitesse;
        double cumul;
        Rect fenetre = new Rect(12, 12, 360, 0);
        bool replie;
        bool place;

        void Awake()
        {
            horloge = new HorlogeDecalee(new HorlogeSysteme());
            dossier = Path.Combine(Application.temporaryCachePath, "atelier");
            Directory.CreateDirectory(dossier);
            ServicesDePartie.RedirigerLaSauvegarde(dossier);
            partie = new Partie(horloge, EtatDeDepart());
            ServicesDePartie.Installer(partie);
        }

        EtatJeu EtatDeDepart()
        {
            var neuf = EtatsDEssai.Depart(horloge);
            switch (depart)
            {
                case Depart.Noue: return EtatsDEssai.Noue(neuf);
                case Depart.NoueDeuxEspeces:
                    var noue = EtatsDEssai.Noue(neuf);
                    noue = EtatsDEssai.AvecEspece(noue, EtatsDEssai.EspecesLivrees[0].Id, 40);
                    if (EtatsDEssai.EspecesLivrees.Count > 1) noue = EtatsDEssai.AvecEspece(noue, EtatsDEssai.EspecesLivrees[1].Id, 12);
                    return EtatsDEssai.AvecPartDeContenance(noue, 0.1);
                case Depart.MiPartie: return EtatsDEssai.MiPartie(horloge);
                default: return neuf;
            }
        }

        /// L'accélération : des pas FIXES en plus de ceux de la boucle, jamais un pas de la
        /// durée de l'image (§11).
        void Update()
        {
            // Après un rechargement de code en Play Mode, plus de partie : voir `OnGUI`.
            if (partie == null || VITESSES[vitesse] <= 1) { cumul = 0; return; }
            cumul += Math.Min(Time.unscaledDeltaTime, 1.0) * (VITESSES[vitesse] - 1);
            while (cumul >= PERIODE_DE_TICK_S)
            {
                partie.Avancer(PERIODE_DE_TICK_S);
                cumul -= PERIODE_DE_TICK_S;
            }
        }

        /* ─── Les actions ───────────────────────────────────────────────────────────*/

        void Poser(Func<EtatJeu, EtatJeu> changement) => partie.Remplacer(changement(partie.Etat));

        /// <summary>
        /// Du temps joué, d'un bloc, par pas fixes : sur le réducteur directement, puis un
        /// seul `Remplacer`. Passer par `Partie.Avancer` rafraîchirait toute l'interface à
        /// chaque pas (36 000 fois pour une heure). Les succès tombés en route sont dans
        /// l'état, mais ne sont pas annoncés.
        /// </summary>
        void AvancerEnJeu(double secondes)
        {
            var etat = partie.Etat;
            for (var pas = (long)Math.Round(secondes / PERIODE_DE_TICK_S); pas > 0; pas--)
                etat = Reducteur.Tick(etat, PERIODE_DE_TICK_S);
            partie.Remplacer(etat);
        }

        /// Une absence réelle : l'horloge saute, et `Reprendre` la crédite comme au retour du joueur.
        void Absence(double heures)
        {
            horloge.Decaler((long)(heures * 3_600_000));
            partie.Reprendre();
        }

        /* ─── Le panneau ────────────────────────────────────────────────────────────*/

        void OnGUI()
        {
            // Un rechargement de code en Play Mode vide les champs non sérialisés : plus de
            // partie à montrer. On se tait plutôt que de lever à chaque image.
            if (partie == null) return;
            // Lisible sur un grand écran comme dans la petite vue Game.
            var echelle = Mathf.Clamp(Screen.height / 900f, 1f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(echelle, echelle, 1f));
            // La première fois, sous la barre du haut et le nom du lieu, qu'on veut voir.
            if (!place)
            {
                fenetre.y = Screen.height * 0.25f / echelle;
                place = true;
            }
            fenetre.height = 0;
            fenetre = GUILayout.Window(ID_DE_LA_FENETRE, fenetre, Panneau, "Atelier IdlePond");
        }

        void Panneau(int _)
        {
            if (GUILayout.Button(replie ? "▸ Déplier" : "▾ Replier")) replie = !replie;
            if (!replie)
            {
                Bandeau();
                section = (Section)GUILayout.Toolbar((int)section, NOMS_DES_SECTIONS);
                GUILayout.Space(4);
                switch (section)
                {
                    case Section.Heros: SectionHeros(); break;
                    case Section.Especes: SectionEspeces(); break;
                    case Section.Economie: SectionEconomie(); break;
                    case Section.Interface: SectionInterface(); break;
                }
            }
            GUI.DragWindow();
        }

        void Bandeau()
        {
            var e = partie.Etat;
            var niveau = e.Cycle.NiveauDuHeros;
            GUILayout.Label($"Héros niv. {niveau} · stade {Gabarits.StadeDuNiveau(niveau)}");
            GUILayout.Label($"Mana {Format.Montant(e.Cycle.ManaCourant)} / {Format.Montant(Economie.Contenance(e))} ({Economie.PartDeContenance(e):P0})");
            GUILayout.Label($"Souffle {Format.Montant(e.Permanent.Souffle)} · paliers {e.Cycle.PaliersOuverts} · renaissances {e.Permanent.NombreDeRenaissances}");
            GUILayout.Label($"Vitesse {NOMS_DES_VITESSES[vitesse]} · sauvegarde : {dossier}");
        }

        void SectionHeros()
        {
            var niveau = partie.Etat.Cycle.NiveauDuHeros;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Niveau −")) Poser(e => EtatsDEssai.AvecNiveauDuHeros(e, niveau - 1));
            if (GUILayout.Button("Niveau +")) Poser(e => EtatsDEssai.AvecNiveauDuHeros(e, niveau + 1));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (var stade = 0; stade < NIVEAUX_DES_STADES.Length; stade++)
            {
                var cible = NIVEAUX_DES_STADES[stade];
                if (GUILayout.Button($"Stade {stade}")) Poser(e => EtatsDEssai.AvecNiveauDuHeros(e, cible));
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Mue : franchir le stade suivant")) Poser(e => EtatsDEssai.AvecNiveauDuHeros(e, EtatsDEssai.NiveauDuStadeSuivant(niveau)));
            if (GUILayout.Button("Grandir (l'acte du joueur, payé en mana)")) partie.Grandir();
        }

        void SectionEspeces()
        {
            foreach (var espece in EtatsDEssai.EspecesLivrees)
            {
                var id = espece.Id;
                var niveau = partie.Etat.Cycle.Especes.TryGetValue(id, out var s) && s.Debloquee ? s.Niveau : 0;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{id} : {(niveau > 0 ? "niv. " + niveau : "verrouillée")}", GUILayout.Width(170));
                if (GUILayout.Button("−")) Poser(e => EtatsDEssai.AvecEspece(e, id, niveau - 1));
                if (GUILayout.Button("+")) Poser(e => EtatsDEssai.AvecEspece(e, id, niveau + 1));
                if (GUILayout.Button("+10")) Poser(e => EtatsDEssai.AvecEspece(e, id, niveau + 10));
                GUILayout.EndHorizontal();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Toutes (niv. 10)")) Poser(e => EtatsDEssai.AvecToutesLesEspeces(e, 10));
            if (GUILayout.Button("Aucune")) Poser(e => EtatsDEssai.AvecToutesLesEspeces(e, 0));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Eau claire")) Poser(e => EtatsDEssai.AvecPartDeContenance(e, 0.1));
            if (GUILayout.Button("Eau troublée")) Poser(e => EtatsDEssai.AvecPartDeContenance(e, 0.95));
            GUILayout.EndHorizontal();
            var paliers = partie.Etat.Cycle.PaliersOuverts;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Paliers −")) Poser(e => EtatsDEssai.AvecPaliersOuverts(e, paliers - 1));
            if (GUILayout.Button("Paliers +")) Poser(e => EtatsDEssai.AvecPaliersOuverts(e, paliers + 1));
            if (GUILayout.Button("La Noue")) Poser(EtatsDEssai.Noue);
            GUILayout.EndHorizontal();
        }

        void SectionEconomie()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mana ×10")) Poser(e => EtatsDEssai.AvecMana(e, e.Cycle.ManaCourant.Max(new Decimal(1)).Mul(10)));
            if (GUILayout.Button("Contenance pleine")) Poser(e => EtatsDEssai.AvecPartDeContenance(e, 1.0));
            if (GUILayout.Button("Souffle +100")) Poser(e => EtatsDEssai.AvecSouffleEnPlus(e, 100));
            GUILayout.EndHorizontal();
            vitesse = GUILayout.Toolbar(vitesse, NOMS_DES_VITESSES);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 min en jeu")) AvancerEnJeu(60);
            if (GUILayout.Button("+1 h en jeu")) AvancerEnJeu(3600);
            if (GUILayout.Button("Absence 8 h")) Absence(8);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Creuser")) partie.Creuser();
            if (GUILayout.Button("Renaissance")) partie.Renaitre();
            if (GUILayout.Button("Repartir de zéro")) partie.Remplacer(EtatsDEssai.Depart(horloge));
            GUILayout.EndHorizontal();
        }

        void SectionInterface()
        {
            if (GUILayout.Button("Début de partie")) partie.Remplacer(EtatsDEssai.Depart(horloge));
            if (GUILayout.Button("Mi-partie")) partie.Remplacer(EtatsDEssai.MiPartie(horloge));
            if (GUILayout.Button("Contenance pleine")) partie.Remplacer(EtatsDEssai.ContenancePleine(horloge));
            if (GUILayout.Button("Après une renaissance")) partie.Remplacer(EtatsDEssai.ApresRenaissance(horloge));
            if (GUILayout.Button("Absence 8 h (écran de retour)")) Absence(8);
        }
    }
}
