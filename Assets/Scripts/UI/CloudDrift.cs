using UnityEngine;

public class CloudDrift : MonoBehaviour
{
    public float speed = 30f;
    public float resetX = -300f;
    public float startX = 500f;

    void Update()
    {
        transform.localPosition += Vector3.right * speed * Time.deltaTime;
        if (transform.localPosition.x > startX)
            transform.localPosition = new Vector3(resetX,
                transform.localPosition.y,
                transform.localPosition.z);
    }
}
