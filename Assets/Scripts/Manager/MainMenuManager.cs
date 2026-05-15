using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Serializable]
public struct LevelButtonLayout
{
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;

    public LevelButtonLayout(Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        this.anchoredPosition = anchoredPosition;
        this.sizeDelta = sizeDelta;
    }
}

public class MainMenuManager : MonoBehaviour
{
    private enum MenuState
    {
        Start,
        Credits,
        LevelSelect
    }

    private const int FixedLevelButtonCount = 7;

    [Header("Panel References")]
    public GameObject startPanel;
    public GameObject creditsPanel;
    public GameObject levelSelectPanel;
    public GameObject darkOverlay;
    public Image backgroundImage;

    [Header("Level Select References")]
    public Transform levelGrid;
    public GameObject levelButtonPrefab;
    public Button[] fixedLevelButtons = new Button[FixedLevelButtonCount];
    public Button resetSaveButton;

    [Header("Start Buttons")]
    public Button startBtn;
    public Button creditsBtn;
    public Button quitBtn;

    [Header("Credits")]
    public Button creditsBackButton;
    public Vector2 creditsBackButtonAnchoredPosition = new Vector2(0f, 110f);
    public Vector2 creditsBackButtonSize = new Vector2(360f, 85f);
    public string creditsBackButtonLabel = "Back";

    [Header("Transition Durations")]
    public float startTitleFadeDuration = 0.32f;
    public float startButtonGroupFadeDuration = 0.48f;
    public float levelSelectFadeDuration = 0.4f;
    public float creditsFadeDuration = 0.38f;

    [Header("Transition Curve")]
    public AnimationCurve fadeCurve = CreateRecommendedFadeCurve();

    [Header("Background Blur")]
    public float panelBlurStrength = 2.2f;

    [Header("Level Button Layout")]
    public LevelButtonLayout[] levelButtonLayouts = CreateDefaultLevelButtonLayouts();

    private MenuState currentState = MenuState.Start;
    private bool isAnimating;
    private Coroutine transitionCoroutine;

    private GameObject startTitleObject;
    private GameObject startButtonGroupObject;
    private CanvasGroup startTitleGroup;
    private CanvasGroup startButtonGroupGroup;
    private CanvasGroup startPanelGroup;
    private CanvasGroup creditsPanelGroup;
    private CanvasGroup levelSelectPanelGroup;
    private Material backgroundBlurMaterialInstance;
    private float currentBackgroundBlur;

    private void OnValidate()
    {
        EnsureLayoutArrayLength();
        EnsureFixedButtonArrayLength();
        EnsureFadeCurve();
    }

    private void Start()
    {
        EnsureLayoutArrayLength();
        EnsureFixedButtonArrayLength();
        EnsureFadeCurve();
        CacheSceneReferences();
        InitializeCanvasGroups();
        EnsureCreditsBackButton();
        EnsureFixedLevelButtons();
        BindStaticButtons();
        InitializeMenuState();
    }

    private void Update()
    {
        if (isAnimating || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameState.Transitioning) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        HandleEscKey();
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        isAnimating = false;
    }

    private void OnDestroy()
    {
        if (backgroundBlurMaterialInstance != null)
        {
            Destroy(backgroundBlurMaterialInstance);
            backgroundBlurMaterialInstance = null;
        }
    }

    private void HandleEscKey()
    {
        if (currentState == MenuState.Credits)
        {
            BeginTransition(TransitionCreditsToStart());
        }
        else if (currentState == MenuState.LevelSelect)
        {
            BeginTransition(TransitionLevelSelectToStart());
        }
    }

