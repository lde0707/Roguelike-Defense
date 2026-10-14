using UnityEngine;
namespace WaveLab.Waves
{
    public sealed class ExampleShot : MonoBehaviour
    {
        private bool playerTeam;
        private Vector3 direction;
        private float damage, speed, remaining;
        private Material ownedMaterial;
        public static GameObject Visual(string name, Vector3 position, Color color, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name; go.transform.position = position; go.transform.localScale = Vector3.one * scale;
            var collider = go.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            var material = new Material(Shader.Find("Sprites/Default")) { color = color };
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<ExampleShot>().ownedMaterial = material;
            return go;
        }
        public static void Fire(ExampleCombat source, ExampleCombat target, float damage, float speed, float range)
        {
            var go = Visual("테스트 탄", source.transform.position, source.playerTeam ? Color.cyan : new Color(1,.3f,.2f), .18f);
            go.name = source.playerTeam ? "플레이어 탄" : "적 탄";
            var shot = go.GetComponent<ExampleShot>();
            shot.playerTeam = source.playerTeam; shot.damage = damage; shot.speed = Mathf.Max(.1f, speed);
            shot.remaining = range + .5f; shot.direction = (target.transform.position - source.transform.position).normalized;
        }
        private void Update()
        {
            if (speed <= 0) return; // Short-lived melee visual.
            Vector3 start = transform.position;
            float step = Mathf.Min(speed * Time.deltaTime, remaining);
            Vector3 end = start + direction * step;
            ExampleCombat hit = null; float nearest = float.PositiveInfinity;
            foreach (var unit in FindObjectsByType<ExampleCombat>(FindObjectsSortMode.None))
            {
                if (!unit.Alive || unit.playerTeam == playerTeam) continue;
                Vector3 offset = unit.transform.position - start;
                float along = Mathf.Clamp(Vector3.Dot(offset, direction), 0, step);
                float radius = Mathf.Max(unit.transform.lossyScale.x, unit.transform.lossyScale.y) * .5f + .09f;
                if (Vector3.Distance(start + direction * along, unit.transform.position) <= radius && along < nearest)
                { nearest = along; hit = unit; }
            }
            if (hit != null) { hit.Stats.TakeDamage(damage); Destroy(gameObject); return; }
            transform.position = end; remaining -= step;
            if (remaining <= 0) Destroy(gameObject);
        }
        private void OnDestroy() { if (ownedMaterial != null) Destroy(ownedMaterial); }
    }
}
