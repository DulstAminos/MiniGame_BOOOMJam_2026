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
            // 尝试在玩家的子物体中寻找 VisualInterpolator 脚本
            VisualInterpolator visualLayer = player.GetComponentInChildren<VisualInterpolator>();

            if (visualLayer != null)
            {
                // 如果找到了视觉层，相机跟随视觉层
                vcam.Follow = visualLayer.transform;
                Debug.Log("相机已成功绑定到玩家的视觉插值层！");
            }
            else
            {
                // 后备方案：如果没有找到插值脚本，就降级跟随根物体
                vcam.Follow = player.transform;
                Debug.LogWarning("未找到 VisualInterpolator 子物体，相机改为跟随玩家根物体，可能会出现抖动。");
            }
        }
        else
        {
            Debug.LogError("场景中没有找到Tag为'Player'的物体！");
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
