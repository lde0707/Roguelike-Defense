using UnityEngine;
using Rushitadel.Player;
namespace Rushitadel.Progression
{
    public sealed partial class CampaignDirector
    {
        public bool NeedsFirstForms=>System.Array.IndexOf(State.turretForms,0)>=0;
        public bool CanBuyNode(int index,out string reason)
        {
            reason="일시정지 메뉴의 플레이어 Web에서 구매 가능";
            if(!PlayerHealth.Instance.IsAlive||Time.timeScale>0||State.phase==RunPhase.Complete||State.phase==RunPhase.Defeat||State.playerWeb==null||index<0||index>=State.playerWeb.nodes.Length)return false;
            var node=State.playerWeb.nodes[index];
            if(node.owned){reason="획득 완료";return false;}
            if(!RandomPlayerWeb.Accessible(State.playerWeb,index)){reason="연결된 노드를 하나 이상 구매하세요";return false;}
            if(Wallet<node.cost){reason="XP 부족";return false;}
            reason="구매 가능";return true;
        }
        public bool BuyNode(int index)
        {
            if(!CanBuyNode(index,out _))return false;
            var n=State.playerWeb.nodes[index];n.owned=true;State.spentXp+=n.cost;
            ApplyBuild();return true; // Persist together with the next safe round checkpoint.
        }
    }
}
