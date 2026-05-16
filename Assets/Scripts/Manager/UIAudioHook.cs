using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIAudioHook : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HookButtonsInScene;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HookButtonsInScene;
    }

    private void HookButtonsInScene(Scene scene, LoadSceneMode mode)
    {
        // 查找场景中所有的 Button 组件（包含被隐藏的）
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in buttons)
        {
            // 防止重复绑定
            btn.onClick.RemoveListener(TriggerClickSound);
            btn.onClick.AddListener(TriggerClickSound);
        }
    }

    private void TriggerClickSound()
    {
        this.TriggerEvent(EventName.OnUIClick);
    }
}
