// CollectibleItem.cs — gắn lên từng GameObject vật phẩm
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10; // điểm cộng khi ăn, mỗi object set khác nhau
    [SerializeField] private bool isHeart = false; // nếu là trái tim thì set true, còn lại false
    
    public int ScoreValue => scoreValue;
    public bool IsHeart => isHeart;
}