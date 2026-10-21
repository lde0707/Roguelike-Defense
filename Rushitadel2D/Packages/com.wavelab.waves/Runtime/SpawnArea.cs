using UnityEngine;
namespace WaveLab.Waves
{
    public enum SpawnShape { Point, Rectangle, Annulus }
    public enum SpawnPlane { XY, XZ }
    public sealed class SpawnArea : MonoBehaviour
    {
        public string key = "main";
        public SpawnShape shape;
        public SpawnPlane plane;
        public Vector2 size = new Vector2(10, 4);
        [Min(0)] public float innerRadius = 3;
        [Min(0)] public float outerRadius = 5;
        [Tooltip("플레이어나 카메라를 지정하면 중심 위치를 따라갑니다. 회전·크기는 영역 Transform 기준입니다.")]
        public Transform follow;
        [Tooltip("이 대상의 안전 반경 안에는 생성하지 않습니다.")]
        public Transform avoid;
        [Min(0)] public float avoidRadius = 2;
        public Vector3 Center => follow != null ? follow.position + transform.position : transform.position;
        public bool Valid => WaveValidation.Finite(size.x) && WaveValidation.Finite(size.y) && size.x > 0 && size.y > 0
            && WaveValidation.Finite(innerRadius) && WaveValidation.Finite(outerRadius) && innerRadius >= 0 && outerRadius >= innerRadius
            && WaveValidation.Finite(avoidRadius) && avoidRadius >= 0;
        public Vector3 Sample(System.Random random)
        {
            Vector2 p = Vector2.zero;
            if (shape == SpawnShape.Rectangle) p = new Vector2(((float)random.NextDouble() - .5f) * size.x, ((float)random.NextDouble() - .5f) * size.y);
            if (shape == SpawnShape.Annulus)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float radius = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius, outerRadius * outerRadius, (float)random.NextDouble()));
                p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return Center + transform.TransformVector(plane == SpawnPlane.XY ? new Vector3(p.x, p.y, 0) : new Vector3(p.x, 0, p.y));
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Valid ? Color.cyan : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(Center, transform.rotation, transform.lossyScale);
            if (shape == SpawnShape.Rectangle) Gizmos.DrawWireCube(Vector3.zero, plane == SpawnPlane.XY ? new Vector3(size.x, size.y, .05f) : new Vector3(size.x, .05f, size.y));
            else if (shape == SpawnShape.Point) Gizmos.DrawWireSphere(Vector3.zero, .2f);
            else { Circle(innerRadius); Circle(outerRadius); }
            Gizmos.matrix = Matrix4x4.identity;
        }
        private void Circle(float r)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i * Mathf.PI * 2 / 48, b = (i + 1) * Mathf.PI * 2 / 48;
                Vector3 p = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), q = new Vector3(Mathf.Cos(b) * r, Mathf.Sin(b) * r, 0);
                if (plane == SpawnPlane.XZ) { p = new Vector3(p.x, 0, p.y); q = new Vector3(q.x, 0, q.y); }
                Gizmos.DrawLine(p, q);
            }
        }
    }
}
