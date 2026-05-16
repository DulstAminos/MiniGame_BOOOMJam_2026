using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAudioBridge : MonoBehaviour
{
    [Header("走动音效设置")]
    [Tooltip("将所有脚步声音效拖入此数组中")]
    public AudioClip[] walkClips;
    public float walkStepInterval = 0.3f; // 脚步声间隔时间
    [Range(0f, 1f)] public float walkVolume = 0.5f;

    private PlayerController player;
    private bool wasGrounded;
    private float walkTimer;
    private int lastWalkClipIndex = -1; // 记录上一次播放的脚步声索引

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
        if (player.IsGrounded && Mathf.Abs(player.MoveInput.x) > 0.01f)
        {
            walkTimer -= Time.deltaTime;
            if (walkTimer <= 0f)
            {
                PlayRandomFootstep(); // 调用随机播放逻辑
                walkTimer = walkStepInterval; // 重置倒计时
            }
        }
        else
        {
            // 如果玩家停下，或者跳在空中，立刻归零计时器。
            walkTimer = 0f;
        }
    }

    /// <summary>
    /// 从数组中随机选取一个脚步声播放，且保证不与上一次重复
    /// </summary>
    private void PlayRandomFootstep()
    {
        // 如果没有配置音效，或者全局管理器丢失，直接返回
        if (walkClips == null || walkClips.Length == 0 || AudioManager.Instance == null)
            return;

        // 如果只配置了1个音效，直接播放即可
        if (walkClips.Length == 1)
        {
            AudioManager.Instance.PlaySFX(walkClips[0], walkVolume);
            return;
        }

        // 随机获取一个索引
        int randomIndex = Random.Range(0, walkClips.Length);

        // 如果随机到的索引和上一次一样，就重新随机，直到不一样为止
        while (randomIndex == lastWalkClipIndex)
        {
            randomIndex = Random.Range(0, walkClips.Length);
        }

        // 记录这次播放的索引
        lastWalkClipIndex = randomIndex;

        // 播放选中的音效
        AudioManager.Instance.PlaySFX(walkClips[randomIndex], walkVolume);
    }
}
