using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>승리/패배 결산 화면. 다시하기와 타이틀 이동을 담당한다.</summary>
public class ResultMenu : MonoBehaviour
{
    [SerializeField] string arenaSceneName = "Arena";
    [SerializeField] string titleSceneName = "Title";
    [SerializeField] Text clearTimeText;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; // 히트스톱 중에 씬이 바뀌었을 수 있다

        if (clearTimeText != null)
            clearTimeText.text = GameSession.Format(GameSession.ClearTimeSeconds);
    }

    public void Retry()
    {
        SceneManager.LoadScene(arenaSceneName);
    }

    public void GoToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
