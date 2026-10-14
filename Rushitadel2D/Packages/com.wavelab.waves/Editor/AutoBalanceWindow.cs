using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    public sealed class AutoBalanceWindow : EditorWindow
    {
        [SerializeField] private BalanceInput input = new BalanceInput();
        private BalanceResult preview;
        private Vector2 scroll;
        private string error;
        [MenuItem("Tools/웨이브랩/자동 밸런스 초안")]
        public static void Open() { GetWindow<AutoBalanceWindow>("자동 웨이브 생성").minSize = new Vector2(650, 600); }
        private void OnDisable() { ClearPreview(); }
        private void ClearPreview() { if (preview?.plan != null && !AssetDatabase.Contains(preview.plan)) DestroyImmediate(preview.plan); preview = null; }
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("스탯으로 웨이브 자동 생성", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("적 프로필·전투 스탯·목표 시간을 입력하세요. 생성 결과를 확인한 뒤 새 계획으로 저장합니다. 기존 프리팹과 계획은 변경하지 않습니다.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("1. 목표", EditorStyles.boldLabel);
            input.totalSeconds = EditorGUILayout.FloatField("총 목표 시간 (초, 휴식 포함)", input.totalSeconds);
            input.waveCount = EditorGUILayout.IntField("웨이브 수 (0 = 자동 결정)", input.waveCount);
            input.restSeconds = EditorGUILayout.FloatField("웨이브 사이 휴식 (초)", input.restSeconds);
            input.difficulty = EditorGUILayout.Popup("난이도", input.difficulty, new[] { "여유롭게", "보통", "어렵게" });
            input.seed = EditorGUILayout.IntField("생성 시드 (같은 값 = 같은 편성)", input.seed);
            EditorGUILayout.Space();EditorGUILayout.LabelField("2. 플레이어의 시작 스탯", EditorStyles.boldLabel);
            input.playerHealth = EditorGUILayout.FloatField("최대 체력", input.playerHealth);
            input.playerDamage = EditorGUILayout.FloatField("한 번 공격의 피해량", input.playerDamage);
            input.playerInterval = EditorGUILayout.FloatField("공격 간격 (초)", input.playerInterval);
            input.hitRate = EditorGUILayout.Slider("예상 명중률", input.hitRate, .01f, 1);
            input.attackUptime = EditorGUILayout.Slider(new GUIContent("공격 가능한 시간 비율", "사거리 밖 이동·재장전 등을 제외하고 실제로 공격 가능한 시간의 비율입니다. 위치 계산 대신 사용하는 가정입니다."), input.attackUptime, .01f, 1);
            EditorGUILayout.LabelField("비율 0.8 = 80%. 여러 무기의 공격력은 유효한 단일 공격 값으로 환산해 입력하세요.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();EditorGUILayout.LabelField("3. 등장할 적", EditorStyles.boldLabel);
            for (int i=0;i<input.enemies.Count;i++)
            {
                var e=input.enemies[i];EditorGUILayout.BeginVertical("box");
                e.profile=(EnemyProfile)EditorGUILayout.ObjectField("적 프로필",e.profile,typeof(EnemyProfile),false);
                e.spawnKey=EditorGUILayout.TextField("스폰 영역 이름",e.spawnKey);
                e.health=EditorGUILayout.FloatField("체력",e.health);
                e.damage=EditorGUILayout.FloatField("1회 공격 피해",e.damage);
                e.attackInterval=EditorGUILayout.FloatField("공격 간격 (초)",e.attackInterval);
                e.hitRate=EditorGUILayout.Slider("플레이어에게 맞힐 비율",e.hitRate,0,1);
                EditorGUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(e.profile==null))
                {
                    if(GUILayout.Button("프로필의 저장된 스탯 가져오기")) { ImportStats(e);GUI.changed=true; }
                    if(GUILayout.Button("보상 설정 열기"))Selection.activeObject=e.profile;
                }
                bool remove=GUILayout.Button("삭제",GUILayout.Width(45));EditorGUILayout.EndHorizontal();
                float xp=0,gold=0;
                if(e.profile!=null)foreach(var r in e.profile.defeatRewards)if(r!=null){if(r.resourceId=="xp")xp+=r.amount;if(r.resourceId=="gold")gold+=r.amount;}
                EditorGUILayout.LabelField($"1마리당 보상: 경험치 {xp} / 골드 {gold}");
                EditorGUILayout.EndVertical();
                if(remove){input.enemies.RemoveAt(i);GUI.changed=true;break;}
            }
            if(GUILayout.Button("+ 적 추가")){input.enemies.Add(new BalanceEnemy());GUI.changed=true;}
            EditorGUILayout.Space();EditorGUILayout.LabelField("4. 성장 가정 (실제 게임에 자동 적용되지 않음)",EditorStyles.boldLabel);
            input.modelGrowth=EditorGUILayout.Toggle("예상 업그레이드 반영",input.modelGrowth);
            if(input.modelGrowth)
            {
                input.firstLevelXp=EditorGUILayout.FloatField("첫 레벨업 필요 경험치",input.firstLevelXp);
                input.xpRequirementStep=EditorGUILayout.FloatField("레벨마다 요구량 증가 비율",input.xpRequirementStep);
                input.damagePerLevel=EditorGUILayout.FloatField("레벨당 공격력 합산 증가율",input.damagePerLevel);
                input.goldUpgradeCost=EditorGUILayout.FloatField("공격력 강화 1회 골드 비용",input.goldUpgradeCost);
                input.damagePerPurchase=EditorGUILayout.FloatField("구매당 공격력 합산 증가율",input.damagePerPurchase);
                EditorGUILayout.HelpBox("증가율 0.1 = 10%. 웨이브 종료마다 보상을 모두 얻고, 가능한 레벨업·공격력 구매를 전부 한다고 가정합니다. 실제 증강 선택·웹 구매 시스템과는 아직 연결되지 않았습니다.",MessageType.Warning);
            }
            if(EditorGUI.EndChangeCheck()){ClearPreview();}
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if(GUILayout.Button("웨이브 자동 생성 · 미리보기",GUILayout.Height(34)))
                {
                    ClearPreview();error=null;
                    try{preview=AutoBalance.Generate(input);}catch(ArgumentException ex){error=ex.Message;}
                }
            }
            if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
            if(preview!=null)
            {
                EditorGUILayout.Space();EditorGUILayout.LabelField($"생성 결과: {preview.rows.Count}웨이브 / 목표 {input.totalSeconds:0.#}초",EditorStyles.boldLabel);
                foreach(var row in preview.rows)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"{row.wave}웨이브 · {row.count}마리 · 전투 {row.seconds:0.0}초",EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"예상 유효 DPS {row.effectiveDps:0.0} | 총 처치 작업량 {row.estimatedWorkSeconds:0.0}초 | 예상 피해 {row.estimatedDamage:0.0}",EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"레벨 {row.startLevel} → {row.endLevel} | 경험치 +{row.xpEarned:0.#} | 골드 +{row.goldEarned:0.#} | 강화 구매 {row.purchases}회",EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField(row.note,EditorStyles.wordWrappedMiniLabel);EditorGUILayout.EndVertical();
                }
                foreach(var warning in preview.warnings)EditorGUILayout.HelpBox(warning,MessageType.Warning);
                if(GUILayout.Button("새 웨이브 계획으로 저장",GUILayout.Height(30)))Save();
            }
            EditorGUILayout.HelpBox(AutoBalance.Assumptions,MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        private void ImportStats(BalanceEnemy e)
        {
            int found=0;
            foreach(var b in e.profile.statBindings)
            {
                if(b==null||string.IsNullOrEmpty(b.sampledUtc))continue;
                if(b.statId=="health.max"){e.health=b.snapshot;found++;}
                if(b.statId=="attack.damage"){e.damage=b.snapshot;found++;}
                if(b.statId=="attack.interval"){e.attackInterval=b.snapshot;found++;}
            }
            error=$"저장된 스탯 {found}개를 가져왔습니다. health.max / attack.damage / attack.interval만 지원합니다. 원본이 변경됐다면 적 프로필에서 먼저 ‘현재 값 읽기’를 누르세요. 가져오지 못한 값은 기존 입력을 유지합니다.";
        }
        private void Save()
        {
            var errors=WaveValidation.Errors(preview.plan);
            if(errors.Count>0){error=string.Join("\n",errors);return;}
            string path=EditorUtility.SaveFilePanelInProject("새 자동 웨이브 계획 저장","자동웨이브","asset", "기존 파일은 덮어쓰지 않습니다.");
            if(string.IsNullOrEmpty(path))return;
            path=AssetDatabase.GenerateUniqueAssetPath(path);
            var saved=Instantiate(preview.plan);AssetDatabase.CreateAsset(saved,path);
            string report=AutoBalance.Assumptions+"\n\n설정:\n"+JsonUtility.ToJson(input,true)+"\n\n웨이브 결과:\n";
            foreach(var row in preview.rows)report+=JsonUtility.ToJson(row,true)+"\n";
            foreach(var warning in preview.warnings)report+="\n주의: "+warning;
            File.WriteAllText(Path.ChangeExtension(path,"balance.txt"),report);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();WaveLabWindow.OpenPlan(saved);
        }
    }
}
