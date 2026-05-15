#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialoguePrefabGenerator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string DialogueFolder = "Assets/Resources/Dialogue";
    private const string WhiteTexturePath = "Assets/Resources/Dialogue/SimpleDialogueWhite.png";
    private const string PrefabPath = "Assets/Resources/Dialogue/SimpleSceneDialogueCanvas.prefab";
    private const string DefaultTmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Dialogue/Generate Simple Dialogue Canvas Prefab")]
    public static void GeneratePrefabAsset()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(DialogueFolder);

        Sprite whiteSprite = EnsureWhiteSprite();
        TMP_FontAsset tmpFont = LoadDefaultTmpFont();

        GameObject root = new GameObject("SimpleSceneDialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        CreateOverlay(root.transform, whiteSprite);
        CreatePortrait(root.transform, whiteSprite, "LeftPortrait", new Color(0.83f, 0.46f, 0.46f, 0.95f), new Vector2(52f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), "LeftPortraitLabel", tmpFont);
        CreatePortrait(root.transform, whiteSprite, "RightPortrait", new Color(0.46f, 0.63f, 0.87f, 0.95f), new Vector2(-52f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), "RightPortraitLabel", tmpFont);
        CreateDialogueBox(root.transform, whiteSprite, tmpFont);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated dialogue prefab at {PrefabPath}");
    }

    [MenuItem("Tools/Dialogue/Upgrade Existing Dialogue Prefab To TMP")]
    public static void UpgradeExistingPrefabToTmp()
    {
        if (!File.Exists(PrefabPath)) return;

        TMP_FontAsset tmpFont = LoadDefaultTmpFont();
        if (tmpFont == null)
        {
            Debug.LogError("TMP default font asset not found. Cannot upgrade dialogue prefab.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConvertLegacyTextToTmp(root.transform.Find("LeftPortrait/LeftPortraitLabel"), tmpFont, false);
            ConvertLegacyTextToTmp(root.transform.Find("RightPortrait/RightPortraitLabel"), tmpFont, false);
            ConvertLegacyTextToTmp(root.transform.Find("DialogueBox/SpeakerName"), tmpFont, false);
            ConvertLegacyTextToTmp(root.transform.Find("DialogueBox/DialogueContent"), tmpFont, true);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Upgraded dialogue prefab text components to TMP at {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [InitializeOnLoadMethod]
    private static void AutoGeneratePrefabIfMissing()
    {
        EditorApplication.delayCall += TryAutoGeneratePrefab;
    }

    private static void TryAutoGeneratePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(PrefabPath) && File.Exists(WhiteTexturePath))
        {
            UpgradeExistingPrefabToTmp();
            return;
        }

        GeneratePrefabAsset();
    }

    private static void CreateOverlay(Transform parent, Sprite sprite)
    {
        GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(parent, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlayObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = new Color(0.65f, 0.65f, 0.65f, 0.35f);
    }

    private static void CreatePortrait(
        Transform parent,
        Sprite sprite,
        string objectName,
        Color color,
        Vector2 anchoredPosition,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        string labelName,
        TMP_FontAsset font)
    {
        GameObject portraitObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(parent, false);

        RectTransform rect = portraitObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = new Vector2(360f, 620f);
        rect.anchoredPosition = anchoredPosition;

        Image image = portraitObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;

        GameObject labelObject = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(portraitObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 34f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.text = objectName.Contains("Left") ? "Left" : "Right";
        label.richText = true;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
    }

    private static void CreateDialogueBox(Transform parent, Sprite sprite, TMP_FontAsset font)
    {
        GameObject boxObject = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        boxObject.transform.SetParent(parent, false);

        RectTransform boxRect = boxObject.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0f, 0f);
        boxRect.anchorMax = new Vector2(1f, 0f);
        boxRect.pivot = new Vector2(0.5f, 0f);
        boxRect.offsetMin = new Vector2(96f, 36f);
        boxRect.offsetMax = new Vector2(-96f, 276f);

        Image image = boxObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = new Color(0f, 0f, 0f, 0.78f);

        CreateText(boxObject.transform, "SpeakerName", font, 28, new Color(1f, 0.94f, 0.72f, 1f), TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, -56f), new Vector2(-32f, -12f), "Speaker");
        CreateText(boxObject.transform, "DialogueContent", font, 36, Color.white, TextAnchor.UpperLeft,
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(32f, 24f), new Vector2(-32f, -64f), "Dialogue line");
    }

    private static void CreateText(
        Transform parent,
        string objectName,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        TextAnchor anchor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string textValue)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = ConvertAlignment(anchor);
        text.color = color;
        text.text = textValue;
        text.richText = true;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        if (objectName == "DialogueContent")
        {
            TMP_LinkAnimator animator = textObject.GetComponent<TMP_LinkAnimator>();
            if (animator == null)
            {
                animator = textObject.AddComponent<TMP_LinkAnimator>();
            }

            animator.BindText(text);
        }
    }

    private static void ConvertLegacyTextToTmp(Transform target, TMP_FontAsset font, bool addLinkAnimator)
    {
        if (target == null) return;

        TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Text legacy = target.GetComponent<Text>();
            if (legacy == null) return;

            string textValue = legacy.text;
            int fontSize = legacy.fontSize;
            Color color = legacy.color;
            bool raycastTarget = legacy.raycastTarget;
            TextAlignmentOptions alignment = ConvertAlignment(legacy.alignment);
            bool wordWrap = legacy.horizontalOverflow != HorizontalWrapMode.Overflow;
            TextOverflowModes overflow = legacy.verticalOverflow == VerticalWrapMode.Overflow
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;

            Object.DestroyImmediate(legacy, true);
            tmp = target.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = textValue;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.raycastTarget = raycastTarget;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wordWrap;
            tmp.overflowMode = overflow;
        }

        tmp.font = font;
        tmp.richText = true;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        TMP_LinkAnimator existingAnimator = target.GetComponent<TMP_LinkAnimator>();
        if (addLinkAnimator)
        {
            if (existingAnimator == null)
            {
                existingAnimator = target.gameObject.AddComponent<TMP_LinkAnimator>();
            }

            existingAnimator.BindText(tmp);
        }
        else if (existingAnimator != null)
        {
            Object.DestroyImmediate(existingAnimator, true);
        }
    }

    private static TMP_FontAsset LoadDefaultTmpFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null) return font;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTmpFontPath);
    }

    private static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.TopLeft;
        }
    }

    private static Sprite EnsureWhiteSprite()
    {
        if (!File.Exists(WhiteTexturePath))
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[]
            {
                Color.white, Color.white,
                Color.white, Color.white
            });
            texture.Apply();

            byte[] pngBytes = texture.EncodeToPNG();
            File.WriteAllBytes(WhiteTexturePath, pngBytes);
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(WhiteTexturePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(WhiteTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteTexturePath);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
