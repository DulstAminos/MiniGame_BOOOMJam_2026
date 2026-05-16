using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAudioBridge : MonoBehaviour
{
    [Header("走动音效设置")]
    public AudioClip walkClip;
    public float walkStepInterval = 0.3f; // 脚步声间隔时间

    private PlayerController player;
    private bool wasGrounded;
    private float walkTimer;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        // 1. 落地音效检测
        if (!wasGrounded && player.IsGrounded)
        {
            // 从空中变为落地瞬间，触发落地音效
            this.TriggerEvent(EventName.OnPlayerLand);
        }
        wasGrounded = player.IsGrounded;

        // 2. 走动音效检测
        if (player.IsGrounded && Mathf.Abs(player.MoveInput.x) > 0.1f)
        {
            walkTimer -= Time.deltaTime;
            if (walkTimer <= 0f)
            {
                if (AudioManager.Instance != null && walkClip != null)
                {
                    AudioManager.Instance.PlaySFX(walkClip, 0.5f); // 脚步声通常音量小点
                }
                walkTimer = walkStepInterval;
            }
        }
        else
        {
            walkTimer = 0f; // 停止移动时重置计时器，保证下次一动就有声音
        }
    }
}
