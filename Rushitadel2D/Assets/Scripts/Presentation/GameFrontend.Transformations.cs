using UnityEngine;
using UnityEngine.UI;
using Rushitadel.Combat;
using Rushitadel.Progression;
namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private int selectedForm;
        public void ShowTransformations()
        {
            var d=CampaignDirector.Instance;if(d==null||d.PendingAugments<=0){d?.ReopenPage();return;}
            var p=SafePanel("포탑 변형 · 외형과 공격 방식 변경");page="augment";
            Label(p,"증강 선택 1회 사용 · 기존 화력/연사는 유지 · 지원형은 전체 1슬롯",45,100,1120,55,23);
            for(int i=1;i<=5;i++){int slot=i;Button(p,(augmentSlot==i?"▶ ":"")+"T"+i,60+(i-1)*215,165,200,55,()=>{augmentSlot=slot;selectedForm=0;ShowTransformations();});}
            var offers=d.OfferedForms(augmentSlot);
            for(int i=0;i<offers.Length;i++)
            {
                int form=offers[i];
                Button(p,(selectedForm==form?"▶ ":"")+TurretForms.Names[form]+"\n\n"+TurretForms.Effects[form],45+i*380,245,350,270,()=>{selectedForm=form;ShowTransformations();},d.CanTransform(augmentSlot,form));
                var sprite=ModularPlayerArt.LoadSprite("ROT-"+form.ToString("00"),true);
                var img=Image(p,"Form preview "+form,155+i*380,530,120,120,Color.white);img.sprite=sprite;img.preserveAspect=true;img.raycastTarget=false;
            }
            Label(p,"16종 후보 중 3택1 · 다시 열어도 선택지는 유지됩니다",65,665,1060,55,24,TextAnchor.MiddleCenter);
            Button(p,"일반 강화",45,745,320,65,ShowAugments);
            Button(p,"변형 확정",780,745,350,65,()=>{if(System.Array.IndexOf(offers,selectedForm)>=0&&d.ChooseTransformation(augmentSlot,selectedForm)){selectedForm=0;augmentChoice=-1;d.ReopenPage();}},System.Array.IndexOf(offers,selectedForm)>=0&&d.CanTransform(augmentSlot,selectedForm));
            back=ShowAugments;
        }
        public void ShowElementSelection()
        {
            var d=CampaignDirector.Instance;var p=SafePanel("플레이어 속성 · 발광 색상");
            Label(p,"안전 정비에서 무료 변경 · 본체/포탑/발사 효과에 적용\n현재 속성 선택은 시각 효과 전용이며 추가 피해나 상태이상은 부여하지 않습니다.",55,105,1100,95,24);
            for(int i=0;i<6;i++){int e=i;var b=Button(p,(d.State.element==e?"▶ ":"")+TurretForms.ElementNames[e],70+(i%3)*365,240+(i/3)*170,330,140,()=>{d.SetElement(e);ShowElementSelection();});var icon=Image(b.transform,"Element swatch",12,12,20,20,TurretForms.ElementColor(e));icon.raycastTarget=false;}
            Button(p,"정비로 돌아가기",710,745,390,65,ShowMaintenance);back=ShowMaintenance;
        }
    }
}
