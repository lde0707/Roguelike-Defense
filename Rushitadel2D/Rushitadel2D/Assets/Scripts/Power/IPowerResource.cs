using System;

namespace Rushitadel.Power
{
    public interface IPowerResource
    {
        event Action<float, float> PowerChanged;

        float CurrentPower { get; }
        float MaximumPower { get; }
        bool TrySpend(float amount);
    }
}
