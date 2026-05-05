using Cinemachine;
using UnityEngine;

// 虚拟相机自动绑定脚本
// 绑定内容：跟随对象（玩家），镜头边界
public class CameraFollowSync : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        // 自动寻找并绑定玩家，玩家需有"Player"Tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            vcam.Follow = player.transform;
        }

        // 自动寻找并绑定边界，边界需有"Bounds"Tag和Polygon Collider 2D
        GameObject bounds = GameObject.FindGameObjectWithTag("Bounds");
        if (bounds != null)
        {
            var confiner = GetComponent<CinemachineConfiner2D>();
            var collider = bounds.GetComponent<Collider2D>();
            if (confiner != null && collider != null)
            {
                confiner.m_BoundingShape2D = collider;
            }
        }
    }
}
