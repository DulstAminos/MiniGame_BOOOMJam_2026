/// <summary>
/// 单例基类
/// </summary>
/// <typeparam name="T">继承单例基类的类型</typeparam>
public class SingletonBase<T> where T : new()
{
    private static T instance;
    private static readonly object locker = new object();
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                        instance = new T();
                }
            }
            return instance;
        }
    }
}
