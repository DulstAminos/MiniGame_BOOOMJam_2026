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
    public float bgmCrossfadeDuration = 0.5f; // 切换世界时声音淡入淡出的时间

    [Header("SFX Clips")]
    public AudioClip uiClickClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip throwClip;
    public AudioClip explodeClip_PartialZone;
    public AudioClip explodeClip_Portal;
    public AudioClip transitionClip;

    private Coroutine crossfadeCoroutine;

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
        // 如果当前已经在放主菜单音乐，直接返回
        if (bgmFrontSource.clip == mainMenuBGM) return;

        bgmBackSource.Stop(); // 停掉里世界音乐
        bgmBackSource.clip = null;

        bgmFrontSource.clip = mainMenuBGM;
        bgmFrontSource.volume = 1f;
        bgmFrontSource.loop = true;
        bgmFrontSource.Play();
    }

    private void PlayGameplayBGM()
    {
        // 跨场景无缝播放检测：如果已经是游戏内BGM，直接返回
        if (bgmFrontSource.clip == gameplayFrontBGM && bgmBackSource.clip == gameplayBackBGM)
        {
            return;
        }

        // 设置剪辑
        bgmFrontSource.clip = gameplayFrontBGM;
        bgmBackSource.clip = gameplayBackBGM;

        // 设置循环
        bgmFrontSource.loop = true;
        bgmBackSource.loop = true;

        // 初始音量设为 0
        bgmFrontSource.volume = 0f;
        bgmBackSource.volume = 0f;

        // 同时调用Play，确保双轨时间轴绝对对齐
        bgmFrontSource.Play();
        bgmBackSource.Play();
    }

    private void OnWorldSwitched(object sender, EventArgs e)
    {
        // 如果当前不在游戏关卡状态（比如没在放游戏BGM），不处理
        if (bgmFrontSource.clip != gameplayFrontBGM) return;

        // 转换事件参数
        if (e is WorldSwitchEventArgs args)
        {
            WorldType activeWorld = args.NewWorld;

            float targetFrontVol = (activeWorld == WorldType.Front) ? 1f : 0f;
            float targetBackVol = (activeWorld == WorldType.Back) ? 1f : 0f;

            if (crossfadeCoroutine != null)
            {
                StopCoroutine(crossfadeCoroutine);
            }
            crossfadeCoroutine = StartCoroutine(CrossfadeBGM(targetFrontVol, targetBackVol));
        }
    }

    // 利用协程实现平滑过渡音量，不会生硬地卡顿
    private IEnumerator CrossfadeBGM(float targetFrontVol, float targetBackVol)
    {
        float startFrontVol = bgmFrontSource.volume;
        float startBackVol = bgmBackSource.volume;
        float timer = 0f;

        while (timer < bgmCrossfadeDuration)
        {
            // 即使在暂停(TimeScale=0)或者转场时，也能保证淡入淡出正常运作
            timer += Time.unscaledDeltaTime;
            float percent = timer / bgmCrossfadeDuration;

            bgmFrontSource.volume = Mathf.Lerp(startFrontVol, targetFrontVol, percent);
            bgmBackSource.volume = Mathf.Lerp(startBackVol, targetBackVol, percent);

            yield return null;
        }

        bgmFrontSource.volume = targetFrontVol;
        bgmBackSource.volume = targetBackVol;
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
