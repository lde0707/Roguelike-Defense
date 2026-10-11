import math, os
from PIL import Image, ImageDraw, ImageFont

OUT = os.path.join(os.path.dirname(__file__), "out")
os.makedirs(OUT, exist_ok=True)
BG = Image.open('/root/.claude/uploads/b5d65218-6c60-502e-b6bc-18b17bd86acf/7a615b3f-image.png').convert('RGB')

def hx(s):
    s = s.lstrip('#'); return tuple(int(s[i:i+2], 16) for i in (0, 2, 4)) + (255,)

OUTLINE = hx('#1A1D35')
SCALE = 1.4
N = 84
C = (N - 1) / 2.0

# ---------------- turret shapes (local u = outward, v = lateral) ----------------
def circ(u, v, cu, cv, r): return (u-cu)**2 + (v-cv)**2 <= r*r
def rect(u, v, u0, u1, v0, v1): return u0 <= u <= u1 and v0 <= v <= v1
def seg(u, v, ang, length, w, u0=1.5):
    a = math.radians(ang); du, dv = math.cos(a), math.sin(a)
    pu, pv = u-u0, v
    t = pu*du + pv*dv
    if t < 0 or t > length: return False
    return abs(-pu*dv + pv*du) <= w/2

# each returns None / 'm' (metal) / 'd' (dark detail) / 'a' (accent) / 'l' (light)
def T01(u, v):  # 쌍열 연사포
    for c in (-1.6, 1.6):
        if rect(u, v, 2, 8.6, c-0.95, c+0.95):
            return 'a' if u > 7.6 else ('l' if v < c else 'm')
def T02(u, v):  # 부채 산탄포
    if 2 <= u <= 7.2 and abs(v) <= 1.6 + (u-2)*0.75:
        if u > 6.3: return 'a' if round(v) % 2 == 0 else 'd'
        return 'm'
def T03(u, v):  # 부착 드릴포
    if 2 <= u <= 10 and abs(v) <= 3.2*(1-(u-2)/8.6):
        if u > 8.6: return 'a'
        return 'd' if int(math.floor((u - v*0.7)/1.3)) % 2 == 0 else 'l'
def T04(u, v):  # 관통 원반포
    if circ(u, v, 6.6, 0, 1.9): return 'a' if not circ(u, v, 6.6, 0, 0.7) else 'd'
    if rect(u, v, 2, 5.2, -4.6, 4.6):
        if rect(u, v, 4.2, 5.2, -3.6, 3.6): return 'd'
        return 'm'
def T05(u, v):  # 폭발 구체포
    if circ(u, v, 5, 0, 3.6):
        if circ(u, v, 5.4, 0, 1.8): return 'd' if not circ(u, v, 5.8, 0, 0.8) else 'a'
        return 'm'
    if rect(u, v, 1.5, 4, -2, 2): return 'm'
def T06(u, v):  # 냉각 분사포 (빗살)
    if rect(u, v, 2, 4.4, -3.8, 3.8): return 'm'
    if 4.4 < u <= 7.6 and abs(v) <= 3.8 + (u-4.4)*0.25:
        k = round(v / 1.55)
        if abs(v - k*1.55) <= 0.5: return 'a' if u > 6.8 else 'm'
def T07(u, v):  # 부식 분무포 (3구)
    if circ(u, v, 3, 0, 2.2): return 'm'
    for ang in (-30, 0, 30):
        if seg(u, v, ang, 6.4, 1.7, 2.5):
            a = math.radians(ang); t = (u-2.5)*math.cos(a) + v*math.sin(a)
            return 'a' if t > 5.4 else 'm'
def T08(u, v):  # 복제 표식포 (ㄷ자 문틀)
    if rect(u, v, 2, 8.5, -3.6, 3.6) and not rect(u, v, 4.2, 9, -1.8, 1.8):
        return 'a' if u > 7.4 else 'm'
    if rect(u, v, 5.2, 7, -0.8, 0.8): return 'a'
def T09(u, v):  # 귀환 부메랑포 (반달)
    if circ(u, v, 3.2, 0, 4.6) and u >= 2 and not circ(u, v, 7.4, 0, 3.4):
        return 'a' if abs(v) > 3.2 else 'm'
def T10(u, v):  # 잔류 톱날포
    d = math.hypot(u-5, v); ang = math.atan2(v, u-5)
    r = 3.0 + 1.3*(((ang+math.pi)/(math.pi/4)) % 1.0)
    if d <= r:
        if d <= 1.3: return 'a'
        if d <= 2.2: return 'd'
        return 'm'
    if rect(u, v, 1.5, 3, -1.6, 1.6): return 'm'
