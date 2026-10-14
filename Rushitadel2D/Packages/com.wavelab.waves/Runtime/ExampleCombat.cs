using UnityEngine;
namespace WaveLab.Waves
{
    public enum ExampleAttackMode { [InspectorName("근거리")] Melee, [InspectorName("원거리")] Ranged }

    // Opt-in XY combat sandbox. Does not change the host game's combat system.
    public sealed class ExampleCombat : MonoBehaviour
    {
        [InspectorName("플레이어 팀")] public bool playerTeam;
        [InspectorName("공격 방식")] public ExampleAttackMode attackMode = ExampleAttackMode.Ranged;
        [InspectorName("자동 추적 이동")] public bool chase = true;
        [InspectorName("투사체 속도")] [Min(.1f)] public float projectileSpeed = 8;
        private ExampleEnemy stats;
        private float cooldown;
        public ExampleEnemy Stats => stats != null ? stats : (stats = GetComponent<ExampleEnemy>());
        public bool Alive => Stats != null && Stats.CurrentHealth > 0;
        private void Update()
        {
            if (!Alive) return;
            cooldown -= Time.deltaTime;
            ExampleCombat target = null;
            float closest = float.PositiveInfinity;
            foreach (var other in FindObjectsByType<ExampleCombat>(FindObjectsSortMode.None))
            {
                if (other == this || other.playerTeam == playerTeam || !other.Alive) continue;
                float distance = Vector2.Distance(transform.position, other.transform.position);
                if (distance < closest) { closest = distance; target = other; }
            }
            if (target == null) return;
            float range = Mathf.Max(.1f, Stats.attackRange);
            if (closest > range)
            {
                if (chase) transform.position = Vector3.MoveTowards(transform.position, target.transform.position,
                    Mathf.Min(Mathf.Max(0, Stats.moveSpeed) * Time.deltaTime, closest - range));
                return;
            }
            if (cooldown > 0) return;
            cooldown = Mathf.Max(.05f, Stats.attackInterval);
            if (attackMode == ExampleAttackMode.Melee)
            {
                target.Stats.TakeDamage(Stats.attackDamage);
                var flash = ExampleShot.Visual("근접 타격", target.transform.position, playerTeam ? Color.cyan : new Color(1,.3f,.2f), .35f);
                Destroy(flash, .12f);
            }
            else ExampleShot.Fire(this, target, Stats.attackDamage, projectileSpeed, range);
        }
    }
}
