using System.Collections.Generic;
using UnityEngine;
using Rushitadel.Presentation;

namespace Rushitadel.Combat
{
    // A finite, per-volley circuit. No global chains, no bonus XP and no repeated conversions.
    public sealed class WaveVolley
    {
        readonly List<EnemyProjectileController> members = new();
        bool broken;
        int returns;
        public bool Broken => broken;
        public int ReturnCount => returns;
        public void Add(EnemyProjectileController shot, bool core)
        {
            members.Add(shot);
            shot.LinkVolley(this, core);
        }
        public void BreakCore()
        {
            if (broken) return;
            broken = true;
            foreach (var shot in members)
                if (shot != null && shot.IsAlive) shot.ArmCounterRicochet();
        }
        public bool ClaimReturn()
        {
            if (!broken || returns >= 3) return false;
            returns++;
            return true;
        }
    }

    public static class WavePattern
    {
        public static readonly string[] Names = { "부채 탄막", "엇갈린 사선", "물결 탄막", "회전 나선", "조준 3연격" };
        public static int Count(int pattern, bool elite) => pattern == 4 ? 3 : elite ? 7 : 5;
        public static Vector2 Direction(Vector2 aim, int pattern, int index, int count, int volley)
        {
            float centered = index - (count - 1) * .5f;
            float angle = pattern switch
            {
                0 => centered * 12f,
                1 => centered * 10f + (volley % 2 == 0 ? 8 : -8),
                2 => centered * 10f + Mathf.Sin(volley * 1.3f) * 14,
                3 => centered * 11f + (volley % 4 - 1.5f) * 8,
                _ => centered * 7f
            };
            return (Vector2)(Quaternion.Euler(0, 0, angle) * aim.normalized);
        }
        public static float Speed(int pattern, int index, bool elite)
            => (pattern == 4 ? 3.1f + index * .45f : pattern == 2 ? 2.4f + index * .12f : 2.8f) * (elite ? 1.08f : 1);
    }
}
