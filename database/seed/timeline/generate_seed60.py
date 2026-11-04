#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
generate_seed60.py - Sinh bộ T-SQL seed "60 ngày" cho VeggieLife (SQL Server).

  python3 generate_seed60.py                 # xuất ra ./out/
  python3 generate_seed60.py --media-map ../../../../Data/_manifest/cloudinary_map.json

Nguyên tắc:
  * ID dải riêng 10001+ cho mọi bảng; email @seed60.veggielife.vn; chạy lại nhiều lần không lỗi (xóa theo dải ID).
  * Dữ liệu master (DietarySchools, Allergens, FoodCategories, Ingredients, SubscriptionPlans, ENumberDictionary)
    được tra theo TÊN/CODE ở runtime -> không phụ thuộc ID của dữ liệu cũ (1-105, 401-499 giữ nguyên).
  * Thời gian: @Now = GETUTCDATE(); mọi mốc = DATEADD(MINUTE,-M,@Now) với M <= 60 ngày.
  * Deterministic: random seed cố định (--seed).
Chỉ dùng thư viện chuẩn Python 3.8+.
"""
import argparse, datetime, hashlib, json, math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from seed60_data import *
from seed60_book import BOOK, BOOK_K, BOOK_SUPP, IMG_BY_KEY, C_MAP
K.update(BOOK_K); SUPP.update(BOOK_SUPP)

ap = argparse.ArgumentParser()
ap.add_argument("--seed", type=int, default=60)
ap.add_argument("--out", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "out"))
ap.add_argument("--media-map", default=None, help="Data/_manifest/cloudinary_map.json (nếu có -> dùng URL thật)")
ap.add_argument("--cloud", default="swrsuenp", help="Cloudinary cloud name dùng cho URL placeholder")
ARGS = ap.parse_args()
rng = random.Random(ARGS.seed)
HERE = os.path.dirname(os.path.abspath(__file__))
SEED_DIR = os.path.normpath(os.path.join(HERE, ".."))

BASE = 10001
DAY = 1440
MAXM = 60 * DAY - 30          # mốc xa nhất (còn nằm trong 60 ngày)
HASH = "$2a$11$GBRajsAbFfn4G4UI7bhu.O8MGs4mXfn/clcozGAZ0npIHYrlShxwO"   # bcrypt dùng lại từ seed cũ
EMAIL_DOMAIN = "seed60.veggielife.vn"
TODAY = datetime.date.today()

# ------------------------------------------------------------------ helpers
def q(s): return "N'" + str(s).replace("'", "''") + "'"
def v(s): return "'" + str(s).replace("'", "''") + "'"
def nn(x): return "NULL" if x is None else str(x)
def bit(b): return "1" if b else "0"
def ts(m): return "NULL" if m is None else "DATEADD(MINUTE,-%d,@Now)" % int(m)
def tsf(m): return "DATEADD(MINUTE,%d,@Now)" % int(m)
def dts(days_ago): return "DATEADD(DAY,%d,CAST(@Now AS DATE))" % (-int(days_ago))
def js(o): return q(json.dumps(o, ensure_ascii=False, separators=(",", ":")))
def clamp(x, lo, hi): return max(lo, min(hi, x))
def r2(x): return int(math.floor(x * 10 + 0.5)) / 10.0
def rnd_between(lo, hi):
    lo, hi = int(lo), int(hi)
    if hi < lo: hi = lo
    return rng.randint(lo, hi)

def sample_m(lo=5, hi=MAXM):
    """Mốc phút-trước-@Now theo vận tốc: W1-2 12% (ngày 46-59), W3-6 38% (18-45), W7-8 50% (0-17)."""
    lo, hi = int(lo), int(hi)
    if hi <= lo: return lo
    for _ in range(40):
        r = rng.random()
        d = rng.randint(46, 59) if r < .12 else (rng.randint(18, 45) if r < .50 else rng.randint(0, 17))
        m = d * DAY + rng.randint(0, DAY - 1)
        if lo <= m <= hi: return m
    return rng.randint(lo, hi)

def media_lookup(kind, key):
    return MEDIA.get((kind, key))
MEDIA = {}
if ARGS.media_map and os.path.exists(ARGS.media_map):
    for k, e in json.load(open(ARGS.media_map, encoding="utf-8")).items():
        MEDIA[(e.get("kind"), k.replace("video:", ""))] = e
def img(folder, key, kind):
    e = media_lookup(kind, key)
    if e: return e["url"]
    return "https://res.cloudinary.com/%s/image/upload/veggielife/seed60/%s/%s.jpg" % (ARGS.cloud, folder, key)
def vid_url(key):
    e = media_lookup("video", key)
    return e["url"] if e else "https://res.cloudinary.com/%s/video/upload/veggielife/seed60/videos/%s.mp4" % (ARGS.cloud, key)
def vid_thumb(key):
    e = media_lookup("video", key)
    return e["thumbnail_url"] if e and e.get("thumbnail_url") else \
        "https://res.cloudinary.com/%s/video/upload/so_2,w_640,c_fill,ar_16:9/veggielife/seed60/videos/%s.jpg" % (ARGS.cloud, key)

PARTS = {}
def part(name):
    PARTS.setdefault(name, [])
    return PARTS[name]

def insert(out, table, cols, rows, identity=False, batch=400):
    if not rows: return
    if identity: out.append("SET IDENTITY_INSERT dbo.%s ON;" % table)
    for i in range(0, len(rows), batch):
        ch = rows[i:i + batch]
        out.append("INSERT INTO dbo.%s (%s) VALUES\n%s;" % (table, ", ".join(cols), ",\n".join("(" + ", ".join(r) + ")" for r in ch)))
    if identity: out.append("SET IDENTITY_INSERT dbo.%s OFF;" % table)
    out.append("PRINT N'   + %s: %d dòng';" % (table, len(rows)))
    COUNTS[table] = COUNTS.get(table, 0) + len(rows)
COUNTS = {}

def insert_join(out, table, tcols, sel, vcols, rows, joins, identity=False, batch=250):
    """INSERT ... SELECT ... FROM (VALUES ...) v(vcols) JOIN <bảng tạm tra cứu>; kiểm tra @@ROWCOUNT = số dòng (không rơi dòng)."""
    if not rows: return
    if identity: out.append("SET IDENTITY_INSERT dbo.%s ON;" % table)
    for i in range(0, len(rows), batch):
        ch = rows[i:i + batch]
        out.append("INSERT INTO dbo.%s (%s)\nSELECT %s\nFROM (VALUES\n%s\n) AS v(%s)\n%s;\nIF @@ROWCOUNT <> %d THROW 50001, N'%s: tra cứu dữ liệu master thất bại (thiếu tên/mã)', 1;" % (
            table, ", ".join(tcols), sel, ",\n".join("(" + ", ".join(r) + ")" for r in ch), ", ".join(vcols), joins, len(ch), table))
    if identity: out.append("SET IDENTITY_INSERT dbo.%s OFF;" % table)
    out.append("PRINT N'   + %s: %d dòng';" % (table, len(rows)))
    COUNTS[table] = COUNTS.get(table, 0) + len(rows)

# ------------------------------------------------------------------ master data đọc từ seed cũ
def read_ingredients():
    txt = open(os.path.join(SEED_DIR, "04_Seed_Ingredients.sql"), encoding="utf-8-sig").read()
    rx = re.compile(r"\(N'((?:[^']|'')*)',\s*N'(?:[^']|'')*',\s*'(\w+)',\s*([\d.]+),\s*(\d),\s*'(\w+)'\)")
    out = []
    for m in rx.finditer(txt):
        out.append(dict(name=m.group(1).replace("''", "'"), stall=m.group(2), kcal=float(m.group(3)), allium=int(m.group(4)), group=m.group(5)))
    return out
def read_enumbers():
    txt = open(os.path.join(SEED_DIR, "06_Seed_ENumberDictionary.sql"), encoding="utf-8-sig").read()
    rx = re.compile(r"\('(E[0-9a-z]+)',\s*N'(?:[^']|'')*',\s*N'(?:[^']|'')*',\s*N'(?:[^']|'')*',\s*'(VEGAN|NON_VEGAN|DOUBTFUL)'")
    return {m.group(1): m.group(2) for m in rx.finditer(txt)}

# Chỉ số K và key ảnh dạng số bám theo thứ tự 119 nguyên liệu gốc của file 04; nguyên liệu nhóm thêm vào sau
# (chèn giữa danh sách) bị bỏ khỏi bảng chỉ số để không làm lệch, nhưng vẫn có trong DB qua file 04.
ING_ADDED_LATER = {"Cơm trắng (Cơm nguội)", "Cơm gạo lứt"}
ING_LIST = [x for x in read_ingredients() if x["name"] not in ING_ADDED_LATER]
# ảnh nguyên liệu đã upload Cloudinary: Data/nguyên liệu/manifest_with_urls.csv (bỏ ảnh độ tin cậy thấp/nghi ngờ)
ING_IMG = {}
_mf = os.path.normpath(os.path.join(HERE, "..", "..", "..", "..", "Data", "nguyên liệu", "manifest_with_urls.csv"))
if os.path.exists(_mf):
    import csv as _csv
    for _r in _csv.DictReader(open(_mf, encoding="utf-8-sig")):
        if _r.get("do_tin_cay") in ("low", "uncertain") or not _r.get("image_url"): continue
        _k = _r["key"]
        if _k.isdigit() and 1 <= int(_k) <= len(ING_LIST): ING_IMG[ING_LIST[int(_k) - 1]["name"]] = _r["image_url"]
        elif _k in IMG_BY_KEY: ING_IMG[IMG_BY_KEY[_k]] = _r["image_url"]
assert len(ING_LIST) >= 119, "Không đọc đủ 119 nguyên liệu từ 04_Seed_Ingredients.sql (%d)" % len(ING_LIST)
ENUM = read_enumbers()
ING = {}                       # key -> dict(name,kcal,allium,group)
for k, idx in K.items(): ING[k] = ING_LIST[idx - 1]
assert ING["dauhu"]["name"].startswith("Đậu hũ trắng") and ING["gung"]["name"].startswith("Gừng") and ING["hanh_la"]["name"].startswith("Hành lá"), "Sai chỉ số nguyên liệu (kiểm tra thứ tự file 04)"
for k, s in SUPP.items(): ING[k] = dict(name=s[0], kcal=s[2], allium=s[3], group=s[4])
SHORT = lambda name: re.sub(r"\s*\(.*?\)", "", name).strip()

# ------------------------------------------------------------------ 1. USERS
NU = 150
users = []
def mk_name(female):
    ho = rng.choice(HO)
    return "%s %s %s" % (ho, rng.choice(DEM_NU if female else DEM_NAM), rng.choice(TEN_NU if female else TEN_NAM))

kinds = ["VIP"] * 28 + ["ACTIVE"] * 91 + ["PENDING"] * 8 + ["QUAR"] * 3 + ["SUSP"] * 8 + ["BAN"] * 3 + ["SOFT"] * 4 + ["ANON"] * 3
rng.shuffle(kinds)
kinds = ["ADMIN", "ADMIN"] + kinds
for i, k in enumerate(kinds):
    uid = BASE + i
    female = rng.random() < 0.55
    users.append(dict(id=uid, idx=i + 1, kind=k, female=female, name=mk_name(female), role="MEMBER", status="ACTIVE",
                      count=0, locked=None, flags=set(), google=None, phone=None, viol_times=[]))
by_kind = lambda k: [u for u in users if u["kind"] == k]
active_pool = by_kind("ACTIVE")
rng.shuffle(active_pool)
def take(n, flag):
    out = active_pool[:n]; del active_pool[:n]
    for u in out: u["flags"].add(flag)
    return out
warn_users = take(14, "warn"); exp3 = take(4, "exp3"); exp10 = take(2, "exp10")
expvip = take(8, "expvip"); refund = take(4, "refund"); lockout = take(2, "lockout"); noprof = take(10, "noprofile")
pay_fail = take(8, "payfail"); pay_pend = take(2, "paypend"); pay_misc = take(6, "paymisc")
violators = []
warn_counts = [1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 1, 2]
for u, c in zip(warn_users, warn_counts): u["count"] = c; violators.append(u)
for u, c in zip(exp3, [4, 5, 6, 4]): u["count"] = c; violators.append(u)
for u, c in zip(exp10, [7, 9]): u["count"] = c; violators.append(u)
for u, c in zip(by_kind("SUSP"), [4, 5, 6, 7, 8, 9, 10, 12]): u["count"] = c; violators.append(u)
for u, c in zip(by_kind("BAN"), [13, 14, 15]): u["count"] = c; violators.append(u)

for u in users:
    k = u["kind"]
    if k == "ADMIN": u["role"] = "ADMIN"
    if k == "VIP": u["role"] = "VIP"
    st = {"PENDING": "PENDING_ACTIVATION", "QUAR": "QUARANTINED", "SUSP": "SUSPENDED", "BAN": "BANNED",
          "SOFT": "SOFT_DELETED", "ANON": "ANONYMIZED_DELETED"}.get(k, "ACTIVE")
    u["status"] = st
    u["email"] = "user%03d@%s" % (u["idx"], EMAIL_DOMAIN)
    if k == "ADMIN": u["email"] = "admin%02d@%s" % (u["idx"], EMAIL_DOMAIN); u["name"] = "Quản trị viên %02d" % u["idx"]
    if k == "ANON":
        u["email"] = "deleted_%d@%s" % (u["id"], EMAIL_DOMAIN); u["name"] = "Người dùng đã xóa"
    else:
        if rng.random() < .75: u["phone"] = "03%08d" % (10000000 + u["idx"] * 7919 % 89999999)
        if rng.random() < .15 and k not in ("ADMIN",): u["google"] = "seed60-google-%05d" % u["idx"]

# --- cân avatar: chỉ có 12 ảnh nam đã duyệt -> user nam ACTIVE vượt số ảnh nam đổi sang nữ (tên nữ mới, giữ họ).
#     Dùng rng riêng để không xê dịch chuỗi ngẫu nhiên của các bảng khác.
AV_MALE = {9, 26, 51, 52, 69, 73, 79, 95, 100, 109, 126, 154}
_rng_g = random.Random(ARGS.seed * 7 + 1)
for u in [u for u in users if not u["female"] and u["status"] == "ACTIVE"][len(AV_MALE):]:
    u["female"] = True
    if u["kind"] != "ADMIN": u["name"] = "%s %s %s" % (u["name"].split()[0], _rng_g.choice(DEM_NU), _rng_g.choice(TEN_NU))

# --- mốc tạo tài khoản (phút trước @Now)
for u in users:
    k = u["kind"]
    if k == "ADMIN": m = 86000 if u["idx"] == 1 else 85400
    elif u in violators:
        m = rnd_between((30 if u["count"] >= 4 else 15) * DAY, 58 * DAY)
    elif k == "PENDING": m = rnd_between(8, 3 * DAY)
    elif "expvip" in u["flags"]: m = rnd_between(33 * DAY, 59 * DAY)
    elif k in ("SOFT", "ANON"): m = sample_m(5 * DAY, MAXM - 100)
    elif k == "VIP": m = max(sample_m(200, MAXM), 200)
    elif "payfail" in u["flags"] or "paymisc" in u["flags"] or "refund" in u["flags"] or "paypend" in u["flags"]:
        m = sample_m(2 * DAY, MAXM)
    else: m = sample_m(60, MAXM)
    u["m_created"] = min(m, MAXM)

# ------------------------------------------------------------------ 2. PAYMENTS (+ VIP/role)
PLAN_MIN = {30: 30 * DAY, 90: 90 * DAY, 365: 365 * DAY}
vips = by_kind("VIP")
rng.shuffle(vips)
chain_vips = vips[:5]
for u in chain_vips:
    u["flags"].add("chain")
    u["m1"] = rnd_between(50 * DAY, 58 * DAY)
    u["m_created"] = min(MAXM, u["m1"] + rnd_between(60, 1500))
single_vips = vips[5:]
txs = []
def add_tx(u, m_create, status, dur, m_start=None, kind="normal"):
    t = dict(id=BASE + len(txs), user=u, dur=dur, status=status, m_create=m_create, kind=kind)
    if status == "PENDING":
        t["m_done"] = None; t["m_start"] = m_create
    elif status == "SUCCESS" or status == "REFUNDED":
        t["m_done"] = max(1, m_create - rnd_between(2, 9)); t["m_start"] = m_start if m_start is not None else t["m_done"]
    elif status == "EXPIRED":
        t["m_done"] = max(1, m_create - 5); t["m_start"] = m_create
    else:
        t["m_done"] = max(1, m_create - rnd_between(1, 5)); t["m_start"] = m_create
    t["m_end"] = t["m_start"] - PLAN_MIN[dur]
    t["gw"] = rng.choice(["MOMO", "VIETQR"])
    txs.append(t); return t

for u in chain_vips:
    t1 = add_tx(u, u["m1"], "SUCCESS", 30, kind="chain1")
    m2 = t1["m_end"] + rnd_between(300, 4000)
    add_tx(u, m2, "SUCCESS", 30, m_start=t1["m_end"], kind="chain2")
for i, u in enumerate(single_vips):
    dur = [30] * 10 + [90] * 8 + [365] * 5
    d = dur[i % len(dur)]
    hi = min(28 * DAY if d == 30 else MAXM, u["m_created"] - 30)
    m = rnd_between(15, max(15, hi))
    add_tx(u, m, "SUCCESS", d)
    if i < 20:
        ma = m + rnd_between(5, 1400)
        if ma <= u["m_created"] - 5:
            add_tx(u, ma, rng.choice(["FAILED", "FAILED", "CANCELLED", "EXPIRED"]), d, kind="retry")
for u in by_kind("ACTIVE"):
    if "expvip" in u["flags"]:
        m = rnd_between(int(31.6 * DAY), max(int(31.7 * DAY), u["m_created"] - 30))
        add_tx(u, m, "SUCCESS", 30, kind="expired")
for u in refund:
    d = rng.choice([30, 90, 365]); m = rnd_between(40, u["m_created"] - 30)
    add_tx(u, m, "REFUNDED", d)
for i, u in enumerate(pay_fail):
    add_tx(u, rnd_between(40, u["m_created"] - 30), "FAILED", rng.choice([30, 90]))
    if i < 4: add_tx(u, rnd_between(40, u["m_created"] - 30), "CANCELLED", 30)
for i, u in enumerate(pay_misc):
    for _ in range([2, 2, 2, 2, 1, 1][i]): add_tx(u, rnd_between(40, u["m_created"] - 30), "EXPIRED", rng.choice([30, 90]))
for i, u in enumerate(pay_pend): add_tx(u, rnd_between(1, 4), "PENDING", 30)

# vai trò theo giao dịch: VIP <=> có SUCCESS còn hạn (kiểm tra cứng)
for u in users:
    act = [t for t in txs if t["user"] is u and t["status"] == "SUCCESS" and t["m_end"] < 0]
    if u["role"] == "VIP": assert act, "VIP thiếu giao dịch SUCCESS còn hạn: %s" % u["email"]
    elif u["role"] == "MEMBER": assert not act, "MEMBER có giao dịch còn hạn: %s" % u["email"]

# ------------------------------------------------------------------ 3. HEALTH PROFILES / WEIGHT / ALLERGIES
def calc_age(dob):
    a = TODAY.year - dob.year
    if dob > TODAY.replace(year=TODAY.year - a) if TODAY.year - a > 0 else False: a -= 1
    return a
def bmi_of(h, w): return r2(w / ((h / 100.0) ** 2))
def bmr_of(h, w, age, male): return r2(10 * w + 6.25 * h - 5 * age + (5 if male else -161))
def tdee_of(b): return r2(b * 1.2)
def target_of(t, goal, male):
    x = t - 400 if goal == "LOSE_WEIGHT" else (t + 400 if goal == "GAIN_MUSCLE" else t)
    return r2(max(x, 1500 if male else 1200))

prof_users = [u for u in users if u["kind"] not in ("ADMIN", "PENDING", "SOFT", "ANON", "BAN") and "noprofile" not in u["flags"]]
rng.shuffle(prof_users)
stale_set = set(id(u) for u in prof_users[:5])
SCHOOLS = ["LACTO_OVO"] * 35 + ["VEGAN"] * 25 + ["BUDDHIST"] * 20 + ["LACTO"] * 8 + ["OVO"] * 6 + ["POLLOTARIAN"] * 3 + ["PESCATARIAN"] * 3
profiles = []; weights = []; allergies = []
ALG = ["SOY"] * 20 + ["GLUTEN"] * 20 + ["PEANUT"] * 15 + ["TREE_NUT"] * 12 + ["SESAME"] * 8 + ["EGG"] * 8 + ["DAIRY"] * 7 + ["CELERY"] * 4 + ["MUSTARD"] * 3 + ["SULFITES"] * 3
for u in prof_users:
    male = not u["female"]
    h = r2(clamp(rng.gauss(168 if male else 157, 6), 148, 188))
    bmi0 = clamp(rng.gauss(23.5, 3.2), 17.8, 33.0)
    w0 = r2(bmi0 * (h / 100.0) ** 2)
    goal = "LOSE_WEIGHT" if bmi0 > 25 and rng.random() < .75 else ("GAIN_MUSCLE" if bmi0 < 19.5 and rng.random() < .75 else rng.choice(["LOSE_WEIGHT"] * 4 + ["MAINTAIN"] * 3 + ["GAIN_MUSCLE"] * 3))
    m_prof = max(3, u["m_created"] - rnd_between(2, 2000))
    if m_prof >= u["m_created"]: m_prof = max(1, u["m_created"] - 1)
    logs = [(m_prof, w0)]
    m = m_prof; w = w0
    while True:
        m -= rnd_between(3 * DAY, 6 * DAY)
        if m < 120: break
        trend = {"LOSE_WEIGHT": -rng.uniform(.1, .35), "GAIN_MUSCLE": rng.uniform(.1, .3), "MAINTAIN": rng.uniform(-.1, .1)}[goal]
        w = max(35.0, w + trend + rng.uniform(-.25, .25)); logs.append((m, round(w, 2)))
    cur = r2(logs[-1][1])
    tw = r2(max(cur - rnd_between(3, 10), 18.5 * (h / 100.0) ** 2 + .5)) if goal == "LOSE_WEIGHT" else (r2(cur + rnd_between(2, 7)) if goal == "GAIN_MUSCLE" else cur)
    age = rnd_between(18, 58)
    dob = TODAY - datetime.timedelta(days=age * 365 + rnd_between(0, 364) + age // 4)
    age = calc_age(dob)
    school = rng.choice(SCHOOLS)
    use_w = logs[0][1] if id(u) in stale_set else cur
    bmi = bmi_of(h, use_w); bmr = bmr_of(h, use_w, age, male); tdee = tdee_of(bmr); tgt = target_of(tdee, goal, male)
    p = dict(user=u, school=school, male=male, dob=dob, h=h, cur=cur, tw=tw, bmi=bmi, bmr=bmr, tdee=tdee, tgt=tgt,
             allium_free=1 if (school == "BUDDHIST" and rng.random() < .95) or rng.random() < .08 else 0,
             region=rng.choice(["NORTH"] * 30 + ["CENTRAL"] * 15 + ["SOUTH"] * 55), stale=1 if id(u) in stale_set else 0,
             goal=goal, m_prof=m_prof, m_upd=logs[-1][0])
    u["profile"] = p; profiles.append(p)
    for lm, lw in logs:
        weights.append(dict(user=u, m=lm, w=lw, note=rng.choice([None, None, None, "Cân buổi sáng", "Sau tuần ăn kiêng", "Đo tại phòng gym"])))
    if rng.random() < .55:
        for code in set(rng.sample(ALG, rnd_between(1, 2))):
            allergies.append(dict(user=u, code=code, m=max(1, m_prof - rnd_between(0, 300)), note=rng.choice([None, None, "Dị ứng nhẹ", "Đã được bác sĩ xác nhận"])))

# ------------------------------------------------------------------ 4. RECIPES
def parse_spec(spec):
    lines = []
    for tok in spec.split():
        parts = tok.split(":")
        key, g = parts[0], float(parts[1]); opt = len(parts) > 2 and parts[2] == "o"
        assert key in ING, "thiếu nguyên liệu key=%s" % key
        lines.append([key, g, opt])
    return lines
ML_KEYS = {"dau_oil", "dau_me", "nuoc_tuong", "nuoc_mam", "dau_hao", "coconut"}
def kcal_total(lines): return sum(g * ING[k]["kcal"] / 100.0 for k, g, _ in lines)

recipes = []
foods = []
slugs = [b["slug"] for b in BOOK]; rng.shuffle(slugs)
BOOK_BY = {b["slug"]: b for b in BOOK}
NAME_VI = {b["slug"]: b["name"] for b in BOOK}
GOAL_LABEL = {"LOSE_WEIGHT": "giảm cân", "MAINTAIN": "giữ cân", "GAIN_MUSCLE": "tăng cân"}
BAND = {"LOSE_WEIGHT": (300, 420), "MAINTAIN": (450, 620), "GAIN_MUSCLE": (680, 900)}
CARB = {"gao", "gao_lut", "gao_nep", "bun", "banh_pho", "mi", "mien", "khoai_tay", "khoai_lang", "khoai_mon", "bot_mi", "bot_gao", "bot_nang", "bot_ngo", "banh_trang"}
PROT = {"dauhu", "dauhu_chien", "dauhu_non", "hukyk", "mican", "chalua", "bo_chay", "suon", "ga_chay", "tempeh", "dau_nanh", "dau_xanh", "dau_phong", "hat_dieu", "egg", "egg_cut", "milk", "cheese"}
FATK = {"dau_oil", "dau_me", "coconut", "butter"}
SUGAR = {"duong"}
SEAS = {"muoi", "tieu", "hat_nem", "nuoc_tuong", "nuoc_mam", "dau_hao", "tuong_hot", "ot", "giam", "chanh", "me", "nghe", "sa", "gung", "toi", "hanh_tim", "hanh_la", "hanh_tay", "boro", "ngo_ri", "ngo_gai", "rau_thom", "hung", "he"}
GMUL = {"LOSE_WEIGHT": dict(fat=.5, sug=.5, carb=.75, prot=1.0, veg=1.3), "MAINTAIN": dict(fat=1, sug=1, carb=1, prot=1, veg=1),
        "GAIN_MUSCLE": dict(fat=1.3, sug=1.0, carb=1.25, prot=1.35, veg=1.0)}
GNOTE = {"LOSE_WEIGHT": "Bản giảm cân: giảm dầu mỡ, đường và tinh bột, tăng lượng rau củ; ăn chậm, nhai kỹ.",
         "MAINTAIN": "Bản giữ cân: khẩu phần cân đối đạm – tinh bột – rau củ.",
         "GAIN_MUSCLE": "Bản tăng cân: tăng đạm thực vật, tinh bột và chất béo tốt; có thể ăn kèm hạt/đậu phộng rang."}
def kind_of(k): return "fat" if k in FATK else "sug" if k in SUGAR else "carb" if k in CARB else "prot" if k in PROT else "veg"
def goal_of(kc): return "LOSE_WEIGHT" if kc < 430 else ("MAINTAIN" if kc <= 640 else "GAIN_MUSCLE")
def per_serv(lines, serv): return kcal_total(lines) / serv
def make_lines(base, goal, serv):
    mul = GMUL[goal]; lines = [[k, round(g * mul[kind_of(k)], 1), o] for k, g, o in base]
    lo, hi = BAND[goal]
    for _ in range(3):
        per = per_serv(lines, serv)
        if lo <= per <= hi: break
        f = clamp(((lo + hi) / 2.0) * serv / max(1.0, kcal_total(lines)), .55, 2.4)
        lines = [[k, (max(5.0, round(g * f / 5.0) * 5.0) if (g >= 15 and k not in SEAS) else g), o] for k, g, o in lines]
    return lines
# --- kế hoạch biến thể: mỗi món 1–3 công thức ---
plan = []
allium_foods = []
for slug in slugs:
    b = BOOK_BY[slug]
    base = [[k, float(g), False] for k, g in b["spec"].items()]
    for k, _, _ in base: assert k in ING, "thiếu nguyên liệu key=%s (%s)" % (k, slug)
    tot = kcal_total(base); serv = 2 if tot <= 2200 else (4 if tot <= 4500 else 6)
    nat = goal_of(per_serv(base, serv))
    if per_serv(base, serv) < 250 or per_serv(base, serv) > 950: nat = "MAINTAIN"
    nvar = rng.choice([1] * 20 + [2] * 45 + [3] * 35)
    goals = [g for g in ("LOSE_WEIGHT", "MAINTAIN", "GAIN_MUSCLE") if g != nat]; rng.shuffle(goals)
    has_chili = any(k in ("ot", "tuong_hot") for k, _, _ in base)
    has_all = any(ING[k]["allium"] for k, _, _ in base)
    variants = [(nat, None, True)]
    extra = [(g, None, False) for g in goals]
    if has_chili: extra.insert(rng.randint(0, 2), (rng.choice(["LOSE_WEIGHT", "MAINTAIN", "GAIN_MUSCLE"]), "no_chili", False))
    if has_all: allium_foods.append((slug, nat, goals, base, serv))
    variants += extra[:nvar - 1]
    foods.append(dict(id=BASE + len(foods), slug=slug, name=b["name"], cat=C_MAP[b["cat"]]))
    for g, flag, is_base in variants: plan.append((slug, g, flag, is_base, base, serv, len(foods) - 1))
# --- bổ sung ~30 công thức KHÔNG HÀNH TỎI (kiêng ngũ vị tân) từ các món có hành/tỏi ---
rng.shuffle(allium_foods)
FOOD_IDX = {f["slug"]: i for i, f in enumerate(foods)}
for slug, nat, goals, base, serv in allium_foods[:30]:
    used = {p[1] for p in plan if p[0] == slug}
    free = [g for g in ("MAINTAIN", "LOSE_WEIGHT", "GAIN_MUSCLE") if g not in used] or ["MAINTAIN"]
    plan.append((slug, rng.choice(free), "no_allium", False, base, serv, FOOD_IDX[slug]))
print("[seed60] món có hành/tỏi: %d; thêm bản không hành tỏi: %d" % (len(allium_foods), min(30, len(allium_foods))))
N_AI = max(4, len(plan) // 10); N_USER = max(12, len(plan) // 4)
roles_pool = ["SYSTEM"] * (len(plan) - N_AI - N_USER) + ["USER"] * N_USER + ["AI"] * N_AI
rng.shuffle(roles_pool)
author_pool = [u for u in users if u["status"] == "ACTIVE" and u["role"] in ("MEMBER", "VIP") and u["m_created"] >= 3 * DAY and u not in violators and "expvip" not in u["flags"]]
viol_recipe_authors = [u for u in violators if u["count"] >= 4][:2]
user_status = (["APPROVED_ACTIVE"] * 5 + ["PENDING_REVIEW"] * 3 + ["DRAFT"] * 2 + ["REJECTED", "ARCHIVED", "QUARANTINED_HIDDEN", "QUARANTINED_HIDDEN"]) * 6
ai_status = (["APPROVED_ACTIVE"] * 3 + ["PENDING_REVIEW"] * 2 + ["DRAFT"]) * 4
rng.shuffle(user_status)
admin1 = users[0]
for (slug, goal, flag, is_base, base, serv, fi), role in zip(plan, roles_pool):
    b = BOOK_BY[slug]; fd = foods[fi]
    cat = fd["cat"]; meals, prep, cook, diff = b["meals"], b["prep"], b["cook"], b["diff"]
    lines = [list(x) for x in base] if is_base else make_lines(base, goal, serv)
    note = [GNOTE[goal]]
    if flag == "no_chili":
        lines = [l for l in lines if l[0] not in ("ot", "tuong_hot")]; note.append("Bản không cay: bỏ ớt, tương ớt và các gia vị cay khi nấu.")
    elif flag == "no_allium":
        lines = [l for l in lines if not ING[l[0]]["allium"]]; note.append("Bản không hành tỏi (hợp người ăn chay kiêng ngũ vị tân): bỏ hành, tỏi, hẹ, boa rô.")
    kcal = r2(kcal_total(lines) / serv)
    groups = set(ING[k]["group"] for k, _, _ in lines)
    assert groups <= {"PLANT", "EGG", "DAIRY"}, (slug, groups)
    has_allium = any(ING[k]["allium"] for k, _, _ in lines)
    has_egg, has_dairy = "EGG" in groups, "DAIRY" in groups
    school = "LACTO_OVO" if (has_egg and has_dairy) else "OVO" if has_egg else "LACTO" if has_dairy else ("VEGAN" if has_allium else "BUDDHIST")
    src_steps = b["steps"]
    if flag == "no_allium":
        AL_RX = re.compile(r"(?<!tiến )(?<!thực )hành(?! động)|tỏi|\bhẹ\b|boa ?-?rô|boaro|củ nén|kiệu", re.I)
        clean = []
        for st_ in src_steps:
            sents = [x for x in re.split(r"(?<=[.!?])\s+", st_) if not AL_RX.search(x)]
            if sents: clean.append(" ".join(sents))
        if len(clean) >= 2: src_steps = clean
    steps = ["%d. %s" % (i + 1, s_.replace("\n", " ")) for i, s_ in enumerate(src_steps)]
    steps.append("%d. Ghi chú – %s" % (len(steps) + 1, " ".join(note)))
    tag = {"no_chili": " (không cay)", "no_allium": " (không hành tỏi)"}.get(flag, "")
    title = "%s – bản %s%s" % (b["name"], GOAL_LABEL[goal], tag)
    r = dict(id=BASE + len(recipes), slug=slug, title=title, name=b["name"], goal=goal, cat=cat, meals=meals, prep=prep, cook=cook,
             serv=serv, diff=diff, lines=lines, kcal=kcal, school=school, has_allium=has_allium, has_egg=has_egg, has_dairy=has_dairy,
             source=role, method="book", instr="\n".join(steps), image=b["image"], ref=b["ref"], food=fd, variant=flag)
    if role == "SYSTEM":
        r.update(user=admin1, status="APPROVED_ACTIVE", verified=1, src_db="SYSTEM", m=rnd_between(45 * DAY, 59 * DAY))
    else:
        r["src_db"] = role
        r["status"] = ai_status.pop() if role == "AI" else user_status.pop()
        if role == "USER" and r["status"] == "QUARANTINED_HIDDEN" and viol_recipe_authors:
            r["user"] = viol_recipe_authors.pop(0); r["viol"] = True
        else:
            r["user"] = rng.choice(author_pool)
        r["verified"] = 1 if (r["status"] == "APPROVED_ACTIVE" and role == "USER" and rng.random() < .5) else 0
        r["m"] = sample_m(5, max(10, r["user"]["m_created"] - 90))
    r["m_upd"] = max(1, r["m"] - rnd_between(30, 3000)) if r["status"] != "DRAFT" else None
    r["views"] = {"APPROVED_ACTIVE": rnd_between(20, 1800 if role == "SYSTEM" else 500)}.get(r["status"], rnd_between(0, 25))
    recipes.append(r)
REC_OK = [r for r in recipes if r["status"] == "APPROVED_ACTIVE"]

# ------------------------------------------------------------------ 5. POSTS / VIDEOS / COMMENTS / VOTES / REPORTS
normal_authors = [u for u in users if u["status"] == "ACTIVE" and u["role"] in ("MEMBER", "VIP") and u not in violators
                  and u["m_created"] >= 2 * DAY and "expvip" not in u["flags"]]
vip_authors = [u for u in normal_authors if u["role"] == "VIP"]
posts, videos, comments, votes, reports = [], [], [], [], []
RID_IMG = {r_["id"]: r_["image"] for r_ in recipes}
NAME_IMG = {}   # tên món sách -> ảnh món (dùng cho bài viết nói về món đó nhưng không gắn công thức)
for r_ in recipes:
    if r_["image"]: NAME_IMG.setdefault(r_["name"], r_["image"])
def new_post(author, m, title, body, status, vis="PUBLIC", recipe=None, with_img=None, dish=None):
    p = dict(id=BASE + len(posts), user=author, m=m, title=title[:150], body=body, status=status, vis=vis,
             recipe=recipe, image=None, views=0, m_upd=None, video=None, report_state=None)
    if with_img is None: with_img = rng.random() < .6
    if with_img: p["image"] = RID_IMG.get(recipe) or NAME_IMG.get(dish)
    posts.append(p); return p
def dish_name(): return NAME_VI[rng.choice(slugs)]

# --- video posts: CHỈ 6 video thật (Data/videos), gắn với công thức gần nghĩa nhất ---
REAL_VIDEOS = [
    ("vid_01", "Cơm chiên vàng rau củ", 62, r"cơm"),
    ("vid_02", "Đậu hũ áp chảo sốt, ăn kèm gạo lứt và bông cải", 35, r"đậu (hũ|phụ).*(sốt|nấm)"),
    ("vid_03", "Đậu hũ sốt cay ăn kèm rau thơm", 85, r"đậu (hũ|phụ).*sốt cay|sốt cay"),
    ("vid_04", "Đậu hũ hấp sốt nấm hương", 90, r"đậu (hũ|phụ).*(hấp|om|nấm đông cô|la hán)"),
    ("vid_05", "Chả giò chay và canh chua chay", 69, r"chả giò"),
    ("vid_06", "Canh chua chay nấm đậu hũ", 93, r"canh chua"),
]
for i, (vkey, vtitle, vdur, rx) in enumerate(REAL_VIDEOS):
    au = rng.choice(vip_authors) if rng.random() < .7 else rng.choice(normal_authors)
    cands = [r for r in REC_OK if re.search(rx, r["name"].lower())] or REC_OK
    r = rng.choice(cands)
    m = sample_m(10, max(30, au["m_created"] - 10))
    p = new_post(au, m, "Video hướng dẫn: %s" % vtitle, "Mình quay lại các bước làm %s, xem video để làm theo nhé." % vtitle.lower(), "APPROVED_ACTIVE", recipe=r["id"], with_img=False)
    p["video"] = dict(key=vkey, status="READY", recipe=r, dur=vdur, vtitle=vtitle)
# --- bài viết thường: tổng 110 (gồm 12 video-post) ---
POST_ST = ["APPROVED_ACTIVE"] * 78 + ["PENDING_REVIEW"] * 8 + ["DRAFT"] * 5 + ["REJECTED"] * 5 + ["DELETED"] * 4
for i in range(98):
    au = rng.choice(normal_authors)
    m = sample_m(5, max(10, au["m_created"] - 10))
    st = rng.choice(POST_ST)
    if m < 2 * DAY and st == "APPROVED_ACTIVE" and rng.random() < .25: st = "PENDING_REVIEW"
    if m > 3 * DAY and st == "PENDING_REVIEW": st = "APPROVED_ACTIVE"
    d = dish_name()
    vis = rng.choice(["PUBLIC"] * 17 + ["FOLLOWERS"] * 2 + ["PRIVATE"] * 1) if st == "APPROVED_ACTIVE" else "PUBLIC"
    rec = rng.choice(REC_OK)["id"] if rng.random() < .3 else None
    p = new_post(au, m, rng.choice(POST_TITLES).format(d=d), rng.choice(POST_BODIES).format(d=d), st, vis, rec, dish=d)
    if st in ("APPROVED_ACTIVE", "REJECTED", "DELETED"): p["m_upd"] = max(1, m - rnd_between(5, 1500))
# --- bài của người dùng bị cách ly (QUARANTINED) cho báo cáo AI_SCAN đang chờ ---
quar_posts = []
for u in by_kind("QUAR"):
    for _ in range(2):
        m = rnd_between(15, min(2 * DAY, u["m_created"] - 30))
        p = new_post(u, m, rng.choice(LABEL_POST), "Bài viết đang chờ AI/quản trị viên xác minh nhãn thành phần.", "QUARANTINED_HIDDEN", with_img=True)
        quar_posts.append(p)

# --- vi phạm: kế hoạch sự kiện theo từng người ---
TIER = lambda c: 0 if c <= 3 else (3 if c <= 6 else (10 if c <= 9 else (30 if c <= 12 else 999)))
admins = [users[0], users[1]]
used_target = set()
approve_events = []   # dict(user, m, kind, type, src)
need_video = []   # chỉ giữ 6 video thật; không tạo video BANNED giả (vi phạm dùng bài viết/bình luận/công thức)
for u in violators:
    k, mc = u["count"], u["m_created"]
    if u in exp3: lo, hi = 3 * DAY + 60, 12 * DAY
    elif u in exp10: lo, hi = 10 * DAY + 60, 25 * DAY
    elif u["status"] == "SUSPENDED": lo, hi = 60, int(TIER(k) * DAY * 0.8)
    elif u["status"] == "BANNED": lo, hi = 60, 20 * DAY
    else: lo, hi = 60, 40 * DAY
    hi = min(hi, mc - 300 - (k - 1) * 70)
    if hi < lo: lo = max(20, hi - 10)
    m_last = rnd_between(lo, max(lo, hi))
    span_hi = mc - 300
    earlier = sorted(set(rnd_between(m_last + 60, span_hi) for _ in range((k - 1) * 3)), reverse=True)
    while len(earlier) < k - 1: earlier.append(earlier[-1] + 70 if earlier else m_last + 70)
    if len(earlier) > k - 1:                       # chọn thưa đều để các mốc cách nhau
        step = len(earlier) / float(k - 1); earlier = [earlier[int(i * step)] for i in range(k - 1)]
    times = sorted(earlier + [m_last], reverse=True)   # cũ -> mới
    u["viol_times"] = times
    u["m_last"] = m_last
    kinds_ev = []
    if any(r.get("viol") and r["user"] is u for r in recipes): kinds_ev.append("recipe")
    if u in need_video: kinds_ev.append("video")
    while len(kinds_ev) < k: kinds_ev.append("comment" if rng.random() < .65 else "post")
    rng.shuffle(kinds_ev)
    for mt, kd in zip(times, kinds_ev):
        typ = {"recipe": rng.choice(["VIO_MEAT", "VIO_LABEL"]), "video": "VIO_MEAT",
               "post": rng.choice(["VIO_MEAT", "VIO_LABEL", "VIO_SPAM"]), "comment": rng.choice(["VIO_SPAM", "VIO_HATE"])}[kd]
        approve_events.append(dict(user=u, m=mt, kind=kd, type=typ, src="AI_SCAN" if rng.random() < .35 else "USER_REPORT"))

def pick_reporter(m, exclude):
    cands = [x for x in normal_authors if x is not exclude and x["m_created"] > m + 5]
    return rng.choice(cands) if cands else None
def report(target_kind, target, typ, src, status, m, owner):
    key = (target_kind, target["id"])
    assert key not in used_target
    used_target.add(key)
    rep = dict(id=BASE + len(reports), kind=target_kind, target=target, type=typ, src=src, status=status, m=m, owner=owner,
               reporter=None if src == "AI_SCAN" else pick_reporter(m, owner),
               admin=rng.choice(admins) if status != "PENDING" else None)
    reports.append(rep); return rep
EVID = {"VIO_MEAT": "Nội dung nhắc đến thịt/hải sản trong món chay: ", "VIO_LABEL": "Gắn nhãn thuần chay nhưng thành phần có nguyên liệu động vật: ",
        "VIO_HATE": "Ngôn từ công kích, xúc phạm người dùng khác: ", "VIO_SPAM": "Quảng cáo/spam, chèn liên kết trái phép: "}

viol_recipes = {r["user"]["id"]: r for r in recipes if r.get("viol")}
for ev in approve_events:
    u, mr, kd, typ = ev["user"], ev["m"], ev["kind"], ev["type"]
    room = lambda cap: rnd_between(10, max(10, min(cap, u["m_created"] - 30 - mr)))
    if kd == "recipe":
        r = viol_recipes[u["id"]]; r["m"] = mr + room(1500); r["m_upd"] = max(1, mr - rnd_between(1, 120)); r["title"] = r["title"]
        report("recipe", r, typ, ev["src"], "APPROVE", mr, u)
    elif kd == "video":
        key = "recipe_video_%02d" % (13 + need_video.index(u))
        p = new_post(u, mr + room(1500), "Video: món chay 'đặc biệt' có thêm thịt", "Video hướng dẫn món chay nhưng bỏ thêm thịt băm cho đậm vị.", "QUARANTINED_HIDDEN", with_img=False)
        p["video"] = dict(key=key, status="BANNED", recipe=None, dur=rnd_between(60, 240), banned=True); p["m_upd"] = max(1, mr - rnd_between(1, 120))
        report("video", p, typ, ev["src"], "APPROVE", mr, u)
    elif kd == "post":
        t = rng.choice(MEAT_POST if typ == "VIO_MEAT" else (LABEL_POST if typ == "VIO_LABEL" else ["Khuyến mãi sốc – inbox ngay!!!"]))
        p = new_post(u, mr + room(2000), t, rng.choice(SPAM_TXT) if typ == "VIO_SPAM" else "Nội dung đã bị cộng đồng báo cáo vì vi phạm quy chuẩn ẩm thực chay.",
                     "QUARANTINED_HIDDEN" if rng.random() < .75 else "DELETED")
        p["m_upd"] = max(1, mr - rnd_between(1, 600)); report("post", p, typ, ev["src"], "APPROVE", mr, u)
    else:
        okp = [p for p in posts if p["status"] == "APPROVED_ACTIVE" and p["vis"] == "PUBLIC" and p["m"] > mr + 25 and p["user"] is not u]
        post = rng.choice(okp) if okp else max(posts, key=lambda x: x["m"])
        hi = min(post["m"] - 5, u["m_created"] - 10, mr + 3000)
        mc_ = rnd_between(mr + 10, max(mr + 10, hi))
        txt = rng.choice(SPAM_TXT if typ == "VIO_SPAM" else HATE_TXT)
        c = dict(id=BASE + len(comments), user=u, post=post, parent=None, m=mc_, text=txt, status="QUARANTINED_HIDDEN", soft=0, m_upd=max(1, mr - rnd_between(1, 600)))
        comments.append(c); report("comment", c, typ, ev["src"], "APPROVE", mr, u)

# --- bình luận bình thường ---
vis_posts = [p for p in posts if p["status"] == "APPROVED_ACTIVE" and p["vis"] == "PUBLIC" and p["user"] in normal_authors]
w_recent = [1.0 + 3.0 * (1 - p["m"] / float(MAXM)) for p in vis_posts]
def add_comment(post, author, m, parent=None, text=None):
    c = dict(id=BASE + len(comments), user=author, post=post, parent=parent, m=m, text=text or rng.choice(COMMENT_TXT),
             status="ACTIVE", soft=0, m_upd=None)
    comments.append(c); return c
tries = 0
n_normal_comments = 0
while n_normal_comments < 340 and tries < 4000:
    tries += 1
    post = rng.choices(vis_posts, w_recent)[0]
    au = rng.choice(normal_authors)
    hi = min(post["m"] - 3, au["m_created"] - 5)
    if hi < 3: continue
    parent = None
    sibs = [c for c in comments if c["post"] is post and c["parent"] is None and c["status"] == "ACTIVE" and c["m"] > 6]
    if sibs and rng.random() < .22:
        parent = rng.choice(sibs); hi = min(hi, parent["m"] - 2)
        if hi < 3: continue
    c = add_comment(post, au, sample_m(2, hi), parent)
    if rng.random() < .03: c["soft"] = 1
    n_normal_comments += 1

# --- báo cáo REJECT / PENDING trên nội dung bình thường ---
normal_comments = [c for c in comments if c["status"] == "ACTIVE" and c["user"] in normal_authors]
normal_posts = [p for p in posts if p["status"] == "APPROVED_ACTIVE" and p["user"] in normal_authors and p["vis"] == "PUBLIC"]
cnt_rej = 0
for _ in range(400):
    if cnt_rej >= 25: break
    if rng.random() < .6:
        t = rng.choice(normal_comments); kind = "comment"
    else:
        t = rng.choice(normal_posts); kind = "post"
    if (kind, t["id"]) in used_target or t["m"] < 6: continue
    mr = max(2, t["m"] - rnd_between(20, 3000))
    report(kind, t, rng.choice(["VIO_SPAM", "VIO_HATE", "VIO_LABEL", "VIO_MEAT"]), rng.choice(["USER_REPORT", "USER_REPORT", "AI_SCAN"]), "REJECT", mr, t["user"]); cnt_rej += 1
cnt_pen = 0
recent_c = [c for c in normal_comments if c["m"] < 3 * DAY]; recent_p = [p for p in normal_posts if p["m"] < 3 * DAY]
for _ in range(400):
    if cnt_pen >= 20: break
    if recent_c and rng.random() < .65: t = rng.choice(recent_c); kind = "comment"
    elif recent_p: t = rng.choice(recent_p); kind = "post"
    else: break
    if (kind, t["id"]) in used_target or t["m"] < 6: continue
    mr = max(1, t["m"] - rnd_between(5, 600))
    report(kind, t, rng.choice(["VIO_SPAM", "VIO_HATE", "VIO_LABEL"]), "USER_REPORT", "PENDING", mr, t["user"])
    if kind == "comment": t["status"] = "FLAGGED"
    cnt_pen += 1
for p in quar_posts:
    mr = max(1, p["m"] - rnd_between(1, 20))
    report("post", p, rng.choice(["VIO_LABEL", "VIO_MEAT"]), "AI_SCAN", "PENDING", mr, p["user"])

# áp trạng thái theo báo cáo APPROVE: số lần vi phạm của user phải khớp
from collections import Counter
approved_cnt = Counter(r["owner"]["id"] for r in reports if r["status"] == "APPROVE")
for u in users:
    assert approved_cnt.get(u["id"], 0) == u["count"], "Count_ViolationReports lệch: %s" % u["email"]
for u in violators:
    if u["status"] == "SUSPENDED": u["locked"] = tsx_val = u["m_last"] - TIER(u["count"]) * DAY
    elif u in exp3 or u in exp10: u["locked"] = u["m_last"] - TIER(u["count"]) * DAY
    u["locked_set"] = u.get("locked") is not None

# --- video (bản ghi) + transcript ---
for p in posts:
    if not p["video"]: continue
    vd = p["video"]
    m_v = max(1, p["m"] - rnd_between(1, 15))
    vd.update(id=BASE + len(videos), post=p, m=m_v)
    r = vd.get("recipe")
    vd["title"] = vd["vtitle"] if vd.get("vtitle") else (("Hướng dẫn nấu %s" % r["name"]) if r else p["title"][:150])
    vd["m_upd"] = None if vd["status"] == "PROCESSING" else max(1, m_v - rnd_between(1, 10))
    vd["views"] = rnd_between(20, 900) if vd["status"] == "READY" else rnd_between(0, 5)
    videos.append(vd)

# --- phiếu bầu ---
voter_pool = normal_authors
vote_set = set()
tries = 0
while len(votes) < 1200 and tries < 40000:
    tries += 1
    post = rng.choices(vis_posts, w_recent)[0]
    au = rng.choice(voter_pool)
    if au is post["user"] or (au["id"], post["id"]) in vote_set: continue
    hi = min(post["m"] - 2, au["m_created"] - 2)
    if hi < 2: continue
    vote_set.add((au["id"], post["id"]))
    votes.append(dict(id=BASE + len(votes), user=au, post=post, m=sample_m(2, hi)))
vcount = Counter(v_["post"]["id"] for v_ in votes)
for p in posts:
    if p["status"] == "APPROVED_ACTIVE" and p["user"] in normal_authors:
        age_d = p["m"] / float(DAY)
        p["views"] = max(rnd_between(3, 30) + int(age_d * rnd_between(1, 8)), vcount.get(p["id"], 0) * rnd_between(6, 20))
    elif p["status"] in ("QUARANTINED_HIDDEN", "DELETED"): p["views"] = rnd_between(10, 400)

# ------------------------------------------------------------------ 6. MEAL PLANS / SLOTS / SWAPS / SHOPPING
MEALS = [("BREAKFAST", "B", .25), ("LUNCH", "L", .35), ("DINNER", "D", .30), ("SNACK", "S", .10)]
def recipes_for(profile, letter):
    out = [r for r in REC_OK if letter in r["meals"]]
    if profile and (profile["allium_free"] or profile["school"] == "BUDDHIST"): out = [r for r in out if not r["has_allium"]]
    if profile:
        sc = profile["school"]
        if sc in ("VEGAN", "BUDDHIST"): out = [r for r in out if not (r["has_egg"] or r["has_dairy"])]
        elif sc == "OVO": out = [r for r in out if not r["has_dairy"]]
        elif sc == "LACTO": out = [r for r in out if not r["has_egg"]]
    return out
def pick_recipe(cands, want, avoid=()):
    c = [r for r in cands if r["id"] not in avoid] or cands
    return rng.choices(c, [1.0 / (0.2 + abs(r["kcal"] - want) / max(want, 1)) for r in c])[0]

plans, slots, swaps = [], [], []
plan_users = [u for u in prof_users if u["status"] == "ACTIVE"]
plan_status = ["ACTIVE"] * 16 + ["ARCHIVED"] * 30 + ["DRAFT"] * 10 + ["CANCELLED"] * 8 + ["TEMPLATE"] * 6
rng.shuffle(plan_status)
active_owner = set()
for st in plan_status:
    dur = rng.choice([7] * 8 + [5, 3])
    if st == "TEMPLATE":
        u = admin1; p = None; school = rng.choice(["VEGAN", "LACTO_OVO", "BUDDHIST"]); tgt = rng.choice([1800.0, 2000.0, 2200.0]); goal = "MAINTAIN"
        n = 0; m_plan = rnd_between(40 * DAY, 58 * DAY)
    else:
        for _ in range(200):
            u = rng.choice(plan_users); p = u["profile"]; maxn = int(u["m_created"] / DAY)
            if st == "ACTIVE" and (u["id"] in active_owner or maxn < 1): continue
            if st == "ARCHIVED" and maxn < dur + 1: continue
            break
        school, tgt, goal = p["school"], p["tgt"], p["goal"]
        maxn = int(u["m_created"] / DAY)
        if st == "ACTIVE": n = rnd_between(0, min(dur - 1, maxn)); active_owner.add(u["id"])
        elif st == "ARCHIVED": n = rnd_between(dur, maxn)
        elif st == "DRAFT": n = rnd_between(-3, min(maxn, 20))
        else: n = rnd_between(0, maxn)
        m_plan = max(3, min(u["m_created"] - 15, (n * DAY + rnd_between(30, 1500)) if n >= 0 else rnd_between(5, 2 * DAY)))
    pl = dict(id=BASE + len(plans), user=u, profile=p, status=st, dur=dur, n=n, m=m_plan, school=school, tgt=tgt, goal=goal,
              name="%s %d ngày #%d" % (("Mẫu thực đơn" if st == "TEMPLATE" else PLAN_NAMES[goal]), dur, len(plans) + 1),
              budget=rng.choice(["ECONOMICAL", "STANDARD", "STANDARD", "PREMIUM"]),
              instr=rng.choice([None, None, "Ít dầu mỡ", "Ưu tiên món nấu nhanh dưới 30 phút", "Không dùng ngũ vị tân", "Tăng đạm thực vật"]),
              ai=0 if st == "TEMPLATE" else (1 if rng.random() < .85 else 0), swaps=0)
    plans.append(pl)
    # --- slots
    used_by_day = {}
    for k in range(dur):
        for mt, letter, share in MEALS:
            if mt == "SNACK" and rng.random() > .4: continue
            cands = recipes_for(p, letter) or [r for r in REC_OK if letter in r["meals"]]
            r = pick_recipe(cands, tgt * share, used_by_day.setdefault(k, set()))
            used_by_day[k].add(r["id"])
            past = (pl["n"] >= 0 and k <= pl["n"])
            done = 1 if (st in ("ACTIVE", "ARCHIVED") and past and (k < pl["n"] or mt in ("BREAKFAST",)) and rng.random() < .78) else 0
            if st == "ARCHIVED" and rng.random() < .8 and past: done = 1 if rng.random() < .8 else 0
            slots.append(dict(id=BASE + len(slots), plan=pl, recipe=r, day=k, meal=mt, letter=letter, done=done,
                              note=rng.choice([None] * 9 + ["Ăn nhẹ trước 20h", "Chuẩn bị nguyên liệu từ tối hôm trước"]), swaps=[]))
# swaps
for pl in plans:
    if pl["status"] not in ("ACTIVE", "ARCHIVED"): continue
    pslots = [s for s in slots if s["plan"] is pl]
    for _ in range(rng.choice([0, 1, 2, 3, 3, 4, 5, 6])):
        s = rng.choice(pslots)
        cands = recipes_for(pl["profile"], s["letter"]) or REC_OK
        new = pick_recipe([r for r in cands if r["id"] != s["recipe"]["id"]] or cands, pl["tgt"] * dict((a, c) for _, a, c in MEALS)[s["letter"]])
        if new["id"] == s["recipe"]["id"]: continue
        last_m = s["swaps"][-1]["m"] if s["swaps"] else pl["m"] - 3
        if last_m <= 4: continue
        m = rnd_between(2, max(3, last_m - 2))
        sw = dict(id=BASE + len(swaps), slot=s, old=s["recipe"], new=new, m=m, reason=rng.choice(SWAP_REASONS), delta=r2(new["kcal"] - s["recipe"]["kcal"]), plan=pl)
        s["swaps"].append(sw); swaps.append(sw); s["recipe"] = new; pl["swaps"] += 1
# shopping lists
lists, items = [], []
sl_plans = [p for p in plans if p["status"] == "ACTIVE"][:12] + [p for p in plans if p["status"] == "ARCHIVED"][:5] + [p for p in plans if p["status"] == "CANCELLED"][:1]
for pl in sl_plans:
    st = {"ACTIVE": rng.choice(["PENDING"] * 7 + ["COMPLETED"] * 3), "ARCHIVED": rng.choice(["COMPLETED"] * 17 + ["CANCELLED"] * 3), "CANCELLED": "CANCELLED"}[pl["status"]]
    L = dict(id=BASE + len(lists), plan=pl, status=st, m=max(1, pl["m"] - rnd_between(5, 600)), days_ago=pl["n"] + rnd_between(0, 1),
             sync=rng.random() < .3, title="Danh sách đi chợ – %s" % pl["name"])
    lists.append(L)
    agg = {}
    for s in [s for s in slots if s["plan"] is pl]:
        for key, g, opt in s["recipe"]["lines"]:
            unit = "ml" if key in ML_KEYS else "g"
            agg[(key, unit)] = agg.get((key, unit), 0.0) + g / float(s["recipe"]["serv"])
    # danh sách gọn: bỏ gia vị/đồ pantry thường có sẵn, chỉ giữ tối đa 14 nguyên liệu chính (lượng nhiều nhất)
    keep = sorted(((k_, u_), q_) for (k_, u_), q_ in agg.items() if k_ not in SEAS and k_ not in ("dau_oil", "duong", "muoi", "dau_me"))
    keep = sorted(keep, key=lambda x: -x[1])[:14]
    for (key, unit), qty in sorted(keep):
        pur = 1 if st == "COMPLETED" else (0 if st == "CANCELLED" else (1 if rng.random() < .4 else 0))
        items.append(dict(id=BASE + len(items), list=L, key=key, qty=round(qty, 2), unit=unit, purchased=pur))

# ------------------------------------------------------------------ 7. PRODUCTS / SCANS
def ean13(prefix12):
    s = sum(int(c) * (3 if i % 2 else 1) for i, c in enumerate(prefix12))
    return prefix12 + str((10 - s % 10) % 10)
products = []; prod_enum = []; prod_alg = []
for i, (name, brand, text, codes, algs) in enumerate(PRODUCTS):
    codes = [c for c in codes if c in ENUM]
    items_ = [x.strip().lower() for x in re.split(r"[,()]", text)]
    animal = any(x in ("sữa", "trứng", "kem", "bơ", "bột sữa", "bột phô mai", "men sữa chua") or x.startswith("gelatin") for x in items_)
    st = [ENUM[c] for c in codes]
    verdict = "NON_VEGAN" if (animal or "NON_VEGAN" in st) else ("DOUBTFUL" if "DOUBTFUL" in st else "VEGAN")
    m = sample_m(10 * DAY, MAXM - 100)
    products.append(dict(id=BASE + i, barcode=ean13("89369" + "%07d" % (1000 + i * 37)), name=name, brand=brand, text=text, verdict=verdict,
                         verified=1 if rng.random() < .6 else 0, m=m, sync=max(1, m - rnd_between(0, 5000)),
                         image=None))
    for c in codes: prod_enum.append((BASE + i, c, m))
    for a, pr in algs: prod_alg.append((BASE + i, a, pr))
scan_users = [u for u in users if u["status"] == "ACTIVE" and u["role"] in ("MEMBER", "VIP") and u["m_created"] > 100]
scans = []
tries = 0
while len(scans) < 750 and tries < 10000:
    tries += 1
    pr = rng.choice(products)
    guest = rng.random() < .35
    if guest:
        hi = pr["m"] - 2; u = None
    else:
        u = rng.choice(scan_users); hi = min(pr["m"] - 2, u["m_created"] - 2)
    if hi < 3: continue
    scans.append(dict(id=BASE + len(scans), user=u, guest=str(uuid.UUID(int=rng.getrandbits(128), version=4)) if guest else None, prod=pr,
                      method="CAMERA_BARCODE" if rng.random() < .8 else "MANUAL_INPUT", trig=1 if pr["verdict"] == "NON_VEGAN" else 0, m=sample_m(2, hi)))

# ------------------------------------------------------------------ 8. FRIDGE
# chỉ nguyên liệu tươi đã có ảnh Cloudinary đã duyệt (ING_IMG) -> thumbnail tủ lạnh luôn có ảnh
FRIDGE_KEYS = ["cai_xanh", "cu_sen", "cu_dau", "gung", "hoa_chuoi", "kho_qua", "mang_tay", "nghe", "ngo_ri", "toi", "ot_chuong",
               "gia_do", "chanh", "dua_trai", "xoai", "bo_sap", "du_du", "hung"]
assert all(ING[k]["name"] in ING_IMG for k in FRIDGE_KEYS), "FRIDGE_KEYS có nguyên liệu chưa có ảnh"
fr_users = [u for u in users if u["status"] == "ACTIVE" and u["role"] in ("MEMBER", "VIP") and u["m_created"] > 3 * DAY]
free_fr = [u for u in fr_users if u["role"] == "MEMBER"]
heavy_fridge = rng.sample(free_fr, 2); heavy_chat = rng.sample([u for u in free_fr if u not in heavy_fridge], 2)
fr_sessions, fr_inv = [], []
day_cnt = Counter()
def add_fridge_session(u, m, items_n=None):
    n_it = items_n or rnd_between(3, 8)
    S = dict(id=BASE + len(fr_sessions), user=u, m=m, img=None, inv=[])
    for key in rng.sample(FRIDGE_KEYS, n_it):
        nonfresh = rng.random() < (0.5 if m > 10 * DAY else 0.12)
        unit = "g"; qty = float(rnd_between(100, 500) // 50 * 50)
        S["inv"].append(dict(id=BASE + len(fr_inv), user=u, key=key, session=S, fresh="NON-FRESH" if nonfresh else "FRESH", qty=qty, unit=unit))
        fr_inv.append(S["inv"][-1])
    fr_sessions.append(S); day_cnt[(u["id"], m // DAY)] += 1
    return S
for u in heavy_fridge:
    d = rnd_between(2, int(u["m_created"] / DAY) - 1)
    for j in range(10): add_fridge_session(u, d * DAY + rnd_between(0, DAY - 1), rnd_between(3, 5))
    u["heavy_fridge_day"] = d
tries = 0
while len(fr_sessions) < 90 and tries < 2000:
    tries += 1
    u = rng.choice(fr_users)
    if u in heavy_fridge: continue
    hi = u["m_created"] - 5
    m = sample_m(5, hi)
    cap = 30 if u["role"] == "VIP" else 10
    if day_cnt[(u["id"], m // DAY)] >= cap: continue
    add_fridge_session(u, m)
for _ in range(60):   # món nhập tay (không gắn session)
    u = rng.choice(fr_users); key = rng.choice(FRIDGE_KEYS)
    fr_inv.append(dict(id=BASE + len(fr_inv), user=u, key=key, session=None, fresh="FRESH" if rng.random() < .8 else "NON-FRESH", qty=float(rnd_between(100, 400) // 50 * 50), unit="g"))

# ---- gán avatar: nữ -> ảnh nữ, nam -> ảnh nam (đã duyệt bằng mắt), mỗi ảnh dùng 1 lần
AV_SKIP ={3, 5, 7, 11, 14, 50, 62, 70, 71, 72, 77, 102, 134, 166, 169, 172, 173, 181, 196} | AV_MALE
AV_KEYS = {}
for (kd_, k_), e_ in MEDIA.items():
    if kd_ == "avatar_image": AV_KEYS[int(k_.split("_")[1])] = e_["url"]
fem_pool = [AV_KEYS[i] for i in sorted(AV_KEYS) if i not in AV_SKIP]; male_pool = [AV_KEYS[i] for i in sorted(AV_KEYS) if i in AV_MALE]
rng.shuffle(fem_pool); rng.shuffle(male_pool)
for u in sorted(users, key=lambda u: u["status"] != "ACTIVE"):   # user ACTIVE nhận ảnh trước
    if u["status"] in ("SOFT_DELETED", "ANONYMIZED_DELETED") or u["kind"] == "DELETED": continue
    pool = fem_pool if u["female"] else male_pool
    if pool: u["avatar"] = pool.pop()
# ------------------------------------------------------------------ 9. VENUES
venues = []; venue_cats = []
for i, (name, addr, lat, lng, rating, hours, price, amen, fully, cats) in enumerate(VENUES):
    m = sample_m(10 * DAY, MAXM - 60)
    venues.append(dict(id=BASE + i, name=name, addr=addr, lat=lat, lng=lng, rating=rating, fully=fully, active=0 if i in (24, 25) and False else 1, m=m,
                       raw={"phone": "028%07d" % rnd_between(3000000, 3999999), "openingHours": hours, "priceRange": price, "amenities": amen, "seedSource": "seed60"}))
    for j, c in enumerate(cats): venue_cats.append((BASE + i, c, 1 if j == 0 else 0, m - rnd_between(0, 100) if m > 120 else m))
VENUE_OK = [2, 3, 4, 6, 11, 12, 13, 14, 16, 18, 19, 23, 24, 26, 28, 29, 30, 31, 32, 34, 37, 38, 41, 42, 44, 47]   # chỉ ảnh không gian quán/món chay đã duyệt
VN_KEYS = {}
for (kd_, k_), e_ in MEDIA.items():
    if kd_ == "venue_image": VN_KEYS[int(k_.split("_")[1])] = e_["url"]
for vv_, ix_ in zip(venues, VENUE_OK):
    if ix_ in VN_KEYS: vv_["thumb"] = VN_KEYS[ix_]
venues[23]["active"] = 0   # quán đã ngừng hoạt động (case Is_Active = 0)

# ------------------------------------------------------------------ 10. CHAT / AI QUOTA / SURVEYS
chat_users = [u for u in users if u["status"] == "ACTIVE" and u["role"] in ("MEMBER", "VIP") and u["m_created"] > 100]
sessions, messages, logs = [], [], []
chat_day = Counter()
def cap_of(u): return 200 if u["role"] == "VIP" else 20
def tok(): i = rnd_between(300, 1800); o = rnd_between(80, 650); return i, o
def cost(i, o): return round(i * 0.075 / 1e6 + o * 0.30 / 1e6, 4)
def add_log(u, feat, m, status="SUCCESS", i=None, o=None, err=None, ms=None):
    if i is None: i, o = tok()
    if status != "SUCCESS": i, o = 0, 0
    logs.append(dict(id=BASE + len(logs), user=u, feat=feat, tokens=i + o, cost=cost(i, o) if status == "SUCCESS" else 0.0, status=status, err=err,
                     ms=ms if ms is not None else rnd_between(900, 5200), m=m))
def add_session(u, m_start, pairs):
    guest = u is None
    S = dict(id=BASE + len(sessions), user=u, guest=str(uuid.UUID(int=rng.getrandbits(128), version=4)) if guest else None, m=m_start, msgs=[])
    m = m_start; first = None
    for j in range(pairs):
        if m < 3: break
        if u is not None:
            d = m // DAY
            if chat_day[(u["id"], d)] >= cap_of(u): break
        q_, a_ = rng.choice(CHAT_QA)
        rec = rng.sample(REC_OK, rnd_between(0, 2)) if rng.random() < .6 else []
        first = first or q_
        meta = {"sources": [{"type": "RECIPE", "id": r["id"], "title": r["title"][:150]} for r in rec], "ragUsed": bool(rec),
                "recommendedRecipes": [{"recipeId": r["id"], "title": r["title"][:150], "cookTimeMinutes": r["prep"] + r["cook"]} for r in rec]}
        ans = a_ + ("" if not rec else " Gợi ý thêm: " + "; ".join(r["name"] for r in rec) + ".")
        messages.append(dict(id=BASE + len(messages), session=S, sender="USER", text=q_, meta=None, m=m))
        messages.append(dict(id=BASE + len(messages), session=S, sender="ASSISTANT", text=ans, meta=meta, m=m - 1))
        S["msgs"].append(m)
        if u is not None:
            chat_day[(u["id"], m // DAY)] += 1
            add_log(u, "AI_CHAT", m - 1)
        m = m - 1 - rnd_between(1, 8)
    S["last"] = (S["msgs"][-1] - 1) if S["msgs"] else m_start
    S["title"] = (first or "Hội thoại mới")[:60]
    S["m_start"] = m_start
    if S["msgs"]: sessions.append(S)
    return S
# kịch bản chạm trần Free 20 tin/ngày -> QUOTA_EXCEEDED
for u in heavy_chat:
    d = rnd_between(3, int(u["m_created"] / DAY) - 1); base = d * DAY + 1200
    for j in range(4): add_session(u, base - j * 90, 5)
    u["heavy_chat_day"] = d
    for j in range(3): add_log(u, "AI_CHAT", d * DAY + 20 - j * 3, "QUOTA_EXCEEDED", err="Vượt hạn mức 20 tin nhắn/ngày (Free)", ms=15)
tries = 0
while len(sessions) < 240 and tries < 3000:
    tries += 1
    guest = rng.random() < .15
    if guest: add_session(None, sample_m(10, MAXM), rnd_between(1, 6)); continue
    u = rng.choice(chat_users)
    add_session(u, sample_m(10, u["m_created"] - 5), rnd_between(1, 9))
for S in sessions: S["profile"] = bool(S["user"] is not None and S["user"].get("profile"))
# nhật ký AI khác
for pl in plans:
    if pl["ai"] and pl["status"] != "TEMPLATE": add_log(pl["user"], "MEAL_PLAN_GEN", max(1, pl["m"] - 1), i=rnd_between(1200, 2500), o=rnd_between(2500, 4500), ms=rnd_between(6000, 18000))
for S in fr_sessions: add_log(S["user"], "AI_FRIDGE", max(1, S["m"] - 1), i=rnd_between(1500, 2600), o=rnd_between(300, 900), ms=rnd_between(2000, 7000))
for u in heavy_fridge:
    d = u["heavy_fridge_day"]
    for j in range(2): add_log(u, "AI_FRIDGE", d * DAY + 15 - j * 4, "QUOTA_EXCEEDED", err="Vượt hạn mức 10 lượt quét tủ lạnh/ngày (Free)", ms=12)
for vd in videos:
    au = vd["post"]["user"]
    if vd["status"] == "READY": add_log(au, "SUMMARIZE_VIDEO", max(1, vd["m"] - 3), i=rnd_between(2500, 6000), o=rnd_between(300, 900), ms=rnd_between(8000, 30000))
    elif vd["status"] == "FAILED": add_log(au, "SUMMARIZE_VIDEO", max(1, vd["m"] - 3), "ERROR", err="Whisper STT thất bại: định dạng âm thanh không hỗ trợ", ms=30000)
for rp in reports:
    if rp["src"] == "AI_SCAN":
        au = rp["owner"]; add_log(au, "CONTENT_MODERATION", max(1, rp["m"] - 0), i=rnd_between(400, 1200), o=rnd_between(50, 200))
for p in rng.sample([p for p in posts if p["status"] == "APPROVED_ACTIVE"], 60): add_log(p["user"], "CONTENT_MODERATION", max(1, p["m"] - 1), i=rnd_between(400, 1200), o=rnd_between(50, 200))
for r in REC_OK: add_log(r["user"], "AI_EMBEDDING", max(1, r["m"] - 1), i=rnd_between(200, 700), o=0, ms=rnd_between(200, 900))
for _ in range(15):
    u = rng.choice(chat_users); add_log(u, "AI_CHAT", sample_m(5, u["m_created"] - 5), "ERROR", err=rng.choice(["Gemini API timeout", "Upstream 503 Service Unavailable"]), ms=30000)
# surveys
surveys = []
for _ in range(60):
    u = rng.choice(chat_users)
    us = [S for S in sessions if S["user"] is u]
    S = rng.choice(us) if us and rng.random() < .7 else None
    m = max(1, (S["last"] - rnd_between(1, 60))) if S else sample_m(3, u["m_created"] - 3)
    if S and m >= S["last"]: m = max(1, S["last"] - 1)
    surveys.append(dict(id=BASE + len(surveys), user=u, session=S, rating=rng.choices([1, 2, 3, 4, 5], [5, 8, 17, 35, 35])[0], q=rng.choice(SURVEY_Q), fb=rng.choice(SURVEY_FB), m=m))

# ================================================================== 11. EMIT SQL
def tsx(m):
    if m is None: return "CAST(NULL AS DATETIME2(7))"
    return ts(m) if m >= 0 else tsf(-m)
def tsn(m):  # NULL-able dùng trong INSERT..VALUES thường
    return "NULL" if m is None else tsx(m)
def sha(s): return hashlib.sha256(s.encode()).hexdigest()
def uid_of(u): return str(u["id"])

# ---- header / cleanup / lookups
H = part("00_header")
H.append("""-- Vá schema: DB dựng từ create_all_tables.sql cũ có thể thiếu cột này (entity FridgeSession.DetectedDataJson cần nó)
IF COL_LENGTH(N'dbo.FridgeSessions', N'Detected_Data_JSON') IS NULL
    ALTER TABLE dbo.FridgeSessions ADD Detected_Data_JSON NVARCHAR(MAX) NULL;
