using UnityEngine;
using Unity.Services.LevelPlay;
using System.Collections;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("LevelPlay Config")]
    [SerializeField] private string androidAppKey = "273361f85";
    [SerializeField] private string iosAppKey = "273361f85";
    [SerializeField] private string rewardedAdUnitId = "k5gp83wqs8u2ott2";
    private LevelPlayRewardedAd rewardedAd;
    private bool isSdkInitialized = false;
    private bool isAdLoaded = false;

    private System.Action onAdSuccess;
    private System.Action onAdFail;
    [Header("Rewarded Ad Wait Settings")]
    [SerializeField] private float maxWaitForAdSeconds = 10f;
    [SerializeField] private float checkInterval = 0.3f;

    private Coroutine waitAdCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject); // sống xuyên suốt các scene, tránh mất Instance khi reload level
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Scene mới được load (ví dụ retry/reload level) -> hủy coroutine chờ ad cũ,
        // vì nó có thể đang giữ callback trỏ tới UI của scene trước đã bị destroy.
        if (waitAdCoroutine != null)
        {
            StopCoroutine(waitAdCoroutine);
            waitAdCoroutine = null;
            Debug.Log("[Ads] Scene đổi -> hủy coroutine chờ ad cũ.");
        }
        // Callback cũ cũng không còn hợp lệ với scene mới, xóa để tránh gọi nhầm UI cũ
        onAdSuccess = null;
        onAdFail = null;
    }

    void Start()
    {
        string appKey = Application.platform == RuntimePlatform.IPhonePlayer ? iosAppKey : androidAppKey;
        Debug.Log($"[Ads] Initializing LevelPlay with AppKey={appKey}");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        LevelPlay.Init(appKey);
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
        UnsubscribeRewardedEvents();
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        isSdkInitialized = true;
        Debug.Log("[Ads] LevelPlay init SUCCESS");
        CreateAndLoadRewardedAd();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        isSdkInitialized = false;
        Debug.LogError($"[Ads] LevelPlay init FAILED: {error}");
    }

    private void CreateAndLoadRewardedAd()
    {
        if (string.IsNullOrEmpty(rewardedAdUnitId) || rewardedAdUnitId.StartsWith("DIEN_"))
        {
            Debug.LogError("[Ads] Chưa điền Rewarded Ad Unit ID trong Inspector!");
            return;
        }

        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;

        LoadAd();
    }

    private void UnsubscribeRewardedEvents()
    {
        if (rewardedAd == null) return;
        rewardedAd.OnAdLoaded -= OnAdLoaded;
        rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        rewardedAd.OnAdDisplayed -= OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        rewardedAd.OnAdRewarded -= OnAdRewarded;
        rewardedAd.OnAdClosed -= OnAdClosed;
    }

    private void LoadAd()
    {
        if (rewardedAd == null) return;
        isAdLoaded = false;
        Debug.Log("[Ads] LoadAd() called");
        rewardedAd.LoadAd();
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        isAdLoaded = true;
        Debug.Log("[Ads] Rewarded ad LOADED");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        isAdLoaded = false;
        Debug.LogWarning($"[Ads] Rewarded ad LOAD FAILED: {error}");
        // Thử load lại sau vài giây, tránh app kẹt mãi không có ad
        Invoke(nameof(LoadAd), 5f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[Ads] Rewarded ad displayed");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"[Ads] Rewarded ad display FAILED: {error}");
        onAdFail?.Invoke();
        LoadAd(); // load sẵn ad tiếp theo

        ResumeMusicAfterAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log("[Ads] Rewarded ad completed - user should be rewarded");
        onAdSuccess?.Invoke();
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("[Ads] Rewarded ad closed");
        LoadAd(); // load sẵn ad tiếp theo

        ResumeMusicAfterAd();
    }

    public void ShowRewardedAd(System.Action onSuccess, System.Action onFail,
    System.Action onWaitStart = null, System.Action onWaitEnd = null)
    {
        onAdSuccess = onSuccess;
        onAdFail = onFail;

        if (!isSdkInitialized)
        {
            Debug.LogWarning("[Ads] SDK chưa init xong.");
            onFail?.Invoke();
            return;
        }

        // Nếu ad đã sẵn sàng -> show ngay, không cần chờ
        if (rewardedAd != null && isAdLoaded)
        {
            Debug.Log("[Ads] Showing rewarded ad now...");
            rewardedAd.ShowAd();
            return;
        }

        // Ad chưa sẵn sàng -> chờ load trong khoảng thời gian giới hạn
        Debug.Log("[Ads] Ad chưa load xong, bắt đầu chờ...");
        if (waitAdCoroutine != null) StopCoroutine(waitAdCoroutine);
        waitAdCoroutine = StartCoroutine(WaitForAdThenShow(onWaitStart, onWaitEnd));
    }
    private IEnumerator WaitForAdThenShow(System.Action onWaitStart, System.Action onWaitEnd)
    {
        onWaitStart?.Invoke(); // báo cho UI hiện loading spinner

        float elapsed = 0f;
        while (elapsed < maxWaitForAdSeconds)
        {
            if (rewardedAd != null && isAdLoaded)
            {
                onWaitEnd?.Invoke(); // tắt loading spinner
                Debug.Log("[Ads] Ad đã load xong trong lúc chờ -> Showing now...");
                rewardedAd.ShowAd();
                waitAdCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;
        }

        // Hết thời gian chờ mà vẫn chưa có ad
        onWaitEnd?.Invoke();
        Debug.LogWarning("[Ads] Chờ quá lâu, ad vẫn chưa load xong -> Fail.");
        onAdFail?.Invoke();
        waitAdCoroutine = null;
    }
    private void ResumeMusicAfterAd()
    {
        AudioListener.pause = false;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeMusic();
        }
    }
}