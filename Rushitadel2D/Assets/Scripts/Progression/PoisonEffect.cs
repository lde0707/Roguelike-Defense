using UnityEngine;
using Rushitadel.Combat;
namespace Rushitadel.Progression
{
    public sealed class PoisonEffect : MonoBehaviour
    {
        private float remaining, tick;
        public void Refresh() { remaining = 3; Rushitadel.Presentation.ProjectileArt.Status(gameObject,"PoisonStatus",3,.6f); }
        private void Update()
        {
            if (Time.timeScale <= 0 || remaining <= 0) return;
            remaining -= Time.deltaTime; tick += Time.deltaTime;
            if (tick >= 1) { tick -= 1; GetComponent<EnemyController>().TakeDamage(8); }
        }
    }
}
