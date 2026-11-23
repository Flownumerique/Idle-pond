"""Inspirations : les références de l'auteur, découpées proprement et passées en pixel art
détaillé, sans la contrainte des gabarits du jeu.

    python -I outils/da/inspirations.py docs/da/reference docs/da/inspirations

Pour chaque sujet : une découpe nette en haute définition (`hd/`), et un sprite en pixels
(`pixels/`) où le corps est opaque et la lumière douce (halo, anneau, aura) devient une trame —
jamais de demi-transparence. Écrit aussi `_planche.png`.
"""
import os
import sys
from PIL import Image, ImageFilter, ImageDraw, ImageFont

REF, SORTIE = sys.argv[1], sys.argv[2]
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# Chaque image : ses sujets (boîte dans l'image d'origine, nom) et l'échelle commune de la
# rangée — une seule échelle par image, pour garder les tailles relatives de l'auteur.
PLAN = {
    "da-1-cycle.png": (0.30, [
        ((90, 130, 390, 440), "cycle-0-oeuf"),
        ((500, 170, 910, 400), "cycle-1-larve"),
        ((1000, 180, 1450, 380), "cycle-2-alevin"),
        ((10, 590, 450, 900), "cycle-3-jeune"),
        ((455, 590, 940, 900), "cycle-4-adulte"),
        ((950, 560, 1530, 910), "cycle-5-grand"),
    ]),
    "da-2-croissance.png": (0.22, [
        ((40, 270, 320, 470), "croissance-0"),
        ((330, 220, 725, 510), "croissance-1"),
        ((730, 150, 1305, 570), "croissance-2"),
        ((1310, 70, 2160, 630), "croissance-3"),
    ]),
    "da-3-eveil.png": (0.20, [
        ((40, 0, 860, 395), "eveil-1-runes"),
        ((880, 0, 1774, 395), "eveil-2-voile"),
        ((0, 405, 860, 887), "eveil-3-etincelles"),
        ((860, 405, 1774, 887), "eveil-4-aura"),
    ]),
}

PLEIN = 200      # au-dessus : le corps
LUEUR = 24       # entre LUEUR et PLEIN : la lumière douce
FRANGE = 5       # en px HD autour du corps : la frange du détourage, à repeindre


