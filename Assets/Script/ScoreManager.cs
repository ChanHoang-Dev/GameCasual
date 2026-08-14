using UnityEngine;
using TMPro; // nếu dùng TextMeshPro để hiển thị điểm

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText; 
    
    private int totalScore = 0;
    void Awake()
    {
        if (scoreText != null)
            scoreText.text = $"Score: 0";
    }

    public void AddScore(int amount)
    {
        if(GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        totalScore += amount;
        GameManager.Instance?.OnScoreChanged(totalScore);
    }
}