using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum DialogueSpeakerSide
{
    Left = 0,
    Right = 1
}

[Serializable]
public class DialogueLineData
{
    public DialogueSpeakerSide speaker;
    public string text;
}

[Serializable]
public class SceneDialogueData
{
    public string leftSpeakerName;
    public string rightSpeakerName;
    public string leftPortraitResourcePath;
    public string rightPortraitResourcePath;
    public float introDelaySeconds;
    public float typewriterCharactersPerSecond;
    public float portraitSlideDuration;
    public DialogueLineData[] lines;
}

public class SimpleSceneDialogueController : MonoBehaviour
{
    private const float DefaultIntroDelay = 1f;
    private const float DefaultTypewriterSpeed = 28f;
    private const float DefaultSlideDuration = 0.45f;
    private const float PortraitOffscreenPadding = 80f;
    private static Sprite fallbackUiSprite;

    private SceneDialogueData dialogueData;
    private Canvas canvas;
    private RectTransform leftPortraitRect;
    private RectTransform rightPortraitRect;
    private Image leftPortraitImage;
    private Image rightPortraitImage;
    private Text leftPortraitLabel;
    private Text rightPortraitLabel;
    private Text speakerNameText;
    private Text dialogueContentText;
    private Color leftPortraitBaseColor;
    private Color rightPortraitBaseColor;

    private Vector2 leftPortraitOnscreenPos;
    private Vector2 rightPortraitOnscreenPos;
    private Vector2 leftPortraitOffscreenPos;
    private Vector2 rightPortraitOffscreenPos;
    private bool ownsInputBlock;
    private bool isTyping;
    private bool isDialogueReady;
    private int currentLineIndex = -1;
    private string currentFullLine = string.Empty;
    private Coroutine typingCoroutine;

    public void Initialize(SceneDialogueData data)
    {
        dialogueData = data;
        ApplyDefaults(dialogueData);
        BuildUi();
        GameplayInputBlocker.AcquireBlock();
        ownsInputBlock = true;
        StartCoroutine(RunDialogueSequence());
    }

