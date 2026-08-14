using UnityEngine;
using UnityEngine.Tilemaps;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController2D : MonoBehaviour
{
    public enum FacingDirection { Down = 0, Up = 1, Left = 2, Right = 3 }

    [Header("Grid Movement")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float moveTime = 0.2f;

    [SerializeField] private LayerMask collisionLayers;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private Tilemap breakableTilemap;
    [Header("Map Bounds")]
    [SerializeField] private Vector2 minBounds; // ví dụ: (-5, -3)
    [SerializeField] private Vector2 maxBounds; // ví dụ: (5, 3)
    [Header("Fire Spawn")]
    [SerializeField] private GameObject firePrefab;      // kéo prefab lửa vào đây
    [SerializeField][Range(0f, 1f)] private float fireSpawnChance = 0.3f; // 30% tỉ lệ spawn

    private Rigidbody2D rb;
    private Animator animator;
    private ScoreManager scoreManager;

    private bool isMoving;
    private Vector2 startPos;
    private Vector2 targetPos;
    private float moveLerp;
    private FacingDirection facing = FacingDirection.Down;
    private bool isAttacking;
    private float attackTimer;

    private static readonly int HashDirection = Animator.StringToHash("Direction");
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int HashAttack = Animator.StringToHash("Attack");

    private static readonly Vector2 lockedDirection = Vector2.up;
    [Header("Mobile Input")]
    private Vector2 pendingMoveDir = Vector2.zero;

    private bool mobileAttackPress = false;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        scoreManager = GetComponent<ScoreManager>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (isMoving)
        {
            TweenMove();
            return;
        }
        HandleAttackInput();
        if (!isAttacking)
            HandleMoveInput();
        TickAttackCooldown();
        UpdateAnimator();
    }
    public void PressMobileMove(Vector2 dir)
    {
        pendingMoveDir = dir;
    }
    public void PressMobileAttack()
    {
        mobileAttackPress = true;
    }
    private void TweenMove()
    {
        moveLerp += Time.deltaTime / moveTime;
        moveLerp = Mathf.Clamp01(moveLerp);

        float t = Mathf.SmoothStep(0f, 1f, moveLerp);//
        rb.MovePosition(Vector2.Lerp(startPos, targetPos, t));//
        if (moveLerp >= 1f)
        {
            rb.MovePosition(targetPos);
            isMoving = false;
            UpdateAnimator();
            CheckCollectibleAtPosition();
        }
    }
    void CheckCollectibleAtPosition()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f);
        foreach (Collider2D hit in hits)
        {
            CollectibleItem item = hit.GetComponent<CollectibleItem>();
            if (item == null) continue;
            if (item.IsHeart)
            {
                HealthManager.Instance?.HealFromPickup();
            }
            else
            {
                scoreManager.AddScore(item.ScoreValue);
            }
            AudioManager.Instance?.PlaySfx("item_pickup");
            Destroy(hit.gameObject);
        }
    }
    private void HandleMoveInput()
    {
        // float x = Input.GetAxisRaw("Horizontal");
        // float y = Input.GetAxisRaw("Vertical");

        // Vector2 dir;
        // if (Mathf.Abs(x) >= Mathf.Abs(y))
        //     dir = new Vector2(Mathf.Sign(x) * (Mathf.Abs(x) > 0.01f ? 1f : 0f), 0f);
        // else
        //     dir = new Vector2(0f, Mathf.Sign(y) * (Mathf.Abs(y) > 0.01f ? 1f : 0f));
        Vector2 dir = pendingMoveDir;

        if (dir == Vector2.zero) return;

        if (dir == lockedDirection) 
        {
            pendingMoveDir = Vector2.zero;
            return;
        }

        facing = GetDirectionFromVector(dir);

        // Kiểm tra vật cản trước khi cho di chuyển
        Vector2 next = (Vector2)transform.position + dir * cellSize;
        if (IsBlocked(next)) 
        {
            pendingMoveDir = Vector2.zero;
            return;
        }

        // Bắt đầu tween
        startPos = transform.position;
        targetPos = next;
        moveLerp = 0f;
        isMoving = true;

        pendingMoveDir = Vector2.zero;
    }
    private void HandleAttackInput()
    {
        // if (Input.GetKeyDown(KeyCode.Space) && attackTimer <= 0f && !isAttacking)
        // {
        //     Attack();
        // }
        if (mobileAttackPress && attackTimer <= 0f && !isAttacking)
        {
            mobileAttackPress = false;
            Attack();
        }
        else if(mobileAttackPress)
        {
            mobileAttackPress = true;
        }
    }
    private bool IsBlocked(Vector2 targetCenter)
    {
        if (targetCenter.x < minBounds.x || targetCenter.x > maxBounds.x
        || targetCenter.y < minBounds.y || targetCenter.y > maxBounds.y)
            return true;
        Vector3Int cellPos = breakableTilemap.WorldToCell(targetCenter);//
        // Collider2D hit = Physics2D.OverlapCircle(targetCenter, cellSize * 0.5f, collisionLayers);
        return breakableTilemap.HasTile(cellPos);
    }

    private void UpdateAnimator()
    {
        animator.SetInteger(HashDirection, (int)facing);
        animator.SetBool(HashIsMoving, isMoving && !isAttacking);
    }

    private void Attack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        animator.SetTrigger(HashAttack);
    }

    private void TickAttackCooldown()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f && isAttacking) isAttacking = false;
    }

    public void DealDamage()
    {
        Vector2 attackOrigin = (Vector2)transform.position + GetDirectionOffset(facing) * cellSize;
        Vector3Int cellPos = breakableTilemap.WorldToCell(attackOrigin);

        if (breakableTilemap.HasTile(cellPos))
        {
            breakableTilemap.SetTile(cellPos, null);
            if(Random.value <= fireSpawnChance && firePrefab != null)
            {
                Vector3 spawnPos = breakableTilemap.GetCellCenterWorld(cellPos);
                Instantiate(firePrefab, spawnPos, Quaternion.identity);
            }
            AudioManager.Instance?.PlaySfx("brick_broken");
            Debug.Log($"Đã phá vỡ: {cellPos}");
        }
    }
    public void EndAttack()
    {
        isAttacking = false;
    }
    private FacingDirection GetDirectionFromVector(Vector2 dir)
    {
        if (dir.x > 0f) return FacingDirection.Right;
        if (dir.x < 0f) return FacingDirection.Left;
        return dir.y > 0f ? FacingDirection.Up : FacingDirection.Down;
    }
    private Vector2 GetDirectionOffset(FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.Up: return Vector2.up;
            case FacingDirection.Down: return Vector2.down;
            case FacingDirection.Left: return Vector2.left;
            case FacingDirection.Right: return Vector2.right;
            default: return Vector2.down;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 attackOrigin = (Vector2)transform.position + GetDirectionOffset(facing) * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(attackOrigin, attackRange);

        Gizmos.color = Color.yellow;
        foreach (Vector2 d in new[] { Vector2.down, Vector2.left, Vector2.right })
            Gizmos.DrawWireCube((Vector2)transform.position + d * cellSize, Vector2.one * cellSize * 0.9f);
    }
}