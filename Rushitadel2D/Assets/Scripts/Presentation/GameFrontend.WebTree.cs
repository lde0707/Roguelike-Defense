using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Progression;
namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private int selectedWebIcon=-1;
        private PlayerWebGraph previewWeb;
        private PlayerWebGraph displayedWeb;
        private ScrollRect webScroll;
        private Vector2 webPosition=new(.5f,0);
        private static Sprite webNodeShape;
        private static Sprite WebNodeShape
        {
            get
            {
                if(webNodeShape!=null)return webNodeShape;
                var texture=new Texture2D(24,24,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
                var pixels=new Color32[576];
                for(int y=0;y<24;y++)for(int x=0;x<24;x++)
                    if(Mathf.Abs(x-11.5f)+Mathf.Abs(y-11.5f)<17)pixels[y*24+x]=Color.white;
                texture.SetPixels32(pixels);texture.Apply();
                return webNodeShape=Sprite.Create(texture,new UnityEngine.Rect(0,0,24,24),Vector2.one*.5f,24);
            }
        }
        private static readonly string[] WebIcons={"Survival","Mobility","Firepower","RapidTraining","PierceExtend","ExtraTrajectory","PowerAmp","EfficientCharge","Survival","HealLink","Command","Firepower","Pierce","Survival","BlastArea","EfficientCharge","Firepower","Survival","Command","Mobility","PowerAmp","Command","Survival","Mobility","Pierce","Scatter","ResidualBlast","Venom","StatusTransfer","HealLink","BlastArea","Survival","Survival","HealLink","Firepower","PierceStable","ExtraTrajectory","Mobility","HealLink","EfficientCharge"};
        private static Color WebColor(int id)=>RandomPlayerWeb.Category(id)==0?new Color32(113,186,203,255):RandomPlayerWeb.Category(id)==1?new Color32(217,153,107,255):new Color32(188,154,213,255);
        private void ShowWebTree(CampaignDirector d)
        {
            if(webScroll!=null)webPosition=webScroll.normalizedPosition;
            Time.timeScale=0;NewPage("web",false);back=ShowPause;
            Image(content,"Web background",0,0,1920,1080,new Color32(22,28,35,255)).raycastTarget=false;
            DrawWebTree(d);
        }
        private void DrawWebTree(CampaignDirector d)
        {
            var graph=d!=null?d.State.playerWeb:(previewWeb??=RandomPlayerWeb.Generate(731));
            if(displayedWeb!=graph){displayedWeb=graph;webPosition=new Vector2(.5f,0);selectedWebIcon=-1;}
            Label(content,"PLAYER WEB",65,35,1000,60,40);
            Label(content,"드래그: 자유 이동 · 휠: 위/아래 · 아이콘 클릭: 효과 확인",65,103,1280,45,23);
            Label(content,"기본 능력 사이의 특별한 업그레이드를 찾아보세요 · 연결선을 따라 좌우 이동 가능",65,151,1250,35,21);
            Label(content,"보유 XP  "+(d!=null?d.Wallet:0),1450,45,400,55,29);
            var viewport=Rect(content,"Web viewport",45,205,1340,755);
            viewport.gameObject.AddComponent<Image>().color=new Color32(25,33,40,255);
            viewport.gameObject.AddComponent<RectMask2D>();
            var web=Rect(viewport,"Web canvas",0,0,RandomPlayerWeb.CanvasWidth,RandomPlayerWeb.CanvasHeight);
            webScroll=viewport.gameObject.AddComponent<ScrollRect>();webScroll.viewport=viewport;webScroll.content=web;
            webScroll.horizontal=true;webScroll.vertical=true;webScroll.inertia=false;webScroll.scrollSensitivity=90;
            webScroll.movementType=ScrollRect.MovementType.Clamped;
            var root=RandomPlayerWeb.Root;
            for(int i=0;i<graph.nodes.Length;i++)
            {
                var n=graph.nodes[i];var parent=n.parent<0?root:new Vector2(graph.nodes[n.parent].x,graph.nodes[n.parent].y);
                bool open=RandomPlayerWeb.Accessible(graph,i);
                WebWire(web,parent,new Vector2(n.x,n.y),WebColor(n.upgrade)*(n.owned?1:open?.65f:.28f));
                foreach(int linked in n.links??System.Array.Empty<int>())
                    WebWire(web,new Vector2(graph.nodes[linked].x,graph.nodes[linked].y),new Vector2(n.x,n.y),new Color(.65f,.73f,.66f,(n.owned||graph.nodes[linked].owned)?.8f:.3f));
            }
            var rb=Image(web,"Web root",root.x-30,root.y-30,60,60,ink);rb.sprite=WebNodeShape;
            var rootFill=Image(rb.transform,"Root fill",3,3,54,54,panel);rootFill.sprite=WebNodeShape;rootFill.raycastTarget=false;
            Label(rb.transform,"시작",0,0,60,60,20,TextAnchor.MiddleCenter);
            for(int i=0;i<graph.nodes.Length;i++)
            {
                int index=i;var n=graph.nodes[i];bool open=RandomPlayerWeb.Accessible(graph,i);
                float size=RandomPlayerWeb.Category(n.upgrade)==0?56:72;
                var b=Button(web,"",n.x-size/2,n.y-size/2,size,size,()=>{selectedWebIcon=index;ShowWebTree(d);});
                b.GetComponent<Image>().sprite=WebNodeShape;
                b.name="Web Node "+index;b.GetComponent<Image>().color=n.owned?new Color32(161,211,144,255):WebColor(n.upgrade)*(open?1:.5f);
                var art=Image(b.transform,"Icon",(size-44)/2,(size-44)/2,44,44,Color.white);
                art.sprite=RetroUiArt.WebIcon(WebIcons[n.upgrade]);art.raycastTarget=false;art.preserveAspect=true;
                if(n.owned)Label(b.transform,"✓",36,-8,25,25,18);
                if(i==selectedWebIcon)b.GetComponent<Image>().color=Color.white;
            }
            Canvas.ForceUpdateCanvases();webScroll.normalizedPosition=webPosition;
            Button(content,"시작 위치",1450,145,390,60,()=>{webScroll.normalizedPosition=new Vector2(.5f,0);webPosition=webScroll.normalizedPosition;});
            Label(content,"노드 "+graph.nodes.Length+"개\n작은 노드: 기본 스탯\n큰 노드: 외형 · 특수 능력\n\n선으로 이어진 구매 노드가\n하나만 있어도 진행 가능",1450,225,390,240,23);
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
            Button(content,"돌아가기",1450,975,390,65,ShowPause,d!=null);
            Label(content,"구매는 즉시 적용 · 진행 내용은 다음 라운드 전환 시 저장됩니다",65,1000,1300,40,23);
        }
        private void WebWire(Transform parent,Vector2 a,Vector2 b,Color color)
        {
            var delta=new Vector2(b.x-a.x,a.y-b.y);
            var wire=Image(parent,"Web connection",a.x,a.y,delta.magnitude,3,color);wire.raycastTarget=false;
            wire.rectTransform.pivot=new Vector2(0,.5f);wire.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }
    }
}
