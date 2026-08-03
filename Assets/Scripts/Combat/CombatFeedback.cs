using System.Collections;
using UnityEngine;

/// <summary>
/// 타격감 담당: 히트스톱(순간 정지), 카메라 흔들림, 타격 파티클.
/// 씬에 하나만 두고 CombatFeedback.Instance로 호출한다.
/// </summary>
/// <remarks>
/// 실행 순서를 크게 잡아 CinemachineBrain(순서 0)이 카메라 트랜스폼을 쓴 뒤에 흔들림을 덮어쓴다.
/// 예전처럼 카메라 추적 대상을 흔들면 vcam의 Damping(0.1~0.3)에 거의 다 걸러진다.
/// </remarks>
[DefaultExecutionOrder(10000)]
public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }

    [Header("히트스톱")]
    [SerializeField, Range(0.01f, 0.5f)] float hitStopScale = 0.05f;

    [Header("카메라 흔들림")]
    [SerializeField] float shakeFrequency = 26f;
    [SerializeField] float positionScale = 0.12f;  // 진폭 1당 이동(m)
    [SerializeField] float rotationScale = 4f;     // 진폭 1당 회전(도) — 흔들림은 회전이 더 잘 느껴진다

    Transform _camera;
    float _shakeAmplitude, _shakeTimer, _shakeDuration;
    float _noiseSeed;

    float _hitStopUntil;
    bool _hitStopRunning;

    ParticleSystem _impactParticles;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (Camera.main != null)
            _camera = Camera.main.transform;
        else
            Debug.LogWarning("Main Camera를 찾지 못해 카메라 흔들림이 비활성화됩니다.", this);

        BuildImpactParticles();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f; // 히트스톱 도중 씬이 바뀌어도 복구
        }
    }

    void BuildImpactParticles()
    {
        var go = new GameObject("ImpactParticles");
        go.transform.SetParent(transform, false);
        _impactParticles = go.AddComponent<ParticleSystem>();
        // 추가되는 순간 자동 재생되므로, 설정을 바꾸기 전에 완전히 정지시켜야 한다
        _impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _impactParticles.main;
        // 계속 재생 상태로 두되 방출은 0 — Emit()으로 터뜨린 입자가 정상적으로 시뮬레이션된다
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.gravityModifier = 1.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.45f), new Color(0.85f, 0.2f, 0.12f));

        var emission = _impactParticles.emission;
        emission.enabled = false; // Emit()으로 직접 발생시킴

        var shape = _impactParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.color = Color.white;
        renderer.material = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        _impactParticles.Play();
    }

    /// <summary>공격이 적중했을 때 한 번에 호출하는 편의 메서드.</summary>
    public static void Impact(Vector3 position, float hitStop, float shakeAmplitude, int particleCount = 24)
    {
        if (Instance == null) return;
        Instance.HitStop(hitStop);
        Instance.Shake(shakeAmplitude, 0.22f);
        Instance.EmitImpact(position, particleCount);
    }

    public void HitStop(float duration)
    {
        if (duration <= 0f) return;
        _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + duration);
        if (!_hitStopRunning) StartCoroutine(HitStopRoutine());
    }

    IEnumerator HitStopRoutine()
    {
        _hitStopRunning = true;
        Time.timeScale = hitStopScale;
        while (Time.unscaledTime < _hitStopUntil)
            yield return null;
        Time.timeScale = 1f;
        _hitStopRunning = false;
    }

    public void Shake(float amplitude, float duration)
    {
        // 더 강한 흔들림이 들어오면 덮어쓴다
        if (amplitude < _shakeAmplitude && _shakeTimer > 0f) return;
        _shakeAmplitude = amplitude;
        _shakeDuration = duration;
        _shakeTimer = duration;
        _noiseSeed = Random.value * 100f; // 매번 다른 방향으로 흔들리도록
    }

    public void EmitImpact(Vector3 position, int count)
    {
        if (_impactParticles == null) return;
        var p = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
        _impactParticles.Emit(p, count);
    }

    void LateUpdate()
    {
        if (_camera == null || _shakeTimer <= 0f) return;

        // 히트스톱 중에도 흔들리도록 unscaled 시간 사용
        _shakeTimer -= Time.unscaledDeltaTime;
        float falloff = Mathf.Clamp01(_shakeTimer / _shakeDuration);
        float strength = _shakeAmplitude * falloff * falloff;
        float t = Time.unscaledTime * shakeFrequency + _noiseSeed;

        // 펄린 노이즈로 부드럽게 흔든다 (랜덤보다 덜 지저분함)
        var noise = new Vector3(
            (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f,
            (Mathf.PerlinNoise(t, t) - 0.5f) * 2f);

        // Cinemachine이 이미 써놓은 트랜스폼 위에 덧씌운다 (다음 프레임에 다시 덮어써짐)
        _camera.position += _camera.rotation * (noise * (strength * positionScale));
        _camera.rotation *= Quaternion.Euler(noise * (strength * rotationScale));
    }
}
