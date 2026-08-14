using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gắn script này vào GameObject cha của mỗi layer (VD: "Layer1").
/// GameObject cha cần có SẴN ÍT NHẤT 1 child là sprite của layer (dùng
/// ParallaxAutoSetupTool để tạo, tool tạo sẵn 2 child PieceA/PieceB).
///
/// Cơ chế: tất cả các "piece" (bản sao sprite giống hệt nhau, đặt liền kề
/// theo trục X) cùng trôi sang phải với scrollSpeed riêng của layer.
/// Khi 1 piece trôi hết ra khỏi mép phải camera, nó được dịch chuyển ra
/// nối liền phía sau đuôi (bên trái) của piece đang ở xa nhất bên trái,
/// tạo vòng lặp vô hạn không hở.
///
/// ĐIỂM QUAN TRỌNG so với bản chỉ dùng 2 piece cố định:
/// Nếu chiều rộng 1 sprite NHỎ HƠN chiều rộng camera đang nhìn thấy (VD do
/// đổi Aspect Ratio, đổi độ phân giải, hoặc camera Orthographic Size lớn),
/// chỉ 2 piece sẽ KHÔNG đủ để phủ kín màn hình -> bị hở. Script này tự tính
/// số piece tối thiểu cần thiết dựa trên camera lúc Start(), và tự Instantiate
/// thêm piece còn thiếu (nhân bản từ piece có sẵn), nên luôn phủ kín camera
/// dù đổi tỉ lệ khung hình.
///
/// Cũng đã bảo toàn phần "dư" (overshoot) khi loop và bám lưới pixel
/// (pixel-perfect) để cuộn mượt, không giật, không nhòe hình pixel art.
/// </summary>
[DisallowMultipleComponent]
public class InfiniteScrollLayer : MonoBehaviour
{
    [Header("Tốc độ cuộn của layer này")]
    [Tooltip("Layer càng gần camera (tiền cảnh) nên để tốc độ càng cao. Lớp xa (bầu trời) để thấp.")]
    public float scrollSpeed = 1f;

    [Header("Camera dùng để tính điểm loop (để trống sẽ tự lấy Camera.main)")]
    public Camera targetCamera;

    [Header("Pixel-perfect")]
    [Tooltip("Bật để khóa vị trí X theo lưới pixel (world unit = 1/PPU), tránh rung/nhòe hình pixel art.")]
    public bool pixelSnap = true;

    [Tooltip("Pixels Per Unit của sprite đang dùng cho layer này. Phải khớp với PPU lúc import sprite.")]
    public float pixelsPerUnit = 100f;

    [Header("Số piece dự phòng")]
    [Tooltip("Số piece cộng thêm NGOÀI số piece tối thiểu cần để phủ kín camera lúc Start(). " +
             "Tăng lên (VD 2-3) nếu game có thể đổi Aspect Ratio / resize cửa sổ lúc đang chạy, " +
             "để có piece dự phòng sẵn thay vì phải chờ tự tạo thêm giữa lúc chơi.")]
    public int extraPieces = 1;

    [Header("Debug")]
    [Tooltip("Bật để xem log khi tự tạo thêm piece hoặc khi 1 piece được loop lại")]
    public bool debugLog = false;

    private readonly List<Transform> pieces = new List<Transform>();
    private readonly List<float> rawX = new List<float>(); // vị trí X "lý tưởng", chưa snap pixel
    private float pieceWidth;
    private float halfPieceWidth;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (transform.childCount < 1)
        {
            Debug.LogError($"[InfiniteScrollLayer] '{name}' cần ít nhất 1 child sprite (PieceA). " +
                            $"Hãy dùng ParallaxAutoSetupTool để tự động tạo, hoặc kiểm tra lại Hierarchy.");
            enabled = false;
            return;
        }

        Transform firstChild = transform.GetChild(0);
        SpriteRenderer sr = firstChild.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError($"[InfiniteScrollLayer] Child '{firstChild.name}' không có SpriteRenderer.");
            enabled = false;
            return;
        }

        pieceWidth = sr.bounds.size.x;
        halfPieceWidth = pieceWidth / 2f;

        if (pieceWidth <= 0f)
        {
            Debug.LogError($"[InfiniteScrollLayer] pieceWidth = 0 cho '{name}'. Kiểm tra sprite/scale.");
            enabled = false;
            return;
        }

        // Thu thập các piece có sẵn (do tool tạo, thường là 2) và sắp theo thứ tự trái -> phải
        for (int i = 0; i < transform.childCount; i++)
            pieces.Add(transform.GetChild(i));
        pieces.Sort((a, b) => a.position.x.CompareTo(b.position.x));

        // Tính số piece tối thiểu để phủ kín camera hiện tại, cộng thêm dự phòng
        int neededPieces = CalculateNeededPieceCount() + Mathf.Max(0, extraPieces);

        while (pieces.Count < neededPieces)
        {
            Transform last = pieces[pieces.Count - 1];
            GameObject clone = Instantiate(last.gameObject, transform);
            clone.name = $"{last.name}_auto{pieces.Count}";
            clone.transform.position = new Vector3(last.position.x + pieceWidth, last.position.y, last.position.z);
            pieces.Add(clone.transform);

            if (debugLog)
                Debug.Log($"[InfiniteScrollLayer] '{name}' tự tạo thêm piece #{pieces.Count} " +
                          $"vì camera rộng hơn số piece có sẵn.");
        }

        foreach (Transform p in pieces)
            rawX.Add(p.position.x);
    }

    private int CalculateNeededPieceCount()
    {
        if (targetCamera == null) return 2;
        float camWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
        // +1 để luôn có ít nhất 1 piece "chờ sẵn" ngoài vùng nhìn thấy, đủ thời gian loop mượt
        int count = Mathf.CeilToInt(camWidth / pieceWidth) + 1;
        return Mathf.Max(count, 2);
    }

    void Update()
    {
        if (targetCamera == null || pieceWidth <= 0f || pieces.Count == 0) return;

        float move = scrollSpeed * Time.deltaTime;
        for (int i = 0; i < rawX.Count; i++)
            rawX[i] += move;

        float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float cameraRightEdge = targetCamera.transform.position.x + camHalfWidth;

        for (int i = 0; i < pieces.Count; i++)
        {
            float pieceLeftEdge = rawX[i] - halfPieceWidth;
            if (pieceLeftEdge >= cameraRightEdge)
            {
                float overshoot = pieceLeftEdge - cameraRightEdge; // phần đã đi lố, giữ lại để không giật
                float leftmostX = FindLeftmostX(excludeIndex: i);
                float newX = (leftmostX - pieceWidth) + overshoot;
                rawX[i] = newX;

                if (debugLog)
                    Debug.Log($"[InfiniteScrollLayer] '{name}' loop piece '{pieces[i].name}' -> x={newX:F3}");
            }
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            Vector3 pos = pieces[i].position;
            pos.x = SnapX(rawX[i]);
            pieces[i].position = pos;
        }
    }

    private float FindLeftmostX(int excludeIndex)
    {
        float min = float.MaxValue;
        for (int i = 0; i < rawX.Count; i++)
        {
            if (i == excludeIndex) continue;
            if (rawX[i] < min) min = rawX[i];
        }
        return min;
    }

    private float SnapX(float x)
    {
        if (!pixelSnap || pixelsPerUnit <= 0f) return x;
        return Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (targetCamera == null) return;
        Gizmos.color = Color.yellow;
        float camHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float x = targetCamera.transform.position.x + camHalfWidth;
        Gizmos.DrawLine(new Vector3(x, -50, 0), new Vector3(x, 50, 0));
    }
#endif
}