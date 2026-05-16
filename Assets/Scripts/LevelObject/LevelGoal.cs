using System.Collections;
using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    [Header("通关设置")]
    public float winDelay = 0.5f; // 碰到终点后延迟多久进入下一关

    private bool isTriggered = false;

    private void OnTriggerStay2D(Collider2D other)
    {
        CheckGoal(other);
    }

    private void CheckGoal(Collider2D other)
    {
        // 1. 基础条件检查
        if (isTriggered || !other.CompareTag("Player")) return;
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        // 2. 检查玩家是否在地面上
        PlayerController playerCtrl = other.GetComponent<PlayerController>();
        if (playerCtrl != null && !playerCtrl.IsGrounded)
        {
            return;
        }

        // 3. 符合条件，执行通关
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
