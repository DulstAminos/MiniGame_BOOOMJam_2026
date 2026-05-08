using UnityEngine;

public class ZoneMovementSymmetric : ZoneMovementBase
{
    public Transform Player;
    public Transform RoomCenter; // 房间的中心点

    protected override void HandleMovement()
    {
        if (Player != null && RoomCenter != null)
        {
            // 计算玩家相对于房间中心的向量
            Vector3 playerToCenter = RoomCenter.position - Player.position;
            // 对称点 = 房间中心 + 该向量
            transform.position = RoomCenter.position + playerToCenter;
        }
    }
}
