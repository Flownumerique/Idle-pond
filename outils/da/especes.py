"""Les poissons de troupe des six tribus, dessinés (Codex v1.4, §2 ; maquette validée le
2026-11-23, docs/da/apercu-poissons-v3.png), à l'échelle de la scène de 360 px.

    python -I outils/da/especes.py            # écrit Assets/IdlePond/Art/Especes/{id}-i{0,1}.png
                                              # et Especes/representants/{id}-i{0..3}.png
    python -I outils/da/especes.py --voir D   # et une planche de contrôle dans D

Chaque espèce : deux images de nage (la queue bat d'un pixel ; l'ombre change la couleur de
son voile), dans un cadre COMMUN aux six — le plus petit qui les contient toutes. Et son
représentant (Codex v1.4 : le Coryphée, la Doyenne, le Capitaine de la Garde, le Vieux Gardien,
la Mère pèlerine, le Grand Héraut), le même dessin à ≈ 60 px, en quatre images de nage comme le
héros, dans un autre cadre commun. Le script imprime les cadres et la palette, à reporter dans
Gabarits.CADRE_D_ESPECE, Gabarits.CADRE_DE_REPRESENTANT et RegistreDArt.ESPECES_DESSINEES. Les fichiers écrits sont de « vrais dessins » : le générateur
de provisoires ne les touche plus.
"""
import json
import math
import os
import random
import sys
from PIL import Image, ImageDraw

RACINE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(RACINE, "Assets", "IdlePond", "Art", "Especes")
# La longueur d'un poisson de troupe (≈ 24 px, gabarits) ; le représentant viendra à ≈ 60.
LONGUEUR_DE_TROUPE = 24
LONGUEUR_DE_REPRESENTANT = 60
IDS = ["vairon", "loche", "epinoche", "chabot", "lamproie", "ombre"]

def C(h): return tuple(int(h[i:i+2],16) for i in (1,3,5))+(255,)
OUT=C('#121C1A'); EYEW=C('#F4F1E4'); PUP=C('#121C1A'); SHINE=C('#FFFFFF')

