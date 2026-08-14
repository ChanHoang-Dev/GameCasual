using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedUI : MonoBehaviour
{
    [System.Serializable]
    public class LevelSlot
    {
        [Tooltip("GameObject vàng - đã mở khóa")]
        public GameObject imageSelect;

        [Tooltip("Text số màn bên trong ImageSelect")]
        public TextMeshProUGUI levelText;

        [Tooltip("Button bên trong ImageSelect")]
        public Button selectButton;

        [Tooltip("GameObject xám - chưa mở khóa")]
        public GameObject imageSelectLock;
    }

    [Header("Level Slots")]
    [SerializeField] private LevelSlot[] levelSlots;

    void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (LevelManager.Instance == null) return;

        for (int i = 0; i < levelSlots.Length; i++)
        {
            LevelSlot slot = levelSlots[i];
            if (slot == null) continue;

            int levelIndex = i + 1;
            bool unlocked = LevelManager.Instance.IsLevelUnlocked(levelIndex);

            // Swap 2 GameObject theo trạng thái unlock
            slot.imageSelect?.SetActive(unlocked);
            slot.imageSelectLock?.SetActive(!unlocked);

            if (unlocked)
            {
                // Set text số màn
                if (slot.levelText != null)
                    slot.levelText.text = levelIndex.ToString();

                // Gắn sự kiện click — capture levelIndex để tránh closure bug
                if (slot.selectButton != null)
                {
                    slot.selectButton.onClick.RemoveAllListeners();
                    int capturedLevel = levelIndex;
                    slot.selectButton.onClick.AddListener(() =>
                        LevelManager.Instance.LoadLevel(capturedLevel));
                }
            }
        }
    }
}