using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private const string UnlockedKey = "MaxLevelUnlocked"; // key lưu trong PlayerPrefs
    private const int TotalLevels = 20;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int MaxUnlockedLevel => PlayerPrefs.GetInt(UnlockedKey, 1);

    public bool IsLevelUnlocked(int level) => level <= MaxUnlockedLevel;

    public int TotalLevelCount => TotalLevels;

    public void UnlockNextLevel(int currentLevel)
    {
        int next = currentLevel + 1;
        if (next > TotalLevels) return; // đã là màn cuối

        if (next > MaxUnlockedLevel)
        {
            PlayerPrefs.SetInt(UnlockedKey, next);
            PlayerPrefs.Save(); // ghi xuống disk ngay lập tức
            Debug.Log($"Đã mở khóa Level {next}");
        }
    }

    public void LoadLevel(int level)
    {
        if (!IsLevelUnlocked(level))
        {
            Debug.LogWarning($"Level {level} chưa được mở khóa!");
            return;
        }
        Time.timeScale = 1f; // reset timeScale phòng trường hợp đang bị freeze
        SceneManager.LoadScene($"Level{level}");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void LoadNextLevel(int currentLevel)
    {
        int next = currentLevel + 1;
        if (next > TotalLevels)
        {
            LoadMainMenu(); // hết game → về sảnh
            return;
        }
        LoadLevel(next);
    }

    public void ReloadCurrentLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    [ContextMenu("Reset All Progress")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedKey);
        PlayerPrefs.Save();
        Debug.Log("Đã reset toàn bộ tiến trình.");
    }
}