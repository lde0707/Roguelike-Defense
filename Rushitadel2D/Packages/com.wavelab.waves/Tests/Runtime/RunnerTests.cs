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

        [UnityTest] public IEnumerator LiveCapBackpressuresWithoutDroppingEnemies()
        {
            plan.maximumAlive=1;var spawned=new List<GameObject>();runner.UnitSpawned+=(go,p)=>spawned.Add(go);
            runner.Play();yield return new WaitForSeconds(.08f);Assert.AreEqual(1,runner.SpawnedCount);
            spawned[0].GetComponent<SpawnedUnit>().ReportDefeated();Object.Destroy(spawned[0]);
            yield return new WaitForSeconds(.08f);Assert.AreEqual(2,runner.SpawnedCount);Assert.AreEqual(1,runner.AliveCount);
            spawned[1].GetComponent<SpawnedUnit>().ReportDefeated();Object.Destroy(spawned[1]);
            yield return new WaitForSeconds(.08f);Assert.AreEqual(3,runner.SpawnedCount);
        }
        [UnityTest] public IEnumerator EliteCapStillLetsNormalGroupSpawn()
        {
            plan.maximumAlive=4;plan.maximumElites=1;plan.waves[0].groups[0].role=EnemyRole.Elite;
            plan.waves[0].groups.Add(new SpawnGroup{enemy=enemy,count=1});
            runner.Play();yield return new WaitForSeconds(.08f);
            Assert.AreEqual(1,runner.EliteCount);Assert.AreEqual(2,runner.AliveCount);
        }
        [UnityTest] public IEnumerator TimedSafeWaveWaitsForKillsAndUsesGraceAfterTarget()
        {
            var wave=plan.waves[0];wave.end=WaveEnd.DurationAndCleared;wave.durationSeconds=.2f;wave.groups[0].count=1;
            runner.safetyTimeoutSeconds=.3f;GameObject unit=null;runner.UnitSpawned+=(go,p)=>unit=go;
            bool completed=false;runner.onWaveCompleted.AddListener(_=>completed=true);
            runner.Play();yield return new WaitForSeconds(.25f);Assert.IsFalse(completed);Assert.IsTrue(runner.IsRunning);
            unit.GetComponent<SpawnedUnit>().ReportDefeated();Object.Destroy(unit);
            yield return new WaitForSeconds(.05f);Assert.IsTrue(completed);
        }
        [UnityTest] public IEnumerator SpawnAvoidanceWaitsUntilSafe()
        {
            var avoid=new GameObject("protected player");runner.spawnAreas[0].avoid=avoid.transform;
            runner.Play();yield return new WaitForSeconds(.04f);Assert.AreEqual(0,runner.SpawnedCount);
            avoid.transform.position=Vector3.one*100;yield return new WaitForSeconds(.05f);
            Assert.Greater(runner.SpawnedCount,0);Object.Destroy(avoid);
        }
        [UnityTest] public IEnumerator PausedRunnerDoesNotSpawn()
        {
            Time.timeScale=0;
            try {runner.Play();yield return null;Assert.AreEqual(0,runner.SpawnedCount);}
            finally {Time.timeScale=1;}
            yield return new WaitForSeconds(.05f);Assert.Greater(runner.SpawnedCount,0);
        }
    }
}
