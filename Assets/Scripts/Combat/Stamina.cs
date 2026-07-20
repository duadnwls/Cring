using UnityEngine;

/// <summary>
/// 소울라이크 스태미나: 소모 후 잠깐의 딜레이를 두고 회복.
/// 스태미나가 0보다 크면 액션 가능 (소모로 0 밑으로 내려가지는 않음 — 다크소울 방식).
/// </summary>
public class Stamina : MonoBehaviour
{
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float regenPerSecond = 28f;
    [SerializeField] float regenDelay = 0.6f; // 마지막 소모 후 회복 시작까지

    public float Max => maxStamina;
    public float Current { get; private set; }

    /// <summary>공격/구르기 가능 여부 — 조금이라도 남아 있으면 허용</summary>
    public bool CanAct => Current > 0f;

    float _lastSpendTime = -99f;

    void Awake()
    {
        Current = maxStamina;
    }

    void Update()
    {
        if (Time.time - _lastSpendTime >= regenDelay && Current < maxStamina)
            Current = Mathf.Min(maxStamina, Current + regenPerSecond * Time.deltaTime);
    }

    public void Spend(float amount)
    {
        Current = Mathf.Max(0f, Current - amount);
        _lastSpendTime = Time.time;
    }

    public void ResetStamina()
    {
        Current = maxStamina;
    }
}
