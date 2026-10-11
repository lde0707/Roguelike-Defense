using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Progression;
namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private int selectedWebIcon=-1;
        private PlayerWebGraph previewWeb;
        private static readonly string[] WebIcons={"Survival","Mobility","Firepower","RapidTraining","PierceExtend","ExtraTrajectory","PowerAmp","EfficientCharge","Survival","HealLink","Command","Firepower","Pierce","Survival","BlastArea","EfficientCharge","Firepower","Survival","Command","Mobility","PowerAmp","Command","Survival","Mobility","Pierce","Scatter","ResidualBlast","Venom","StatusTransfer","HealLink","BlastArea","Survival","Survival","HealLink","Firepower","PierceStable","ExtraTrajectory","Mobility","HealLink","EfficientCharge"};
        private static Color WebColor(int id)=>RandomPlayerWeb.Category(id)==0?new Color32(113,186,203,255):RandomPlayerWeb.Category(id)==1?new Color32(217,153,107,255):new Color32(188,154,213,255);
        private void ShowWebTree(CampaignDirector d)
        {
            Time.timeScale=0;NewPage("maintenance",false);
            Image(content,"Web background",0,0,1920,1080,new Color32(22,28,35,255)).raycastTarget=false;
            DrawWebTree(d);
        }
        private void DrawWebTree(CampaignDirector d)
        {
            var graph=d!=null?d.State.playerWeb:(previewWeb??=RandomPlayerWeb.Generate(731));
            Label(content,"PLAYER WEB",65,35,1000,60,40);
            Label(content,"시작 → 두 갈래 분기 · 연결된 노드 구매 · 클릭으로 효과 확인",65,103,1280,45,23);
            Label(content,"후보 가중치: 기본 60% · 외형 25% · 특수 15%   (첫 분기는 기본 능력)",65,151,1250,35,21);
            Label(content,"보유 XP  "+(d!=null?d.Wallet:0),1450,45,400,55,29);
            var root=new Vector2(730,925);
            for(int i=0;i<graph.nodes.Length;i++)
            {
                var n=graph.nodes[i];var parent=n.parent<0?root:new Vector2(graph.nodes[n.parent].x,graph.nodes[n.parent].y);
                bool open=n.parent<0||graph.nodes[n.parent].owned;
                WebWire(parent,new Vector2(n.x,n.y),WebColor(n.upgrade)*(n.owned?1:open?.65f:.28f));
            }
            var rb=Image(content,"Web root",root.x-30,root.y-30,60,60,ink);
            Image(rb.transform,"Root fill",3,3,54,54,panel).raycastTarget=false;
            Label(rb.transform,"시작",0,0,60,60,20,TextAnchor.MiddleCenter);
            for(int i=0;i<graph.nodes.Length;i++)
            {
                int index=i;var n=graph.nodes[i];bool open=n.parent<0||graph.nodes[n.parent].owned;
                var b=Button(content,"",n.x-28,n.y-28,56,56,()=>{selectedWebIcon=index;ShowWebTree(d);});
                b.name="Web Node "+index;b.GetComponent<Image>().color=n.owned?new Color32(161,211,144,255):WebColor(n.upgrade)*(open?1:.5f);
                var art=Image(b.transform,"Icon",6,6,44,44,Color.white);
                art.sprite=Resources.Load<Sprite>("WebIcons/"+WebIcons[n.upgrade]);art.raycastTarget=false;art.preserveAspect=true;
                if(n.owned)Label(b.transform,"✓",36,-8,25,25,18);
            }
            var detail=Rect(content,"Web details",1430,490,430,430);
            Image(detail,"Details border",0,0,430,430,ink).raycastTarget=false;
            Image(detail,"Details fill",3,3,424,424,panel).raycastTarget=false;
            if(selectedWebIcon>=0&&selectedWebIcon<graph.nodes.Length)
            {
                var n=graph.nodes[selectedWebIcon];
                Label(detail,RandomPlayerWeb.Titles[n.upgrade],22,20,385,45,27);
                Label(detail,RandomPlayerWeb.CategoryName(n.upgrade)+" · "+n.cost+" XP",22,75,385,35,22);
                int rank=RandomPlayerWeb.Rank(d?.State,n.upgrade);
                Label(detail,RandomPlayerWeb.Effects[n.upgrade],22,120,385,140,23);
                Label(detail,"누적 "+rank+" / "+RandomPlayerWeb.MaxRank(n.upgrade)+(RandomPlayerWeb.Category(n.upgrade)==0?"단계 · 중복 가능":"단계"),22,265,385,35,22);
                string reason="전투 런에서 구매 가능";bool can=d!=null&&d.CanBuyNode(selectedWebIcon,out reason);
                Label(detail,reason,22,303,385,35,22);
                Button(detail,n.owned?"획득 완료":"노드 구매",22,340,385,65,()=>{d.BuyNode(selectedWebIcon);ShowWebTree(d);},can);
            }
            else Label(detail,"아이콘을 선택하세요\n\n클릭은 확인만 합니다.\n구매 버튼으로 XP를 사용합니다.",25,35,380,300,26);
            if(d!=null&&!string.IsNullOrEmpty(d.SaveError))Button(content,"저장 실패 · 재시도",1450,390,390,65,()=>{d.SaveCheckpoint("Web 저장 재시도");ShowWebTree(d);});
            Button(content,"출발",1450,975,390,65,()=>d?.Depart(),d!=null);
            Label(content,"구매한 경로는 유지됩니다 · 새 게임마다 모양과 증강 구성이 달라집니다",65,1000,1300,40,23);
        }
        private void WebWire(Vector2 a,Vector2 b,Color color)
        {
            var delta=new Vector2(b.x-a.x,a.y-b.y);
            var wire=Image(content,"Web connection",a.x,a.y,delta.magnitude,3,color);wire.raycastTarget=false;
            wire.rectTransform.pivot=new Vector2(0,.5f);wire.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
