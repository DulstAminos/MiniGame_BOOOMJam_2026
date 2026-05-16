using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [Header("道具库存")]
    [SerializeField] private int partialZoneCount = 5;
    [SerializeField] private int portalCount = 3;

    public ItemType CurrentItem { get; private set; } = ItemType.None;

    private PlayerInput controls;

    private void Awake()
    {
        controls = new PlayerInput();
        controls.Player.SwitchItem.performed += ctx => SwitchToNextItem();
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        // 游戏开始时触发一次初始化事件，让UI同步
        SetCurrentItem(ItemType.None);
    }

    /// <summary>
    /// 修改道具数量
    /// </summary>
    public void ModifyItemCount(ItemType type, int deltaAmount)
    {
        int newCount = 0;
        if (type == ItemType.PartialZone)
        {
            partialZoneCount = Mathf.Max(0, partialZoneCount + deltaAmount);
            newCount = partialZoneCount;
        }
        else if (type == ItemType.Portal)
        {
            portalCount = Mathf.Max(0, portalCount + deltaAmount);
            newCount = portalCount;
        }

        // 触发数量改变事件
        this.TriggerEvent(EventName.OnItemCountChanged, new ItemCountArgs { ItemType = type, NewCount = newCount });

        // 安全检查：如果当前手持的道具被扣完，强制切回空手
        if (CurrentItem == type && newCount <= 0)
        {
            SetCurrentItem(ItemType.None);
        }
    }

    /// <summary>
    /// 修改当前持有的道具
    /// </summary>
    public void SetCurrentItem(ItemType newType)
    {
        if (CurrentItem == newType) return; // 没变化则不触发

        CurrentItem = newType;

        // 触发当前手持道具改变事件
        this.TriggerEvent(EventName.OnCurrentItemChanged, new CurrentItemArgs { NewItemType = newType });
        Debug.Log($"当前切换为道具: {CurrentItem}");
    }

    /// <summary>
    /// 处理道具循环切换逻辑
    /// </summary>
    private void SwitchToNextItem()
    {
        if (GameplayInputBlocker.IsBlocked) return;

        if (CurrentItem == ItemType.None)
        {
            if (partialZoneCount > 0) SetCurrentItem(ItemType.PartialZone);
            else if (portalCount > 0) SetCurrentItem(ItemType.Portal);
        }
        else if (CurrentItem == ItemType.PartialZone)
        {
            if (portalCount > 0) SetCurrentItem(ItemType.Portal);
            else SetCurrentItem(ItemType.None);
        }
        else if (CurrentItem == ItemType.Portal)
        {
            SetCurrentItem(ItemType.None);
        }
    }

    // 提供给外部的查询接口
    public int GetItemCount(ItemType type)
    {
        if (type == ItemType.PartialZone) return partialZoneCount;
        if (type == ItemType.Portal) return portalCount;
        return 0;
    }
}

// 自定义事件参数
public class ItemCountArgs : EventArgs
{
    public ItemType ItemType { get; set; }
    public int NewCount { get; set; }
}

public class CurrentItemArgs : EventArgs
{
    public ItemType NewItemType { get; set; }
}
