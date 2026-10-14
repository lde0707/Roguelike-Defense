using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    [CustomEditor(typeof(SpawnArea))]
    public sealed class SpawnAreaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("영역 이름", "적 그룹의 스폰 영역 키와 같은 값을 입력하세요."));
            var shape = serializedObject.FindProperty("shape");
            shape.enumValueIndex = EditorGUILayout.Popup("등장 범위", shape.enumValueIndex, new[] { "고정 지점", "사각형 안에서 무작위", "원형 띠 안에서 무작위" });
            var plane = serializedObject.FindProperty("plane");
            plane.enumValueIndex = EditorGUILayout.Popup("맵 방향", plane.enumValueIndex, new[] { "2D 평면 (XY)", "3D 바닥 (XZ)" });
            if (shape.enumValueIndex == 1) EditorGUILayout.PropertyField(serializedObject.FindProperty("size"), new GUIContent("영역 크기 (가로·세로)"));
            if (shape.enumValueIndex == 2)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("innerRadius"), new GUIContent("안쪽 반지름 (비워둘 범위)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("outerRadius"), new GUIContent("바깥쪽 반지름"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("follow"), new GUIContent("따라갈 대상 (선택)", "플레이어나 카메라를 넣으면 대상 주변에서 등장합니다."));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("따라갈 대상이 있으면 이 오브젝트의 위치는 대상에서 떨어진 거리로 사용합니다. 장애물·최소 거리 검사는 아직 제공하지 않습니다.", MessageType.Info);
        }
        private void OnSceneGUI()
        {
            var a=(SpawnArea)target;Handles.color=Color.cyan;
            Handles.Label(a.Center, $"WaveLab: {a.key}");
            EditorGUI.BeginChangeCheck();
            var center=Handles.PositionHandle(a.Center,a.transform.rotation);
            if(EditorGUI.EndChangeCheck()) {Undo.RecordObject(a.transform,"Move spawn area");a.transform.position += center-a.Center;}
            if(a.shape==SpawnShape.Annulus)
            {
                EditorGUI.BeginChangeCheck();
                var rotation=a.plane==SpawnPlane.XY?a.transform.rotation*Quaternion.Euler(90,0,0):a.transform.rotation;
                float outer=Handles.RadiusHandle(rotation,a.Center,a.outerRadius);
                if(EditorGUI.EndChangeCheck()){Undo.RecordObject(a,"Resize spawn radius");a.outerRadius=Mathf.Max(a.innerRadius,outer);EditorUtility.SetDirty(a);}
            }
        }
    }
    [CustomEditor(typeof(WaveRunner))]
    public sealed class WaveRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("웨이브 실행 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("plan"), new GUIContent("실행할 웨이브 계획"));
            var areas = serializedObject.FindProperty("spawnAreas");
            EditorGUILayout.LabelField("연결할 스폰 영역", EditorStyles.boldLabel);
            for (int i = 0; i < areas.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(areas.GetArrayElementAtIndex(i), new GUIContent($"영역 {i + 1}"));
                if (GUILayout.Button("삭제", GUILayout.Width(45))) { areas.GetArrayElementAtIndex(i).objectReferenceValue = null; areas.DeleteArrayElementAtIndex(i); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 스폰 영역 연결")) { areas.arraySize++; areas.GetArrayElementAtIndex(areas.arraySize - 1).objectReferenceValue = null; }
            var start = serializedObject.FindProperty("startWaveIndex");
            start.intValue = Mathf.Max(0, EditorGUILayout.IntField("시작 웨이브 번호", start.intValue + 1) - 1);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnStart"), new GUIContent("플레이 시 자동 실행"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("safetyTimeoutSeconds"), new GUIContent("안전 정지 시간 (초)", "처치 알림이 오지 않아 진행이 멈추면 이 시간 후 중지합니다."));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("게임 기능 연결 (개발자용)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onWaveStarted"), new GUIContent("웨이브 시작 시"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onWaveCompleted"), new GUIContent("웨이브 완료 시"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onReward"), new GUIContent("처치 보상 지급 시"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlanCompleted"), new GUIContent("전체 웨이브 완료 시"));
            serializedObject.ApplyModifiedProperties();
            var r=(WaveRunner)target;
            if(GUILayout.Button("연결 검사"))
            {var errors=r.ValidateSetup();EditorUtility.DisplayDialog("WaveLab 검사",errors.Count==0?"연결 검사 통과":string.Join("\n",errors),"확인");}
            using(new EditorGUI.DisabledScope(!Application.isPlaying))
            {if(GUILayout.Button("실행"))r.Play();if(GUILayout.Button("중지"))r.Stop();}
            EditorGUILayout.HelpBox(r.Status+"\n적 처치 스크립트는 SpawnedUnit.ReportDefeated()를 호출해야 보상이 발생합니다. 비활성화/풀 반환은 ReportDespawned()를 호출하세요.",MessageType.Info);
        }
    }

    [CustomEditor(typeof(WavePlan))]
    public sealed class WavePlanEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var plan = (WavePlan)target;
            EditorGUILayout.LabelField("웨이브 계획", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("등록된 웨이브", plan.waves.Count + "개");
            EditorGUILayout.HelpBox("전용 편집기에서 적·수량·등장 위치·시간을 설정하세요.", MessageType.Info);
            if (GUILayout.Button("이 계획 편집하기")) WaveLabWindow.OpenPlan(plan);
        }
    }

    [CustomEditor(typeof(DemoLifetime))]
    public sealed class DemoLifetimeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seconds"), new GUIContent("자동 처치까지 시간 (초)"));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("연결 확인용 예제입니다. 실제 적의 체력이나 전투 기능이 아닙니다.", MessageType.Info);
        }
    }
}
