"""La faune d'ambiance (Codex v1.3, §2) : ce qui vit dans la mare sans produire.

    python -I outils/da/faune.py            # écrit Assets/IdlePond/Art/Faune/{nom}-i{n}.png
    python -I outils/da/faune.py --voir D   # et une planche de contrôle dans D

Dessinée au pixel, à l'échelle de la scène de 360 px. Chaque bête est une grille de caractères ;
chaque caractère, une couleur. Le script imprime la palette, à reporter dans
RegistreDArt.FAUNE_DESSINEE. Les méduses du Gour, elles, sont dans le décor (l'ambiance).
"""
import json
import os
import sys
from PIL import Image

RACINE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(RACINE, "Assets", "IdlePond", "Art", "Faune")

COULEURS = {
    "k": "#121C1A",  # contour
    # la libellule-lanterne : corps turquoise, ailes de verre, lanterne d'or
    "t": "#2FB8B0", "T": "#7FE6E8", "w": "#D8FFF0", "W": "#A8F0D0", "o": "#FFE070", "O": "#FFF0A0",
    # le gerris-scribe
    "g": "#4A3424", "G": "#6A4C30",
    # la grenouille-chantre
    "v": "#3FA060", "V": "#7CD070", "j": "#F6C840", "J": "#FFF0C0", "e": "#FFE070",
    # l'écrevisse porte-bannière, et sa bannière de Sève
    "r": "#B8452A", "R": "#E07040", "b": "#8FE070", "B": "#FFE070", "h": "#6A4C30",
    # l'hydre, pâle, aux bras qui pendent
    "y": "#B8E8C8", "Y": "#E6F8EE", "p": "#F4A8C8",
    # la fleur des jardins de la Sève
    "f": "#F4A8C8", "F": "#FFD0E4", "c": "#FFE070", "s": "#3FA060", "S": "#5CC070",
}

# Les dessins : regard à droite, une rangée par ligne, du haut vers le bas ; « . » est vide.
DESSINS = {
    "libellule": [
        [
            "..W.W.........",
            ".WwWwW........",
            "..WwW.kk......",
            "ttttttttTkk...",
            "..WwW.kk......",
            ".WwWwW........",
            "..W.W.........",
        ],
        [
            "..............",
            "..W.W.........",
            ".WwWwW.kk.....",
            "ttttttttTkk...",
            ".WwWwW.kk.....",
            "..W.W.........",
            "..............",
        ],
        # La lanterne : les ailes s'allument, en éclair.
        [
            "..O.O.........",
            ".OoOoO........",
            "..OoO.kk......",
            "ttttttttTkk...",
            "..OoO.kk......",
            ".OoOoO........",
            "..O.O.........",
        ],
    ],
    "gerris": [
        [
            "g.......g..",
            ".g..G..g...",
            "..GGGGGGk..",
            ".g..g..g...",
            "g...g...g..",
        ],
        [
            ".g.....g...",
            "..g.G.g....",
            "..GGGGGGk..",
            "..g.g.g....",
            ".g..g..g...",
        ],
    ],
    "grenouille": [
        [
            "...kk...kk....",
            "..kek..kek....",
            ".kVVVkkVVVk...",
            "kVVvVVVVvVVk..",
            "kvvvvvvvvvvvk.",
            ".kvvjjjjvvvk..",
            "..kkkkkkkkk...",
        ],
        # Le chant : la gorge se gonfle.
        [
            "...kk...kk....",
            "..kek..kek....",
            ".kVVVkkVVVk...",
            "kVVvVVVVvVVk..",
            "kvvvvvvvvvvvk.",
            ".kvvJJJJJvvk..",
            "..kkJJJJJkk...",
            "....kkkkk.....",
        ],
    ],
    "ecrevisse": [
        [
            "........hhh...",
            "..rrrrrrr.kRk.",
            ".rRRRRRRRrRRk.",
            "rrrrrrrrrrrRk.",
            ".r.r.r.r..kRk.",
            "r.r.r.r.......",
        ],
        [
            "........hhh...",
            "..rrrrrrr.kRk.",
            ".rRRRRRRRrRRk.",
            "rrrrrrrrrrrRk.",
            "r.r.r.r...kRk.",
            ".r.r.r.r......",
        ],
    ],
    "ecrevisse-banniere": [
        [
            "........bbbBb.",
            "........bBbbb.",
            "........bbbBb.",
            "........h.....",
            "........h.....",
            "........hhh...",
            "..rrrrrrr.kRk.",
            ".rRRRRRRRrRRk.",
            "rrrrrrrrrrrRk.",
            ".r.r.r.r..kRk.",
            "r.r.r.r.......",
        ],
        [
            "........bbBbb.",
            "........bbbbB.",
            "........bBbbb.",
            "........h.....",
            "........h.....",
            "........hhh...",
            "..rrrrrrr.kRk.",
            ".rRRRRRRRrRRk.",
            "rrrrrrrrrrrRk.",
            "r.r.r.r...kRk.",
            ".r.r.r.r......",
        ],
    ],
    "hydre": [
        [
            "..yy...",
            "..yY...",
            "..yY...",
            "..yY...",
            "..yY...",
            ".yyYy..",
            ".ypYy..",
            "y.yy.y.",
            "y.y.y.y",
            ".y..y..",
            ".y...y.",
            "y.....y",
        ],
        [
            "..yy...",
            "..yY...",
            "..yY...",
            "..yY...",
            "..yY...",
            ".yyYy..",
            ".ypYy..",
            ".yyy.y.",
            "y.y.y.y",
            "y..y..y",
            ".y..y..",
            ".y...y.",
        ],
    ],
    # Les jardins de la Sève, à chaque renaissance : le bourgeon, la fleur, la grande fleur.
    "fleur": [
        [
            "...",
            ".f.",
            ".S.",
            ".s.",
            ".s.",
        ],
        [
            ".f.f.",
            "fFcFf",
            ".fFf.",
            "..S..",
            ".sS..",
            "..s..",
            "..s..",
        ],
        [
            "..f.f..",
            ".fFfFf.",
            "fFFcFFf",
            ".fFFFf.",
            "..fSf..",
            "...S...",
            ".sSS...",
            "...Ss..",
            "...s...",
        ],
    ],
}


