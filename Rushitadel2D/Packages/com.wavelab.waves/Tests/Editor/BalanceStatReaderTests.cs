using NUnit.Framework;
using UnityEngine;
using WaveLab.Waves.Editor;
namespace WaveLab.Waves.Tests
{
    public class BalanceStatReaderTests
    {
        [Test] public void ReadsLiveBindingsInsteadOfStaleSnapshots()
        {
            var go=new GameObject();var stats=go.AddComponent<ExampleEnemy>();var profile=ScriptableObject.CreateInstance<EnemyProfile>();
            try
            {
                profile.prefab=go;stats.attackRange=9;
                profile.statBindings.Add(new NumericBinding{statId="attack.range",source=stats,propertyPath="attackRange",snapshot=1});
                Assert.IsTrue(BalanceStatReader.Read(go,profile,"attack.range",out float range));Assert.AreEqual(9,range);
                stats.attackRange=12;Assert.IsTrue(BalanceStatReader.Read(go,profile,"attack.range",out range));Assert.AreEqual(12,range);
                Assert.IsTrue(BalanceStatReader.Read(go,null,"movement.speed",out float speed));Assert.AreEqual(stats.moveSpeed,speed);
                profile.statBindings[0].propertyPath="missing";Assert.IsFalse(BalanceStatReader.Read(go,profile,"attack.range",out range));
                profile.statBindings.Clear();profile.statBindings.Add(new NumericBinding{statId="attack.speed",source=stats,propertyPath="moveSpeed"});stats.moveSpeed=4;
                Assert.IsTrue(BalanceStatReader.Interval(null,profile,out float interval));Assert.AreEqual(.25f,interval);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(profile);}
        }
    }
}
