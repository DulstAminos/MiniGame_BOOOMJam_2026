using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject pauseCanvasRoot; // PauseCanvas 本身
    public Button resumeBtn;
    public Button mainMenuBtn;
    public Button quitBtn;

    private void Start()
    {
        pauseCanvasRoot.SetActive(false);

        resumeBtn.onClick.AddListener(ResumeGame);
        mainMenuBtn.onClick.AddListener(ReturnToMainMenu);
        quitBtn.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        // 如果正在转场，忽略 ESC
        if (GameManager.Instance.CurrentState == GameState.Transitioning) return;
        // 如果在主菜单，不处理这里的暂停逻辑
        if (GameManager.Instance.CurrentState == GameState.MainMenu) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                Debug.Log("游戏暂停");
                PauseGame();
            }
            else if (GameManager.Instance.CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    private void PauseGame()
    {
        GameManager.Instance.ChangeState(GameState.Paused);
        pauseCanvasRoot.SetActive(true);
    }

    public void ResumeGame()
    {
        pauseCanvasRoot.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Playing);
    }

    private void ReturnToMainMenu()
    {
        // 恢复时间再加载场景，否则协程或动画可能会卡住
        Time.timeScale = 1f;
        SceneFlowManager.Instance.LoadMainMenu();
    }

    private void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }
}
