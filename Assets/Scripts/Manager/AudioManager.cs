using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip mainMenuBGM;
    public AudioClip gameplayBGM;

    [Header("SFX Clips")]
    public AudioClip uiClickClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip throwClip;
    public AudioClip explodeClip_PartialZone;
    public AudioClip explodeClip_Portal;
    public AudioClip transitionClip;

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

        SceneManager.sceneLoaded -= OnSceneLoadedForBGM;
    }

    // --- BGM 管理 ---
    private void OnSceneLoadedForBGM(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            PlayBGM(mainMenuBGM);
        }
        else
        {
            PlayBGM(gameplayBGM);
        }
    }

    private void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
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