    private void BindStaticButtons()
    {
        if (startBtn != null)
        {
            PrepareButtonVisualTransition(startBtn);
            startBtn.onClick.RemoveAllListeners();
            startBtn.onClick.AddListener(OpenLevelSelect);
        }

        if (creditsBtn != null)
        {
            PrepareButtonVisualTransition(creditsBtn);
            creditsBtn.onClick.RemoveAllListeners();
            creditsBtn.onClick.AddListener(OpenCredits);
        }

        if (quitBtn != null)
        {
            PrepareButtonVisualTransition(quitBtn);
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(QuitGame);
        }

        if (resetSaveButton != null)
        {
            PrepareButtonVisualTransition(resetSaveButton);
            resetSaveButton.onClick.RemoveAllListeners();
            resetSaveButton.onClick.AddListener(ResetSave);
        }
    }

    private void CacheSceneReferences()
    {
        if (startPanel == null) startPanel = transform.Find("StartPanel")?.gameObject;
        if (creditsPanel == null) creditsPanel = transform.Find("CreditsPanel")?.gameObject;
        if (levelSelectPanel == null) levelSelectPanel = transform.Find("LevelSelectPanel")?.gameObject;
        if (darkOverlay == null) darkOverlay = transform.Find("DarkOverlay")?.gameObject;
        if (backgroundImage == null) backgroundImage = transform.Find("BackgroundImage")?.GetComponent<Image>();

        if (levelGrid == null && levelSelectPanel != null)
        {
            levelGrid = levelSelectPanel.transform.Find("LevelGrid");
        }

        if (resetSaveButton == null && levelSelectPanel != null)
        {
            resetSaveButton = levelSelectPanel.GetComponentInChildren<Button>(true);
            if (resetSaveButton != null && resetSaveButton.gameObject.name != "Reset")
            {
                Button namedReset = levelSelectPanel.transform.Find("Reset")?.GetComponent<Button>();
                if (namedReset != null) resetSaveButton = namedReset;
            }
        }

        if (startBtn == null && startPanel != null)
        {
            startBtn = startPanel.transform.Find("ButtonGroup/Button_Start")?.GetComponent<Button>();
        }

        if (creditsBtn == null && startPanel != null)
        {
            creditsBtn = startPanel.transform.Find("ButtonGroup/Button_Credits")?.GetComponent<Button>();
        }

        if (quitBtn == null && startPanel != null)
        {
            quitBtn = startPanel.transform.Find("ButtonGroup/Button_Exit")?.GetComponent<Button>();
        }
    }

    private void InitializeCanvasGroups()
    {
        if (startPanel != null)
        {
            startPanelGroup = GetOrAddCanvasGroup(startPanel);
            startTitleObject = startPanel.transform.Find("Title")?.gameObject;
            startButtonGroupObject = startPanel.transform.Find("ButtonGroup")?.gameObject;

            if (startTitleObject != null)
            {
                startTitleObject.SetActive(true);
                startTitleGroup = GetOrAddCanvasGroup(startTitleObject);
            }

            if (startButtonGroupObject != null)
            {
                startButtonGroupObject.SetActive(true);
                startButtonGroupGroup = GetOrAddCanvasGroup(startButtonGroupObject);
            }
        }

        if (creditsPanel != null)
        {
            creditsPanelGroup = GetOrAddCanvasGroup(creditsPanel);
            Transform creditsTitle = creditsPanel.transform.Find("Title");
            if (creditsTitle != null) creditsTitle.gameObject.SetActive(true);
            Transform creditsContent = creditsPanel.transform.Find("Credits");
            if (creditsContent != null) creditsContent.gameObject.SetActive(true);
        }

        if (levelSelectPanel != null)
        {
            levelSelectPanelGroup = GetOrAddCanvasGroup(levelSelectPanel);
        }

        InitializeBackgroundBlur();
    }

    private void InitializeMenuState()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        SetPanelActive(startPanel, true);
        SetPanelActive(creditsPanel, true);
        SetPanelActive(levelSelectPanel, true);

        SetStartPanelVisuals(1f, 1f, true);
        SetStartButtonsVisible(true);
        SetPanelCanvasState(startPanelGroup, 1f, true, true);
        SetCreditsBackButtonVisible(false);
        SetPanelCanvasState(creditsPanelGroup, 0f, false, false);