def rgba(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


def image(grille, largeur, hauteur):
    """Posée en bas à gauche d'un cadre commun à toutes les images de la bête : les
    images d'une même bête ont la même taille."""
    im = Image.new("RGBA", (largeur, hauteur), (0, 0, 0, 0))
    haut = hauteur - len(grille)
    for y, ligne in enumerate(grille):
        for x, c in enumerate(ligne):
            if c != ".": im.putpixel((x, haut + y), rgba(COULEURS[c]))
    return im


def main():
    voir = sys.argv[sys.argv.index("--voir") + 1] if "--voir" in sys.argv else None
    os.makedirs(ART, exist_ok=True)
    cadres, palette = {}, set()
    for nom, grilles in DESSINS.items():
        largeur = max(len(l) for g in grilles for l in g)
        hauteur = max(len(g) for g in grilles)
        cadres[nom] = [largeur, hauteur, len(grilles)]
        for n, g in enumerate(grilles):
            for ligne in g:
                for c in ligne:
                    if c != ".": palette.add("0x" + COULEURS[c][1:].upper())
            image(g, largeur, hauteur).save(os.path.join(ART, f"{nom}-i{n}.png"))
    print(json.dumps({"cadres": cadres, "palette": sorted(palette)}))
    if voir:
        os.makedirs(voir, exist_ok=True)
        E = 10
        ims = [(n, Image.open(os.path.join(ART, f"{n}-i{i}.png"))) for n, (_, _, k) in cadres.items() for i in range(k)]
        out = Image.new("RGB", (sum(im.width * E + 16 for _, im in ims) + 16, max(im.height for _, im in ims) * E + 32), (47, 154, 128))
        x = 16
        for _, im in ims:
            g = im.resize((im.width * E, im.height * E), Image.NEAREST); out.paste(g, (x, 16), g); x += g.width + 16
        out.save(os.path.join(voir, "faune.png"))


main()
