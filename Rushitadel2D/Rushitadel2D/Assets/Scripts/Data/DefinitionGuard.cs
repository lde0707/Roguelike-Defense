using UnityEngine;

namespace Rushitadel.Data
{
    public static class DefinitionGuard
    {
        public static bool Require(MonoBehaviour owner, GameplayDefinition definition)
        {
            if (definition != null) return true;
            Debug.LogError($"{owner.name}/{owner.GetType().Name}: assign a definition asset in the Inspector.", owner);
            owner.enabled = false;
            return false;
        }
    }
}
