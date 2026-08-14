using UnityEngine;
public class FireEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float moveTime = 0.2f;    // thời gian tween mỗi bước
    [SerializeField] private float moveDelay = 0.8f;   // delay dừng giữa các bước

    [Header("Collision")]
    [SerializeField] private LayerMask collisionLayers; // layer tường/gạch chặn lửa

    private Transform player;
    private Rigidbody2D rb;

    private bool isMoving;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float moveLerp;
    private float delayTimer;

    private bool hasDamaged;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        delayTimer = moveDelay; // bắt đầu đợi 1 chu kỳ trước khi di chuyển
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (player == null) return;

        if (isMoving)
        {
            TweenMove();
            return;
        }
        if (!hasDamaged)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < cellSize * 0.6f)
            {
                HitPlayer();
                return;
            }
        }

        // Đếm delay giữa các bước
        delayTimer -= Time.deltaTime;
        if (delayTimer > 0f) return;

        // Hết delay → tính bước đi tiếp theo
        Vector2 dir = GetNextDirection();
        if (dir == Vector2.zero) return;

        Vector2 next = (Vector2)transform.position + dir * cellSize;
        if (IsBlocked(next)) return;

        startPos = transform.position;
        targetPos = next;
        moveLerp = 0f;
        isMoving = true;
    }

    private void TweenMove()
    {
        moveLerp += Time.deltaTime / moveTime;
        moveLerp = Mathf.Clamp01(moveLerp);

        float t = Mathf.SmoothStep(0f, 1f, moveLerp);
        rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));

        if (moveLerp >= 1f)
        {
            rb.MovePosition(targetPos);
            isMoving = false;
            delayTimer = moveDelay; // reset delay sau mỗi bước
            CheckPlayerAtCurrentPosition();
        }
    }
    private Vector2 GetNextDirection()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;

        // Làm tròn để tránh floating-point noise khi đứng cùng ô
        if (Mathf.Abs(toPlayer.x) < 0.1f && Mathf.Abs(toPlayer.y) < 0.1f)
            return Vector2.zero; // đã đứng cùng ô với player

        Vector2 primaryDir;
        Vector2 secondaryDir;

        // Ưu tiên trục xa hơn
        if (Mathf.Abs(toPlayer.x) >= Mathf.Abs(toPlayer.y))
        {
            primaryDir = new Vector2(Mathf.Sign(toPlayer.x), 0f);
            secondaryDir = new Vector2(0f, Mathf.Sign(toPlayer.y));
        }
        else
        {
            primaryDir = new Vector2(0f, Mathf.Sign(toPlayer.y));
            secondaryDir = new Vector2(Mathf.Sign(toPlayer.x), 0f);
        }
        if (primaryDir == Vector2.up) primaryDir = Vector2.zero;
        if (secondaryDir == Vector2.up) secondaryDir = Vector2.zero;
        // Thử hướng ưu tiên trước, nếu bị chặn thì thử hướng phụ
        Vector2 primaryNext = (Vector2)transform.position + primaryDir * cellSize;
        Vector2 secondaryNext = (Vector2)transform.position + secondaryDir * cellSize;

        if (primaryDir != Vector2.zero && !IsBlocked(primaryNext)) return primaryDir;
        if (secondaryDir != Vector2.zero && !IsBlocked(secondaryNext)) return secondaryDir;

        return Vector2.zero; // cả 2 hướng đều bị chặn
    }


    private bool IsBlocked(Vector2 targetCenter)
    {
        if (collisionLayers == 0) return false;
        Collider2D hit = Physics2D.OverlapCircle(targetCenter, cellSize * 0.4f, collisionLayers);
        return hit != null;
    }


    private void CheckPlayerAtCurrentPosition()
    {
        if (hasDamaged) return;

        // Check xem player có đứng cùng ô không
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < cellSize * 0.5f)
            HitPlayer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDamaged) return;
        if (!other.CompareTag("Player")) return;
        HitPlayer();
    }

    private void HitPlayer()
    {
        hasDamaged = true;
        HealthManager.Instance?.TakeFire();
        AudioManager.Instance?.PlaySfx("dame_hit");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, Vector2.one * cellSize * 0.9f);
    }
}