using UnityEngine;

using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,       // 主菜单状态
    Playing,        // 游玩状态
    Paused,         // 暂停状态
    Transitioning   // 转场状态（无视一切输入）
}

public class GameManager : MonoSingleton<GameManager>
{
    private const string MainMenuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (!ReferenceEquals(Instance, this)) return;

        // Directly starting a level scene bypasses the menu/transition flow,
        // so ensure gameplay scenes still enter the Playing state.
        if (CurrentState == default)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            ChangeState(string.Equals(activeSceneName, MainMenuSceneName) ? GameState.MainMenu : GameState.Playing);
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        if (newState == GameState.Paused)
            Time.timeScale = 0f;
        else if (newState == GameState.Playing)
            Time.timeScale = 1f;

        Debug.Log($"游戏状态切换为: {newState}");
    }
}
