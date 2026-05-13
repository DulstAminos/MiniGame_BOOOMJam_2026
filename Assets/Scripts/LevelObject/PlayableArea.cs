using UnityEngine;

public class PlayableArea : MonoBehaviour
{
    [Header("死亡设置")]
    public float deathDelay = 0.5f; // 离开区域后延迟多久重置

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 检查状态，防止通关后转场期间触发死亡
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            Debug.Log("玩家离开游玩区域，视为死亡！");

            // 防止连按或重复触发导致多次加载
            GameManager.Instance.ChangeState(GameState.Transitioning);

            // 调用在阶段一写好的统一重置方法
            SceneFlowManager.Instance.ReloadCurrentLevel(deathDelay);
        }
    }
}
