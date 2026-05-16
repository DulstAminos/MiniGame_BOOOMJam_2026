using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmFrontSource; // 用于主菜单 和 表世界 BGM
    [SerializeField] private AudioSource bgmBackSource;  // 仅用于 里世界 BGM
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayFrontBGM; // 表世界音乐
    public AudioClip gameplayBackBGM;  // 里世界音乐

    [Header("BGM Transition Settings")]
    [Tooltip("音乐淡入淡出所需的时间(秒)")]
    public float bgmFadeDuration = 0.5f;

    [Header("SFX Clips")]
    public AudioClip uiClickClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip throwClip;
    public AudioClip explodeClip_PartialZone;
    public AudioClip explodeClip_Portal;
    public AudioClip transitionClip;

    // BGM 状态机变量
    private AudioClip pendingFrontClip;
    private AudioClip pendingBackClip;
    private bool isSwitchingClips; // 标记是否正在“换歌”阶段
    private float targetFrontVol = 0f;
    private float targetBackVol = 0f;

    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        // 关键防护：如果当前物体是即将被销毁的重复体，则不注册事件
        if (Instance != this) return;

        // 订阅所有需要发声的事件
        EventManager.Instance.AddListener(EventName.OnPlayerJump, PlayJumpSFX);
        EventManager.Instance.AddListener(EventName.OnPlayerLand, PlayLandSFX);
        EventManager.Instance.AddListener(EventName.OnPlayerThrow, PlayThrowSFX);
        EventManager.Instance.AddListener(EventName.OnBombExplode_PartialZone, PlayExplodeSFX_PartialZone);
        EventManager.Instance.AddListener(EventName.OnBombExplode_Portal, PlayExplodeSFX_Portal);
        EventManager.Instance.AddListener(EventName.OnTransitionStart, PlayTransitionSFX);
        EventManager.Instance.AddListener(EventName.OnUIClick, PlayUIClickSFX);

        // 世界切换订阅：负责音量调整
        EventManager.Instance.AddListener(EventName.OnWorldSwitch, OnWorldSwitched);

        SceneManager.sceneLoaded += OnSceneLoadedForBGM;
    }

    private void OnDisable()
    {
        if (Instance != this) return;

        EventManager.Instance.RemoveListener(EventName.OnPlayerJump, PlayJumpSFX);
        EventManager.Instance.RemoveListener(EventName.OnPlayerLand, PlayLandSFX);
        EventManager.Instance.RemoveListener(EventName.OnPlayerThrow, PlayThrowSFX);
        EventManager.Instance.RemoveListener(EventName.OnBombExplode_PartialZone, PlayExplodeSFX_PartialZone);
        EventManager.Instance.RemoveListener(EventName.OnBombExplode_Portal, PlayExplodeSFX_Portal);
        EventManager.Instance.RemoveListener(EventName.OnTransitionStart, PlayTransitionSFX);
        EventManager.Instance.RemoveListener(EventName.OnUIClick, PlayUIClickSFX);

        EventManager.Instance.RemoveListener(EventName.OnWorldSwitch, OnWorldSwitched);

        SceneManager.sceneLoaded -= OnSceneLoadedForBGM;
    }

    private void Update()
    {
        // 计算每秒音量改变的速度（防除零错误）
        float fadeSpeed = (bgmFadeDuration > 0.01f) ? (1f / bgmFadeDuration) : 100f;

        if (isSwitchingClips)
        {
            // 旧音乐淡出。
            bgmFrontSource.volume = Mathf.MoveTowards(bgmFrontSource.volume, 0f, fadeSpeed * Time.unscaledDeltaTime);
            bgmBackSource.volume = Mathf.MoveTowards(bgmBackSource.volume, 0f, fadeSpeed * Time.unscaledDeltaTime);

            // 当两轨音量都完全降为0时，瞬间切换歌曲
            if (bgmFrontSource.volume <= 0f && bgmBackSource.volume <= 0f)
            {
                bgmFrontSource.clip = pendingFrontClip;
                bgmBackSource.clip = pendingBackClip;

                // 播放新曲
                if (pendingFrontClip != null)
                    bgmFrontSource.Play();
                else bgmFrontSource.Stop();
                if (pendingBackClip != null)
                    bgmBackSource.Play();
                else bgmBackSource.Stop();

                // 结束换歌阶段，进入下一帧的淡入阶段
                isSwitchingClips = false;
            }
        }
        else
        {
            // 向目标音量平滑过渡
            bgmFrontSource.volume = Mathf.MoveTowards(bgmFrontSource.volume, targetFrontVol, fadeSpeed * Time.unscaledDeltaTime);
            bgmBackSource.volume = Mathf.MoveTowards(bgmBackSource.volume, targetBackVol, fadeSpeed * Time.unscaledDeltaTime);
        }
    }

    // --- BGM 管理 ---
    private void OnSceneLoadedForBGM(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            PlayMainMenuBGM();
        }
        else
        {
            PlayGameplayBGM();
        }
    }

    private void PlayMainMenuBGM()
    {
        targetFrontVol = 1f;
        targetBackVol = 0f;
        ChangeBGMClips(mainMenuBGM, null);
    }

    private void PlayGameplayBGM()
    {
        // 初始化目标为0，真正正确的音量将由紧接着 LevelWorldManager 触发的 OnWorldSwitch 事件分配
        targetFrontVol = 0f;
        targetBackVol = 0f;
        ChangeBGMClips(gameplayFrontBGM, gameplayBackBGM);
    }

    private void ChangeBGMClips(AudioClip front, AudioClip back)
    {
        // 如果下一首要放的歌和现在正在放的歌一样，直接 return
        if (bgmFrontSource.clip == front && bgmBackSource.clip == back && !isSwitchingClips)
            return;

        pendingFrontClip = front;
        pendingBackClip = back;
        isSwitchingClips = true;

        // 如果目前系统并没有在播放声音，强行把音量设为0，跳过淡出，直接进入淡入状态。
        if (!bgmFrontSource.isPlaying && !bgmBackSource.isPlaying)
        {
            bgmFrontSource.volume = 0f;
            bgmBackSource.volume = 0f;
        }
    }

    private void OnWorldSwitched(object sender, EventArgs e)
    {
        if (e is WorldSwitchEventArgs args)
        {
            // 只有在游玩关卡时才响应事件
            if (pendingFrontClip == gameplayFrontBGM || bgmFrontSource.clip == gameplayFrontBGM)
            {
                WorldType activeWorld = args.NewWorld;

                targetFrontVol = (activeWorld == WorldType.Front) ? 1f : 0f;
                targetBackVol = (activeWorld == WorldType.Back) ? 1f : 0f;
            }
        }
    }

    // --- SFX 播放方法 ---
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    // --- 事件回调函数 ---
    private void PlayJumpSFX(object sender, EventArgs e) => PlaySFX(jumpClip);
    private void PlayLandSFX(object sender, EventArgs e) => PlaySFX(landClip);
    private void PlayThrowSFX(object sender, EventArgs e) => PlaySFX(throwClip);
    private void PlayExplodeSFX_PartialZone(object sender, EventArgs e) => PlaySFX(explodeClip_PartialZone);
    private void PlayExplodeSFX_Portal(object sender, EventArgs e) => PlaySFX(explodeClip_Portal);
    private void PlayTransitionSFX(object sender, EventArgs e) => PlaySFX(transitionClip);
    private void PlayUIClickSFX(object sender, EventArgs e) => PlaySFX(uiClickClip);
}
