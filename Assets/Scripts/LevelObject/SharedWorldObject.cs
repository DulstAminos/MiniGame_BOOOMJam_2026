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
        if (PhysicsNode != null) PhysicsNode.SetActive(true);

        // 调用基类的 Start 以注册自己和监听事件
        base.Start();
    }

    /// <summary>
    /// 重写状态检测逻辑
    /// </summary>
    public override void CheckAndApplyState()
    {
        if (VisualNode_Front != null) VisualNode_Front.SetActive(true);
        if (VisualNode_Back != null) VisualNode_Back.SetActive(true);

        WorldType activeWorld = LevelWorldManager.Instance.CurrentActiveWorld;
        ZoneType? zoneIn = LevelWorldManager.Instance.GetHighestPriorityZoneAt(CenterPosition);

        // 利用三元运算确定谁在“外面”，谁在“里面”
        bool isFrontActive = (activeWorld == WorldType.Front);
        Renderer outsideRenderer = isFrontActive ? Renderer_Front : Renderer_Back;
        Renderer insideRenderer = isFrontActive ? Renderer_Back : Renderer_Front;

        // 通用透明度逻辑：如果在预览区，都是半透明；否则全实心
        float alpha = zoneIn == ZoneType.PreviewOnly ? 0.5f : 1.0f;

        // 设置主世界（外部）渲染器：如果被完全替换区覆盖，则被遮罩裁掉；否则正常显示
        SpriteMaskInteraction outMask = zoneIn == ZoneType.AllSwitch ? SpriteMaskInteraction.VisibleOutsideMask : SpriteMaskInteraction.None;
        SetRendererMaskAndAlpha(outsideRenderer, outMask, alpha);

        // 设置隐藏世界（内部）渲染器：永远只在遮罩内部显示
        SetRendererMaskAndAlpha(insideRenderer, SpriteMaskInteraction.VisibleInsideMask, alpha);
    }
}
