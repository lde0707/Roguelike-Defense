using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace WaveLab.Waves.Tests
{
    public class ExampleCombatTests
    {
        [UnityTest]
        public IEnumerator MeleeAndRangedDealDamageButRespectRange()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            yield return new EnterPlayMode();
            var a=new GameObject("attacker");var sa=a.AddComponent<ExampleEnemy>();sa.attackDamage=5;sa.attackInterval=.1f;sa.attackRange=2;
            var ca=a.AddComponent<ExampleCombat>();ca.playerTeam=true;ca.chase=false;ca.attackMode=ExampleAttackMode.Melee;
            var b=new GameObject("target");var sb=b.AddComponent<ExampleEnemy>();
            var cb=b.AddComponent<ExampleCombat>();cb.enabled=false;
            b.transform.position=new Vector3(5,0,0);
            float health=sb.CurrentHealth;
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(health,sb.CurrentHealth,"Outside range must not hit");
            b.transform.position=Vector3.right;
            yield return new WaitForSeconds(.15f);
            Assert.Less(sb.CurrentHealth,health,"Melee must hit");
            ca.attackMode=ExampleAttackMode.Ranged;
            health=sb.CurrentHealth;
            float deadline=Time.time+3;
            while(sb.CurrentHealth>=health && Time.time<deadline) yield return null;
            Assert.Less(sb.CurrentHealth,health,"Projectile must hit");
            Object.Destroy(a);Object.Destroy(b);
            foreach(var shot in Object.FindObjectsByType<ExampleShot>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
            yield return null;
            yield return new ExitPlayMode();
        }
    }
}
