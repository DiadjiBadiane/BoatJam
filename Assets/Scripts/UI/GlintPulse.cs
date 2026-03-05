using UnityEngine;
using UnityEngine.UI;

public class GlintPulse : MonoBehaviour
{
    Image img;
    void Start() => img = GetComponent<Image>();
    void Update()
    {
        float t = Mathf.PingPong(Time.time * 0.5f, 1f);
        img.color = new Color(1f, 0.86f, 0.31f, Mathf.Lerp(0.2f, 0.55f, t));
        GetComponent<RectTransform>().sizeDelta =
            new Vector2(Mathf.Lerp(160f, 240f, t), 10f);
    }
}