def poisson(sp, L):
    k=L/40.0
    W=int(L*1.45)+6; H=int(L*0.95)+6
    img=Image.new('RGBA',(W,H),(0,0,0,0)); px=img.load()
    d=ImageDraw.Draw(img)
    a=L/2; b=sp['h']*k/2
    cx=W*0.55; cy=H*0.55
    rnd=random.Random(sp['seed'])
    def hh(x):
        u=(x-cx)/a
        if abs(u)>1: return -1
        base=(1-abs(u)**2.4)**(1/2.4)
        t=(u+1)/2  # 0 tail .. 1 head
        return b*base*(sp['taper']+(1-sp['taper'])*t**0.6) if u<0.6 else b*base*(1.0+0.15*(1-t))
    # nageoires derriere le corps
    x0=cx-a+1
    tl=sp['tail']*k; th=sp['tailh']*k
    if sp['tailtype']=='fork':
        d.polygon([(x0+2,cy-1),(x0-tl,cy-th),(x0-tl*0.55,cy),(x0-tl,cy+th),(x0+2,cy+1)],fill=C(sp['fin']))
    elif sp['tailtype']=='leaf':
        d.polygon([(x0+2,cy),(x0-tl*0.5,cy-th),(x0-tl,cy),(x0-tl*0.5,cy+th)],fill=C(sp['acc']))
        d.line([(x0+1,cy),(x0-tl+1,cy)],fill=C(sp['acc2']))
        for i in range(1,3):
            xx=x0-tl*0.3*i; d.line([(xx,cy),(xx-2*k*2,cy-th*0.5)],fill=C(sp['acc2'])); d.line([(xx,cy),(xx-2*k*2,cy+th*0.5)],fill=C(sp['acc2']))
    elif sp['tailtype']=='round':
        d.ellipse([x0-tl,cy-th,x0+3,cy+th],fill=C(sp['fin']))
    elif sp['tailtype']=='point':
        d.polygon([(x0+3,cy-th*0.5),(x0-tl,cy),(x0+3,cy+th*0.5)],fill=C(sp['fin']))
    # dorsale
    dx=cx+sp['dorsx']*a
    if sp.get('sail'):
        top=cy-b-sp['sail']*k
        pts=[(cx-a*0.6,cy-b*0.5),(cx-a*0.62,top+6*k),(cx-a*0.45,top+1),(cx-a*0.2,top+3*k),(cx+a*0.02,top),(cx+a*0.18,top+5*k),(cx+a*0.3,cy-b*0.8)]
        d.polygon(pts,fill=C(sp['acc']))
        # bandes du vitrail
        for i,col in enumerate(sp['sailcols']):
            yy=top+3+i*max(1,int(3*k*2))
            d.line([(cx-a*0.5,yy+3),(cx+a*0.15,yy)],fill=C(col))
        for i in range(4):
            px_=int(cx-a*0.4+i*a*0.15); py_=int(top+5*k+i%2*2)
            if 0<=px_<W and 0<=py_<H: px[px_,py_]=C(sp['acc2'])
    else:
        dh=sp['dorsh']*k
        d.polygon([(dx-a*0.28,cy-hh(dx)+2),(dx-a*0.12,cy-hh(dx)-dh),(dx+a*0.02,cy-hh(dx)-dh*0.8),(dx+a*0.14,cy-hh(dx)+2)],fill=C(sp['fin']))
        if k>0.6:
            for i in range(3):
                xx=dx-a*0.18+i*a*0.1; d.line([(xx,cy-hh(dx)+1),(xx+a*0.03,cy-hh(dx)-dh*0.75)],fill=C(sp['dos']))
    # pelvienne / pectorale
    pxp=cx+a*0.35
    if sp['pect']>0:
        d.polygon([(pxp+a*0.05,cy+b*0.5),(pxp-a*0.32,cy+b*0.75+sp['pect']*k),(pxp-a*0.38,cy+b*0.6+sp['pect']*k*0.6),(pxp-a*0.15,cy+b*0.45)],fill=C(sp['fin']))
    # corps
    for x in range(W):
        h_=hh(x+0.5)
        if h_<=0: continue
        for y in range(H):
            if abs(y+0.5-cy)<=h_:
                rel=(y+0.5-(cy-h_))/(2*h_)
                if rel<sp['cut'][0]: col=sp['dos']
                elif rel<sp['cut'][1]: col=sp['corps']
                else:
                    col=sp['ventre']
                    if sp.get('gorge') and x<cx: col=sp['corps2'] if 'corps2' in sp else sp['corps']
                if 0.14<rel<0.24 and x>cx-a*0.3 and x<cx+a*0.7: col=sp['reflet']
                px[x,y]=C(col)
    # taches
    for _ in range(sp.get('spots',0)):
        x=rnd.randint(int(cx-a*0.7),int(cx+a*0.5)); 
        h_=hh(x+0.5)
        if h_<=1: continue
        y=rnd.randint(int(cy-h_)+1,int(cy+h_*0.3))
        if px[x,y][3]: 
            px[x,y]=C(sp['tache'])
            if L>20 and x+1<W and px[x+1,y][3]: px[x+1,y]=C(sp['tache'])
    # details propres
    for f in sp.get('extra',[]): f(d,px,cx,cy,a,b,k,W,H,hh)
    # contour
    out=img.copy(); po=out.load()
    for x in range(W):
        for y in range(H):
            if px[x,y][3]==0:
                for dx_,dy_ in ((1,0),(-1,0),(0,1),(0,-1)):
                    xx,yy=x+dx_,y+dy_
                    if 0<=xx<W and 0<=yy<H and px[xx,yy][3]: po[x,y]=OUT; break
    # opercule
    gx=cx+a*sp.get('gill',0.42)
    for y in range(H):
        h_=hh(gx)
        if h_>0 and abs(y+0.5-cy)<h_*0.62 and L>20:
            xg=int(gx-((y+0.5-cy)/h_)**2*a*0.06)
            if po[xg,y][3] and po[xg,y]!=OUT: po[xg,y]=C(sp['dos'])
    # oeil (apres contour)
    ex=int(cx+a*sp['eyex']); ey=int(cy-b*sp['eyey'])
    if sp.get('joue') and L>20:
        jx,jy=ex-1,int(ey+sp['eye']*k+2)
        for dx_ in range(-1,2):
            if po[jx+dx_,jy][3] and po[jx+dx_,jy]!=OUT: po[jx+dx_,jy]=C(sp['joue'])
    r=max(1,round(sp['eye']*k)) if L>20 else 1
    ImageDraw.Draw(out).ellipse([ex-r,ey-r,ex+r,ey+r],fill=OUT)
    ImageDraw.Draw(out).ellipse([ex-r+1,ey-r+1,ex+r-1,ey+r-1],fill=EYEW if r>1 else PUP)
    if r>1:
        pr=max(1,r-1)
        ImageDraw.Draw(out).ellipse([ex-pr+1,ey-pr+1,ex+pr-1+1,ey+pr-1+1],fill=C(sp.get('iris','#121C1A')))
        po[ex-pr+1 if pr>1 else ex, ey-pr+1 if pr>1 else ey-1]=SHINE
    # bouche souriante
    mx=int(cx+a*0.97); my=int(cy+b*sp.get('mouthy',0.15))
    if sp.get('mouth','smile')=='smile' and mx<W:
        po[mx-1,my]=OUT; 
        if L>20: po[mx-2,my+1]=OUT
    return out

