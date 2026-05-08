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

    [Tooltip("视觉节点上的渲染器（SpriteRenderer 或 TilemapRenderer）")]
    public Renderer VisualRenderer;

    // 获取物体的判定中心点（通常就是 Transform 位置，如果物体中心有偏移可以在这里修改）
    public Vector2 CenterPosition => transform.position;

    protected virtual void Start()
    {
        // 自动获取 Renderer
        if (VisualNode != null && VisualRenderer == null)
        {
            VisualRenderer = VisualNode.GetComponent<Renderer>();
        }

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

    protected virtual void OnDestroy()
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
    public virtual void CheckAndApplyState()
    {
        // 询问管理器：基于物体现在的位置，其视觉上应呈现哪个世界的状态
        WorldType expectedWorldVisual = LevelWorldManager.Instance.GetVisualExpectedWorldAt(CenterPosition);
        // 询问管理器：基于物体现在的位置，其物理上处于哪个世界
        WorldType expectedWorldPhysical = LevelWorldManager.Instance.GetPhysicalExpectedWorldAt(CenterPosition);

        // 应用物理状态
        ApplyPhysicsState(SourceWorld == expectedWorldPhysical);

        // 应用视觉状态
        ApplyVisualState(SourceWorld == expectedWorldVisual);
    }

    /// <summary>
    /// 处理物理层的显隐
    /// </summary>
    protected virtual void ApplyPhysicsState(bool isActive)
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
    /// 核心视觉表现逻辑
    /// </summary>
    protected virtual void ApplyVisualState(bool isActive)
    {
        if (VisualRenderer == null) return;

        // 获取当前主世界
        WorldType activeWorld = LevelWorldManager.Instance.CurrentActiveWorld;

        // 获取物体当前所在的最高优先级区域类型
        ZoneType? zoneIn = LevelWorldManager.Instance.GetHighestPriorityZoneAt(CenterPosition);

        // === 核心对称渲染逻辑 ===
        if (SourceWorld == activeWorld)
        {
            // 【情况 A】当前主世界的物体
            // 只在遮罩外部可见 (VisibleOutsideMask)

            VisualRenderer.gameObject.SetActive(true);

            // 如果被替换区覆盖，需要被遮罩裁掉；预览区则只是透视，不覆盖。
            if (zoneIn == ZoneType.AllSwitch)
            {
                SetMaskInteraction(VisualRenderer, SpriteMaskInteraction.VisibleOutsideMask);
            }
            else
            {
                SetMaskInteraction(VisualRenderer, SpriteMaskInteraction.None); // 正常显示
            }

            SetAlpha(VisualRenderer, zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f); // 在预览区则半透明
        }
        else
        {
            // 【情况 B】另一个世界（隐藏世界）的物体
            // 只在遮罩内部可见 (VisibleInsideMask)

            // 开启物体，但用遮罩隐藏它
            VisualRenderer.gameObject.SetActive(true);
            SetMaskInteraction(VisualRenderer, SpriteMaskInteraction.VisibleInsideMask);

            // 如果处在预览区，半透明 (0.5f)；如果处在替换区，全实体 (1.0f)；都不在则设为完全透明 (0f) 
            if (zoneIn == ZoneType.PreviewOnly)
                SetAlpha(VisualRenderer, 0.5f);
            else if (zoneIn == ZoneType.AllSwitch)
                SetAlpha(VisualRenderer, 1.0f);
            else
                SetAlpha(VisualRenderer, 0.0f);
        }
    }

    // 辅助方法：兼容 SpriteRenderer 和 TilemapRenderer 设置遮罩
    private void SetMaskInteraction(Renderer r, SpriteMaskInteraction interaction)
    {
        if (r is SpriteRenderer sr) sr.maskInteraction = interaction;
        else if (r is UnityEngine.Tilemaps.TilemapRenderer tr) tr.maskInteraction = interaction;
    }

    // 辅助方法：设置透明度
    private void SetAlpha(Renderer r, float alpha)
    {
        if (r is SpriteRenderer sr)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
        else if (r is UnityEngine.Tilemaps.TilemapRenderer tr)
        {
            var tilemap = tr.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap != null)
            {
                Color c = tilemap.color;
                c.a = alpha;
                tilemap.color = c;
            }
        }
    }
}
