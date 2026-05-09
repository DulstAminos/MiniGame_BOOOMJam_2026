using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡事件管理器
/// 负责维护与世界状态有关的场景级逻辑
/// </summary>
public class LevelWorldManager : MonoBehaviour
{
    // 场景级单例
    public static LevelWorldManager Instance { get; private set; }

    [Header("关卡设置")]
    [Tooltip("关卡初始处于哪个世界")]
    public WorldType InitialWorld = WorldType.Front;

    // 当前主世界状态
    public WorldType CurrentActiveWorld { get; private set; }

    private void Awake()
    {
        // 场景级单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始化当前世界
        CurrentActiveWorld = InitialWorld;
    }

    private void Start()
    {
        // 触发一次初始化事件，让所有物体根据初始世界设置自身状态
        this.TriggerEvent(EventName.OnWorldSwitch, new WorldSwitchEventArgs(CurrentActiveWorld, CurrentActiveWorld));
    }

    /// <summary>
    /// 请求进行完全切换的公共接口
    /// </summary>
    public void RequestTotalSwitch()
    {
        WorldType oldWorld = CurrentActiveWorld;

        // 核心对称逻辑：直接翻转状态
        CurrentActiveWorld = (CurrentActiveWorld == WorldType.Front) ? WorldType.Back : WorldType.Front;

        Debug.Log($"完全切换发生：从 {oldWorld} 切换到 {CurrentActiveWorld}");

        // 触发世界切换事件
        this.TriggerEvent(EventName.OnWorldSwitch, new WorldSwitchEventArgs(oldWorld, CurrentActiveWorld));
    }

    // ============ 对象注册与通知 =============

    // 记录当前激活的所有部分切换区域
    private Dictionary<int, PartialZoneData> activePartialZones = new Dictionary<int, PartialZoneData>();
    private int nextZoneId = 0;

    // 存储当前关卡所有的 WorldObject
    private List<WorldObject> allWorldObjects = new List<WorldObject>();

    /// <summary>
    /// 物体在 Start 时注册自己
    /// </summary>
    public void RegisterWorldObject(WorldObject obj)
    {
        if (!allWorldObjects.Contains(obj)) allWorldObjects.Add(obj);
    }

    /// <summary>
    /// 物体在 OnDestroy 时注销自己
    /// </summary>
    public void UnregisterWorldObject(WorldObject obj)
    {
        allWorldObjects.Remove(obj);
    }


    /// <summary>
    /// 局部通知核心逻辑：仅唤醒圆圈内的物体进行检测
    /// </summary>
    private void NotifyObjectsNearArea(Vector2 center, float radius)
    {
        float delta = 1f;  // 适当扩大通知范围
        float r = radius + delta;
        float sqrRadius = r * r;
        foreach (var obj in allWorldObjects)
        {
            // 使用平方距离计算，避免开方运算，提高性能
            float sqrDistance = (obj.CenterPosition - center).sqrMagnitude;
            if (sqrDistance <= sqrRadius)
            {
                obj.CheckAndApplyState();
            }
        }
    }

    // ============ 区域管理 =============

    /// <summary>
    /// 注册一个新的部分切换区域
    /// </summary>
    /// <returns>返回该区域的唯一ID，用于后续更新或移除</returns>
    public int RegisterPartialZone(ZoneType type, Vector2 center, float radius)
    {
        int id = nextZoneId++;
        activePartialZones.Add(id, new PartialZoneData(id, type, center, radius));

        // 区域生成时，只通知区域附近的物体
        NotifyObjectsNearArea(center, radius);
        return id;
    }

    /// <summary>
    /// 更新已存在区域的位置和大小
    /// </summary>
    public void UpdatePartialZone(int id, Vector2 newCenter, float newRadius)
    {
        if (activePartialZones.TryGetValue(id, out PartialZoneData zone))
        {
            NotifyObjectsNearArea(zone.Center, zone.Radius); // 通知旧位置

            zone.Center = newCenter;
            zone.Radius = newRadius;

            NotifyObjectsNearArea(newCenter, newRadius); // 通知新位置
        }
    }

    /// <summary>
    /// 移除一个部分切换区域
    /// </summary>
    public void RemovePartialZone(int id)
    {
        if (activePartialZones.TryGetValue(id, out PartialZoneData zone))
        {
            activePartialZones.Remove(id);

            // 区域移除时，通知原区域内的物体恢复状态
            NotifyObjectsNearArea(zone.Center, zone.Radius);
        }
    }

    // ============ 核心查询算法 =============

    /// <summary>
    /// 【物理层专用】获取该位置理论上应该呈现的物理世界。
    /// 独有物体 (WorldObject) 用这个来决定是否开启物理。
    /// </summary>
    public WorldType GetPhysicalExpectedWorldAt(Vector2 position)
    {
        foreach (var zone in activePartialZones.Values)
        {
            // 物理层只受 AllSwitch 影响
            if (zone.Type == ZoneType.AllSwitch && (position - zone.Center).sqrMagnitude <= zone.Radius * zone.Radius)
            {
                return CurrentActiveWorld == WorldType.Front ? WorldType.Back : WorldType.Front;
            }
        }
        return CurrentActiveWorld;
    }

    /// <summary>
    /// 【视觉层专用】获取物体当前所在位置的最高优先级区域类型。
    /// 优先级：AllSwitch > PreviewOnly > None(返回null)
    /// </summary>
    public ZoneType? GetHighestPriorityZoneAt(Vector2 position)
    {
        ZoneType? highestZone = null;

        foreach (var zone in activePartialZones.Values)
        {
            float sqrDistance = (position - zone.Center).sqrMagnitude;
            if (sqrDistance <= zone.Radius * zone.Radius)
            {
                // 如果是替换区，优先级最高，直接返回
                if (zone.Type == ZoneType.AllSwitch) return ZoneType.AllSwitch;
                // 不是替换区，只能是预览区，先记录下来，继续找找看有没有物理区覆盖它
                highestZone = ZoneType.PreviewOnly;

            }
        }
        return highestZone;
    }
}
