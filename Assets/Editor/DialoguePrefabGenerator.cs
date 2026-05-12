#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DialoguePrefabGenerator
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string DialogueFolder = "Assets/Resources/Dialogue";
    private const string WhiteTexturePath = "Assets/Resources/Dialogue/SimpleDialogueWhite.png";
    private const string PrefabPath = "Assets/Resources/Dialogue/SimpleSceneDialogueCanvas.prefab";

    [MenuItem("Tools/Dialogue/Generate Simple Dialogue Canvas Prefab")]
    public static void GeneratePrefabAsset()
    {
        EnsureFolder(ResourcesFolder);
        EnsureFolder(DialogueFolder);

        Sprite whiteSprite = EnsureWhiteSprite();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
        CreatePortrait(root.transform, whiteSprite, "LeftPortrait", new Color(0.83f, 0.46f, 0.46f, 0.95f), new Vector2(52f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), "LeftPortraitLabel", font);
        CreatePortrait(root.transform, whiteSprite, "RightPortrait", new Color(0.46f, 0.63f, 0.87f, 0.95f), new Vector2(-52f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), "RightPortraitLabel", font);
        CreateDialogueBox(root.transform, whiteSprite, font);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated dialogue prefab at {PrefabPath}");
    }

    [InitializeOnLoadMethod]
    private static void AutoGeneratePrefabIfMissing()
    {
        EditorApplication.delayCall += TryAutoGeneratePrefab;
    }

    private static void TryAutoGeneratePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(PrefabPath) && File.Exists(WhiteTexturePath)) return;

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
        Font font)
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

        GameObject labelObject = new GameObject(labelName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(portraitObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 24f);
        labelRect.offsetMax = new Vector2(-24f, -24f);

        Text label = labelObject.GetComponent<Text>();
        label.font = font;
        label.fontSize = 34;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = objectName.Contains("Left") ? "Left" : "Right";
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private static void CreateDialogueBox(Transform parent, Sprite sprite, Font font)
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
        Font font,
        int fontSize,
        Color color,
        TextAnchor anchor,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 offsetMin,
        Vector2 offsetMax,
        string textValue)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.text = textValue;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
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
