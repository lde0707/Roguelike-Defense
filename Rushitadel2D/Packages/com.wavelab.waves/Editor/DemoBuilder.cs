using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves.Editor
{
    public static class DemoBuilder
    {
        [MenuItem("Tools/WaveLab/Create Separate Demo Scene")]
        public static void Create()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var folder=AssetDatabase.GenerateUniqueAssetPath("Assets/WaveLabDemo");
            AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Demo Camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=7;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.08f,.1f,.14f);camera.clearFlags=CameraClearFlags.SolidColor;
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Demo unit - auto defeat in 2 seconds";cube.AddComponent<DemoLifetime>();
            var prefab=PrefabUtility.SaveAsPrefabAsset(cube,folder+"/DemoEnemy.prefab");Object.DestroyImmediate(cube);
            var enemy=ScriptableObject.CreateInstance<EnemyProfile>();enemy.prefab=prefab;enemy.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=5});AssetDatabase.CreateAsset(enemy,folder+"/DemoEnemy.asset");
            var plan=ScriptableObject.CreateInstance<WavePlan>();
            for(int i=0;i<3;i++)plan.waves.Add(new WaveDefinition{title="Demo "+(i+1),restSeconds=1,groups=new List<SpawnGroup>{new SpawnGroup{enemy=enemy,count=4+i*2,intervalSeconds=.5f}}});
            AssetDatabase.CreateAsset(plan,folder+"/DemoPlan.asset");
            var area=new GameObject("Spawn area").AddComponent<SpawnArea>();area.shape=SpawnShape.Rectangle;area.size=new Vector2(10,6);
            var runner=new GameObject("WaveLab Runner").AddComponent<WaveRunner>();runner.plan=plan;runner.spawnAreas=new[]{area};runner.playOnStart=true;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),folder+"/WaveLabDemo.unity");AssetDatabase.SaveAssets();Selection.activeObject=plan;WaveLabWindow.Open();
            EditorUtility.DisplayDialog("데모 생성 완료","독립 씬을 만들었습니다. Play를 누르면 3개 웨이브가 실행됩니다. 큐브는 2초 후 자동 처치되는 연결 검증용이며 전투/밸런스 시뮬레이션이 아닙니다.","확인");
        }
    }
}
