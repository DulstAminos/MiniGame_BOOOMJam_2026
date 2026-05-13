using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool isQuitting = false; // 标记程序是否正在退出
    public static T Instance
    {
        get
        {
            // 如果程序正在退出，直接返回 null，防止在退出阶段自动创建新对象
            if (isQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject); // 切换场景时不销毁
        }
        else if (instance != this)
        {
            Destroy(gameObject); // 保证全游戏只有一个实例
        }
    }

    // 当程序退出时触发
    protected virtual void OnApplicationQuit()
    {
        isQuitting = true;
    }

    // 当物体被销毁时触发
    protected virtual void OnDestroy()
    {
        isQuitting = true;
    }
}
