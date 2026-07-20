using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 보스 AI: 대기 → 포효(어그로) → 추적 → 근접 공격(2패턴 랜덤) → 쿨다운 반복.
/// 플레이어와 같은 방식의 코드 주도 애니메이션(CrossFade) 사용.
/// </summary>
[RequireComponent(typeof(Health), typeof(NavMeshAgent))]
public class BossAI : MonoBehaviour
{
    enum State { Dormant, Roar, Chase, Attack, Cooldown, Dead }

    [Header("감지/이동")]
    [SerializeField] float aggroRange = 12f;
    [SerializeField] float attackRange = 2.4f;
    [SerializeField] float turnSpeed = 8f;

    [Header("공격")]
    [SerializeField] float swipeDamage = 15f;
    [SerializeField] float punchDamage = 25f;
    [SerializeField, Range(0f, 1f)] float punchChance = 0.4f;
    [SerializeField] float hitReach = 1.8f;    // 판정 구체 중심까지 전방 거리
    [SerializeField] float hitRadius = 1.5f;
    [SerializeField, Range(0f, 1f)] float hitMoment = 0.45f;
    [SerializeField] Vector2 cooldownRange = new Vector2(0.7f, 1.6f);

    const string IdleState = "Idle";
    const string WalkState = "Walk";
    const string SwipeState = "Swipe";
    const string PunchState = "Punch";
    const string RoarState = "Roar";
    const string DieState = "Die";

    Animator _animator;
    NavMeshAgent _agent;
    Health _health;
    Transform _player;
    Health _playerHealth;

    State _state = State.Dormant;
    float _stateTime;
    float _cooldownDuration;
    bool _didHit;
    float _currentAttackLen;
    float _currentAttackDamage;

    float _swipeLen = 1f, _punchLen = 1f, _roarLen = 1f;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _health.OnDied += HandleDied;
        _health.OnDamaged += (amount, from) =>
        {
            Debug.Log($"[보스] {amount} 피해! 남은 체력: {_health.Current}/{_health.Max}");
            if (_state == State.Dormant) EnterRoar(); // 선공당하면 바로 전투 시작
        };

        var tpc = FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (tpc != null)
        {
            _player = tpc.transform;
            _playerHealth = tpc.GetComponent<Health>();
        }

        _swipeLen = FindClipLength("Mutant Swiping");
        _punchLen = FindClipLength("Mutant Punch");
        _roarLen = FindClipLength("Mutant Roaring");

        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(IdleState, 0.1f);
    }

    float FindClipLength(string clipName)
    {
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;
        Debug.LogError($"보스 클립을 찾을 수 없음: '{clipName}'", this);
        return 1f;
    }

    float DistanceToPlayer => _player != null
        ? Vector3.Distance(transform.position, _player.position)
        : float.MaxValue;

    void Update()
    {
        if (_state == State.Dead || _player == null) return;

        // 플레이어가 죽으면 전투 종료
        if (_playerHealth != null && _playerHealth.IsDead && _state != State.Dormant)
        {
            EnterIdleVictory();
            return;
        }

        _stateTime += Time.deltaTime;

        switch (_state)
        {
            case State.Dormant:
                if (DistanceToPlayer <= aggroRange) EnterRoar();
                break;

            case State.Roar:
                FacePlayer();
                if (_stateTime >= _roarLen * 0.9f) EnterChase();
                break;

            case State.Chase:
                _agent.SetDestination(_player.position);
                if (DistanceToPlayer <= attackRange) EnterAttack();
                break;

            case State.Attack:
                UpdateAttack();
                break;

            case State.Cooldown:
                FacePlayer();
                if (_stateTime >= _cooldownDuration) EnterChase();
                break;
        }
    }

    void FacePlayer()
    {
        Vector3 to = _player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(to), Time.deltaTime * turnSpeed);
    }

    void EnterRoar()
    {
        if (_state == State.Roar || _state == State.Dead) return;
        _state = State.Roar;
        _stateTime = 0f;
        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(RoarState, 0.15f);
    }

    void EnterChase()
    {
        _state = State.Chase;
        _stateTime = 0f;
        _agent.isStopped = false;
        _animator.CrossFadeInFixedTime(WalkState, 0.2f);
    }

    void EnterAttack()
    {
        _state = State.Attack;
        _stateTime = 0f;
        _didHit = false;
        _agent.isStopped = true;

        bool punch = Random.value < punchChance;
        _currentAttackLen = punch ? _punchLen : _swipeLen;
        _currentAttackDamage = punch ? punchDamage : swipeDamage;
        _animator.CrossFadeInFixedTime(punch ? PunchState : SwipeState, 0.1f);
    }

    void UpdateAttack()
    {
        float n = _stateTime / _currentAttackLen;

        // 초반에만 방향 보정 (후반엔 휘두른 방향 유지 → 구르기로 피할 수 있게)
        if (n < 0.3f) FacePlayer();

        if (!_didHit && n >= hitMoment)
        {
            _didHit = true;
            DoHit();
        }

        if (n >= 0.9f)
        {
            _state = State.Cooldown;
            _stateTime = 0f;
            _cooldownDuration = Random.Range(cooldownRange.x, cooldownRange.y);
            _animator.CrossFadeInFixedTime(IdleState, 0.2f);
        }
    }

    void DoHit()
    {
        Vector3 center = transform.position + transform.forward * hitReach + Vector3.up * 1.2f;
        foreach (var col in Physics.OverlapSphere(center, hitRadius, ~0, QueryTriggerInteraction.Ignore))
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target == _health) continue;
            target.TakeDamage(_currentAttackDamage, transform.position);
        }
    }

    void EnterIdleVictory()
    {
        _state = State.Dormant;
        _stateTime = 0f;
        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(IdleState, 0.3f);
    }

    void HandleDied()
    {
        _state = State.Dead;
        _agent.isStopped = true;
        _agent.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
        _animator.CrossFadeInFixedTime(DieState, 0.15f);
        Debug.Log("[보스] 처치! 클리어!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * hitReach + Vector3.up * 1.2f, hitRadius);
    }
}
