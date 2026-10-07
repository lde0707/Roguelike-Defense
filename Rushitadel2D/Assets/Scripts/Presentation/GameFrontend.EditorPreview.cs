#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        public GameObject BuildEditorPreview(int view)
        {
            font=Resources.Load<Font>("Fonts/NeoDunggeunmo");
            audioSource=gameObject.AddComponent<AudioSource>();
            if(view==0)ShowTitle();
            else if(view==1)ShowSlots(true);
            else if(view==2)ShowSlots(false);
            else if(view<=5)
            {
                ShowSettings(ShowTitle);
                if(view>3){settingsTab=view-3;DrawSettings();}
            }
            else if(view==6){ShowTitle();ConfirmQuit(ShowTitle);}
            else if(view==7) {newSlot=1;candidates=new[]{0,1};ShowThemes();}
            else {NewPage("maintenance",false);Image(content,"Web background",0,0,1920,1080,new Color32(22,28,35,255));selectedWebIcon=6;DrawWebTree(null);}
            Canvas.ForceUpdateCanvases();
            foreach(var graphic in canvas.GetComponentsInChildren<Graphic>(true))graphic.material=null;
            // Preview never handles input. Its art/layout/hover states remain inspectable.
            canvas.GetComponent<GraphicRaycaster>().enabled=false;
            canvas.transform.SetParent(null,false);
            if(titleCursorMaterial!=null)DestroyImmediate(titleCursorMaterial);
            if(titleBackgroundMaterial!=null)DestroyImmediate(titleBackgroundMaterial);
            return canvas.gameObject;
        }
    }
}
#endif
