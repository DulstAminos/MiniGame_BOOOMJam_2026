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
    [Tooltip("负责物理的子物体")]
    public GameObject PhysicsNode;

    [Tooltip("负责视觉的子物体")]
    public GameObject VisualNode;

    [Tooltip("视觉节点上的渲染器")]
    public Renderer VisualRenderer;

    // 获取物体的判定中心点
    public Vector2 CenterPosition => transform.position;

    protected virtual void Start()
    {
        // 自动获取 Renderer
        if (VisualNode != null && VisualRenderer == null)
            VisualRenderer = VisualNode.GetComponent<Renderer>();

        // 向管理器注册自身
        if (LevelWorldManager.Instance != null)
            LevelWorldManager.Instance.RegisterWorldObject(this);

        // 监听“完全切换”事件
        EventManager.Instance.AddListener(EventName.OnWorldSwitch, OnTotalWorldSwitched);

        // 初始化时进行一次强制状态检测
        CheckAndApplyState();
    }

    protected virtual void OnDestroy()
    {
        // 销毁时清理引用和事件，防止内存泄漏
        if (LevelWorldManager.Instance != null)
            LevelWorldManager.Instance.UnregisterWorldObject(this);

        EventManager.Instance.RemoveListener(EventName.OnWorldSwitch, OnTotalWorldSwitched);
    }

    /// <summary>
    /// 事件回调：当发生完全切换时，所有物体都必须检测状态
    /// </summary>
    private void OnTotalWorldSwitched(object sender, EventArgs e) => CheckAndApplyState();

    /// <summary>
    /// 核心检测逻辑：只在必要时（事件触发或被区域覆盖时）被调用
    /// </summary>
    public virtual void CheckAndApplyState()
    {
        // 独立处理物理状态
        WorldType expectedWorldPhysical = LevelWorldManager.Instance.GetPhysicalExpectedWorldAt(CenterPosition);
        ApplyPhysicsState(SourceWorld == expectedWorldPhysical);

        // 独立处理视觉状态 (无需传参，内部自行获取优先级)
        ApplyVisualState();
    }

    /// <summary>
    /// 处理物理层的显隐
    /// </summary>
    protected virtual void ApplyPhysicsState(bool isActive)
    {
        if (PhysicsNode != null && PhysicsNode.activeSelf != isActive)
        {
            // 通过启用/禁用物理子节点来控制物理表现
            PhysicsNode.SetActive(isActive);
        }
    }

    /// <summary>
    /// 核心视觉表现逻辑
    /// </summary>
    protected virtual void ApplyVisualState()
    {
        if (VisualRenderer == null) return;
        VisualRenderer.gameObject.SetActive(true);

        // 获取当前主世界
        WorldType activeWorld = LevelWorldManager.Instance.CurrentActiveWorld;

        // 获取物体当前所在的最高优先级区域类型
        ZoneType? zoneIn = LevelWorldManager.Instance.GetHighestPriorityZoneAt(CenterPosition);

        // === 核心对称渲染逻辑 ===
        if (SourceWorld == activeWorld)
        {
            // 【情况 A】当前主世界的物体

            // 默认不透明，在遮罩外显示
            float alpha = 1.0f;
            SpriteMaskInteraction mask = SpriteMaskInteraction.VisibleOutsideMask;
            // 在替换区则可在遮罩内显示，几乎透明
            if (zoneIn == ZoneType.AllSwitch)
            {
                alpha = 0.2f;
                mask = SpriteMaskInteraction.None;
            }

            SetRendererMaskAndAlpha(VisualRenderer, mask, alpha);
        }
        else
        {
            // 【情况 B】另一个世界（隐藏世界）的物体

            // 默认几乎透明，替换区不透明
            float alpha = 0.2f;
            if (zoneIn == ZoneType.AllSwitch) alpha = 1.0f;

            SetRendererMaskAndAlpha(VisualRenderer, SpriteMaskInteraction.VisibleInsideMask, alpha);
        }
    }

    /// <summary>
    /// 辅助方法：统一设置渲染器的遮罩与透明度（供自身与子类复用）
    /// </summary>
    /// <param name="alpha">为null则不改变透明度</param>
    protected void SetRendererMaskAndAlpha(Renderer r, SpriteMaskInteraction mask, float? alpha)
    {
        if (r == null) return;

        if (r is SpriteRenderer sr)
        {
            sr.maskInteraction = mask;
            Color c = sr.color; c.a = alpha ?? c.a; sr.color = c;
        }
        else if (r is UnityEngine.Tilemaps.TilemapRenderer tr)
        {
            tr.maskInteraction = mask;
            var tilemap = tr.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap != null)
            {
                Color c = tilemap.color; c.a = alpha ?? c.a; tilemap.color = c;
            }
        }
    }
}
