using UnityEngine;

[RequireComponent(typeof(Collider2D))] // 强制要求挂载碰撞体
public class ItemPickup : MonoBehaviour
{
    [Header("拾取物设置")]
    [Tooltip("此拾取物对应的道具类型")]
    public ItemType itemTypeToGive = ItemType.PartialZone;

    [Tooltip("拾取后增加的数量")]
    public int giveAmount = 1;

    // 当有物体进入此触发器时自动调用
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 判断是否是标签为 "Player" 的物体碰到了它
        if (collision.CompareTag("Player"))
        {
            // 尝试获取玩家身上的 InventoryManager 组件
            InventoryManager inventory = collision.GetComponent<InventoryManager>();

            if (inventory != null)
            {
                // 增加道具数量
                inventory.ModifyItemCount(itemTypeToGive, giveAmount);

                Debug.Log($"<color=green>玩家拾取了 {giveAmount} 个 {itemTypeToGive}!</color>");

                // 拾取成功，销毁拾取物自身
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("碰到了Player标签的物体，但它身上没有InventoryManager脚本！");
            }
        }
    }
}
