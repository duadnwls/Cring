using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "YOU DIED" / "VICTORY" 연출. 검은 배경이 서서히 깔리고 문구가 천천히 떠오른다.
/// </summary>
public class GameEndScreen : MonoBehaviour
{
    [SerializeField] Image backdrop;
    [SerializeField] Text messageText;

    static readonly Color DeathColor = new Color(0.62f, 0.09f, 0.06f);
    static readonly Color VictoryColor = new Color(0.85f, 0.78f, 0.45f);

    void Awake()
    {
        SetAlpha(0f);
    }

    public IEnumerator ShowYouDied(float hold, float fadeDuration)
    {
        messageText.text = "YOU DIED";
        messageText.color = DeathColor;
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(hold);
    }

    public IEnumerator ShowVictory(float fadeDuration)
    {
        messageText.text = "VICTORY";
        messageText.color = VictoryColor;
        yield return Fade(0f, 1f, fadeDuration);
    }

    public IEnumerator FadeOut(float fadeDuration)
    {
        yield return Fade(1f, 0f, fadeDuration);
    }

    /// <summary>결산 씬으로 넘어가기 전, 문구 없이 화면만 어둡게 덮는다.</summary>
    public IEnumerator FadeToBlack(float fadeDuration)
    {
        messageText.text = "";
        _backdropMax = 1f;
        yield return Fade(0f, 1f, fadeDuration);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    /// <summary>결산 씬 전환용 암전에서는 배경을 완전히 불투명하게 덮는다.</summary>
    float _backdropMax = 0.82f;

    void SetAlpha(float a)
    {
        if (backdrop != null)
        {
            var c = backdrop.color;
            backdrop.color = new Color(c.r, c.g, c.b, a * _backdropMax);
        }
        if (messageText != null)
        {
            var c = messageText.color;
            messageText.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
