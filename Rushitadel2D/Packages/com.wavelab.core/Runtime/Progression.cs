using System;
using System.Collections.Generic;
namespace WaveLab.Core
{
    public enum ModifierOperation { Flat, AdditivePercent, Multiply }
    [Serializable] public sealed class StatValue
    {
        public string id = "health.max";
        public string unit = "HP";
        public float value = 100;
    }
    [Serializable] public sealed class StatModifier
    {
        public string sourceId;
        public string statId;
        public ModifierOperation operation;
        public float amount;
    }
    public static class StatMath
    {
        // (base + sum(flat)) * (1 + sum(percent)) * product(multiplier), then clamp.
        public static float Evaluate(string id, float baseValue, IEnumerable<StatModifier> modifiers,
            float minimum = 0, float maximum = float.MaxValue)
        {
            double flat = 0, percent = 0, product = 1;
            if (minimum > maximum) throw new ArgumentException("Invalid stat limits.");
            if (modifiers != null) foreach (var m in modifiers)
            {
                if (m == null || m.statId != id) continue;
                if (float.IsNaN(m.amount) || float.IsInfinity(m.amount)) throw new ArgumentException("Non-finite modifier.");
                switch (m.operation)
                {
                    case ModifierOperation.Flat: flat += m.amount; break;
                    case ModifierOperation.AdditivePercent: percent += m.amount; break;
                    case ModifierOperation.Multiply: product *= m.amount; break;
                }
            }
            return (float)Math.Max(minimum, Math.Min(maximum, (baseValue + flat) * (1 + percent) * product));
        }
    }
    [Serializable] public sealed class ResourceAmount
    {
        public string resourceId = "xp";
        public float amount;
    }
    [Serializable] public sealed class UpgradeRank
    {
        public string definitionId;
        public int rank;
    }
    // Mutable run data only. Never mutate authoring assets to represent upgrades.
    [Serializable] public sealed class GrowthState
    {
        public int schemaVersion = 1;
        public int seed = 1;
        public int level = 1;
        public List<ResourceAmount> resources = new List<ResourceAmount>();
        public List<UpgradeRank> upgrades = new List<UpgradeRank>();
        public List<string> purchasedNodeIds = new List<string>();
        public List<StatModifier> modifiers = new List<StatModifier>();
    }
    public interface IStatProvider { bool TryGetStat(string statId, out float value); }
    public interface IRewardReceiver { void Grant(string resourceId, float amount); }
}
