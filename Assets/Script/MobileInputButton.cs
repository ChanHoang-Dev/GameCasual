using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MobileInputButton : MonoBehaviour, IPointerClickHandler
{
    public enum DirectionType { Up, Down, Left, Right }

    [Header("Tham chiếu")]
    [SerializeField] private PlayerController2D player;

    [Header("Loại nút")]
    [SerializeField] private bool isAttackButton = false;
    [SerializeField] private DirectionType directionType = DirectionType.Down;

    private Vector2 DirVector
    {
        get
        {
            switch (directionType)
            {
                case DirectionType.Up: return Vector2.up;
                case DirectionType.Down: return Vector2.down;
                case DirectionType.Left: return Vector2.left;
                case DirectionType.Right: return Vector2.right;
                default: return Vector2.zero;
            }
        }
    }

    void Reset()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController2D>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (player == null) return;
 
        if (isAttackButton)
        {
            player.PressMobileAttack();
        }
        else
        {
            player.PressMobileMove(DirVector);
        }
    }
}