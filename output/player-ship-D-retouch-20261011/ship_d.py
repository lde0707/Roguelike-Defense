import math, os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out_d"); os.makedirs(OUT + "/sprites", exist_ok=True)
BG = Image.open('/root/.claude/uploads/b5d65218-6c60-502e-b6bc-18b17bd86acf/7a615b3f-image.png').convert('RGB')

def hx(s):
    s = s.lstrip('#'); return tuple(int(s[i:i+2], 16) for i in (0, 2, 4)) + (255,)

OUTLINE = hx('#1A1D35')
HULL = {k: hx(v) for k, v in dict(base='#B48570', sub='#946A58', light='#D6AE93', shadow='#7A5546',
                                  dark='#38283A', core='#92CFC6', corel='#D4F2EC').items()}
TP = {'m': hx('#646A84'), 'l': hx('#99A0B8'), 's': hx('#474B62'), 'd': hx('#2A2C3C'),
      'a': hx('#9AD6CC'), 'h': hx('#DDF5EF'), 'o': OUTLINE}

# ---------------- hand-pixelled turrets (13 wide, muzzle up) ----------------
BASE = [
    "....lmmms....",
    "...lmmmmms...",
    "..lmmdddmms..",
    "..lmdddddms..",
    "..lmmdddmms..",
    "...smmmmms...",
    "....sssss....",
]
PIVOT_FROM_BOTTOM = 3   # base row b3 = rotation pivot

M = {
 'ROT-01': ("쌍열 연사포", [
    "...hah.hah...",
    "...lms.lms...",
    "...lms.lms...",
    "...lms.lms...",
    "...ddd.ddd...",
    "..lmmmmmmms..",
 ]),
 'ROT-02': ("부채 산탄포", [
    "a.a.a.a.a.a.a",
    "lmmmmmmmmmmms",
    ".lmmmmmmmmms.",
    "..lmmmmmmms..",
    "...lmmmmms...",
    "...ddmmmdd...",
 ]),
 'ROT-03': ("부착 드릴포", [
    "......h......",
    ".....aha.....",
    ".....ldd.....",
    "....lmmdd....",
    "....ddmms....",
    "...lmddmms...",
    "...ddmmdds...",
    "..lmmddmmds..",
    "..dddddddd...",
 ]),
 'ROT-04': ("관통 원반포", [
    ".....aaa.....",
    "....ahhaa....",
    "....aadaa....",
    ".....aaa.....",
    "lmdddddddddms",
    "lmmmmmmmmmmms",
    ".lmmmmmmmmms.",
 ]),
 'ROT-05': ("폭발 구체포", [
    "....lmmms....",
    "...lmdddms...",
    "..lmdddddms..",
    "..lmddaddms..",
    "..lmdddddms..",
    "...lmdddms...",
    "....lmmms....",
    "....lmmms....",
 ]),
 'ROT-06': ("냉각 분사포", [
    ".h.h.h.h.h.h.",
    ".a.a.a.a.a.a.",
    ".l.m.m.m.m.s.",
    "lmmmmmmmmmmms",
    "lmmmmmmmmmmms",
    ".dmmmmmmmmmd.",
 ]),
 'ROT-07': ("부식 분무포", [
    "aa...aha...aa",
    "lm...lms...ms",
    ".lm..lms..ms.",
    "..lm.lms.ms..",
    "...lmmmmms...",
    "...ddmmmdd...",
 ]),
 'ROT-08': ("복제 표식포", [
    ".aa.......aa.",
    ".lm.......ms.",
    ".lm..aaa..ms.",
    ".lm..aha..ms.",
    ".lm..aaa..ms.",
    ".lmmmmmmmmms.",
    ".dmmmmmmmmmd.",
 ]),
 'ROT-09': ("귀환 부메랑포", [
    "ha.........ah",
    "aaa.......aaa",
    ".lmm.....mms.",
    ".lmmmdddmmms.",
    "..lmmmmmmms..",
    "...lmmmmms...",
 ]),
 'ROT-10': ("잔류 톱날포", [
    "..a.a.a.a.a..",
    "...ahhaaaa...",
    "..lmmmmmmms..",
    ".lmmddddmmms.",
    ".lmdmmmmmdms.",
    ".lmmddddmmms.",
    "..lmmmmmmms..",
 ]),
 'ROT-11': ("충격 파동포", [
    "....aahaa....",
    "..aaa...aaa..",
    ".aa.......aa.",
    "...lmmmmms...",
    ".llmmmmmmmss.",
    ".lmmmmmmmmms.",
    "....dmmmd....",
 ]),
 'ROT-12': ("십자 분열포", [
    ".hhhhhhhhhhh.",
    ".lmmmmdmmmms.",
    ".lmmmmdmmmms.",
    ".lmdddddddms.",
    ".lmmmmdmmmms.",
    ".lmmmmdmmmms.",
    ".lmmmmmmmmms.",
 ]),
 'ROT-13': ("감속 그물포", [
    "ha.........ah",
    "lm.........ms",
    "lma.......ams",
    "lmmmmmmmmmmms",
    ".sssslmssss..",
    ".....lms.....",
    "....lmmms....",
 ]),
 'ROT-14': ("과충전 표식포", [
    "..hhhhhhhhh..",
    "..lmmmmmmms..",
    ".alddddddda..",
    ".almdahadma..",
    "..lmdddddms..",
    "..lmmmmmmms..",
    "..ddmmmmmdd..",
 ]),
 'ROT-15': ("봉합 포말포", [
    "......hh.....",
    "..ll.hhah....",
    ".lhhl.aa.ll..",
    ".lhhl...lhhl.",
    "..ll.lll.ll..",
    "....lmmms....",
    "...lmmmmms...",
 ]),
 'ROT-16': ("연쇄 도체포", [
    ".ha.......ah.",
    ".aa.......aa.",
    "..lm.....ms..",
    "..lm.....ms..",
    "...lm...ms...",
    "...lmmmmms...",
    "....lmmms....",
 ]),
}

