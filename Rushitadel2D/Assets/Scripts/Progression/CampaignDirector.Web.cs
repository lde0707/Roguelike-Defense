using UnityEngine;
using Rushitadel.Player;
namespace Rushitadel.Progression
{
    public sealed partial class CampaignDirector
    {
        public bool NeedsFirstForms=>System.Array.IndexOf(State.turretForms,0)>=0;
        public bool CanBuyNode(int index,out string reason)
        {
            reason="정비에서 회복 선택 후 구매 가능";
            if(State.phase!=RunPhase.Maintenance||!State.recovered||State.playerWeb==null||index<0||index>=State.playerWeb.nodes.Length)return false;
            var node=State.playerWeb.nodes[index];
            if(node.owned){reason="획득 완료";return false;}
            if(node.parent>=0&&!State.playerWeb.nodes[node.parent].owned){reason="연결된 이전 노드 필요";return false;}
            if(Wallet<node.cost){reason="XP 부족";return false;}
            reason="구매 가능";return true;
        }
        public bool BuyNode(int index)
        {
            if(!CanBuyNode(index,out _))return false;
            var n=State.playerWeb.nodes[index];n.owned=true;State.spentXp+=n.cost;
            ApplyBuild();SaveCheckpoint("안전 정비 · 랜덤 Web 구매");return true;
        }
    }
}
