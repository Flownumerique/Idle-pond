"""Tire des sprites à l'échelle du jeu des images de référence : réduction par moyenne,
alpha tranché (jamais de demi-transparence), palette du héros, lissage, contour d'un pixel.

    python -I outils/da/pixeliser.py docs/da/reference <dossier de sortie>

Écrit un PNG par sujet et `_planche.png`. Une base à reprendre à la main, pas un dessin fini."""
import sys, os
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(__file__))
from extraire import sujets

REF, SORTIE = sys.argv[1], sys.argv[2]
os.makedirs(SORTIE, exist_ok=True)
CONTOUR = (74, 36, 16, 255)

def reduire(im, boite, largeur, marge=4):
    x0, y0, x1, y1 = boite[:4]
    x0, y0 = max(0, x0 - marge), max(0, y0 - marge)
    x1, y1 = min(im.width, x1 + marge), min(im.height, y1 + marge)
    morceau = im.crop((x0, y0, x1, y1))
    hauteur = max(1, round(morceau.height * largeur / morceau.width))
    # Moyenne prémultipliée : la frange transparente ne salit pas les couleurs.
    pm = morceau.convert("RGBa").resize((largeur, hauteur), Image.BOX).convert("RGBA")
    return pm

def trancher(im, seuil):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= seuil else (0, 0, 0, 0)
    return im

def reduire_palette(im, n):
    opaque = [p for p in im.getdata() if p[3] == 255]
    if not opaque: return im
    echantillon = Image.new("RGB", (len(opaque), 1)); echantillon.putdata([p[:3] for p in opaque])
    pal = echantillon.quantize(colors=n, method=Image.Quantize.MEDIANCUT)
    couleurs = pal.getpalette()[:3 * n]
    teintes = [tuple(couleurs[i:i + 3]) for i in range(0, len(couleurs), 3)]
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            p = px[x, y]
            if p[3] == 0: continue
            c = min(teintes, key=lambda t: (t[0] - p[0]) ** 2 * 3 + (t[1] - p[1]) ** 2 * 4 + (t[2] - p[2]) ** 2 * 2)
            px[x, y] = c + (255,)
    return im

