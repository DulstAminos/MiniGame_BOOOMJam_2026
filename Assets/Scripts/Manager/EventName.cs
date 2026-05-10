using System;

/// <summary>
/// 储存事件名称
/// </summary>
public static class EventName
{
    public const string OnWorldSwitch = nameof(OnWorldSwitch);
    public const string OnItemCountChanged = nameof(OnItemCountChanged);       // 道具数量改变时触发
    public const string OnCurrentItemChanged = nameof(OnCurrentItemChanged);   // 当前手持道具切换时触发
}
