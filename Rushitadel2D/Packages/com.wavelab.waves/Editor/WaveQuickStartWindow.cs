using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    public sealed class WaveQuickStartWindow : EditorWindow
    {
        public static Func<BalanceInput, GameObject> LoadCampaignPreset;
        public static Action<WavePlan> InstallCampaign;
        public static Action<WavePlan> PlanSaved;
        public static Action RemoveCampaign;
        [SerializeField] private BalanceInput input=new BalanceInput{modelGrowth=false};
        [SerializeField] private GameObject player;
        [SerializeField] private BalanceResult result;
        [SerializeField] private WavePlan saved;
        [SerializeField] private WaveRunner runner;
        [SerializeField] private int step;
        [SerializeField] private bool manual, campaign;
        private Vector2 scroll;
        private bool details, help, unitStats;
        private string error, notice;
        private static readonly string[] Steps={"1  유닛", "2  전투 느낌", "3  미리보기", "4  저장·사용"};
        private static readonly string[] Roles={"일반", "빠른 적", "튼튼한 적", "원거리", "엘리트"};
        [MenuItem("Tools/웨이브랩/시작하기 (쉬운 자동 생성)",false,0)]
        public static void Open(){GetWindow<WaveQuickStartWindow>("쉬운 웨이브 제작").minSize=new Vector2(720,620);}
        private void OnDestroy(){ClearResult();}
        private void ClearResult(){if(result?.plan!=null&&!AssetDatabase.Contains(result.plan))DestroyImmediate(result.plan);result=null;saved=null;}
        private void Go(int next){step=next;scroll=Vector2.zero;error=null;notice=null;GUI.FocusControl(null);}
        private void OnGUI()
        {
            var title=new GUIStyle(EditorStyles.boldLabel){fontSize=20,wordWrap=true};
            EditorGUILayout.Space(12);EditorGUILayout.LabelField("웨이브를 쉽게 설계하세요",title,GUILayout.Height(30));
            EditorGUILayout.LabelField("유닛을 고르고 전투 느낌을 정하면, 편성과 적용 순서를 안내합니다.",EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                for(int i=0;i<Steps.Length;i++)
                {
                    using(new EditorGUI.DisabledScope(i>=2&&result==null))
                        if(GUILayout.Toggle(step==i,Steps[i],EditorStyles.miniButton,GUILayout.Height(30))&&step!=i)Go(i);
                }
                EditorGUILayout.EndHorizontal();
                if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
                if(!string.IsNullOrEmpty(notice))EditorGUILayout.HelpBox(notice,MessageType.Info);
                scroll=EditorGUILayout.BeginScrollView(scroll);
                EditorGUILayout.Space(10);
                if(step==0)Units();else if(step==1)Goal();else if(step==2)Preview();else Delivery();
                EditorGUILayout.Space(12);
                help=EditorGUILayout.Foldout(help,"사용법 · 막혔을 때 확인할 것",true);
                if(help)EditorGUILayout.HelpBox(Usage,MessageType.None);
                EditorGUILayout.EndScrollView();
            }
            if(EditorApplication.isPlaying)EditorGUILayout.HelpBox("실행 중에는 설계를 잠급니다. Play를 종료하면 계속 편집할 수 있습니다.",MessageType.Info);
        }
        private static void Heading(string title,string subtitle)
        {
            EditorGUILayout.LabelField(title,EditorStyles.boldLabel);
            EditorGUILayout.LabelField(subtitle,EditorStyles.wordWrappedLabel);EditorGUILayout.Space(8);
        }
        private static bool Primary(string label)=>GUILayout.Button(label,GUILayout.Height(38));
        private void Units()
        {
            Heading("어떤 전투를 만들까요?","처음에는 준비된 유닛으로 시작하세요. 기존 프리팹과 씬은 바꾸지 않습니다.");
            EditorGUILayout.BeginHorizontal();
            if(LoadCampaignPreset!=null&&GUILayout.Button("우리 게임 유닛 불러오기",GUILayout.Height(36)))Attempt(()=>{
                ClearResult();input=new BalanceInput{modelGrowth=false};player=LoadCampaignPreset(input);manual=true;campaign=true;
                notice="본편의 9개 일반 라운드를 설계합니다. 보스·정비·테마 전환은 기존 규칙을 유지합니다. 중앙 공격 기준의 보수적인 초안입니다. 포탑 합산 화력은 세부 조절의 실측 유효 DPS로 보정하세요.";
            });
            if(GUILayout.Button("전투 예제로 시작",GUILayout.Height(36)))Attempt(LoadExample);
            EditorGUILayout.EndHorizontal();EditorGUILayout.Space(12);
            EditorGUI.BeginChangeCheck();
            using(new EditorGUI.DisabledScope(campaign))player=(GameObject)EditorGUILayout.ObjectField("플레이어 프리팹",player,typeof(GameObject),false);
            EditorGUILayout.LabelField("등장할 적과 역할",EditorStyles.boldLabel);
            unitStats=EditorGUILayout.Foldout(unitStats,"상세 스탯 확인 / 수정 (선택)",true);
            for(int i=0;i<input.enemies.Count;i++)
            {
                var e=input.enemies[i];EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                e.profile=(EnemyProfile)EditorGUILayout.ObjectField(e.profile,typeof(EnemyProfile),false,GUILayout.MinWidth(180));
                e.role=(EnemyRole)EditorGUILayout.Popup((int)e.role,Roles,GUILayout.Width(100));
                if(GUILayout.Button("삭제",GUILayout.Width(48))){input.enemies.RemoveAt(i);GUI.changed=true;EditorGUILayout.EndHorizontal();EditorGUILayout.EndVertical();break;}
                EditorGUILayout.EndHorizontal();
                e.selectionWeight=EditorGUILayout.Slider(new GUIContent("등장 비중","같은 예산에 들어가는 적 중 상대적으로 얼마나 자주 선택할지 정합니다."),e.selectionWeight,.25f,3);
                if(manual&&unitStats){e.health=EditorGUILayout.FloatField("체력",e.health);e.damage=EditorGUILayout.FloatField("공격 피해 추정",e.damage);e.attackInterval=EditorGUILayout.FloatField("공격 간격 (초)",e.attackInterval);}
                EditorGUILayout.EndVertical();
            }
            if(GUILayout.Button("+ 적 프로필 추가")){input.enemies.Add(new BalanceEnemy());GUI.changed=true;}
            using(new EditorGUI.DisabledScope(campaign))manual=EditorGUILayout.ToggleLeft("스탯 직접 입력 / 게임 시작값 사용",manual);
            if(manual&&unitStats)
            {
                input.playerHealth=EditorGUILayout.FloatField("플레이어 최대 체력",input.playerHealth);
                input.playerDamage=EditorGUILayout.FloatField("대표 공격 피해",input.playerDamage);
                input.playerInterval=EditorGUILayout.FloatField("대표 공격 간격 (초)",input.playerInterval);
            }
            if(EditorGUI.EndChangeCheck())ClearResult();
            EditorGUILayout.Space(8);
            using(new EditorGUI.DisabledScope(input.enemies.Count==0||input.enemies.Any(e=>e.profile==null)))
                if(Primary("다음 · 전투 느낌 정하기"))Attempt(()=>{ReadStats();var errors=AutoBalance.Validate(input);if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors));Go(1);});
            if(campaign&&GUILayout.Button("일반 프리팹 모드로 전환")){campaign=false;ClearResult();}
        }
        private void Goal()
        {
            Heading("시간과 전투 느낌만 먼저 정하세요","동시 적 상한에 도달하면 추가 등장은 기다립니다. 적을 빼거나 한꺼번에 밀어 넣지 않습니다.");
            EditorGUI.BeginChangeCheck();
            input.totalSeconds=EditorGUILayout.Slider(campaign?"일반 라운드 계획 시간 (분)":"계획 시간 (분)",input.totalSeconds/60f,1,30)*60;
            input.difficulty=EditorGUILayout.Popup("난이도",input.difficulty,new[]{"여유롭게", "보통", "도전적으로"});
            input.rhythm=EditorGUILayout.Popup("전투 흐름",input.rhythm,new[]{"꾸준한 전투", "압박 후 숨 고르기", "점점 강해지는 전투"});
            input.maximumAlive=EditorGUILayout.IntSlider("동시에 살아 있는 적",input.maximumAlive,2,campaign?8:16);
            input.maximumElites=EditorGUILayout.IntSlider("그중 엘리트 최대",Mathf.Min(input.maximumElites,input.maximumAlive),1,Mathf.Min(campaign?2:4,input.maximumAlive));
            using(new EditorGUI.DisabledScope(campaign))input.restSeconds=EditorGUILayout.Slider("정리 후 휴식 (초)",input.restSeconds,0,15);
            EditorGUILayout.HelpBox(campaign?"본편: 3스테이지 × 3라운드. 보스 전투와 정비 시간은 목표 시간에 포함되지 않습니다. 실제 전투는 적 정리 속도에 따라 길어질 수 있습니다.":"기본은 목표 시간이 지난 뒤 잔여 적까지 정리하고 쉬는 방식입니다. 목표 시간은 실제 완료 시각을 보장하지 않습니다.",MessageType.Info);
            details=EditorGUILayout.Foldout(details,"세부 조절 · 실측 화력 / 거리 / 재현 번호",true);
            if(details)
            {
                using(new EditorGUI.DisabledScope(campaign))input.waveCount=EditorGUILayout.IntSlider("웨이브 수 (0 = 자동)",input.waveCount,0,30);
                input.seed=EditorGUILayout.IntField("재현 번호 (시드)",input.seed);
                input.effectiveDpsOverride=EditorGUILayout.FloatField("실측 유효 DPS (0 = 추정)",input.effectiveDpsOverride);
                EditorGUILayout.LabelField("중앙 공격과 모든 포탑을 합친 실제 적 HP 감소량 ÷ 전투 시간. 탄 제거량은 DPS에 더하지 마세요.",EditorStyles.wordWrappedMiniLabel);
                input.hitRate=EditorGUILayout.Slider("추정 명중률",input.hitRate,.1f,1);
                input.attackUptime=EditorGUILayout.Slider("추정 공격 가동률",input.attackUptime,.1f,1);
                input.pressureMargin=EditorGUILayout.Slider("동시 공격 안전 여유",input.pressureMargin,1,2);
                input.recoveryPercent=EditorGUILayout.Slider("웨이브 후 예상 회복 비율",input.recoveryPercent,0,.5f);
                input.playerRange=EditorGUILayout.FloatField("플레이어 대표 사거리",input.playerRange);
                input.playerMoveSpeed=EditorGUILayout.FloatField("플레이어 접근 속도",input.playerMoveSpeed);
                foreach(var e in input.enemies)
                {
                    EditorGUILayout.LabelField(e.profile!=null?e.profile.name:"적 미지정",EditorStyles.boldLabel);
                    e.encounterDistance=EditorGUILayout.FloatField("조우 거리",e.encounterDistance);
                    if(manual){e.attackRange=EditorGUILayout.FloatField("적 사거리",e.attackRange);e.moveSpeed=EditorGUILayout.FloatField("적 이동속도",e.moveSpeed);}
                }
            }
            if(EditorGUI.EndChangeCheck())ClearResult();
            if(Primary("이 설정으로 웨이브 만들기"))Attempt(()=>{ClearResult();ReadStats();if(campaign){input.waveCount=9;input.restSeconds=0;}input.clearBeforeRest=true;result=AutoBalance.Generate(input);Go(2);});
        }
        private void Preview()
        {
            if(result?.plan==null){Go(1);return;}
            Heading($"{result.rows.Count}개 웨이브 · 총 {result.rows.Sum(r=>r.count)}마리",$"동시 적 {result.plan.maximumAlive} / 엘리트 {result.plan.maximumElites} · 잔여 적 정리 후 휴식");
            EditorGUILayout.HelpBox("예상 난이도입니다. 탄 파괴·광역·도탄·다중 포탑의 실제 교전은 테스트에서 확인하세요. 생성 시 성장 보너스를 미리 지급하지 않습니다.",MessageType.Info);
            for(int i=0;i<result.plan.waves.Count;i++)
            {
                var w=result.plan.waves[i];var r=result.rows[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(w.title+" · "+r.count+"마리 · 목표 "+r.seconds.ToString("0")+"초",EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"예상 피해 {r.estimatedDamage:0.#} · 누적 잔여 HP {r.remainingHealth:0.#} · 휴식 {w.restSeconds:0.#}초",EditorStyles.wordWrappedLabel);
                foreach(var g in w.groups)EditorGUILayout.LabelField($"{g.enemy.name}  ×{g.count}    첫 등장 {g.startSeconds:0.#}초 / 간격 {g.intervalSeconds:0.#}초",EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }
            foreach(var warning in result.warnings)EditorGUILayout.HelpBox(warning,MessageType.Warning);
            if(Primary("다음 · 저장하고 사용하기"))Go(3);
            if(GUILayout.Button("전투 느낌 다시 조절"))Go(1);
        }
        private void Delivery()
        {
            if(result?.plan==null){Go(1);return;}
            Heading("만든 웨이브를 사용하세요",campaign?"저장하면 inGame에 자동 연결됩니다. 플레이 중에는 다음 라운드부터 반영됩니다.":"테스트 씬에 적용한 뒤 Play로 동작을 확인하세요.");
            if(saved==null){if(Primary("1. 새 웨이브 파일 저장"))Attempt(Save);return;}
            EditorGUILayout.HelpBox("저장됨: "+AssetDatabase.GetAssetPath(saved)+"\n같은 폴더에 계산 보고서와 사용 안내를 함께 저장했습니다.",MessageType.Info);
            if(campaign)
            {
                using(new EditorGUI.DisabledScope(InstallCampaign==null))if(Primary("본편에 다시 연결"))Attempt(()=>{InstallCampaign(saved);notice="본편 연결을 저장했습니다. Title 씬에서 새 게임을 시작하세요. 별도 WaveRunner를 추가하지 않습니다. 기존 저장은 다음 라운드 시작 시 새 편성을 사용합니다.";});
                if(RemoveCampaign!=null&&GUILayout.Button("본편 연결 해제 · 기존 편성 사용"))Attempt(()=>{RemoveCampaign();notice="다음 실행부터 기존 캠페인 편성을 사용합니다. 만든 계획은 보존됩니다.";});
                EditorGUILayout.HelpBox("Title → 새 게임으로 확인\n수정: Tools → 웨이브랩 → 현재 본편 웨이브 수정 → 저장\n플레이 중 저장은 다음 라운드부터 반영됩니다.",MessageType.None);
            }
            else
            {
                if(GUILayout.Button("2. 전투 테스트 씬 열기",GUILayout.Height(32)))Attempt(OpenTest);
                runner=(WaveRunner)EditorGUILayout.ObjectField("적용할 실행기",runner,typeof(WaveRunner),true);
                using(new EditorGUI.DisabledScope(runner==null||EditorUtility.IsPersistent(runner)))if(Primary("3. 현재 실행기에 적용 · 연결 검사"))Attempt(Apply);
                EditorGUILayout.HelpBox("4. Ctrl+S로 씬 저장 → Play\n완료되지 않으면 적의 사망 코드에서 SpawnedUnit.ReportDefeated()를 호출했는지 확인하세요. 단순 비활성화/풀 반환은 ReportDespawned()로 알립니다. 보상 이벤트는 실제 XP 시스템에 한 번만 연결하세요.",MessageType.None);
            }
            if(GUILayout.Button("다른 계획의 원하는 라운드에 배치"))WavePlacementWindow.OpenPlan(saved);
            if(GUILayout.Button("선택 웨이브 세부 편집"))WaveLabWindow.OpenPlan(saved);
            if(GUILayout.Button("사용 안내 파일 열기"))Application.OpenURL(Path.GetFullPath(Path.ChangeExtension(AssetDatabase.GetAssetPath(saved),"usage.txt")));
        }
        private void LoadExample()
        {
            var plan=AssetDatabase.LoadAssetAtPath<WavePlan>("Assets/WaveLabBattle/BattlePlan.asset");
            var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WaveLabBattle/Player.prefab");
            if(plan==null||p==null)throw new ArgumentException("예제 생성: Tools → 웨이브랩 → 플레이어 전투 테스트 씬 만들기. 생성 후 다시 눌러주세요.");
            ClearResult();campaign=false;manual=false;player=p;input=new BalanceInput{modelGrowth=false};
            foreach(var profile in plan.waves.SelectMany(w=>w.groups).Select(g=>g.enemy).Distinct())input.enemies.Add(new BalanceEnemy{profile=profile});
            notice="예제 준비 완료. 적 역할과 비중을 정한 뒤 다음으로 이동하세요.";
        }
        private void ReadStats()
        {
            if(manual)return;
            var p=Read(player,null,"플레이어");input.playerHealth=p[0];input.playerDamage=p[1];input.playerInterval=p[2];input.playerRange=p[3];input.playerMoveSpeed=p[4];
            foreach(var e in input.enemies){p=Read(e.profile?.prefab,e.profile,e.profile!=null?e.profile.name:"적");e.health=p[0];e.damage=p[1];e.attackInterval=p[2];e.attackRange=p[3];e.moveSpeed=p[4];}
        }
        private static float[] Read(GameObject prefab,EnemyProfile profile,string label)
        {
            string[] ids={"health.max","attack.damage","attack.interval","attack.range","movement.speed"};var values=new float[5];
            for(int i=0;i<5;i++){bool ok=i==2?BalanceStatReader.Interval(prefab,profile,out values[i]):BalanceStatReader.Read(prefab,profile,ids[i],out values[i]);if(!ok)throw new ArgumentException(label+" 스탯을 읽을 수 없습니다. ‘스탯 직접 입력’을 켜거나 적 프로필에서 숫자 필드를 연결하세요.");}return values;
        }
        private void Attempt(Action action){error=null;notice=null;try{action();}catch(ArgumentException ex){error=ex.Message;}catch(IOException ex){error="파일 저장 실패: "+ex.Message;}}
        private void Save()
        {
            var errors=WaveValidation.Errors(result.plan);if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors));
            string path=EditorUtility.SaveFilePanelInProject("웨이브 저장","설계한웨이브","asset","새 파일과 사용 안내를 저장합니다.");if(string.IsNullOrEmpty(path))return;
            path=AssetDatabase.GenerateUniqueAssetPath(path);saved=Instantiate(result.plan);AssetDatabase.CreateAsset(saved,path);
            string report=AutoBalance.Assumptions+"\n입력:\n"+JsonUtility.ToJson(input,true);
            foreach(var row in result.rows)report+="\n"+JsonUtility.ToJson(row,true);
            foreach(var warning in result.warnings)report+="\n"+warning;
            File.WriteAllText(Path.ChangeExtension(path,"balance.txt"),report);
            File.WriteAllText(Path.ChangeExtension(path,"usage.txt"),(campaign?"대상: Rushitadel 본편 일반 라운드\n":"대상: WaveRunner 테스트 씬\n")+Usage);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            if(campaign)InstallCampaign?.Invoke(saved);
            else PlanSaved?.Invoke(saved);
            notice=campaign?"저장 완료 · inGame에 자동 연결되었습니다. 진행 중인 전투는 유지하고 다음 라운드부터 반영됩니다.":"저장 완료";
            EditorGUIUtility.PingObject(saved);
        }
        private void OpenTest()
        {
            const string path="Assets/WaveLabBattle/BattleTest.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)throw new ArgumentException("Tools → 웨이브랩 → 플레이어 전투 테스트 씬 만들기를 먼저 실행하세요.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(path);runner=FindFirstObjectByType<WaveRunner>();
        }
        private void Apply()
        {
            if(saved.waves.SelectMany(w=>w.groups).Any(g=>!string.IsNullOrEmpty(g.enemy.gameKey)))throw new ArgumentException("게임 전용 유닛은 본편 적용 버튼을 사용하세요. 예제 전투 씬에서는 전용 동작을 구성하지 않습니다.");
            var old=runner.plan;runner.plan=saved;var errors=runner.ValidateSetup();runner.plan=old;
            if(errors.Count>0)throw new ArgumentException("실행기의 Spawn Areas에 영역을 연결하세요. 각 영역의 key는 계획의 spawnKey와 같아야 합니다.\n"+string.Join("\n",errors));
            Undo.RecordObject(runner,"Apply generated waves");runner.plan=saved;runner.playOnStart=true;
            EditorUtility.SetDirty(runner);EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);notice="적용 완료. Ctrl+S로 씬을 저장한 뒤 Play를 누르세요.";
        }
        public const string Usage="[제작] 1 유닛 → 2 전투 느낌 → 3 미리보기 → 4 저장·사용. 새 생성 결과를 저장하면 본편에 자동 연결됩니다. 기존 편성은 Tools → 웨이브랩 → 현재 본편 웨이브 수정에서 편집 후 저장하세요. 플레이 중 저장한 변경은 다음 라운드부터 적용됩니다.\n\n[본편] 우리 게임 유닛 불러오기 → 9라운드 생성 → 파일 저장(본편 자동 연결) → Title에서 새 게임. 보스·정비·테마는 기존 규칙입니다. 본편에는 WaveRunner를 추가하지 마세요. 연결 해제로 기존 편성에 복귀할 수 있습니다.\n\n[일반 테스트] 예제 불러오기 → 저장 → BattleTest 씬 열기 → 실행기에 적용 → Ctrl+S → Play. 사용자 프리팹은 사망 시 ReportDefeated(), 풀 반환 시 ReportDespawned()를 연결하세요.\n\n[조절] 너무 어렵다: 난이도/동시 적/엘리트 상한 감소. 단조롭다: 전투 흐름/역할/비중 변경. 너무 쉽다: 실측 유효 DPS를 보정. 실측 DPS는 실제 적 체력 감소량을 전투 시간으로 나눈 값이며 탄 제거량을 더하지 않습니다.\n\n[시간] 목표는 계획 기준이며 적 상한·배치 공간·잔여 적 정리 때문에 실제 시간은 늘어날 수 있습니다. 안전 정지 시 사망 알림과 스폰 영역 공간을 확인하세요. 휴식은 적 정리 뒤 시작합니다.\n\n[주의] 예상 HP·회복·성장은 계산값입니다. 실제 회복이나 업그레이드를 지급하지 않습니다. 광역·도탄·탄 파괴·전투 승률은 실제 플레이로 확인하세요.";
    }
}
