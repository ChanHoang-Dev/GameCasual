using UnityEngine;
using UnityEngine.UI;


public class HealthUI : MonoBehaviour
{
    [Header("Heart Images")]
    [Tooltip("Kéo 3 Image UI vào đây theo thứ tự: [0]=tim 1 (trái), [1]=tim 2, [2]=tim 3 (phải)")]
    [SerializeField] private Image[] heartImages; // 3 phần tử

    void Start()
    {
        if (HealthManager.Instance == null) return;

        // Lắng nghe event từ HealthManager
        HealthManager.Instance.OnHealthChanged += UpdateHeartUI;
        HealthManager.Instance.OnDeath += OnPlayerDeath;

        // Hiển thị trạng thái ban đầu
        UpdateHeartUI(HealthManager.Instance.CurrentHealth);
    }

    void OnDestroy()
    {
        if (HealthManager.Instance == null) return;
        HealthManager.Instance.OnHealthChanged -= UpdateHeartUI;
        HealthManager.Instance.OnDeath -= OnPlayerDeath;
    }

    /// <summary>
    /// Cập nhật fill amount của từng tim dựa trên currentHealth (0.0 → 3.0).
    /// Tim hao từ phải sang trái:
    ///   health = 2.3 → tim[0]=1.0, tim[1]=1.0, tim[2]=0.3
    ///   health = 1.7 → tim[0]=1.0, tim[1]=0.7, tim[2]=0.0
    ///   health = 0.4 → tim[0]=0.4, tim[1]=0.0, tim[2]=0.0
    /// </summary>
    private void UpdateHeartUI(float currentHealth)
    {
        if (heartImages == null || heartImages.Length == 0) return;

        int maxHearts = heartImages.Length;

        for (int i = 0; i < maxHearts; i++)
        {
            if (heartImages[i] == null) continue;

            // Tim thứ i (0-indexed từ trái sang phải)
            // Fill = phần máu thuộc về tim này (clamp 0→1)
            float fillAmount = Mathf.Clamp01(currentHealth - i);
            heartImages[i].fillAmount = fillAmount;
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("Nhân vật đã chết!");
    }
}