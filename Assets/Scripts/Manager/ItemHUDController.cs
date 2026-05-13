using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemHUDController : MonoBehaviour
{
    [Header("道具图标引用")]
    public Image imgNone;
    public Image imgPartial;
    public Image imgPortal;

    [Header("道具数量文本")]
    public TMP_Text txtPartial;
    public TMP_Text txtPortal;

    [Header("选中框引用")]
    public GameObject frameNone;
    public GameObject framePartial;
    public GameObject framePortal;

    // 定义正常颜色和变暗的颜色
    private Color normalColor = Color.white;
    private Color darkColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private void Start()
    {
        // 从 InventoryManager 初始化当前 UI 状态
        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null)
        {
            UpdateItemCountUI(ItemType.PartialZone, inv.GetItemCount(ItemType.PartialZone));
            UpdateItemCountUI(ItemType.Portal, inv.GetItemCount(ItemType.Portal));
            UpdateSelectionFrame(inv.CurrentItem);
        }
    }

    private void OnEnable()
    {
        // 注册事件监听
        EventManager.Instance.AddListener(EventName.OnItemCountChanged, OnItemCountChanged);
        EventManager.Instance.AddListener(EventName.OnCurrentItemChanged, OnCurrentItemChanged);
    }

    private void OnDisable()
    {
        // 移除事件监听，防止内存泄漏
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener(EventName.OnItemCountChanged, OnItemCountChanged);
            EventManager.Instance.RemoveListener(EventName.OnCurrentItemChanged, OnCurrentItemChanged);
        }
    }

    // --- 事件回调处理 ---

    private void OnItemCountChanged(object sender, EventArgs args)
    {
        if (args is ItemCountArgs itemCountArgs)
        {
            UpdateItemCountUI(itemCountArgs.ItemType, itemCountArgs.NewCount);
        }
    }

    private void OnCurrentItemChanged(object sender, EventArgs args)
    {
        if (args is CurrentItemArgs currentItemArgs)
        {
            UpdateSelectionFrame(currentItemArgs.NewItemType);
        }
    }

    // --- UI 更新逻辑 ---

    private void UpdateItemCountUI(ItemType type, int count)
    {
        if (type == ItemType.PartialZone)
        {
            txtPartial.text = count.ToString();
            imgPartial.color = count > 0 ? normalColor : darkColor;
        }
        else if (type == ItemType.Portal)
        {
            txtPortal.text = count.ToString();
            imgPortal.color = count > 0 ? normalColor : darkColor;
        }
    }

    private void UpdateSelectionFrame(ItemType currentItem)
    {
        // 先把所有选中框关闭
        if (frameNone != null) frameNone.SetActive(false);
        if (framePartial != null) framePartial.SetActive(false);
        if (framePortal != null) framePortal.SetActive(false);

        // 根据当前选中的道具类型，激活对应的选中框
        switch (currentItem)
        {
            case ItemType.None:
                if (frameNone != null) frameNone.SetActive(true);
                break;
            case ItemType.PartialZone:
                if (framePartial != null) framePartial.SetActive(true);
                break;
            case ItemType.Portal:
                if (framePortal != null) framePortal.SetActive(true);
                break;
        }
    }
}
