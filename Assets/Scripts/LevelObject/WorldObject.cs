using System;
using UnityEngine;

/// <summary>
/// 所有关卡物体的基类，负责处理表里世界切换逻辑
/// 层次要求：根节点挂载此脚本，下挂 PhysicsNode 和 VisualNode
/// </summary>
public class WorldObject : MonoBehaviour
{
    [Header("World Settings")]
    [Tooltip("该物体原生属于哪个世界")]
    public WorldType SourceWorld;

    [Header("Module References")]
    [Tooltip("负责物理的子物体（包含Collider2D, Rigidbody2D等）")]
    public GameObject PhysicsNode;

    [Tooltip("负责视觉的子物体（包含SpriteRenderer, Animator等）")]
    public GameObject VisualNode;

    // 获取物体的判定中心点（通常就是 Transform 位置，如果物体中心有偏移可以在这里修改）
    public Vector2 CenterPosition => transform.position;

    private void Start()
    {
        // 向管理器注册自身
        if (LevelWorldManager.Instance != null)
        {
            LevelWorldManager.Instance.RegisterWorldObject(this);
        }

        // 监听“完全切换”事件
        EventManager.Instance.AddListener(EventName.OnWorldSwitch, OnTotalWorldSwitched);

        // 初始化时进行一次强制状态检测
        CheckAndApplyState();
    }

    private void OnDestroy()
    {
        // 销毁时清理引用和事件，防止内存泄漏
        if (LevelWorldManager.Instance != null)
        {
            LevelWorldManager.Instance.UnregisterWorldObject(this);
        }
        EventManager.Instance.RemoveListener(EventName.OnWorldSwitch, OnTotalWorldSwitched);
    }

    /// <summary>
    /// 事件回调：当发生完全切换时，所有物体都必须检测状态
    /// </summary>
    private void OnTotalWorldSwitched(object sender, EventArgs e)
    {
        CheckAndApplyState();
    }

    /// <summary>
    /// 核心检测逻辑：只在必要时（事件触发或被区域覆盖时）被调用
    /// </summary>
    public void CheckAndApplyState()
    {
        // 询问管理器：基于物体现在的位置，其物理上处于哪个世界？
        WorldType expectedWorld = LevelWorldManager.Instance.GetExpectedWorldAt(CenterPosition);

        // 判断是否应该处于物理激活状态
        // 如果我原生所属的世界 == 我理论上该处的世界，我就该激活。
        bool shouldBePhysicallyActive = (SourceWorld == expectedWorld);

        // 应用物理状态
        ApplyPhysicsState(shouldBePhysicallyActive);

        // 应用视觉状态（目前预留，不阻碍后续 URP Stencil 的开发）
        // 在 URP Stencil 方案完成前，可以暂时用 SetActive 粗略控制显示，
        // 等 Stencil 写好后，这里可能只需要改变 Shader 的某些参数，或完全不需要操作
        ApplyVisualState(shouldBePhysicallyActive);
    }

    /// <summary>
    /// 处理物理层的显隐
    /// </summary>
    private void ApplyPhysicsState(bool isActive)
    {
        if (PhysicsNode != null)
        {
            // 通过启用/禁用物理子节点来控制物理表现
            if (PhysicsNode.activeSelf != isActive)
            {
                PhysicsNode.SetActive(isActive);
            }
        }
    }

    /// <summary>
    /// 处理视觉层的表现 (预留给后续的 URP 渲染层开发)
    /// </summary>
    private void ApplyVisualState(bool isActive)
    {
        if (VisualNode != null)
        {
            // 【占位逻辑】在 URP 渲染方案完成前，暂且和物理保持一致。
            // 未来这里可以改为：不禁用 GameObject，而是通知 VisualNode 里的脚本改变 Material 参数等。
            if (VisualNode.activeSelf != isActive)
            {
                VisualNode.SetActive(isActive);
            }
        }
    }
}
