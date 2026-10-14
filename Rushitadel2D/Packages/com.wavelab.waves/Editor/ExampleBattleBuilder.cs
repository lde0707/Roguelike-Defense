using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace WaveLab.Waves.Editor
{
    public static class ExampleBattleBuilder
    {
        [MenuItem("Tools/웨이브랩/플레이어 전투 테스트 씬 만들기")]
        public static void Create()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            BuildBatch();
        }
        public static void BuildBatch()
        {
            string enemies = ExampleEnemyBuilder.Build();
            string folder = AssetDatabase.GenerateUniqueAssetPath("Assets/WaveLabBattle");
            AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("전투 카메라").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=8;camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.08f,.12f);
            var player=GameObject.CreatePrimitive(PrimitiveType.Sphere);player.name="플레이어";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            var material=new Material(Shader.Find("Sprites/Default")){color=new Color(.2f,.85f,1)};
            AssetDatabase.CreateAsset(material,folder+"/Player.mat");player.GetComponent<Renderer>().sharedMaterial=material;
            player.transform.position=new Vector3(0,-2,0);player.transform.localScale=Vector3.one*.8f;
            var stats=player.AddComponent<ExampleEnemy>();stats.maxHealth=250;stats.attackDamage=20;stats.attackInterval=.4f;stats.moveSpeed=2;stats.attackRange=6;
            var combat=player.AddComponent<ExampleCombat>();combat.playerTeam=true;combat.attackMode=ExampleAttackMode.Ranged;
            PrefabUtility.SaveAsPrefabAsset(player,folder+"/Player.prefab");
            var plan=ScriptableObject.CreateInstance<WavePlan>();
            string[] names={"01_약한적","02_빠른적","03_튼튼한적"};
            for(int i=0;i<3;i++)
            {
                var wave=new WaveDefinition{title="전투 테스트 "+(i+1),restSeconds=3};
                for(int j=0;j<=i;j++)wave.groups.Add(new SpawnGroup{enemy=AssetDatabase.LoadAssetAtPath<EnemyProfile>(enemies+"/"+names[j]+"_적프로필.asset"),count=j==2?1:3,startSeconds=j*1.2f,intervalSeconds=2});
                plan.waves.Add(wave);
            }
            AssetDatabase.CreateAsset(plan,folder+"/BattlePlan.asset");
            var area=new GameObject("적 등장 영역").AddComponent<SpawnArea>();area.shape=SpawnShape.Rectangle;area.size=new Vector2(10,2);area.transform.position=new Vector3(0,5,0);
            var runner=new GameObject("웨이브 실행기").AddComponent<WaveRunner>();runner.plan=plan;runner.spawnAreas=new[]{area};runner.playOnStart=true;
            var hud=new GameObject("전투 상태").AddComponent<ExampleBattleHud>();hud.player=stats;hud.runner=runner;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),folder+"/BattleTest.unity");
            AssetDatabase.SaveAssets();Debug.Log("WAVELAB_BATTLE_OK "+folder+" enemies="+enemies);
        }
    }
}