def decouper(im):
    """Sépare le corps (opaque) de la lumière douce, et repeint la frange du détourage —
    ces pixels rouges et jaunes collés au bord — de la couleur de la lueur."""
    a = im.getchannel("A")
    corps = a.point(lambda v: 255 if v >= PLEIN else 0)
    # Le corps est rongé d'un pixel : son bord, c'est souvent déjà la frange.
    corps = corps.filter(ImageFilter.MinFilter(3))
    pres = corps.filter(ImageFilter.MaxFilter(2 * FRANGE + 1))
    px, pc, pp = im.load(), corps.load(), pres.load()
    sortie = Image.new("RGBA", im.size, (0, 0, 0, 0))
    so = sortie.load()
    lueurs = []
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, al = px[x, y]
            if pc[x, y]:
                so[x, y] = (r, g, b, 255)
            elif al >= LUEUR and not pp[x, y]:
                lueurs.append((r, g, b))
                so[x, y] = (r, g, b, al)
    # La couleur moyenne de la lueur de ce sujet : elle remplace la frange.
    # Une vraie lueur couvre une bonne part du corps ; quelques pixels, c'est du bruit.
    lueurs_larges = len(lueurs) > 0.15 * sum(1 for y in range(im.height) for x in range(im.width) if pc[x, y])
    if lueurs:
        n = len(lueurs)
        moyenne = tuple(sum(c[i] for c in lueurs) // n for i in range(3))
    else:
        moyenne = (255, 220, 150)
    for y in range(im.height):
        for x in range(im.width):
            al = px[x, y][3]
            if not pc[x, y] and pp[x, y] and al >= LUEUR and lueurs_larges:
                # Là où le sujet baigne dans une vraie lueur (l'œuf, l'aura), la frange
                # reprend la couleur de la lueur ; ailleurs, elle disparaît.
                so[x, y] = moyenne + (al // 2,)
    return sortie


def pixeliser(hd, largeur, couleurs=28):
    """La découpe HD réduite en pixels : moyenne prémultipliée, puis le corps opaque et la
    lueur tramée ; palette commune au sujet ; contour du corps seulement."""
    hauteur = max(1, round(hd.height * largeur / hd.width))
    petit = hd.convert("RGBa").resize((largeur, hauteur), Image.BOX).convert("RGBA")
    px = petit.load()
    corps = set()
    for y in range(hauteur):
        for x in range(largeur):
            r, g, b, a = px[x, y]
            if a >= 150:
                px[x, y] = (r, g, b, 255); corps.add((x, y))
            elif a >= 18 and a / 150 * 0.85 > (BAYER[y & 3][x & 3] + .5) / 16:
                # La lumière douce, en trame : éclaircie, pour qu'elle se lise comme lumière.
                px[x, y] = (min(255, r + 30), min(255, g + 30), min(255, b + 20), 255)
            else:
                px[x, y] = (0, 0, 0, 0)
    # Un point de trame isolé n'est pas de la lumière, c'est un reste : il part.
    seuls = [(x, y) for y in range(hauteur) for x in range(largeur)
             if px[x, y][3] and (x, y) not in corps
             and sum(1 for dx in range(-2, 3) for dy in range(-2, 3)
                     if (dx or dy) and 0 <= x + dx < largeur and 0 <= y + dy < hauteur and px[x + dx, y + dy][3]) < 2]
    for x, y in seuls: px[x, y] = (0, 0, 0, 0)
    # Une palette par sujet, tirée de ses propres couleurs.
    pleins = [px[x, y][:3] for y in range(hauteur) for x in range(largeur) if px[x, y][3]]
    if pleins:
        ech = Image.new("RGB", (len(pleins), 1)); ech.putdata(pleins)
        pal = ech.quantize(colors=couleurs, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
        p = pal.getpalette()[:3 * couleurs]
        teintes = [tuple(p[i:i + 3]) for i in range(0, len(p), 3)]
        for y in range(hauteur):
            for x in range(largeur):
                if px[x, y][3]:
                    c = px[x, y]
                    px[x, y] = min(teintes, key=lambda t: (t[0] - c[0]) ** 2 * 3 + (t[1] - c[1]) ** 2 * 4 + (t[2] - c[2]) ** 2 * 2) + (255,)
    # Le contour : un brun sombre, sur le bord du corps qui donne sur le vide ou la lueur.
    bord = [(x, y) for (x, y) in corps
            if any((x + dx, y + dy) not in corps for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in bord:
        r, g, b, _ = px[x, y]
        if r + g + b > 200:  # un bord déjà sombre reste tel quel
            px[x, y] = (74, 30, 12, 255)
    return petit


def planche(sujets, chemin, echelle=3):
    fonte = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 16)
    rangs = {}
    for image, nom, s in sujets: rangs.setdefault(image, []).append((nom, s))
    marge = 24
    largeur = max(sum(s.width * echelle + marge for _, s in r) for r in rangs.values()) + marge
    hauteur = sum(max(s.height for _, s in r) * echelle + 70 for r in rangs.values()) + marge
    fond = Image.new("RGB", (largeur, hauteur), (16, 30, 52))
    # Un fond de nuit, en deux tons, pour que la lueur tramée se lise.
    d = ImageDraw.Draw(fond)
    for y in range(0, hauteur, 2): d.line([(0, y), (largeur, y)], fill=(18, 34, 58))
    y = marge // 2
    for image, r in rangs.items():
        d.text((marge, y), image, fill=(235, 225, 200), font=fonte)
        x = marge
        h = max(s.height for _, s in r) * echelle
        for nom, s in r:
            g = s.resize((s.width * echelle, s.height * echelle), Image.NEAREST)
            fond.paste(g, (x, y + 26 + (h - g.height) // 2), g)
            d.text((x, y + 30 + h), f"{nom} · {s.width}×{s.height}", fill=(200, 210, 225), font=fonte)
            x += g.width + marge
        y += h + 70
    fond.save(chemin)
    return fond.size


def main():
    os.makedirs(os.path.join(SORTIE, "hd"), exist_ok=True)
    os.makedirs(os.path.join(SORTIE, "pixels"), exist_ok=True)
    sujets = []
    for fichier, (echelle, boites) in PLAN.items():
        im = Image.open(os.path.join(REF, fichier)).convert("RGBA")
        for boite, nom in boites:
            hd = decouper(im.crop(boite))
            hd = hd.crop(hd.getbbox())
            hd.save(os.path.join(SORTIE, "hd", nom + ".png"))
            s = pixeliser(hd, max(24, round(hd.width * echelle)))
            s.save(os.path.join(SORTIE, "pixels", nom + ".png"))
            sujets.append((fichier, nom, s))
    print("planche", planche(sujets, os.path.join(SORTIE, "_planche.png")))


main()
