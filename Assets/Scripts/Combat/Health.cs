using System;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitFrom);
}

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    /// <summary>무적 상태 (구르기 i-frame 등). true인 동안 모든 피해 무시.</summary>
    public bool Invulnerable { get; set; }

    /// <summary>(피해량, 공격자 위치)</summary>
    public event Action<float, Vector3> OnDamaged;
    public event Action OnDied;

    void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitFrom)
    {
        if (IsDead || Invulnerable) return;

        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount, hitFrom);

        if (IsDead)
            OnDied?.Invoke();
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        Invulnerable = false;
    }
}
