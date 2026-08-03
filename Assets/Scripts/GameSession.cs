using UnityEngine;

/// <summary>
/// 씬을 넘나들며 유지되는 플레이 기록. 씬을 새로 로드해도 값이 남도록 static으로 둔다.
/// </summary>
public static class GameSession
{
    /// <summary>아레나에 들어온 시각 (Time.timeSinceLevelLoad 기준이 아니라 절대 시각)</summary>
    static float _runStartTime;
    static bool _running;

    /// <summary>마지막으로 클리어한 시간(초). 아직 클리어 전이면 0.</summary>
    public static float ClearTimeSeconds { get; private set; }

    public static void StartRun()
    {
        _runStartTime = Time.time;
        _running = true;
    }

    public static void FinishRun()
    {
        if (!_running) return;
        ClearTimeSeconds = Time.time - _runStartTime;
        _running = false;
    }

    /// <summary>경과 시간(진행 중이면 현재까지, 끝났으면 최종 기록)</summary>
    public static float CurrentElapsed =>
        _running ? Time.time - _runStartTime : ClearTimeSeconds;

    /// <summary>"12:34.56" 형식으로 변환</summary>
    public static string Format(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return $"{minutes:00}:{rest:00.00}";
    }
}
