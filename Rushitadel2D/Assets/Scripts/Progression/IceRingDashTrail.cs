using UnityEngine;
namespace Rushitadel.Progression
{
    // Five short world-space snapshots; no collider, light, bloom or per-frame allocation.
    public sealed class IceRingDashTrail:MonoBehaviour
    {
        private readonly SpriteRenderer[] ghosts=new SpriteRenderer[5];
        private readonly float[] born=new float[5];
        private GameObject root;
        private SpriteRenderer source;
        private Vector3 previous;
        private float next;
        private int cursor;
        public bool Emitting {get;private set;}
        public void SetEmitting(bool value)
        {
            Emitting=value;previous=transform.position;next=0;
            if(source==null)source=GetComponentInChildren<ThemeBossAnimation>()?.GetComponent<SpriteRenderer>();
            if(root!=null||source==null)return;
            root=new GameObject("Ice ring dash echoes");
            for(int i=0;i<ghosts.Length;i++)
            {
                var go=new GameObject("Ice echo "+i);go.transform.SetParent(root.transform);
                ghosts[i]=go.AddComponent<SpriteRenderer>();ghosts[i].sharedMaterial=source.sharedMaterial;
                ghosts[i].sortingOrder=source.sortingOrder-1;ghosts[i].enabled=false;born[i]=-100;
            }
        }
        private void LateUpdate()
        {
            if(root==null||Time.timeScale<=0)return;
            Vector3 movement=transform.position-previous;
            if(Emitting&&source!=null&&movement.sqrMagnitude>.000001f&&Time.time>=next)
            {
                var ghost=ghosts[cursor];ghost.sprite=source.sprite;
                ghost.transform.SetPositionAndRotation(source.transform.position-movement,source.transform.rotation);
                ghost.transform.localScale=source.transform.lossyScale;
                born[cursor]=Time.time;cursor=(cursor+1)%ghosts.Length;next=Time.time+.035f;
            }
            previous=transform.position;
            for(int i=0;i<ghosts.Length;i++)
            {
                float fade=1-(Time.time-born[i])/.18f;
                ghosts[i].enabled=fade>0;
                ghosts[i].color=new Color(.45f,.85f,1,Mathf.Max(0,fade)*.22f);
            }
        }
        private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
