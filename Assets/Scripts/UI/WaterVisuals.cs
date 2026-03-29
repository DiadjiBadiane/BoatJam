using UnityEngine;
using UnityEngine.UI;

public class AnimatedTiledRawImage : MonoBehaviour
{
    public float tileSize = 0f;
    public float tilesY = 4f;
    public Vector2 scrollSpeed = new Vector2(0.015f, 0.004f);

    RawImage rawImage;
    RectTransform rectTransform;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        Apply();
    }

    void OnEnable() => Apply();

    void Update() => Apply();

    void OnRectTransformDimensionsChange() => Apply();

    void Apply()
    {
        if (rawImage == null || rawImage.texture == null || rectTransform == null) return;

        rawImage.texture.wrapMode = TextureWrapMode.Repeat;

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f) return;

        float t = Time.unscaledTime;
        float uvWidth;
        float uvHeight;

        if (tileSize > 0f)
        {
            uvWidth = rect.width / tileSize;
            uvHeight = rect.height / tileSize;
        }
        else
        {
            float aspect = rect.width / rect.height;
            uvHeight = tilesY;
            uvWidth = aspect * tilesY;
        }

        rawImage.uvRect = new Rect(
            t * scrollSpeed.x,
            t * scrollSpeed.y,
            Mathf.Max(0.01f, uvWidth),
            Mathf.Max(0.01f, uvHeight));
    }
}

public class AnimatedMaterialOffset : MonoBehaviour
{
    public Renderer targetRenderer;
    public Vector2 scrollSpeed = new Vector2(0.02f, -0.01f);

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (targetRenderer == null) return;

        var material = targetRenderer.sharedMaterial;
        if (material == null) return;

        Vector2 offset = Time.unscaledTime * scrollSpeed;
        if (material.HasProperty("_BaseMap")) material.SetTextureOffset("_BaseMap", offset);
        if (material.HasProperty("_MainTex")) material.SetTextureOffset("_MainTex", offset);
    }
}
