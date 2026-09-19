using Rushitadel.Power;
using UnityEngine;

namespace Rushitadel.Spawning
{
    /// <summary>Scene composition: connects prefab instances without storing scene references in assets.</summary>
    public sealed class GameplayBindings : MonoBehaviour
    {
        [SerializeField] private PlayerPower playerPower;
        [SerializeField] private EnemySpawner enemySpawner;

        private void Awake()
        {
            if (playerPower == null || enemySpawner == null)
            {
                Debug.LogError("Gameplay Bindings requires a player and an enemy spawner.", this);
                enabled = false;
                return;
            }
            enemySpawner.SetEnemyCountModifier(playerPower);
        }
    }
}
