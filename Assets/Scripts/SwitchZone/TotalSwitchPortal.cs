using UnityEngine;

/// <summary>
/// 完全切换道具生成的传送门区域
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TotalSwitchPortal : MonoBehaviour
{
    private void Start()
    {
        // 确保它是一个触发器
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果是玩家
        if (collision.CompareTag("Player"))
        {
            // 触发完全切换
            LevelWorldManager.Instance.RequestTotalSwitch();

            // 切换后，销毁传送门
            Destroy(gameObject);
        }
    }
}
