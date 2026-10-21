using UnityEngine;
using Rushitadel.World;
namespace Rushitadel.Combat
{
    // Prefab identity is independent of the theme selected for the current stage.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(EnemyRangedAttack))]
    public sealed class CampaignEnemyPrefab : MonoBehaviour
    {
        [Range(0,6)] public int variant;
        public bool overrideTheme;
        public PressureTheme theme;
        public PressureTheme ResolveTheme(PressureTheme fallback)=>overrideTheme?theme:fallback;
        private void Start()
        {
            if (GetComponent<EnemyArchetype>() != null) return;
            var pressure = ForwardPressureController.Instance;
            var director = Rushitadel.Progression.CampaignDirector.Instance;
            gameObject.AddComponent<EnemyArchetype>().Configure(variant,
                director != null && director.Initialized ? director.Stage : 1,
                ResolveTheme(pressure != null ? pressure.Theme : PressureTheme.Grass));
        }
    }
}