def runes(col):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        for (rx,ry) in ((-0.1,0.05),(0.2,0.0)):
            x=int(cx+a*rx); y=int(cy+b*ry)
            s=max(1,int(1.5*k*2/2))
            for dx in range(-s,s+1): 
                if 0<=x+dx<W: px[x+dx,y]=C(col)
            for dy in range(-s,s+1):
                if 0<=y+dy<H: px[x,y+dy]=C(col)
    return f
def barbillons(col,tip):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        x=cx+a*0.98; y=cy+b*0.35
        for i,(ex,ey) in enumerate(((6,3),(7,6),(4,7))):
            d.line([(x,y),(x+ex*k,y+ey*k)],fill=C(col))
            tx,ty=int(x+ex*k),int(y+ey*k)
            if 0<=tx<W and 0<=ty<H: px[tx,ty]=C(tip)
    return f
def epines(col,tip):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        for i,u in enumerate((-0.25,0.05,0.3)):
            x=cx+a*u; y=cy-hh(x)
            ln=(7-i)*k*1.2
            d.line([(x,y),(x+1.5*k,y-ln)],fill=C(col),width=max(1,int(k*1.6)))
            tx,ty=int(x+1.5*k),int(y-ln)
            if 0<=tx<W and 0<=ty<H: px[tx,ty]=C(tip)
    return f
def lichen(col,col2):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        x=cx+a*0.45; y=cy-b*0.75
        d.ellipse([x-3*k,y-2*k,x+3*k,y+1.5*k],fill=C(col))
        if k>0.6: d.point([(x-k,y-k),(x+k,y)],fill=C(col2))
    return f
def ventouse(col,ring):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        x=cx+a*0.99; r=max(1,b*0.75)
        d.ellipse([x-r*0.6,cy-r,x+r*0.9,cy+r],fill=C(ring))
        d.ellipse([x-r*0.2,cy-r*0.5,x+r*0.5,cy+r*0.5],fill=C(col))
        # pores branchiaux
        for i in range(7):
            xx=int(cx+a*0.55-i*a*0.06); yy=int(cy-b*0.05)
            if 0<=xx<W and px[xx,yy][3]: px[xx,yy]=C('#3A2458')
    return f
