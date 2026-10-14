using System.Collections;
using UnityEngine;
namespace WaveLab.Waves
{
    // Explicit opt-in demo only. Not a real enemy AI or a damage model.
    public sealed class DemoLifetime : MonoBehaviour
    {
        public float seconds = 2;
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(seconds);
            GetComponent<SpawnedUnit>()?.ReportDefeated();
            Destroy(gameObject);
        }
    }
}
