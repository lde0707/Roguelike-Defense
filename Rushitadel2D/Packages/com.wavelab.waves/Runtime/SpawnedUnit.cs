using System;
using UnityEngine;
namespace WaveLab.Waves
{
    // Existing health scripts call ReportDefeated before despawning. Destruction is not a kill.
    public sealed class SpawnedUnit : MonoBehaviour
    {
        private Action<bool> resolved;
        public void Initialize(Action<bool> callback) { resolved = callback; }
        public void ReportDefeated() { Resolve(true); }
        public void ReportDespawned() { Resolve(false); }
        private void OnDestroy() { Resolve(false); }
        private void Resolve(bool defeated)
        {
            var callback = resolved; resolved = null; callback?.Invoke(defeated);
        }
        internal void Detach() { resolved = null; }
    }
}
