# Будує геометрію трас в офіційній орієнтації F1 з даних хронометражу (TracingInsights / FastF1):
# контур траси, номери поворотів, межі секторів, профіль швидкості/передач/обертів/газу/гальма.
# Запуск: python3 -I gen_official.py <папка dl> <tracks.json> <picked.json>
import json, math, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from events import EVENTS

DL, TRACKS, PICKED = sys.argv[1], sys.argv[2], sys.argv[3]
tracks = json.load(open(TRACKS))
picked = json.load(open(PICKED))
LONG, PAD, NPROF = 300.0, 30.0, 240
AUTO_TURNS = {'my-1999': 15, 'es-2026': 22}      # для нових трас немає офіційних даних про повороти

def load(p):
    return json.load(open(p))

def find_corners(repo, ev):
    for ses in ['Qualifying', 'Race', 'Practice 3', 'Practice 2', 'Practice 1', 'Sprint', 'Sprint Qualifying']:
        for name in ['corners.json', 'cor.json']:
            p = os.path.join(repo, ev, ses, name)
            if os.path.exists(p):
                c = load(p)
                if c.get('CornerNumber'):
                    return c
    return None

def cum(pts, closed):
    loop = pts + [pts[0]] if closed else pts
    s = [0.0]
    for i in range(len(loop) - 1):
        s.append(s[-1] + math.dist(loop[i], loop[i + 1]))
    return loop, s

def resample(pts, n, closed=True):
    loop, s = cum(pts, closed)
    tot = s[-1]; out = []; j = 0
    for k in range(n):
        d = tot * k / n
        while s[j + 1] < d: j += 1
        t = (d - s[j]) / ((s[j + 1] - s[j]) or 1e-9)
        a, b = loop[j], loop[j + 1]
        out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
    return out

def project(pts, q, closed=True):
    """Частка периметра (0..1) найближчої точки ламаної до q."""
    loop, s = cum(pts, closed); tot = s[-1]; best = (1e18, 0)
    for i in range(len(loop) - 1):
        a, b = loop[i], loop[i + 1]; dx, dy = b[0] - a[0], b[1] - a[1]; L2 = dx * dx + dy * dy or 1e-9
        t = max(0, min(1, ((q[0] - a[0]) * dx + (q[1] - a[1]) * dy) / L2))
        d = (a[0] + dx * t - q[0]) ** 2 + (a[1] + dy * t - q[1]) ** 2
        if d < best[0]: best = (d, (s[i] + (s[i + 1] - s[i]) * t) / tot)
    return best[1]

def rdp(pts, eps):
    if len(pts) < 3: return pts
    a, b = pts[0], pts[-1]; dx, dy = b[0] - a[0], b[1] - a[1]; n = math.hypot(dx, dy) or 1e-9
    dm, ix = 0, 0
    for i in range(1, len(pts) - 1):
        p = pts[i]; d = abs(dy * p[0] - dx * p[1] + b[0] * a[1] - b[1] * a[0]) / n
        if d > dm: dm, ix = d, i
    return rdp(pts[:ix + 1], eps)[:-1] + rdp(pts[ix:], eps) if dm > eps else [a, b]

