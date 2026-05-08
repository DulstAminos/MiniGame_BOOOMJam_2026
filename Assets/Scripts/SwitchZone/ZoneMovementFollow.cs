using UnityEngine;

public class ZoneMovementFollow : ZoneMovementBase
{
    [Tooltip("要跟随的目标（玩家）")]
    public Transform Target;

    [Tooltip("跟随偏移量")]
    public Vector3 Offset;

    protected override void HandleMovement()
    {
        if (Target != null)
        {
            transform.position = Target.position + Offset;
        }
    }
}
