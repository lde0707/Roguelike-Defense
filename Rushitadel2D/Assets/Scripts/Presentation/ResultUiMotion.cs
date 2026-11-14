using UnityEngine;
using UnityEngine.UI;

namespace Rushitadel.Presentation
{
    // Lives on the result page: changing pages immediately cancels its effects.
    public sealed class ResultUiMotion : MonoBehaviour
    {
        private CanvasGroup group;
        private RectTransform emblem;
        private Vector2 origin;
        private bool victory;
        private float elapsed;
        private RectTransform[] motes;
        private Image[] moteImages;
        public float Elapsed => elapsed;
        public void Initialize(RectTransform art, bool won)
        {
            emblem=art; origin=art.anchoredPosition; victory=won;
            group=gameObject.AddComponent<CanvasGroup>(); group.alpha=0;
            motes=new RectTransform[12]; moteImages=new Image[12];
            for(int i=0;i<motes.Length;i++)
            {
                var go=new GameObject("Result pixel "+i,typeof(RectTransform),typeof(Image));
                go.transform.SetParent(transform,false);
                var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
                rt.sizeDelta=Vector2.one*(i%3==0?6:4);
                var img=go.GetComponent<Image>();img.raycastTarget=false;
                motes[i]=rt;moteImages[i]=img;
            }
            Tick(0);
        }
        private void Update() { if(group!=null) Tick(Time.unscaledDeltaTime); }
        public void Tick(float delta)
        {
            elapsed+=delta;
            float entrance=Mathf.Clamp01(elapsed/.45f);
            group.alpha=entrance;
            // Only the artwork moves, keeping text and controls crisp and stationary.
            float settle=(1-entrance)*(victory?18:-12);
            float idle=entrance*Mathf.Sin(elapsed*(victory?2f:1.25f))*(victory?3:1);
            emblem.anchoredPosition=origin+new Vector2(0,Mathf.Round(settle+idle));
            for(int i=0;i<motes.Length;i++)
            {
                float phase=Mathf.Repeat(elapsed*.22f+i/12f,1);
                float angle=i*2.399963f;
                motes[i].anchoredPosition=new Vector2(600+Mathf.Cos(angle)*(150+phase*80),
                    -245+Mathf.Sin(angle)*90+(victory?1:-1)*phase*100);
                var tint=victory?new Color(.72f,.81f,.62f):new Color(.64f,.46f,.40f);
                tint.a=Mathf.Sin(phase*Mathf.PI)*.5f*entrance;moteImages[i].color=tint;
            }
        }
        public static AudioClip CreateCue(bool won)
        {
            const int rate=22050;
            float[] notes=won?new[]{523.25f,659.25f,783.99f,1046.5f}:new[]{392f,329.63f,261.63f,196f};
            const float step=.16f;
            var samples=new float[(int)(rate*1.15f)];
            for(int i=0;i<samples.Length;i++)
            {
                float time=i/(float)rate;
                for(int n=0;n<notes.Length;n++)
                {
                    float t=time-n*step;
                    float duration=n==3?.6f:.22f;
                    if(t<0||t>=duration)continue;
                    float envelope=Mathf.Min(1,t/.008f)*Mathf.Pow(1-t/duration,2);
                    float wave=2/Mathf.PI*Mathf.Asin(Mathf.Sin(t*notes[n]*2*Mathf.PI));
                    samples[i]+=wave*envelope*.13f;
                }
            }
            var clip=AudioClip.Create(won?"Retro clear":"Retro game over",samples.Length,1,rate,false);
            clip.SetData(samples,0);return clip;
        }
    }
}
