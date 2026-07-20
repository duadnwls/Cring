using System.Collections;
using UnityEngine;

/// <summary>
/// 공격 판정 테스트용 허수아비. 맞으면 빨갛게 번쩍이고, 죽으면 쓰러졌다가 3초 뒤 부활한다.
/// </summary>
[RequireComponent(typeof(Health))]
public class TrainingDummy : MonoBehaviour
{
    [SerializeField] float respawnDelay = 3f;

    Health _health;
    Renderer _renderer;
    Color _baseColor;
    Quaternion _uprightRotation;

    void Awake()
    {
        _health = GetComponent<Health>();
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
            _baseColor = _renderer.material.color;
        _uprightRotation = transform.rotation;

        _health.OnDamaged += HandleDamaged;
        _health.OnDied += HandleDied;
    }

    void HandleDamaged(float amount, Vector3 hitFrom)
    {
        Debug.Log($"[더미] {amount} 피해! 남은 체력: {_health.Current}/{_health.Max}");
        StopAllCoroutines();
        StartCoroutine(Flash());
    }

    void HandleDied()
    {
        StopAllCoroutines();
        StartCoroutine(DieAndRespawn());
    }

    IEnumerator Flash()
    {
        if (_renderer == null) yield break;
        _renderer.material.color = Color.red;
        yield return new WaitForSeconds(0.12f);
        _renderer.material.color = _baseColor;
    }

    IEnumerator DieAndRespawn()
    {
        // 뒤로 쓰러지는 연출
        float t = 0f;
        Quaternion fallen = _uprightRotation * Quaternion.Euler(-90f, 0f, 0f);
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(_uprightRotation, fallen, t / 0.4f);
            yield return null;
        }

        yield return new WaitForSeconds(respawnDelay);

        transform.rotation = _uprightRotation;
        if (_renderer != null) _renderer.material.color = _baseColor;
        _health.ResetHealth();
        Debug.Log("[더미] 부활!");
    }
}
