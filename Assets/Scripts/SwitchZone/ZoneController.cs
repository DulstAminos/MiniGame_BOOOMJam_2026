using UnityEngine;

/// <summary>
/// 区域控制器，挂载在具有 Sprite Mask 的区域实体上。
/// 负责与 LevelWorldManager 通信，报告自己的位置和大小。
/// </summary>
[RequireComponent(typeof(SpriteMask))]
public class ZoneController : MonoBehaviour
{
    [Header("区域设置")]
    [Tooltip("区域类型：物理切换 还是 仅视觉预览")]
    public ZoneType Type = ZoneType.PreviewOnly;

    [Tooltip("区域的有效作用半径（需与 SpriteMask 的大小匹配）")]
    public float Radius = 3f;

    // 记录在管理器中的唯一 ID
    private int zoneID = -1;
    private Vector2 lastPosition;

    private void Start()
    {
        // 注册到管理器，获取 ID
        if (LevelWorldManager.Instance != null)
        {
            zoneID = LevelWorldManager.Instance.RegisterPartialZone(Type, transform.position, Radius);
            lastPosition = transform.position;
        }
    }

    private void Update()
    {
        // 只有位置发生变化时，才通知管理器更新（节省性能）
        if (Vector2.SqrMagnitude((Vector2)transform.position - lastPosition) > 0.001f)
        {
            if (LevelWorldManager.Instance != null && zoneID != -1)
            {
                LevelWorldManager.Instance.UpdatePartialZone(zoneID, transform.position, Radius);
                lastPosition = transform.position;
            }
        }
    }

    private void OnDestroy()
    {
        // 销毁时从管理器中注销
        if (LevelWorldManager.Instance != null && zoneID != -1)
        {
            LevelWorldManager.Instance.RemovePartialZone(zoneID);
        }
    }

#if UNITY_EDITOR
    // 在 Editor 中画出半径辅助线，方便调节
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Type == ZoneType.AllSwitch ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
#endif
}