def graines(col,n):
    def f(d,px,cx,cy,a,b,k,W,H,hh):
        rr=random.Random(3)
        for _ in range(n):
            x=rr.randint(0,W-1); y=rr.randint(0,H-1)
            if px[x,y][3]==0 and (x<cx-a*0.6 or y<cy-b-2): px[x,y]=C(col)
    return f

ESPECES=[
 dict(nom='le vairon · le Chœur', seed=1, h=17, taper=0.5, cut=(0.32,0.6), joue='#FF8AA0', dos='#2E7D5B', corps='#7FC8A0', ventre='#E8483A', reflet='#B9F0CF',
      fin='#5DB07E', acc='#B8E86A', acc2='#6FA83A', tailtype='leaf', tail=13, tailh=6, dorsh=6, dorsx=-0.05, pect=3, eye=4.2, eyex=0.6, eyey=0.12,
      extra=[]),
 dict(nom='la loche · les Devineresses', seed=2, h=13, taper=0.7, cut=(0.35,0.66), dos='#6E4A22', corps='#C08A3E', ventre='#F0D9A0', reflet='#E2B470', tache='#7A5020', spots=14,
      fin='#A8783A', tailtype='round', tail=7, tailh=5, dorsh=3, dorsx=-0.2, pect=2, eye=3.4, eyex=0.66, eyey=0.18, mouthy=0.35,
      extra=[runes('#4FE0D0'), barbillons('#5A3A18','#FFFFFF')]),
 dict(nom="l'épinoche · la Garde", seed=3, h=18, taper=0.42, joue='#FF8AA0', cut=(0.33,0.58), dos='#2F5A35', corps='#6FA05A', ventre='#D8283A', reflet='#A8D88A', gorge=True,
      fin='#5A8A48', tailtype='fork', tail=9, tailh=6, dorsh=3, dorsx=-0.45, pect=4, eye=4.0, eyex=0.6, eyey=0.12, iris='#2F7FE0',
      extra=[epines('#FF8FA0','#FFE4EA')]),
 dict(nom='le chabot · les Gardiens', seed=4, h=21, taper=0.3, cut=(0.4,0.7), dos='#4B545C', corps='#7C8790', ventre='#C9CFC9', reflet='#A4AFB8', tache='#3A4148', spots=26,
      fin='#69747C', tailtype='round', tail=7, tailh=5, dorsh=4, dorsx=-0.25, pect=7, eye=3.8, eyex=0.6, eyey=0.3, mouthy=0.3,
      extra=[lichen('#C8A040','#F0D070')]),
 dict(nom='la lamproie · les Pèlerines', seed=5, h=10, taper=0.6, cut=(0.35,0.62), dos='#5A3F7A', corps='#9B7FC0', ventre='#D9C8F0', reflet='#C4A8E8',
      fin='#7E60A8', tailtype='point', tail=8, tailh=4, dorsh=2, dorsx=-0.45, pect=0, eye=2.8, eyex=0.68, eyey=0.12, mouth='none', gill=0.3,
      extra=[ventouse('#2A1A40','#9FF0FF')]),
 dict(nom="l'ombre · les Hérauts", seed=6, h=16, taper=0.5, cut=(0.32,0.6), joue='#F0A0D0', dos='#4A5A8A', corps='#A8B8D8', ventre='#E8EEF8', reflet='#D4E0F4', tache='#3A4870', spots=8,
      fin='#7888B0', acc='#C060F0', acc2='#FFE070', sail=13, sailcols=['#8A50E0','#40D0C0','#F070C0'], tailtype='fork', tail=9, tailh=6, dorsx=0, pect=3, eye=4.0, eyex=0.6, eyey=0.12),
]


