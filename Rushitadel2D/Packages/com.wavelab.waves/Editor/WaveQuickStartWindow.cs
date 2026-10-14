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
        [SerializeField] private BalanceInput input=new BalanceInput{modelGrowth=false};
        [SerializeField] private GameObject player;
        private BalanceResult result;
        private WavePlan saved;
        private WaveRunner runner;
        private int step;
        private Vector2 scroll;
        private bool details;
        private string error,notice;
        [MenuItem("Tools/웨이브랩/시작하기 (쉬운 자동 생성)",false,0)]
        public static void Open(){GetWindow<WaveQuickStartWindow>("웨이브 만들기").minSize=new Vector2(540,460);}
        private void OnDisable(){Clear();step=0;}
        private void Clear(){if(result?.plan!=null&&!AssetDatabase.Contains(result.plan))DestroyImmediate(result.plan);result=null;saved=null;}
        private void OnGUI()
        {
            EditorGUILayout.Space(10);EditorGUILayout.LabelField("웨이브 만들기",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("① 유닛 선택   →   ② 시간·난이도   →   ③ 저장·테스트");
            scroll=EditorGUILayout.BeginScrollView(scroll);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {if(step==0)Units();else if(step==1)Goal();else Result();}
            if(EditorApplication.isPlaying)EditorGUILayout.HelpBox("Play를 종료한 뒤 설정을 변경하세요.",MessageType.Info);
            if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
            if(!string.IsNullOrEmpty(notice))EditorGUILayout.HelpBox(notice,MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
        private void Units()
        {
            EditorGUILayout.HelpBox("처음이라면 ‘예제로 채우기’를 누르세요. 직접 설정하려면 Project 창에서 파일을 끌어 넣으면 됩니다.",MessageType.Info);
            if(GUILayout.Button("처음 사용해요 — 예제로 채우기",GUILayout.Height(34)))LoadExample();
            EditorGUILayout.Space(10);EditorGUI.BeginChangeCheck();
            player=(GameObject)EditorGUILayout.ObjectField("플레이어 프리팹",player,typeof(GameObject),false);
            EditorGUILayout.LabelField("등장할 적",EditorStyles.boldLabel);
            for(int i=0;i<input.enemies.Count;i++)
            {
                EditorGUILayout.BeginHorizontal();
                input.enemies[i].profile=(EnemyProfile)EditorGUILayout.ObjectField("적 "+(i+1),input.enemies[i].profile,typeof(EnemyProfile),false);
                bool remove=GUILayout.Button("빼기",GUILayout.Width(48));EditorGUILayout.EndHorizontal();
                if(remove){input.enemies.RemoveAt(i);GUI.changed=true;break;}
            }
            if(GUILayout.Button("+ 적 추가")){input.enemies.Add(new BalanceEnemy());GUI.changed=true;}
            if(EditorGUI.EndChangeCheck()){Clear();error=null;notice=null;}
            EditorGUILayout.LabelField("적은 ‘적프로필.asset’ 파일을 선택하세요. 체력·사거리·공격속도는 자동으로 읽습니다.",EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(12);
            using(new EditorGUI.DisabledScope(player==null||input.enemies.Count==0||input.enemies.Any(e=>e.profile==null)))
                if(GUILayout.Button("다음 — 시간과 난이도",GUILayout.Height(36)))
                    Attempt(()=>{ReadStats();step=1;scroll=Vector2.zero;});
            Advanced();
        }
        private void Goal()
        {
            EditorGUILayout.HelpBox("시간과 난이도만 정하세요. 웨이브 수와 적 수는 자동으로 정해집니다.",MessageType.Info);
            EditorGUI.BeginChangeCheck();
            input.totalSeconds=EditorGUILayout.FloatField("총 플레이 시간 (분)",input.totalSeconds/60f)*60f;
            input.difficulty=EditorGUILayout.Popup("난이도",input.difficulty,new[]{"쉬움","보통","어려움"});
            if(EditorGUI.EndChangeCheck())Clear();
            EditorGUILayout.LabelField("기본: 성장 계산 끔 · 휴식 5초 · 조우 거리 6유닛\n실제 맵이나 성장 규칙이 다르면 고급 설정에서 바꾸세요.",EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(12);
            if(GUILayout.Button("웨이브 만들기",GUILayout.Height(38)))Attempt(()=>{Clear();ReadStats();result=AutoBalance.Generate(input);step=2;details=false;scroll=Vector2.zero;});
            if(GUILayout.Button("← 유닛 다시 선택")){step=0;error=null;notice=null;}
            Advanced();
        }
        private void Result()
        {
            if(result==null){step=1;return;}
            EditorGUILayout.LabelField($"완료! {result.rows.Count}웨이브 · 총 {result.rows.Sum(r=>r.count)}마리 · 약 {input.totalSeconds/60:0.#}분",EditorStyles.boldLabel);
            foreach(var r in result.rows)EditorGUILayout.LabelField($"{r.wave}웨이브     {r.count}마리     전투 {r.seconds:0.#}초");
            EditorGUILayout.HelpBox("예상치이므로 실제 전투 테스트가 필요합니다. 아래 순서대로 진행하세요.",MessageType.Info);
            details=EditorGUILayout.Foldout(details,"계산 상세·주의사항 보기",true);
            if(details)
            {
                foreach(var r in result.rows)EditorGUILayout.LabelField($"{r.wave}웨이브: DPS {r.effectiveDps:0.#} · 예상 피해 {r.estimatedDamage:0.#} · XP +{r.xpEarned:0.#} · 골드 +{r.goldEarned:0.#}\n{r.note}",EditorStyles.wordWrappedLabel);
                foreach(var warning in result.warnings)EditorGUILayout.HelpBox(warning,MessageType.Warning);
                EditorGUILayout.HelpBox(AutoBalance.Assumptions,MessageType.Info);
            }
            if(saved==null){if(GUILayout.Button("1. 웨이브 파일 저장",GUILayout.Height(36)))Attempt(Save);}
            else
            {
                EditorGUILayout.LabelField("저장 완료: "+AssetDatabase.GetAssetPath(saved),EditorStyles.wordWrappedMiniLabel);
                if(GUILayout.Button("2. 전투 테스트 씬 열기",GUILayout.Height(32)))OpenTest();
                runner=(WaveRunner)EditorGUILayout.ObjectField("적용할 실행기",runner,typeof(WaveRunner),true);
                using(new EditorGUI.DisabledScope(runner==null||EditorUtility.IsPersistent(runner)))
                    if(GUILayout.Button("3. 저장한 웨이브 적용",GUILayout.Height(32)))Attempt(Apply);
                EditorGUILayout.HelpBox("적용 후 Ctrl+S로 씬을 저장하고 Unity 상단 ▶ Play를 누르세요. 기존 데모가 아닌 전투 테스트 씬에서 실행하세요. 이 버튼은 웨이브만 바꾸며 씬의 플레이어/스폰 위치는 직접 맞춰야 합니다.",MessageType.Info);
                if(GUILayout.Button("세부 편집기 열기 (선택)"))WaveLabWindow.OpenPlan(saved);
            }
            if(GUILayout.Button("← 시간·난이도 바꾸기")){step=1;error=null;notice=null;}
        }
        private void Advanced()
        {
            EditorGUILayout.Space(12);
            if(GUILayout.Button("고급 설정이 필요해요 (별도 창)"))AutoBalanceWindow.OpenAdvanced(input,player);
            EditorGUILayout.LabelField("직접 스탯 연결·수동 입력·성장·스폰 거리 등은 고급 창에서 별도로 생성합니다. 고급 창 변경은 이 화면에 덮어쓰지 않습니다.",EditorStyles.wordWrappedMiniLabel);
        }
        private void LoadExample()
        {
            var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WaveLabBattle/Player.prefab");
            var plan=AssetDatabase.LoadAssetAtPath<WavePlan>("Assets/WaveLabBattle/BattlePlan.asset");
            if(p==null||plan==null){error="예제가 없습니다. Tools → 웨이브랩 → 플레이어 전투 테스트 씬 만들기를 먼저 실행하세요.";return;}
            Clear();player=p;input=new BalanceInput{modelGrowth=false};
            foreach(var profile in plan.waves.SelectMany(w=>w.groups).Select(g=>g.enemy).Where(e=>e!=null).Distinct())input.enemies.Add(new BalanceEnemy{profile=profile});
            error=null;notice="예제 유닛을 채웠습니다. ‘다음’을 누르세요.";
        }
        private void ReadStats()
        {
            var p=Read(player,null,"플레이어");input.playerHealth=p[0];input.playerDamage=p[1];input.playerInterval=p[2];input.playerRange=p[3];input.playerMoveSpeed=p[4];
            foreach(var e in input.enemies){p=Read(e.profile?.prefab,e.profile,e.profile!=null?e.profile.name:"적");e.health=p[0];e.damage=p[1];e.attackInterval=p[2];e.attackRange=p[3];e.moveSpeed=p[4];}
        }
        private static float[] Read(GameObject prefab,EnemyProfile profile,string name)
        {
            var ids=new[]{"health.max","attack.damage","attack.interval","attack.range","movement.speed"};
            var labels=new[]{"체력","공격력","공격 간격","사거리","이동속도"};var values=new float[5];
            for(int i=0;i<5;i++)
            {
                bool ok=i==2?BalanceStatReader.Interval(prefab,profile,out values[i]):BalanceStatReader.Read(prefab,profile,ids[i],out values[i]);
                if(!ok)throw new ArgumentException(name+"의 "+labels[i]+"를 읽지 못했습니다. ‘고급 설정이 필요해요’를 눌러 스탯 연결 또는 수동 입력을 사용하세요.");
            }
            return values;
        }
        private void Attempt(Action action){error=null;notice=null;try{action();}catch(ArgumentException ex){error=ex.Message;}}
        private void Save()
        {
            string path=EditorUtility.SaveFilePanelInProject("웨이브 저장","자동웨이브","asset","새 파일로 저장합니다.");if(string.IsNullOrEmpty(path))return;
            path=AssetDatabase.GenerateUniqueAssetPath(path);saved=Instantiate(result.plan);AssetDatabase.CreateAsset(saved,path);
            string report=AutoBalance.Assumptions+"\n설정:\n"+JsonUtility.ToJson(input,true);
            foreach(var row in result.rows)report+="\n"+JsonUtility.ToJson(row,true);
            foreach(var warning in result.warnings)report+="\n주의: "+warning;
            File.WriteAllText(Path.ChangeExtension(path,"balance.txt"),report);AssetDatabase.SaveAssets();AssetDatabase.Refresh();EditorGUIUtility.PingObject(saved);
        }
        private void OpenTest()
        {
            const string path="Assets/WaveLabBattle/BattleTest.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null){error="전투 테스트 씬이 없습니다. Tools → 웨이브랩 → 플레이어 전투 테스트 씬 만들기를 먼저 실행하세요.";return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(path);runner=UnityEngine.Object.FindFirstObjectByType<WaveRunner>();error=null;notice=null;
        }
        private void Apply()
        {
            var old=runner.plan;runner.plan=saved;var errors=runner.ValidateSetup();runner.plan=old;
            if(errors.Count>0)throw new ArgumentException("씬 연결 확인이 필요합니다:\n"+string.Join("\n",errors));
            Undo.RecordObject(runner,"Apply generated waves");runner.plan=saved;runner.playOnStart=true;
            EditorUtility.SetDirty(runner);EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            notice="적용했습니다. Ctrl+S로 씬을 저장한 뒤 ▶ Play를 누르세요.";
        }
    }
}
