using UnityEngine;
using Rushitadel.Combat;
using Rushitadel.World;
using Rushitadel.Spawning;
namespace Rushitadel.Progression
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController),typeof(EnemyRangedAttack))]
    public sealed class CampaignBossPrefab : MonoBehaviour
    {
        public BossKind kind;
        [Range(1,3)] public int stage=1;
        private void Start()
        {
            if(GetComponent<CampaignBoss>()!=null)return;
            var attack=GetComponent<EnemyRangedAttack>();attack.enabled=false;
            GetComponent<EnemyController>().ConfigureRun(EnemySpawner.BossHealthForStage(stage),100,true);
            if(TryGetComponent<CullBehindCamera>(out var cull))cull.enabled=false;
            var theme=kind==BossKind.Butterfly?PressureTheme.Grass:kind==BossKind.Lava?PressureTheme.Fire:kind==BossKind.Ice?PressureTheme.Ice:PressureTheme.Water;
            gameObject.AddComponent<CampaignBoss>().Configure(attack.ProjectilePrefab,stage,theme);
        }
    }
}
