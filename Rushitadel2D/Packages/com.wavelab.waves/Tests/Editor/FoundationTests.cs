using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves.Tests
{
    public sealed class FoundationTests
    {
        [Test] public void StatOrderAndClamp()
        {
            var mods = new[] {
                new StatModifier { statId="hp", operation=ModifierOperation.Flat, amount=20 },
                new StatModifier { statId="hp", operation=ModifierOperation.AdditivePercent, amount=.5f },
                new StatModifier { statId="hp", operation=ModifierOperation.Multiply, amount=2 },
                new StatModifier { statId="other", operation=ModifierOperation.Flat, amount=999 } };
            Assert.AreEqual(360, StatMath.Evaluate("hp", 100, mods), .001);
            Assert.AreEqual(200, StatMath.Evaluate("hp", 100, mods, 0, 200), .001);
        }
        [Test] public void PartialBatchSchedule()
        {
            var g = new SpawnGroup { count=5, batchSize=2, startSeconds=2, intervalSeconds=.5f };
            Assert.AreEqual(3, g.LastSpawnSeconds);
        }
        [Test] public void ValidationRejectsMissingPrefabDuplicateIdsAndNonFiniteTime()
        {
            var p=ScriptableObject.CreateInstance<WavePlan>();
            p.waves.Add(new WaveDefinition { id="same", groups=new List<SpawnGroup> { new SpawnGroup { id="same", intervalSeconds=float.NaN } } });
            var errors=WaveValidation.Errors(p);
            Assert.IsTrue(errors.Exists(e=>e.Contains("ID")));
            Assert.IsTrue(errors.Exists(e=>e.Contains("프리팹")));
            Assert.IsTrue(errors.Exists(e=>e.Contains("시간")));
            Object.DestroyImmediate(p);
        }
        [Test] public void AnnulusSamplesStayInBoundsAndAreSeeded()
        {
            var go=new GameObject("area"); var a=go.AddComponent<SpawnArea>();
            a.shape=SpawnShape.Annulus; a.innerRadius=2; a.outerRadius=5;
            var x=new System.Random(41); var y=new System.Random(41);
            for(int i=0;i<100;i++) { var p=a.Sample(x); Assert.AreEqual(p,a.Sample(y)); Assert.That(p.magnitude,Is.InRange(1.999f,5.001f)); }
            Object.DestroyImmediate(go);
        }
        [Test] public void DefeatSignalIsIdempotent()
        {
            var go=new GameObject("unit");var u=go.AddComponent<SpawnedUnit>();int kills=0;
            u.Initialize(k=> { if(k) kills++; }); u.ReportDefeated();u.ReportDefeated();u.ReportDespawned();
            Object.DestroyImmediate(go);Assert.AreEqual(1,kills);
        }
        [Test] public void DespawnIsNotAReward()
        {
            var go=new GameObject("unit");var u=go.AddComponent<SpawnedUnit>();bool? kill=null;
            u.Initialize(k=>kill=k);u.ReportDespawned();u.ReportDefeated();Object.DestroyImmediate(go);Assert.AreEqual(false,kill);
        }
    }
}
