using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private enum MenuState { Start, Credits, LevelSelect }
    private MenuState currentState = MenuState.Start;

    [Header("面板引用")]
    public GameObject startPanel;
    public GameObject creditsPanel;
    public GameObject levelSelectPanel;
    public GameObject darkOverlay; // 半透明遮罩

    [Header("选关界面引用")]
    public Transform levelGrid;
    public GameObject levelButtonPrefab;
    public Button resetSaveButton;

    [Header("开始界面按钮引用")]
    public Button startBtn;
    public Button creditsBtn;
    public Button quitBtn;

    private List<GameObject> spawnedLevelButtons = new List<GameObject>();

    private void Start()
    {
        // 确保游戏状态处于主菜单
        GameManager.Instance.ChangeState(GameState.MainMenu);

        // 绑定开始界面按钮事件
        startBtn.onClick.AddListener(OpenLevelSelect);
        creditsBtn.onClick.AddListener(OpenCredits);
        quitBtn.onClick.AddListener(QuitGame);

        resetSaveButton.onClick.AddListener(ResetSave);

        ShowPanel(MenuState.Start);
    }

    private void Update()
    {
        // 如果正在转场中，忽略一切输入
        if (GameManager.Instance.CurrentState == GameState.Transitioning) return;

        // 使用 Input System 的快捷按键检测 ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscKey();
        }
    }

    private void HandleEscKey()
    {
        if (currentState == MenuState.Credits || currentState == MenuState.LevelSelect)
        {
            ShowPanel(MenuState.Start);
        }
    }

    #region 状态切换逻辑
    private void OpenLevelSelect() => ShowPanel(MenuState.LevelSelect);
    private void OpenCredits() => ShowPanel(MenuState.Credits);
    private void QuitGame()
    {
        Debug.Log("退出游戏");
        Application.Quit();
    }

    private void ShowPanel(MenuState state)
    {
        currentState = state;

        // 只有选关界面不显示半透明遮罩
        darkOverlay.SetActive(state != MenuState.LevelSelect);

        startPanel.SetActive(state == MenuState.Start);
        creditsPanel.SetActive(state == MenuState.Credits);
        levelSelectPanel.SetActive(state == MenuState.LevelSelect);

        if (state == MenuState.LevelSelect)
        {
            RefreshLevelGrid();
        }
    }
    #endregion

    #region 选关与数据逻辑
    private void RefreshLevelGrid()
    {
        // 清理旧按钮
        foreach (var btn in spawnedLevelButtons) Destroy(btn);
        spawnedLevelButtons.Clear();

        int totalLevels = DataManager.Instance.GetTotalLevelCount();
        int unlockedIndex = DataManager.Instance.UnlockedLevelIndex;

        for (int i = 0; i < totalLevels; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, levelGrid);
            spawnedLevelButtons.Add(btnObj);

            Button btn = btnObj.GetComponent<Button>();
            TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();

            txt.text = (i + 1).ToString();

            if (i <= unlockedIndex)
            {
                btn.interactable = true;
                int levelIndex = i; // 闭包陷阱：必须缓存局部变量
                btn.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
            }
            else
            {
                btn.interactable = false; // 未解锁置灰
            }
        }
    }

    private void OnLevelButtonClicked(int index)
    {
        if (GameManager.Instance.CurrentState == GameState.Transitioning) return;

        // 调用阶段一的转场加载逻辑
        SceneFlowManager.Instance.LoadLevel(index);
    }

    private void ResetSave()
    {
        DataManager.Instance.ResetProgress();
        RefreshLevelGrid(); // 刷新网格状态
    }
    #endregion
}
