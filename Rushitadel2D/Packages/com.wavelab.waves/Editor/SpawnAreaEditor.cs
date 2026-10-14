using UnityEditor;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    [CustomEditor(typeof(SpawnArea))]
    public sealed class SpawnAreaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Point: 고정 지점 / Rectangle: 사각형 / Annulus: 중심을 비운 원형 영역. Follow 사용 시 이 오브젝트의 위치는 월드 오프셋입니다. 장애물·최소 거리 검사는 아직 제공하지 않습니다.", MessageType.Info);
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
            DrawDefaultInspector();var r=(WaveRunner)target;
            if(GUILayout.Button("연결 검사"))
            {var errors=r.ValidateSetup();EditorUtility.DisplayDialog("WaveLab 검사",errors.Count==0?"연결 검사 통과":string.Join("\n",errors),"확인");}
            using(new EditorGUI.DisabledScope(!Application.isPlaying))
            {if(GUILayout.Button("실행"))r.Play();if(GUILayout.Button("중지"))r.Stop();}
            EditorGUILayout.HelpBox(r.Status+"\n적 처치 스크립트는 SpawnedUnit.ReportDefeated()를 호출해야 보상이 발생합니다. 비활성화/풀 반환은 ReportDespawned()를 호출하세요.",MessageType.Info);
        }
    }
}
