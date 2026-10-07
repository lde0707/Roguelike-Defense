using System;
using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Progression;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        [Serializable] private sealed class WebIconCatalog { public string source,sheet; public WebIconNode[] nodes; }
        [Serializable] private sealed class WebIconNode { public string id,branch,kind,rarity,title,description,effect,role,note; public int sourceRow; }
        private WebIconCatalog webCatalog;
        private int selectedWebIcon=-1;
        private bool showLegacyWeb;
        private RectTransform webDetails;
        private Image[] webBorders;
        private readonly Color[] webBranchColors={new Color32(108,188,204,255),new Color32(211,182,105,255),new Color32(210,137,95,255),new Color32(154,192,101,255),new Color32(176,147,205,255),new Color32(209,191,123,255)};

        private void ShowWebTree(CampaignDirector d)
        {
            Time.timeScale=0; NewPage("maintenance",false);
            Image(content,"Web background",0,0,1920,1080,new Color32(22,28,35,255)).raycastTarget=false;
            DrawWebTree(d);
        }

        private void DrawWebTree(CampaignDirector d)
        {
            if(webCatalog==null) webCatalog=JsonUtility.FromJson<WebIconCatalog>(Resources.Load<TextAsset>("WebIcons/Catalog").text);
            Label(content,"PLAYER WEB",65,38,850,65,44);
            Label(content,"중앙 하단에서 시작 · 아이콘을 클릭해 증강 확인",68,107,1150,40,24);
            Label(content,"보유 XP  "+(d!=null?d.Wallet.ToString():"0"),1460,45,390,60,30,TextAnchor.MiddleRight);
            Label(content,"엑셀 기획 24종\n효과 연결 준비 중",1460,160,375,75,24);
            Button(content,"현재 강화 6종",1460,260,375,60,()=>{showLegacyWeb=true;ShowMaintenance();},d!=null);
            Button(content,"플레이어 속성",1460,338,375,60,ShowElementSelection,d!=null);
            if(d!=null&&!string.IsNullOrEmpty(d.SaveError))
                Button(content,"저장 실패 · 재시도",1460,415,375,50,()=>{d.SaveCheckpoint("Web 저장 재시도");ShowMaintenance();});
            var root=new Vector2(960,910);
            var rootBox=Image(content,"Web root",root.x-42,root.y-42,84,84,ink);
            Image(rootBox.transform,"Root fill",4,4,76,76,panel).raycastTarget=false;
            Label(rootBox.transform,"시작",0,0,84,84,23,TextAnchor.MiddleCenter);
            var positions=new Vector2[24];
            for(int branch=0;branch<6;branch++)
            {
                float x=150+branch*235;
                positions[branch]=new Vector2(x,730);
                positions[6+branch*3]=new Vector2(x,535);
                positions[7+branch*3]=new Vector2(x-55,300);
                positions[8+branch*3]=new Vector2(x+55,405);
                WebWire(root,positions[branch],webBranchColors[branch]*.52f);
                WebWire(positions[branch],positions[6+branch*3],webBranchColors[branch]*.65f);
                WebWire(positions[6+branch*3],positions[7+branch*3],webBranchColors[branch]*.65f);
                WebWire(positions[6+branch*3],positions[8+branch*3],webBranchColors[branch]*.65f);
                Label(content,webCatalog.nodes[6+branch*3].branch, x-75,195,150,40,26,TextAnchor.MiddleCenter).color=webBranchColors[branch];
            }
            rootBox.transform.SetAsLastSibling();
            webBorders=new Image[24];
            for(int i=0;i<24;i++)
            {
                int id=i; var n=webCatalog.nodes[i]; var pos=positions[i];
                var b=Button(content,"",pos.x-38,pos.y-38,76,76,()=>SelectWebIcon(id));
                b.name="Web Icon "+n.id;
                webBorders[i]=b.GetComponent<Image>();
                var art=Image(b.transform,"Icon",6,6,64,64,Color.white);
                art.sprite=Resources.Load<Sprite>("WebIcons/"+n.id); art.raycastTarget=false; art.preserveAspect=true;
            }
            webDetails=Rect(content,"Web details",1430,490,430,430);
            Image(webDetails,"Details border",0,0,430,430,ink).raycastTarget=false;
            Image(webDetails,"Details fill",3,3,424,424,panel).raycastTarget=false;
            string next=d==null?"안전 정비에서 XP 사용":d.Round<3?$"다음 ROUND {d.Round+1}":d.Stage<3?"다음 스테이지 테마 선택":"런 완료";
            Label(content,next,65,995,1080,45,25);
            Button(content,d!=null&&d.Stage==3&&d.Round==3?"런 완료":"출발",1460,975,375,65,()=>d?.Depart(),d!=null);
            Label(content,"선은 UI 배치 제안입니다. 미정 수치와 선행 조건은 아직 구매에 적용되지 않습니다.",65,1043,1300,26,19);
            SelectWebIcon(selectedWebIcon);
        }

        private void WebWire(Vector2 a,Vector2 b,Color color)
        {
            var delta=new Vector2(b.x-a.x,a.y-b.y);
            var wire=Image(content,"Web connection",a.x,a.y,delta.magnitude,4,color);
            wire.raycastTarget=false;
            wire.rectTransform.pivot=new Vector2(0,.5f);
            wire.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        }

        private void SelectWebIcon(int id)
        {
            selectedWebIcon=id;
            for(int i=0;i<webBorders.Length;i++) webBorders[i].color=i==id?Color.white:webBranchColors[i<6?i:(i-6)/3];
            // Keep the frame; replace only the selected node's details, never the tree.
            for(int i=webDetails.childCount-1;i>=2;i--)
            {
                var child=webDetails.GetChild(i).gameObject;child.SetActive(false);
                if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
            }
            if(id<0||id>=webCatalog.nodes.Length)
            {
                Label(webDetails,"아이콘을 선택하세요",25,35,380,55,29);
                Label(webDetails,"이름 · 효과 · 역할을\n여기에서 확인할 수 있습니다.\n\n클릭만으로 XP가 소비되지 않습니다.",25,115,380,220,24);
                return;
            }
            var n=webCatalog.nodes[id];
            var icon=Image(webDetails,"Selected icon",23,20,64,64,Color.white);icon.sprite=Resources.Load<Sprite>("WebIcons/"+n.id);icon.raycastTarget=false;
            Label(webDetails,n.title,103,22,300,46,28);
            Label(webDetails,n.branch+" · "+n.rarity+" · "+n.kind,103,68,300,36,20);
            Label(webDetails,n.description,25,117,380,77,23);
            Label(webDetails,n.effect,25,201,380,78,24).color=webBranchColors[id<6?id:(id-6)/3];
            Label(webDetails,"역할: "+n.role,25,285,380,47,21);
            Label(webDetails,"기획 노드 · 효과 연결 준비 중\n비용 미정 / XP 소비 없음",25,344,380,62,21);
        }
    }
}
