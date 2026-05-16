using UnityEngine;

public class ZoneMovementSymmetric : ZoneMovementBase
{
    [Tooltip("玩家位置引用")]
    public Transform Player;
    [Tooltip("关卡中心点")]
    public Vector3 RoomCenter = Vector3.zero; // 房间的中心点，默认为原点

    private void Start()
    {
        if (Player == null)
        {
            // 自动寻找并绑定玩家，玩家需有"Player"Tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Player = player.transform;
            }
        }
    }

    protected override void HandleMovement()
    {
        if (Player != null && RoomCenter != null)
        {
            // 计算玩家相对于房间中心的向量
            Vector3 playerToCenter = RoomCenter - Player.position;
            // 对称点 = 房间中心 + 该向量
            transform.position = RoomCenter + playerToCenter;
        }
    }
}
