using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 소울라이크 전투: 공격 2콤보 + 구르기(무적 프레임).
/// ThirdPersonController 위에 얹히는 방식 — 액션 중에는 MovementLocked로 이동만 잠그고
/// 카메라 조작과 중력은 그대로 ThirdPersonController가 처리한다.
/// </summary>
[DefaultExecutionOrder(-10)] // ThirdPersonController보다 먼저 Update가 돌아야 이동 잠금이 같은 프레임에 반영됨
[RequireComponent(typeof(CharacterController), typeof(Health), typeof(Stamina))]
public class PlayerCombat : MonoBehaviour
{
    enum State { Locomotion, Attack1, Attack2, Roll, HitStun }

    [Header("공격")]
    [SerializeField] float attackDamage = 20f;
    [SerializeField] float attackReach = 1.5f;    // 판정 구체 중심까지의 전방 거리
    [SerializeField] float attackRadius = 1.1f;   // 판정 구체 반지름
    [Tooltip("휘두를 때 앞으로 나가는 속도(m/s). 0이면 제자리에서 공격한다.")]
    [SerializeField] float attackLunge = 0f;
    [SerializeField, Range(0f, 1f)] float hitMoment = 0.38f;   // 데미지 판정 시점 (정규화 시간)
    [SerializeField, Range(0f, 1f)] float comboMoment = 0.55f; // 이후부터 다음 액션으로 캔슬 가능
    [SerializeField] LayerMask hittableLayers = ~0;

    [Header("구르기")]
    [SerializeField] float rollDistance = 3.2f;        // 구르기 총 이동 거리(m)
    [Tooltip("이동에 걸리는 시간(초). 클립 길이와 무관하게 고정 — 클립이 짧아도 순간이동처럼 되지 않는다.")]
    [SerializeField] float rollMoveDuration = 0.62f;
    [SerializeField, Range(0f, 1f)] float iframeStart = 0.03f;
    [SerializeField, Range(0f, 1f)] float iframeEnd = 0.6f;

    [Header("구르기 반응성")]
    [Tooltip("구르기 전환 블렌딩 시간(초). 짧을수록 즉각적으로 느껴진다.")]
    [SerializeField] float rollCrossFade = 0.02f;
    [Tooltip("클립 앞부분의 준비 동작을 건너뛰는 비율(0~0.3).")]
    [SerializeField, Range(0f, 0.3f)] float rollStartSkip = 0.06f;

    [Header("스태미나 소모")]
    [SerializeField] float attackStaminaCost = 22f;
    [SerializeField] float rollStaminaCost = 20f;
    [SerializeField] float sprintStaminaPerSecond = 12f;
    [SerializeField] float sprintRecoverThreshold = 25f; // 고갈 후 이만큼 회복돼야 다시 달리기 가능

    [Header("애니메이션")]
    [SerializeField] float crossFade = 0.12f;
    [SerializeField] string locomotionStateName = "Idle Walk Run Blend";

    const string Attack1StateName = "Attack1";
    const string Attack2StateName = "Attack2";
    const string RollStateName = "Roll";
    const string HitStateName = "Hit";
    const string DeathStateName = "Death";
    // 클립 이름은 NewPlayerAnimationSetup이 임포트할 때 이 짧은 이름으로 바꿔준다
    const string Attack1ClipName = "Slash1";
    const string Attack2ClipName = "Slash2";
    const string RollClipName = "Dive";
    const string HitClipName = "Impact";
    const float InputBufferSeconds = 0.35f; // 선입력 유지 시간

    Animator _animator;
    CharacterController _controller;
    ThirdPersonController _tpc;
    StarterAssetsInputs _input;
    Health _health;
    Stamina _stamina;
    Transform _cameraTransform;

    State _state = State.Locomotion;
    float _stateTime;
    float _attackBufferedUntil = -1f;
    float _rollBufferedUntil = -1f;
    bool _didHit;
    Vector3 _rollDir;
    float _rollMoveElapsed;
    float _attack1Len = 1f, _attack2Len = 1f, _rollLen = 1f, _hitLen = 1f;
    readonly HashSet<Health> _hitThisSwing = new HashSet<Health>();

    public bool IsBusy => _state != State.Locomotion;

    bool AttackBuffered => Time.time <= _attackBufferedUntil;
    bool RollBuffered => Time.time <= _rollBufferedUntil;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _tpc = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _health = GetComponent<Health>();
        _stamina = GetComponent<Stamina>();
        _cameraTransform = Camera.main != null ? Camera.main.transform : null;

        _attack1Len = FindClipLength(Attack1ClipName);
        _attack2Len = FindClipLength(Attack2ClipName);
        _rollLen = FindClipLength(RollClipName);
        _hitLen = FindClipLength(HitClipName);

