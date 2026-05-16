using UnityEngine;

public class ZoneMovementFollow : ZoneMovementBase
{
    [Tooltip("要跟随的目标（玩家）")]
    public Transform Target;

    [Tooltip("跟随偏移量")]
    public Vector3 Offset = Vector3.zero;

    private void Start()
    {
        if (Target == null)
        {
            // 自动寻找并绑定玩家，玩家需有"Player"Tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Target = player.transform;
            }
        }
    }

    protected override void HandleMovement()
    {
        if (Target != null)
        {
            transform.position = Target.position + Offset;
        }
    }
}