    private void Update()
    {
        if (!isDialogueReady || Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (isTyping)
        {
            CompleteCurrentLine();
            return;
        }

        ShowNextLine();
    }

    private void OnDestroy()
    {
        if (ownsInputBlock)
        {
            GameplayInputBlocker.ReleaseBlock();
            ownsInputBlock = false;
        }
    }

    private static void ApplyDefaults(SceneDialogueData data)
    {
        if (string.IsNullOrWhiteSpace(data.leftSpeakerName)) data.leftSpeakerName = "Left";
        if (string.IsNullOrWhiteSpace(data.rightSpeakerName)) data.rightSpeakerName = "Right";
        if (data.introDelaySeconds <= 0f) data.introDelaySeconds = DefaultIntroDelay;
        if (data.typewriterCharactersPerSecond <= 0f) data.typewriterCharactersPerSecond = DefaultTypewriterSpeed;
        if (data.portraitSlideDuration <= 0f) data.portraitSlideDuration = DefaultSlideDuration;
        if (data.lines == null) data.lines = Array.Empty<DialogueLineData>();
    }

    private void BuildUi()
    {
        Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (TryBuildUiFromPrefab(fallbackFont))
        {
            LayoutPortraitsFromCurrentPlacement();
            CachePortraitBaseColors();
            return;
        }

        BuildFallbackRuntimeUi(fallbackFont);
        LayoutPortraitsFromCurrentPlacement();
        CachePortraitBaseColors();
    }

    private bool TryBuildUiFromPrefab(Font fallbackFont)
    {
        GameObject prefab = Resources.Load<GameObject>("Dialogue/SimpleSceneDialogueCanvas");
        if (prefab == null) return false;

        GameObject instance = Instantiate(prefab, transform);
        instance.name = prefab.name;

        canvas = instance.GetComponent<Canvas>();
        leftPortraitImage = RequireChildComponent<Image>(instance.transform, "LeftPortrait");
        rightPortraitImage = RequireChildComponent<Image>(instance.transform, "RightPortrait");
        speakerNameText = RequireChildComponent<Text>(instance.transform, "DialogueBox/SpeakerName");
        dialogueContentText = RequireChildComponent<Text>(instance.transform, "DialogueBox/DialogueContent");

        if (canvas == null || leftPortraitImage == null || rightPortraitImage == null || speakerNameText == null || dialogueContentText == null)
        {
            Destroy(instance);
            canvas = null;
            return false;
        }

        leftPortraitRect = leftPortraitImage.rectTransform;
        rightPortraitRect = rightPortraitImage.rectTransform;
        leftPortraitLabel = TryGetChildComponent<Text>(instance.transform, "LeftPortrait/LeftPortraitLabel");
        rightPortraitLabel = TryGetChildComponent<Text>(instance.transform, "RightPortrait/RightPortraitLabel");

        canvas.enabled = false;

        EnsureImageHasSprite(leftPortraitImage);
        EnsureImageHasSprite(rightPortraitImage);
        EnsureTextHasFont(speakerNameText, fallbackFont);
        EnsureTextHasFont(dialogueContentText, fallbackFont);

        if (leftPortraitLabel != null)
        {
            EnsureTextHasFont(leftPortraitLabel, fallbackFont);
            leftPortraitLabel.text = dialogueData.leftSpeakerName;
        }

        if (rightPortraitLabel != null)
        {
            EnsureTextHasFont(rightPortraitLabel, fallbackFont);
            rightPortraitLabel.text = dialogueData.rightSpeakerName;
        }

        ApplyPortraitAssets();
        return true;
    }

    private void BuildFallbackRuntimeUi(Font font)
    {
        Sprite defaultSprite = GetFallbackUiSprite();

        GameObject canvasObject = new GameObject("SimpleDialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvas.enabled = false;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.sprite = defaultSprite;
        overlayImage.type = Image.Type.Simple;
        overlayImage.color = new Color(0.65f, 0.65f, 0.65f, 0.35f);

        leftPortraitImage = CreatePortrait("LeftPortrait", canvasObject.transform, defaultSprite, new Color(0.83f, 0.46f, 0.46f, 0.95f), out leftPortraitRect, out leftPortraitLabel, font, dialogueData.leftSpeakerName);
        rightPortraitImage = CreatePortrait("RightPortrait", canvasObject.transform, defaultSprite, new Color(0.46f, 0.63f, 0.87f, 0.95f), out rightPortraitRect, out rightPortraitLabel, font, dialogueData.rightSpeakerName);

        GameObject boxObject = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        boxObject.transform.SetParent(canvasObject.transform, false);

        RectTransform boxRect = boxObject.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0f);
        boxRect.anchorMax = new Vector2(1f, 0f);
        boxRect.pivot = new Vector2(0.5f, 0f);
        boxRect.offsetMin = new Vector2(96f, 36f);
        boxRect.offsetMax = new Vector2(-96f, 276f);

        Image boxImage = boxObject.GetComponent<Image>();
        boxImage.sprite = defaultSprite;
        boxImage.type = Image.Type.Simple;
        boxImage.color = new Color(0f, 0f, 0f, 0.78f);

        speakerNameText = CreateText("SpeakerName", boxObject.transform, font, 28, TextAnchor.UpperLeft, new Color(1f, 0.94f, 0.72f, 1f));
        RectTransform speakerRect = speakerNameText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0.5f, 1f);
        speakerRect.offsetMin = new Vector2(32f, -56f);
        speakerRect.offsetMax = new Vector2(-32f, -12f);

        dialogueContentText = CreateText("DialogueContent", boxObject.transform, font, 36, TextAnchor.UpperLeft, Color.white);
        RectTransform contentRect = dialogueContentText.rectTransform;
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.offsetMin = new Vector2(32f, 24f);
        contentRect.offsetMax = new Vector2(-32f, -64f);

        leftPortraitRect.anchorMin = new Vector2(0f, 0f);
        leftPortraitRect.anchorMax = new Vector2(0f, 0f);
        leftPortraitRect.pivot = new Vector2(0f, 0f);
        leftPortraitRect.sizeDelta = new Vector2(360f, 620f);
        leftPortraitRect.anchoredPosition = new Vector2(52f, 0f);

        rightPortraitRect.anchorMin = new Vector2(1f, 0f);
        rightPortraitRect.anchorMax = new Vector2(1f, 0f);
        rightPortraitRect.pivot = new Vector2(1f, 0f);
        rightPortraitRect.sizeDelta = new Vector2(360f, 620f);
        rightPortraitRect.anchoredPosition = new Vector2(-52f, 0f);

        ApplyPortraitAssets();
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Image CreatePortrait(string objectName, Transform parent, Sprite defaultSprite, Color fallbackColor, out RectTransform portraitRect, out Text label, Font font, string labelText)
    {
        GameObject portraitObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(parent, false);

        portraitRect = portraitObject.GetComponent<RectTransform>();
        Image portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.sprite = defaultSprite;
        portraitImage.type = Image.Type.Simple;
        portraitImage.color = fallbackColor;
        portraitImage.raycastTarget = false;

        label = CreateText(objectName + "Label", portraitObject.transform, font, 34, TextAnchor.MiddleCenter, Color.white);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);
        label.text = labelText;

        return portraitImage;
    }