def inside(pt, poly):
    x, y = pt; c = False; j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]; xj, yj = poly[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi: c = not c
        j = i
    return c

def interp(xs, ys, x):
    if x <= xs[0]: return ys[0]
    if x >= xs[-1]: return ys[-1]
    lo, hi = 0, len(xs) - 1
    while hi - lo > 1:
        m = (lo + hi) // 2
        if xs[m] <= x: lo = m
        else: hi = m
    t = (x - xs[lo]) / ((xs[hi] - xs[lo]) or 1e-9)
    return ys[lo] + (ys[hi] - ys[lo]) * t

def auto_corners(line, k):
    """Пошук k поворотів за кривиною (для трас без офіційних даних)."""
    n = len(line); step = 1.0 / n
    head = []
    for i in range(n):
        a, b = line[i - 1], line[(i + 1) % n]
        head.append(math.atan2(b[1] - a[1], b[0] - a[0]))
    turn = []
    for i in range(n):
        d = head[(i + 1) % n] - head[i]
        d = (d + math.pi) % (2 * math.pi) - math.pi
        turn.append(d)
    w = max(2, n // 90)
    score = [abs(sum(turn[(i + j) % n] for j in range(-w, w + 1))) for i in range(n)]
    order = sorted(range(n), key=lambda i: -score[i])
    chosen = []
    sep = max(3, int(n * 0.012))
    for i in order:
        if score[i] < 0.12: break
        if all(min(abs(i - c), n - abs(i - c)) > sep for c in chosen):
            chosen.append(i)
        if len(chosen) == k: break
    chosen.sort()
    out = []
    for num, i in enumerate(chosen, 1):
        sgn = 1 if sum(turn[(i + j) % n] for j in range(-w, w + 1)) > 0 else -1
        out.append((num, str(num), i * step, sgn))
    return out

result_rows = []
for t in tracks:
    tid = t['id']; yr, ev = EVENTS[tid]
    repo = os.path.join(DL, 'ti' + yr)
    def valid(c):
        # дані придатні, якщо позиції без пропусків і стрибків, а швидкість на початку і в кінці кола збігається
        n = len(c['x'])
        if n < 300 or any(not isinstance(c[k][i], (int, float)) for k in ('x', 'y', 'speed', 'distance') for i in range(n)):
            return False
        steps = [math.dist((c['x'][i], c['y'][i]), (c['x'][i + 1], c['y'][i + 1])) / 10 for i in range(n - 1)]
        path = sum(steps)
        if max(steps) > 120 or abs(path - c['distance'][-1]) > 0.06 * c['distance'][-1]:
            return False
        frozen = max_run = 0
        for st in steps:
            frozen = frozen + 1 if st < 0.5 else 0; max_run = max(max_run, frozen)
        return max_run < 6 and abs(c['speed'][0] - c['speed'][-1]) < 30
    tel = None
    for lap_time, tel_path, ses in picked[tid][2]:
        cand = load(os.path.join(repo, tel_path))['tel']
        if valid(cand):
            tel = cand; break
    assert tel is not None, tid
    # прибираємо відліки з порожніми значеннями ('None')
    keys = ['time', 'x', 'y', 'distance', 'speed', 'gear', 'rpm', 'throttle', 'brake']
    def num(v): return isinstance(v, (int, float)) and not isinstance(v, bool) or isinstance(v, bool)
    good = [i for i in range(len(tel['time'])) if all(isinstance(tel[k][i], (int, float)) for k in keys)]
    tel = {k: [tel[k][i] for i in good] for k in keys}
    cor = find_corners(repo, ev)

    tx, ty, tdist = tel['x'], tel['y'], tel['distance']
    lap_m = tdist[-1]
    # Офіційна орієнтація: поворот на кут Rotation (як у FastF1), вісь y — донизу (екран).
    rot = cor.get('Rotation') if cor else None
    if rot is None:
        rot = None
    def rot_pt(x, y, r):
        c, s = math.cos(math.radians(r)), math.sin(math.radians(r))
        return (x * c - y * s, x * s + y * c)

    sf_raw = (tx[0], ty[0])
    if cor and cor.get('TrackX'):
        raw = list(zip(cor['TrackX'], cor['TrackY']))
        if raw[0] == raw[-1]: raw = raw[:-1]
        # Початок контуру — лінія старту/фінішу, напрямок — як рух боліда.
        f0 = project(raw, sf_raw); fa = project(raw, (interp(tdist, tx, 300), interp(tdist, ty, 300)))
        dense = resample(raw, 2000)
        k0 = int(round(f0 * 2000)) % 2000
        dense = dense[k0:] + dense[:k0]
        if ((fa - f0) % 1) > 0.5: dense = [dense[0]] + dense[1:][::-1]
        line_raw = dense
    else:
        line_raw = [(x, y) for x, y in zip(tx, ty)]
        # прибираємо дублікати і замикаємо контур
        clean = [line_raw[0]]
        for p in line_raw[1:]:
            if math.dist(p, clean[-1]) > 5: clean.append(p)
        line_raw = resample(clean, 2000)

    if rot is None:
        # Немає офіційного кута — вирівнюємо за GeoJSON-схемою, де північ угорі (стара схема у tracks.json).
        old = [tuple(p) for p in t['layout']['points']]
        A = resample(old, 180)
        best = (1e18, 0)
        for r in range(0, 360, 1):
            R = [rot_pt(x, y, r) for x, y in line_raw]
            R = [(x, -y) for x, y in R]
            B = resample(R, 180)
            def norm(P):
                xs = [p[0] for p in P]; ys = [p[1] for p in P]; cx = sum(xs) / len(xs); cy = sum(ys) / len(ys)
                sc = max(max(xs) - min(xs), max(ys) - min(ys)) or 1
                return [((x - cx) / sc, (y - cy) / sc) for x, y in P]
            a, b = norm(A), norm(B)
            # контури можуть мати різні точки старту — шукаємо найкращий зсув
            m = min(sum(math.dist(a[i], b[(i + sh) % 180]) for i in range(0, 180, 3)) for sh in range(0, 180, 3))
            if m < best[0]: best = (m, r)
        rot = best[1]; orient = 'north'
    else:
        orient = 'official'

    R = [rot_pt(x, y, rot) for x, y in line_raw]
    R = [(x, -y) for x, y in R]
    xs = [p[0] for p in R]; ys = [p[1] for p in R]
    sc = LONG / max(max(xs) - min(xs), max(ys) - min(ys))
    W = round((max(xs) - min(xs)) * sc + 2 * PAD, 1); H = round((max(ys) - min(ys)) * sc + 2 * PAD, 1)
    def to_map(x, y):
        x, y = rot_pt(x, y, rot); y = -y
        return ((x - min(xs)) * sc + PAD, (y - min(ys)) * sc + PAD)
    P = [((x - min(xs)) * sc + PAD, (y - min(ys)) * sc + PAD) for x, y in R]
    P = resample(P, 720)

    # --- профіль швидкості: кожен відлік телеметрії проєктуємо на контур ---
    tf = []; prev = None
    for i in range(len(tx)):
        f = project(P, to_map(tx[i], ty[i]))
        if prev is None:
            v = f - 1 if f > 0.5 else f
        else:
            # обираємо «розгортку» (f-1, f, f+1 + ціле число кіл), найближчу до попередньої точки
            base = math.floor(prev)
            v = min((f + base + k for k in (-1, 0, 1)), key=lambda c: abs(c - prev))
            if v < prev: v = prev + 1e-6
        tf.append(v); prev = v
    span_f = tf[-1] - tf[0]
    tf = [(f - tf[0]) / span_f for f in tf]          # 0..1 уздовж контуру
    def prof(key, scale=1, nd=0):
        vals = [float(v) for v in tel[key]]
        out = []
        for k in range(NPROF):
            f = (k + 0.5) / NPROF
            v = interp(tf, vals, f) * scale
            out.append(round(v, nd) if nd else int(round(v)))
        return out
    profile = {
        'lapSeconds': round(lap_time, 3), 'lapMeters': round(lap_m), 'source': f"{yr} {ev}, {ses}, {tel_path.split('/')[2]}, коло {tel_path.split('/')[3].split('_')[0]}",
        'speed': prof('speed'), 'gear': prof('gear'), 'rpm': prof('rpm'), 'throttle': prof('throttle'),
        'brake': [1 if v >= 0.5 else 0 for v in prof('brake', nd=2)],
    }

    # --- сектори: момент перетину меж секторів з таблиці часів ---
    sectors = []
    sl = load(os.path.join(repo, ev, ses, 'session_laptimes.json'))
    drv = tel_path.split('/')[2]; lapn = int(tel_path.split('/')[3].split('_')[0])
    for i in range(len(sl['time'])):
        if sl['drv'][i] == drv and int(sl['lap'][i]) == lapn:
            s1, s2 = sl['s1'][i], sl['s2'][i]
            if isinstance(s1, (int, float)) and isinstance(s2, (int, float)):
                for tt in (s1, s1 + s2):
                    sectors.append(round(interp(tel['time'], tf, tt), 4))
    if len(sectors) != 2: sectors = [0.333, 0.667]

    # --- повороти ---
    turns = []
    if cor and tid not in AUTO_TURNS:
        labels = cor.get('CornerLabel') or [str(n) for n in cor['CornerNumber']]
        for i, num in enumerate(cor['CornerNumber']):
            cx, cy = to_map(cor['X'][i], cor['Y'][i])
            f = project(P, (cx, cy))
            a = math.radians(cor['Angle'][i] + rot)
            d = (math.cos(a), -math.sin(a))
            turns.append({'number': int(num), 'label': str(labels[i]), 'fraction': round(f, 4), 'dir': d})
    else:
        for num, lab, f, sgn in auto_corners(P, AUTO_TURNS.get(tid, 14)):
            i = int(f * len(P)) % len(P); a, b = P[i - 2], P[(i + 2) % len(P)]
            tx_, ty_ = b[0] - a[0], b[1] - a[1]; L = math.hypot(tx_, ty_) or 1
            # мітка — із зовнішнього боку повороту
            n = (-ty_ / L, tx_ / L) if sgn < 0 else (ty_ / L, -tx_ / L)
            turns.append({'number': num, 'label': lab, 'fraction': round(f, 4), 'dir': n})

    def point_at(f):
        f %= 1; i = f * len(P); j = int(i); k = i - j; a, b = P[j % len(P)], P[(j + 1) % len(P)]
        return (a[0] + (b[0] - a[0]) * k, a[1] + (b[1] - a[1]) * k)
    for tr in turns:
        p = point_at(tr['fraction']); d = tr.pop('dir')
        tr['x'], tr['y'] = round(p[0], 1), round(p[1], 1)
        tr['labelX'], tr['labelY'] = round(p[0] + d[0] * 12, 1), round(p[1] + d[1] * 12, 1)

    # --- піт-лейн (схематично, як раніше) ---
    loop, seg_s = cum(P, True); Ltot = seg_s[-1]
    def at(sv):
        return point_at(sv / Ltot)
    def tan(sv):
        a, b = at(sv - 4), at(sv + 4); d = math.hypot(b[0] - a[0], b[1] - a[1]) or 1; return ((b[0] - a[0]) / d, (b[1] - a[1]) / d)
    def nrm(sv):
        tt = tan(sv); return (-tt[1], tt[0])
    p0 = at(0); n0 = nrm(0)
    side = 1 if inside((p0[0] + n0[0] * 8, p0[1] + n0[1] * 8), P) else -1
    E, X, Rr, OFF = 60, 48, 14, 6.5
    lane = []; sv = -E - Rr
    while sv <= X + Rr + 0.01:
        k = 1.0
        if sv < -E: u = (sv + E + Rr) / Rr; k = u * u * (3 - 2 * u)
        if sv > X: u = (X + Rr - sv) / Rr; k = u * u * (3 - 2 * u)
        p = at(sv); n = nrm(sv); lane.append((p[0] + n[0] * OFF * k * side, p[1] + n[1] * OFF * k * side)); sv += 2.5
    lane = rdp(lane, 0.15)
    trackS = rdp(P, 0.25)

    # --- підпис «Старт / фініш»: подалі від траси, піт-лейну і номерів поворотів ---
    sfa = (p0[0] - n0[0] * 6, p0[1] - n0[1] * 6); sfb = (p0[0] + n0[0] * 6, p0[1] + n0[1] * 6)
    dense = [at(i * 2) for i in range(int(Ltot / 2))] + lane + [(tr['labelX'], tr['labelY']) for tr in turns]
    def place(anchor, w):
        cands = []
        for rad in range(10, 130, 3):
            for a in range(0, 360, 10):
                x = anchor[0] + math.cos(math.radians(a)) * rad; y = anchor[1] + math.sin(math.radians(a)) * rad + 4
                if x - w / 2 < 3 or x + w / 2 > W - 3 or y < 12 or y > H - 3: continue
                pts = [(xx, yy) for xx in [x - w / 2 + i * (w / 8) for i in range(9)] for yy in (y - 9, y - 4.5, y)]
                dm = min(min(math.hypot(px - xx, py - yy) for px, py in dense) for xx, yy in pts)
                cands.append((0 if dm >= 8 else 1, rad if dm >= 8 else -dm, x, y, rad))
        cands.sort(); cc = cands[0]; return cc[2], cc[3], cc[4]
    sx, sy, srad = place(p0, 74)

    entry = project(trackS, lane[0]); exit_ = project(trackS, lane[-1]); span = (exit_ - entry) % 1
    # Типовий гоночний темп: найшвидше коло гонки 2026 (або 2025) + 1.5 % (паливо, знос шин).
    race_times = [c[0] for c in picked[tid][2] if c[2] == 'Race']
    quali_times = [c[0] for c in picked[tid][2] if c[2] == 'Qualifying']
    race_best = min(race_times) if race_times else min(quali_times) * 1.05
    typical = round(race_best * 1.015, 1)
    t['typicalLapSeconds'] = typical
    pit_loss = cor.get('PitLossNormal') if cor else None
    t['pitLaneSeconds'] = round(span * typical + (pit_loss - 2.75 if pit_loss else 19.5), 1)
    t['layout'] = {
        'points': [[round(x, 1), round(y, 1)] for x, y in trackS],
        'pitLane': [[round(x, 1), round(y, 1)] for x, y in lane],
        'pitEntry': round(entry, 5), 'pitExit': round(exit_, 5),
        'startFinish': [round(v, 1) for v in (*sfa, *sfb)],
        'labelPosition': [round(sx, 1), round(sy, 1)], 'labelLeader': srad > 40,
        'width': W, 'height': H, 'orientation': orient, 'rotation': rot,
        'sectors': sectors, 'turns': turns,
    }
    t['profile'] = profile
    result_rows.append((tid, orient, rot, len(turns), round(lap_m), typical, t['pitLaneSeconds'], sectors, W, H))
    print(tid, orient, rot, len(turns), round(lap_m), typical, t['pitLaneSeconds'], sectors, W, H, file=sys.stderr)

json.dump(tracks, open(TRACKS, 'w'), ensure_ascii=False, indent=1)
