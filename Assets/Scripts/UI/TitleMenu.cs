using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>타이틀 화면. 시작하기 / 게임 설명 토글.</summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] string arenaSceneName = "Arena";
    [SerializeField] GameObject descriptionPanel;

    void Start()
    {
        // 아레나에서 잠갔던 커서를 반드시 풀어준다 (버튼을 눌러야 하므로)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f; // 히트스톱 도중 씬이 바뀌었을 경우 대비

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(arenaSceneName);
    }

    public void ToggleDescription()
    {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(!descriptionPanel.activeSelf);
    }
}