    private void ApplyPortraitAssets()
    {
        Sprite leftSprite = LoadPortrait(dialogueData.leftPortraitResourcePath);
        if (leftSprite != null)
        {
            leftPortraitImage.sprite = leftSprite;
            leftPortraitImage.type = Image.Type.Simple;
            leftPortraitImage.preserveAspect = true;
            if (leftPortraitLabel != null) leftPortraitLabel.gameObject.SetActive(false);
        }
        else if (leftPortraitLabel != null)
        {
            leftPortraitLabel.gameObject.SetActive(true);
            leftPortraitLabel.text = dialogueData.leftSpeakerName;
        }

        Sprite rightSprite = LoadPortrait(dialogueData.rightPortraitResourcePath);
        if (rightSprite != null)
        {
            rightPortraitImage.sprite = rightSprite;
            rightPortraitImage.type = Image.Type.Simple;
            rightPortraitImage.preserveAspect = true;
            if (rightPortraitLabel != null) rightPortraitLabel.gameObject.SetActive(false);
        }
        else if (rightPortraitLabel != null)
        {
            rightPortraitLabel.gameObject.SetActive(true);
            rightPortraitLabel.text = dialogueData.rightSpeakerName;
        }
    }

    private void LayoutPortraitsFromCurrentPlacement()
    {
        if (leftPortraitRect == null || rightPortraitRect == null) return;

        leftPortraitOnscreenPos = leftPortraitRect.anchoredPosition;
        rightPortraitOnscreenPos = rightPortraitRect.anchoredPosition;

        float leftWidth = leftPortraitRect.rect.width > 0f ? leftPortraitRect.rect.width : leftPortraitRect.sizeDelta.x;
        float rightWidth = rightPortraitRect.rect.width > 0f ? rightPortraitRect.rect.width : rightPortraitRect.sizeDelta.x;

        leftPortraitOffscreenPos = new Vector2(
            leftPortraitOnscreenPos.x - leftWidth - PortraitOffscreenPadding,
            leftPortraitOnscreenPos.y);
        rightPortraitOffscreenPos = new Vector2(
            rightPortraitOnscreenPos.x + rightWidth + PortraitOffscreenPadding,
            rightPortraitOnscreenPos.y);

        ResetPortraitsToOffscreen();
    }

    private void ResetPortraitsToOffscreen()
    {
        if (leftPortraitRect != null)
        {
            leftPortraitRect.anchoredPosition = leftPortraitOffscreenPos;
        }

        if (rightPortraitRect != null)
        {
            rightPortraitRect.anchoredPosition = rightPortraitOffscreenPos;
        }
    }

    private void CachePortraitBaseColors()
    {
        leftPortraitBaseColor = leftPortraitImage != null ? leftPortraitImage.color : Color.white;
        rightPortraitBaseColor = rightPortraitImage != null ? rightPortraitImage.color : Color.white;
    }

    private Sprite LoadPortrait(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        return Resources.Load<Sprite>(resourcePath);
    }

    private static Sprite GetFallbackUiSprite()
    {
        if (fallbackUiSprite != null) return fallbackUiSprite;

        Texture2D texture = Texture2D.whiteTexture;
        fallbackUiSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        fallbackUiSprite.name = "SimpleDialogueFallbackSprite";
        return fallbackUiSprite;
    }

    private static void EnsureImageHasSprite(Image image)
    {
        if (image == null || image.sprite != null) return;
        image.sprite = GetFallbackUiSprite();
        image.type = Image.Type.Simple;
    }

    private static void EnsureTextHasFont(Text text, Font fallbackFont)
    {
        if (text == null || text.font != null) return;
        text.font = fallbackFont;
    }