def T11(u, v):  # 충격 파동포 (평판 + 이중 호)
    d = math.hypot(u+3.5, v)
    if 5.6 <= d <= 7.0 and abs(v) <= 3.6 and u > 1.5: return 'm'
    if 8.0 <= d <= 9.0 and abs(v) <= 4.8 and u > 1.5: return 'a'
    if rect(u, v, 1.5, 3.5, -1.2, 1.2): return 'm'
def T12(u, v):  # 십자 분열포
    if rect(u, v, 2, 8.2, -3.1, 3.1):
        if (rect(u, v, 4.6, 5.6, -2.3, 2.3) or rect(u, v, 2.8, 7.4, -0.5, 0.5)): return 'd'
        if u > 7.4: return 'a'
        return 'm'
def T13(u, v):  # 감속 그물포 (양끝 갈고리)
    if rect(u, v, 2, 6, -1.0, 1.0): return 'm'
    if rect(u, v, 5.6, 7, -4.4, 4.4): return 'm'
    if rect(u, v, 7, 9.2, -4.4, -3.0) or rect(u, v, 7, 9.2, 3.0, 4.4): return 'a' if u > 8.2 else 'm'
    if rect(u, v, 8.4, 9.2, -3.0, -2.0) or rect(u, v, 8.4, 9.2, 2.0, 3.0): return 'a'
def T14(u, v):  # 과충전 표식포 (사각 전극)
    if rect(u, v, 2, 7.6, -3.2, 3.2):
        if rect(u, v, 3.8, 6.2, -1.7, 1.7): return 'a' if rect(u, v, 4.6, 5.4, -0.6, 0.6) else 'd'
        if u > 6.8: return 'l'
        return 'm'
def T15(u, v):  # 봉합 포말포 (거품)
    for (cu, cv, r, t) in ((8.4, 0.4, 1.3, 'a'), (6.5, -2.2, 1.8, 'l'), (6.9, 2.1, 1.5, 'a'), (5.6, 0, 1.4, 'l'), (3.6, 0, 2.6, 'm')):
        if circ(u, v, cu, cv, r): return t
def T16(u, v):  # 연쇄 도체포 (벌어진 두 전극)
    if circ(u, v, 3, 0, 2.2): return 'm'
    for ang in (-20, 20):
        a = math.radians(ang)
        tipu, tipv = 2.5 + 6.2*math.cos(a), 6.2*math.sin(a)
        if circ(u, v, tipu, tipv, 1.2): return 'a'
        if seg(u, v, ang, 6.2, 1.5, 2.5): return 'm'

TURRETS = {
    'ROT-01': ('쌍열 연사포', T01), 'ROT-02': ('부채 산탄포', T02), 'ROT-03': ('부착 드릴포', T03),
    'ROT-04': ('관통 원반포', T04), 'ROT-05': ('폭발 구체포', T05), 'ROT-06': ('냉각 분사포', T06),
    'ROT-07': ('부식 분무포', T07), 'ROT-08': ('복제 표식포', T08), 'ROT-09': ('귀환 부메랑포', T09),
    'ROT-10': ('잔류 톱날포', T10), 'ROT-11': ('충격 파동포', T11), 'ROT-12': ('십자 분열포', T12),
    'ROT-13': ('감속 그물포', T13), 'ROT-14': ('과충전 표식포', T14), 'ROT-15': ('봉합 포말포', T15),
    'ROT-16': ('연쇄 도체포', T16),
}

# ---------------- hulls (x,y relative to centre, y down; slot 0 points up) ----------------
def polar(x, y):
    r = math.hypot(x, y); a = math.atan2(x, -y)  # 0 = up, clockwise
    return r, a
def slot_phase(a):  # angular distance to nearest slot, radians
    s = 2*math.pi/5
    return abs(((a + s/2) % s) - s/2)

def H_disc(x, y):   # A: 판석 원반
    r, a = polar(x, y)
    if r > 16.2: return None
    if r <= 4.2: return 'core' if r <= 3.0 else 'dark'
    if 4.2 < r <= 6.4: return 'sub'
    if abs(r - 12.0) <= 0.55: return 'dark'
    off = slot_phase(a + math.pi/5)
    if off*r <= 0.7 and r > 6.4: return 'dark'
    return 'base' if r < 12 else 'sub'

