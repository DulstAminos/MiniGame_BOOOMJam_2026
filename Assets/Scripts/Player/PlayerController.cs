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
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer; // 需要检测的地面Layer

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Input System 的消息回调
    // 对应 Action Map 里的 Move
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    // 对应 Action Map 里的 Jump
    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void Update()
    {
        // 落地检测
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    void FixedUpdate()
    {
        // 左右移动（直接修改速度）
        rb.velocity = new Vector2(moveInput.x * moveSpeed, rb.velocity.y);
    }

#if UNITY_EDITOR
    // 在编辑器里画出检测圆圈，方便调试
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
#endif
}
