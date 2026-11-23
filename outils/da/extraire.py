"""Repère les sujets d'une image de référence (composantes opaques) et les liste."""
import sys
from PIL import Image
from collections import deque

def sujets(chemin, seuil=128, aire_min=3000, pas=2):
    im = Image.open(chemin).convert("RGBA")
    a = im.getchannel("A")
    w, h = im.size
    W, H = w // pas, h // pas
    px = a.resize((W, H), Image.NEAREST).load()
    vu = [[False] * W for _ in range(H)]
    boites = []
    for y in range(H):
        for x in range(W):
            if vu[y][x] or px[x, y] < seuil: continue
            q = deque([(x, y)]); vu[y][x] = True
            x0 = x1 = x; y0 = y1 = y; n = 0
            while q:
                cx, cy = q.popleft(); n += 1
                x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < W and 0 <= ny < H and not vu[ny][nx] and px[nx, ny] >= seuil:
                        vu[ny][nx] = True; q.append((nx, ny))
            if n * pas * pas >= aire_min:
                boites.append((x0 * pas, y0 * pas, (x1 + 1) * pas, (y1 + 1) * pas, n * pas * pas))
    return im, sorted(boites, key=lambda b: (b[1] // 200, b[0]))

if __name__ == "__main__":
    for c in sys.argv[1:]:
        _, b = sujets(c)
        print(c)
        for x in b: print("  ", x)
