import json
from pathlib import Path
import openpyxl

root=Path(__file__).resolve().parents[3]
out=Path(__file__).parent
jobs=json.loads((out/"jobs.json").read_text(encoding="utf-8"))
live={"PlayerBasic","PlayerPower","TowerBasic","WebPierce","WebSpread","WebBlast","WebPoison","SplitFragment","BlastRing","PoisonStatus","IceShell","S3-13"}
assets=[{"id":j[0],"name":j[1],"pixels":j[2],"file":f"Rushitadel2D/Assets/Resources/ProjectileArt/{j[0]}.png",
         "use":"runtime" if j[0].startswith("ROT-") or j[0] in live else "prepared-for-unimplemented-design",
         "renderFlipY":j[0] in {"ROT-11","S3-14"}, "design":j[3]} for j in jobs]
catalog={"method":"Built-in image_gen; nearest-neighbor pixel export; transparent RGBA","assets":assets,"sourceCoverage":[]}
def rows(book,sheet):
    s=openpyxl.load_workbook(root/book,data_only=True)[sheet]
    return list(enumerate(s.values,1))
book="Rushitadel_회전포탑_변형증강.xlsx"
for row,values in rows(book,"회전형 변형"):
    if isinstance(values[0],str) and values[0].startswith("ROT-"):
        catalog["sourceCoverage"].append({"book":book,"sheet":"회전형 변형","row":row,"id":values[0],"name":values[1],"art":[values[0]]})
book="Rushitadel_증강_정리본v2.xlsx"
for row,v in rows(book,"02_포탑변형"):
    if isinstance(v[0],str) and v[0].startswith("S3-"):
        catalog["sourceCoverage"].append({"book":book,"sheet":"02_포탑변형","row":row,"id":v[0],"name":v[1],"art":[v[0]],"note":"Artwork prepared; existing ROT combat rules are not replaced by v2 S3 mechanics."})
special={("S3-01","L04"):["PrismRound"],("S3-05","L02"):["BlastRing"],("S3-06","L03"):["BlastRing"],
("S3-07","L04"):["ResidualField"],("S3-09","L03"):["BlastRing"],("S3-10","L03"):["AcidPool"],
("S3-10","L04"):["PoisonStatus"],("S3-11","L03"):["IceShell"],("S3-11","L04"):["IceShell"],
("S3-13","L04"):["BlastRing"],("S3-15","L04"):["MicroMissile"]}
for row,v in rows(book,"04_변형전용"):
    if isinstance(v[0],str) and v[0].startswith("S3-"):
        art=special.get((v[0],v[2]),["TargetMark"] if v[0]=="S3-12" else [v[0]])
        catalog["sourceCoverage"].append({"book":book,"sheet":"04_변형전용","row":row,"id":v[0]+"/"+str(v[2]),"name":v[3],"art":art,
        "note":"Visual mapping only. Count, timing, size and damage modifiers reuse the corresponding base sprite."})
web={"관통":["WebPierce"],"분산":["WebSpread"],"폭발":["WebBlast","BlastRing","ResidualField"],"독":["WebPoison","PoisonStatus"],"지원":["TargetMark"],"동력":["PlayerPower","BlastRing"]}
for row,v in rows(book,"01_플레이어Web"):
    if row<4:continue
    catalog["sourceCoverage"].append({"book":book,"sheet":"01_플레이어Web","row":row,"name":v[3],"art":web.get(v[0],["TowerBasic"]),
    "note":"Numerical upgrades reuse base art. Proposed gameplay effects are not implemented by this art task."})
common={"UC-01":["TowerBasic"],"UC-02":["TargetMark","BlastRing"],"UC-03":["TargetMark"],"UC-04":["BlastRing"],"UC-05":["TargetMark"],"UC-06":["TargetMark"],"UC-07":["SplitFragment"],"UC-08":["TargetMark","BlastRing"],"UC-09":["TowerBasic"],"UC-10":["TowerBasic","BlastRing"]}
for row,v in rows(book,"03_포탑공통"):
    if isinstance(v[0],str) and v[0] in common:
        catalog["sourceCoverage"].append({"book":book,"sheet":"03_포탑공통","row":row,"id":v[0],"name":v[1],"art":common[v[0]],"note":"Shared impact/target overlay or base projectile; numeric behavior remains a gameplay implementation task."})
legacy={"S3-01":"S3-01","S3-03":"ROT-02","S3-04":"ROT-03","S3-05":"S3-05","S3-06":"S3-06","S3-07":"S3-07","S3-10":"ROT-07","S3-11":"S3-11","S3-13":"S3-13","S3-14":"ROT-11","S3-16":"ROT-08","S3-18":"LegacyShield"}
book="Rushitadel_포탑_요격돌파_증강기획.xlsx"
for row,v in rows(book,"변형증강"):
    if isinstance(v[0],str) and v[0] in legacy:
        catalog["sourceCoverage"].append({"book":book,"sheet":"변형증강","row":row,"id":v[0],"name":v[1],"art":[legacy[v[0]]],"note":"Legacy ID namespace: S3-04 here is a drill, unlike the v2 sniper."})
known={a["id"] for a in assets}
for entry in catalog["sourceCoverage"]:
    assert all(a in known for a in entry["art"]),entry
(out/"catalog.json").write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding="utf-8")
print(f'{len(assets)} assets; {len(catalog["sourceCoverage"])} workbook row mappings')
