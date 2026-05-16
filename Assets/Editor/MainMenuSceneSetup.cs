#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuSceneSetup
{
    private const string MainMenuSceneName = "MainMenu";
    private const string LevelButtonPrefabPath = "Assets/Prefabs/UI/LevelButton.prefab";
    private const int FixedLevelButtonCount = 7;

    private static readonly LevelButtonLayout[] DefaultLayouts =
    {
        new LevelButtonLayout(new Vector2(-720f, 120f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(-480f, -40f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(-240f, 140f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(0f, -20f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(240f, 130f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(480f, -50f), new Vector2(220f, 220f)),
        new LevelButtonLayout(new Vector2(720f, 110f), new Vector2(220f, 220f))
    };

    static MainMenuSceneSetup()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TrySetupActiveScene;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        TrySetupScene(scene);
    }

    private static void TrySetupActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        TrySetupScene(EditorSceneManager.GetActiveScene());
    }

    private static void TrySetupScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        if (!string.Equals(scene.name, MainMenuSceneName)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (!ApplyMainMenuSetup(scene)) return;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainMenu scene setup completed: fixed level buttons and credits back button are now in the scene.");
    }

    private static bool ApplyMainMenuSetup(Scene scene)
    {
        MainMenuManager manager = scene
            .GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainMenuManager>(true))
            .FirstOrDefault();

        if (manager == null) return false;

        bool changed = false;

        changed |= EnsureManagerReferences(manager);
        changed |= EnsureLevelButtons(manager, scene);
        changed |= EnsureCreditsBackButton(manager, scene);

        if (changed)
        {
            EditorUtility.SetDirty(manager);
        }

        return changed;
    }

    private static bool EnsureManagerReferences(MainMenuManager manager)
    {
        bool changed = false;

        if (manager.startPanel == null)
        {
            manager.startPanel = manager.transform.Find("StartPanel")?.gameObject;
            changed |= manager.startPanel != null;
        }

        if (manager.creditsPanel == null)
        {
            manager.creditsPanel = manager.transform.Find("CreditsPanel")?.gameObject;
            changed |= manager.creditsPanel != null;
        }

        if (manager.levelSelectPanel == null)
        {
            manager.levelSelectPanel = manager.transform.Find("LevelSelectPanel")?.gameObject;
            changed |= manager.levelSelectPanel != null;
        }

        if (manager.darkOverlay == null)
        {
            manager.darkOverlay = manager.transform.Find("DarkOverlay")?.gameObject;
            changed |= manager.darkOverlay != null;
        }

        if (manager.levelGrid == null && manager.levelSelectPanel != null)
        {
            manager.levelGrid = manager.levelSelectPanel.transform.Find("LevelGrid");
            changed |= manager.levelGrid != null;
        }

        if (manager.levelButtonPrefab == null)
        {
            manager.levelButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelButtonPrefabPath);
            changed |= manager.levelButtonPrefab != null;
        }

        if (manager.startBtn == null && manager.startPanel != null)
        {
            manager.startBtn = manager.startPanel.transform.Find("ButtonGroup/Button_Start")?.GetComponent<Button>();
            changed |= manager.startBtn != null;
        }

        if (manager.creditsBtn == null && manager.startPanel != null)
        {
            manager.creditsBtn = manager.startPanel.transform.Find("ButtonGroup/Button_Credits")?.GetComponent<Button>();
            changed |= manager.creditsBtn != null;
        }

        if (manager.quitBtn == null && manager.startPanel != null)
        {
            manager.quitBtn = manager.startPanel.transform.Find("ButtonGroup/Button_Exit")?.GetComponent<Button>();
            changed |= manager.quitBtn != null;
        }

        if (manager.resetSaveButton == null && manager.levelSelectPanel != null)
        {
            manager.resetSaveButton = manager.levelSelectPanel.transform.Find("Reset")?.GetComponent<Button>();
            changed |= manager.resetSaveButton != null;
        }

        if (manager.fixedLevelButtons == null || manager.fixedLevelButtons.Length != FixedLevelButtonCount)
        {
            Button[] previous = manager.fixedLevelButtons;
            manager.fixedLevelButtons = new Button[FixedLevelButtonCount];
            if (previous != null)
            {
                int copyCount = Mathf.Min(previous.Length, manager.fixedLevelButtons.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    manager.fixedLevelButtons[i] = previous[i];
                }
            }
            changed = true;
        }

        if (manager.levelButtonLayouts == null || manager.levelButtonLayouts.Length != FixedLevelButtonCount)
        {
            manager.levelButtonLayouts = DefaultLayouts.ToArray();
            changed = true;
        }

        return changed;
    }

    private static bool EnsureLevelButtons(MainMenuManager manager, Scene scene)
    {
        if (manager.levelGrid == null || manager.levelButtonPrefab == null) return false;

        bool changed = false;

        LayoutGroup layoutGroup = manager.levelGrid.GetComponent<LayoutGroup>();
        if (layoutGroup != null && layoutGroup.enabled)
        {
            layoutGroup.enabled = false;
            EditorUtility.SetDirty(layoutGroup);
            changed = true;
        }

        for (int i = 0; i < FixedLevelButtonCount; i++)
        {
            string buttonName = $"Button_Level{i + 1}";
            bool created = false;

            Button button = manager.fixedLevelButtons[i];
            if (button == null)
            {
                button = manager.levelGrid.Find(buttonName)?.GetComponent<Button>();
            }

            if (button == null)
            {
                GameObject buttonObject = PrefabUtility.InstantiatePrefab(manager.levelButtonPrefab, scene) as GameObject;
                if (buttonObject == null) continue;

                buttonObject.name = buttonName;
                buttonObject.transform.SetParent(manager.levelGrid, false);
                buttonObject.SetActive(true);
                button = buttonObject.GetComponent<Button>();
                created = true;
                changed = true;
            }

            if (button == null) continue;

            if (button.name != buttonName)
            {
                button.name = buttonName;
                changed = true;
            }

            if (button.transform.parent != manager.levelGrid)
            {
                button.transform.SetParent(manager.levelGrid, false);
                changed = true;
            }

            if (manager.fixedLevelButtons[i] != button)
            {
                manager.fixedLevelButtons[i] = button;
                changed = true;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                string desiredText = (i + 1).ToString();
                if (label.text != desiredText)
                {
                    label.text = desiredText;
                    EditorUtility.SetDirty(label);
                    changed = true;
                }
            }

            if (created)
            {
                ApplyLayout(button, manager.levelButtonLayouts, i);
            }
        }

        return changed;
    }

    private static bool EnsureCreditsBackButton(MainMenuManager manager, Scene scene)
    {
        if (manager.creditsPanel == null) return false;

        bool changed = false;
        bool created = false;

        Button backButton = manager.creditsBackButton;
        if (backButton == null)
        {
            backButton = manager.creditsPanel.transform.Find("Button_Back")?.GetComponent<Button>();
        }

        if (backButton == null && manager.quitBtn != null)
        {
            GameObject backObject = Object.Instantiate(manager.quitBtn.gameObject, manager.creditsPanel.transform, false);
            backObject.name = "Button_Back";
            backObject.SetActive(true);
            backButton = backObject.GetComponent<Button>();
            created = backButton != null;
            changed |= created;
        }

        if (backButton == null) return changed;

        if (manager.creditsBackButton != backButton)
        {
            manager.creditsBackButton = backButton;
            changed = true;
        }

        if (backButton.transform.parent != manager.creditsPanel.transform)
        {
            backButton.transform.SetParent(manager.creditsPanel.transform, false);
            changed = true;
        }

        if (backButton.name != "Button_Back")
        {
            backButton.name = "Button_Back";
            changed = true;
        }

        backButton.gameObject.SetActive(true);

        if (created)
        {
            RectTransform rect = backButton.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = manager.creditsBackButtonAnchoredPosition;
                rect.sizeDelta = manager.creditsBackButtonSize;
            }

            TMP_Text label = backButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.text = manager.creditsBackButtonLabel;
                EditorUtility.SetDirty(label);
            }
        }

        return changed;
    }

    private static void ApplyLayout(Button button, LevelButtonLayout[] layouts, int index)
    {
        if (button == null || layouts == null || index < 0 || index >= layouts.Length) return;

        RectTransform rect = button.transform as RectTransform;
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = layouts[index].anchoredPosition;
        rect.sizeDelta = layouts[index].sizeDelta;
    }
}
#endif