    private static T RequireChildComponent<T>(Transform root, string relativePath) where T : Component
    {
        Transform target = root.Find(relativePath);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static T TryGetChildComponent<T>(Transform root, string relativePath) where T : Component
    {
        Transform target = root.Find(relativePath);
        return target != null ? target.GetComponent<T>() : null;
    }

    private IEnumerator RunDialogueSequence()
    {
        if (dialogueData.introDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(dialogueData.introDelaySeconds);
        }

        ResetPortraitsToOffscreen();
        canvas.enabled = true;
        Canvas.ForceUpdateCanvases();
        yield return null;
        yield return SlidePortraitsIn();

        if (dialogueData.lines.Length == 0)
        {
            FinishDialogue();
            yield break;
        }

        isDialogueReady = true;
        ShowNextLine();
    }

    private IEnumerator SlidePortraitsIn()
    {
        float duration = dialogueData.portraitSlideDuration;
        ResetPortraitsToOffscreen();

        if (duration <= 0f)
        {
            leftPortraitRect.anchoredPosition = leftPortraitOnscreenPos;
            rightPortraitRect.anchoredPosition = rightPortraitOnscreenPos;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            leftPortraitRect.anchoredPosition = Vector2.LerpUnclamped(leftPortraitOffscreenPos, leftPortraitOnscreenPos, eased);
            rightPortraitRect.anchoredPosition = Vector2.LerpUnclamped(rightPortraitOffscreenPos, rightPortraitOnscreenPos, eased);
            yield return null;
        }

        leftPortraitRect.anchoredPosition = leftPortraitOnscreenPos;
        rightPortraitRect.anchoredPosition = rightPortraitOnscreenPos;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex >= dialogueData.lines.Length)
        {
            FinishDialogue();
            return;
        }

        DialogueLineData line = dialogueData.lines[currentLineIndex];
        currentFullLine = line.text ?? string.Empty;
        speakerNameText.text = line.speaker == DialogueSpeakerSide.Left ? dialogueData.leftSpeakerName : dialogueData.rightSpeakerName;

        UpdatePortraitFocus(line.speaker);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(currentFullLine));
    }

    private IEnumerator TypeLine(string fullText)
    {
        isTyping = true;
        dialogueContentText.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            yield break;
        }

        float timer = 0f;
        float interval = 1f / dialogueData.typewriterCharactersPerSecond;
        int visibleCharacters = 0;

        while (visibleCharacters < fullText.Length)
        {
            timer += Time.unscaledDeltaTime;
            while (timer >= interval && visibleCharacters < fullText.Length)
            {
                visibleCharacters++;
                timer -= interval;
            }

            dialogueContentText.text = fullText.Substring(0, visibleCharacters);
            yield return null;
        }

        dialogueContentText.text = fullText;
        isTyping = false;
        typingCoroutine = null;
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        dialogueContentText.text = currentFullLine;
        isTyping = false;
    }

    private void UpdatePortraitFocus(DialogueSpeakerSide activeSpeaker)
    {
        leftPortraitImage.color = GetPortraitDisplayColor(leftPortraitBaseColor, activeSpeaker == DialogueSpeakerSide.Left);
        rightPortraitImage.color = GetPortraitDisplayColor(rightPortraitBaseColor, activeSpeaker == DialogueSpeakerSide.Right);
    }

    private static Color GetPortraitDisplayColor(Color baseColor, bool isActive)
    {
        if (isActive) return baseColor;
        return new Color(baseColor.r * 0.45f, baseColor.g * 0.45f, baseColor.b * 0.45f, baseColor.a * 0.9f);
    }

    private void FinishDialogue()
    {
        isDialogueReady = false;

        if (ownsInputBlock)
        {
            GameplayInputBlocker.ReleaseBlock();
            ownsInputBlock = false;
        }

        Destroy(gameObject);
    }
}

public static class SimpleSceneDialogueBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnDialogueForScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        TextAsset dialogueAsset = Resources.Load<TextAsset>($"Dialogue/{sceneName}");
        if (dialogueAsset == null) return;

        SceneDialogueData dialogueData = JsonUtility.FromJson<SceneDialogueData>(dialogueAsset.text);
        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Length == 0) return;

        GameObject dialogueRoot = new GameObject("SimpleSceneDialogueRuntime");
        SimpleSceneDialogueController controller = dialogueRoot.AddComponent<SimpleSceneDialogueController>();
        controller.Initialize(dialogueData);
    }
}
