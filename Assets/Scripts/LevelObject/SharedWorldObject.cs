using UnityEngine;

/// <summary>
/// 共有物体类（继承自 WorldObject）。
/// 特点：物理层永远激活，视觉层根据当前所在世界动态切换。
/// </summary>
public class SharedWorldObject : WorldObject
{
    [Header("Shared Object Specific")]
    [Tooltip("表世界的视觉节点")]
    public GameObject VisualNode_Front;

    [Tooltip("里世界的视觉节点")]
    public GameObject VisualNode_Back;

    protected override void Start()
    {
        // 对于共有物体，它的物理节点必须永远处于激活状态。
        // 所以在这里强制开启它，并脱离基类的物理控制逻辑。
        if (PhysicsNode != null)
        {
            PhysicsNode.SetActive(true);
        }

        // 调用基类的 Start 以注册自己和监听事件
        base.Start();
    }

    /// <summary>
    /// 重写状态检测逻辑
    /// </summary>
    public override void CheckAndApplyState()
    {
        // 共有物体的物理永远不变，所以不需要调用 ApplyPhysicsState()。

        // 只关心在当前位置，视觉上应该呈现哪个世界的样子
        WorldType expectedWorldVisual = LevelWorldManager.Instance.GetVisualExpectedWorldAt(CenterPosition);

        // 调用重写后的视觉处理方法
        ApplyVisualStateForShared(expectedWorldVisual);
    }

    /// <summary>
    /// 针对共有物体的视觉状态处理
    /// </summary>
    /// <param name="expectedWorld">该位置当前理论上的世界类型</param>
    private void ApplyVisualStateForShared(WorldType expectedWorld)
    {
        // 如果理论世界是表世界，激活 Front 节点，隐藏 Back 节点
        if (expectedWorld == WorldType.Front)
        {
            if (VisualNode_Front != null && !VisualNode_Front.activeSelf)
                VisualNode_Front.SetActive(true);

            if (VisualNode_Back != null && VisualNode_Back.activeSelf)
                VisualNode_Back.SetActive(false);
        }
        // 如果理论世界是里世界，激活 Back 节点，隐藏 Front 节点
        else
        {
            if (VisualNode_Front != null && VisualNode_Front.activeSelf)
                VisualNode_Front.SetActive(false);

            if (VisualNode_Back != null && !VisualNode_Back.activeSelf)
                VisualNode_Back.SetActive(true);
        }
    }

    // ==========屏蔽基类的无用方法==========

    protected override void ApplyPhysicsState(bool isActive)
    {
        // 留空：屏蔽基类的物理开关逻辑，保证共有物体的物理层永远开启
    }

    protected override void ApplyVisualState(bool isActive)
    {
        // 留空：屏蔽基类的单一视觉节点开关逻辑，改用上面的 ApplyVisualStateForShared
    }
}