IF COL_LENGTH(N'dbo.FridgeInventory', N'Thumbnail_URL') IS NULL
    ALTER TABLE dbo.FridgeInventory ADD Thumbnail_URL NVARCHAR(MAX) NULL;
GO
-- ============================================================================
-- SEED60 - bộ dữ liệu mock 60 ngày cho VeggieLife (SQL Server). SINH TỰ ĐỘNG bởi generate_seed60.py (seed=%d)
-- Dải ID 10001+; email @%s; chạy lại nhiều lần an toàn (xóa theo dải ID rồi nạp lại).
-- LƯU Ý: dọn dẹp xóa MỌI dòng có ID >= 10001 ở các bảng trên (kể cả dòng app tạo sau khi nạp seed).
-- File lưu UTF-8: chạy bằng sqlcmd cần cờ -f 65001.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
DECLARE @Now DATETIME2(7) = GETUTCDATE();
DECLARE @Dow0 INT = DATEDIFF(DAY, '19000101', CAST(@Now AS DATE)) %% 7;   -- 0 = thứ Hai
PRINT N'>> SEED60 bắt đầu lúc ' + CONVERT(NVARCHAR(30), @Now, 121) + N' (UTC)';
BEGIN TRANSACTION;""" % (ARGS.seed, EMAIL_DOMAIN))

CL = part("01_cleanup")
CL.append("PRINT N'>> [1/3] Dọn dữ liệu seed cũ (Level 2 -> Level 1, dải ID 10001-99999; user theo email seed)...';")
# Dải seed cố định 10001-99999; app tạo dữ liệu mới từ 100000 (xem footer) nên không bị xóa nhầm.
# Bảng gắn User_ID chỉ xóa user seed (email @seed60...) để giữ tài khoản thật lỡ rơi vào dải trước khi có footer đẩy identity.
SEED_USERS = "User_ID IN (SELECT User_ID FROM dbo.Users WHERE Email LIKE N'%%@%s')" % EMAIL_DOMAIN
for tbl, col, extra in [
    ("MealSlotSwapHistories", "Swap_History_ID", ""), ("ShoppingListItems", "Item_ID", ""), ("ShoppingLists", "List_ID", ""),
    ("MealSlots", "Slot_ID", ""), ("MealPlans", "Plan_ID", ""), ("ViolationReports", "Report_ID", ""), ("Votes", "Vote_ID", ""),
    ("Comments", "Comment_ID", " AND Parent_Comment_ID IS NOT NULL"), ("Comments", "Comment_ID", ""),
    ("VideoTranscripts", "Transcript_ID", ""), ("Videos", "Video_ID", ""), ("Posts", "Post_ID", ""), ("Surveys", "Survey_ID", ""),
    ("ChatMessages", "Message_ID", ""), ("ChatSessions", "Chat_Session_ID", ""), ("AIQuotaLogs", "Log_ID", ""),
    ("UserScanHistory", "Scan_ID", ""), ("FridgeInventory", "Inventory_ID", ""), ("FridgeSessions", "Session_ID", ""),
    ("Product_ENumbers", "Product_ID", ""), ("ProductAllergens", "Product_ID", ""), ("Products", "Product_ID", ""),
    ("VenueCategories", "Venue_ID", ""), ("VeganVenues", "Venue_ID", ""), ("RecipeIngredients", "Recipe_ID", ""), ("Recipes", "Recipe_ID", ""), ("Foods", "Food_ID", ""),
    ("IngredientAllergens", "Ingredient_ID", ""), ("Ingredients", "Ingredient_ID", ""), ("PaymentTransactions", "Transaction_ID", ""),
    ("UserAllergies", None, ""), ("UserWeightHistory", "Log_ID", ""), ("HealthProfiles", None, ""),
    ("AuditLogs", "Audit_ID", ""), ("Users", None, "")]:
    where = SEED_USERS if col is None else "%s BETWEEN 10001 AND 99999%s" % (col, extra)
    CL.append("DELETE FROM dbo.%s WHERE %s;" % (tbl, where))
CL.append("PRINT N'   dọn xong.';")

LK = part("02_lookups")
LK.append("""PRINT N'>> [2/3] Tra cứu dữ liệu master theo tên/mã...';
SELECT Dietary_School_ID, School_Code INTO #sch FROM dbo.DietarySchools;
SELECT Category_ID, Category_Name INTO #cat FROM dbo.FoodCategories;
SELECT Allergen_ID, Code INTO #alg FROM dbo.Allergens;
SELECT Duration_Days, MIN(Plan_ID) AS Plan_ID, MAX(Price) AS Price INTO #plan FROM dbo.SubscriptionPlans WHERE Duration_Days IN (30, 90, 365) GROUP BY Duration_Days;
IF (SELECT COUNT(*) FROM #sch) < 7   THROW 50010, N'Thiếu DietarySchools - hãy chạy seed master (01..08) trước.', 1;
IF (SELECT COUNT(*) FROM #cat) < 14  THROW 50011, N'Thiếu FoodCategories - hãy chạy seed master trước.', 1;
IF (SELECT COUNT(*) FROM #alg) < 10  THROW 50012, N'Thiếu Allergens - hãy chạy seed master trước.', 1;
IF (SELECT COUNT(*) FROM #plan) < 3  THROW 50013, N'Thiếu SubscriptionPlans 30/90/365 ngày - hãy chạy 08_Seed_SubscriptionPlans.sql trước.', 1;
IF (SELECT COUNT(*) FROM dbo.Ingredients WHERE Ingredient_ID < 10001) < 100 THROW 50014, N'Thiếu Ingredients master - hãy chạy 04_Seed_Ingredients.sql trước.', 1;""")

# ---- Level 0: nguyên liệu bổ sung (chèn nếu tên chưa có)
L0 = part("10_level0_ingredients")
L0.append("PRINT N'>> [3/3] Nạp dữ liệu seed...';\nSET IDENTITY_INSERT dbo.Ingredients ON;")
rows = []
for i, (k, s) in enumerate(SUPP.items()):
    rows.append("(%d, %s, N'g', %s, %s, %d, %s, %s, 0, %s)" % (BASE + i, q(s[0]), v(s[1]), s[2], s[3], v(s[4]), v(ING_IMG[s[0]]) if s[0] in ING_IMG else "NULL", ts(86000)))
L0.append("INSERT INTO dbo.Ingredients (Ingredient_ID, Ingredient_Name, Standard_Unit, Default_Stall_Type, Calories_Per_100g, Is_Allium, Food_Group, Image_Url, Is_Locked, Created_At)\n"
          "SELECT x.* FROM (VALUES\n%s\n) AS x(id, nm, unit, stall, kcal, allium, fg, img, locked, created)\nWHERE NOT EXISTS (SELECT 1 FROM dbo.Ingredients e WHERE e.Ingredient_Name = x.nm);" % ",\n".join(rows))
L0.append("SET IDENTITY_INSERT dbo.Ingredients OFF;")
used_names = sorted(set(ING[k]["name"] for k in ING))
L0.append("SELECT Ingredient_ID, Ingredient_Name INTO #ing FROM dbo.Ingredients;")
L0.append("IF EXISTS (SELECT 1 FROM (VALUES %s) n(nm) LEFT JOIN #ing i ON i.Ingredient_Name = n.nm WHERE i.Ingredient_ID IS NULL) THROW 50015, N'Thiếu nguyên liệu master mà công thức seed60 cần (kiểm tra 04_Seed_Ingredients.sql).', 1;" % ", ".join("(%s)" % q(n_) for n_ in used_names))
L0.append("PRINT N'   + Ingredients bổ sung (nếu chưa có): %d';" % len(SUPP))

# ---- Level 1: users
U = part("11_level1_users"); rows = []
for u in users:
    mc = u["m_created"]; st = u["status"]
    login = None if st == "PENDING_ACTIVATION" else rnd_between(1, max(1, min(14 * DAY, mc - 1)))
    if "lockout" in u["flags"]: fl, lk = 5, tsf(rnd_between(5, 12))
    else: fl, lk = (rnd_between(1, 3) if rng.random() < .08 else 0), (tsx(u["locked"]) if u.get("locked") is not None else "NULL")
    if st in ("SUSPENDED", "BANNED") or u in exp3 or u in exp10: upd = max(1, u["m_last"] - 1)
    elif st in ("SOFT_DELETED", "ANONYMIZED_DELETED"): upd = rnd_between(1, max(1, min(mc - 1, 20 * DAY)))
    elif st == "QUARANTINED": upd = rnd_between(1, min(mc - 1, 2 * DAY))
    else: upd = rnd_between(1, max(1, mc - 1)) if rng.random() < .65 else None
    pwd = "NULL" if (u["google"] or st == "ANONYMIZED_DELETED") else q(HASH)
    rows.append([str(u["id"]), v(u["email"]), pwd, q(u["name"]), nn(v(u["phone"]) if u["phone"] else None), v(u["avatar"]) if u.get("avatar") else "NULL", v(u["role"]), str(fl), lk,
                 ts(login) if login else "NULL", ts(mc), ts(upd) if upd else "NULL", v(u["google"]) if u["google"] else "NULL", v(st), str(u["count"])])
insert(U, "Users", ["User_ID", "Email", "Password_Hash", "Full_Name", "Phone_Number", "Avatar_Url", "Role", "Failed_Login_Count", "Locked_Until", "Last_Login_At",
                    "Created_At", "Updated_At", "Google_ID", "Account_Status", "Count_ViolationReports"], rows, identity=True)

# ---- profiles / weights / allergies
HP = part("12_level1_health")
rows = [[str(p["user"]["id"]), v(p["school"]), v("MALE" if p["male"] else "FEMALE"), v(p["dob"].isoformat()), str(p["h"]), str(p["cur"]), str(p["tw"]), str(p["bmi"]),
         str(p["bmr"]), str(p["tdee"]), str(p["tgt"]), bit(p["allium_free"]), v(p["region"]), ts(p["m_prof"]), ts(p["m_upd"]), bit(p["stale"]), v(p["goal"])] for p in profiles]
insert_join(HP, "HealthProfiles", ["User_ID", "Dietary_School_ID", "Gender", "Date_Of_Birth", "Height_Cm", "Current_Weight_Kg", "Target_Weight_Kg", "BMI_Value", "BMR_Kcal",
            "TDEE_Kcal", "Target_Daily_Calories", "Requires_Allium_Free", "Region_Preference", "Created_At", "Updated_At", "Is_Stale_Metrics", "HealthGoal"],
            "v.uid, s.Dietary_School_ID, v.g, CAST(v.dob AS DATE), v.h, v.cw, v.tw, v.bmi, v.bmr, v.tdee, v.tgt, v.af, v.rg, v.cr, v.up, v.st, v.hg",
            ["uid", "sc", "g", "dob", "h", "cw", "tw", "bmi", "bmr", "tdee", "tgt", "af", "rg", "cr", "up", "st", "hg"], rows, "JOIN #sch s ON s.School_Code = v.sc")
rows = []
for i, w in enumerate(weights):
    rows.append([str(BASE + i), str(w["user"]["id"]), "%.2f" % w["w"], dts(w["m"] // DAY), q(w["note"]) if w["note"] else "NULL", ts(w["m"])])
insert(HP, "UserWeightHistory", ["Log_ID", "User_ID", "Weight_Kg", "Logged_Date", "Note", "Created_At"], rows, identity=True)
rows = [[str(a["user"]["id"]), v(a["code"]), q(a["note"]) if a["note"] else "NULL", ts(a["m"]), ts(a["m"])] for a in allergies]
insert_join(HP, "UserAllergies", ["User_ID", "Allergen_ID", "Notes", "Created_At", "Updated_At"], "v.uid, a.Allergen_ID, v.nt, v.cr, v.up",
            ["uid", "ac", "nt", "cr", "up"], rows, "JOIN #alg a ON a.Code = v.ac")

# ---- payments
PY = part("13_level1_payments"); rows = []
for t in txs:
    code = "SEED60-%s-%06d" % (t["gw"], t["id"] - BASE + 1)
    rows.append([str(t["id"]), str(t["user"]["id"]), str(t["dur"]), v(t["gw"]), v(code), v(t["status"]), v(sha(code)), tsx(t["m_create"]), tsx(t["m_done"]), tsx(t["m_start"]), tsx(t["m_end"])])
insert_join(PY, "PaymentTransactions", ["Transaction_ID", "Plan_ID", "User_ID", "Gateway_Provider", "Partner_Transaction_Code", "Amount_VND", "Transaction_Status", "Signature_Hash",
            "Created_At", "Completed_At", "Receipt_URL", "Starts_At", "Ends_At"], "v.tid, p.Plan_ID, v.uid, v.gw, v.code, p.Price, v.st, v.sig, v.cr, v.done, NULL, v.sa, v.ea",
            ["tid", "uid", "dur", "gw", "code", "st", "sig", "cr", "done", "sa", "ea"], rows, "JOIN #plan p ON p.Duration_Days = v.dur", identity=True)

# ---- recipes
RC = part("14_level1_recipes"); rows = []
# Ảnh món (Foods.Image_URL) = ảnh sách của công thức đầu tiên thuộc món đó
FOOD_IMG = {}
for r_ in recipes:
    if r_["image"]: FOOD_IMG.setdefault(r_["food"]["id"], r_["image"])
frows = ["(%d, %s, %s, %s, %s)" % (f["id"], q(f["cat"]), q(f["name"]), v(FOOD_IMG[f["id"]]) if f["id"] in FOOD_IMG else "CAST(NULL AS NVARCHAR(500))", ts(86000)) for f in foods]
RC.append("SET IDENTITY_INSERT dbo.Foods ON;")
RC.append("SELECT x.id, c.Category_ID, x.nm, x.img, x.cr INTO #foodsrc FROM (VALUES\n%s\n) AS x(id, catnm, nm, img, cr)\nJOIN #cat c ON c.Category_Name = x.catnm;" % ",\n".join(frows))
RC.append("INSERT INTO dbo.Foods (Food_ID, Category_ID, Food_Name, Image_URL, Created_At)\nSELECT id, Category_ID, nm, img, cr FROM #foodsrc x WHERE NOT EXISTS (SELECT 1 FROM dbo.Foods e WHERE e.Food_Name = x.nm);")
RC.append("SET IDENTITY_INSERT dbo.Foods OFF;")
# Món cùng tên đã có sẵn trong DB mà chưa có ảnh thì gắn ảnh sách
RC.append("UPDATE e SET e.Image_URL = x.img FROM dbo.Foods e JOIN #foodsrc x ON x.nm = e.Food_Name WHERE e.Image_URL IS NULL AND x.img IS NOT NULL;")
RC.append("DROP TABLE #foodsrc;")
RC.append("SELECT Food_ID, Food_Name INTO #food FROM dbo.Foods;")
RC.append("PRINT N'   + Foods (món ăn khái niệm từ sách): %d';" % len(foods)); COUNTS["Foods"] = len(foods)
for r in recipes:
    desc = "%s – công thức từ sách Tuyệt kỹ 500 công thức nấu ăn chay (%s), phù hợp người %s, khoảng %s kcal mỗi khẩu phần (%d khẩu phần)." % (r["name"], r["ref"], GOAL_LABEL[r["goal"]], r["kcal"], r["serv"])
    rows.append([str(r["id"]), q(r["food"]["name"]), str(r["user"]["id"]), v(r["school"]), q(r["title"]), q(desc), q(r["instr"]), str(r["prep"]), str(r["cook"]), str(r["serv"]),
                 str(r["kcal"]), v(r["diff"]), bit(r["verified"]), v(r["image"]), v(r["status"]), str(r["views"]), v(r["src_db"]), ts(r["m"]), tsx(r["m_upd"])])
# Schema mới: Recipes không còn Category_ID, danh mục lấy qua Foods.Category_ID
insert_join(RC, "Recipes", ["Recipe_ID", "Food_ID", "User_ID", "Dietary_School_ID", "Title", "Description", "Instructions", "Prep_Time_Minutes", "Cook_Time_Minutes", "Servings",
            "Calories_Kcal", "Difficulty_Level", "Is_Verified", "Image_Url", "Status", "View_Count", "Source", "Created_At", "Updated_At"],
            "v.rid, f.Food_ID, v.uid, s.Dietary_School_ID, v.ti, v.de, v.ins, v.pr, v.ck, v.sv, v.kc, v.df, v.vf, v.im, v.st, v.vw, v.sr, v.cr, v.up",
            ["rid", "fn", "uid", "sc", "ti", "de", "ins", "pr", "ck", "sv", "kc", "df", "vf", "im", "st", "vw", "sr", "cr", "up"], rows,
            "JOIN #food f ON f.Food_Name = v.fn\nJOIN #sch s ON s.School_Code = v.sc", identity=True, batch=60)
NOTES = ["Rửa sạch, cắt khúc", "Thái lát mỏng", "Băm nhỏ", "Ngâm nước ấm", "Cắt miếng vừa ăn", None, None, None]
rows = []
for r in recipes:
    for key, g, opt in r["lines"]:
        unit = "ml" if key in ML_KEYS else "g"
        nt_ = rng.choice(NOTES)
        rows.append([str(r["id"]), q(ING[key]["name"]), "%.2f" % g, q(unit), q(nt_) if nt_ else "NULL", bit(opt)])
insert_join(RC, "RecipeIngredients", ["Recipe_ID", "Ingredient_ID", "Quantity_Needed", "Unit", "Preparation_Note", "Is_Optional"], "v.rid, i.Ingredient_ID, v.qty, v.u, v.nt, v.op",
            ["rid", "nm", "qty", "u", "nt", "op"], rows, "JOIN #ing i ON i.Ingredient_Name = v.nm")

# ---- products + venues
PV = part("15_level1_products_venues")
rows = [[str(p["id"]), v(p["barcode"]), q(p["name"]), q(p["brand"]), q(p["text"]), v(p["image"]) if p["image"] else "NULL", v(p["verdict"]), bit(p["verified"]), ts(p["m"]), ts(p["sync"])] for p in products]
insert(PV, "Products", ["Product_ID", "Barcode", "Product_Name", "Brand", "Ingredients_Text", "Image_Url", "Vegan_Verdict", "Admin_Verified", "Created_At", "Last_Synced_At"], rows, identity=True)
insert(PV, "Product_ENumbers", ["Product_ID", "Code", "Created_At"], [[str(pid), v(c), ts(m)] for pid, c, m in prod_enum])
insert_join(PV, "ProductAllergens", ["Product_ID", "Allergen_ID", "Presence_Type"], "v.pid, a.Allergen_ID, v.pt", ["pid", "ac", "pt"],
            [[str(pid), v(a), v(pr)] for pid, a, pr in prod_alg], "JOIN #alg a ON a.Code = v.ac")
rows = [[str(x["id"]), "'GOONG'", v("goong_seed60_%03d" % (x["id"] - BASE + 1)), q(x["name"]), q(x["addr"]), "%.6f" % x["lat"], "%.6f" % x["lng"], "%.1f" % x["rating"],
         v(x["thumb"]) if x.get("thumb") else "NULL", bit(x["fully"]), bit(x["active"]), js(x["raw"]), ts(x["m"])] for x in venues]
insert(PV, "VeganVenues", ["Venue_ID", "External_Source", "External_Place_ID", "Venue_Name", "Address", "Latitude", "Longitude", "Rating", "Thumbnail_Url", "Is_Fully_Vegan", "Is_Active",
                           "Raw_Origin_JSON", "Created_At"], rows, identity=True)
insert_join(PV, "VenueCategories", ["Category_ID", "Venue_ID", "Is_Primary", "Assigned_At"], "c.Category_ID, v.vid, v.pr, v.at", ["vid", "cn", "pr", "at"],
            [[str(vid), q(cn), bit(pr), ts(m)] for vid, cn, pr, m in venue_cats], "JOIN #cat c ON c.Category_Name = v.cn")

# ---- community
CM = part("20_level2_community")
rows = []
for p in posts:
    rows.append([str(p["id"]), str(p["user"]["id"]), nn(p["recipe"]), q(p["title"]), q(p["body"]), v(p["image"]) if p["image"] else "NULL", v(p["vis"]), str(p["views"]), v(p["status"]),
                 ts(p["m"]), ts(p["m_upd"]) if p["m_upd"] else "NULL"])
insert(CM, "Posts", ["Post_ID", "User_ID", "Recipe_ID", "Title", "Content", "Image_Url", "Visibility", "View_Count", "Moderation_Status", "Created_At", "Updated_At"], rows, identity=True)
rows = []; trows = []
for vd in videos:
    rows.append([str(vd["id"]), str(vd["post"]["id"]), q(vd["title"]), v(vid_url(vd["key"])), v(vid_thumb(vd["key"])), str(vd["dur"]), str(vd["views"]), v(vd["status"]), ts(vd["m"]), ts(vd["m_upd"]) if vd["m_upd"] else "NULL"])
    r = vd.get("recipe"); tm = vd["m"] - rnd_between(1, 3)
    if vd["status"] == "BANNED":
        raw = "Chào các bạn, hôm nay mình làm món chay đặc biệt. Mình cho thêm chút thịt băm vào cho đậm vị, rồi nêm nếm và cho ra đĩa."; js_ = "NULL"; ts_ = "FAILED"; summ = "Video có nhắc đến thịt trong món chay, vi phạm quy chuẩn ẩm thực chay."; rid = "NULL"
        comp = ts(max(1, tm - 2))
    elif vd["status"] == "READY":
        steps = [s_.split(". ", 1)[1] for s_ in r["instr"].split("\n")]
        raw = "Chào các bạn, hôm nay mình hướng dẫn nấu %s. %s Chúc các bạn thành công!" % (r["name"], " ".join(steps))
        ej = {"title": r["name"], "servings": r["serv"], "cookTimeMinutes": r["prep"] + r["cook"],
              "ingredients": [{"name": SHORT(ING[k]["name"]), "quantity": g} for k, g, _ in r["lines"]], "steps": steps, "tips": ["Chọn nguyên liệu tươi để món ngon hơn."]}
        js_ = js(ej); ts_ = "COMPLETED"; summ = "Hướng dẫn nấu %s với %d nguyên liệu chính, khoảng %d kcal mỗi khẩu phần." % (r["name"], len(r["lines"]), r["kcal"]); rid = str(r["id"])
        comp = ts(max(1, tm - 2))
    elif vd["status"] == "PROCESSING":
        raw = "NULL"; js_ = "NULL"; ts_ = "PENDING"; summ = "NULL"; rid = "NULL"; comp = "NULL"
    else:
        raw = "NULL"; js_ = "NULL"; ts_ = "FAILED"; summ = "NULL"; rid = "NULL"; comp = "NULL"
    trows.append([str(vd["id"]), str(vd["id"]), rid, q(raw) if raw != "NULL" else "NULL", js_, q(summ) if summ != "NULL" else "NULL", "'whisper-large-v3'" if vd["status"] in ("READY", "BANNED") else "NULL", v(ts_), ts(tm), comp])
insert(CM, "Videos", ["Video_ID", "Post_ID", "Title", "Video_Url", "Thumbnail_Url", "Duration_Seconds", "View_Count", "Status", "Created_At", "Updated_At"], rows, identity=True)
insert(CM, "VideoTranscripts", ["Transcript_ID", "Video_ID", "Recipe_ID", "Raw_Transcript", "Extracted_Recipe_JSON", "AI_Summary", "STT_Model_Version", "Status", "Created_At", "Completed_At"], trows, identity=True)
rows = [[str(c["id"]), str(c["user"]["id"]), nn(c["parent"]["id"] if c["parent"] else None), str(c["post"]["id"]), q(c["text"]), bit(c["soft"]), v(c["status"]), ts(c["m"]),
         ts(c["m_upd"]) if c["m_upd"] else "NULL"] for c in comments]
insert(CM, "Comments", ["Comment_ID", "User_ID", "Parent_Comment_ID", "Post_ID", "Content", "Is_Soft_Deleted", "Status", "Created_At", "Updated_At"], rows, identity=True)
insert(CM, "Votes", ["Vote_ID", "User_ID", "Post_ID", "Created_At"], [[str(x["id"]), str(x["user"]["id"]), str(x["post"]["id"]), ts(x["m"])] for x in votes], identity=True)
rows = []
for rp in reports:
    t = rp["target"]; tgt = {"recipe": "NULL, NULL, NULL, NULL", "comment": "NULL, %d, NULL, NULL" % t["id"], "post": "NULL, NULL, %d, NULL" % t["id"], "video": "NULL, NULL, NULL, %d" % t["id"]}
    if rp["kind"] == "recipe": cols = [str(t["id"]), "NULL", "NULL", "NULL"]
    elif rp["kind"] == "comment": cols = ["NULL", str(t["id"]), "NULL", "NULL"]
    elif rp["kind"] == "post": cols = ["NULL", "NULL", str(t["id"]), "NULL"]
    else: cols = ["NULL", "NULL", "NULL", str(t["video"]["id"] if "video" in t and t.get("video") else t["id"])]
    ev = EVID[rp["type"]] + ("“%s”" % (t.get("text") or t.get("title") or t.get("name") or "")[:120])
    rows.append([str(rp["id"]), str(rp["reporter"]["id"]) if rp["reporter"] else "NULL", str(rp["admin"]["id"]) if rp["admin"] else "NULL"] + cols +
                [v(rp["type"]), q(ev[:500]), v(rp["src"]), v(rp["status"]), ts(rp["m"])])
insert(CM, "ViolationReports", ["Report_ID", "Reporter_User_ID", "Admin_ID", "Recipe_ID", "Comment_ID", "Post_ID", "Video_ID", "Violation_Type", "Evidence_Summary", "Report_Source", "Status", "Created_At"], rows, identity=True)

# ---- meal plans
MP = part("21_level2_mealplans")
rows = []
for pl in plans:
    sd = pl["n"]
    rows.append([str(pl["id"]), str(pl["user"]["id"]), v(pl["school"]), q(pl["name"]), dts(sd), dts(sd - (pl["dur"] - 1)), str(pl["tgt"]), v(pl["budget"]),
                 q(pl["instr"]) if pl["instr"] else "NULL", v(pl["status"]), bit(pl["ai"]), "21", str(21 - pl["swaps"]), str(pl["dur"]), ts(pl["m"])])
insert_join(MP, "MealPlans", ["Plan_ID", "User_ID", "Dietary_School_ID", "Plan_Name", "Start_Date", "End_Date", "Target_Daily_Kcal", "Budget_Tier", "Custom_Instructions", "Plan_Status",
            "Generated_By_AI", "Max_Swaps_Allowed", "Swaps_Remaining", "Duration_Days", "Created_At"],
            "v.pid, v.uid, s.Dietary_School_ID, v.nm, CAST(v.sd AS DATE), CAST(v.ed AS DATE), v.tk, v.bt, v.ci, v.ps, v.ai, v.ms, v.sr, v.dd, v.cr",
            ["pid", "uid", "sc", "nm", "sd", "ed", "tk", "bt", "ci", "ps", "ai", "ms", "sr", "dd", "cr"], rows, "JOIN #sch s ON s.School_Code = v.sc", identity=True)
DOW = "CHOOSE(((@Dow0-(%d)+(%d))%%7+7)%%7+1,'MON','TUE','WED','THU','FRI','SAT','SUN')"
rows = [[str(s["id"]), str(s["plan"]["id"]), str(s["recipe"]["id"]), DOW % (s["plan"]["n"], s["day"]), v(s["meal"]), bit(s["done"]), q(s["note"]) if s["note"] else "NULL"] for s in slots]
insert(MP, "MealSlots", ["Slot_ID", "Plan_ID", "Recipe_ID", "Day_Of_Week", "Meal_Type", "Is_Completed", "Notes"], rows, identity=True)
rows = [[str(x["id"]), str(x["slot"]["id"]), str(x["old"]["id"]), str(x["new"]["id"]), q(x["reason"]), str(x["delta"]), ts(x["m"])] for x in swaps]
insert(MP, "MealSlotSwapHistories", ["Swap_History_ID", "Slot_ID", "Old_Recipe_ID", "New_Recipe_ID", "Swap_Reason", "Calories_Delta", "Swapped_At"], rows, identity=True)
rows = [[str(L["id"]), str(L["plan"]["id"]), q(L["title"][:150]), dts(L["days_ago"]), v(L["status"]), bit(L["sync"]), ts(L["m"])] for L in lists]
insert(MP, "ShoppingLists", ["List_ID", "Plan_ID", "List_Title", "Target_Shopping_Date", "Status", "Is_Offline_Synced", "Created_At"], rows, identity=True)
rows = [[str(i_["id"]), str(i_["list"]["id"]), q(ING[i_["key"]]["name"]), "%.2f" % i_["qty"], q(i_["unit"]), bit(i_["purchased"])] for i_ in items]
insert_join(MP, "ShoppingListItems", ["Item_ID", "List_ID", "Ingredient_ID", "Aggregated_Quantity", "Unit", "Is_Purchased"], "v.iid, v.lid, i.Ingredient_ID, v.qty, v.u, v.pu",
            ["iid", "lid", "nm", "qty", "u", "pu"], rows, "JOIN #ing i ON i.Ingredient_Name = v.nm", identity=True, batch=300)

# ---- fridge + scans
FS = part("22_level2_fridge_scan")
rows = []
for S in fr_sessions:
    det = []
    for it in S["inv"]:
        nm = SHORT(ING[it["key"]]["name"])
        det.append({"inventoryId": it["id"], "name": nm, "freshnessStatus": it["fresh"], "quantity": "%d g" % int(it["qty"]), "amount": it["qty"], "unit": it["unit"],
                    "box2d": [rnd_between(0, 400), rnd_between(0, 400), rnd_between(450, 900), rnd_between(450, 900)], "foodGroup": "PLANT",
                    "isAnimalProduct": False, "isRedMeat": False, "confidence": round(rng.uniform(.8, .99), 2)})
    rows.append([str(S["id"]), str(S["user"]["id"]), v(S["img"] or next((ING_IMG[ING[it["key"]]["name"]] for it in S["inv"] if ING[it["key"]]["name"] in ING_IMG), sorted(ING_IMG.values())[S["id"] % len(ING_IMG)])), str(len(det)), js(det), ts(S["m"])])
insert(FS, "FridgeSessions", ["Session_ID", "User_ID", "Image_CDN_URL", "Detected_Items_Count", "Detected_Data_JSON", "Created_At"], rows, identity=True)
rows = [[str(x["id"]), str(x["user"]["id"]), q(SHORT(ING[x["key"]]["name"])), nn(x["session"]["id"] if x["session"] else None), v(x["fresh"]), "%.2f" % x["qty"], q(x["unit"])] for x in fr_inv]
insert_join(FS, "FridgeInventory", ["Inventory_ID", "User_ID", "Ingredient_ID", "Session_ID", "Custom_Item_Name", "Freshness_Status", "Quantity", "Unit", "Thumbnail_URL"],
            "v.iid, v.uid, i.Ingredient_ID, v.sid, v.cn, v.fs, v.qty, v.u, v.th",["iid", "uid", "nm", "sid", "cn", "fs", "qty", "u", "th"],
            [[r_[0], r_[1], q(ING[x["key"]]["name"]), r_[3], r_[2], r_[4], r_[5], r_[6],
              v(ING_IMG[ING[x["key"]]["name"]])]   # thumbnail = ảnh nguyên liệu
             for r_, x in zip(rows, fr_inv)], "JOIN #ing i ON i.Ingredient_Name = v.nm", identity=True, batch=300)
rows = [[str(x["id"]), str(x["user"]["id"]) if x["user"] else "NULL", v(x["guest"]) if x["guest"] else "NULL", str(x["prod"]["id"]), v(x["method"]), bit(x["trig"]), ts(x["m"])] for x in scans]
insert(FS, "UserScanHistory", ["Scan_ID", "User_ID", "Guest_UUID", "Product_ID", "Scan_Method", "Is_Non_Vegan_Triggered", "Scanned_At"], rows, identity=True)

# ---- chat + AI quota + surveys
CH = part("23_level2_chat_ai")
rows = [[str(S["id"]), str(S["user"]["id"]) if S["user"] else "NULL", str(S["user"]["id"]) if (S["user"] and S["profile"]) else "NULL", v(S["guest"]) if S["guest"] else "NULL",
         q(S["title"]), "'NUTRITION_CONSULTATION'", ts(S["m_start"]), ts(S["last"])] for S in sessions]
insert(CH, "ChatSessions", ["Chat_Session_ID", "User_ID", "Profiles_ID", "Guest_UUID", "Session_Title", "Context_Type", "Started_At", "Updated_At"], rows, identity=True)
rows = [[str(x["id"]), str(x["session"]["id"]), v(x["sender"]), q(x["text"]), js(x["meta"]) if x["meta"] else "NULL", ts(x["m"])] for x in messages]
insert(CH, "ChatMessages", ["Message_ID", "Chat_Session_ID", "Sender_Type", "Content", "Metadata_JSON", "Created_At"], rows, identity=True)
rows = [[str(l["id"]), str(l["user"]["id"]), v(l["feat"]), str(l["tokens"]), "%.4f" % l["cost"], v(l["status"]), q(l["err"]) if l["err"] else "NULL", str(l["ms"]), ts(l["m"])] for l in logs]
insert(CH, "AIQuotaLogs", ["Log_ID", "User_ID", "Feature_Code", "Tokens_Consumed", "Cost_Estimate_USD", "Request_Status", "Error_Message", "Execution_Time_Ms", "Created_At"], rows, identity=True)
rows = [[str(x["id"]), str(x["user"]["id"]), str(x["session"]["id"]) if x["session"] else "NULL", str(x["rating"]), q(x["q"]), q(x["fb"]) if x["fb"] else "NULL", ts(x["m"])] for x in surveys]
insert(CH, "Surveys", ["Survey_ID", "User_ID", "Session_ID", "Rating_Score", "Context_Question", "Feedback_Content", "Created_At"], rows, identity=True)

# ---- audit logs
AU = part("24_level2_audit")
audits = []
def audit(m, actor, role, action, table, tid, old=None, new=None, note=None):
    audits.append((max(1, int(m)), actor["id"] if actor else None, role, action, table, str(tid), old, new, note))
role_at = lambda u: "VIP" if u["role"] == "VIP" else "MEMBER"
for u in users:
    if u["kind"] == "ADMIN": continue
    audit(u["m_created"], u, "MEMBER", "USER_REGISTERED", "Users", u["id"], None, {"email": u["email"], "role": "MEMBER", "accountStatus": "PENDING_ACTIVATION"})
    if u["status"] != "PENDING_ACTIVATION":
        audit(max(1, u["m_created"] - rnd_between(1, 30)), None, "SYSTEM", "USER_ACTIVATED", "Users", u["id"], {"Account_Status": "PENDING_ACTIVATION"}, {"Account_Status": "ACTIVE"})
for t in txs:
    if t["status"] == "PENDING": continue
    act = {"SUCCESS": "PAYMENT_SUCCESS", "FAILED": "PAYMENT_FAILED", "EXPIRED": "PAYMENT_EXPIRED"}.get(t["status"], "PAYMENT_RECONCILE")
    audit(t["m_done"], None, "SYSTEM", act, "PaymentTransactions", t["id"], {"Transaction_Status": "PENDING"}, {"Transaction_Status": t["status"], "Gateway": t["gw"], "DurationDays": t["dur"]})
for u in users:
    if "expvip" in u["flags"]:
        t = [x for x in txs if x["user"] is u and x["kind"] == "expired"][0]
        audit(t["m_end"], None, "SYSTEM", "VIP_EXPIRED", "Users", u["id"], {"Role": "VIP"}, {"Role": "MEMBER"})
evs = {}
for ev in approve_events: evs.setdefault(ev["user"]["id"], []).append(ev)
for uid_, lst in evs.items():
    u = [x for x in users if x["id"] == uid_][0]
    for i, ev in enumerate(sorted(lst, key=lambda e: -e["m"]), 1):
        stat = "ACTIVE" if i <= 3 else ("BANNED" if i >= 13 else "SUSPENDED")
        prev = "ACTIVE" if i <= 4 else ("BANNED" if i > 13 else "SUSPENDED")
        act = "WARNING" if i <= 3 else ("BAN" if i >= 13 else "SUSPEND_%dD" % TIER(i))
        audit(ev["m"] - rnd_between(1, 30), rng.choice(admins), "ADMIN", "USER_SANCTION", "Users", uid_,
              {"Count_ViolationReports": i - 1, "Account_Status": prev if i > 1 else "ACTIVE"}, {"Count_ViolationReports": i, "Account_Status": stat, "Sanction": act}, "Thang chế tài tự động")
for rp in reports:
    if rp["status"] in ("APPROVE", "REJECT"):
        audit(rp["m"] - rnd_between(1, 60), rp["admin"], "ADMIN", "MODERATION_DECISION", "ViolationReports", rp["id"], {"Status": "PENDING"}, {"Status": rp["status"], "Type": rp["type"]})
for r in recipes:
    if r["verified"]: audit(r["m"] - rnd_between(1, 200), admin1, "ADMIN", "RECIPE_VERIFY", "Recipes", r["id"], {"Is_Verified": False}, {"Is_Verified": True})
for pl in plans:
    if pl["status"] == "ACTIVE": audit(max(1, pl["m"] - 2), pl["user"], role_at(pl["user"]), "MEAL_PLAN_ACTIVATED", "MealPlans", pl["id"], {"Plan_Status": "DRAFT"}, {"Plan_Status": "ACTIVE"})
    if pl["status"] == "CANCELLED": audit(max(1, pl["m"] - rnd_between(5, 600)), pl["user"], role_at(pl["user"]), "MEAL_PLAN_CANCELLED", "MealPlans", pl["id"], {"Plan_Status": "ACTIVE"}, {"Plan_Status": "CANCELLED"})
for sw in swaps[:60]:
    audit(sw["m"], sw["plan"]["user"], role_at(sw["plan"]["user"]), "MEAL_SLOT_SWAPPED", "MealSlots", sw["slot"]["id"], {"Recipe_ID": sw["old"]["id"]}, {"Recipe_ID": sw["new"]["id"]}, sw["reason"])
for key, o, n_ in [("CHAT_LIMIT_DAILY_FREE", "15", "20"), ("CHAT_LIMIT_DAILY_VIP", "150", "200"), ("FRIDGE_SCAN_LIMIT_DAILY_FREE", "5", "10"), ("FRIDGE_SCAN_LIMIT_DAILY_VIP", "20", "30"),
                   ("MAX_SWAPS_PER_PLAN", "14", "21"), ("AI_COST_ALERT_USD", "8", "10")]:
    audit(rnd_between(20 * DAY, 58 * DAY), rng.choice(admins), "ADMIN", "UPDATE_SYSTEM_SETTING", "SystemSettings", key, {"Setting_Value": o}, {"Setting_Value": n_})
rows = []
for i, (m, actor, role, action, table, tid, old, new, note) in enumerate(sorted(audits, key=lambda a: -a[0])):
    rows.append([str(BASE + i), str(actor) if actor else "NULL", v(role), v(action), v(table), v(tid), js(old) if old else "NULL", js(new) if new else "NULL", q(note) if note else "NULL", ts(m)])
insert(AU, "AuditLogs", ["Audit_ID", "Actor_User_ID", "Actor_Role", "Action", "Target_Table", "Target_ID", "Old_Value", "New_Value", "Note", "Created_At"], rows, identity=True)

# ---- footer + ghi file
FT = part("99_footer")
FT.append("COMMIT TRANSACTION;")
# Đẩy identity mọi bảng lên 100000: dữ liệu app tạo sau khi nạp nằm ngoài dải seed 10001-99999, nạp lại không xóa nhầm.
FT.append("DECLARE @rs NVARCHAR(MAX) = N'';\n"
          "SELECT @rs = @rs + N'IF IDENT_CURRENT(''dbo.' + t.name + N''') < 99999 DBCC CHECKIDENT(''dbo.' + t.name + N''', RESEED, 99999) WITH NO_INFOMSGS;'\n"
          "FROM sys.identity_columns ic JOIN sys.tables t ON t.object_id = ic.object_id WHERE SCHEMA_NAME(t.schema_id) = N'dbo';\n"
          "EXEC sp_executesql @rs;\n"
          "PRINT N'   identity các bảng đã đẩy lên 100000 (dữ liệu app mới nằm ngoài dải seed).';")
FT.append("DROP TABLE #sch; DROP TABLE #cat; DROP TABLE #alg; DROP TABLE #plan; DROP TABLE #ing;")
FT.append("PRINT N'>> SEED60 hoàn tất: %d dòng được nạp. Chạy verify_timeline.sql để kiểm tra chéo.';" % sum(COUNTS.values()))

os.makedirs(ARGS.out, exist_ok=True)
def write(name, text):
    with open(os.path.join(ARGS.out, name), "w", encoding="utf-8-sig", newline="\n") as f: f.write(text)
order = sorted(PARTS.keys())
for name in order: write("%s.sql" % name, "\n".join(PARTS[name]) + "\n")
write("seed60_all.sql", "\nGO\n".join([]) + "\n".join("\n".join(PARTS[n]) + "\n" for n in order))
# bản cập nhật ảnh nguyên liệu (1 URL / nguyên liệu, KHÔNG tạo bảng mới)
upd = ["-- Cập nhật Ingredients.Image_Url (1 ảnh / nguyên liệu, nguồn Pexels qua Cloudinary). Chỉ chạm dòng đang NULL. Sinh bởi generate_seed60.py", "SET NOCOUNT ON;"]
for nm, url in sorted(ING_IMG.items()):
    upd.append("UPDATE dbo.Ingredients SET Image_Url = %s WHERE Ingredient_Name = %s AND Image_Url IS NULL;" % (v(url), q(nm)))
write("update_ingredient_images.sql", "\n".join(upd) + "\n")
summary = {"seed": ARGS.seed, "total_rows": sum(COUNTS.values()), "tables": COUNTS}
json.dump(summary, open(os.path.join(ARGS.out, "_summary.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("Đã ghi %d file vào %s" % (len(order) + 3, ARGS.out)); print("Tổng số dòng: %d" % sum(COUNTS.values()))
for k_, c_ in sorted(COUNTS.items(), key=lambda x: -x[1]): print("  %-24s %6d" % (k_, c_))
