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
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private EnemyProfile playerStatProfile;
        [SerializeField] private bool readOnGenerate=true;
        [MenuItem("Tools/웨이브랩/자동 밸런스 초안")]
        public static void Open() { WaveQuickStartWindow.Open(); }
        public static void OpenAdvanced(BalanceInput settings, GameObject player)
        {
            var window=GetWindow<AutoBalanceWindow>("고급 웨이브 설정");window.ClearPreview();
            window.input=JsonUtility.FromJson<BalanceInput>(JsonUtility.ToJson(settings));
            window.playerPrefab=player;window.minSize=new Vector2(650,600);
        }
        private void OnDisable() { ClearPreview(); }
        private void ClearPreview() { if (preview?.plan != null && !AssetDatabase.Contains(preview.plan)) DestroyImmediate(preview.plan); preview = null; }
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("고급 설정 — 직접 조정할 때만 사용", EditorStyles.boldLabel);
            if(GUILayout.Button("쉬운 시작 화면으로 돌아가기"))WaveQuickStartWindow.Open();
            EditorGUILayout.HelpBox("적 프로필·전투 스탯·목표 시간을 입력하세요. 생성 결과를 확인한 뒤 새 계획으로 저장합니다. 기존 프리팹과 계획은 변경하지 않습니다.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("1. 목표", EditorStyles.boldLabel);
            input.totalSeconds = EditorGUILayout.FloatField("총 목표 시간 (초, 휴식 포함)", input.totalSeconds);
            input.waveCount = EditorGUILayout.IntField("웨이브 수 (0 = 자동 결정)", input.waveCount);
            input.restSeconds = EditorGUILayout.FloatField("웨이브 사이 휴식 (초)", input.restSeconds);
            input.difficulty = EditorGUILayout.Popup("난이도", input.difficulty, new[] { "여유롭게", "보통", "어렵게" });
            input.seed = EditorGUILayout.IntField("생성 시드 (같은 값 = 같은 편성)", input.seed);
            input.maximumAlive=EditorGUILayout.IntSlider("동시 적 상한",input.maximumAlive,1,64);
            input.maximumElites=EditorGUILayout.IntSlider("동시 엘리트",Mathf.Min(input.maximumElites,input.maximumAlive),1,input.maximumAlive);
            input.rhythm=EditorGUILayout.Popup("전투 흐름",input.rhythm,new[]{"꾸준하게","압박 후 숨 고르기","점층"});
            input.clearBeforeRest=EditorGUILayout.Toggle("잔여 적 정리 후 휴식",input.clearBeforeRest);
            input.effectiveDpsOverride=EditorGUILayout.FloatField("실측 유효 DPS (0 = 추정)",input.effectiveDpsOverride);
            input.pressureMargin=EditorGUILayout.Slider("동시 공격 안전 여유",input.pressureMargin,1,3);
            input.recoveryPercent=EditorGUILayout.Slider("예상 회복 비율",input.recoveryPercent,0,1);
            EditorGUILayout.Space();EditorGUILayout.LabelField("2. 플레이어의 시작 스탯", EditorStyles.boldLabel);
            playerPrefab=(GameObject)EditorGUILayout.ObjectField("플레이어 프리팹",playerPrefab,typeof(GameObject),false);
            playerStatProfile=(EnemyProfile)EditorGUILayout.ObjectField(new GUIContent("스탯 연결 프로필 (선택)","다른 게임 스크립트는 기존 프로필 형식으로 숫자 필드를 연결할 수 있습니다. 보상은 플레이어 계산에 사용하지 않습니다."),playerStatProfile,typeof(EnemyProfile),false);
            readOnGenerate=EditorGUILayout.Toggle("생성 시 현재 프리팹 스탯 읽기",readOnGenerate);
            EditorGUILayout.HelpBox("자동 읽기에는 체력·피해·공격 간격(또는 초당 공격 횟수)·사거리·이동속도 5개가 필요합니다. 연결이 없으면 생성하지 않습니다. 수동 입력하려면 자동 읽기를 끄세요. 이동하지 않는 유닛은 이동속도 0으로 계산하세요.",MessageType.Info);
            input.playerHealth = EditorGUILayout.FloatField("최대 체력", input.playerHealth);
            input.playerDamage = EditorGUILayout.FloatField("한 번 공격의 피해량", input.playerDamage);
            input.playerInterval = EditorGUILayout.FloatField("공격 간격 (초)", input.playerInterval);
            input.playerRange=EditorGUILayout.FloatField("플레이어 사거리 (유닛)",input.playerRange);
            input.playerMoveSpeed=EditorGUILayout.FloatField("플레이어 접근 이동속도",input.playerMoveSpeed);
            input.hitRate = EditorGUILayout.Slider("예상 명중률", input.hitRate, .01f, 1);
            input.attackUptime = EditorGUILayout.Slider(new GUIContent("사거리 안 공격 가동률", "접근 시간은 별도로 계산합니다. 여기에는 회피·재장전 등 사거리 안에서 공격을 쉬는 시간만 반영하세요."), input.attackUptime, .01f, 1);
            EditorGUILayout.LabelField("비율 0.8 = 80%. 여러 무기의 공격력은 유효한 단일 공격 값으로 환산해 입력하세요.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();EditorGUILayout.LabelField("3. 등장할 적", EditorStyles.boldLabel);
            for (int i=0;i<input.enemies.Count;i++)
            {
                var e=input.enemies[i];EditorGUILayout.BeginVertical("box");
                e.profile=(EnemyProfile)EditorGUILayout.ObjectField("적 프로필",e.profile,typeof(EnemyProfile),false);
                e.role=(EnemyRole)EditorGUILayout.EnumPopup("적 역할",e.role);
                e.selectionWeight=EditorGUILayout.Slider("등장 비중",e.selectionWeight,.25f,3);
                e.spawnKey=EditorGUILayout.TextField("스폰 영역 이름",e.spawnKey);
                e.health=EditorGUILayout.FloatField("체력",e.health);
                e.damage=EditorGUILayout.FloatField("1회 공격 피해",e.damage);
                e.attackInterval=EditorGUILayout.FloatField("공격 간격 (초)",e.attackInterval);
                e.attackRange=EditorGUILayout.FloatField("사거리 (유닛)",e.attackRange);
                e.moveSpeed=EditorGUILayout.FloatField("접근 이동속도",e.moveSpeed);
                e.encounterDistance=EditorGUILayout.FloatField(new GUIContent("예상 조우 거리 (유닛)","이 적의 스폰 영역과 플레이어 사이 대표 거리. 실제 씬 위치를 자동 분석하지 않습니다."),e.encounterDistance);
                e.hitRate=EditorGUILayout.Slider("플레이어에게 맞힐 비율",e.hitRate,0,1);
                EditorGUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(e.profile==null))
                {
                    if(GUILayout.Button("프리팹의 현재 스탯 읽기")) { try{ImportStats(e);error=null;}catch(ArgumentException ex){error=ex.Message;} GUI.changed=true; }
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
                    try
                    {
                        if(readOnGenerate)
                        {
                            var p=ReadStats(playerPrefab!=null?playerPrefab:playerStatProfile?.prefab,playerStatProfile,"플레이어");
                            input.playerHealth=p[0];input.playerDamage=p[1];input.playerInterval=p[2];input.playerRange=p[3];input.playerMoveSpeed=p[4];
                            foreach(var enemy in input.enemies)ImportStats(enemy);
                        }
                        preview=AutoBalance.Generate(input);
                    }catch(ArgumentException ex){error=ex.Message;}
                }
            }
            if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
            if(preview!=null)
            {
                EditorGUILayout.Space();EditorGUILayout.LabelField($"생성 결과: {preview.rows.Count}웨이브 / 목표 {input.totalSeconds:0.#}초",EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"사용한 플레이어 스탯: HP {input.playerHealth:0.#} / 피해 {input.playerDamage:0.#} / 간격 {input.playerInterval:0.###}초 / 사거리 {input.playerRange:0.#} / 이동 {input.playerMoveSpeed:0.#}",EditorStyles.wordWrappedLabel);
                foreach(var enemy in input.enemies)
                {
                    double approach=AutoBalance.ReachTime(enemy.encounterDistance,input.playerRange,enemy.attackRange,input.playerMoveSpeed,enemy.moveSpeed);
                    string time=double.IsInfinity(approach)?"접근 불가":approach.ToString("0.##")+"초";
                    EditorGUILayout.LabelField($"{enemy.profile.name}: 사거리 {enemy.attackRange:0.#}, 간격 {enemy.attackInterval:0.###}초, 이동 {enemy.moveSpeed:0.#}, 조우 {enemy.encounterDistance:0.#} → 플레이어 접근 {time}",EditorStyles.wordWrappedMiniLabel);
                }
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
            var p=ReadStats(e.profile?.prefab,e.profile,e.profile!=null?e.profile.name:"적 프로필 미지정");
            e.health=p[0];e.damage=p[1];e.attackInterval=p[2];e.attackRange=p[3];e.moveSpeed=p[4];
        }
        private static float[] ReadStats(GameObject prefab,EnemyProfile profile,string label)
        {
            string[] ids={"health.max","attack.damage","attack.interval","attack.range","movement.speed"};
            var values=new float[5];
            for(int i=0;i<ids.Length;i++)
            {
                bool ok=i==2?BalanceStatReader.Interval(prefab,profile,out values[i]):BalanceStatReader.Read(prefab,profile,ids[i],out values[i]);
                if(!ok)throw new ArgumentException(label+": "+ids[i]+" 현재 값을 읽을 수 없습니다. 프리팹의 IStatProvider 또는 프로필의 숫자 필드 연결을 확인하세요.");
            }
            return values;
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
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            try { WaveQuickStartWindow.PlanSaved?.Invoke(saved); }
            catch(ArgumentException ex) { error="파일은 저장했지만 본편 연결에 실패했습니다: "+ex.Message; }
            WaveLabWindow.OpenPlan(saved);
        }
    }
}
