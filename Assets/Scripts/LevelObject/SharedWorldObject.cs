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

    [Tooltip("表世界视觉节点的 Renderer")]
    public Renderer Renderer_Front;
    [Tooltip("里世界视觉节点的 Renderer")]
    public Renderer Renderer_Back;

    protected override void Start()
    {
        // 自动获取 Renderer
        if (VisualNode_Front != null && Renderer_Front == null) Renderer_Front = VisualNode_Front.GetComponent<Renderer>();
        if (VisualNode_Back != null && Renderer_Back == null) Renderer_Back = VisualNode_Back.GetComponent<Renderer>();

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
        WorldType activeWorld = LevelWorldManager.Instance.CurrentActiveWorld;
        ZoneType? zoneIn = LevelWorldManager.Instance.GetHighestPriorityZoneAt(CenterPosition);

        // 无论如何，两个节点都要激活，交给 SpriteMask 去做像素级的裁剪
        if (VisualNode_Front != null) VisualNode_Front.SetActive(true);
        if (VisualNode_Back != null) VisualNode_Back.SetActive(true);

        if (activeWorld == WorldType.Front)
        {
            // 当前是表世界：表贴图在外面，里贴图在遮罩里
            SetupSharedNode(Renderer_Front, SpriteMaskInteraction.VisibleOutsideMask, zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f, zoneIn == ZoneType.AllSwitch);
            SetupSharedNode(Renderer_Back, SpriteMaskInteraction.VisibleInsideMask, zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f, true);
        }
        else
        {
            // 当前是里世界：对称反转
            SetupSharedNode(Renderer_Back, SpriteMaskInteraction.VisibleOutsideMask, zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f, zoneIn == ZoneType.AllSwitch);
            SetupSharedNode(Renderer_Front, SpriteMaskInteraction.VisibleInsideMask, zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f, true);
        }
    }

    private void SetupSharedNode(Renderer r, SpriteMaskInteraction maskMode, float alpha, bool applyMask)
    {
        if (r == null) return;

        // 如果不需要遮罩裁剪，则设为 None
        if (r is SpriteRenderer sr)
        {
            sr.maskInteraction = applyMask ? maskMode : SpriteMaskInteraction.None;
            Color c = sr.color; c.a = alpha; sr.color = c;
        }
        else if (r is UnityEngine.Tilemaps.TilemapRenderer tr)
        {
            tr.maskInteraction = applyMask ? maskMode : SpriteMaskInteraction.None;
            var tm = tr.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tm) { Color c = tm.color; c.a = alpha; tm.color = c; }
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