def H_penta(x, y):  # B: 오각 요새
    r, a = polar(x, y)
    s = 2*math.pi/5
    k = math.cos(s/2) / math.cos(slot_phase(a))   # pentagon radius factor, vertices at slots
    R = 16.6 * min(k, 1.0) if False else 15.8 * (math.cos(s/2)/math.cos(((a + s/2) % s) - s/2))
    R = min(R, 15.6)  # chamfer vertices
    if r > R: return None
    if r <= 5.6:
        if r <= 2.6: return 'core'
        if r <= 3.6: return 'dark'
        return 'sub' if int((a+math.pi)/(math.pi/5)) % 2 else 'base'
    if abs(r - 7.0) <= 0.55: return 'dark'
    if r > R - 3.4: return 'sub'
    return 'base'

def H_star(x, y):   # C: 별 거북
    r, a = polar(x, y)
    ph = slot_phase(a)
    R = 9.8 + 7.4*max(0, 1 - ph/(math.pi/5))**1.3
    if r > R: return None
    if r <= 5.2: return 'core' if r <= 3.4 else 'dark'
    if r <= 7.6: return 'base'
    if ph*r <= 1.1 and r < R - 2.2: return 'sub'
    if r > R - 1.6: return 'sub'
    return 'base'

def H_gear(x, y):   # D: 톱니 원반 (5개 함선 포대 홈)
    r, a = polar(x, y)
    ph = slot_phase(a)
    teeth = 15.4 + (1.2 if (int((a % (2*math.pi/5)) / (2*math.pi/25)) % 2 == 1) else 0)
    R = teeth
    if ph*r < 4.4: R = 13.2  # turret bay recess
    if r > R: return None
    if r <= 6.0:
        if r <= 2.4: return 'core'
        if abs(x) <= 0.6 or abs(y) <= 0.6: return 'dark'
        return 'sub' if r <= 4.4 else 'dark'
    if r > 12.6 and ph*r >= 4.4: return 'sub'
    if abs(r - 9.4) <= 0.5: return 'dark'
    return 'base'

def H_petal(x, y):  # E: 꽃잎 포드
    r, a = polar(x, y)
    inside = r <= 8.2
    lobe = None
    for k in range(5):
        ang = k*2*math.pi/5
        cx, cy = 9.2*math.sin(ang), -9.2*math.cos(ang)
        d = math.hypot(x-cx, y-cy)
        if d <= 6.6: inside = True; lobe = d if lobe is None else min(lobe, d)
    if not inside: return None
    if r <= 5.0: return 'core' if r <= 3.0 else 'dark'
    if r <= 6.4: return 'sub'
    if lobe is not None and lobe <= 2.2: return 'sub'
    return 'base'

HULLS = [
    dict(key='A', name='판석 원반', desc='넓은 원형 장갑판 + 5분할 이음새', fn=H_disc, mount=14.6,
         pal=dict(base='#CFC5AE', sub='#AEA48E', light='#EAE2CE', shadow='#8E846F', dark='#3B3A50', core='#D7A15E', corel='#F1CF92'),
         tpal=dict(m='#5F6580', l='#8A90A8', d='#2F3348', a='#E3B46C'),
         turrets=['ROT-01', 'ROT-02', 'ROT-03', 'ROT-06', 'ROT-05']),
    dict(key='B', name='오각 요새', desc='오각 장갑 + 꼭짓점 포대, 묵직한 성채형', fn=H_penta, mount=13.2,
         pal=dict(base='#626B86', sub='#4E566F', light='#8E96AE', shadow='#3E455C', dark='#262A40', core='#E2B36C', corel='#F6DBA6'),
         tpal=dict(m='#B9AE97', l='#DDD3BC', d='#4A4A5A', a='#E8B66A'),
         turrets=['ROT-04', 'ROT-07', 'ROT-09', 'ROT-11', 'ROT-16']),
    dict(key='C', name='별 거북', desc='5방향 팔 끝에 포탑, 회전 방향이 잘 읽힘', fn=H_star, mount=14.8,
         pal=dict(base='#73A096', sub='#5A857C', light='#A6C9BD', shadow='#4D7069', dark='#24343A', core='#EADAA8', corel='#FBF1D2'),
         tpal=dict(m='#4E5670', l='#7C849C', d='#262A3E', a='#F0D58E'),
         turrets=['ROT-08', 'ROT-10', 'ROT-12', 'ROT-13', 'ROT-14']),
    dict(key='D', name='톱니 원반', desc='톱니 테두리 + 움푹한 포대 홈 5개', fn=H_gear, mount=12.6,
         pal=dict(base='#B48570', sub='#946A58', light='#D6AE93', shadow='#7A5546', dark='#38283A', core='#92CFC6', corel='#CDEDE6'),
         tpal=dict(m='#54596F', l='#80869C', d='#2A2C3C', a='#9AD6CC'),
         turrets=['ROT-15', 'ROT-01', 'ROT-03', 'ROT-07', 'ROT-05']),
    dict(key='E', name='꽃잎 포드', desc='5개 둥근 포드가 맞물린 부드러운 실루엣', fn=H_petal, mount=13.8,
         pal=dict(base='#CAD5CE', sub='#A6B4AC', light='#EFF4EF', shadow='#8B9993', dark='#33384A', core='#D58D8A', corel='#F1C2B8'),
         tpal=dict(m='#5B6079', l='#878DA6', d='#2C3044', a='#E39A8F'),
         turrets=['ROT-02', 'ROT-06', 'ROT-04', 'ROT-12', 'ROT-16']),
]

