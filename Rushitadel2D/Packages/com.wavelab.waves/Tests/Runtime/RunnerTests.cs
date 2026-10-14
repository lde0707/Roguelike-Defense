using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WaveLab.Core;
namespace WaveLab.Waves.Tests
{
    public sealed class RunnerTests
    {
        private GameObject prefab, root, areaObject;
        private EnemyProfile enemy;
        private WavePlan plan;
        private WaveRunner runner;
        [SetUp] public void Setup()
        {
            prefab=new GameObject("template"); enemy=ScriptableObject.CreateInstance<EnemyProfile>();enemy.prefab=prefab;
            enemy.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=3});
            plan=ScriptableObject.CreateInstance<WavePlan>();
            plan.waves.Add(new WaveDefinition{restSeconds=0,groups=new List<SpawnGroup>{new SpawnGroup{enemy=enemy,count=3,batchSize=2,intervalSeconds=.02f}}});
            root=new GameObject("runner");runner=root.AddComponent<WaveRunner>();runner.plan=plan;
            areaObject=new GameObject("area");runner.spawnAreas=new[]{areaObject.AddComponent<SpawnArea>()};
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            runner.Stop(); Object.Destroy(root);Object.Destroy(areaObject);Object.Destroy(prefab);Object.Destroy(enemy);Object.Destroy(plan);yield return null;
        }
        [UnityTest] public IEnumerator SpawnsRewardsAndCompletesWithoutMutatingPlan()
        {
            float xp=0;bool done=false;
            runner.onReward.AddListener((id,value)=>xp+=value);runner.onPlanCompleted.AddListener(()=>done=true);
            runner.UnitSpawned+=(go,p)=>{go.GetComponent<SpawnedUnit>().ReportDefeated();Object.Destroy(go);};
            runner.Play();
            float until=Time.realtimeSinceStartup+5;
            while(!done&&Time.realtimeSinceStartup<until)yield return null;
            Assert.IsTrue(done);Assert.AreEqual(3,runner.SpawnedCount);Assert.AreEqual(9,xp);Assert.AreEqual(3,plan.waves[0].groups[0].count);
        }
        [UnityTest] public IEnumerator StopDoesNotAwardOrComplete()
        {
            int rewards=0;bool complete=false;runner.onReward.AddListener((id,x)=>rewards++);runner.onPlanCompleted.AddListener(()=>complete=true);
            runner.Play();yield return null;runner.Stop();yield return null;
            Assert.AreEqual(0,rewards);Assert.IsFalse(complete);Assert.AreEqual(0,runner.AliveCount);
        }
        [UnityTest] public IEnumerator RunningPlanUsesSnapshot()
        {
            int count=0;runner.UnitSpawned+=(go,p)=>{count++;go.GetComponent<SpawnedUnit>().ReportDefeated();Object.Destroy(go);};
            runner.Play();plan.waves[0].groups[0].count=99;
            float until=Time.realtimeSinceStartup+5;while(runner.IsRunning&&Time.realtimeSinceStartup<until)yield return null;
            Assert.AreEqual(3,count);
        }
    }
}
