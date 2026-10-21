using NUnit.Framework;
using UnityEngine;
namespace WaveLab.Waves.Tests
{
    public class WavePlacementTests
    {
        WavePlan target;
        EnemyProfile profile;
        WaveDefinition source;
        [SetUp] public void Setup()
        {
            target=ScriptableObject.CreateInstance<WavePlan>();
            profile=ScriptableObject.CreateInstance<EnemyProfile>();
            source=new WaveDefinition{title="Reusable",restSeconds=4,durationSeconds=45,end=WaveEnd.DurationAndCleared};
            source.groups.Add(new SpawnGroup{enemy=profile,count=12,role=EnemyRole.Elite,spawnKey="left",batchSize=2,startSeconds=3,intervalSeconds=2});
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(target);Object.DestroyImmediate(profile);}
        [Test] public void ReuseCreatesIndependentCompositionsAndFreshIds()
        {
            WavePlacement.Place(target,source,0,false);WavePlacement.Place(target,source,1,false);
            Assert.AreNotEqual(target.waves[0].id,target.waves[1].id);
            Assert.AreNotEqual(target.waves[0].groups[0].id,target.waves[1].groups[0].id);
            target.waves[0].groups[0].count=99;
            Assert.AreEqual(12,source.groups[0].count);Assert.AreEqual(12,target.waves[1].groups[0].count);
            Assert.AreSame(profile,target.waves[0].groups[0].enemy);
        }
        [Test] public void ReplacementPreservesSlotAndOrder()
        {
            target.waves.Add(new WaveDefinition{title="Stage 2 / Round 3"});
            var id=target.waves[0].id;
            WavePlacement.Place(target,source,0,true);
            Assert.AreEqual(id,target.waves[0].id);Assert.AreEqual("Stage 2 / Round 3",target.waves[0].title);
            Assert.AreEqual(1,target.waves.Count);Assert.AreEqual(12,target.waves[0].groups[0].count);
        }
        [Test] public void PlacementSupportsArbitraryRoundCountsAndSelfCopy()
        {
            for(int i=0;i<14;i++)WavePlacement.Place(target,source,i,false);
            WavePlacement.Place(target,target.waves[0],3,false);
            Assert.AreEqual(15,target.waves.Count);
            Assert.AreNotSame(target.waves[0],target.waves[3]);
        }
        [Test] public void CopyRetainsTimingAndRoleButNotTargetLimits()
        {
            target.maximumAlive=3;target.seed=19;WavePlacement.Place(target,source,0,false);
            var w=target.waves[0];var g=w.groups[0];
            Assert.AreEqual(45,w.durationSeconds);Assert.AreEqual(4,w.restSeconds);
            Assert.AreEqual(WaveEnd.DurationAndCleared,w.end);Assert.AreEqual(EnemyRole.Elite,g.role);
            Assert.AreEqual("left",g.spawnKey);Assert.AreEqual(3,g.startSeconds);Assert.AreEqual(2,g.intervalSeconds);Assert.AreEqual(2,g.batchSize);
            Assert.AreEqual(3,target.maximumAlive);Assert.AreEqual(19,target.seed);
        }
        [Test] public void InvalidPlacementDoesNotMutateTarget()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>WavePlacement.Place(target,source,0,true));
            Assert.Throws<System.ArgumentOutOfRangeException>(()=>WavePlacement.Place(target,source,-1,false));
            Assert.AreEqual(0,target.waves.Count);
        }
    }
}
