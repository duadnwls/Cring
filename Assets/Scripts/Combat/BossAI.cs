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
    [SerializeField] float swipeDamage = 22f;
    [SerializeField] float punchDamage = 34f;
    [SerializeField, Range(0f, 1f)] float punchChance = 0.4f;
    [Tooltip("판정 구체 중심까지의 전방 거리. 크면 밀착한 상대가 사각지대에 들어간다.")]
    [SerializeField] float hitReach = 1.1f;
    [SerializeField] float hitRadius = 1.9f;
    [SerializeField, Range(0f, 1f)] float hitMoment = 0.45f;
    [SerializeField] Vector2 cooldownRange = new Vector2(0.55f, 1.15f);

    [Tooltip("공격 중 앞으로 밀고 들어오는 속도(m/s). 굴러서 도망가는 플레이어를 따라붙는다.")]
    [SerializeField] float attackLunge = 2.2f;

    [Header("연속 공격")]
    [Tooltip("1페이즈에서 한 번에 이어붙일 공격 횟수 범위")]
    [SerializeField] Vector2Int comboRange = new Vector2Int(1, 2);
    [Tooltip("연속 공격 사이의 짧은 간격(초)")]
    [SerializeField] float comboGap = 0.18f;

    [Header("디버그")]
    [Tooltip("공격 판정 순간의 정보를 콘솔에 남긴다. 빌드 전에는 꺼둘 것.")]
    [SerializeField] bool debugHits = false;

    [Header("2페이즈")]
    [Tooltip("체력이 이 비율 이하로 떨어지면 광폭화")]
    [SerializeField, Range(0f, 1f)] float phase2Threshold = 0.5f;
    [SerializeField] Vector2Int phase2ComboRange = new Vector2Int(2, 4);
    [SerializeField] Vector2 phase2CooldownRange = new Vector2(0.3f, 0.7f);
    [SerializeField] float phase2SpeedMultiplier = 1.35f;
    [SerializeField] float phase2DamageMultiplier = 1.2f;

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
    Vector3 _spawnPosition;
    Quaternion _spawnRotation;

    bool _isPhase2;
    int _comboRemaining;
    float _baseAgentSpeed;
    float _baseTurnSpeed;

    void Awake()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    void Start()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _health.OnDied += HandleDied;
        _health.OnDamaged += (amount, from) =>
        {
            if (_state == State.Dormant) EnterRoar(); // 선공당하면 바로 전투 시작
            CheckPhase2();
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

        _baseAgentSpeed = _agent.speed;
        _baseTurnSpeed = turnSpeed;
        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(IdleState, 0.1f);
    }

    /// <summary>체력이 절반 아래로 떨어지면 광폭화 — 포효하고 더 빠르고 집요해진다.</summary>
    void CheckPhase2()
    {
        if (_isPhase2 || _state == State.Dead) return;
        if (_health.Current / _health.Max > phase2Threshold) return;

        _isPhase2 = true;
        _agent.speed = _baseAgentSpeed * phase2SpeedMultiplier;
        turnSpeed = _baseTurnSpeed * 1.4f;
        _comboRemaining = 0;

        CombatFeedback.Instance?.Shake(0.8f, 0.6f);
        Debug.Log("[보스] 2페이즈 돌입!");
        EnterRoar();
    }

    float FindClipLength(string clipName)
    {
        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;
        Debug.LogError($"보스 클립을 찾을 수 없음: '{clipName}'", this);
        return 1f;
    }

    /// <summary>전투 중 여부 — 보스 체력바 표시 조건</summary>
    public bool IsEngaged => _state != State.Dormant && _state != State.Dead;

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
                if (_stateTime >= _cooldownDuration)
                {
                    // 연속타가 남아 있고 아직 사거리 안이면 곧바로 다음 타
                    if (_comboRemaining > 0 && DistanceToPlayer <= attackRange * 1.6f)
                        StartSingleAttack();
                    else
                        EnterChase();
                }
                break;
        }
    }

    void FacePlayer(float speedFactor = 1f)
    {
        Vector3 to = _player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(to), Time.deltaTime * turnSpeed * speedFactor);
    }

    void EnterRoar()
    {
        if (_state == State.Roar || _state == State.Dead) return;
        _state = State.Roar;
        _stateTime = 0f;
        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(RoarState, 0.15f);
        GameAudio.BossRoar();
    }

    void EnterChase()
    {
        _state = State.Chase;
        _stateTime = 0f;
        _agent.isStopped = false;
        _animator.CrossFadeInFixedTime(WalkState, 0.2f);
    }

    /// <summary>새 연속 공격을 시작한다. 몇 대를 이어칠지 여기서 정한다.</summary>
    void EnterAttack()
    {
        var range = _isPhase2 ? phase2ComboRange : comboRange;
        _comboRemaining = Random.Range(range.x, range.y + 1);
        StartSingleAttack();
    }

    void StartSingleAttack()
    {
        _state = State.Attack;
        _stateTime = 0f;
        _didHit = false;
        _agent.isStopped = true;
        _comboRemaining--;

        bool punch = Random.value < punchChance;
        _currentAttackLen = punch ? _punchLen : _swipeLen;
        _currentAttackDamage = (punch ? punchDamage : swipeDamage) *
                               (_isPhase2 ? phase2DamageMultiplier : 1f);
        _animator.CrossFadeInFixedTime(punch ? PunchState : SwipeState, 0.1f);
        GameAudio.BossSwing();
    }

    void UpdateAttack()
    {
        float n = _stateTime / _currentAttackLen;

        // 초반엔 정상 속도로, 판정 직전까지는 느리게 계속 추적한다.
        // 예전엔 앞 30%에서 회전을 멈춰서, 밀착한 채 옆으로 도는 것만으로 안전했다.
        if (n < 0.3f) FacePlayer();
        else if (n < hitMoment * 0.9f) FacePlayer(0.35f);

        // 휘두르면서 앞으로 밀고 들어온다 — 뒤로만 굴러서는 못 피한다.
        // NavMeshAgent가 켜져 있으므로 transform을 직접 옮기지 않고 Agent.Move를 쓴다.
        if (attackLunge > 0f && n > 0.1f && n < hitMoment && DistanceToPlayer > 1.2f)
            _agent.Move(transform.forward * (attackLunge * Time.deltaTime));

        if (!_didHit && n >= hitMoment)
        {
            _didHit = true;
            DoHit();
        }

        if (n >= 0.9f)
        {
            // 남은 연속타가 있으면 짧은 간격만 두고 바로 다음 공격
            if (_comboRemaining > 0)
            {
                _state = State.Cooldown;
                _stateTime = 0f;
                _cooldownDuration = comboGap;
            }
            else
            {
                _state = State.Cooldown;
                _stateTime = 0f;
                var range = _isPhase2 ? phase2CooldownRange : cooldownRange;
                _cooldownDuration = Random.Range(range.x, range.y);
            }
            _animator.CrossFadeInFixedTime(IdleState, 0.2f);
        }
    }

    void DoHit()
    {
        Vector3 center = transform.position + transform.forward * hitReach + Vector3.up * 1.2f;
        var hits = Physics.OverlapSphere(center, hitRadius, ~0, QueryTriggerInteraction.Ignore);

        if (debugHits)
        {
            string names = hits.Length == 0
                ? "(없음)"
                : string.Join(", ", System.Array.ConvertAll(hits, c => c.name));
            float distToPlayer = _player != null
                ? Vector3.Distance(center, _player.position + Vector3.up * 1f)
                : -1f;
            Debug.Log($"[보스 판정] 중심={center} 반경={hitRadius} | 걸린 콜라이더: {names}\n" +
                      $"  플레이어까지(중심 기준) {distToPlayer:F2}m, " +
                      $"보스-플레이어 거리 {DistanceToPlayer:F2}m, " +
                      $"플레이어 무적={(_playerHealth != null && _playerHealth.Invulnerable)}");
        }

        foreach (var col in hits)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target == _health) continue;

            bool blocked = target.Invulnerable || target.IsDead; // 구르기 무적으로 흘렸는지
            target.TakeDamage(_currentAttackDamage, transform.position);
            if (blocked) continue;

            // 맞은 쪽이 더 크게 흔들린다
            Vector3 contact = col.ClosestPoint(center);
            CombatFeedback.Impact(contact, hitStop: 0.06f, shakeAmplitude: 1f, particleCount: 18);
        }
    }

    /// <summary>플레이어 리스폰 시 보스를 초기 상태로 되돌린다.</summary>
    public void ResetBoss()
    {
        _health.ResetHealth();

        // NavMeshAgent가 켜진 채로 위치를 바꾸면 무시되므로 Warp 사용
        if (_agent.enabled && _agent.isOnNavMesh)
            _agent.Warp(_spawnPosition);
        else
            transform.position = _spawnPosition;
        transform.rotation = _spawnRotation;

        _state = State.Dormant;
        _stateTime = 0f;
        _didHit = false;
        _isPhase2 = false;
        _comboRemaining = 0;
        _agent.speed = _baseAgentSpeed;
        turnSpeed = _baseTurnSpeed;
        _agent.isStopped = true;
        _animator.CrossFadeInFixedTime(IdleState, 0.2f);
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
        GameAudio.BossDeath();
        GameAudio.StopBgm();
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
