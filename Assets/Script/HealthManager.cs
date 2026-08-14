using UnityEngine;


/// </summary>
public class HealthManager : MonoBehaviour
{
    public static HealthManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private float drainPerSecond = 0.1f; // tốc độ hao mỗi giây (0.1 = ~10s/tim)
    [SerializeField] private float heartValuePerPickup = 1f; // nhặt 1 Heart = cộng bao nhiêu

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public int MaxHearts => maxHearts;

    // Event để HealthUI lắng nghe, cập nhật UI khi máu thay đổi
    public System.Action<float> OnHealthChanged;
    public System.Action OnDeath;

    void Awake()
    {
        // Singleton đơn giản
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject);
            return; 
        }
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHearts; // bắt đầu với 3 tim đầy
        OnHealthChanged?.Invoke(currentHealth);
    }

    void Update()
    {
        if (currentHealth <= 0f) return;

        // Hao dần theo thời gian
        SetHealth(currentHealth - drainPerSecond * Time.deltaTime);
    }

    public void HealFromPickup()
    {
        SetHealth(currentHealth + heartValuePerPickup);
    }

    /// Gọi khi nhân vật chạm lửa — mất đúng 1 tim đầy.
    public void TakeFire()
    {
        SetHealth(currentHealth - 1f);
    }
    public void ReviveToFullHealth()
    {
        SetHealth(maxHearts);
    }

    /// Set máu với clamp và fire event.
    private void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHearts);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0f)
            OnDeath?.Invoke();
    }
}