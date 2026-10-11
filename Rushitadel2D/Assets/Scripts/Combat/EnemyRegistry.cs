using System.Collections.Generic;
using UnityEngine;

namespace Rushitadel.Combat
{
    public static class EnemyRegistry
    {
        private static readonly HashSet<EnemyController> Enemies = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Enemies.Clear();

        public static IEnumerable<EnemyController> ActiveEnemies => Enemies;

        public static void Register(EnemyController enemy) => Enemies.Add(enemy);
        public static void Unregister(EnemyController enemy) => Enemies.Remove(enemy);

        public static EnemyController FindClosest(Vector3 position, float maximumDistance = float.PositiveInfinity)
        {
            EnemyController closest = null;
            float closestDistance = maximumDistance * maximumDistance;

            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                float distance = (enemy.transform.position - position).sqrMagnitude;
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        public static EnemyController FindClosestToSegment(Vector3 start, Vector3 end, float radius)
        {
            EnemyController closest = null;
            float closestDistance = radius * radius;
            Vector3 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;

            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 enemyOffset = enemy.transform.position - start;
                float progress = segmentLengthSquared > 0f
                    ? Mathf.Clamp01(Vector3.Dot(enemyOffset, segment) / segmentLengthSquared)
                    : 0f;
                Vector3 closestPoint = start + segment * progress;
                float distance = (enemy.transform.position - closestPoint).sqrMagnitude;
                if (distance <= closestDistance)
                {
                    closestDistance = distance;
                    closest = enemy;
                }
            }

            return closest;
        }

        public static List<EnemyController> FindAllWithinRadius(Vector3 center, float radius)
        {
            List<EnemyController> results = new();
            float radiusSquared = radius * radius;

            foreach (EnemyController enemy in Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 offset = DamagePosition(enemy, center) - center;
                offset.z = 0f;
                if (offset.sqrMagnitude <= radiusSquared)
                {
                    results.Add(enemy);
                }
            }

            return results;
        }

        public static Vector3 DamagePosition(EnemyController enemy, Vector3 query)
        {
            var boss = enemy.GetComponent<Rushitadel.Progression.CampaignBoss>();
            return boss != null ? boss.DamagePosition(query) : enemy.transform.position;
        }
    }
}
