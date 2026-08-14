#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// EDITOR TOOL - đặt file này vào thư mục "Editor" bất kỳ trong Assets
/// (VD: Assets/Editor/ParallaxAutoSetupTool.cs), Unity sẽ tự loại nó khỏi build.
///
/// Cách dùng:
/// 1. Menu Unity: Tools > Parallax > Auto Setup Layers
/// 2. Kéo 11 Sprite (PNG đã import) vào danh sách "Layer Sprites" theo thứ tự
///    từ XA nhất (bầu trời) đến GẦN nhất (tiền cảnh).
/// 3. Set tốc độ scroll cho từng layer (hoặc dùng nút "Auto Fill Speed Gradient"
///    để tự sinh dải tốc độ tăng dần).
/// 4. Bấm "Generate Parallax Layers" -> tool sẽ tự:
///    - Tạo GameObject cha cho mỗi layer, đặt đúng Sorting Order (xa -> sau, gần -> trước)
///    - Tạo 2 child (PieceA, PieceB) đặt liền kề nhau theo chiều rộng sprite
///    - Gắn sẵn component InfiniteScrollLayer với scrollSpeed tương ứng
///
/// LƯU Ý: Tool KHÔNG còn tự động chỉnh Camera Orthographic Size nữa.
/// Bạn tự chỉnh Camera theo ý mình sau khi Generate.
/// </summary>
public class ParallaxAutoSetupTool : EditorWindow
{
    private class LayerEntry
    {
        public Sprite sprite;
        public float speed;
    }

    private System.Collections.Generic.List<LayerEntry> layers = new System.Collections.Generic.List<LayerEntry>();
    private Vector2 mainScroll;
    private Vector2 scrollPos;
    private float baseY = 0f;
    private string rootName = "ParallaxLayers";
    private float minSpeed = 0.3f;
    private float maxSpeed = 4f;

    [MenuItem("Tools/Parallax/Auto Setup Layers")]
    public static void ShowWindow()
    {
        var win = GetWindow<ParallaxAutoSetupTool>("Parallax Auto Setup");
        win.minSize = new Vector2(420, 500);
    }

    void OnEnable()
    {
        if (layers.Count == 0)
        {
            for (int i = 0; i < 12; i++)
                layers.Add(new LayerEntry { sprite = null, speed = 1f });
        }
    }

    void OnGUI()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        EditorGUILayout.LabelField("Parallax Auto Setup (11 layers)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Kéo sprite vào theo thứ tự: layer 1 = XA nhất (bầu trời), layer cuối = GẦN nhất (tiền cảnh).",
            MessageType.Info);

