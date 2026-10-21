#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WaveLab.Core;
using WaveLab.Waves;
using WaveLab.Waves.Editor;
using Rushitadel.Progression;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Editor
{
    [InitializeOnLoad]
    public static class CampaignWaveDesigner
    {
        private const string SettingsPath="Assets/Resources/WaveLabCampaign.asset";
        static CampaignWaveDesigner()
        {
            WaveQuickStartWindow.LoadCampaignPreset=Load;
            WaveQuickStartWindow.InstallCampaign=Install;
            WaveQuickStartWindow.RemoveCampaign=Remove;
            WaveQuickStartWindow.PlanSaved=AutoInstall;
        }
        public static void AutoInstall(WavePlan plan)
        {
            // Generic libraries and other scene plans must not replace the campaign implicitly.
            var settings=AssetDatabase.LoadAssetAtPath<CampaignWaveSettings>(SettingsPath);
            if(plan == null || settings == null || settings.plan != plan) return;
            Install(plan);
        }
        [MenuItem("Tools/웨이브랩/우리 게임 적 프리팹 준비")]
        public static void PrepareCatalog()
        {
            Load(new BalanceInput());
            Debug.Log("본편 적 프리팹 7종 준비 완료: Assets/Prefabs/CampaignEnemies. 프로필: Assets/WaveLabCampaignProfiles");
        }
        [MenuItem("Tools/웨이브랩/현재 본편 웨이브 수정")]
        public static void OpenCurrent()
        {
            var settings=AssetDatabase.LoadAssetAtPath<CampaignWaveSettings>(SettingsPath);
            if(settings!=null && settings.plan!=null) WaveLabWindow.OpenPlan(settings.plan);
            else WaveQuickStartWindow.Open();
        }
        private static GameObject Prefab(GameObject source,int variant,string label)
        {
            const string folder="Assets/Prefabs/CampaignEnemies";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Prefabs","CampaignEnemies");
            string path=folder+"/Enemy"+variant.ToString("00")+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(existing!=null)return existing;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                go.name=label;
                go.AddComponent<CampaignEnemyPrefab>().variant=variant;
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/EnemyArt/Grass/"+(variant<5?"N0"+(variant+1):"E0"+(variant-4))+".png");
                if(sprite!=null){go.GetComponent<SpriteRenderer>().sprite=sprite;go.GetComponent<SpriteRenderer>().color=Color.white;}
                return PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        private static float Value(Component component,string field,float fallback)
        {
            if(component==null)return fallback;
            var property=new SerializedObject(component).FindProperty(field);
            return property!=null?property.floatValue:fallback;
        }
        public static GameObject Load(BalanceInput input)
        {
            var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/RangedEnemy.prefab");
            if(player==null||enemy==null)throw new ArgumentException("본편 Player/RangedEnemy 프리팹을 찾지 못했습니다.");
            const string folder="Assets/WaveLabCampaignProfiles";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets","WaveLabCampaignProfiles");
            input.enemies.Clear();input.waveCount=9;input.wavesPerTier=3;input.restSeconds=0;input.totalSeconds=600;
            input.modelGrowth=false;input.maximumAlive=8;input.maximumElites=2;
            input.playerHealth=Value(player.GetComponent<PlayerHealth>(),"maximumHealth",100);
            input.playerDamage=Value(player.GetComponent<PlayerAttackController>(),"damage",25);
            input.playerInterval=Value(player.GetComponent<PlayerAttackController>(),"shotInterval",.25f);
            input.playerRange=Value(player.GetComponent<PlayerAttackController>(),"attackRange",8);
            input.playerMoveSpeed=Value(player.GetComponent<PlayerController>(),"moveSpeed",4);
            // Start conservatively with the central attack. Measured combined DPS can replace this.
            input.effectiveDpsOverride=0;
            string[] names={"일반 사수","빠른 사수","중장갑 포격수","저격수","지원 사수","엘리트 사수","엘리트 포격수"};
            EnemyRole[] roles={EnemyRole.Normal,EnemyRole.Fast,EnemyRole.Tank,EnemyRole.Ranged,EnemyRole.Ranged,EnemyRole.Elite,EnemyRole.Elite};
            for(int i=0;i<7;i++)
            {
                string path=folder+"/"+i.ToString("00")+"_"+names[i]+".asset";
                var profile=AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
                if(profile==null)
                {
                    profile=ScriptableObject.CreateInstance<EnemyProfile>();profile.name=names[i];profile.prefab=enemy;profile.gameKey="rushitadel:"+i;
                    profile.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=i>=5?35:20});
                    AssetDatabase.CreateAsset(profile,path);
                }
                var prefab=Prefab(enemy,i,names[i]);
                profile.prefab=prefab;profile.gameKey="rushitadel:"+i;EditorUtility.SetDirty(profile);
                // Keep references from previously saved plans, including older profile filenames.
                foreach(var guid in AssetDatabase.FindAssets("t:EnemyProfile",new[]{folder}))
                {
                    var old=AssetDatabase.LoadAssetAtPath<EnemyProfile>(AssetDatabase.GUIDToAssetPath(guid));
                    if(old!=null && old.gameKey==profile.gameKey && old.prefab!=prefab)
                    {old.prefab=prefab;EditorUtility.SetDirty(old);}
                }
                float weight=i==2?1.35f:i==3?.9f:1;
                input.enemies.Add(new BalanceEnemy{profile=profile,role=roles[i],selectionWeight=i>=5?.35f:1,
                    health=(i>=5?600:180)*weight,healthPerTier=(i>=5?180:60)*weight,
                    damage=i>=5?16:10,attackInterval=i==0?.95f:i==3?2.4f:i>=5?2.8f:1.8f,
                    moveSpeed=0,attackRange=10,encounterDistance=7,hitRate=.15f});
            }
            AssetDatabase.SaveAssets();return player;
        }
        public static void Install(WavePlan plan)
        {
            var errors=CampaignWaveSettings.Errors(plan);
            if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors));
            if(!AssetDatabase.Contains(plan))throw new ArgumentException("계획을 먼저 파일로 저장하세요.");
            var settings=AssetDatabase.LoadAssetAtPath<CampaignWaveSettings>(SettingsPath);
            if(settings==null){settings=ScriptableObject.CreateInstance<CampaignWaveSettings>();AssetDatabase.CreateAsset(settings,SettingsPath);}
            Undo.RecordObject(settings,"Apply campaign wave plan");settings.plan=plan;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(settings);
        }
        public static void Remove()
        {
            var settings=AssetDatabase.LoadAssetAtPath<CampaignWaveSettings>(SettingsPath);
            if(settings==null)return;
            Undo.RecordObject(settings,"Remove campaign wave plan");settings.plan=null;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        }
    }
}
#endif
