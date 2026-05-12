using UnityEngine;

public enum GameState
{
    MainMenu,       // 主菜单状态
    Playing,        // 游玩状态
    Paused,         // 暂停状态
    Transitioning   // 转场状态（无视一切输入）
}

public class GameManager : MonoSingleton<GameManager>
{
    public GameState CurrentState { get; private set; }

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
