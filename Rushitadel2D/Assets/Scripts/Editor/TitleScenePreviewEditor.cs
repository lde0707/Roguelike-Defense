#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Presentation;

namespace Rushitadel.Editor
{
    [CustomEditor(typeof(TitleScenePreview))]
    public sealed class TitleScenePreviewEditor:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var preview=(TitleScenePreview)target;
            EditorGUILayout.HelpBox(preview.guide,MessageType.Info);
            if(preview.pages!=null)foreach(var page in preview.pages)
            {
                if(page!=null&&GUILayout.Button(page.name))
                {
                    foreach(var item in preview.pages){Undo.RecordObject(item,"Select title preview");item.SetActive(item==page);}
                    EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);SceneView.RepaintAll();
                }
            }
            EditorGUILayout.HelpBox("게임 시작: 슬롯 선택 → 테마 → 도트 셔터 → 전투\n이어하기: 안전 저장 슬롯 복원\n설정: 표시 드롭다운 / 음량 / 조작\n종료: 확인 / 취소\nPlay 전용: 배경 스크롤, 커서 색 반전, 버튼 hover, 셔터 전환",MessageType.None);
        }
    }
    public static class TitleScenePreviewBuilder
    {
        [MenuItem("Rushitadel/Refresh Title Scene Preview")]
        public static void Refresh()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            foreach(var root in scene.GetRootGameObjects())
                if(root.GetComponent<TitleScenePreview>()!=null||root.GetComponentInChildren<Canvas>(true)!=null||root.GetComponent<UnityEngine.EventSystems.EventSystem>()!=null)Object.DestroyImmediate(root);
            var host=new GameObject("LATEST TITLE UI - Select Preview Page");host.tag="EditorOnly";
            var preview=host.AddComponent<TitleScenePreview>();preview.pages=new GameObject[9];
            string[] names={"00 타이틀","01 게임 시작 - 슬롯","02 이어하기 - 슬롯","03 설정 - 표시","04 설정 - 사운드","05 설정 - 조작","06 게임 종료 확인","07 시작 테마 선택","08 플레이어 Web"};
            for(int i=0;i<names.Length;i++)
            {
                var builder=new GameObject("Temporary UI builder").AddComponent<GameFrontend>();
                var page=builder.BuildEditorPreview(i);page.name=names[i];page.transform.SetParent(host.transform,false);
                preview.pages[i]=page;page.SetActive(i==0);Object.DestroyImmediate(builder.gameObject);
            }
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=host;
            Debug.Log("TITLE_SCENE_PREVIEW_READY 9 pages");
        }
        public static void BuildAndExit(){Refresh();EditorApplication.Exit(0);}
        public static void BuildAndValidate(){Refresh();TitleUiValidation.RunExisting();}
    }
}
#endif
