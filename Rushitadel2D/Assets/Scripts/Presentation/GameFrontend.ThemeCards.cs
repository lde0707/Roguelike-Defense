using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rushitadel.Presentation
{
    public sealed partial class GameFrontend
    {
        private static readonly string[] ThemeCardAssets={"Grass","Fire","Ice","Water","WaterElectric"};
        private void ThemeCard(Transform parent,int theme,bool selected,float x,float y,float width,float height,Action choose)
        {
            var button=Button(parent,Themes[theme],x,y,width,height,choose);
            button.GetComponent<Image>().color=selected?ink:new Color32(85,101,105,255);
            var art=Image(button.transform,"Theme Illustration",4,4,width-8,height-8,Color.white);
            art.sprite=Resources.Load<Sprite>("ThemeCards/"+ThemeCardAssets[theme]);
            art.raycastTarget=false;art.transform.SetSiblingIndex(1);
            var band=Image(button.transform,"Title backing",4,height-58,width-8,54,new Color(.07f,.10f,.13f,.94f));
            band.raycastTarget=false;band.transform.SetSiblingIndex(2);
            var label=button.GetComponentInChildren<Text>();
            label.rectTransform.anchoredPosition=new Vector2(12,-height+58);
            label.rectTransform.sizeDelta=new Vector2(width-24,54);
            label.text=(selected?"▶ ":"")+Themes[theme];
            button.targetGraphic=art;
            var colors=button.colors;colors.normalColor=selected?Color.white:new Color(.82f,.82f,.82f);
            colors.highlightedColor=Color.white;colors.selectedColor=Color.white;colors.pressedColor=new Color(.65f,.65f,.65f);button.colors=colors;
        }
    }
}
