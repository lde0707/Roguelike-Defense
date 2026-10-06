namespace Rushitadel.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        float MaximumHealth { get; }
        void TakeDamage(float amount);
    }
}
