// Assets/Scripts/Core/BoatMeshFitter.cs
using UnityEngine;

/// <summary>
/// Attach to a boat prefab root alongside BoatMovement.
/// - Rotates the mesh to correct orientation.
/// - Scales the mesh so it fits exactly <cellCount> grid cells.
/// - Replaces any existing collider with a BoxCollider sized to the grid
///   footprint so raycasts (click/tap selection) always work correctly.
/// </summary>
[DisallowMultipleComponent]
public class BoatMeshFitter : MonoBehaviour
{
    [Header("Fit")]
    [Tooltip("How many grid cells this boat spans along its movement axis.")]
    [SerializeField] int cellCount = 2;

    [Tooltip("How many grid cells this boat spans perpendicular to its movement axis.")]
    [SerializeField] int cellWidth = 1;

    [Tooltip("Scale multiplier after fitting — tweak for breathing room.")]
    [SerializeField, Range(0.5f, 4.0f)] float scaleFactor = 0.88f;

    [Header("Rotation correction")]
    [Tooltip("Applied on top of the prefab's existing rotation. " +
             "Use X=90 to lay a vertical mesh flat; Y=90/-90 to fix facing.")]
    [SerializeField] Vector3 rotationOffset = Vector3.zero;

    [Header("Collider")]
    [Tooltip("Height of the BoxCollider (world units). Raise this if clicks miss " +
             "from a perspective camera angle.")]
    [SerializeField] float colliderHeight = 0.4f;

    [Header("Vertical placement")]
    [Tooltip("Small gap kept between the hull and the water plane.")]
    [SerializeField] float waterClearance = 0.02f;

    public float WorldYOffset { get; private set; }
    public Vector3 WorldCenterOffsetXZ { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        ApplyRotation();
        FitToGrid();
        // Collider rebuilt in Start — LevelLoader sets BoatMovement.size /
        // isHorizontal after Awake, so we must wait one frame.
    }

    void Start()
    {
        RebuildCollider();
    }

    /// <summary>
    /// Runtime hook for loaders to enforce consistent visual sizing.
    /// </summary>
    public void ApplyFitSettings(int newCellCount, int newCellWidth, float newScaleFactor, float newColliderHeight)
    {
        cellCount = Mathf.Max(1, newCellCount);
        cellWidth = Mathf.Max(1, newCellWidth);
        scaleFactor = Mathf.Max(0.1f, newScaleFactor);
        colliderHeight = Mathf.Max(0.01f, newColliderHeight);

        FitToGrid();
        RebuildCollider();
    }

    // ── Step 1: rotation ──────────────────────────────────────────────────────

    void ApplyRotation()
    {
        if (rotationOffset != Vector3.zero)
            transform.rotation *= Quaternion.Euler(rotationOffset);
    }

    // ── Step 2: scale to fit grid ─────────────────────────────────────────────

