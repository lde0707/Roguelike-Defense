using UnityEngine;

namespace Rushitadel.Presentation
{
    // A short, player-following pulse with rising green crosses. No gameplay colliders.
    public sealed class PlayerHealEffect : MonoBehaviour
    {
        private const float Duration=1.1f;
        private readonly LineRenderer[] crosses=new LineRenderer[12];
        private LineRenderer ring;
        private Material material;
        private float age;
        public static void Play(Transform player)
        {
            var existing=player.GetComponentInChildren<PlayerHealEffect>();
            if(existing!=null){existing.age=0;return;}
            var go=new GameObject("Full health recovery");
            go.transform.SetParent(player,false);
            go.AddComponent<PlayerHealEffect>();
        }
        private LineRenderer Line(string label,int count,float width)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();
            line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=count;
            line.widthMultiplier=width;line.sortingOrder=125;
            return line;
        }
        private void Awake()
        {
            material=new Material(Shader.Find("Sprites/Default"));
            ring=Line("Healing pulse",33,.035f);
            for(int i=0;i<crosses.Length;i++)crosses[i]=Line("Healing cross",2,.055f);
            Draw();
        }
        private void LateUpdate()
        {
            // Pause with gameplay so theme/upgrade menus do not consume the effect.
            age+=Time.deltaTime;
            if(age>=Duration){Destroy(gameObject);return;}
            transform.rotation=Quaternion.identity;
            Draw();
        }
        private void Draw()
        {
            float t=age/Duration;
            var green=new Color(.35f,.95f,.52f,(1-t)*.8f);
            ring.startColor=ring.endColor=green;
            float radius=.35f+.7f*t;
            for(int i=0;i<=32;i++)
            {float a=i*Mathf.PI/16;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;
                var p=new Vector3(Mathf.Cos(a)*(.4f+.2f*t),Mathf.Sin(a)*.35f+t*.8f,0);
                float size=.075f;
                var horizontal=crosses[i*2];var vertical=crosses[i*2+1];
                horizontal.startColor=horizontal.endColor=vertical.startColor=vertical.endColor=green;
                horizontal.SetPosition(0,p-Vector3.right*size);horizontal.SetPosition(1,p+Vector3.right*size);
                vertical.SetPosition(0,p-Vector3.up*size);vertical.SetPosition(1,p+Vector3.up*size);
            }
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
