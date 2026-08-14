using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("Level Info")]
    [SerializeField] private int currentLevel = 1;//

    [Header("Win Condition")]
    [SerializeField] private int requiredScore = 100;

    [Header("UI - Score")]
    [SerializeField] private TextMeshProUGUI currentScoreText;
    [SerializeField] private TextMeshProUGUI requiredScoreText;

    [Header("UI - PanelsWin")]
    [SerializeField] private GameObject youWinPanel;
    [SerializeField] private Button nextLevelButton;   // chuyển màn kế
    [SerializeField] private Button mainMenuButtonWin; // về sảnh từ Win panel
    [Header("UI - PanelsLose")]
    [SerializeField] private GameObject youLosePanel;
    [SerializeField] private GameObject reviveSection; // section chứa nút hồi sinh
    [SerializeField] private Button reviveButton;        // chơi
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownReviveBtn = 10f; // thời gian đếm ngược trước khi về sảnh
    [SerializeField] private int maxReviveCount = 2; // số lần hồi sinh tối đa
    [SerializeField] private GameObject retrySection; // section chơi lại
    [SerializeField] private Button retryButton;        // chơi lại màn hiện tại
    [SerializeField] private Button mainMenuButtonLose; // về sảnh từ Lose panel
    [Header("UI - PanelPause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButtonPause; // về sảnh từ Pause panel
    [Header("UI - TextLevel")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("References")]
    [SerializeField] private PlayerController2D player;

    [Header("Ad Loading / Failed")]
    [SerializeField] private GameObject adLoadingPanel; // panel hiển thị khi đang chờ load quảng cáo
    [SerializeField] private GameObject adFailedPanel;  // panel hiển thị khi ad lỗi/hết giờ chờ
    [SerializeField] private TextMeshProUGUI adFailedText; // text mô tả lỗi trong adFailedPanel
    [SerializeField] private Button adFailedBackButton;    // nút "Quay lại" trong adFailedPanel

    private int currentScore = 0;
    private bool isGameOver = false;

    public bool IsGameOver => isGameOver;
    private int reviveUsed = 0; // số lần hồi sinh đã sử dụng
    private float timer;
    private bool isCountingDown = false;
    private Vector2 deathPosition; // vị trí chết của player

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Ẩn cả 2 panel lúc bắt đầu
        youWinPanel?.SetActive(false);
        youLosePanel?.SetActive(false);
        adLoadingPanel?.SetActive(false);
        adFailedPanel?.SetActive(false);

        // Hiển thị điểm yêu cầu
        if (requiredScoreText != null)
            requiredScoreText.text = $"Mục tiêu: {requiredScore}";

        UpdateScoreUI();
        nextLevelButton?.onClick.AddListener(OnClickNextLevel);
        mainMenuButtonWin?.onClick.AddListener(OnClickMainMenu);
        reviveButton?.onClick.AddListener(OnClickRevive);
        retryButton?.onClick.AddListener(OnClickRetry);
        pauseButton?.onClick.AddListener(OnClickPause);
        mainMenuButtonLose?.onClick.AddListener(OnClickMainMenu);
        resumeButton?.onClick.AddListener(OnClickResume);
        mainMenuButtonPause?.onClick.AddListener(OnClickMainMenu);
        adFailedBackButton?.onClick.AddListener(OnClickAdFailedBack);

        // Lắng nghe event chết từ HealthManager
        if (HealthManager.Instance != null)
            HealthManager.Instance.OnDeath += TriggerLose;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string displayName = Regex.Replace(sceneName, "([a-z])([A-Z])", "$1 $2"); // thêm khoảng trắng giữa chữ thường và chữ hoa
        if (levelText != null)
            levelText.text = displayName;
    }
    void Update()
    {
        if (!isCountingDown) return;
        timer -= Time.unscaledDeltaTime; // sử dụng unscaledDeltaTime để bỏ qua Time.timeScale
        countdownText.text = $"Hồi sinh trong: {Mathf.Ceil(timer)}s";

        if (timer <= 0f)
            OnCountdownExpired();
    }

    void OnDestroy()
    {
        if (HealthManager.Instance != null)
            HealthManager.Instance.OnDeath -= TriggerLose;
    }
    public void OnScoreChanged(int newScore)
    {
        if (isGameOver) return;

        currentScore = newScore;
        UpdateScoreUI();

        if (currentScore >= requiredScore)
            TriggerWin();
    }

    private void TriggerWin()
    {
        if (isGameOver) return;
        isGameOver = true;

        LevelManager.Instance.UnlockNextLevel(currentLevel);
        youWinPanel?.SetActive(true);
        LockGame();
        AudioManager.Instance?.PlayWin();

        Debug.Log("YOU WIN!");
    }

    private void TriggerLose()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (player != null)
            deathPosition = player.transform.position; // lưu vị trí chết của player

        youLosePanel?.SetActive(true);
        LockGame();
        AudioManager.Instance?.PlayLose();

        Debug.Log("YOU LOSE!");
        if (reviveUsed < maxReviveCount)
        {
            ShowReviveOption();
        }
        else
        {
            ShowRetryOption();
        }
    }
    private void ShowReviveOption()
    {
        reviveSection?.SetActive(true);
        retrySection?.SetActive(false);
        adLoadingPanel?.SetActive(false);
        adFailedPanel?.SetActive(false);
        timer = countdownReviveBtn;
        isCountingDown = true;
    }
    private void OnCountdownExpired()
    {
        isCountingDown = false;
        ShowRetryOption();
    }
    private void ShowRetryOption()
    {
        reviveSection?.SetActive(false);
        retrySection?.SetActive(true);
        adLoadingPanel?.SetActive(false);
        adFailedPanel?.SetActive(false);
        isCountingDown = false;
    }
    private void LockGame()
    {
        Time.timeScale = 0f; // freeze toàn bộ game
    }
    public void UnlockGame()
    {
        Time.timeScale = 1f;
    }

    private void UpdateScoreUI()
    {
        if (currentScoreText != null)
            currentScoreText.text = $"Điểm: {currentScore}";
    }
    public void OnClickNextLevel()
    {
        LevelManager.Instance.LoadNextLevel(currentLevel);
    }
    public void OnClickRevive()
    {
        Debug.Log("=== OnClickRevive() CALLED ===");
        isCountingDown = false; // dừng đếm ngược khi đang chờ/xem ads

        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("AdsManager.Instance is NULL");
            ShowAdFailedPanel("Không thể tải quảng cáo lúc này.");
            return;
        }

        // Ẩn panel hồi sinh, hiện panel đang chờ, khóa toàn bộ tương tác
        SetReviveInteractable(false);
        reviveSection?.SetActive(false);
        adFailedPanel?.SetActive(false);
        adLoadingPanel?.SetActive(true);

        Debug.Log("Calling ShowRewardedAd...");
        AdsManager.Instance.ShowRewardedAd(
            onSuccess: DoRevive,
            onFail: () => ShowAdFailedPanel("Quảng cáo chưa sẵn sàng. Vui lòng thử lại sau."),
            onWaitStart: () => { if (adLoadingPanel) adLoadingPanel.SetActive(true); },
            onWaitEnd: () => { if (adLoadingPanel) adLoadingPanel.SetActive(false); }
        );
    }

    private void DoRevive()
    {
        adLoadingPanel?.SetActive(false);
        adFailedPanel?.SetActive(false);
        SetReviveInteractable(true);

        reviveUsed++;
        youLosePanel?.SetActive(false);
        isGameOver = false;
        UnlockGame();
        if (player != null)
        {
            player.transform.position = deathPosition;
        }
        HealthManager.Instance?.ReviveToFullHealth();

        // Fix mất nhạc nền sau khi xem xong quảng cáo hồi sinh
        AudioManager.Instance?.PlayMusicFor(MusicType.GamePlay, forceRestart: true);

        Debug.Log($"Player revived ({reviveUsed}/{maxReviveCount}, position: {deathPosition})");
    }


    private void ShowAdFailedPanel(string message)
    {
        adLoadingPanel?.SetActive(false);
        reviveSection?.SetActive(false);

        if (adFailedText != null)
            adFailedText.text = message;

        adFailedPanel?.SetActive(true);
        // Không mở lại reviveButton ở đây - chỉ mở khi bấm "Quay lại",
        // để tránh người chơi bấm hồi sinh trong lúc panel lỗi đang hiện.
    }
    private void OnClickAdFailedBack()
    {
        adFailedPanel?.SetActive(false);
        SetReviveInteractable(true);

        // Nếu vẫn còn lượt hồi sinh và game chưa chuyển sang trạng thái khác thì quay lại panel hồi sinh
        if (reviveUsed < maxReviveCount && isGameOver)
        {
            ShowReviveOption();
        }
        else
        {
            ShowRetryOption();
        }
    }

    private void SetReviveInteractable(bool value)
    {
        if (reviveButton != null)
            reviveButton.interactable = value;
    }

    public void OnClickRetry()
    {
        LevelManager.Instance.ReloadCurrentLevel();
    }
    public void OnClickPause()
    {
        pausePanel?.SetActive(true);
        LockGame();
    }
    public void OnClickResume()
    {
        pausePanel?.SetActive(false);
        UnlockGame();
    }
    public void OnClickMainMenu()
    {
        LevelManager.Instance.LoadMainMenu();
    }
}