# ---------------- rasteriser ----------------
def render_ship(h, n=N):
    pal = {k: hx(v) for k, v in h['pal'].items()}
    tpal = {k: hx(v) for k, v in h['tpal'].items()}
    lab = [[None]*n for _ in range(n)]
    col = [[None]*n for _ in range(n)]
    c = (n-1)/2
    # hull
    hull = [[h['fn']((x-c)/SCALE, (y-c)/SCALE) for x in range(n)] for y in range(n)]
    ins = lambda x, y: 0 <= x < n and 0 <= y < n and hull[y][x] is not None
    for y in range(n):
        for x in range(n):
            t = hull[y][x]
            if t is None: continue
            lab[y][x] = 'H'
            if t == 'core':
                col[y][x] = pal['corel'] if (x-c+y-c) < -1.5 else pal['core']
            elif t in ('base', 'sub'):
                if not ins(x-1, y-1) or not ins(x-1, y) and not ins(x, y-1): col[y][x] = pal['light']
                elif not ins(x+1, y+1): col[y][x] = pal['shadow']
                else: col[y][x] = pal[t]
            else:
                col[y][x] = pal[t]
    # turrets
    for k, tid in enumerate(h['turrets']):
        fn = TURRETS[tid][1]
        ang = k*2*math.pi/5
        dx, dy = math.sin(ang), -math.cos(ang)
        mx, my = c + dx*h['mount']*SCALE, c + dy*h['mount']*SCALE
        tmask = {}
        for y in range(n):
            for x in range(n):
                px, py = (x-mx)/SCALE, (y-my)/SCALE
                u = px*dx + py*dy; v = px*dy - py*dx  # v: perpendicular
                t = None
                if u*u + v*v <= 2.9**2:
                    t = 'base'
                    if u*u + v*v <= 1.1**2: t = 'd'
                else:
                    t = fn(u, v)
                if t: tmask[(x, y)] = t
        for (x, y), t in tmask.items():
            lab[y][x] = 'T%d' % k
            if t == 'base':
                if (x-1, y-1) not in tmask: col[y][x] = tpal['l']
                else: col[y][x] = tpal['m']
            else:
                col[y][x] = tpal[t]
    # outlines: outside silhouette + turret/hull seam
    img = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    for y in range(n):
        for x in range(n):
            if col[y][x] is not None:
                l = lab[y][x]
                seam = False
                if l.startswith('T'):
                    for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        xx, yy = x+ox, y+oy
                        if 0 <= xx < n and 0 <= yy < n and lab[yy][xx] is not None and lab[yy][xx] != l:
                            seam = True
                img.putpixel((x, y), OUTLINE if seam else col[y][x])
            else:
                for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    xx, yy = x+ox, y+oy
                    if 0 <= xx < n and 0 <= yy < n and col[yy][xx] is not None:
                        img.putpixel((x, y), OUTLINE); break
    return img

