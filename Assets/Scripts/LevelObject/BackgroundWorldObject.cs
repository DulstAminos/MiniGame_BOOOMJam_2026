using UnityEngine;

/// <summary>
/// 背景物体类（继承自 WorldObject）
/// 特点：无物理表现，视觉层覆盖全屏并永远受 SpriteMask 影响，始终跟随主摄像机。
/// </summary>
public class BackgroundWorldObject : WorldObject
{
    [Header("背景设置")]
    [Tooltip("背景距离摄像机的 Z 轴深度（值越大越靠后）")]
    public float DepthZ = 10f;

    private Transform mainCameraTransform;

    protected override void Start()
    {
        // 自动获取主摄像机
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("场景中没有 Tag 为 MainCamera 的摄像机！背景跟随可能失效。");
        }

        // 背景不需要物理节点，强制关闭
        if (PhysicsNode != null)
        {
            PhysicsNode.SetActive(false);
        }

        base.Start(); // 注册自身
    }

    /// <summary>
    /// 使用 LateUpdate 确保在摄像机移动完之后，背景再移动，避免画面抖动
    /// </summary>
    private void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // 跟随摄像机的 X 和 Y，固定 Z 深度
            transform.position = new Vector3(mainCameraTransform.position.x, mainCameraTransform.position.y, DepthZ);
        }
    }

    /// <summary>
    /// 重写核心检测逻辑
    /// </summary>
    public override void CheckAndApplyState()
    {
        // 只处理视觉状态
        ApplyVisualState();
    }

    /// <summary>
    /// 重写核心视觉逻辑，抛弃基类的距离判断。
    /// </summary>
    protected override void ApplyVisualState()
    {
        if (VisualRenderer == null) return;
        VisualRenderer.gameObject.SetActive(true);

        WorldType activeWorld = LevelWorldManager.Instance.CurrentActiveWorld;

        if (SourceWorld == activeWorld)
        {
            // 当前主世界的背景：设定为永远在遮罩外显示
            SetRendererMaskAndAlpha(VisualRenderer, SpriteMaskInteraction.VisibleOutsideMask, null);
        }
        else
        {
            // 隐藏世界的背景：设定为永远在遮罩内显示
            SetRendererMaskAndAlpha(VisualRenderer, SpriteMaskInteraction.VisibleInsideMask, null);
        }
    }
}