        RefreshLevelButtons();
        SetPanelCanvasState(levelSelectPanelGroup, 0f, false, false);

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
        }

        currentState = MenuState.Start;
    }

    private void OpenLevelSelect()
    {
        if (isAnimating || currentState != MenuState.Start) return;
        BeginTransition(TransitionStartToLevelSelect());
    }

    private void OpenCredits()
    {
        if (isAnimating || currentState != MenuState.Start) return;
        BeginTransition(TransitionStartToCredits());
    }

    private void QuitGame()
    {
        if (isAnimating) return;

        Debug.Log("退出游戏");
        Application.Quit();
    }

    private void BeginTransition(IEnumerator routine)
    {
        if (routine == null || isAnimating) return;
        transitionCoroutine = StartCoroutine(RunManagedTransition(routine));
    }

    private IEnumerator RunManagedTransition(IEnumerator routine)
    {
        isAnimating = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Transitioning);
        }

        yield return routine;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.MainMenu);
        }

        transitionCoroutine = null;
        isAnimating = false;
    }

    private IEnumerator TransitionStartToLevelSelect()
    {
        float totalDuration = GetCombinedTransitionDuration(levelSelectFadeDuration);
        StartCoroutine(AnimateBackgroundBlur(currentBackgroundBlur, panelBlurStrength, totalDuration));
        yield return FadeStartPanelOut();

        if (darkOverlay != null) darkOverlay.SetActive(false);

        RefreshLevelButtons();
        yield return FadePanelIn(levelSelectPanel, levelSelectPanelGroup, levelSelectFadeDuration);
        currentState = MenuState.LevelSelect;
    }

    private IEnumerator TransitionLevelSelectToStart()
    {
        float totalDuration = GetCombinedTransitionDuration(levelSelectFadeDuration);
        StartCoroutine(AnimateBackgroundBlur(currentBackgroundBlur, 0f, totalDuration));
        yield return FadePanelOut(levelSelectPanel, levelSelectPanelGroup, levelSelectFadeDuration);

        if (darkOverlay != null) darkOverlay.SetActive(true);

        yield return FadeStartPanelIn();
        currentState = MenuState.Start;
    }

    private IEnumerator TransitionStartToCredits()
    {
        float totalDuration = GetCombinedTransitionDuration(creditsFadeDuration);
        StartCoroutine(AnimateBackgroundBlur(currentBackgroundBlur, panelBlurStrength, totalDuration));
        yield return FadeStartPanelOut();

        if (darkOverlay != null) darkOverlay.SetActive(true);

        yield return FadeCreditsPanelIn();
        currentState = MenuState.Credits;
    }

    private IEnumerator TransitionCreditsToStart()
    {
        float totalDuration = GetCombinedTransitionDuration(creditsFadeDuration);
        StartCoroutine(AnimateBackgroundBlur(currentBackgroundBlur, 0f, totalDuration));
        yield return FadeCreditsPanelOut();

        if (darkOverlay != null) darkOverlay.SetActive(true);

        yield return FadeStartPanelIn();
        currentState = MenuState.Start;
    }

    private IEnumerator FadeStartPanelOut()
    {
        SetPanelActive(startPanel, true);
        SetPanelCanvasState(startPanelGroup, 1f, false, false);
        SetStartButtonsVisible(false);
        yield return FadeStartTitle(1f, 0f);
        SetStartPanelVisuals(0f, 0f, false);
        SetPanelCanvasState(startPanelGroup, 0f, false, false);
    }

    private IEnumerator FadeStartPanelIn()
    {
        SetPanelActive(startPanel, true);
        SetStartPanelVisuals(0f, 0f, true);
        SetStartButtonsVisible(false);
        SetPanelCanvasState(startPanelGroup, 1f, false, false);
        PrepareUiForReveal(startPanel);
        yield return null;
        yield return FadeStartTitle(0f, 1f);
        SetStartPanelVisuals(1f, 1f, true);
        SetStartButtonsVisible(true);
        SetPanelCanvasState(startPanelGroup, 1f, true, true);
    }

    private IEnumerator FadeStartTitle(float from, float to)
    {
        float duration = Mathf.Max(0.0001f, startTitleFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (startTitleGroup != null)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                startTitleGroup.alpha = Mathf.LerpUnclamped(from, to, EvaluateFadeCurve(progress));
            }

            yield return null;
        }

        if (startTitleGroup != null) startTitleGroup.alpha = to;
    }

    private IEnumerator FadeCreditsPanelIn()
    {
        SetCreditsBackButtonVisible(false);
        yield return FadePanelIn(creditsPanel, creditsPanelGroup, creditsFadeDuration);
        SetCreditsBackButtonVisible(true);
    }

    private IEnumerator FadeCreditsPanelOut()
    {
        SetCreditsBackButtonVisible(false);
        yield return FadePanelOut(creditsPanel, creditsPanelGroup, creditsFadeDuration);
    }

    private IEnumerator FadePanelIn(GameObject panel, CanvasGroup canvasGroup, float duration)
    {
        if (panel == null || canvasGroup == null) yield break;

        SetPanelActive(panel, true);
        SetPanelCanvasState(canvasGroup, 0f, false, false);
        PrepareUiForReveal(panel);
        yield return null;
        yield return FadeCanvasGroup(canvasGroup, 0f, 1f, duration);
        SetPanelCanvasState(canvasGroup, 1f, true, true);
    }

    private IEnumerator FadePanelOut(GameObject panel, CanvasGroup canvasGroup, float duration)
    {
        if (panel == null || canvasGroup == null) yield break;

        SetPanelActive(panel, true);
        SetPanelCanvasState(canvasGroup, 1f, false, false);
        yield return FadeCanvasGroup(canvasGroup, 1f, 0f, duration);
        SetPanelCanvasState(canvasGroup, 0f, false, false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.LerpUnclamped(from, to, EvaluateFadeCurve(t));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void InitializeBackgroundBlur()
    {
        if (backgroundImage == null) return;

        Shader blurShader = Resources.Load<Shader>("Shaders/UIGaussianBlurSprite");
        if (blurShader == null)
        {
            Debug.LogWarning("Background blur shader not found at Resources/Shaders/UIGaussianBlurSprite.");
            return;
        }

        backgroundBlurMaterialInstance = new Material(blurShader)
        {
            name = "MainMenuBackgroundBlur (Runtime)"
        };
        backgroundImage.material = backgroundBlurMaterialInstance;
        SetBackgroundBlur(0f);
    }

    private IEnumerator AnimateBackgroundBlur(float from, float to, float duration)
    {
        if (backgroundBlurMaterialInstance == null)
        {
            currentBackgroundBlur = to;
            yield break;
        }

        if (duration <= 0f)
        {
            SetBackgroundBlur(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetBackgroundBlur(Mathf.LerpUnclamped(from, to, EvaluateFadeCurve(t)));
            yield return null;
        }

        SetBackgroundBlur(to);
    }

    private void SetBackgroundBlur(float blurValue)
    {
        currentBackgroundBlur = Mathf.Max(0f, blurValue);

        if (backgroundBlurMaterialInstance == null) return;
        backgroundBlurMaterialInstance.SetFloat("_BlurSize", currentBackgroundBlur);
    }

    private static void PrepareUiForReveal(GameObject panel)
    {
        if (panel == null) return;

        RectTransform rect = panel.transform as RectTransform;
        if (rect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                texts[i].ForceMeshUpdate();
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    private void EnsureFixedLevelButtons()
    {
        if (levelGrid == null) return;

        LayoutGroup layoutGroup = levelGrid.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        for (int i = 0; i < FixedLevelButtonCount; i++)
        {
            bool wasMissing = fixedLevelButtons[i] == null;

            if (fixedLevelButtons[i] == null)
            {
                Transform existingChild = levelGrid.Find(GetLevelButtonName(i));
                if (existingChild != null)
                {
                    fixedLevelButtons[i] = existingChild.GetComponent<Button>();
                }
            }

            if (fixedLevelButtons[i] == null)
            {
                fixedLevelButtons[i] = CreateFixedLevelButton(i);
            }

            if (wasMissing && fixedLevelButtons[i] != null)
            {
                ApplyLevelButtonLayout(fixedLevelButtons[i], i);
            }

            PrepareButtonVisualTransition(fixedLevelButtons[i]);
        }
    }

    private Button CreateFixedLevelButton(int index)
    {
        if (levelGrid == null || levelButtonPrefab == null) return null;

        GameObject buttonObject = Instantiate(levelButtonPrefab, levelGrid);
        buttonObject.name = GetLevelButtonName(index);
        buttonObject.SetActive(true);

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.gameObject.SetActive(true);
        }

        return button;
    }

    private void ApplyLevelButtonLayout(Button button, int index)
    {
        if (button == null || index < 0 || index >= levelButtonLayouts.Length) return;

        RectTransform rect = button.transform as RectTransform;
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = levelButtonLayouts[index].anchoredPosition;
        rect.sizeDelta = levelButtonLayouts[index].sizeDelta;
    }

    private void RefreshLevelButtons()
    {
        EnsureFixedLevelButtons();

        if (DataManager.Instance == null) return;

        int totalLevels = Mathf.Min(FixedLevelButtonCount, DataManager.Instance.GetTotalLevelCount());
        int unlockedIndex = DataManager.Instance.UnlockedLevelIndex;

        for (int i = 0; i < fixedLevelButtons.Length; i++)
        {
            Button button = fixedLevelButtons[i];
            if (button == null) continue;

            bool shouldShow = i < totalLevels && i <= unlockedIndex;
            button.gameObject.SetActive(shouldShow);

            button.onClick.RemoveAllListeners();
            button.interactable = shouldShow;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = GetLevelButtonLabel(i);
            }

            if (shouldShow)
            {
                int levelIndex = i;
                button.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
            }
        }

        if (DataManager.Instance.GetTotalLevelCount() > FixedLevelButtonCount)
        {
            Debug.LogWarning($"Main menu is configured for {FixedLevelButtonCount} fixed level buttons, but DataManager has more levels.");
        }
    }

    private string GetLevelButtonLabel(int index)
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.levels != null &&
            index >= 0 &&
            index < DataManager.Instance.levels.Count &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.levels[index].levelName))
        {
            return DataManager.Instance.levels[index].levelName;
        }

        return (index + 1).ToString();
    }

    private void OnLevelButtonClicked(int index)
    {
        if (isAnimating || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameState.Transitioning) return;
        if (SceneFlowManager.Instance == null) return;

        SceneFlowManager.Instance.LoadLevel(index);
    }

    private void ResetSave()
    {
        if (DataManager.Instance == null) return;

        DataManager.Instance.ResetProgress();
        RefreshLevelButtons();
    }

    private void EnsureCreditsBackButton()
    {
        if (creditsPanel == null) return;
        bool createdAtRuntime = false;

        if (creditsBackButton == null)
        {
            creditsBackButton = creditsPanel.transform.Find("Button_Back")?.GetComponent<Button>();
        }

        if (creditsBackButton == null && quitBtn != null)
        {
            GameObject backButtonObject = Instantiate(quitBtn.gameObject, creditsPanel.transform);
            backButtonObject.name = "Button_Back";
            creditsBackButton = backButtonObject.GetComponent<Button>();
            createdAtRuntime = creditsBackButton != null;
        }

        if (creditsBackButton == null) return;

        if (createdAtRuntime)
        {
            RectTransform rect = creditsBackButton.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = creditsBackButtonAnchoredPosition;
                rect.sizeDelta = creditsBackButtonSize;
            }

            TMP_Text label = creditsBackButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = creditsBackButtonLabel;
            }
        }

        creditsBackButton.gameObject.SetActive(true);
        PrepareButtonVisualTransition(creditsBackButton);
        creditsBackButton.onClick.RemoveAllListeners();
        creditsBackButton.onClick.AddListener(() =>
        {
            if (!isAnimating && currentState == MenuState.Credits)
            {
                BeginTransition(TransitionCreditsToStart());
            }
        });
    }

    private void SetStartPanelVisuals(float titleAlpha, float buttonAlpha, bool visible)
    {
        if (visible)
        {
            if (startTitleObject != null) startTitleObject.SetActive(true);
        }
        else
        {
            if (startTitleObject != null) startTitleObject.SetActive(false);
        }

        if (startTitleGroup != null) startTitleGroup.alpha = titleAlpha;
        if (startButtonGroupGroup != null) startButtonGroupGroup.alpha = buttonAlpha;
    }

    private void SetStartButtonsVisible(bool visible)
    {
        if (startButtonGroupObject != null)
        {
            startButtonGroupObject.SetActive(visible);
        }
    }

    private void SetCreditsBackButtonVisible(bool visible)
    {
        if (creditsBackButton != null)
        {
            creditsBackButton.gameObject.SetActive(visible);
        }
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private static void PrepareButtonVisualTransition(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.fadeDuration = 0f;
        button.colors = colors;

        if (button.targetGraphic != null)
        {
            button.targetGraphic.CrossFadeColor(colors.normalColor, 0f, true, true);
            button.targetGraphic.canvasRenderer.SetAlpha(colors.normalColor.a);
        }
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null) return null;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private static void SetPanelCanvasState(CanvasGroup canvasGroup, float alpha, bool interactable, bool blocksRaycasts)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private void EnsureLayoutArrayLength()
    {
        if (levelButtonLayouts != null && levelButtonLayouts.Length == FixedLevelButtonCount) return;

        LevelButtonLayout[] previous = levelButtonLayouts;
        levelButtonLayouts = CreateDefaultLevelButtonLayouts();

        if (previous == null) return;

        int copyCount = Mathf.Min(previous.Length, levelButtonLayouts.Length);
        for (int i = 0; i < copyCount; i++)
        {
            if (previous[i].sizeDelta != Vector2.zero)
            {
                levelButtonLayouts[i] = previous[i];
            }
        }
    }

    private void EnsureFixedButtonArrayLength()
    {
        if (fixedLevelButtons != null && fixedLevelButtons.Length == FixedLevelButtonCount) return;

        Button[] previous = fixedLevelButtons;
        fixedLevelButtons = new Button[FixedLevelButtonCount];

        if (previous == null) return;

        int copyCount = Mathf.Min(previous.Length, fixedLevelButtons.Length);
        for (int i = 0; i < copyCount; i++)
        {
            fixedLevelButtons[i] = previous[i];
        }
    }

    private void EnsureFadeCurve()
    {
        if (fadeCurve == null || fadeCurve.length == 0)
        {
            fadeCurve = CreateRecommendedFadeCurve();
        }
    }

    private float GetCombinedTransitionDuration(float targetPanelDuration)
    {
        return Mathf.Max(0.0001f, startTitleFadeDuration + targetPanelDuration);
    }

    private float EvaluateFadeCurve(float t)
    {
        if (fadeCurve == null || fadeCurve.length == 0) return t;
        return fadeCurve.Evaluate(Mathf.Clamp01(t));
    }

    private static string GetLevelButtonName(int index)
    {
        return $"Button_Level{index + 1}";
    }

    private static AnimationCurve CreateRecommendedFadeCurve()
    {
        return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    private static LevelButtonLayout[] CreateDefaultLevelButtonLayouts()
    {
        return new[]
        {
            new LevelButtonLayout(new Vector2(-720f, 120f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(-480f, -40f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(-240f, 140f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(0f, -20f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(240f, 130f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(480f, -50f), new Vector2(220f, 220f)),
            new LevelButtonLayout(new Vector2(720f, 110f), new Vector2(220f, 220f))
        };
    }
}
