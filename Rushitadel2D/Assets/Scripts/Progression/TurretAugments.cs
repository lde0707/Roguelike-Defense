using System;
using System.Linq;
using UnityEngine;
using Rushitadel.Combat;
namespace Rushitadel.Progression
{
    public static class TurretAugments
    {
        public static readonly string[] Names={"정밀 요격","순환 장전","고유 기구 강화","집중 제압","잔류 설계","중량 탄두"};
        public static readonly string[] Effects={"선택 포탑 피해 +15% (합산)","선택 포탑 발사 빈도 +10% (합산)","변형 포탑의 고유 성능 강화","적탄 명중 시 20% 감속 0.5초","설치물 지속시간 +20% (합산)","최대 HP 100 이상 적탄에 피해 +20% (합산)"};
        public static readonly int[] Caps={5,5,3,1,3,3};
        public static readonly string[] Unique={"","매 단계 피해 +12%","적탄당 산탄 명중 상한 +1","드릴 타격 횟수 +1","원반 관통 대상 +1","폭발 대상 상한 +2","감속 지속 +0.3초","부식 지속 +0.5초","표식당 복제 발사 수 +1","편도 타격 대상 +1","톱날 타격 횟수 +1","파동 대상 상한 +2","분열 파편 피해 +25%","그물 포획 대상 +1","증폭 계수 +0.1","포말 요격 예산 +15","연쇄 대상 +1"};
        public static int Rank(int slot,int kind) {var d=CampaignDirector.Instance;return d!=null&&d.Initialized&&d.State.turretUpgrades!=null?d.State.turretUpgrades[(slot-1)*6+kind]:0;}
        public static int UniqueRank(int slot)=>Rank(slot,2);
        public static bool Eligible(CampaignState state,int slot,int kind)
        {
            if(slot<1||slot>6||kind<0||kind>=6||state.turretForms.Any(f=>f==0))return false;
            int f=state.turretForms[slot-1];
            if(state.turretUpgrades[(slot-1)*6+kind]>=Caps[kind])return false;
            if(TurretForms.Support(f))return kind==1||kind==2||kind==4;
            if(kind==1&&f==6)return false;
            if(kind==3&&(f==3||f==6||f==10||f==11||f==13||f==15))return false;
            if(kind==4)return f==8||f==10||f==13||f==14||f==15;
            return true;
        }
        public static string Description(int form,int kind)=>kind==2?Unique[form]:Effects[kind];
    }
    public sealed partial class CampaignDirector
    {
        public bool ChooseCompletionReward(int slot)
        {
            if(NeedsFirstForms||PendingAugments<=0||slot<1||slot>6||!Rushitadel.Player.PlayerHealth.Instance.IsAlive||OfferedUpgrades(slot).Length!=0)return false;
            Rushitadel.Player.PlayerHealth.Instance.Heal(10);
            var p=Rushitadel.Player.PlayerHealth.Instance.GetComponent<Rushitadel.Power.PlayerPower>();p.RestorePower(Mathf.Min(p.MaximumPower,p.CurrentPower+20),false);
            State.completionRewards++;State.augmentsChosen++;return true;
        }
        public int[] OfferedUpgrades(int slot)
        {
            var session=FindAnyObjectByType<Rushitadel.Presentation.RunSession>();
            var rng=new System.Random(unchecked((session!=null?session.Seed:1)*397+State.augmentsChosen*31+slot));
            return Enumerable.Range(0,6).Where(k=>TurretAugments.Eligible(State,slot,k)).OrderBy(_=>rng.Next()).Take(3).ToArray();
        }
        public bool ChooseUpgrade(int slot,int kind)
        {
            if(PendingAugments<=0||!Rushitadel.Player.PlayerHealth.Instance.IsAlive||!OfferedUpgrades(slot).Contains(kind))return false;
            State.turretUpgrades[(slot-1)*6+kind]++;State.augmentsChosen++;return true;
        }
    }
}
