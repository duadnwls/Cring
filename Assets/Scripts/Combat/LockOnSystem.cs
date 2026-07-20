using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 락온: Q/휠클릭으로 가까운 적을 타겟팅.
/// 카메라가 타겟을 향하고, 플레이어는 타겟을 바라보며 스트레이프 이동한다.
/// </summary>
[DefaultExecutionOrder(-5)] // ThirdPersonController(0)의 LateUpdate보다 먼저 실행되어 카메라 각도를 넘겨줌
public class LockOnSystem : MonoBehaviour
{
    [SerializeField] float findRadius = 18f;      // 락온 시작 가능 거리
    [SerializeField] float breakDistance = 22f;   // 이보다 멀어지면 자동 해제
    [SerializeField] float maxFindAngle = 65f;    // 카메라 정면 기준 탐색 각도
    [SerializeField] float cameraLerpSpeed = 9f;
    [SerializeField] float lockedPitch = 14f;     // 락온 중 카메라 내려다보는 각도
    [SerializeField] LayerMask targetLayers = ~0;

    public Transform CurrentTarget { get; private set; }

    ThirdPersonController _tpc;
    Health _selfHealth;
    Health _targetHealth;
    Transform _cam;
    Transform _indicator;
    float _yaw, _pitch;

    void Start()
    {
        _tpc = GetComponent<ThirdPersonController>();
        _selfHealth = GetComponent<Health>();
        _cam = Camera.main != null ? Camera.main.transform : null;

        // 타겟 위에 띄울 표시 (그레이박스용 빨간 구슬)
        var ind = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ind.name = "LockOnIndicator";
        Destroy(ind.GetComponent<Collider>());
        ind.transform.localScale = Vector3.one * 0.25f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = Color.red };
        ind.GetComponent<Renderer>().material = mat;
        ind.SetActive(false);
        _indicator = ind.transform;
    }

#if ENABLE_INPUT_SYSTEM
    public void OnLockOn(InputValue value)
    {
        if (!value.isPressed) return;

        if (CurrentTarget != null) Unlock();
        else Lock(FindTarget());
    }
#endif

    Transform FindTarget()
    {
        if (_cam == null) return null;

        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var col in Physics.OverlapSphere(transform.position, findRadius, targetLayers, QueryTriggerInteraction.Ignore))
        {
            var h = col.GetComponentInParent<Health>();
            if (h == null || h == _selfHealth || h.IsDead) continue;

            float angle = Vector3.Angle(_cam.forward, h.transform.position - _cam.position);
            if (angle > maxFindAngle) continue;

            // 화면 중앙에 가깝고 거리가 가까운 대상 우선
            float score = angle + Vector3.Distance(transform.position, h.transform.position) * 2f;
            if (score < bestScore)
            {
                bestScore = score;
                best = h.transform;
            }
        }
        return best;
    }

    void Lock(Transform target)
    {
        if (target == null) return;

        CurrentTarget = target;
        _targetHealth = target.GetComponentInParent<Health>();
        _tpc.StrafeTarget = target;
        _tpc.LockCameraPosition = true; // 마우스 카메라 입력 차단, 각도는 우리가 지정

        Vector3 e = _tpc.CinemachineCameraTarget.transform.rotation.eulerAngles;
        _yaw = e.y;
        _pitch = e.x > 180f ? e.x - 360f : e.x;

        _indicator.gameObject.SetActive(true);
    }

    public void Unlock()
    {
        CurrentTarget = null;
        _targetHealth = null;
        _tpc.StrafeTarget = null;
        _tpc.LockCameraPosition = false;
        if (_indicator != null)
            _indicator.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (CurrentTarget == null) return;

        // 타겟이 죽었거나 너무 멀어지면 해제
        if (_targetHealth == null || _targetHealth.IsDead ||
            Vector3.Distance(transform.position, CurrentTarget.position) > breakDistance)
        {
            Unlock();
            return;
        }

        _indicator.position = CurrentTarget.position + Vector3.up * 2.2f;

        // 카메라가 타겟 방향을 부드럽게 따라가도록 각도 지정
        Vector3 dir = CurrentTarget.position - transform.position;
        float desiredYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _yaw = Mathf.LerpAngle(_yaw, desiredYaw, Time.deltaTime * cameraLerpSpeed);
        _pitch = Mathf.Lerp(_pitch, lockedPitch, Time.deltaTime * cameraLerpSpeed);
        _tpc.SetCameraAngles(_yaw, _pitch);
    }
}