    void FitToGrid()
    {
        // Prefer the root hull mesh when present.
        // Child accessory meshes (props/windows/wheels) can inflate bounds and
        // make the computed scale too small.
        var rootFilter = GetComponent<MeshFilter>();
        var filters = rootFilter != null
            ? new[] { rootFilter }
            : GetComponentsInChildren<MeshFilter>(true);

        if (filters.Length == 0 || filters[0].sharedMesh == null)
        {
            Debug.LogWarning($"[BoatMeshFitter] No MeshFilter on '{name}' — skipping scale fit.");
            return;
        }

        transform.localScale = Vector3.one;   // reset before measuring

        bool foundValidBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] == null || filters[i].sharedMesh == null)
                continue;

            Bounds worldBounds = TransformBounds(filters[i].sharedMesh.bounds, filters[i].transform);
            if (!foundValidBounds)
            {
                bounds = worldBounds;
                foundValidBounds = true;
            }
            else
            {
                bounds.Encapsulate(worldBounds);
            }
        }

        if (!foundValidBounds)
        {
            Debug.LogWarning($"[BoatMeshFitter] MeshFilter exists on '{name}' but has no mesh — skipping scale fit.");
            return;
        }

        float cell        = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;
        float targetLong  = cell * cellCount * scaleFactor;
        float targetShort = cell * cellWidth * scaleFactor;
        float meshLong    = Mathf.Max(bounds.size.x, bounds.size.z);
        float meshShort   = Mathf.Min(bounds.size.x, bounds.size.z);

        if (meshLong <= 0f)
        {
            Debug.LogWarning($"[BoatMeshFitter] Mesh length is zero on '{name}' — skipping scale fit.");
            return;
        }

        float byLong  = targetLong / meshLong;
        float byShort = meshShort > 0f ? targetShort / meshShort : byLong;
        float s = Mathf.Min(byLong, byShort);
        transform.localScale = Vector3.one * s;

        UpdateWorldYOffset(filters);

        Debug.Log($"[BoatMeshFitter] '{name}': meshLong={meshLong:F3} meshShort={meshShort:F3} targetLong={targetLong:F3} targetShort={targetShort:F3} scale={s:F4}");
    }

    // ── Step 3: rebuild collider to match grid footprint ─────────────────────
    // We always use a plain BoxCollider sized to the boat's grid footprint.
    // This is rotation-independent and always works with Physics.Raycast.

    void RebuildCollider()
    {
        // Remove every collider on this object and all children
        foreach (var col in GetComponentsInChildren<Collider>(true))
            Destroy(col);

        float cell = GridManager.Instance != null ? GridManager.Instance.cellSize : 1f;

        var  bm           = GetComponent<BoatMovement>();
        int  size         = bm != null ? bm.size         : cellCount;
        int  width        = bm != null ? bm.WidthCells   : cellWidth;
        bool isHorizontal = bm != null ? bm.isHorizontal : true;

        float longSide  = cell * size;
        float shortSide = cell * width;

        // Add a generous margin on every side so clicks near the edge of the
        // boat — or slightly outside it — still register. The collider is kept
        // invisible and does not affect gameplay, so a bigger hitbox only helps.
        float margin = cell * 0.25f;

        var box    = gameObject.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size   = isHorizontal
            ? new Vector3((longSide  + margin) / Mathf.Max(0.001f, transform.lossyScale.x),
                          Mathf.Max(colliderHeight, cell * 0.6f) / Mathf.Max(0.001f, transform.lossyScale.y),
                          (shortSide + margin) / Mathf.Max(0.001f, transform.lossyScale.z))
            : new Vector3((shortSide + margin) / Mathf.Max(0.001f, transform.lossyScale.x),
                          Mathf.Max(colliderHeight, cell * 0.6f) / Mathf.Max(0.001f, transform.lossyScale.y),
                          (longSide  + margin) / Mathf.Max(0.001f, transform.lossyScale.z));
    }

    // ── Helper: transform local mesh bounds into world space ──────────────────

    static Bounds TransformBounds(Bounds local, Transform t)
    {
        Vector3 c = local.center, e = local.extents;
        var b = new Bounds(t.TransformPoint(c), Vector3.zero);
        b.Encapsulate(t.TransformPoint(c + new Vector3( e.x,  e.y,  e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3(-e.x,  e.y,  e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3( e.x, -e.y,  e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3(-e.x, -e.y,  e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3( e.x,  e.y, -e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3(-e.x,  e.y, -e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3( e.x, -e.y, -e.z)));
        b.Encapsulate(t.TransformPoint(c + new Vector3(-e.x, -e.y, -e.z)));
        return b;
    }

    void UpdateWorldYOffset(MeshFilter[] filters)
    {
        bool foundValidBounds = false;
        Bounds bounds = default;

        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i] == null || filters[i].sharedMesh == null)
                continue;

            Bounds worldBounds = TransformBounds(filters[i].sharedMesh.bounds, filters[i].transform);
            if (!foundValidBounds)
            {
                bounds = worldBounds;
                foundValidBounds = true;
            }
            else
            {
                bounds.Encapsulate(worldBounds);
            }
        }

        if (!foundValidBounds)
        {
            WorldYOffset = 0f;
            WorldCenterOffsetXZ = Vector3.zero;
            return;
        }

        // Compensates off-center prefab pivots so visual hull center sits on the
        // logical grid footprint center.
        WorldCenterOffsetXZ = new Vector3(
            bounds.center.x - transform.position.x,
            0f,
            bounds.center.z - transform.position.z);

        WorldYOffset = Mathf.Max(waterClearance, -bounds.min.y + waterClearance);
    }
}