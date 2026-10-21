using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    public sealed class WavePlacementWindow : EditorWindow
    {
        [SerializeField] WavePlan sourcePlan,target;
        [SerializeField] WaveTemplate template;
        [SerializeField] int sourceIndex,selected;
        Vector2 scroll;
        string notice;
        bool failed;
        [MenuItem("Tools/웨이브랩/웨이브 보관함 · 라운드 배치")]
        public static void Open(){GetWindow<WavePlacementWindow>("웨이브 배치").minSize=new Vector2(680,570);}
        public static void OpenPlan(WavePlan plan){Open();GetWindow<WavePlacementWindow>().target=plan;}
        void OnEnable(){Undo.undoRedoPerformed+=Repaint;}
        void OnDisable(){Undo.undoRedoPerformed-=Repaint;}
        WaveDefinition Source=>template!=null?template.wave:
            sourcePlan!=null&&sourcePlan.waves.Count>0?sourcePlan.waves[Mathf.Clamp(sourceIndex,0,sourcePlan.waves.Count-1)]:null;
        void Attempt(Action action)
        {
            try{action();failed=false;}
            catch(ArgumentException e){failed=true;notice=e.Message;}
            catch(System.IO.IOException e){failed=true;notice=e.Message;}
        }
        void Place(int index,bool replace)
        {
            Undo.RecordObject(target,"Place wave");
            WavePlacement.Place(target,Source,index,replace);
            selected=index;EditorUtility.SetDirty(target);
            notice="배치 완료 · 저장하면 연결된 실행 대상에 반영됩니다.";
        }
        void Move(int direction)
        {
            Undo.RecordObject(target,"Move round");
            var wave=target.waves[selected];target.waves.RemoveAt(selected);
            selected+=direction;target.waves.Insert(selected,wave);EditorUtility.SetDirty(target);
        }
        void SaveTemplate()
        {
            string path=EditorUtility.SaveFilePanelInProject("웨이브 보관","보관한웨이브","asset","여러 계획에서 재사용할 웨이브입니다.");
            if(string.IsNullOrEmpty(path))return;
            var asset=CreateInstance<WaveTemplate>();asset.wave=WavePlacement.Copy(Source);
            AssetDatabase.CreateAsset(asset,AssetDatabase.GenerateUniqueAssetPath(path));AssetDatabase.SaveAssets();
            template=asset;notice="보관함에 저장했습니다. 다른 계획에서도 이 파일을 선택해 배치하세요.";
        }
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("1. 가져올 웨이브",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("생성한 계획에서 한 웨이브를 고르거나 보관한 웨이브 파일을 선택하세요. 테마·스테이지·라운드 개수 제한 없이 사용할 수 있습니다.",MessageType.Info);
            template=(WaveTemplate)EditorGUILayout.ObjectField("보관한 웨이브",template,typeof(WaveTemplate),false);
            using(new EditorGUI.DisabledScope(template!=null))
            {
                sourcePlan=(WavePlan)EditorGUILayout.ObjectField("가져올 계획",sourcePlan,typeof(WavePlan),false);
                if(sourcePlan!=null&&sourcePlan.waves.Count>0)
                    sourceIndex=EditorGUILayout.Popup("가져올 웨이브",Mathf.Clamp(sourceIndex,0,sourcePlan.waves.Count-1),
                        sourcePlan.waves.Select((w,i)=>$"{i+1:00} · {w.title}").ToArray());
            }
            if(Source!=null)EditorGUILayout.LabelField($"{Source.title} · 그룹 {Source.groups.Count}개 · 적 {Source.groups.Sum(g=>g.count)}마리",EditorStyles.wordWrappedLabel);
            using(new EditorGUI.DisabledScope(Source==null||EditorApplication.isPlaying))
                if(GUILayout.Button("선택한 웨이브를 보관함 파일로 저장"))Attempt(SaveTemplate);
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("2. 배치할 계획과 라운드",EditorStyles.boldLabel);
            var next=(WavePlan)EditorGUILayout.ObjectField("대상 계획",target,typeof(WavePlan),false);
            if(next!=target){target=next;selected=0;notice=null;}
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if(GUILayout.Button("새 배치 계획 만들기"))Attempt(()=>{
                    var path=EditorUtility.SaveFilePanelInProject("배치 계획 저장","라운드배치","asset","원하는 씬의 WaveRunner에 연결할 계획입니다.");
                    if(string.IsNullOrEmpty(path))return;
                    target=CreateInstance<WavePlan>();
                    if(sourcePlan!=null){target.maximumAlive=sourcePlan.maximumAlive;target.maximumElites=sourcePlan.maximumElites;target.minimumSpawnSpacing=sourcePlan.minimumSpawnSpacing;target.seed=sourcePlan.seed;}
                    AssetDatabase.CreateAsset(target,AssetDatabase.GenerateUniqueAssetPath(path));selected=0;
                });
                if(target!=null)
                {
                    selected=Mathf.Clamp(selected,0,Mathf.Max(0,target.waves.Count-1));
                    for(int i=0;i<target.waves.Count;i++)
                        if(GUILayout.Toggle(selected==i,$"{i+1:00}  {target.waves[i].title}  · 적 {target.waves[i].groups.Sum(g=>g.count)}마리","Button",GUILayout.Height(28)))selected=i;
                    if(target.waves.Count>0)
                    {
                        var title=EditorGUILayout.TextField("선택 위치 이름",target.waves[selected].title);
                        if(title!=target.waves[selected].title){Undo.RecordObject(target,"Name round");target.waves[selected].title=title;EditorUtility.SetDirty(target);}
                        EditorGUILayout.BeginHorizontal();
                        using(new EditorGUI.DisabledScope(selected==0))if(GUILayout.Button("위로 이동"))Move(-1);
                        using(new EditorGUI.DisabledScope(selected==target.waves.Count-1))if(GUILayout.Button("아래로 이동"))Move(1);
                        if(GUILayout.Button("선택 위치 삭제")){Undo.RecordObject(target,"Remove round");target.waves.RemoveAt(selected);selected=Mathf.Max(0,selected-1);EditorUtility.SetDirty(target);}
                        EditorGUILayout.EndHorizontal();
                    }
                    using(new EditorGUI.DisabledScope(Source==null))
                    {
                        EditorGUILayout.BeginHorizontal();
                        if(GUILayout.Button("맨 뒤에 추가",GUILayout.Height(32)))Attempt(()=>Place(target.waves.Count,false));
                        using(new EditorGUI.DisabledScope(target.waves.Count==0))
                        {
                            if(GUILayout.Button("선택 위치 앞에 삽입",GUILayout.Height(32)))Attempt(()=>Place(selected,false));
                            if(GUILayout.Button("선택 위치 교체",GUILayout.Height(32)))Attempt(()=>Place(selected,true));
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.HelpBox("교체는 위치 이름을 유지합니다. 배치한 웨이브는 독립 복사본이므로 원본을 고쳐도 다른 배치는 바뀌지 않습니다. 공통 적 상한·시드는 대상 계획 설정을 사용합니다.",MessageType.None);
                    if(GUILayout.Button("3. 배치 저장",GUILayout.Height(36)))Attempt(()=>{
                        var errors=WaveValidation.Errors(target);
                        if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors.Take(5)));
                        AssetDatabase.SaveAssets();WaveQuickStartWindow.PlanSaved?.Invoke(target);
                        notice="저장 완료 · 이 계획이 연결된 실행 대상에서 새 실행 시 사용됩니다. 본편 연결 대상은 다음 라운드부터 반영됩니다.";
                    });
                    if(GUILayout.Button("대상 계획 상세 편집"))WaveLabWindow.OpenPlan(target);
                }
            }
            if(!string.IsNullOrEmpty(notice))EditorGUILayout.HelpBox(notice,failed?MessageType.Error:MessageType.Info);
            EditorGUILayout.HelpBox("일반 게임: 씬별 WaveRunner의 Plan에 대상 계획을 연결하면 위에서 아래 순서로 실행됩니다. startWaveIndex로 시작 위치를 고를 수 있습니다.\n러시타델: ‘현재 본편 웨이브 수정’에서 사용하는 계획을 대상으로 선택하세요. 본편 연결의 9라운드 규칙은 공용 툴의 제한이 아닙니다.",MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }
}
