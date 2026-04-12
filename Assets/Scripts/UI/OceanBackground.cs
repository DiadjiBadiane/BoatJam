// Assets/Scripts/UI/OceanBackground.cs
// Procedural animated ocean that fills the camera background behind the puzzle grid.
// Uses Custom/OceanWater shader for animated waves, caustics, and foam — all in the fragment shader.
// Auto-bootstraps onto the Main Camera at runtime — no manual scene setup needed.

using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class OceanBackground : MonoBehaviour
{
    // ── Auto-bootstrap ────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttachInCurrentScene();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAttachInCurrentScene();

    static void TryAttachInCurrentScene()
    {
        // Need a GridManager present (only in GameScene)
        if (GridManager.Instance == null && Object.FindObjectOfType<GridManager>() == null) return;
        if (Object.FindObjectOfType<OceanBackground>() != null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        cam.gameObject.AddComponent<OceanBackground>();
        Debug.Log("[OceanBackground] Auto-attached to Main Camera");
    }

    // ── Config ────────────────────────────────────────────────────────────────
    const float PLANE_SIZE   = 120f;    // world units — big enough to always fill camera
    const int   SUBDIVISIONS = 60;      // mesh resolution for vertex wave displacement
    const float PLANE_Y      = -0.55f;  // sit well below the grid and boats

    Camera     _cam;
    Mesh       _mesh;
    Material   _mat;
    GameObject _plane;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _cam = GetComponent<Camera>() ?? Camera.main;
        if (_cam != null)
        {
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Hex("061a20"); // dark harbor water fallback
        }

        BuildOceanPlane();
    }

    void OnDisable()
    {
        if (_plane != null) { DestroyImmediate(_plane); _plane = null; }
        if (_mat   != null) { DestroyImmediate(_mat);   _mat = null; }
        if (_mesh  != null) { DestroyImmediate(_mesh);  _mesh = null; }
    }

    void Update()
    {
        // Retry building if the grid wasn't ready on first attempt
        if (_plane == null) BuildOceanPlane();
    }

    // ── Build the ocean mesh ──────────────────────────────────────────────────

    void BuildOceanPlane()
    {
        if (_plane != null) return;

        _mesh = new Mesh { name = "OceanSurface" };

        int vPerSide  = SUBDIVISIONS + 1;
        int vertCount = vPerSide * vPerSide;
        var verts = new Vector3[vertCount];
        var uvs   = new Vector2[vertCount];

        // Centre the plane on the grid
        float gridCX = 3.5f;
        float gridCZ = 3.5f;
        if (GridManager.Instance != null)
        {
            float cs = GridManager.Instance.cellSize;
            gridCX = GridManager.Instance.width  * cs * 0.5f - cs * 0.5f;
            gridCZ = GridManager.Instance.height * cs * 0.5f - cs * 0.5f;
        }

        float halfSize = PLANE_SIZE * 0.5f;
        for (int z = 0; z < vPerSide; z++)
        {
            for (int x = 0; x < vPerSide; x++)
            {
                int i = z * vPerSide + x;
                float tx = (float)x / SUBDIVISIONS;
                float tz = (float)z / SUBDIVISIONS;

                verts[i] = new Vector3(
                    gridCX - halfSize + tx * PLANE_SIZE,
                    PLANE_Y,
                    gridCZ - halfSize + tz * PLANE_SIZE);
                uvs[i] = new Vector2(tx, tz);
            }
        }

        // Triangles
        var tris = new int[SUBDIVISIONS * SUBDIVISIONS * 6];
        int ti = 0;
        for (int z = 0; z < SUBDIVISIONS; z++)
        {
            for (int x = 0; x < SUBDIVISIONS; x++)
            {
                int bl = z * vPerSide + x;
                int br = bl + 1;
                int tl = bl + vPerSide;
                int tr = tl + 1;
                tris[ti++] = bl; tris[ti++] = tl; tris[ti++] = br;
                tris[ti++] = br; tris[ti++] = tl; tris[ti++] = tr;
            }
        }

        _mesh.vertices  = verts;
        _mesh.uv        = uvs;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        _mesh.bounds = new Bounds(
            new Vector3(gridCX, PLANE_Y, gridCZ),
            Vector3.one * PLANE_SIZE * 2f);

        // Material — use Custom/OceanWater shader (all animation is GPU-side)
        _mat = BuildOceanMaterial();

        // Spawn the plane — in world space, not parented to camera
        _plane = new GameObject("OceanWavePlane");
        _plane.transform.position = Vector3.zero;
        _plane.transform.rotation = Quaternion.identity;

        var mf = _plane.AddComponent<MeshFilter>();
        mf.sharedMesh = _mesh;

        var mr = _plane.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Debug.Log($"[OceanBackground] Ocean plane created. Shader: {_mat.shader.name}");
    }

    // ── Material ──────────────────────────────────────────────────────────────

    Material BuildOceanMaterial()
    {
        Shader shader = Shader.Find("Custom/OceanWater");

        // Fallback chain if custom shader hasn't compiled yet
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var mat = new Material(shader)
        {
            name = "OceanSurfaceMat",
            hideFlags = HideFlags.HideAndDontSave
        };

        // Harbor water colors — close tones for uniform look
        if (mat.HasProperty("_DeepColor"))    mat.SetColor("_DeepColor",    Hex("0b2e38"));
        if (mat.HasProperty("_MidColor"))     mat.SetColor("_MidColor",     Hex("0f3a45"));
        if (mat.HasProperty("_ShallowColor")) mat.SetColor("_ShallowColor", Hex("144850"));
        if (mat.HasProperty("_FoamColor"))    mat.SetColor("_FoamColor",    Hex("a8c8c4"));
        if (mat.HasProperty("_CausticColor")) mat.SetColor("_CausticColor", new Color(0.10f, 0.25f, 0.22f, 0.2f));

        // Fallback
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Hex("0f3a45"));
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color",     Hex("0f3a45"));

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry - 1;
        return mat;
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }
}