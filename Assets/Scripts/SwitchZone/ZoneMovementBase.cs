using UnityEngine;

/// <summary>
/// 移动策略基类
/// </summary>
public abstract class ZoneMovementBase : MonoBehaviour
{
    // 子类实现具体的移动逻辑
    protected abstract void HandleMovement();

    private void Update()
    {
        HandleMovement();
    }
}
