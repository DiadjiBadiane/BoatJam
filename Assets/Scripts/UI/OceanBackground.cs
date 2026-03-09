// Assets/Scripts/Rendering/OceanBackground.cs
// Attach to any GameObject in GameScene (or let UIManager find it).
// Renders a full-screen ocean gradient behind everything, matching the HTML mockup.
// No materials, no textures — pure code.

using UnityEngine;

[ExecuteAlways]
public class OceanBackground : MonoBehaviour
{
    // ── Sky gradient stops (top → bottom), matching the HTML mockup ──────────
    // #0ea5e9 → #0284c7 → #0369a1 → #1e3a5f → #0f2239
    static readonly Color SKY_TOP    = Hex("0ea5e9");
    static readonly Color SKY_MID1   = Hex("0284c7");
    static readonly Color SKY_MID2   = Hex("0369a1");
    static readonly Color SEA_TOP    = Hex("1e3a5f");
    static readonly Color SEA_BOTTOM = Hex("0f2239");

    Camera   _cam;
    Mesh     _mesh;
    Material _mat;

    // ─────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _cam = GetComponent<Camera>() ?? Camera.main;
        if (_cam != null) _cam.clearFlags = CameraClearFlags.SolidColor;

        BuildQuad();
        BuildMaterial();
    }

    void OnDisable()
    {
        if (_mat  != null) DestroyImmediate(_mat);
        if (_mesh != null) DestroyImmediate(_mesh);
    }

    void OnRenderObject()
    {
        if (_mat == null || _mesh == null) return;
        _mat.SetPass(0);
        Graphics.DrawMeshNow(_mesh, Matrix4x4.identity);
    }

    // ── Build a full-screen quad in clip space (-1..1) ────────────────────────

    void BuildQuad()
    {
        if (_mesh != null) return;
        _mesh = new Mesh { name = "OceanBG_Quad" };

        // Five rows of vertices so we can assign the 5 gradient stops
        // y positions in NDC: 1 (top), 0.5, 0, -0.5, -1 (bottom)
        float[] ys = { 1f, 0.5f, 0f, -0.5f, -1f };
        Color[] cols = { SKY_TOP, SKY_MID1, SKY_MID2, SEA_TOP, SEA_BOTTOM };

        var verts  = new Vector3[ys.Length * 2];
        var colors = new Color[ys.Length * 2];
        for (int i = 0; i < ys.Length; i++)
        {
            verts[i * 2]     = new Vector3(-1f, ys[i], 0.999f);
            verts[i * 2 + 1] = new Vector3( 1f, ys[i], 0.999f);
            colors[i * 2]     = cols[i];
            colors[i * 2 + 1] = cols[i];
        }

        // Two triangles per strip segment
        int segCount = ys.Length - 1;
        var tris = new int[segCount * 6];
        for (int s = 0; s < segCount; s++)
        {
            int b = s * 6, r = s * 2;
            tris[b]   = r;     tris[b+1] = r+2; tris[b+2] = r+1;
            tris[b+3] = r+1;   tris[b+4] = r+2; tris[b+5] = r+3;
        }

        _mesh.vertices  = verts;
        _mesh.colors    = colors;
        _mesh.triangles = tris;
        _mesh.bounds    = new Bounds(Vector3.zero, Vector3.one * 999f);
    }

    // ── Unlit vertex-color material ───────────────────────────────────────────

    void BuildMaterial()
    {
        if (_mat != null) return;
        // "Sprites/Default" is always available in Unity; vertex color + unlit
        var shader = Shader.Find("Unlit/Color");
        // Use vertex-color shader if available, otherwise fallback
        var vcShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Unlit/Color");

        // Best cross-platform choice for vertex-colored unlit geometry:
        _mat = new Material(Shader.Find("Hidden/Internal-Colored") ?? Shader.Find("Unlit/Color"))
        {
            name = "OceanBG_Mat",
            hideFlags = HideFlags.HideAndDontSave
        };
        _mat.SetInt("_ZWrite", 0);
        _mat.SetInt("_ZTest",  (int)UnityEngine.Rendering.CompareFunction.Always);
        _mat.renderQueue = -1; // render before everything else
    }

    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }
}