def contourner(im):
    """Le bord du sujet passe au brun de contour : il efface la frange du détourage."""
    px = im.load(); bord = []
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3] == 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if not (0 <= nx < im.width and 0 <= ny < im.height) or px[nx, ny][3] == 0:
                    bord.append((x, y)); break
    for x, y in bord: px[x, y] = CONTOUR
    # un pixel isolé n'est que du bruit
    for y in range(im.height):
        for x in range(im.width):
            if px[x, y][3] and all(not (0 <= x + dx < im.width and 0 <= y + dy < im.height) or px[x + dx, y + dy][3] == 0
                                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                px[x, y] = (0, 0, 0, 0)
    return im

PALETTE = {
    "contour": 0x4A2410, "profond": 0xB83A10, "orange vif": 0xE06A10, "orange": 0xF08C1E,
    "or": 0xF2B84A, "or clair": 0xF8D070, "creme": 0xFFE6A8, "creme pale": 0xFFF4D0,
    "eclat": 0xFFFBEA, "joue": 0xFF9A80, "oeil": 0x2A1408,
    "turquoise": 0x1EA8B8, "turquoise clair": 0x7FE6E8, "etoile": 0xFFE070,
}
TEINTES = {k: ((v >> 16) & 255, (v >> 8) & 255, v & 255) for k, v in PALETTE.items()}
CYANS = ("turquoise", "turquoise clair")

def caler(im):
    """Chaque pixel prend la teinte la plus proche de la palette du héros ; ce qui tire vers le
    cyan (le mana) ne peut prendre qu'un turquoise, pour ne pas se perdre dans l'orange."""
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0: continue
            cyan = b > r + 10 and g > r
            rose = r > 235 and 110 < g < 175 and b > 105
            noms = CYANS if cyan else ["joue"] if rose else [k for k in TEINTES if k not in CYANS and k != "joue"]
            nom = min(noms, key=lambda k: (TEINTES[k][0] - r) ** 2 * 3 + (TEINTES[k][1] - g) ** 2 * 4 + (TEINTES[k][2] - b) ** 2 * 2)
            px[x, y] = TEINTES[nom] + (255,)
    return im

def lisser(im, passes=1):
    """Un pixel seul de sa couleur parmi ses huit voisins prend la couleur la plus fréquente
    autour de lui : le grain de la réduction s'en va, l'œil et le mana restent."""
    gardes = {TEINTES[k] for k in ("oeil", "turquoise", "turquoise clair", "etoile", "eclat", "joue")}
    for _ in range(passes):
        src = im.copy().load(); px = im.load()
        for y in range(im.height):
            for x in range(im.width):
                p = src[x, y]
                if p[3] == 0 or p[:3] in gardes: continue
                voisins = [src[x + dx, y + dy] for dx in (-1, 0, 1) for dy in (-1, 0, 1)
                           if (dx or dy) and 0 <= x + dx < im.width and 0 <= y + dy < im.height]
                pleins = [v for v in voisins if v[3] and v[:3] != TEINTES["contour"]]
                if sum(1 for v in pleins if v == p) >= 2 or not pleins: continue
                compte = {}
                for v in pleins: compte[v] = compte.get(v, 0) + 1
                px[x, y] = max(compte, key=compte.get)
    return im

def sprite(im, boite, largeur, seuil):
    s = reduire(im, boite, largeur)
    return contourner(lisser(caler(trancher(s, seuil)), 2))

# Chaque sujet : (référence, index de la boîte, nom, largeur en px, couleurs)
# (index de la boîte, nom, largeur en px, seuil d'alpha)
PLAN = {
    "da-1-cycle.png": [(0, "0-oeuf", 13, 40), (1, "1-larve", 20, 60),
                       (3, "3-stade1", 30, 140), (4, "4-stade2", 44, 140), (5, "5-stade3", 65, 140)],
    "da-2-croissance.png": [(2, "2-alevin", 23, 140), (3, "voile-1", 32, 140), (0, "voile-2", 48, 140), (1, "voile-3", 70, 140)],
    "da-3-eveil.png": [(0, "eveil-1-runes", 66, 140), (1, "eveil-2-voile", 80, 120), (2, "eveil-3-etincelles", 72, 90), (3, "eveil-4-aura", 72, 70)],
}

lignes = []
for fichier, plan in PLAN.items():
    im, boites = sujets(os.path.join(REF, fichier))
    ligne = []
    for i, nom, largeur, seuil in plan:
        s = sprite(im, boites[i], largeur, seuil)
        s.save(os.path.join(SORTIE, nom + ".png"))
        ligne.append((nom, s))
    lignes.append((fichier, ligne))

# La planche : chaque sprite à ×5, sur l'eau du Gour, avec son nom et sa taille.
E = 5
fonte = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 15)
largeurs = [sum(s.width * E + 24 for _, s in l) + 24 for _, l in lignes]
hauteurs = [max(s.height for _, s in l) * E + 60 for _, l in lignes]
planche = Image.new("RGB", (max(largeurs), sum(hauteurs) + 20), (20, 44, 74))
d = ImageDraw.Draw(planche)
y = 10
for (fichier, ligne), h in zip(lignes, hauteurs):
    d.text((12, y), fichier, fill=(230, 230, 210), font=fonte)
    x = 12
    for nom, s in ligne:
        g = s.resize((s.width * E, s.height * E), Image.NEAREST)
        planche.paste(g, (x, y + 22), g)
        d.text((x, y + 26 + g.height), f"{nom} · {s.width}×{s.height}", fill=(200, 210, 220), font=fonte)
        x += g.width + 24
    y += h
planche.save(os.path.join(SORTIE, "_planche.png"))
print("ok", planche.size)
