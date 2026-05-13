using UnityEngine;

public class ItemProjectile : MonoBehaviour
{
    [Header("飞行设置")]
    public float flySpeed = 20f;       // 飞行速度
    public LayerMask hitLayerMask;     // 碰到后会立刻生成的Layer

    [Header("生成的预制体引用")]
    public GameObject partialZonePrefab; // 部分切换区域预制体
    public GameObject portalPrefab;      // 完全切换传送门预制体

    // 内部状态
    private ItemType itemType;
    private Vector2 targetPos;
    private float finalSize;
    private bool isFired = false;
    private ThrowController ownerThrowController;

    /// <summary>
    /// 初始化并发射投掷物
    /// </summary>
    public void Fire(ItemType type, Vector2 target, float size, ThrowController owner)
    {
        itemType = type;
        targetPos = target;
        finalSize = size;
        ownerThrowController = owner;
        isFired = true;

        // 让投掷物朝向目标点飞行
        Vector2 dir = targetPos - (Vector2)transform.position;
        if (dir != Vector2.zero)
        {
            transform.right = dir.normalized;
        }
    }

    private void Update()
    {
        if (!isFired) return;

        Vector2 currentPos = transform.position;
        Vector2 dir = (targetPos - currentPos).normalized;
        float distanceToTarget = Vector2.Distance(currentPos, targetPos);

        // 这一帧即将移动的距离
        float stepLength = flySpeed * Time.deltaTime;

        // 射线检测：预判这一帧的移动轨迹是否会碰到特定 Layer
        // 射线长度取“步长”与“距离终点距离”的最小值，防止超出终点去检测
        float checkDistance = Mathf.Min(stepLength, distanceToTarget);
        RaycastHit2D hit = Physics2D.Raycast(currentPos, dir, checkDistance, hitLayerMask);

        if (hit.collider != null)
        {
            // 撞到了特定Layer的碰撞体，立刻在碰撞点生成
            SpawnZone(hit.point);
            return;
        }

        // 移动逻辑
        if (distanceToTarget <= stepLength)
        {
            // 没撞到障碍，且到达了指定的终点
            SpawnZone(targetPos);
        }
        else
        {
            // 继续平滑飞行
            transform.Translate(dir * stepLength, Space.World);
        }
    }

    /// <summary>
    /// 在指定坐标实例化最终的游戏实体
    /// </summary>
    private void SpawnZone(Vector2 spawnPosition)
    {
        // 判断生成区域
        GameObject prefab = null;
        if (itemType == ItemType.PartialZone && partialZonePrefab != null)
        {
            prefab = partialZonePrefab;
        }
        else if (itemType == ItemType.Portal && portalPrefab != null)
        {
            prefab = portalPrefab;
        }

        // 生成区域并设置半径
        if (prefab != null)
        {
            GameObject zoneObj = Instantiate(prefab, spawnPosition, Quaternion.identity);

            ZoneController controller = zoneObj.GetComponent<ZoneController>();
            if (controller != null)
            {
                controller.Radius = finalSize;
            }

            if (itemType == ItemType.PartialZone && ownerThrowController != null)
            {
                ownerThrowController.RegisterThrownPartialZone(zoneObj);
            }
        }

        // 区域生成完毕，销毁投掷物自身
        Destroy(gameObject);
    }
}
