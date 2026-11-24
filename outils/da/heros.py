"""Le héros du prototype, tiré des inspirations (docs/da/inspirations/hd/croissance-*.png).

    python -I outils/da/heros.py            # écrit les sprites dans Assets/IdlePond/Art/Heros/
    python -I outils/da/heros.py --voir D   # écrit aussi des planches de contrôle dans D

Pour chaque stade : le corps en quatre images (seule la queue bat, d'un pixel), une palette
commune à tous les stades (à reporter dans RegistreDArt.HEROS_DESSINE), et les ancrages.
Les fichiers écrits sont de « vrais dessins » : le générateur de provisoires ne les touche plus.
"""
import json
import os
import sys
from PIL import Image, ImageFilter

RACINE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
HD = os.path.join(RACINE, "docs", "da", "inspirations", "hd")
ART = os.path.join(RACINE, "Assets", "IdlePond", "Art", "Heros")
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]

# La largeur de chaque stade : celle des cadres (Gabarits.CadreDuCorps), à l'échelle de la scène
# de 360 px (2026-11-24) — une fois et demie celle de 240 (23, 30, 44, 65).
LARGEURS = [34, 44, 65, 97]
COULEURS = 16
CONTOUR = (74, 30, 12)
# Le côté de l'œil, par stade : environ un pixel par 22 de longueur.
COTES_D_OEIL = [2, 2, 3, 4]


def reduire(hd, largeur):
    # Le grain des écailles, à cette taille, n'est plus que du bruit : on l'adoucit avant de
    # réduire, d'autant plus que le sprite est petit.
    rayon = max(3, round(hd.width / largeur / 2) * 2 + 1)
    couleur = hd.convert("RGB").filter(ImageFilter.MedianFilter(min(rayon, 9)))
    hd = Image.merge("RGBA", (*couleur.split(), hd.getchannel("A")))
    hauteur = max(1, round(hd.height * largeur / hd.width))
    petit = hd.convert("RGBa").resize((largeur, hauteur), Image.BOX).convert("RGBA")
    px = petit.load()
    for y in range(hauteur):
        for x in range(largeur):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= 140 else (0, 0, 0, 0)
    return petit


def palette_commune(images):
    pleins = [p[:3] for im in images for p in im.getdata() if p[3]]
    ech = Image.new("RGB", (len(pleins), 1)); ech.putdata(pleins)
    pal = ech.quantize(colors=COULEURS, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).getpalette()
    teintes = [tuple(pal[i:i + 3]) for i in range(0, 3 * COULEURS, 3)]
    return sorted(set(teintes) | {CONTOUR}, key=lambda c: sum(c))


def caler(im, teintes):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            c = px[x, y]
            if c[3]:
                px[x, y] = min(teintes, key=lambda t: (t[0] - c[0]) ** 2 * 3 + (t[1] - c[1]) ** 2 * 4 + (t[2] - c[2]) ** 2 * 2) + (255,)


def lisser(im):
    """Un pixel seul de sa couleur parmi ses huit voisins prend la couleur dominante autour."""
    src = im.copy().load(); px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            p = src[x, y]
            if not p[3]: continue
            voisins = [src[x + dx, y + dy] for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                       if (dx or dy) and 0 <= x + dx < im.width and 0 <= y + dy < im.height and src[x + dx, y + dy][3]]
            if not voisins or sum(1 for v in voisins if v == p) >= 2: continue
            if sum(p[:3]) < 150: continue  # l'œil et les traits sombres restent
            compte = {}
            for v in voisins: compte[v] = compte.get(v, 0) + 1
            px[x, y] = max(compte, key=compte.get)


