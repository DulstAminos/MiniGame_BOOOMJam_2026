using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private enum MenuState { Start, Credits, LevelSelect }

    [Header("Panels")]
    public CanvasGroup startPanel;
    public CanvasGroup creditsPanel;
    public CanvasGroup levelSelectPanel;
    public UIBlurController blurController;

    [Header("Static Buttons")]
    public Button startBtn;
    public Button creditsBtn;
    public Button quitBtn;
    public Button creditsBackBtn;

    [Header("Level Select Setup")]
    public Button[] levelButtons = new Button[7]; // 拖入Button_Level1~7

    [Header("Reset Feature Setup")]
    public bool enableResetFeature = false; // 重置功能开关
    public Button resetBtn;
    public GameObject resetTip;

    [Header("Transition Settings")]
    public float fadeDuration = 0.4f;
    public float panelBlurStrength = 2.2f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private MenuState currentState = MenuState.Start;
    private bool isAnimating = false;

    private void Start()
    {
        BindButtons();
        InitializeState();
    }

    private void Update()
    {
        if (isAnimating || GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Transitioning) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscKey();
        }
    }

    // ================= 初始化与绑定 =================
    private void BindButtons()
    {
        // 静态按钮绑定（去除了RemoveAllListeners，兼容全局UI音效）
        if (startBtn) startBtn.onClick.AddListener(() => ChangeMenuState(MenuState.LevelSelect));
        if (creditsBtn) creditsBtn.onClick.AddListener(() => ChangeMenuState(MenuState.Credits));
        if (quitBtn) quitBtn.onClick.AddListener(() => Application.Quit());
        if (creditsBackBtn) creditsBackBtn.onClick.AddListener(() => ChangeMenuState(MenuState.Start));

        // 重置按钮逻辑
        if (resetBtn && resetTip)
        {
            resetBtn.gameObject.SetActive(enableResetFeature);
            resetTip.SetActive(false); // 默认隐藏提示
            if (enableResetFeature)
            {
                resetBtn.onClick.AddListener(ResetSaveProgress);
            }
        }
    }

    private void InitializeState()
    {
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.MainMenu);

        // 强制初始化所有面板的透明度和交互状态
        SetPanelState(startPanel, 1f, true);
        SetPanelState(creditsPanel, 0f, false);
        SetPanelState(levelSelectPanel, 0f, false);

        currentState = MenuState.Start;
    }

    private void RefreshLevelButtons()
    {
        if (DataManager.Instance == null) return;

        int totalLevels = DataManager.Instance.GetTotalLevelCount();
        int unlockedIndex = DataManager.Instance.UnlockedLevelIndex;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button btn = levelButtons[i];
            if (btn == null) continue;

            bool isUnlocked = i < totalLevels && i <= unlockedIndex;
            btn.gameObject.SetActive(isUnlocked); // 隐藏未解锁关卡

            // 每次刷新重新绑定关卡点击事件
            btn.onClick.RemoveAllListeners();
            if (isUnlocked)
            {
                int levelIndex = i;
                btn.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
                btn.onClick.AddListener(() => this.TriggerEvent(EventName.OnUIClick));
            }
        }
    }

    // ================= 交互逻辑 =================
    private void HandleEscKey()
    {
        if (currentState == MenuState.Credits || currentState == MenuState.LevelSelect)
        {
            ChangeMenuState(MenuState.Start);
        }
    }

    private void OnLevelButtonClicked(int index)
    {
        if (isAnimating) return;
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadLevel(index);
        }
    }

    private void ResetSaveProgress()
    {
        if (DataManager.Instance == null) return;
        DataManager.Instance.ResetProgress();
        RefreshLevelButtons();

        // 短暂显示重置提示
        if (resetTip)
        {
            resetTip.SetActive(true);
            Invoke(nameof(HideResetTip), 2f);
        }
    }

    private void HideResetTip() => resetTip?.SetActive(false);

    // ================= 动画与状态机 =================
    private void ChangeMenuState(MenuState newState)
    {
        if (isAnimating || currentState == newState) return;
        StartCoroutine(TransitionRoutine(currentState, newState));
    }

    private IEnumerator TransitionRoutine(MenuState fromState, MenuState toState)
    {
        isAnimating = true;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.Transitioning);

        CanvasGroup fromPanel = GetPanelFromState(fromState);
        CanvasGroup toPanel = GetPanelFromState(toState);

        // 背景模糊处理
        float targetBlur = (toState == MenuState.Start) ? 0f : panelBlurStrength;
        float currentBlur = (fromState == MenuState.Start) ? 0f : panelBlurStrength;
        if (blurController) blurController.AnimateBlur(currentBlur, targetBlur, fadeDuration, fadeCurve);

        // 如果是要去选关界面，先刷新数据
        if (toState == MenuState.LevelSelect) RefreshLevelButtons();

        // 交叉淡入淡出动画
        float elapsed = 0f;
        SetPanelState(toPanel, 0f, false); // 先显示目标面板，但透明度为0

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / fadeDuration));

            if (fromPanel) fromPanel.alpha = Mathf.LerpUnclamped(1f, 0f, t);
            if (toPanel) toPanel.alpha = Mathf.LerpUnclamped(0f, 1f, t);

            yield return null;
        }

        // 确保最终状态
        SetPanelState(fromPanel, 0f, false);
        SetPanelState(toPanel, 1f, true);

        currentState = toState;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameState.MainMenu);
        isAnimating = false;
    }

    private CanvasGroup GetPanelFromState(MenuState state)
    {
        return state switch
        {
            MenuState.Start => startPanel,
            MenuState.Credits => creditsPanel,
            MenuState.LevelSelect => levelSelectPanel,
            _ => null
        };
    }

    private void SetPanelState(CanvasGroup panel, float alpha, bool interactable)
    {
        if (panel == null) return;

        // 为了节省性能，当alpha为0时，直接关闭GameObject；否则开启
        panel.gameObject.SetActive(alpha > 0.01f || interactable);
        panel.alpha = alpha;
        panel.interactable = interactable;
        panel.blocksRaycasts = interactable;
    }
}