        _health.OnDamaged += HandleDamaged;
        _health.OnDied += HandleDied;
    }

    void HandleDamaged(float amount, Vector3 hitFrom)
    {
        if (_health.IsDead) return;
        GameAudio.PlayerHit();   // 때릴 때와 같은 타격음 — 맞는 감각을 똑같이 준다
        GameAudio.PlayerHurt();  // 신음 소리(파일이 있으면)
        StartHitStun();
    }

    void HandleDied()
    {
        _health.Invulnerable = false;
        _tpc.MovementLocked = true;
        var lockOn = GetComponent<LockOnSystem>();
        if (lockOn != null) lockOn.Unlock();
        _animator.CrossFadeInFixedTime(DeathStateName, 0.1f);
        GameAudio.PlayerDeath();
        GameAudio.StopBgm();
        // 리스폰은 GameManager가 처리
    }

    /// <summary>리스폰 시 전투 상태를 초기값으로 되돌린다.</summary>
    public void ResetCombatState()
    {
        _state = State.Locomotion;
        _stateTime = 0f;
        _attackBufferedUntil = -1f;
        _rollBufferedUntil = -1f;
        _didHit = false;
        _hitThisSwing.Clear();
        _health.Invulnerable = false;
        _tpc.MovementLocked = false;
        _tpc.SprintBlocked = false;
        _animator.CrossFadeInFixedTime(locomotionStateName, 0.1f);
    }

    float FindClipLength(string clipName)
    {
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;

        Debug.LogError($"애니메이션 클립을 찾을 수 없음: '{clipName}' — Tools > Setup Player Combat을 다시 실행하세요.", this);
        return 1f;
    }

#if ENABLE_INPUT_SYSTEM
    // PlayerInput(Send Messages)이 호출
    public void OnAttack(InputValue value)
    {
        if (value.isPressed) _attackBufferedUntil = Time.time + InputBufferSeconds;
    }

    public void OnRoll(InputValue value)
    {
        if (value.isPressed) _rollBufferedUntil = Time.time + InputBufferSeconds;
    }
