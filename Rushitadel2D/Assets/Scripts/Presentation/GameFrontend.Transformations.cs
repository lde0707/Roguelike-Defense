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
            if(!d.NeedsFirstForms){ShowAugments();return;}
            if(d.State.turretForms[augmentSlot-1]!=0){augmentSlot=System.Array.IndexOf(d.State.turretForms,0)+1;selectedForm=0;}
            var p=SafePanel("포탑 변형 · 외형과 공격 방식 변경");page="augment";
            Label(p,"첫 증강은 변형 필수 · 6포탑 모두 변형 후 일반 증강 개방 · 지원형 최대 1개",45,100,1120,55,23);
            for(int i=1;i<=6;i++){int slot=i;bool ready=d.State.turretForms[i-1]!=0;Button(p,(augmentSlot==i?"▶ ":"")+"T"+i+(ready?" 완료":""),60+(i-1)*180,165,165,55,()=>{augmentSlot=slot;selectedForm=0;ShowTransformations();},!ready);}
            var offers=d.OfferedForms(augmentSlot);
            for(int i=0;i<offers.Length;i++)
            {
                int form=offers[i];
                Button(p,(selectedForm==form?"▶ ":"")+TurretForms.Names[form]+"\n\n"+TurretForms.Effects[form],45+i*380,245,350,270,()=>{selectedForm=form;ShowTransformations();},d.CanTransform(augmentSlot,form));
                var sprite=ModularPlayerArt.LoadSprite("ROT-"+form.ToString("00"),true);
                var img=Image(p,"Form preview "+form,155+i*380,530,120,120,Color.white);img.sprite=sprite;img.preserveAspect=true;img.raycastTarget=false;
            }
            Label(p,"16종 후보 중 3택1 · 다시 열어도 선택지는 유지됩니다",65,665,1060,55,24,TextAnchor.MiddleCenter);
            Button(p,"변형 확정",780,745,350,65,()=>{if(System.Array.IndexOf(offers,selectedForm)>=0&&d.ChooseTransformation(augmentSlot,selectedForm)){selectedForm=0;augmentChoice=-1;d.ReopenPage();}},System.Array.IndexOf(offers,selectedForm)>=0&&d.CanTransform(augmentSlot,selectedForm));
            back=ShowPause;
        }
    }
}
