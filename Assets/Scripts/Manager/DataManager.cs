using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoSingleton<DataManager>
{
    [Header("关卡配置 (按顺序填入)")]
    public List<LevelConfig> levels = new List<LevelConfig>();

    // 当前解锁的最高关卡索引 (0 表示只解锁了第一关)
    public int UnlockedLevelIndex { get; private set; }

    // 当前正在游玩的关卡索引
    public int CurrentPlayingIndex { get; set; }

    protected override void Awake()
    {
        base.Awake();
        LoadProgress();
    }

    /// <summary> 读取存档 </summary>
    public void LoadProgress()
    {
        UnlockedLevelIndex = PlayerPrefs.GetInt("UnlockedLevelIndex", 0);
    }

    /// <summary> 通关时解锁下一关 </summary>
    public void UnlockNextLevel()
    {
        if (CurrentPlayingIndex >= UnlockedLevelIndex && UnlockedLevelIndex < levels.Count - 1)
        {
            UnlockedLevelIndex++;
            PlayerPrefs.SetInt("UnlockedLevelIndex", UnlockedLevelIndex);
            PlayerPrefs.Save();
        }
    }

    /// <summary> 重置存档（仅第一关可玩） </summary>
    public void ResetProgress()
    {
        UnlockedLevelIndex = 0;
        PlayerPrefs.SetInt("UnlockedLevelIndex", 0);
        PlayerPrefs.Save();
    }

    /// <summary> 获取关卡总数 </summary>
    public int GetTotalLevelCount() => levels.Count;
}
