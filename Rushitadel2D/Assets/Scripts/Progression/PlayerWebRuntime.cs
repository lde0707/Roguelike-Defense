using System.Linq;
using UnityEngine;
using Rushitadel.Player;
using Rushitadel.Combat;
using Rushitadel.Presentation;
namespace Rushitadel.Progression
{
    // Timers use scaled combat time: opening menus cannot recharge or heal the player.
    public sealed class PlayerWebRuntime:MonoBehaviour
    {
        private float pulseTime,shieldTime,reclaimTime;
        private float shield;
        private PlayerHealth health;
        private readonly SpriteRenderer[] marks=new SpriteRenderer[8];
        public float Shield=>shield;
        private void Awake(){health=GetComponent<PlayerHealth>();}
        public float Absorb(float damage){float used=Mathf.Min(shield,damage);shield-=used;return damage-used;}
        public void Reclaim()
        {
            if((!RandomPlayerWeb.Has(29)&&!RandomPlayerWeb.Has(39))||Time.time<reclaimTime)return;
            reclaimTime=Time.time+.5f;if(RandomPlayerWeb.Has(29))health.Heal(.5f);
            var power=GetComponent<Rushitadel.Power.PlayerPower>();
            if(RandomPlayerWeb.Has(39))power.RestorePower(power.CurrentPower+RandomPlayerWeb.Rank(39),power.State==Rushitadel.Power.PowerCycleState.Recovering);
        }
        private void Update()
        {
            if(Time.timeScale<=0||health==null||!health.IsAlive)return;
            var d=CampaignDirector.Instance;if(d==null||!d.Initialized)return;
            bool combat=d.State.phase==RunPhase.Round||d.State.phase==RunPhase.Boss;
            if(!combat)return;
            health.Heal(.3f*RandomPlayerWeb.Rank(9)*Time.deltaTime);
            if(RandomPlayerWeb.Has(31)) {shieldTime+=Time.deltaTime;if(shieldTime>=12){shieldTime-=12;shield=20;ProjectileArt.Burst(transform.position,"BlastRing",Vector2.one*.9f,new Color(.5f,.8f,1,.4f),.25f);}}
            if(RandomPlayerWeb.Has(30)) {pulseTime+=Time.deltaTime;if(pulseTime>=8){pulseTime-=8;foreach(var b in FindObjectsByType<EnemyProjectileController>(FindObjectsSortMode.None).Where(b=>b.IsAlive&&Vector2.Distance(b.transform.position,transform.position)<=1.6f).OrderBy(b=>Vector2.SqrMagnitude(b.transform.position-transform.position)).Take(12))b.TakeDamage(30);ProjectileArt.Burst(transform.position,"BlastRing",Vector2.one*3.2f,new Color(.5f,.85f,1,.5f),.25f);}}
        }
        private void LateUpdate()
        {
            // Small exterior fittings communicate purchases without changing collision size.
            for(int i=0;i<8;i++)
            {
                bool owned=RandomPlayerWeb.Has(16+i);
                if(owned&&marks[i]==null)
                {
                    var go=new GameObject("Web fitting "+(16+i));go.transform.SetParent(transform,false);
                    marks[i]=go.AddComponent<SpriteRenderer>();marks[i].sprite=i==6?ProjectileArt.Load("BlastRing"):TransformationShot.Glyph;marks[i].sortingOrder=106;
                    marks[i].color=new Color(.72f,.85f,.84f,.75f);
                }
                if(marks[i]==null)continue;marks[i].enabled=owned;
                float angle=i*Mathf.PI/4+Time.time*(i==7?1:0);
                marks[i].transform.localPosition=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*.68f;
                marks[i].transform.localScale=new Vector3(i==3?.18f:.09f,i==1?.16f:.08f,1);
                marks[i].transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
                if(i==6){marks[i].transform.localPosition=Vector3.zero;marks[i].transform.localScale=Vector3.one*1.35f;marks[i].color=new Color(.72f,.85f,.84f,.28f);}
            }
        }
    }
}