def contourner(im, couleur=CONTOUR):
    px = im.load()
    bord = [(x, y) for y in range(im.height) for x in range(im.width) if px[x, y][3] and any(
        not (0 <= x + dx < im.width and 0 <= y + dy < im.height) or px[x + dx, y + dy][3] == 0
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    for x, y in bord:
        if sum(px[x, y][:3]) > 200: px[x, y] = couleur + (255,)


def pedoncule(im):
    """La colonne la plus étroite de la moitié arrière : là où la queue commence."""
    px = im.load()
    def epaisseur(x): return sum(1 for y in range(im.height) if px[x, y][3])
    debut, fin = round(im.width * 0.15), round(im.width * 0.55)
    return min(range(debut, fin), key=lambda x: (epaisseur(x), -x))


def battre(im, queue, decalage):
    """La queue (tout ce qui est à gauche du pédoncule) monte ou descend d'un pixel,
    progressivement : rien au pédoncule, tout au bout."""
    if decalage == 0: return im.copy()
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    src, dst = im.load(), out.load()
    for y in range(im.height):
        for x in range(im.width):
            if x >= queue: dst[x, y] = src[x, y]
    for x in range(queue):
        d = decalage if x < queue * 0.55 else 0
        for y in range(im.height):
            sy = y + d  # origine en haut : +1 = la queue monte
            if 0 <= sy < im.height and src[x, sy][3]: dst[x, y] = src[x, sy]
    return out


def tronc_hd(hd):
    """Le tronc dans l'image HD : le masque du sujet, ouvert par un disque assez large pour
    effacer les nageoires (plus fines que le corps) et ne garder que le corps. Rendu comme un
    masque à 1/4 de la taille."""
    a = hd.getchannel("A").point(lambda v: 255 if v > 128 else 0)
    petit = a.resize((hd.width // 4, hd.height // 4), Image.NEAREST)
    k = max(3, round(petit.height * 0.36) | 1)
    ouvert = petit.filter(ImageFilter.MinFilter(k)).filter(ImageFilter.MaxFilter(k))
    # On ne garde que la plus grande tache : le corps (la queue, elle, est coupée au pédoncule).
    px = ouvert.load(); w, h = ouvert.size; vu = set(); meilleure = set()
    for y in range(h):
        for x in range(w):
            if px[x, y] and (x, y) not in vu:
                tache, pile = set(), [(x, y)]; vu.add((x, y))
                while pile:
                    cx, cy = pile.pop(); tache.add((cx, cy))
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        n = (cx + dx, cy + dy)
                        if 0 <= n[0] < w and 0 <= n[1] < h and px[n] and n not in vu: vu.add(n); pile.append(n)
                if len(tache) > len(meilleure): meilleure = tache
    m = Image.new("L", (w, h), 0); pm = m.load()
    for c in meilleure: pm[c] = 255
    return m


def oeil_hd(hd, tronc):
    """L'œil : dans l'avant du tronc, le plus grand amas de pixels très sombres dont tout le
    voisinage est sombre aussi (le cœur de la pupille, pas un trait). Fractions (origine en haut)."""
    px = hd.load(); w, h = hd.size; pt = tronc.load()
    def noir(x, y): return 0 <= x < w and 0 <= y < h and px[x, y][3] > 200 and sum(px[x, y][:3]) < 120
    coeur = [(x, y) for y in range(0, h, 2) for x in range(round(w * 0.55), w, 2)
             if pt[min(tronc.width - 1, x // 4), min(tronc.height - 1, y // 4)]
             and all(noir(x + dx, y + dy) for dx in (-4, 0, 4) for dy in (-4, 0, 4))]
    if not coeur: return 0.85, 0.45
    # l'amas le plus peuplé, autour de son centre
    cx = sorted(c[0] for c in coeur)[len(coeur) // 2]; cy = sorted(c[1] for c in coeur)[len(coeur) // 2]
    proches = [c for c in coeur if abs(c[0] - cx) < w * 0.06 and abs(c[1] - cy) < h * 0.08]
    return sum(c[0] for c in proches) / len(proches) / w, sum(c[1] for c in proches) / len(proches) / h


def poser_oeil(im, fx, fy, stade):
    """L'œil redessiné : sombre, avec un éclat dès qu'il fait deux pixels."""
    px = im.load()
    x, y = round(fx * im.width - 0.5), round(fy * im.height - 0.5)
    cote = COTES_D_OEIL[stade]
    for dx in range(cote):
        for dy in range(cote):
            if 0 <= x + dx < im.width and 0 <= y + dy < im.height and px[x + dx, y + dy][3]:
                px[x + dx, y + dy] = (42, 20, 8, 255)
    if cote >= 2: px[x, y] = (255, 251, 234, 255)
    return x + cote // 2, y + cote // 2


def ancrages(im, queue, ox, oy):
    """Les six ancrages, origine en bas à gauche, chacun sur un pixel opaque hors contour et
    hors de la queue (qui bat) : branchies derrière l'œil, tête, dos, ventre, flanc, queue."""
    px = im.load(); h = im.height
    def plein(x, y): return 0 <= x < im.width and 0 <= y < h and px[x, y][3] and px[x, y][:3] != CONTOUR
    def haut(x): return min((y for y in range(h) if plein(x, y)), default=oy)
    def bas(x): return max((y for y in range(h) if plein(x, y)), default=oy)
    milieu = (queue + ox) // 2
    # Le corps, à la colonne du milieu : on vise sous le dos et au-dessus du ventre, pas dans
    # les nageoires — la bande qui contient l'œil, élargie.
    def proche(x, y):
        for r in range(0, 6):
            for dx, dy in ((0, 0), (0, r), (0, -r), (r, 0), (-r, 0)):
                if plein(x + dx, y + dy): return [x + dx, h - 1 - (y + dy)]
        return [x, h - 1 - y]
    demi = max(2, round((bas(ox) - haut(ox)) * 0.35))
    return {
        "Branchies": proche(ox - max(2, round(im.width * 0.1)), oy),
        "Tete": proche(ox, oy + 1),
        "Dos": proche(milieu, oy - demi),
        "Ventre": proche(milieu, oy + demi),
        "Flanc": proche(milieu, oy),
        "Queue": proche(queue + 2, oy),
    }


NOUE, GOUR = (143, 224, 112, 255), (143, 216, 255, 255)


def marques(im, stade, queue, ox, oy, tronc):
    """Les marques des lieux, sur ce corps : les branchies en vert de Sève derrière l'œil ;
    les membranes en bleu de Courant, une colonne sur trois des nageoires (hors queue, qui bat)."""
    px = im.load()
    pt = tronc.resize(im.size, Image.BOX).load()
    def dans(x, y): return pt[x, y] >= 128
    noue = Image.new("RGBA", im.size, (0, 0, 0, 0)); gour = Image.new("RGBA", im.size, (0, 0, 0, 0))
    pn, pg = noue.load(), gour.load()
    demi = max(1, round(0.13 * im.height))
    for k in range(min(3, stade + 1)):
        x = ox - 2 - COTES_D_OEIL[stade] - 2 * k
        for y in range(oy - demi, oy + demi + 1):
            if 0 <= x < im.width and 0 <= y < im.height and px[x, y][3] and px[x, y][:3] != CONTOUR and dans(x, y):
                pn[x, y] = NOUE
    def membranes(pas, debut):
        n = 0
        for y in range(im.height):
            for x in range(debut, ox - 1):
                if px[x, y][3] and px[x, y][:3] != CONTOUR and not dans(x, y) and x % pas == 1:
                    pg[x, y] = GOUR; n += 1
        return n
    # Une colonne sur trois des nageoires ; sur un petit corps, une sur deux, plus près de la
    # queue ; sinon, le haut du dos, une colonne sur deux.
    if not membranes(3, queue + 4) and not membranes(2, queue + 2):
        for x in range(queue + 2, ox - 1, 2):
            haut = min((y for y in range(im.height) if px[x, y][3] and px[x, y][:3] != CONTOUR), default=None)
            if haut is not None: pg[x, haut] = GOUR
    return noue, gour


# L'éveil (bible v0.4, d'après da-3) : des calques sur une toile plus grande que le corps,
# d'une marge MARGES[stade] de chaque côté. Couleurs : à reporter dans RegistreDArt.EVEIL.
MARGES = [12, 15, 21, 27]
TURQUOISE, TURQUOISE_CLAIR = (30, 168, 184, 255), (127, 230, 232, 255)
CREME_PALE, ETOILE, OR_CLAIR, ECLAT = (255, 244, 208, 255), (255, 224, 112, 255), (248, 208, 112, 255), (255, 251, 234, 255)
EVEIL = [TURQUOISE, TURQUOISE_CLAIR, CREME_PALE, ETOILE, OR_CLAIR, ECLAT]


def toile_d_eveil(im, s):
    m = MARGES[s]
    return Image.new("RGBA", (im.width + 2 * m, im.height + 2 * m), (0, 0, 0, 0)), m


def runes(im, s, queue, ox, oy, tronc):
    """I · les runes : une spirale turquoise sur le flanc, et une ligne de points le long du
    dos — sur le corps seulement, les nageoires sont aux membranes du Gour. Même cadre que le corps."""
    px = im.load(); pt = tronc.resize(im.size, Image.BOX).load()
    out = Image.new("RGBA", im.size, (0, 0, 0, 0)); po = out.load()
    def corps(x, y): return 0 <= x < im.width and 0 <= y < im.height and px[x, y][3] and px[x, y][:3] != CONTOUR
    # la spirale, au milieu du flanc
    cx, cy = (queue + ox) // 2 + 1, oy + max(1, round(0.08 * im.height))
    motif = {0: [(0, 0), (1, 0)], 1: [(0, 0), (1, 0), (1, 1), (0, 1)],
             2: [(0, 0), (1, 0), (2, 0), (2, 1), (2, 2), (1, 2), (0, 2), (0, 1), (1, 1)],
             3: [(0, 0), (1, 0), (2, 0), (3, 0), (3, 1), (3, 2), (3, 3), (2, 3), (1, 3), (0, 3), (0, 2), (1, 1), (2, 1)]}[s]
    for i, (dx, dy) in enumerate(motif):
        if corps(cx + dx, cy + dy): po[cx + dx, cy + dy] = TURQUOISE_CLAIR if i == len(motif) - 1 else TURQUOISE
    # Le long du dos, sous le contour et dans le tronc : un point sur deux. Les nageoires
    # restent aux membranes du Gour.
    for x in range(queue + 3, ox - 1, 2):
        haut = min((y for y in range(im.height) if corps(x, y) and pt[x, y] >= 128), default=None)
        if haut is not None and haut + 1 < im.height and corps(x, haut + 1): po[x, haut + 1] = TURQUOISE
    return out


def voile(im, s, image):
    """II · le voile : trois rubans qui sortent de la queue et ondulent ; crème dehors,
    turquoise au milieu ; deux images, la phase décalée."""
    import math
    t, m = toile_d_eveil(im, s); pt = t.load(); px = im.load()
    gauche = min(x for x in range(im.width) for y in range(im.height) if px[x, y][3])
    colonne = [y for y in range(im.height) if px[gauche + 1, y][3]] or [im.height // 2]
    haut, bas = min(colonne), max(colonne)
    longueur = m + max(4, round(im.width * 0.25))
    for r, (y0, couleur) in enumerate([(haut + 1, CREME_PALE), ((haut + bas) // 2, TURQUOISE_CLAIR), (bas - 1, CREME_PALE)]):
        for i in range(longueur):
            x = m + gauche + 2 - i
            y = m + y0 + round(math.sin(i * 0.45 + r * 1.9 + image * math.pi) * (1 + i / longueur * 2))
            if i > longueur * 0.55 and (i + r) % 2: continue  # le bout s'effiloche
            if 0 <= x < t.width and 0 <= y < t.height and not (m <= x < m + im.width and m <= y < m + im.height and px[x - m, y - m][3]):
                pt[x, y] = couleur if i < longueur * 0.7 else (TURQUOISE if r == 1 else CREME_PALE)
                if r == 1 and i < longueur * 0.4 and 0 <= y + 1 < t.height: pt[x, y + 1] = TURQUOISE
    return t


def anneau(im, s):
    """III · l'anneau : une ellipse pointillée d'or autour de lui (les étoiles y tournent en jeu)."""
    import math
    t, m = toile_d_eveil(im, s); pt = t.load()
    cx, cy = t.width / 2 - 0.5, t.height / 2 - 0.5
    rx, ry = t.width / 2 - 2, t.height / 2 - 2
    pas = max(60, round(2 * math.pi * max(rx, ry)))
    for k in range(pas):
        if k % 3: continue
        a = k / pas * 2 * math.pi
        pt[round(cx + rx * math.cos(a)), round(cy + ry * math.sin(a))] = ETOILE if k % 2 else OR_CLAIR
    return t


def aura(im, s):
    """IV · l'aura : un ovale de lumière d'or — clairsemé près de lui, dense vers son bord,
    comme un halo qui l'enveloppe."""
    t, m = toile_d_eveil(im, s); pt = t.load()
    cx, cy = t.width / 2 - 0.5, t.height / 2 - 0.5
    rx, ry = t.width / 2 - 1, t.height / 2 - 1
    for y in range(t.height):
        for x in range(t.width):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d > 1: continue
            densite = 0.03 + 0.5 * max(0.0, d - 0.5) / 0.5
            if densite > (BAYER[y & 3][x & 3] + .5) / 16:
                pt[x, y] = ETOILE if (x + y) % 3 else OR_CLAIR
    return t


def etoile():
    """Une étoile à quatre branches, 7 × 7 : les branches, et le cœur d'éclat qui déborde d'un
    pixel en croix."""
    t = Image.new("RGBA", (7, 7), (0, 0, 0, 0)); pt = t.load()
    for i in range(7): pt[3, i] = ETOILE; pt[i, 3] = ETOILE
    for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)): pt[3 + dx, 3 + dy] = ECLAT
    return t


# Le renouveau (bible v0.4, d'après da-1 ; Codex, acte III : il rentre dans son œuf, et le même
# en ressort) : l'œuf et la larve, joués à chaque renaissance. Pas
# des stades : une courte animation. Mêmes couleurs que le héros (et le crème de l'éveil).
LARGEUR_DE_L_OEUF, LARGEUR_DE_LA_LARVE = 25, 37
# Translucides : un contour doux (un brun du corps), et tout ce qui n'est pas la réserve d'or
# pâli vers le blanc.
CONTOUR_DOUX = (180, 109, 56)


def palir(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and not (r - b > 150 and g < 190):
                px[x, y] = (round(r * .55 + 255 * .45), round(g * .55 + 255 * .45), round(b * .55 + 255 * .45), a)


def renouveau(teintes):
    pal = teintes + [CREME_PALE[:3]]
    oeuf = reduire(Image.open(os.path.join(HD, "cycle-0-oeuf.png")).convert("RGBA"), LARGEUR_DE_L_OEUF)
    larve = reduire(Image.open(os.path.join(HD, "cycle-1-larve.png")).convert("RGBA"), LARGEUR_DE_LA_LARVE)
    for im in (oeuf, larve): palir(im); caler(im, pal); lisser(im); contourner(im, CONTOUR_DOUX)
    # L'œil de l'embryon, enroulé dans l'œuf ; celui de la larve, grand, à l'avant.
    poser_oeil(oeuf, 0.64, 0.40, 1)
    poser_oeil(larve, 0.84, 0.32, 1)
    # L'œuf fendu : une fêlure en zigzag sur le haut de la coquille.
    fendu = oeuf.copy(); pf = fendu.load()
    y0 = round(oeuf.height * 0.22)
    for x in range(3, oeuf.width - 3):
        y = y0 + (0 if (x // 2) % 2 else 1)
        if pf[x, y][3]: pf[x, y] = CONTOUR + (255,)
    for x, y in ((oeuf.width // 2, y0 - 1), (oeuf.width // 2, y0 - 2), (oeuf.width // 2 - 3, y0 + 2)):
        if pf[x, y][3]: pf[x, y] = CONTOUR + (255,)
    queue = round(larve.width * 0.45)
    return oeuf, fendu, [battre(larve, queue, d) for d in (1, -1)]


def main():
    voir = sys.argv[sys.argv.index("--voir") + 1] if "--voir" in sys.argv else None
    hds = [Image.open(os.path.join(HD, f"croissance-{s}.png")).convert("RGBA") for s in range(len(LARGEURS))]
    troncs = [tronc_hd(hd) for hd in hds]
    yeux = [oeil_hd(hd, t) for hd, t in zip(hds, troncs)]
    corps = [reduire(hd, w) for hd, w in zip(hds, LARGEURS)]
    teintes = palette_commune(corps)
    for im in corps: caler(im, teintes); lisser(im); contourner(im)
    teintes = teintes + [(42, 20, 8), (255, 251, 234)]
    infos = []
    os.makedirs(ART, exist_ok=True)
    os.makedirs(os.path.join(ART, "marques"), exist_ok=True)
    for s, im in enumerate(corps):
        queue = pedoncule(im)
        ox, oy = poser_oeil(im, *yeux[s], s)
        noue, gour = marques(im, s, queue, ox, oy, troncs[s])
        os.makedirs(os.path.join(ART, "eveil"), exist_ok=True)
        runes(im, s, queue, ox, oy, troncs[s]).save(os.path.join(ART, "eveil", f"runes-s{s}.png"))
        for i in range(2): voile(im, s, i).save(os.path.join(ART, "eveil", f"voile-s{s}-i{i}.png"))
        anneau(im, s).save(os.path.join(ART, "eveil", f"anneau-s{s}.png"))
        aura(im, s).save(os.path.join(ART, "eveil", f"aura-s{s}.png"))
        noue.save(os.path.join(ART, "marques", f"noue-s{s}.png"))
        gour.save(os.path.join(ART, "marques", f"gour-s{s}.png"))
        for i, d in enumerate((0, 1, 0, -1)):
            battre(im, queue, d).save(os.path.join(ART, f"corps-s{s}-i{i}.png"))
        infos.append({"stade": s, "cadre": [im.width, im.height], "pedoncule": queue,
                      "oeil": [ox, im.height - 1 - oy], "ancrages": ancrages(im, queue, ox, oy)})
    etoile().save(os.path.join(ART, "eveil", "etoile.png"))
    os.makedirs(os.path.join(ART, "renouveau"), exist_ok=True)
    oeuf, fendu, larves = renouveau(teintes)
    oeuf.save(os.path.join(ART, "renouveau", "oeuf.png"))
    fendu.save(os.path.join(ART, "renouveau", "oeuf-fendu.png"))
    for i, l in enumerate(larves): l.save(os.path.join(ART, "renouveau", f"larve-i{i}.png"))
    infos.append({"renouveau": {"oeuf": list(oeuf.size), "larve": list(larves[0].size)}})
    hexa = ["0x%02X%02X%02X" % t for t in teintes]
    infos.append({"eveil": ["0x%02X%02X%02X" % c[:3] for c in EVEIL], "marges": MARGES})
    print(json.dumps({"palette": hexa, "stades": infos}, indent=1))
    if voir:
        os.makedirs(voir, exist_ok=True)
        E = 8
        W = sum(im.width * E + 16 for im in corps) + 16
        H = max(im.height for im in corps) * E + 32
        out = Image.new("RGB", (W, H), (30, 70, 120)); x = 16
        for im in corps:
            g = im.resize((im.width * E, im.height * E), Image.NEAREST); out.paste(g, (x, 16), g); x += g.width + 16
        out.save(os.path.join(voir, "heros-corps.png"))
        ims = [oeuf, fendu] + larves
        out = Image.new("RGB", (sum(i.width * E + 16 for i in ims) + 16, max(i.height for i in ims) * E + 32), (30, 70, 120)); x = 16
        for im in ims:
            g = im.resize((im.width * E, im.height * E), Image.NEAREST); out.paste(g, (x, 16), g); x += g.width + 16
        out.save(os.path.join(voir, "heros-renouveau.png"))


main()
