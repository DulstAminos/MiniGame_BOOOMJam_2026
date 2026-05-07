using System;
using UnityEngine;

/// <summary>
/// 世界类型枚举
/// </summary>
public enum WorldType
{
    Front, // 表世界
    Back   // 里世界
}

/// <summary>
/// 传递世界切换数据的事件参数
/// </summary>
public class WorldSwitchEventArgs : EventArgs
{
    public WorldType OldWorld { get; private set; }
    public WorldType NewWorld { get; private set; }

    public WorldSwitchEventArgs(WorldType oldWorld, WorldType newWorld)
    {
        OldWorld = oldWorld;
        NewWorld = newWorld;
    }
}

/// <summary>
/// 部分切换区域的数据结构
/// </summary>
public class PartialZoneData
{
    public int ID { get; private set; }      // 区域唯一ID（方便移除）
    public Vector2 Center { get; set; }      // 区域中心点
    public float Radius { get; set; }        // 区域半径

    public PartialZoneData(int id, Vector2 center, float radius)
    {
        ID = id;
        Center = center;
        Radius = radius;
    }
}
