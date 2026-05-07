using System;

/// <summary>
/// 储存事件名称
/// </summary>
public static class EventName
{
    public const string OnWorldSwitch = nameof(OnWorldSwitch);
    // 事件名称样例
    //public const string OnTest = nameof(OnTest);
}

// 事件参数样例
//public class OnTestEventArgs : EventArgs
//{
//    public string testString;
//}

// 事件处理器样例
//private void OnTestEventHandler(object sender, EventArgs e)
//{
//    var data = e as OnTestEventArgs;
//    if (data != null) Debug.Log(data.testString);
//}
