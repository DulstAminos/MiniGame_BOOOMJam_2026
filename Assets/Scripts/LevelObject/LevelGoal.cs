using System.Collections;
using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    [Header("通关设置")]
    public float winDelay = 0.5f; // 碰到终点后延迟多久进入下一关

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 防止重复触发，且必须是玩家
        if (isTriggered || !other.CompareTag("Player")) return;

        // 必须在游玩状态下才能触发通关（防止转场时误触）
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        isTriggered = true;
        StartCoroutine(WinRoutine());
    }

    private IEnumerator WinRoutine()
    {
        // 1. 拦截输入，防止玩家在通关延迟期间死亡或按R键
        GameManager.Instance.ChangeState(GameState.Transitioning);

        // 2. 解锁下一关进度
        DataManager.Instance.UnlockNextLevel();
        Debug.Log("关卡已通关，进度已保存！");

        // 3. 延迟停顿（可以用来播放欢呼音效或通关粒子）
        yield return new WaitForSeconds(winDelay);

        // 4. 判断并加载下一关
        int nextIndex = DataManager.Instance.CurrentPlayingIndex + 1;
        if (nextIndex < DataManager.Instance.GetTotalLevelCount())
        {
            SceneFlowManager.Instance.LoadLevel(nextIndex);
        }
        else
        {
            // 如果是最后一关，通关后返回主菜单（或者播放通关字幕）
            Debug.Log("全部关卡已通关！");
            SceneFlowManager.Instance.LoadMainMenu();
        }
    }
}