def turret_rows(tid):
    rows = M[tid][1] + BASE
    for r in rows: assert len(r) == 13, (tid, r)
    return rows

def turret_fill(tid):
    rows = turret_rows(tid)
    im = Image.new('RGBA', (13, len(rows)), (0, 0, 0, 0))
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != '.': im.putpixel((x, y), TP[ch])
    return im

def add_outline(im):
    w, h = im.size
    out = Image.new('RGBA', (w+2, h+2), (0, 0, 0, 0)); out.paste(im, (1, 1))
    px = out.load(); src = out.copy().load()
    for y in range(h+2):
        for x in range(w+2):
            if src[x, y][3]: continue
            for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x+ox, y+oy
                if 0 <= xx < w+2 and 0 <= yy < h+2 and src[xx, yy][3]:
                    px[x, y] = OUTLINE; break
    return out

# ---------------- RotSprite (EPX x3 -> rotate nearest -> downsample) ----------------
def epx(im):
    w, h = im.size; s = im.load()
    o = Image.new('RGBA', (w*2, h*2)); d = o.load()
    g = lambda x, y: s[min(max(x, 0), w-1), min(max(y, 0), h-1)]
    for y in range(h):
        for x in range(w):
            P = s[x, y]; A = g(x, y-1); B = g(x+1, y); C = g(x-1, y); D = g(x, y+1)
            p1 = A if (C == A and C != D and A != B) else P
            p2 = B if (A == B and A != C and B != D) else P
            p3 = C if (D == C and D != B and C != A) else P
            p4 = D if (B == D and B != A and D != C) else P
            d[2*x, 2*y] = p1; d[2*x+1, 2*y] = p2; d[2*x, 2*y+1] = p3; d[2*x+1, 2*y+1] = p4
    return o

def rotsprite(im, deg, pivot):
    """rotate fill sprite around pivot (x,y in px). returns (img, new_pivot)."""
    w, h = im.size
    pad = int(math.hypot(w, h)) + 2
    canvas = Image.new('RGBA', (pad*2, pad*2), (0, 0, 0, 0))
    canvas.paste(im, (pad - int(pivot[0]), pad - int(pivot[1])))
    big = epx(epx(epx(canvas)))
    rot = big.rotate(-deg, resample=Image.NEAREST, center=(pad*8 + 4, pad*8 + 4))
    small = Image.new('RGBA', canvas.size, (0, 0, 0, 0)); sp = small.load(); rp = rot.load()
    for y in range(canvas.size[1]):
        for x in range(canvas.size[0]):
            sp[x, y] = rp[x*8+4, y*8+4]
    return small, (pad, pad)

def clean_orphans(im):
    """replace single pixels whose colour matches none of their 4 neighbours (anti-mush)."""
    w, h = im.size; s = im.copy().load(); d = im.load()
    for y in range(1, h-1):
        for x in range(1, w-1):
            P = s[x, y]
            if not P[3] or P == OUTLINE: continue
            nb = [s[x+1, y], s[x-1, y], s[x, y+1], s[x, y-1]]
            if P not in nb:
                opaque = [n for n in nb if n[3] and n != OUTLINE]
                if len(opaque) >= 3:
                    d[x, y] = max(set(opaque), key=opaque.count)
    return im

