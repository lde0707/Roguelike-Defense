using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    public sealed class WaveLabWindow : EditorWindow
    {
        [SerializeField] private WavePlan plan;
        [SerializeField] private int waveIndex;
        [SerializeField] private int groupIndex;
        private Vector2 leftScroll, middleScroll, rightScroll;
        private List<string> errors = new List<string>();
        private bool inspected;
        private string saveNotice;
        [MenuItem("Tools/웨이브랩/웨이브 편집기")]
        public static void StartEasy(){WaveQuickStartWindow.Open();}
        [MenuItem("Tools/웨이브랩/세부 편집기 (고급)")]
        public static void Open() { var w = GetWindow<WaveLabWindow>("웨이브 편집기"); w.minSize = new Vector2(900, 520); }
        public static void OpenPlan(WavePlan selected) { Open(); var w = GetWindow<WaveLabWindow>(); w.plan = selected; w.waveIndex = w.groupIndex = 0; }
        private void OnEnable() { Undo.undoRedoPerformed += Repaint; }
        private void OnDisable() { Undo.undoRedoPerformed -= Repaint; }
        private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var next = (WavePlan)EditorGUILayout.ObjectField(plan, typeof(WavePlan), false, GUILayout.MinWidth(180));
            if (next != plan) { plan = next; waveIndex = groupIndex = 0; inspected = false; }
            if (GUILayout.Button("새 웨이브 계획", EditorStyles.toolbarButton)) CreatePlan();
            if (GUILayout.Button("적 등록", EditorStyles.toolbarButton)) CreateEnemy();
            if (GUILayout.Button("웨이브 배치", EditorStyles.toolbarButton)) WavePlacementWindow.OpenPlan(plan);
            if (GUILayout.Button("자동 생성", EditorStyles.toolbarButton)) AutoBalanceWindow.Open();
            if (GUILayout.Button("검사", EditorStyles.toolbarButton)) { inspected = true; errors = WaveValidation.Errors(plan); }
            if (GUILayout.Button("저장", EditorStyles.toolbarButton))
            {
                try { AssetDatabase.SaveAssets(); WaveQuickStartWindow.PlanSaved?.Invoke(plan); saveNotice="저장 완료. 본편 프로필을 사용한 계획은 inGame에 연결되며 다음 라운드부터 반영됩니다."; }
                catch(ArgumentException ex){saveNotice="본편 연결 실패: "+ex.Message;}
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("‘자동 생성’에서 스탯·목표 시간으로 초안을 만든 뒤 여기서 수정하세요. 위치·도탄·실제 생존율을 검증하는 전투 시뮬레이션은 아직 제공하지 않습니다.", MessageType.Info);
            if (plan == null) { EditorGUILayout.LabelField("새 웨이브 계획을 만들거나 웨이브 계획 파일을 끌어 넣으세요.", EditorStyles.boldLabel); return; }
            // Never change the authoring asset during a run.
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                DrawWaves(); DrawGroups(); DrawDetails();
                EditorGUILayout.EndHorizontal();
            }
            if(!string.IsNullOrEmpty(saveNotice))EditorGUILayout.HelpBox(saveNotice,MessageType.Info);
            DrawTestControls();
            if (inspected)
            {
                errors = WaveValidation.Errors(plan);
                EditorGUILayout.HelpBox(errors.Count == 0 ? "플랜 기본 검사 통과. 씬 연결은 실행기에서 별도로 검사합니다." : string.Join("\n", errors.GetRange(0, Math.Min(5, errors.Count))), errors.Count == 0 ? MessageType.Info : MessageType.Error);
                if (errors.Count > 5) EditorGUILayout.LabelField($"외 {errors.Count - 5}개 — 첫 오류부터 수정하세요.");
            }
        }
        private void Change(string title, Action action)
        { Undo.RecordObject(plan, title); action(); EditorUtility.SetDirty(plan); }
        private void DrawWaves()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(190));
            EditorGUILayout.LabelField("웨이브 목록", EditorStyles.boldLabel);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            for (int i = 0; i < plan.waves.Count; i++)
                if (GUILayout.Toggle(waveIndex == i, $"{i + 1:00}  {plan.waves[i].title}", "Button")) { if (waveIndex != i) groupIndex = 0; waveIndex = i; }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("+ 웨이브")) Change("Add wave", () => { plan.waves.Add(new WaveDefinition { title = "웨이브 " + (plan.waves.Count + 1) }); waveIndex = plan.waves.Count - 1; groupIndex = 0; });
            if (plan.waves.Count > 0)
            {
                waveIndex = Mathf.Clamp(waveIndex, 0, plan.waves.Count - 1);
                if (GUILayout.Button("현재 웨이브 복제")) Change("Duplicate wave", () =>
                {
                    var copy = JsonUtility.FromJson<WaveDefinition>(JsonUtility.ToJson(plan.waves[waveIndex]));
                    copy.id = Guid.NewGuid().ToString("N"); copy.title += " 복사본";
                    foreach (var g in copy.groups) g.id = Guid.NewGuid().ToString("N");
                    plan.waves.Insert(waveIndex + 1, copy); waveIndex++; groupIndex = 0;
                });
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(waveIndex == 0)) if (GUILayout.Button("↑")) Move(-1);
                using (new EditorGUI.DisabledScope(waveIndex == plan.waves.Count - 1)) if (GUILayout.Button("↓")) Move(1);
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("웨이브 삭제") && EditorUtility.DisplayDialog("웨이브 삭제", "선택한 웨이브를 삭제할까요? 실행 취소로 복원할 수 있습니다.", "삭제", "취소"))
                    Change("Delete wave", () => { plan.waves.RemoveAt(waveIndex); waveIndex = Mathf.Max(0, waveIndex - 1); groupIndex = 0; });
            }
            EditorGUILayout.EndVertical();
        }
        private void Move(int direction)
        { Change("Move wave", () => { var w = plan.waves[waveIndex]; plan.waves.RemoveAt(waveIndex); waveIndex += direction; plan.waves.Insert(waveIndex, w); }); }
        private void DrawGroups()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(260));
            EditorGUILayout.LabelField("등장 그룹", EditorStyles.boldLabel);
            middleScroll = EditorGUILayout.BeginScrollView(middleScroll);
            if (plan.waves.Count > 0)
            {
                var w = plan.waves[waveIndex];
                for (int i = 0; i < w.groups.Count; i++)
                {
                    var g = w.groups[i];
                    var label = $"{i + 1}. {(g.enemy != null ? g.enemy.name : "적 미선택")} ×{g.count}\n{g.startSeconds:0.##}초 → {g.LastSpawnSeconds:0.##}초 | {g.spawnKey}";
                    if (GUILayout.Toggle(groupIndex == i, label, "Button", GUILayout.Height(52))) groupIndex = i;
                }
                if (GUILayout.Button("+ 적 그룹")) Change("Add group", () => { w.groups.Add(new SpawnGroup()); groupIndex = w.groups.Count - 1; });
                if (w.groups.Count > 0)
                {
                    groupIndex = Mathf.Clamp(groupIndex, 0, w.groups.Count - 1);
                    if (GUILayout.Button("그룹 복제")) Change("Duplicate group", () => { var copy = JsonUtility.FromJson<SpawnGroup>(JsonUtility.ToJson(w.groups[groupIndex])); copy.id = Guid.NewGuid().ToString("N"); w.groups.Insert(groupIndex + 1, copy); groupIndex++; });
                    if (GUILayout.Button("그룹 삭제")) Change("Delete group", () => { w.groups.RemoveAt(groupIndex); groupIndex = Mathf.Max(0, groupIndex - 1); });
                }
            }
            EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical();
        }
        private void DrawDetails()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.MinWidth(290));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
            if (plan.waves.Count > 0)
            {
                var so = new SerializedObject(plan); so.Update();
                var w = so.FindProperty("waves").GetArrayElementAtIndex(waveIndex);
                EditorGUILayout.LabelField("웨이브 설정", EditorStyles.boldLabel);
                Field(w, "title", "이름");
                var end = w.FindPropertyRelative("end");
                end.enumValueIndex = EditorGUILayout.Popup("완료 조건", end.enumValueIndex, new[] { "등장 종료 + 잔여 적 없음", "모두 등장 (적 이월)", "시간 경과 (적 이월)", "목표 시간 + 잔여 적 정리 (권장)" });
                if (end.enumValueIndex >= 2) Field(w, "durationSeconds", "목표 전투 시간 (초)");
                Field(w, "restSeconds", "완료 후 휴식 (초)");
                EditorGUILayout.PropertyField(so.FindProperty("maximumAlive"),new GUIContent("동시 적 상한 (0 = 무제한)"));
                EditorGUILayout.PropertyField(so.FindProperty("maximumElites"),new GUIContent("동시 엘리트 상한"));
                EditorGUILayout.PropertyField(so.FindProperty("minimumSpawnSpacing"),new GUIContent("최소 등장 간격 (거리)"));
                EditorGUILayout.PropertyField(so.FindProperty("seed"), new GUIContent("위치 추첨 시드"));
                var groups = w.FindPropertyRelative("groups");
                if (groups.arraySize > 0)
                {
                    groupIndex = Mathf.Clamp(groupIndex, 0, groups.arraySize - 1);
                    var g = groups.GetArrayElementAtIndex(groupIndex);
                    EditorGUILayout.Space(); EditorGUILayout.LabelField("선택한 적 그룹", EditorStyles.boldLabel);
                    Field(g, "enemy", "적 프로필"); Field(g, "count", "총 마릿수"); Field(g, "spawnKey", "스폰 영역 키");
                    Field(g,"role","역할 (엘리트 상한 판정)");
                    Field(g, "startSeconds", "첫 등장 (초)"); Field(g, "intervalSeconds", "배치 간격 (초)"); Field(g, "batchSize", "한 번에 등장할 수");
                    EditorGUILayout.HelpBox("그룹 시간은 웨이브 시작 기준입니다. 여러 그룹은 동시에 실행됩니다. 스폰 키는 씬의 스폰 영역와 일치해야 합니다.", MessageType.None);
                    if (g.FindPropertyRelative("enemy").objectReferenceValue != null && GUILayout.Button("적 스탯·보상 연결 열기")) Selection.activeObject = g.FindPropertyRelative("enemy").objectReferenceValue;
                }
                so.ApplyModifiedProperties();
            }
            EditorGUILayout.EndScrollView(); EditorGUILayout.EndVertical();
        }
        private static void Field(SerializedProperty parent, string name, string label) => EditorGUILayout.PropertyField(parent.FindPropertyRelative(name), new GUIContent(label), true);
        private void DrawTestControls()
        {
            EditorGUILayout.BeginHorizontal("box");
            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button("씬에 웨이브 실행기 추가"))
                { var go = new GameObject("웨이브 실행기"); Undo.RegisterCreatedObjectUndo(go, "Create WaveLab Runner"); go.AddComponent<WaveRunner>().plan = plan; Selection.activeGameObject = go; }
                if (GUILayout.Button("씬에 스폰 영역 추가"))
                { var go = new GameObject("스폰 영역"); Undo.RegisterCreatedObjectUndo(go, "Create spawn area"); go.AddComponent<SpawnArea>(); Selection.activeGameObject = go; }
                EditorGUILayout.LabelField("실행기에 스폰 영역을 연결한 뒤 플레이 모드에서 테스트하세요.");
            }
            else
            {
                var runner = FindFirstObjectByType<WaveRunner>();
                if (runner == null) EditorGUILayout.LabelField("현재 씬에 웨이브 실행기가 없습니다.");
                else
                {
                    using (new EditorGUI.DisabledScope(runner.IsRunning || runner.plan != plan))
                        if (GUILayout.Button("선택 웨이브부터 실행")) { runner.startWaveIndex = waveIndex; runner.Play(); }
                    if (GUILayout.Button("중지")) runner.Stop();
                    EditorGUILayout.LabelField($"{runner.Status} | 생성 {runner.SpawnedCount} / 잔여 {runner.AliveCount} / 처치 {runner.DefeatedCount}");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("테스트 성장 상태: 현재 씬의 실제 상태를 사용합니다. 이전 웨이브의 성장을 자동 복원하지 않습니다.", EditorStyles.miniLabel);
        }
        private void CreatePlan()
        {
            var path = EditorUtility.SaveFilePanelInProject("웨이브 플랜 저장", "WavePlan", "asset", ""); if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<WavePlan>(); asset.waves.Add(new WaveDefinition { title = "웨이브 1" });
            AssetDatabase.CreateAsset(asset, path); plan = asset; waveIndex = groupIndex = 0;
        }
        private void CreateEnemy()
        {
            var path = EditorUtility.SaveFilePanelInProject("적 프로필 저장", "EnemyProfile", "asset", ""); if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<EnemyProfile>(); AssetDatabase.CreateAsset(asset, path); Selection.activeObject = asset;
        }
    }
}
