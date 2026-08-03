using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 소울라이크 게임 루프: 사망 → "YOU DIED" → 리스폰(보스 리셋) / 보스 처치 → "VICTORY".
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] Health playerHealth;
    [SerializeField] BossAI boss;
    [SerializeField] Health bossHealth;
    [SerializeField] GameEndScreen endScreen;

    [Header("타이밍")]
    [SerializeField] float deathTextDelay = 1.2f;   // 쓰러지는 모션을 보여주는 시간
    [SerializeField] float fadeDuration = 0.8f;
    [SerializeField] float victoryWatchTime = 2.2f; // 보스가 쓰러지는 걸 지켜보는 시간

    [Header("결산 씬")]
    [SerializeField] string victorySceneName = "Victory";
    [SerializeField] string defeatSceneName = "Defeat";

    bool _sequenceRunning;

    void Start()
    {
        if (playerHealth == null || boss == null || bossHealth == null)
        {
            Debug.LogError("GameManager 참조가 비었습니다. Tools > Setup Game Loop을 실행하세요.", this);
            enabled = false;
            return;
        }

        playerHealth.OnDied += HandlePlayerDied;
        bossHealth.OnDied += HandleBossDied;

        GameSession.StartRun(); // 클리어 타임 측정 시작
    }

    void HandlePlayerDied()
    {
        if (_sequenceRunning) return;
        StartCoroutine(DeathSequence());
    }

    void HandleBossDied()
    {
        if (_sequenceRunning) return;
        StartCoroutine(VictorySequence());
    }

    IEnumerator DeathSequence()
    {
        _sequenceRunning = true;

        // 쓰러지는 모션을 잠깐 보여준 뒤 암전 → 패배 결산 씬.
        // "YOU DIED" 문구는 결산 씬에서 크게 띄우므로 여기서는 중복해서 띄우지 않는다.
        yield return new WaitForSeconds(deathTextDelay);
        if (endScreen != null) yield return endScreen.FadeToBlack(fadeDuration);

        SceneManager.LoadScene(defeatSceneName);
    }

    IEnumerator VictorySequence()
    {
        _sequenceRunning = true;
        GameSession.FinishRun(); // 보스가 쓰러진 순간을 기록

        // 쓰러지는 모션을 지켜본 뒤 암전 → 결산 씬
        yield return new WaitForSeconds(victoryWatchTime);
        if (endScreen != null) yield return endScreen.FadeToBlack(fadeDuration);

        SceneManager.LoadScene(victorySceneName);
    }

}
