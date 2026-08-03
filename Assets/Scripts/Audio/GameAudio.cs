using UnityEngine;

/// <summary>
/// 효과음/배경음 재생 담당. 클립이 비어 있으면 조용히 넘어가므로,
/// 사운드 파일이 아직 없어도 게임은 정상 동작한다.
/// </summary>
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("플레이어")]
    [SerializeField] AudioClip playerSwing;   // 검 휘두르는 소리
    [SerializeField] AudioClip playerHit;     // 적을 때렸을 때
    [SerializeField] AudioClip playerHurt;    // 맞았을 때
    [SerializeField] AudioClip playerRoll;    // 구르기
    [SerializeField] AudioClip playerDeath;

    [Header("보스")]
    [SerializeField] AudioClip bossRoar;
    [SerializeField] AudioClip bossSwing;
    [SerializeField] AudioClip bossHurt;
    [SerializeField] AudioClip bossDeath;

    [Header("타이밍")]
    [Tooltip("검 휘두르는 소리를 늦게 내보내는 시간(초). 실제 칼이 지나가는 순간에 맞춘다.")]
    [SerializeField] float swingDelay = 0.3f;

    [Header("배경음")]
    [SerializeField] AudioClip bgm;
    [SerializeField, Range(0f, 1f)] float bgmVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] float sfxVolume = 0.85f;

    AudioSource _bgmSource;
    AudioSource[] _sfxSources;
    int _nextSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = bgmVolume;
        _bgmSource.spatialBlend = 0f;

        // 소리가 겹쳐도 끊기지 않도록 여러 개를 돌려 쓴다
        _sfxSources = new AudioSource[8];
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            _sfxSources[i] = src;
        }

        if (bgm != null)
        {
            _bgmSource.clip = bgm;
            _bgmSource.Play();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void PlayClip(AudioClip clip, float volumeScale, float pitchJitter, float delay = 0f)
    {
        if (clip == null) return;

        var src = _sfxSources[_nextSource];
        _nextSource = (_nextSource + 1) % _sfxSources.Length;

        src.clip = clip;
        src.volume = sfxVolume * volumeScale;
        // 같은 소리가 반복될 때 기계적으로 들리지 않도록 음정을 조금씩 흔든다
        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

        if (delay > 0f) src.PlayDelayed(delay);
        else src.Play();
    }

    // ── 외부에서 부르는 정적 편의 메서드 (인스턴스가 없어도 안전) ──
    public static void PlayerSwing() =>
        Instance?.PlayClip(Instance.playerSwing, 1f, 0.07f, Instance.swingDelay);
    public static void PlayerHit() => Instance?.PlayClip(Instance.playerHit, 1f, 0.06f);
    public static void PlayerHurt() => Instance?.PlayClip(Instance.playerHurt, 1f, 0.05f);
    public static void PlayerRoll() => Instance?.PlayClip(Instance.playerRoll, 0.8f, 0.08f);
    public static void PlayerDeath() => Instance?.PlayClip(Instance.playerDeath, 1f, 0f);

    public static void BossRoar() => Instance?.PlayClip(Instance.bossRoar, 1f, 0.03f);
    public static void BossSwing() => Instance?.PlayClip(Instance.bossSwing, 1f, 0.06f);
    public static void BossHurt() => Instance?.PlayClip(Instance.bossHurt, 0.7f, 0.08f);
    public static void BossDeath() => Instance?.PlayClip(Instance.bossDeath, 1f, 0f);

    public static void StopBgm()
    {
        if (Instance != null && Instance._bgmSource != null)
            Instance._bgmSource.Stop();
    }
}
