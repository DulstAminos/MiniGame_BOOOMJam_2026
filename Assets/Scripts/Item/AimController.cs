using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimController : MonoBehaviour
{
    [Header("预瞄设置")]
    public float maxThrowRadius = 5f;

    [Header("视觉组件引用")]
    public GameObject rangeIndicator;
    public LineRenderer trajectoryLine;
    public Transform crosshair;
    public Transform previewCircle;

    private PlayerInput controls;
    private Camera mainCam;
    private bool isAimingActive = false; // 是否处于预瞄状态

    // 对外暴露的最终目标点
    public Vector2 CurrentTargetPos { get; private set; }

    private void Awake()
    {
        mainCam = Camera.main;
        controls = new PlayerInput();
        SetVisualsActive(false); // 默认关闭视觉
    }

    private void OnEnable()
    {
        controls.Enable();
        rangeIndicator.transform.localScale = Vector3.one * 2 * maxThrowRadius;
        // 注册事件监听：手持道具发生改变
        EventManager.Instance.AddListener(EventName.OnCurrentItemChanged, OnCurrentItemChangedHandler);
    }

    private void OnDisable()
    {
        controls.Disable();
        // 注销事件监听
        EventManager.Instance.RemoveListener(EventName.OnCurrentItemChanged, OnCurrentItemChangedHandler);
    }

    /// <summary>
    /// 事件回调：处理当前道具切换
    /// </summary>
    private void OnCurrentItemChangedHandler(object sender, EventArgs e)
    {
        if (e is CurrentItemArgs args)
        {
            // 只要不是空手，就激活预瞄状态
            isAimingActive = (args.NewItemType != ItemType.None);
            SetVisualsActive(isAimingActive);
        }
    }

    private void Update()
    {
        // 如果未激活预瞄（空手），直接跳过计算
        if (!isAimingActive) return;

        CalculateAim();
    }

    // 计算目标
    private void CalculateAim()
    {
        // 获取鼠标的世界坐标
        Vector2 mouseScreenPos = controls.Player.AimPosition.ReadValue<Vector2>();
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        // 计算方向和截断限制
        Vector2 playerPos = transform.position;
        Vector2 direction = mouseWorldPos - playerPos;
        float distance = direction.magnitude;

        if (distance > maxThrowRadius)
        {
            // 超出范围：截断在边界上
            CurrentTargetPos = playerPos + direction.normalized * maxThrowRadius;
        }
        else
        {
            // 在范围内：就在鼠标位置
            CurrentTargetPos = mouseWorldPos;
        }

        // 更新视觉UI
        UpdateVisuals(playerPos, CurrentTargetPos);
    }

    private void UpdateVisuals(Vector2 startPos, Vector2 targetPos)
    {
        // 更新准星位置
        crosshair.position = targetPos;

        // 更新虚线
        trajectoryLine.SetPosition(0, startPos);
        trajectoryLine.SetPosition(1, targetPos);

        // 如果预览圈处于激活状态，让它也紧跟准星
        if (previewCircle != null && previewCircle.gameObject.activeSelf)
        {
            previewCircle.position = targetPos;
        }
    }

    private void SetVisualsActive(bool isActive)
    {
        if (rangeIndicator.activeSelf != isActive) rangeIndicator.SetActive(isActive);
        if (trajectoryLine.enabled != isActive) trajectoryLine.enabled = isActive;
        if (crosshair.gameObject.activeSelf != isActive) crosshair.gameObject.SetActive(isActive);
    }
}