def battre(im, decalage):
    """La queue (le tiers arrière) monte ou descend d'un pixel ; le reste ne bouge pas."""
    if decalage == 0: return im.copy()
    px = im.load()
    pleins = [x for x in range(im.width) for y in range(im.height) if px[x, y][3]]
    gauche, droite = min(pleins), max(pleins)
    queue = gauche + round((droite - gauche) * 0.3)
    out = im.copy(); po = out.load()
    for x in range(queue):
        for y in range(im.height): po[x, y] = (0, 0, 0, 0)
        for y in range(im.height):
            sy = y + decalage
            if 0 <= sy < im.height and px[x, sy][3]: po[x, y] = px[x, sy]
    return out


def representant(sp):
    """Les quatre images du représentant : la queue au milieu, haute, au milieu, basse — comme le
    héros ; le voile de l'ombre change de couleur à mi-course."""
    a = poisson(sp, LONGUEUR_DE_REPRESENTANT)
    b = a
    if sp.get("sail"):
        autre = dict(sp); autre["acc"] = "#40D0C0"
        b = poisson(autre, LONGUEUR_DE_REPRESENTANT)
    return [battre(a, 0), battre(a, -1), battre(b, 0), battre(b, 1)]


def rogner(toutes, dossier):
    """Écrit chaque image dans le cadre commun (plus un pixel transparent de chaque côté) ;
    rend le cadre et les couleurs employées."""
    boites = [im.getbbox() for ims in toutes.values() for im in ims]
    boite = (min(b[0] for b in boites) - 1, min(b[1] for b in boites) - 1,
             max(b[2] for b in boites) + 1, max(b[3] for b in boites) + 1)
    os.makedirs(dossier, exist_ok=True)
    couleurs = set()
    for i, ims in toutes.items():
        for n, im in enumerate(ims):
            c = im.crop(boite)
            for p in c.getdata():
                if p[3]: couleurs.add("0x%02X%02X%02X" % p[:3])
            c.save(os.path.join(dossier, f"{i}-i{n}.png"))
    return [boite[2] - boite[0], boite[3] - boite[1]], couleurs


def images(sp):
    """Les deux images de nage : la queue haute, la queue basse ; le voile de l'ombre change de
    couleur d'une image à l'autre (RegistreDArt : #C060F0 puis #40D0C0)."""
    a = poisson(sp, LONGUEUR_DE_TROUPE)
    if sp.get("sail"):
        autre = dict(sp); autre["acc"] = "#40D0C0"
        b = poisson(autre, LONGUEUR_DE_TROUPE)
    else:
        b = a
    return [battre(a, -1), battre(b, 1)]


def main():
    voir = sys.argv[sys.argv.index("--voir") + 1] if "--voir" in sys.argv else None
    cadre, couleurs = rogner({i: images(sp) for i, sp in zip(IDS, ESPECES)}, ART)
    dossier = os.path.join(ART, "representants")
    cadre_r, couleurs_r = rogner({i: representant(sp) for i, sp in zip(IDS, ESPECES)}, dossier)
    print(json.dumps({"cadre": cadre, "representant": cadre_r, "palette": sorted(couleurs | couleurs_r)}))
    if voir:
        os.makedirs(voir, exist_ok=True)
        E = 5
        out = Image.new("RGB", (len(IDS) * (cadre_r[0] * E + 16) + 16, cadre_r[1] * E + cadre[1] * E + 48), (40, 110, 100))
        for k, i in enumerate(IDS):
            x = 16 + k * (cadre_r[0] * E + 16)
            im = Image.open(os.path.join(dossier, f"{i}-i0.png"))
            gr = im.resize((im.width * E, im.height * E), Image.NEAREST); out.paste(gr, (x, 16), gr)
            im = Image.open(os.path.join(ART, f"{i}-i0.png"))
            gr = im.resize((im.width * E, im.height * E), Image.NEAREST); out.paste(gr, (x, 32 + cadre_r[1] * E), gr)
        out.save(os.path.join(voir, "especes.png"))


main()
