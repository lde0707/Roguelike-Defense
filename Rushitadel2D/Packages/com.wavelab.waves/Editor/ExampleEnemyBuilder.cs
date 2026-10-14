using System;
using UnityEditor;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves.Editor
{
    public static class ExampleEnemyBuilder
    {
        [MenuItem("Tools/웨이브랩/예시 적 3종 만들기")]
        public static void Create()
        {
            if(EditorApplication.isPlaying) return;
            string folder=Build();Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        public static void BuildBatch() { Debug.Log("WAVELAB_EXAMPLE_ENEMIES_OK " + Build()); }
        public static string Build()
        {
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/WaveLabExampleEnemies");
            AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
            BuildOne(folder,"01_약한적",PrimitiveType.Sphere,new Color(.35f,.8f,.65f),.65f,30,2,2.5f,1,3,10,3);
            BuildOne(folder,"02_빠른적",PrimitiveType.Capsule,new Color(1,.75f,.25f),.5f,50,3,1.5f,3,3,18,5);
            BuildOne(folder,"03_튼튼한적",PrimitiveType.Cube,new Color(.55f,.65f,1),1,160,8,3, .6f,4,40,12);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();return folder;
        }
        private static void BuildOne(string folder,string name,PrimitiveType shape,Color color,float scale,
            float hp,float damage,float interval,float speed,float range,float xp,float gold)
        {
            // Sprite shader keeps primitive sample colors visible without scene lights.
            var shader=Shader.Find("Sprites/Default");
            if(shader==null)throw new InvalidOperationException("Sprites/Default shader not available.");
            var material=new Material(shader){name=name+"_색상",color=color};
            AssetDatabase.CreateAsset(material,folder+"/"+name+".mat");
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.localScale=Vector3.one*scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<CircleCollider2D>().isTrigger=true;
            go.GetComponent<Renderer>().sharedMaterial=material;
            var stats=go.AddComponent<ExampleEnemy>();stats.maxHealth=hp;stats.attackDamage=damage;stats.attackInterval=interval;stats.moveSpeed=speed;stats.attackRange=range;
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,folder+"/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);
            var source=prefab.GetComponent<ExampleEnemy>();
            var profile=ScriptableObject.CreateInstance<EnemyProfile>();profile.prefab=prefab;
            profile.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=xp});
            profile.defeatRewards.Add(new ResourceAmount{resourceId="gold",amount=gold});
            Add(profile,source,"health.max","HP","maxHealth",hp);
            Add(profile,source,"attack.damage","HP","attackDamage",damage);
            Add(profile,source,"attack.interval","s","attackInterval",interval);
            Add(profile,source,"movement.speed","unit/s","moveSpeed",speed);
            Add(profile,source,"attack.range","unit","attackRange",range);
            AssetDatabase.CreateAsset(profile,folder+"/"+name+"_적프로필.asset");
            if(profile.prefab.GetComponent<ExampleEnemy>().maxHealth!=hp || profile.statBindings.Count!=5)
                throw new InvalidOperationException("Example enemy validation failed: "+name);
        }
        private static void Add(EnemyProfile p,ExampleEnemy source,string id,string unit,string field,float value)
        { p.statBindings.Add(new NumericBinding{statId=id,unit=unit,source=source,propertyPath=field,snapshot=value,sampledUtc=DateTime.UtcNow.ToString("O")}); }
    }
    [CustomEditor(typeof(ExampleEnemy))]
    public sealed class ExampleEnemyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            string[] fields={"maxHealth","attackDamage","attackInterval","moveSpeed","attackRange","moveOnSpawn","moveDirection"};
            string[] labels={"최대 체력","공격 1회 피해","공격 간격 (초)","이동속도","사거리","자동 이동 사용","이동 방향"};
            for(int i=0;i<fields.Length;i++)EditorGUILayout.PropertyField(serializedObject.FindProperty(fields[i]),new GUIContent(labels[i]));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("밸런스 입력·스폰·피격 연결용 예제입니다. 공격 AI·투사체 발사는 포함하지 않습니다. 이동은 켜야 작동합니다. 실제 게임 공격에서는 TakeDamage를 연결하세요.",MessageType.Info);
            if(Application.isPlaying)
            {
                var e=(ExampleEnemy)target;EditorGUILayout.LabelField("현재 체력",e.CurrentHealth.ToString());
                if(GUILayout.Button("테스트: 피해 10"))e.TakeDamage(10);
                if(GUILayout.Button("테스트: 처치"))e.TakeDamage(e.maxHealth);
            }
        }
    }
}
