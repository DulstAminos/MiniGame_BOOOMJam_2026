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

    [Header("Level Settings")]
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
        WorldSwitchEventArgs args = new WorldSwitchEventArgs(oldWorld, CurrentActiveWorld);
        this.TriggerEvent(EventName.OnWorldSwitch, args);
    }

    //============部分切换区域逻辑=============

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
        if (!allWorldObjects.Contains(obj))
        {
            allWorldObjects.Add(obj);
        }
    }

    /// <summary>
    /// 物体在 OnDestroy 时注销自己
    /// </summary>
    public void UnregisterWorldObject(WorldObject obj)
    {
        allWorldObjects.Remove(obj);
    }

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
            Vector2 oldCenter = zone.Center;
            float oldRadius = zone.Radius;

            zone.Center = newCenter;
            zone.Radius = newRadius;

            // 区域移动时，旧区域和新区域覆盖的物体都需要重新检测状态
            NotifyObjectsNearArea(oldCenter, oldRadius);
            NotifyObjectsNearArea(newCenter, newRadius);
        }
    }

    /// <summary>
    /// 移除一个部分切换区域
    /// </summary>
    public void RemovePartialZone(int id)
    {
        if (activePartialZones.TryGetValue(id, out PartialZoneData zone))
        {
            Vector2 oldCenter = zone.Center;
            float oldRadius = zone.Radius;
            activePartialZones.Remove(id);

            // 区域移除时，通知原区域内的物体恢复状态
            NotifyObjectsNearArea(oldCenter, oldRadius);
        }
    }

    /// <summary>
    /// 【物理层专用】只判定 AllSwitch 类型的区域。
    /// 独有物体 (WorldObject) 用这个来决定是否开启物理。
    /// </summary>
    public WorldType GetPhysicalExpectedWorldAt(Vector2 position)
    {
        return GetWorldAtByZoneType(position, ZoneType.AllSwitch);
    }

    /// <summary>
    /// 【视觉层专用】判定所有类型的区域（AllSwitch 和 PreviewOnly）。
    /// 共享物体 (SharedWorldObject) 和独有物体的视觉层用这个来决定长什么样。
    /// </summary>
    public WorldType GetVisualExpectedWorldAt(Vector2 position)
    {
        return GetWorldAtByZoneType(position, null);
    }

    /// <summary>
    /// 内部通用判定算法：根据空间位置，获取该位置理论上应该呈现的世界类型。
    /// 可指定检测的部分区域类型
    /// </summary>
    /// <param name="position">物体的中心点坐标</param>
    /// <param name="targetZoneType">需检测的部分区域类型，null表示不限制区域类型</param>
    /// <returns>该位置当前属于表世界还是里世界</returns>
    private WorldType GetWorldAtByZoneType(Vector2 position, ZoneType? targetZoneType)
    {
        bool isInsideZone = false;

        // 遍历所有的部分切换区域，检查点是否在区域内
        foreach (var zone in activePartialZones.Values)
        {
            // 如果指定了类型，且当前区域类型不符，则跳过
            if (targetZoneType.HasValue && zone.Type != targetZoneType.Value) continue;

            float sqrDistance = (position - zone.Center).sqrMagnitude;
            if (sqrDistance <= zone.Radius * zone.Radius)
            {
                isInsideZone = true;
                break;
            }
        }

        // 如果在圈内，则是“相反的世界”；如果在圈外，则是“当前的主世界”
        if (isInsideZone)
            return CurrentActiveWorld == WorldType.Front ? WorldType.Back : WorldType.Front;
        else
            return CurrentActiveWorld;
    }

    /// <summary>
    /// 局部通知核心逻辑：仅唤醒圆圈内的物体进行检测
    /// </summary>
    private void NotifyObjectsNearArea(Vector2 center, float radius)
    {
        float sqrRadius = radius * radius;
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
}
