using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AimController), typeof(InventoryManager))]
public class ThrowController : MonoBehaviour
{
    [Header("蓄力设置 (部分切换区域)")]
    public float minSize = 1f;       // 最小生成尺寸
    public float maxSize = 5f;       // 最大生成尺寸
    public float chargeSpeed = 4f;   // 蓄力速度 (每秒增加的尺寸)

    [Header("固定设置 (传送门)")]
    public float fixedPortalSize = 3f; // 传送门的固定逻辑尺寸

    [Header("投掷实体设置")]
    public ItemProjectile projectilePrefab; // 飞行实体
    public Transform throwPoint;            // 投掷起点

    // 单位尺寸对应的预览圈直径
    private float previewScaleMultiplier = 2f;

    private AimController aimController;
    private InventoryManager inventory;
    private PlayerInput controls;

    // 状态机变量
    private bool isCharging = false;
    private float currentChargeSize;

    private void Awake()
    {
        aimController = GetComponent<AimController>();
        inventory = GetComponent<InventoryManager>();
        controls = new PlayerInput();

        // 绑定输入系统的事件
        // started = 按下 ; canceled = 松开
        controls.Player.UseItem.started += OnUseItemStarted;
        controls.Player.UseItem.canceled += OnUseItemReleased;
        controls.Player.Cancel.started += OnCancelPressed;
    }

    private void OnEnable()
    {
        controls.Enable();
        // 监听道具切换事件：如果蓄力途中切换了，强制打断蓄力
        EventManager.Instance.AddListener(EventName.OnCurrentItemChanged, OnItemChangedInterrupt);
    }

    private void OnDisable()
    {
        controls.Disable();
        EventManager.Instance.RemoveListener(EventName.OnCurrentItemChanged, OnItemChangedInterrupt);
    }

    private void Update()
    {
        // 只有拿着部分切换道具并且正在蓄力时，才执行尺寸增加逻辑
        if (isCharging && inventory.CurrentItem == ItemType.PartialZone)
        {
            currentChargeSize += chargeSpeed * Time.deltaTime;
            currentChargeSize = Mathf.Clamp(currentChargeSize, minSize, maxSize);

            // 视觉反馈：动态预览圈，用于预览将要生成的区域大小
            if (aimController.previewCircle != null)
            {
                // 尺寸计算：Scale = Size * 乘数
                float scaleValue = currentChargeSize * previewScaleMultiplier;
                aimController.previewCircle.localScale = new Vector3(scaleValue, scaleValue, 1f);
            }
        }
    }

    /// <summary>
    /// 鼠标左键按下
    /// </summary>
    private void OnUseItemStarted(InputAction.CallbackContext ctx)
    {
        if (inventory.CurrentItem == ItemType.None) return;

        if (inventory.CurrentItem == ItemType.PartialZone)
        {
            // 道具1：按下左键开始蓄力
            isCharging = true;
            currentChargeSize = minSize;

            // 开始蓄力时，显示预览圈
            if (aimController.previewCircle != null)
            {
                aimController.previewCircle.gameObject.SetActive(true);
                float initialScale = currentChargeSize * previewScaleMultiplier;
                aimController.previewCircle.localScale = new Vector3(initialScale, initialScale, 1f);
            }
        }
        else if (inventory.CurrentItem == ItemType.Portal)
        {
            // 道具2：传送门固定大小，点击左键直接投掷
            ExecuteThrow(ItemType.Portal, fixedPortalSize);
        }
    }

    /// <summary>
    /// 鼠标左键松开
    /// </summary>
    private void OnUseItemReleased(InputAction.CallbackContext ctx)
    {
        // 只有蓄力状态下松开左键，才触发部分切换道具的投掷
        if (isCharging && inventory.CurrentItem == ItemType.PartialZone)
        {
            ExecuteThrow(ItemType.PartialZone, currentChargeSize);
        }
    }

    /// <summary>
    /// 鼠标右键按下 (取消逻辑)
    /// </summary>
    private void OnCancelPressed(InputAction.CallbackContext ctx)
    {
        if (isCharging)
        {
            // 如果正在蓄力，右键仅取消蓄力，不收回道具
            InterruptCharge();
            Debug.Log("已取消蓄力！");
        }
        else if (inventory.CurrentItem != ItemType.None)
        {
            // 如果只是在预瞄（没蓄力），右键直接取消装备状态（切回空手）
            inventory.SetCurrentItem(ItemType.None);
        }
    }

    /// <summary>
    /// 道具切换事件回调 (用于异常打断)
    /// </summary>
    private void OnItemChangedInterrupt(object sender, EventArgs e)
    {
        if (isCharging) InterruptCharge();
    }

    /// <summary>
    /// 中断并重置蓄力状态
    /// </summary>
    private void InterruptCharge()
    {
        isCharging = false;
        currentChargeSize = minSize;
        // 隐藏 previewCircle
        if (aimController.previewCircle != null)
        {
            aimController.previewCircle.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 执行投掷核心逻辑
    /// </summary>
    private void ExecuteThrow(ItemType itemType, float finalSize)
    {
        //  二次确认道具数量充足
        if (inventory.GetItemCount(itemType) <= 0) return;

        // 消耗库存
        inventory.ModifyItemCount(itemType, -1);

        // 获取目标点
        Vector2 targetPos = aimController.CurrentTargetPos;

        // --- 修改点：实例化飞行物并调用 Fire ---
        if (projectilePrefab != null)
        {
            Vector2 startPos = throwPoint != null ? (Vector2)throwPoint.position : (Vector2)transform.position;
            ItemProjectile proj = Instantiate(projectilePrefab, startPos, Quaternion.identity);
            proj.Fire(itemType, targetPos, finalSize);
        }
        else
        {
            Debug.LogError("未绑定 Projectile Prefab！");
        }

        // 重置状态机，准备下一次使用
        InterruptCharge();
    }
}