#endif

    void Update()
    {
        if (_health.IsDead) return;

        HandleSprintStamina();

        switch (_state)
        {
            case State.Locomotion:
                UpdateLocomotion();
                break;
            case State.Attack1:
                UpdateAttack(_attack1Len, canChainToAttack2: true);
                break;
            case State.Attack2:
                UpdateAttack(_attack2Len, canChainToAttack2: false);
                break;
            case State.Roll:
                UpdateRoll();
                break;
            case State.HitStun:
                UpdateHitStun();
                break;
        }

        // 액션 중에는 점프 입력 무시 (버튼 상태만 소거, TPC가 원래 하던 방식과 동일)
        if (IsBusy) _input.jump = false;
    }

    void HandleSprintStamina()
    {
        // 달리는 동안 스태미나 소모
        bool sprinting = !IsBusy && _input.sprint && !_tpc.SprintBlocked &&
                         _input.move != Vector2.zero && _tpc.Grounded;
        if (sprinting)
            _stamina.Spend(sprintStaminaPerSecond * Time.deltaTime);

        // 고갈되면 달리기 차단, 일정량 회복될 때까지 유지
        if (_stamina.Current <= 0f)
            _tpc.SprintBlocked = true;
        else if (_tpc.SprintBlocked && _stamina.Current >= sprintRecoverThreshold)
            _tpc.SprintBlocked = false;
    }

    void UpdateLocomotion()
    {
        if (!_tpc.Grounded) return;

        if (RollBuffered && _stamina.CanAct)
        {
            StartRoll();
        }
        else if (AttackBuffered && _stamina.CanAct)
        {
            StartAttack(first: true);
        }
    }

    void UpdateAttack(float clipLength, bool canChainToAttack2)
    {
        _stateTime += Time.deltaTime;
        float n = _stateTime / clipLength;

        // 데미지 판정 (스윙당 1회)
        if (!_didHit && n >= hitMoment)
        {
            _didHit = true;
            DoHit();
        }

        // 휘두르는 동안 살짝 전진 (묵직한 발 디딤 느낌). 0이면 제자리 공격.
        if (attackLunge > 0f && n > 0.15f && n < 0.45f)
            _controller.Move(transform.forward * (attackLunge * Time.deltaTime));

        // 캔슬 윈도우: 구르기 우선, 그다음 콤보
        if (n >= comboMoment)
        {
            if (RollBuffered && _stamina.CanAct)
            {
                StartRoll();
                return;
            }
            if (canChainToAttack2 && AttackBuffered && _stamina.CanAct)
            {
                StartAttack(first: false);
                return;
            }
        }

        if (n >= 0.9f)
            EndAction();
    }

    void UpdateRoll()
    {
        float prevN = _stateTime / _rollLen;
        _stateTime += Time.deltaTime;
        float n = _stateTime / _rollLen;

        // 무적 프레임
        _health.Invulnerable = n >= iframeStart && n <= iframeEnd;

        // 구르기 이동: rollMoveDuration 동안 rollDistance만큼.
        // 클립 길이가 아니라 실제 시간을 기준으로 삼아야 속도가 예측 가능하다.
        float prevMove = _rollMoveElapsed;
        _rollMoveElapsed += Time.deltaTime;
        float progress = RollEase(Mathf.Clamp01(_rollMoveElapsed / rollMoveDuration));
        float prevProgress = RollEase(Mathf.Clamp01(prevMove / rollMoveDuration));
        _controller.Move(_rollDir * (rollDistance * (progress - prevProgress)));

        // 구르기 후반부 캔슬: 공격 또는 연속 구르기
        if (n >= 0.7f)
        {
            if (AttackBuffered && _stamina.CanAct)
            {
                _health.Invulnerable = false;
                StartAttack(first: true);
                return;
            }
            if (RollBuffered && _stamina.CanAct)
            {
                StartRoll();
                return;
            }
        }

        if (n >= 0.85f)
            EndAction();
    }

    /// <summary>
    /// 부드럽게 가속했다가 감속 (smoothstep). 이전의 급가속 곡선은 첫 프레임에
    /// 거리를 너무 많이 벌어서 순간이동처럼 보이고 카메라가 따라가지 못했다.
    /// </summary>
    static float RollEase(float t) => t * t * (3f - 2f * t);

    void StartHitStun()
    {
        _state = State.HitStun;
        _stateTime = 0f;
        _health.Invulnerable = false;
        _tpc.MovementLocked = true;
        _animator.CrossFadeInFixedTime(HitStateName, 0.08f);
    }

    void UpdateHitStun()
    {
        _stateTime += Time.deltaTime;
        if (_stateTime / _hitLen >= 0.75f)
            EndAction();
    }

    void StartAttack(bool first)
    {
        _stamina.Spend(attackStaminaCost);
        _attackBufferedUntil = -1f;
        _state = first ? State.Attack1 : State.Attack2;
        _stateTime = 0f;
        _didHit = false;
        _hitThisSwing.Clear();
        _health.Invulnerable = false;
        _tpc.MovementLocked = true;
        _animator.CrossFadeInFixedTime(first ? Attack1StateName : Attack2StateName, crossFade);
        GameAudio.PlayerSwing();
    }

    void StartRoll()
    {
        _stamina.Spend(rollStaminaCost);
        _rollBufferedUntil = -1f;
        _state = State.Roll;
        // 준비 동작을 건너뛴 지점에서 시작하므로 진행 시간도 그만큼 앞당겨 맞춘다
        _stateTime = _rollLen * rollStartSkip;
        _rollMoveElapsed = 0f;
        _tpc.MovementLocked = true;

        // 구르기 방향: 이동 입력(카메라 기준), 입력 없으면 현재 바라보는 방향
        Vector2 mv = _input.move;
        if (mv.sqrMagnitude > 0.01f && _cameraTransform != null)
        {
            float camYaw = _cameraTransform.eulerAngles.y;
            _rollDir = Quaternion.Euler(0f, camYaw, 0f) * new Vector3(mv.x, 0f, mv.y).normalized;
        }
        else
        {
            _rollDir = transform.forward;
        }
        transform.rotation = Quaternion.LookRotation(_rollDir);

        // 짧은 블렌딩 + 준비 구간을 건너뛴 시점부터 재생 → 키를 누른 즉시 튀어나가는 느낌
        _animator.CrossFadeInFixedTime(RollStateName, rollCrossFade, 0, _rollLen * rollStartSkip);
        GameAudio.PlayerRoll();
    }

    void EndAction()
    {
        _health.Invulnerable = false;
        _tpc.MovementLocked = false;
        _state = State.Locomotion;
        _animator.CrossFadeInFixedTime(locomotionStateName, 0.15f);
    }

    void DoHit()
    {
        Vector3 center = transform.position + transform.forward * attackReach + Vector3.up * 1f;
        foreach (var col in Physics.OverlapSphere(center, attackRadius, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target == _health || _hitThisSwing.Contains(target)) continue;

            _hitThisSwing.Add(target);
            target.TakeDamage(attackDamage, transform.position);

            // 타격감: 맞은 지점에서 순간 정지 + 카메라 흔들림 + 불꽃
            Vector3 contact = col.ClosestPoint(center);
            CombatFeedback.Impact(contact, hitStop: 0.075f, shakeAmplitude: 0.45f, particleCount: 26);
            GameAudio.PlayerHit();
            GameAudio.BossHurt();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackReach + Vector3.up * 1f, attackRadius);
    }
}
