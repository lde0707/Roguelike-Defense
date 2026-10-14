using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves.Tests
{
    public sealed class AutoBalanceTests
    {
        private GameObject prefab;
        private EnemyProfile enemy;
        private BalanceInput input;
        [SetUp] public void Setup()
        {
            prefab=new GameObject("test enemy");enemy=ScriptableObject.CreateInstance<EnemyProfile>();enemy.prefab=prefab;
            enemy.defeatRewards.Add(new ResourceAmount{resourceId="xp",amount=20});
            enemy.defeatRewards.Add(new ResourceAmount{resourceId="gold",amount=10});
            input=new BalanceInput();input.enemies.Add(new BalanceEnemy{profile=enemy,health=20,damage=0});
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(enemy);UnityEngine.Object.DestroyImmediate(prefab);}
        [Test] public void GeneratesValidPlanAndConservesTargetTime()
        {
            var r=AutoBalance.Generate(input);
            Assert.IsEmpty(WaveValidation.Errors(r.plan));
            Assert.AreEqual(input.totalSeconds,r.plan.waves.Sum(w=>w.durationSeconds+w.restSeconds),.01);
            Assert.Greater(r.rows.Count,0);UnityEngine.Object.DestroyImmediate(r.plan);
        }
        [Test] public void SeedReproducesCompositionAndIds()
        {
            var a=AutoBalance.Generate(input);var b=AutoBalance.Generate(input);
            Assert.AreEqual(JsonUtility.ToJson(a.plan),JsonUtility.ToJson(b.plan));
            UnityEngine.Object.DestroyImmediate(a.plan);UnityEngine.Object.DestroyImmediate(b.plan);
        }
        [Test] public void GrowthChangesLaterDpsWithoutChangingSource()
        {
            input.waveCount=3;input.firstLevelXp=20;input.goldUpgradeCost=30;
            var r=AutoBalance.Generate(input);
            Assert.Greater(r.rows[1].effectiveDps,r.rows[0].effectiveDps);
            Assert.Greater(r.rows[0].endLevel,1);Assert.Greater(r.rows[0].purchases,0);
            Assert.AreEqual(20,input.playerDamage);Assert.AreEqual(20,enemy.defeatRewards[0].amount);
            UnityEngine.Object.DestroyImmediate(r.plan);
        }
        [Test] public void DisabledGrowthKeepsDpsAndLevel()
        {
            input.modelGrowth=false;var r=AutoBalance.Generate(input);
            Assert.IsTrue(r.rows.All(w=>w.endLevel==1&&w.purchases==0&&w.effectiveDps==r.rows[0].effectiveDps));
            UnityEngine.Object.DestroyImmediate(r.plan);
        }
        [Test] public void RejectsImpossibleAndNonFiniteInputs()
        {
            input.playerInterval=0;Assert.Throws<ArgumentException>(()=>AutoBalance.Generate(input));
            input.playerInterval=.5f;input.totalSeconds=float.NaN;Assert.Throws<ArgumentException>(()=>AutoBalance.Generate(input));
            input.totalSeconds=10;input.waveCount=100;Assert.Throws<ArgumentException>(()=>AutoBalance.Generate(input));
            input.waveCount=1;input.enemies[0].health=10000000;Assert.Throws<ArgumentException>(()=>AutoBalance.Generate(input));
        }
        [Test] public void IncomingDamageReducesEnemyBudget()
        {
            input.modelGrowth=false;input.waveCount=1;var safe=AutoBalance.Generate(input);
            input.enemies[0].damage=20;input.enemies[0].hitRate=1;
            var risky=AutoBalance.Generate(input);
            Assert.Less(risky.rows[0].count,safe.rows[0].count);
            Assert.LessOrEqual(risky.rows[0].estimatedDamage,input.playerHealth*.22f+.01f);
            UnityEngine.Object.DestroyImmediate(safe.plan);UnityEngine.Object.DestroyImmediate(risky.plan);
        }
        [Test] public void RangeAndMovementChangeWaveCounts()
        {
            input.modelGrowth=false;input.waveCount=1;input.playerRange=1;input.playerMoveSpeed=1;
            var shortRange=AutoBalance.Generate(input);
            input.playerRange=10;var longRange=AutoBalance.Generate(input);
            Assert.Greater(longRange.rows[0].count,shortRange.rows[0].count);
            input.playerRange=1;input.playerMoveSpeed=5;var fast=AutoBalance.Generate(input);
            Assert.Greater(fast.rows[0].count,shortRange.rows[0].count);
            UnityEngine.Object.DestroyImmediate(shortRange.plan);UnityEngine.Object.DestroyImmediate(longRange.plan);UnityEngine.Object.DestroyImmediate(fast.plan);
        }
        [Test] public void AttackSpeedChangesWaveBudget()
        {
            input.waveCount=1;input.modelGrowth=false;
            var slow=AutoBalance.Generate(input);input.playerInterval=.1f;var fast=AutoBalance.Generate(input);
            Assert.Greater(fast.rows[0].count,slow.rows[0].count);
            UnityEngine.Object.DestroyImmediate(slow.plan);UnityEngine.Object.DestroyImmediate(fast.plan);
        }
        [Test] public void LongerEnemyRangeAddsFirstStrikeExposure()
        {
            var e=input.enemies[0];e.damage=10;e.hitRate=1;input.playerRange=1;
            e.attackRange=1;double close=AutoBalance.IncomingDamage(input,e,30);
            e.attackRange=8;Assert.Greater(AutoBalance.IncomingDamage(input,e,30),close);
            double damage=AutoBalance.IncomingDamage(input,e,30);e.attackInterval/=2;
            Assert.AreEqual(damage*2,AutoBalance.IncomingDamage(input,e,30),.001);
        }
        [Test] public void StationaryShortRangeCannotReachStationaryLongRange()
        {
            input.playerMoveSpeed=0;input.playerRange=1;
            Assert.IsTrue(double.IsInfinity(AutoBalance.WorkSeconds(input,input.enemies[0],30)));
            Assert.Throws<ArgumentException>(()=>AutoBalance.Generate(input));
            input.enemies[0].encounterDistance=0;
            Assert.AreEqual(input.enemies[0].health/30d,AutoBalance.WorkSeconds(input,input.enemies[0],30),.001);
        }
        [Test] public void RejectsInvalidSpatialStats()
        {
            input.playerRange=float.NaN;Assert.IsNotEmpty(AutoBalance.Validate(input));
            input.playerRange=3;input.enemies[0].moveSpeed=-1;Assert.IsNotEmpty(AutoBalance.Validate(input));
        }
    }
}
