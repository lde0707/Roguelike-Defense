#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WaveLab.Core;
using WaveLab.Waves;
using Rushitadel.Combat;
using Rushitadel.World;
using Rushitadel.Progression;
namespace Rushitadel.Editor
{
    public static class ThemedEnemyCatalog
    {
        public const string Prefabs="Assets/Prefabs/ThemedEnemies";
        public const string Profiles="Assets/WaveLabThemeProfiles";
        static readonly string[] Names={"일반 사수","빠른 사수","중장갑 포격수","저격수","지원 사수","엘리트 사수","엘리트 포격수"};
        static void Folder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));
            AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));
        }
        [MenuItem("Tools/웨이브랩/전체 테마 적 프리팹 준비")]
        public static void Prepare()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RangedEnemy.prefab");
            if(source==null)throw new System.ArgumentException("RangedEnemy 프리팹이 없습니다.");
            Folder(Prefabs);Folder(Profiles);
            foreach(PressureTheme theme in System.Enum.GetValues(typeof(PressureTheme)))
            {
                string folder=theme.ToString();Folder(Prefabs+"/"+folder);Folder(Profiles+"/"+folder);
                for(int i=0;i<7;i++)
                {
                    string code=i<5?"N0"+(i+1):"E0"+(i-4);
                    string path=Prefabs+"/"+folder+"/"+code+".prefab";
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(prefab==null)
                    {
                        var go=(GameObject)PrefabUtility.InstantiatePrefab(source);
                        try
                        {
                            go.name=folder+" · "+Names[i];
                            var identity=go.AddComponent<CampaignEnemyPrefab>();identity.variant=i;identity.overrideTheme=true;identity.theme=theme;
                            var art=go.GetComponent<SpriteRenderer>();
                            art.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/EnemyArt/"+EnemyArchetype.ThemeFolder(theme)+"/"+code+".png");
                            art.color=Color.white;
                            prefab=PrefabUtility.SaveAsPrefabAsset(go,path);
                        }
                        finally{Object.DestroyImmediate(go);}
                    }
                    Profile(Profiles+"/"+folder+"/"+code+"_"+Names[i]+".asset",prefab,"rushitadel:theme:"+(int)theme+":"+i,i>=5?35:20);
                }
            }
            Folder(Prefabs+"/Bosses");Folder(Profiles+"/Bosses");
            foreach(BossKind kind in System.Enum.GetValues(typeof(BossKind)))
            {
                string path=Prefabs+"/Bosses/"+kind+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(source);
                    try
                    {
                        go.name=kind+" Boss";
                        go.AddComponent<CampaignBossPrefab>().kind=kind;
                        go.GetComponent<EnemyRangedAttack>().enabled=false;
                        var cull=go.GetComponent<CullBehindCamera>();if(cull!=null)cull.enabled=false;
                        var art=go.GetComponent<SpriteRenderer>();
                        art.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/BossArt/"+kind+"/Body.png");
                        art.color=Color.white;
                        prefab=PrefabUtility.SaveAsPrefabAsset(go,path);
                    }
                    finally{Object.DestroyImmediate(go);}
                }
                Profile(Profiles+"/Bosses/"+kind+".asset",prefab,"rushitadel:boss:"+kind,100);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("테마 전용 적 35종 + 보스 4종 준비 완료. 기존 프리팹과 프로필 GUID는 유지됩니다.");
        }
        static void Profile(string path,GameObject prefab,string key,int xp)
        {
            var profile=AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
            if(profile!=null)return;
            profile=ScriptableObject.CreateInstance<EnemyProfile>();
            profile.prefab=prefab;profile.gameKey=key;
            profile.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=xp});
            AssetDatabase.CreateAsset(profile,path);
        }
    }
}
#endif