def render_turret(tid, tpal_hex, n=34, sc=1.7):
    tpal = {k: hx(v) for k, v in tpal_hex.items()}
    img = Image.new('RGBA', (n, n), (0, 0, 0, 0)); fn = TURRETS[tid][1]
    mx, my = (n-1)/2, n*0.68
    filled = {}
    for y in range(n):
        for x in range(n):
            u = (my - y)/sc; v = (x - mx)/sc
            if u*u + v*v <= 2.9**2: t = 'd' if u*u+v*v <= 1.1**2 else ('l' if u > 0.8 and v < 0 else 'm')
            else: t = fn(u, v)
            if t: filled[(x, y)] = tpal[t]
    for (x, y), cc in filled.items(): img.putpixel((x, y), cc)
    for y in range(n):
        for x in range(n):
            if (x, y) in filled: continue
            if any((x+ox, y+oy) in filled for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                img.putpixel((x, y), OUTLINE)
    return img

def shadow_of(sprite, alpha=70):
    sh = Image.new('RGBA', sprite.size, (0, 0, 0, 0))
    a = sprite.split()[3]
    sh.paste((20, 24, 60, alpha), mask=a)
    return sh

def font(sz, bold=False):
    p = '/usr/share/fonts/opentype/noto/NotoSansCJK-Bold.ttc' if bold else '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
    if not os.path.exists(p): p = '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
    return ImageFont.truetype(p, sz, index=1)

ships = []
for h in HULLS:
    s = render_ship(h)
    s.save(f"{OUT}/ship_{h['key']}_logical.png")
    s.resize((N*8, N*8), Image.NEAREST).save(f"{OUT}/ship_{h['key']}_x8.png")
    ships.append(s)

# ---- sheet 1: 5 drafts on background ----
SC = 4; cell = 400
lane = BG.crop((560, 0, 1370, 1080))
W = cell*5 + 40*6; H = 120 + cell + 260
sheet = Image.new('RGB', (W, H), (36, 40, 66))
d = ImageDraw.Draw(sheet)
d.text((40, 30), '플레이어 비행체 초안 5종 · 회전 포탑 5슬롯', font=font(40, True), fill=(236, 232, 220))
for i, (h, s) in enumerate(zip(HULLS, ships)):
    x0 = 40 + i*(cell+40); y0 = 110
    tile = lane.crop((0, 200, 810, 1010)).resize((cell, cell), Image.NEAREST)
    sheet.paste(tile, (x0, y0))
    big = s.resize((N*SC, N*SC), Image.NEAREST)
    ox = x0 + (cell - N*SC)//2; oy = y0 + (cell - N*SC)//2
    sheet.paste(shadow_of(big), (ox+10, oy+14), shadow_of(big))
    sheet.paste(big, (ox, oy), big)
    ty = y0 + cell + 18
    d.text((x0, ty), f"{h['key']}. {h['name']}", font=font(32, True), fill=(240, 236, 224))
    d.text((x0, ty+48), h['desc'], font=font(19), fill=(190, 194, 214))
    for j, tid in enumerate(h['turrets']):
        d.text((x0, ty+86+j*30), f"{['①','②','③','④','⑤'][j]} {tid} {TURRETS[tid][0]}", font=font(20), fill=(214, 216, 232))
sheet.save(f"{OUT}/01_비행체_초안5종.png")

# ---- sheet 2: in-game scale preview on full background ----
pre = BG.copy().convert('RGBA')
pos = [(700, 200), (1050, 200), (875, 470), (700, 740), (1050, 740)]
for (cx, cy), s in zip(pos, ships):
    big = s.resize((N*2, N*2), Image.NEAREST)
    o = (cx - N, cy - N)
    pre.alpha_composite(shadow_of(big), (o[0]+6, o[1]+9)); pre.alpha_composite(big, o)
pd = ImageDraw.Draw(pre)
for (cx, cy), h in zip(pos, HULLS):
    pd.text((cx-14, cy+90), h['key'], font=font(30, True), fill=(26, 29, 53))
pre.convert('RGB').save(f"{OUT}/02_인게임_배율_미리보기.png")

# ---- sheet 3: 16 turret catalog ----
TS = 7; tn = 34; cw = 300; chh = 330
cat = Image.new('RGB', (cw*4 + 80, 120 + chh*4), (36, 40, 66))
cd = ImageDraw.Draw(cat)
cd.text((40, 30), '포탑 16종 실루엣 · 회전포탑_변형증강.xlsx 기준', font=font(40, True), fill=(236, 232, 220))
tp = dict(m='#5F6580', l='#8A90A8', d='#2F3348', a='#E3B46C')
for i, tid in enumerate(TURRETS):
    r, cidx = divmod(i, 4)
    x0 = 40 + cidx*cw; y0 = 110 + r*chh
    cd.rectangle((x0+10, y0, x0+cw-10, y0+250), fill=(108, 116, 178))
    t = render_turret(tid, tp, tn).resize((tn*TS, tn*TS), Image.NEAREST)
    cat.paste(t, (x0 + (cw-tn*TS)//2, y0+5), t)
    cd.text((x0+14, y0+258), f"{tid} {TURRETS[tid][0]}", font=font(22, True), fill=(236, 232, 220))
cat.save(f"{OUT}/03_포탑16종_실루엣.png")
print('ok')
