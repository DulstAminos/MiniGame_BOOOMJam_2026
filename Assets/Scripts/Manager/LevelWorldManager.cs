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

    /// <summary>
    /// 注册一个新的部分切换区域
    /// </summary>
    /// <returns>返回该区域的唯一ID，用于后续更新或移除</returns>
    public int RegisterPartialZone(Vector2 center, float radius)
    {
        int id = nextZoneId++;
        activePartialZones.Add(id, new PartialZoneData(id, center, radius));
        return id;
    }

    /// <summary>
    /// 更新已存在区域的位置和大小
    /// </summary>
    public void UpdatePartialZone(int id, Vector2 newCenter, float newRadius)
    {
        if (activePartialZones.TryGetValue(id, out PartialZoneData zone))
        {
            zone.Center = newCenter;
            zone.Radius = newRadius;
        }
    }

    /// <summary>
    /// 移除一个部分切换区域
    /// </summary>
    public void RemovePartialZone(int id)
    {
        if (activePartialZones.ContainsKey(id))
        {
            activePartialZones.Remove(id);
        }
    }

    /// <summary>
    /// 【核心算法】根据空间位置，获取该位置理论上应该呈现的世界类型
    /// </summary>
    /// <param name="position">物体的中心点坐标</param>
    /// <returns>该位置当前属于表世界还是里世界</returns>
    public WorldType GetExpectedWorldAt(Vector2 position)
    {
        bool isInsideAnyZone = false;

        // 遍历所有的部分切换区域，检查点是否在区域内
        // （由于2D圆的判定非常快，直接算距离平方即可，避免开方消耗性能）
        foreach (var zone in activePartialZones.Values)
        {
            float sqrDistance = (position - zone.Center).sqrMagnitude;
            if (sqrDistance <= zone.Radius * zone.Radius)
            {
                isInsideAnyZone = true;
                break; // 只要在一个区域内，就被“替换”
            }
        }

        // 如果在圈内，则是“相反的世界”；如果在圈外，则是“当前的主世界”
        if (isInsideAnyZone)
        {
            return CurrentActiveWorld == WorldType.Front ? WorldType.Back : WorldType.Front;
        }
        else
        {
            return CurrentActiveWorld;
        }
    }
}
