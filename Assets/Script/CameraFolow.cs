using UnityEngine;

/// <summary>
/// Tự điều khiển camera follow player, có giới hạn bounds (thay thế Cinemachine Confiner2D).
/// Khi camera chạm giới hạn, camera dừng lại (không đi tiếp) nhưng Player vẫn được tự do
/// di chuyển tới cuối bounds. Camera chỉ follow lại khi Player quay về vùng camera có thể theo.
///
/// Cách dùng:
/// - Gắn script này lên Main Camera (Orthographic).
/// - Kéo Player vào field "target".
/// - Kéo Collider2D giới hạn map (ví dụ LimitMap) vào field "boundsCollider".
/// - Tắt/gỡ Cinemachine Position Composer + Confiner2D khỏi CinemachineCamera (không cần nữa).
/// </summary>
[RequireComponent(typeof(Camera))]
public class PlayerFollowCameraLimited : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Player

    [Header("Bounds")]
    [SerializeField] private Collider2D boundsCollider; // Collider2D giới hạn map (LimitMap)

    [Header("Follow Settings")]
    [SerializeField] private Vector2 offset = Vector2.zero;
    [SerializeField] private float smoothTime = 0.15f; // độ mượt khi follow (giống Damping)
    [SerializeField] private bool limitX = true;
    [SerializeField] private bool limitY = true;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;

    // Giới hạn thực tế mà TÂM CAMERA được phép di chuyển vào (đã trừ nửa viewport)
    private float minCamX, maxCamX, minCamY, maxCamY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        RecalculateBounds();
    }

    /// <summary>
    /// Tính lại giới hạn camera dựa trên bounds collider + kích thước viewport hiện tại.
    /// Gọi lại hàm này nếu Orthographic Size thay đổi (ví dụ đổi độ phân giải màn hình).
    /// </summary>
    public void RecalculateBounds()
    {
        if (boundsCollider == null)
        {
            Debug.LogWarning("[CameraFollow] Chưa gán boundsCollider!");
            return;
        }

        Bounds b = boundsCollider.bounds;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        // Nếu map nhỏ hơn viewport theo 1 chiều nào đó, camera sẽ đứng yên giữa map theo chiều đó
        if (b.size.x < camHalfWidth * 2f)
        {
            minCamX = maxCamX = b.center.x;
        }
        else
        {
            minCamX = b.min.x + camHalfWidth;
            maxCamX = b.max.x - camHalfWidth;
        }

        if (b.size.y < camHalfHeight * 2f)
        {
            minCamY = maxCamY = b.center.y;
        }
        else
        {
            minCamY = b.min.y + camHalfHeight;
            maxCamY = b.max.y - camHalfHeight;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        // Clamp vị trí camera mong muốn vào trong giới hạn cho phép
        float clampedX = limitX ? Mathf.Clamp(desiredPos.x, minCamX, maxCamX) : desiredPos.x;
        float clampedY = limitY ? Mathf.Clamp(desiredPos.y, minCamY, maxCamY) : desiredPos.y;

        Vector3 targetPos = new Vector3(clampedX, clampedY, transform.position.z);

        // Làm mượt việc di chuyển camera (giống Damping của Cinemachine)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }

    // Vẽ gizmo giới hạn camera trong Scene view để dễ debug
    private void OnDrawGizmosSelected()
    {
        if (boundsCollider == null || cam == null) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minCamX + maxCamX) / 2f, (minCamY + maxCamY) / 2f, 0f);
        Vector3 size = new Vector3(maxCamX - minCamX, maxCamY - minCamY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}