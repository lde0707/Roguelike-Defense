using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    [CustomEditor(typeof(EnemyProfile))]
    public sealed class EnemyProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"), new GUIContent("적 프리팹"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defeatRewards"), new GUIContent("처치 보상 (기존 보상과 중복 연결 금지)"), true);
            serializedObject.ApplyModifiedProperties();
            var p = (EnemyProfile)target;
            EditorGUILayout.HelpBox("스탯 연결은 읽기 전용 스냅샷입니다. 원본 스탯·HP를 변경하지 않습니다. Component 또는 데이터 에셋을 넣고 숫자 필드를 선택하세요.", MessageType.Info);
            for (int i = 0; i < p.statBindings.Count; i++)
            {
                var b = p.statBindings[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                string id = EditorGUILayout.TextField("스탯 ID", b.statId);
                string unit = EditorGUILayout.TextField("단위", b.unit);
                var src = EditorGUILayout.ObjectField("기존 데이터", b.source, typeof(UnityEngine.Object), false);
                if (EditorGUI.EndChangeCheck())
                { Undo.RecordObject(p, "Edit stat binding"); b.statId = id; b.unit = unit; if (b.source != src) b.propertyPath = ""; b.source = src; EditorUtility.SetDirty(p); }
                if (b.source is GameObject go)
                {
                    EditorGUILayout.LabelField("읽을 컴포넌트를 선택하세요.");
                    foreach (var c in go.GetComponents<Component>()) if (c != null && GUILayout.Button(c.GetType().Name))
                    { Undo.RecordObject(p, "Select component"); b.source = c; b.propertyPath = ""; EditorUtility.SetDirty(p); }
                }
                else if (b.source != null)
                {
                    var so = new SerializedObject(b.source); so.Update();
                    var paths = new List<string> { "(숫자 필드 선택)" };
                    var it = so.GetIterator();
                    while (it.NextVisible(true)) if (it.propertyType == SerializedPropertyType.Float || it.propertyType == SerializedPropertyType.Integer) paths.Add(it.propertyPath);
                    int current = Math.Max(0, paths.IndexOf(b.propertyPath));
                    int selected = EditorGUILayout.Popup("필드", current, paths.ToArray());
                    if (selected != current) { Undo.RecordObject(p, "Select stat field"); b.propertyPath = paths[selected]; EditorUtility.SetDirty(p); }
                    var prop = so.FindProperty(b.propertyPath ?? "");
                    bool numeric = prop != null && (prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Integer);
                    using (new EditorGUI.DisabledScope(!numeric)) if (GUILayout.Button("현재 값 읽기"))
                    { Undo.RecordObject(p, "Read stat snapshot"); b.snapshot = prop.propertyType == SerializedPropertyType.Float ? (float)prop.doubleValue : prop.longValue; b.sampledUtc = DateTime.UtcNow.ToString("O"); EditorUtility.SetDirty(p); }
                    if (!numeric) EditorGUILayout.HelpBox("숫자 필드 연결이 필요합니다. 프로퍼티·실행 중 계산값은 추후 어댑터가 필요합니다.", MessageType.Warning);
                }
                EditorGUILayout.LabelField($"저장된 값: {b.snapshot} {b.unit}", b.sampledUtc ?? "미수집");
                bool remove = GUILayout.Button("연결 삭제"); EditorGUILayout.EndVertical();
                if (remove) { Undo.RecordObject(p, "Remove binding"); p.statBindings.RemoveAt(i); EditorUtility.SetDirty(p); break; }
            }
            if (GUILayout.Button("+ 스탯 연결")) { Undo.RecordObject(p, "Add binding"); p.statBindings.Add(new NumericBinding()); EditorUtility.SetDirty(p); }
        }
    }
}
