using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("物理检测")]
    [SerializeField] private Transform groundCheck; // 用于检测地面的空物体
    [SerializeField] private Vector2 checkCapsuleSize = new Vector2(0.6f, 0.2f);
    [SerializeField] private CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Horizontal;
    [SerializeField] private LayerMask groundLayer; // 需要检测的地面Layer

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;

    public Vector2 MoveInput => moveInput;
    public bool IsGrounded => isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Input System 的消息回调
    // 对应 Action Map 里的 Move
    public void OnMove(InputValue value)
    {
        if (GameplayInputBlocker.IsBlocked || GameManager.Instance.CurrentState != GameState.Playing)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = value.Get<Vector2>();
    }

    // 对应 Action Map 里的 Jump
    public void OnJump(InputValue value)
    {
        if (GameplayInputBlocker.IsBlocked || GameManager.Instance.CurrentState != GameState.Playing) return;

        if (value.isPressed && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    // 对应 Action Map 里的 Reset
    public void OnReset(InputValue value)
    {
        GameManager gameManager = GameManager.Instance;
        SceneFlowManager sceneFlowManager = SceneFlowManager.Instance;
        if (!value.isPressed || GameplayInputBlocker.IsBlocked || gameManager == null || sceneFlowManager == null) return;
        if (gameManager.CurrentState != GameState.Playing) return;

        gameManager.ChangeState(GameState.Transitioning);
        sceneFlowManager.ReloadCurrentLevel();
    }

    void Update()
    {
        // 落地检测
        isGrounded = Physics2D.OverlapCapsule(groundCheck.position, checkCapsuleSize, capsuleDirection, 0f, groundLayer);
    }

    void FixedUpdate()
    {
        if (GameplayInputBlocker.IsBlocked)
        {
            moveInput = Vector2.zero;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // 左右移动（直接修改速度）
        rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);
    }

#if UNITY_EDITOR
    // 在编辑器里画出胶囊体检测范围，方便调试
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            DrawWireCapsule2D(groundCheck.position, checkCapsuleSize, capsuleDirection);
        }
    }

    // 辅助方法：绘制 2D 胶囊体线框
    private void DrawWireCapsule2D(Vector2 center, Vector2 size, CapsuleDirection2D direction)
    {
        if (direction == CapsuleDirection2D.Horizontal)
        {
            float radius = size.y / 2f;
            float offset = Mathf.Max(0, (size.x - size.y) / 2f);

            Vector2 leftCenter = center + Vector2.left * offset;
            Vector2 rightCenter = center + Vector2.right * offset;

            Gizmos.DrawWireSphere(leftCenter, radius);
            Gizmos.DrawWireSphere(rightCenter, radius);
            Gizmos.DrawLine(leftCenter + Vector2.up * radius, rightCenter + Vector2.up * radius);
            Gizmos.DrawLine(leftCenter + Vector2.down * radius, rightCenter + Vector2.down * radius);
        }
        else // Vertical
        {
            float radius = size.x / 2f;
            float offset = Mathf.Max(0, (size.y - size.x) / 2f);

            Vector2 topCenter = center + Vector2.up * offset;
            Vector2 bottomCenter = center + Vector2.down * offset;

            Gizmos.DrawWireSphere(topCenter, radius);
            Gizmos.DrawWireSphere(bottomCenter, radius);
            Gizmos.DrawLine(topCenter + Vector2.left * radius, bottomCenter + Vector2.left * radius);
            Gizmos.DrawLine(topCenter + Vector2.right * radius, bottomCenter + Vector2.right * radius);
        }
    }
#endif
}
