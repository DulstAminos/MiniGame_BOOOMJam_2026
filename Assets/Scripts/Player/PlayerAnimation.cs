using System;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;
    private PlayerController playerCtrl;

    // 用于记录和保持玩家的朝向，默认设为 -1 (面向左)
    private float faceX = -1f;

    void Awake()
    {
        playerCtrl = GetComponent<PlayerController>();
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError("未能在子物体找到Animator组件！");
            return;
        }

        // 初始化默认朝向
        anim.SetFloat("FaceX", faceX);
    }

    // 启用时注册事件
    void OnEnable()
    {
        EventManager.Instance.AddListener(EventName.OnPlayerThrow, OnTriggerThrowAnimation);
    }

    // 禁用时注销事件（防止内存泄漏或空引用）
    void OnDisable()
    {
        // 确保游戏退出时 EventManager 还没被销毁
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(EventName.OnPlayerThrow, OnTriggerThrowAnimation);
        }
    }

    void Update()
    {
        if (anim == null || playerCtrl == null) return;

        // 1. 获取输入状态
        float moveX = playerCtrl.MoveInput.x;
        bool isGrounded = playerCtrl.IsGrounded;
        bool isMoving = Mathf.Abs(moveX) > 0.01f;

        // 2. 更新方向稳定性逻辑
        if (moveX > 0)
        {
            faceX = 1f; // 右
        }
        else if (moveX < 0)
        {
            faceX = -1f; // 左
        }

        // 3. 传递给 Animator 的混合树
        anim.SetFloat("FaceX", faceX);
        anim.SetBool("IsMoving", isMoving);
        anim.SetBool("IsGrounded", isGrounded);
    }

    private void OnTriggerThrowAnimation(object sender, EventArgs e)
    {
        if (anim != null)
        {
            anim.SetTrigger("Throw");
        }
    }
}
