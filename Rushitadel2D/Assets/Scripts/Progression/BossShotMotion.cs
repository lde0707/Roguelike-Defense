using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.Player;

namespace Rushitadel.Progression
{
    // Existing hostile projectile controller remains the sole movement/collision authority.
    [DefaultExecutionOrder(-20)]
    public sealed class BossShotMotion : MonoBehaviour
    {
        public CampaignBoss Owner;
        public float Turn, ReturnAfter, SplitAfter;
        public string ChildArt;
        private float age;
        private Vector2 origin;
        private EnemyProjectileController shot;
        private void Awake(){shot=GetComponent<EnemyProjectileController>();origin=transform.position;}
        private void Update()
        {
            if(Time.timeScale<=0)return;
            if(Owner==null || !Owner.Health.IsAlive){shot.Despawn();return;}
            if(!shot.IsAlive)return;
            age+=CombatTempo.DeltaTime;
            if(ReturnAfter>0 && age>=ReturnAfter)
            {
                Vector2 delta=origin-(Vector2)transform.position;
                if(delta.magnitude<.25f){shot.Despawn();return;}
                shot.SetHeading(delta);
            }
            else if(Turn!=0)shot.SetHeading(Quaternion.Euler(0,0,Turn*CombatTempo.DeltaTime)*shot.Direction);
            if(SplitAfter>0 && age>=SplitAfter)
            {
                // Never split into a player at point-blank range. Breaking the parent cancels children.
                var player=PlayerHealth.Instance;
                if(player!=null && Vector2.Distance(player.transform.position,transform.position)<1.4f)return;
                for(int i=-1;i<=1;i++)Owner.FireAt(transform.position,
                    Quaternion.Euler(0,0,i*25)*shot.Direction,ChildArt,12,3.8f,.13f,0);
                shot.Despawn();
            }
        }
    }
}
