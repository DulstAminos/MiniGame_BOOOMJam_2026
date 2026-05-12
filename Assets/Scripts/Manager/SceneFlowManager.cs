using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoSingleton<SceneFlowManager>
{
    // 主菜单场景名称常量
    private const string MainMenuSceneName = "MainMenu";

    [Header("转场配置")]
    public float transitionDuration = 0.5f; // 转场动画时间
    public float waitTimeBetween = 0.2f;    // 黑屏停留时间

    [Header("Sprite Mask 引用")]
    public Transform maskTransform;         // 拖入 CircleMask 的 Transform
    public GameObject transitionSystemRoot; // 拖入 TransitionSystem 根节点，用于开关显示

    // 根据圆形素材大小调整 maxScale，确保放大后屏幕完全没有黑边
    private Vector3 maxScale = new Vector3(40f, 40f, 1f);
    private Vector3 minScale = Vector3.zero;
    private float startZPosition;

    protected override void Awake()
    {
        base.Awake();
        if (maskTransform != null)
        {
            maskTransform.localScale = maxScale;
        }
        // 游戏一开始先隐藏转场系统，避免在编辑器里遮挡视线
        if (transitionSystemRoot != null)
        {
            // 记录初始的 Z 轴
            startZPosition = transitionSystemRoot.transform.position.z;
            transitionSystemRoot.SetActive(false);
        }
    }

    /// <summary> 加载关卡 </summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= DataManager.Instance.levels.Count) return;

        DataManager.Instance.CurrentPlayingIndex = levelIndex;
        string sceneName = DataManager.Instance.levels[levelIndex].sceneName;
        StartCoroutine(TransitionAndLoad(sceneName, GameState.Playing));
    }

    /// <summary> 回到主菜单 </summary>
    public void LoadMainMenu()
    {
        StartCoroutine(TransitionAndLoad(MainMenuSceneName, GameState.MainMenu));
    }

    /// <summary> 重新加载当前关卡 (死亡/R键) </summary>
    public void ReloadCurrentLevel(float delay = 0f)
    {
        StartCoroutine(ReloadRoutine(delay));
    }

    private IEnumerator ReloadRoutine(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay); // 注意：这里用受时间缩放影响的等待

        string sceneName = DataManager.Instance.levels[DataManager.Instance.CurrentPlayingIndex].sceneName;
        yield return StartCoroutine(TransitionAndLoad(sceneName, GameState.Playing));
    }

    private IEnumerator TransitionAndLoad(string sceneName, GameState targetState)
    {
        // 1. 设置状态为转场中，拦截输入并暂停时间
        GameManager.Instance.ChangeState(GameState.Transitioning);
        Time.timeScale = 0f;

        // 激活转场物体
        transitionSystemRoot.SetActive(true);
        // 初始化位置
        UpdateTransitionPosition();

        // 2. 转出动画 (圆圈收缩，画面变黑)
        float timer = 0f;
        while (timer < transitionDuration)
        {
            // 在转场时，确保转场中心始终跟随主摄像机
            UpdateTransitionPosition();

            timer += Time.unscaledDeltaTime; // 使用不受TimeScale影响的时间
            float percent = timer / transitionDuration;
            maskTransform.localScale = Vector3.Lerp(maxScale, minScale, percent);
            yield return null;
        }
        maskTransform.localScale = minScale;

        // 3. 停顿与异步加载场景
        yield return new WaitForSecondsRealtime(waitTimeBetween);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // --- 场景加载完毕 ---
        // 对齐新场景的摄像机
        if (Camera.main != null)
            transitionSystemRoot.transform.position = Camera.main.transform.position;

        // 触发初始化玩家位置事件 (供 LevelManager 监听)
        this.TriggerEvent(EventName.OnSceneLoaded);

        // 4. 转入动画 (圆圈扩散，画面显露)
        timer = 0f;
        while (timer < transitionDuration)
        {
            UpdateTransitionPosition();

            timer += Time.unscaledDeltaTime;
            float percent = timer / transitionDuration;
            maskTransform.localScale = Vector3.Lerp(minScale, maxScale, percent);
            yield return null;
        }
        maskTransform.localScale = maxScale;

        // 隐藏转场物体，节省渲染开销
        transitionSystemRoot.SetActive(false);

        // 5. 恢复目标状态和时间
        GameManager.Instance.ChangeState(targetState);
    }

    private void UpdateTransitionPosition()
    {
        if (Camera.main != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            // 只同步 X 和 Y，保持 Z 轴不变 (或者直接设为 0)
            transitionSystemRoot.transform.position = new Vector3(camPos.x, camPos.y, startZPosition);
        }
    }
}
