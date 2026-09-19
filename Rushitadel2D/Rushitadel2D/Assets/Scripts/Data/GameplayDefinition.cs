using UnityEngine;

namespace Rushitadel.Data
{
    /// <summary>Shared authoring data. Runtime health, timers and XP belong to components.</summary>
    public abstract class GameplayDefinition : ScriptableObject
    {
        public void Validate() => ValidateValues();
        protected virtual void OnValidate() => ValidateValues();
        protected abstract void ValidateValues();
    }
}
