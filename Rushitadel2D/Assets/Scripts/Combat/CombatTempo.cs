using UnityEngine;
namespace Rushitadel.Combat
{
    // Local combat tempo: never changes menus, spawning, rewards or Time.timeScale.
    public static class CombatTempo
    {
        public const float SpeedMultiplier=1.7f;
        public static float DeltaTime=>Time.deltaTime*SpeedMultiplier;
    }
}