        rootName = EditorGUILayout.TextField("Tên GameObject gốc", rootName);
        baseY = EditorGUILayout.FloatField("Vị trí Y chung", baseY);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Danh sách Layer (từ xa -> gần)", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(280));
        for (int i = 0; i < layers.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField($"L{i + 1}", GUILayout.Width(25));
            layers[i].sprite = (Sprite)EditorGUILayout.ObjectField(layers[i].sprite, typeof(Sprite), false, GUILayout.Width(140));
            EditorGUILayout.LabelField("Tốc độ", GUILayout.Width(45));
            layers[i].speed = EditorGUILayout.FloatField(layers[i].speed, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Thêm layer"))
            layers.Add(new LayerEntry { sprite = null, speed = 1f });
        if (GUILayout.Button("- Xóa layer cuối") && layers.Count > 1)
            layers.RemoveAt(layers.Count - 1);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Tự động sinh tốc độ tăng dần", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        minSpeed = EditorGUILayout.FloatField("Speed Min (xa)", minSpeed);
        maxSpeed = EditorGUILayout.FloatField("Speed Max (gần)", maxSpeed);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("Auto Fill Speed Gradient (min -> max)"))
        {
            AutoFillSpeeds();
        }

        EditorGUILayout.Space(15);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Parallax Layers", GUILayout.Height(36)))
        {
            GenerateLayers();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void AutoFillSpeeds()
    {
        int n = layers.Count;
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            layers[i].speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        }
    }

    private void GenerateLayers()
    {
        // Validate
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].sprite == null)
            {
                EditorUtility.DisplayDialog("Thiếu sprite", $"Layer {i + 1} chưa có sprite. Vui lòng kéo đủ sprite trước khi generate.", "OK");
                return;
            }
        }

        try
        {
            CheckPPUConsistency();
        }
        catch (System.OperationCanceledException)
        {
            return; // người dùng bấm Hủy ở dialog cảnh báo PPU
        }

        GameObject root = GameObject.Find(rootName);
        if (root == null)
            root = new GameObject(rootName);

        Undo.RegisterCreatedObjectUndo(root, "Create Parallax Root");

        for (int i = 0; i < layers.Count; i++)
        {
            CreateLayer(root.transform, layers[i].sprite, layers[i].speed, i, layers.Count);
        }

        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Hoàn tất",
            $"Đã tạo {layers.Count} layer parallax trong '{rootName}'.\n" +
            "Kiểm tra Hierarchy và nhấn Play để xem kết quả.\n" +
            "Lưu ý: bạn cần tự chỉnh Camera Orthographic Size theo ý muốn.", "OK");
    }

    /// <summary>
    /// Đọc PPU thực tế từ Texture Import Settings (không dùng sprite.pixelsPerUnit
    /// vì muốn cảnh báo rõ nếu người dùng đổi PPU nhưng chưa Apply, hoặc PPU lệch giữa các layer).
    /// </summary>
    private float GetSpritePPU(Sprite sprite)
    {
        if (sprite == null) return 100f;
        string path = AssetDatabase.GetAssetPath(sprite);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
            return importer.spritePixelsPerUnit;
        return sprite.pixelsPerUnit; // fallback
    }

    private void CheckPPUConsistency()
    {
        float? firstPPU = null;
        System.Text.StringBuilder mismatch = new System.Text.StringBuilder();

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].sprite == null) continue;
            float ppu = GetSpritePPU(layers[i].sprite);
            if (firstPPU == null) firstPPU = ppu;
            else if (!Mathf.Approximately(ppu, firstPPU.Value))
                mismatch.AppendLine($"  Layer {i + 1} ({layers[i].sprite.name}): PPU = {ppu} (khác với PPU chuẩn {firstPPU.Value})");
        }

        if (mismatch.Length > 0)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Cảnh báo: PPU không đồng nhất",
                $"Các sprite sau có Pixels Per Unit khác với layer đầu tiên:\n\n{mismatch}\n" +
                "Điều này khiến tỉ lệ giữa các layer bị lệch nhau trong world space.\n" +
                "Bạn vẫn muốn tiếp tục generate?",
                "Vẫn tiếp tục", "Hủy");
            if (!proceed)
                throw new System.OperationCanceledException();
        }
    }

    private void CreateLayer(Transform parentRoot, Sprite sprite, float speed, int index, int total)
    {
        string layerName = $"Layer_{index + 1}_{sprite.name}";

        // Xóa layer cùng tên nếu đã tồn tại (để chạy lại tool không bị trùng)
        Transform existing = parentRoot.Find(layerName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject layerObj = new GameObject(layerName);
        Undo.RegisterCreatedObjectUndo(layerObj, "Create Layer");
        layerObj.transform.SetParent(parentRoot, false);
        layerObj.transform.position = new Vector3(0, baseY, 0);

        // Tính chiều rộng sprite thực tế (theo world units, dựa trên pixels per unit + scale mặc định)
        float width = sprite.bounds.size.x;

        // Piece A
        GameObject pieceA = CreateSpritePiece(layerObj.transform, sprite, "PieceA", index, total);
        pieceA.transform.localPosition = Vector3.zero;

        // Piece B - đặt liền kề ngay bên phải Piece A
        GameObject pieceB = CreateSpritePiece(layerObj.transform, sprite, "PieceB", index, total);
        pieceB.transform.localPosition = new Vector3(width, 0, 0);

        // Gắn script InfiniteScrollLayer
        InfiniteScrollLayer scroll = layerObj.AddComponent<InfiniteScrollLayer>();
        scroll.scrollSpeed = speed;
        scroll.targetCamera = Camera.main;
    }

    private GameObject CreateSpritePiece(Transform parent, Sprite sprite, string pieceName, int layerIndex, int total)
    {
        GameObject piece = new GameObject(pieceName);
        Undo.RegisterCreatedObjectUndo(piece, "Create Piece");
        piece.transform.SetParent(parent, false);

        SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // Sorting order: layer xa (index nhỏ) nằm dưới, layer gần (index lớn) nằm trên
        sr.sortingOrder = layerIndex;

        return piece;
    }
}
#endif