# ---------------- hull D (mirror-symmetric) ----------------
HN = 50
def hull_label(x, y):
    r = math.hypot(x, y); a = math.degrees(math.atan2(x, -y)) % 360
    t = ((a + 36) % 72) - 36           # angle relative to nearest slot
    R = 21.0
    if 20 <= abs(t) <= 29: R = 23.0       # teeth: two per gap
    if abs(t) < 13: R = 18.5              # turret bay recess
    if r > R: return None
    if r <= 3.2: return 'core'
    if r <= 4.6: return 'dark'
    if r <= 7.6:
        return 'dark' if (abs(x) < 0.7 and y < 0) or abs(abs(t) - 36) * r / 57 < 0.55 else 'sub'
    if r <= 8.8: return 'dark'
    if abs(r - 13.4) <= 0.6: return 'dark'
    if r > R - 2.6: return 'sub'
    return 'base'

def render_hull():
    c = (HN - 1) / 2
    lab = [[None]*HN for _ in range(HN)]
    for y in range(HN):
        for x in range(HN // 2 + 1):
            l = hull_label(x - c, y - c)
            lab[y][x] = l; lab[y][HN-1-x] = l
    ins = lambda x, y: 0 <= x < HN and 0 <= y < HN and lab[y][x] is not None
    im = Image.new('RGBA', (HN, HN), (0, 0, 0, 0))
    for y in range(HN):
        for x in range(HN):
            l = lab[y][x]
            if l is None: continue
            if l == 'core': col = HULL['corel'] if (x - c) + (y - c) < -2 else HULL['core']
            elif l in ('base', 'sub'):
                if not ins(x-1, y) or not ins(x, y-1): col = HULL['light']
                elif not ins(x+1, y) or not ins(x, y+1): col = HULL['shadow']
                else: col = HULL[l]
            else: col = HULL[l]
            im.putpixel((x, y), col)
    return im

MOUNT_R = 17.0
SN = 76
def assemble(loadout):
    hull = render_hull()
    canvas = Image.new('RGBA', (SN, SN), (0, 0, 0, 0))
    off = (SN - HN) // 2
    canvas.paste(hull, (off, off))
    labels = [[('H' if canvas.getpixel((x, y))[3] else None) for x in range(SN)] for y in range(SN)]
    c = (SN - 1) / 2
    for k, tid in enumerate(loadout):
        fill = turret_fill(tid)
        piv = (6.5, fill.size[1] - PIVOT_FROM_BOTTOM - 0.5)
        rot, (px, py) = rotsprite(fill, k * 72, piv)
        rot = clean_orphans(rot)
        ang = math.radians(k * 72)
        mx = c + math.sin(ang) * MOUNT_R; my = c - math.cos(ang) * MOUNT_R
        ox, oy = round(mx - px), round(my - py)
        rp = rot.load()
        for y in range(rot.size[1]):
            for x in range(rot.size[0]):
                if rp[x, y][3]:
                    X, Y = x + ox, y + oy
                    if 0 <= X < SN and 0 <= Y < SN:
                        canvas.putpixel((X, Y), rp[x, y]); labels[Y][X] = 'T%d' % k
    # outline + seams
    src = canvas.copy().load(); out = canvas.load()
    for y in range(SN):
        for x in range(SN):
            l = labels[y][x]
            nbs = [(x+1, y), (x-1, y), (x, y+1), (x, y-1)]
            if l is None:
                if any(0 <= a < SN and 0 <= b < SN and labels[b][a] for a, b in nbs): out[x, y] = OUTLINE
            elif l.startswith('T'):
                if any(0 <= a < SN and 0 <= b < SN and labels[b][a] == 'H' for a, b in nbs): out[x, y] = OUTLINE
    return canvas

def compose_display(loadout, sc=5, ss=4):
    """engine-like preview: pixels stay square, sprites rotate at screen resolution."""
    size = SN * sc
    canvas = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    h = add_outline(render_hull()).resize(((HN+2)*sc, (HN+2)*sc), Image.NEAREST)
    canvas.alpha_composite(h, ((size - h.size[0])//2, (size - h.size[1])//2))
    c = size / 2
    for k, tid in enumerate(loadout):
        fill = turret_fill(tid); t = add_outline(fill)
        pvx, pvy = 7.5, fill.size[1] - PIVOT_FROM_BOTTOM - 0.5 + 1
        f = sc * ss
        big = t.resize((t.size[0]*f, t.size[1]*f), Image.NEAREST)
        R = int(math.hypot(t.size[0], t.size[1]) * f) + 4
        sq = Image.new('RGBA', (2*R, 2*R), (0, 0, 0, 0))
        sq.paste(big, (R - int(pvx*f), R - int(pvy*f)))
        sq = sq.rotate(-k*72, resample=Image.NEAREST, center=(R, R))
        sq = sq.resize((2*R//ss, 2*R//ss), Image.BOX)
        ang = math.radians(k*72)
        mx = c + math.sin(ang)*MOUNT_R*sc; my = c - math.cos(ang)*MOUNT_R*sc
        canvas.alpha_composite(sq, (round(mx - R/ss), round(my - R/ss)))
    return canvas

def font(sz, bold=False):
    p = '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc'
    if not os.path.exists(p) or not bold: p = '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
    return ImageFont.truetype(p, sz, index=1)

def shadow_of(sprite, alpha=70):
    sh = Image.new('RGBA', sprite.size, (0, 0, 0, 0)); sh.paste((20, 24, 60, alpha), mask=sprite.split()[3]); return sh

LOADOUTS = [
    ("기본 조합", ['ROT-01', 'ROT-02', 'ROT-03', 'ROT-06', 'ROT-05']),
    ("제어 조합", ['ROT-04', 'ROT-07', 'ROT-09', 'ROT-11', 'ROT-16']),
    ("지원 조합", ['ROT-08', 'ROT-10', 'ROT-12', 'ROT-13', 'ROT-14']),
    ("혼합 조합", ['ROT-15', 'ROT-01', 'ROT-03', 'ROT-07', 'ROT-05']),
]

# export sprites
hull = render_hull(); add_outline(hull).save(f"{OUT}/sprites/hull_D.png")
for tid in M: add_outline(turret_fill(tid)).save(f"{OUT}/sprites/turret_{tid}.png")
ships = [compose_display(l) for _, l in LOADOUTS]
for i, s in enumerate(ships): s.save(f"{OUT}/ship_D_loadout{i+1}_preview.png")

# sheet 1: loadouts on background
SC = 5; cell = 440; lane = BG.crop((560, 200, 1370, 1010))
W = 40 + 4*(cell+40); H = 110 + cell + 230
sh = Image.new('RGB', (W, H), (36, 40, 66)); d = ImageDraw.Draw(sh)
d.text((40, 28), 'D. 톱니 원반 · 리터치 (포탑 수작업 픽셀 · 화면 배율 회전 미리보기)', font=font(38, True), fill=(236, 232, 220))
for i, ((nm, lo), s) in enumerate(zip(LOADOUTS, ships)):
    x0 = 40 + i*(cell+40); y0 = 100
    sh.paste(lane.resize((cell, cell), Image.NEAREST), (x0, y0))
    big = s
    o = (x0 + (cell - big.size[0])//2, y0 + (cell - big.size[1])//2)
    sh.paste(shadow_of(big), (o[0]+10, o[1]+14), shadow_of(big)); sh.paste(big, o, big)
    ty = y0 + cell + 16
    d.text((x0, ty), f"로드아웃 {i+1} · {nm}", font=font(28, True), fill=(240, 236, 224))
    for j, tid in enumerate(lo):
        d.text((x0, ty+44+j*30), f"{'①②③④⑤'[j]} {tid} {M[tid][0]}", font=font(20), fill=(214, 216, 232))
sh.save(f"{OUT}/01_D_로드아웃4종.png")

# sheet 2: turret catalog (upright, crisp)
TS = 12; cw = 260; chh = 300
cat = Image.new('RGB', (40 + 4*cw + 40, 110 + 4*chh), (36, 40, 66)); cd = ImageDraw.Draw(cat)
cd.text((40, 28), '포탑 16종 리터치 · 13px 폭 수작업 픽셀', font=font(38, True), fill=(236, 232, 220))
for i, tid in enumerate(M):
    r, cc = divmod(i, 4); x0 = 40 + cc*cw; y0 = 100 + r*chh
    cd.rectangle((x0+8, y0, x0+cw-8, y0+240), fill=(108, 116, 178))
    t = add_outline(turret_fill(tid)); t = t.resize((t.size[0]*TS, t.size[1]*TS), Image.NEAREST)
    cat.paste(t, (x0 + (cw - t.size[0])//2, y0 + 240 - t.size[1] - 8), t)
    cd.text((x0+12, y0+248), f"{tid} {M[tid][0]}", font=font(21, True), fill=(236, 232, 220))
cat.save(f"{OUT}/02_포탑16종_리터치.png")

# sheet 3: in-game scale
pre = BG.copy().convert('RGBA')
s = compose_display(LOADOUTS[0][1], sc=2, ss=8)
o = (965 - s.size[0]//2, 560 - s.size[1]//2)
pre.alpha_composite(shadow_of(s), (o[0]+6, o[1]+9)); pre.alpha_composite(s, o)
pre.convert('RGB').save(f"{OUT}/03_D_인게임_미리보기.png")
print('ok')
