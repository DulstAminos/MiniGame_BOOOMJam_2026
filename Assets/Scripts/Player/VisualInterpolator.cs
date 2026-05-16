using UnityEngine;

public class VisualInterpolator : MonoBehaviour
{
    private Transform parentTransform;
    private Vector3 previousTargetPosition;
    private Vector3 currentTargetPosition;
    private Vector3 initialLocalPosition;

    void Start()
    {
        parentTransform = transform.parent;
        if (parentTransform == null)
        {
            Debug.LogError("VisualInterpolator 必须挂载在角色的子物体上！");
            return;
        }

        // 记录子物体相对父物体的初始本地偏移（比如脚底偏移）
        initialLocalPosition = transform.localPosition;

        // 初始化位置
        previousTargetPosition = parentTransform.position;
        currentTargetPosition = parentTransform.position;
    }

    void FixedUpdate()
    {
        if (parentTransform == null) return;

        // 在物理帧推进时，更新记录的位置
        previousTargetPosition = currentTargetPosition;
        currentTargetPosition = parentTransform.position;
    }

    void Update()
    {
        if (parentTransform == null) return;

        // 1. 处理 TimeScale 为 0 的情况（如游戏暂停）
        if (Time.timeScale == 0f || Time.fixedDeltaTime <= 0f)
        {
            // 暂停时直接跟随父物体，不进行插值
            transform.position = parentTransform.position + parentTransform.TransformVector(initialLocalPosition);
            return;
        }

        // 2. 使用 double 计算高精度的插值比例 (Alpha)
        // Time.timeAsDouble: 当前帧的精确时间
        // Time.fixedTimeAsDouble: 上一次 FixedUpdate 发生的精确时间
        double alpha = (Time.timeAsDouble - Time.fixedTimeAsDouble) / (double)Time.fixedDeltaTime;

        // 将 alpha 限制在 0 到 1 之间。防止在极端掉帧时推断过度导致视觉穿模
        alpha = System.Math.Clamp(alpha, 0.0, 1.0);

        // 3. 计算插值后的根物体世界坐标
        Vector3 interpolatedRootPos = Vector3.Lerp(previousTargetPosition, currentTargetPosition, (float)alpha);

        // 4. 应用到视觉子物体上 (插值后的根物体位置 + 本地偏移量)
        transform.position = interpolatedRootPos + parentTransform.TransformVector(initialLocalPosition);
    }

    /// <summary>
    /// 当角色瞬间移动（如重生、传送）时调用此方法，防止出现跨越屏幕的残影拉扯
    /// </summary>
    public void ResetPosition()
    {
        if (parentTransform != null)
        {
            previousTargetPosition = parentTransform.position;
            currentTargetPosition = parentTransform.position;
            transform.position = parentTransform.position + parentTransform.TransformVector(initialLocalPosition);
        }
    }
}
