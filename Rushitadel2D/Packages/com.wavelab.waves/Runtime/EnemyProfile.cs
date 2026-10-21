using System;
using System.Collections.Generic;
using UnityEngine;
using WaveLab.Core;
namespace WaveLab.Waves
{
    [Serializable] public sealed class NumericBinding
    {
        public string statId = "health.max";
        public string unit = "HP";
        public UnityEngine.Object source;
        public string propertyPath;
        public float snapshot;
        public string sampledUtc;
    }
    [CreateAssetMenu(menuName = "WaveLab/Enemy Profile")]
    public sealed class EnemyProfile : ScriptableObject
    {
        [SerializeField] private string id;
        public string Id => id;
        public GameObject prefab;
        [Tooltip("게임 연결용 식별자. 일반 프리팹에서는 비워 두세요.")]
        public string gameKey;
        public List<ResourceAmount> defeatRewards = new List<ResourceAmount>();
        public List<NumericBinding> statBindings = new List<NumericBinding>();
        private void OnValidate() { if (string.IsNullOrEmpty(id)) id = Guid.NewGuid().ToString("N"); }
    }
}
